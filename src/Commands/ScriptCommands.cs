using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Commands;

/// <summary>
/// The commands a script asked players be given, and the only place a script's
/// handler is called. Sole owner of what happens when one of them fails.
/// </summary>
/// <remarks>
/// Nothing is registered as a script runs. A run records what it wants and
/// <see cref="Activate"/> carries it out, so a run that is thrown away leaves the
/// server's command list untouched. That matters more here than it does for events:
/// the game refuses a command whose name is already taken, so a second run
/// registering as it went would not merely duplicate the command, it would fail.
/// </remarks>
/// <param name="api">Server whose command list these join.</param>
/// <param name="ours">
/// Names a previous run of these same scripts already registered. A dry run declares
/// them all over again and would otherwise refuse every one as taken, so the names
/// this mod put there itself are not a clash.
/// </param>
public sealed class ScriptCommands(ICoreServerAPI api, IReadOnlyCollection<string>? ours = null)
{
    private readonly List<CommandSpec> declared = [];
    private readonly Dictionary<CommandSpec, ScriptOrigin> origins = [];
    private readonly HashSet<string> already =
        new(ours ?? [], StringComparer.OrdinalIgnoreCase);

    /// <summary>The names declared, in the order scripts asked for them.</summary>
    public IEnumerable<string> Names => declared.Select(command => command.Name);

    /// <summary>
    /// Records one command, checking now whatever can be checked now: the game only
    /// complains once a command is registered, and by then the script that asked for
    /// it is long finished.
    /// </summary>
    public void Declare(CommandSpec spec, ScriptOrigin origin)
    {
        Check(spec, origin, spec.Name);

        if (declared.Any(other => other.Name.Equals(spec.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ScriptError(origin, $"a script has already declared the command '{spec.Name}'");
        }

        if (!already.Contains(spec.Name) && api.ChatCommands.Get(spec.Name.ToLowerInvariant()) is not null)
        {
            throw new ScriptError(origin,
                $"'{spec.Name}' is already a command on this server.");
        }

        declared.Add(spec);
        origins[spec] = origin;
    }

    /// <summary>Everything about a command that can be judged without registering it.</summary>
    private static void Check(CommandSpec spec, ScriptOrigin origin, string path)
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            throw new ScriptError(origin, $"{path} has no name, so there would be nothing to type");
        }

        if (spec.Name.Any(char.IsWhiteSpace))
        {
            throw new ScriptError(origin, $"'{spec.Name}' has a space in it, and a command name is one word");
        }

        var children = spec.Subcommands ?? [];

        if (spec.Handler is null && children.Length == 0)
        {
            throw new ScriptError(origin,
                $"{path} has neither a handler nor any subcommands, so running it would do nothing");
        }

        foreach (var argument in spec.Args ?? [])
        {
            if (string.IsNullOrWhiteSpace(argument.Name))
            {
                throw new ScriptError(origin, $"{path} has an argument with no name");
            }

            if (argument.Values is { Length: > 0 } && argument.Type != ArgumentKind.Word)
            {
                throw new ScriptError(origin,
                    $"{path} argument '{argument.Name}' lists values, which only a 'word' accepts");
            }

            if (argument is { Optional: true, Type: ArgumentKind.Player })
            {
                throw new ScriptError(origin,
                    $"{path} argument '{argument.Name}' is a player and cannot be optional, "
                    + "because the game has no reader for one that may be missing");
            }
        }

        foreach (var child in children) Check(child, origin, $"{path} {child.Name}");
    }

    /// <summary>
    /// Registers every command this run declared. Called once, by the run whose
    /// handlers are meant to be live, and never by one whose results are discarded.
    /// </summary>
    public void Activate()
    {
        foreach (var spec in declared)
        {
            var origin = origins[spec];
            var command = api.ChatCommands.Create(spec.Name.ToLowerInvariant())
                .WithDescription(spec.Description)
                .RequiresPrivilege(spec.Privilege);

            if (spec.RequiresPlayer) command.RequiresPlayer();

            Build(command, spec, origin);

            try
            {
                command.Validate();
            }
            catch (Exception refused)
            {
                // The game checks a command only once it is whole, and says so with an
                // exception that names nothing a script author would recognise.
                throw new ScriptError(origin, $"the game refused the command '{spec.Name}': {refused.Message}");
            }
        }

        declared.Clear();
        origins.Clear();
    }

    /// <summary>Fills in one command's arguments, handler and the commands under it.</summary>
    private void Build(IChatCommand command, CommandSpec spec, ScriptOrigin origin)
    {
        var arguments = spec.Args ?? [];

        if (arguments.Length > 0)
        {
            command.WithArgs([.. arguments.Select(Parser)]);
        }

        if (spec.Handler is { } handler)
        {
            command.HandleWith(called => Run(handler, arguments, called, origin, spec.Name));
        }

        foreach (var child in spec.Subcommands ?? [])
        {
            var sub = command.BeginSubCommand(child.Name.ToLowerInvariant())
                .WithDescription(child.Description);

            if (child.RequiresPlayer) sub.RequiresPlayer();

            Build(sub, child, origin);
            sub.EndSubCommand();
        }
    }

    /// <summary>The game's reader for one kind of typed value.</summary>
    private ICommandArgumentParser Parser(CommandArgumentSpec spec)
    {
        var parsers = api.ChatCommands.Parsers;
        var name = spec.Name;

        return (spec.Type, spec.Optional) switch
        {
            (ArgumentKind.Word, _) when spec.Values is { Length: > 0 } values =>
                spec.Optional ? parsers.OptionalWordRange(name, values) : parsers.WordRange(name, values),
            (ArgumentKind.Word, false) => parsers.Word(name),
            (ArgumentKind.Word, true) => parsers.OptionalWord(name),
            (ArgumentKind.Int, false) => parsers.Int(name),
            (ArgumentKind.Int, true) => parsers.OptionalInt(name),
            (ArgumentKind.Number, false) => parsers.Double(name),
            (ArgumentKind.Number, true) => parsers.OptionalDouble(name),
            (ArgumentKind.Bool, false) => parsers.Bool(name),
            (ArgumentKind.Bool, true) => parsers.OptionalBool(name),
            (ArgumentKind.Text, false) => parsers.All(name),
            (ArgumentKind.Text, true) => parsers.OptionalAll(name),
            (ArgumentKind.Player, _) => parsers.OnlinePlayer(name),
            _ => throw new InvalidOperationException($"{spec.Type} has no reader here"),
        };
    }

    /// <summary>
    /// Calls one handler with what was typed, and turns what it hands back into the
    /// answer the game shows.
    /// </summary>
    /// <remarks>
    /// A handler that throws is reported to whoever ran the command and logged, and
    /// the command stays. This differs from an event handler, which is dropped after
    /// one failure: an event fires on its own and a broken one would keep failing
    /// unwatched, where a command is only run by someone who is standing there to
    /// read the answer and decide whether to try again.
    /// </remarks>
    private TextCommandResult Run(
        ScriptValue.Func handler,
        IReadOnlyList<CommandArgumentSpec> arguments,
        TextCommandCallingArgs called,
        ScriptOrigin origin,
        string name)
    {
        try
        {
            var payload = new CommandPayload(called.Caller, Typed(arguments, called));
            return Answer(handler.Call([PayloadWriter.Table(payload)]));
        }
        catch (Exception failure)
        {
            api.Logger.Error("[moontweaks] {0}: handler for '{1}' failed: {2}", origin, name, failure.Message);
            return TextCommandResult.Error($"the script handling this failed: {failure.Message}");
        }
    }

    /// <summary>What was typed, keyed by the names the command gave its arguments.</summary>
    /// <remarks>
    /// Walked against the readers the command was built with rather than against
    /// <c>ArgCount</c>, which counts the words they consumed rather than the readers
    /// themselves and answers minus one as soon as any of them takes an unbounded
    /// number — which the one reading the rest of the line always does.
    /// </remarks>
    private static ScriptValue Typed(
        IReadOnlyList<CommandArgumentSpec> arguments, TextCommandCallingArgs called)
    {
        var entries = new Dictionary<string, ScriptValue>();

        for (var index = 0; index < arguments.Count && index < called.Parsers.Count; index++)
        {
            // A player is handed over as the identifier every other binding takes,
            // rather than as an object nothing else here would accept.
            entries[arguments[index].Name] = called[index] switch
            {
                IPlayer player => new ScriptValue.Str(player.PlayerUID),
                var value => PayloadWriter.Value(value),
            };
        }

        return new ScriptValue.Map(entries);
    }

    /// <summary>
    /// What a handler hands back, as the game's own answer. A string is what the
    /// caller is shown; a table naming an <c>error</c> is shown as one; nothing at all
    /// is a command that did its work quietly.
    /// </summary>
    private static TextCommandResult Answer(ScriptValue returned) => returned switch
    {
        ScriptValue.Str message => TextCommandResult.Success(message.Value),
        ScriptValue.Map map when map.Entries.TryGetValue("error", out var problem) =>
            TextCommandResult.Error(problem is ScriptValue.Str said ? said.Value : "the command failed"),
        _ => TextCommandResult.Success(),
    };
}
