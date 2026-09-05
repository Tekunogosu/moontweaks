using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Commands;
using MoonTweaks.Events;
using MoonTweaks.Recipes;
using MoonTweaks.Reference;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Host;

/// <summary>
/// Runs the server's Lua scripts once the vanilla recipe loader has finished, then
/// applies everything they asked for in a single pass.
/// </summary>
public class MoonTweaksSystem : ModSystem
{
    /// <summary>What this server's scripts applied at startup, for the list command.</summary>
    private MutationLog? applied;

    /// <summary>
    /// The interpreter the startup run left behind, kept alive for as long as the
    /// handlers inside it may be called. Disposed with the mod rather than with the
    /// run that made it.
    /// </summary>
    private IScriptHost? host;

    /// <summary>What this server's scripts are listening for.</summary>
    private ScriptEvents? events;

    /// <summary>
    /// Every plugin this server holds, found once at startup. Empty until then, and
    /// empty for good when one of them was refused, since a run with a refused plugin
    /// never happens.
    /// </summary>
    private IReadOnlyList<Plugin> plugins = [];

    /// <summary>What the last successful run bound each plugin at, for the list command.</summary>
    private IReadOnlyList<BoundPlugin> boundPlugins = [];

    /// <summary>
    /// This server's settings, read once by whichever of the startup phases reaches
    /// them first. <c>AssetsLoaded</c> runs before <c>StartServerSide</c>, but both
    /// want them and neither is a safe place to assume the other has already been.
    /// </summary>
    private MoonTweaksConfig? settings;

    /// <summary>
    /// Commands the last successful run registered. A check re-declares them, and
    /// needs to know they are this mod's own rather than a clash with something else.
    /// Only the names the server actually took are on it: one it refused belongs to
    /// whoever holds it, and a check should report that as the clash it is.
    /// </summary>
    private IReadOnlyList<string> commandNames = [];

    /// <summary>The vanilla recipe loader runs at 1.0; scripts must observe its output.</summary>
    public override double ExecuteOrder() => 1.1;

    /// <summary>Recipes are server-authoritative, so only the server runs scripts.</summary>
    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    /// <summary>
    /// Registers the command surface. Its privilege comes from the settings file, so
    /// a server that has not chosen one keeps commands with the administrators.
    /// </summary>
    /// <remarks>
    /// Attempted rather than performed, because the game refuses a command whose name
    /// is already taken and a refusal here is not this mod's alone to pay for: what
    /// escapes a mod's start is caught by the game, which then drops the whole mod
    /// system — so a server that happens to have another <c>/moontweaks</c> would
    /// lose the scripts that already ran as well as the commands that describe them.
    /// </remarks>
    public override void StartServerSide(ICoreServerAPI api)
    {
        Attempt(api.Logger, "adding the /moontweaks command", () =>
        {
            var folder = ScriptLibrary.PathFor();
            var config = Settings(folder, api.Logger);

            api.ChatCommands.Create("moontweaks")
                .WithDescription("MoonTweaks scripting tools")
                .RequiresPrivilege(config.CommandPrivilege)
                .BeginSubCommand("list")
                    .WithDescription("List the changes this server's scripts applied at startup")
                    .HandleWith(_ => Answered(api.Logger, "list", () =>
                    {
                        if (applied is not { Changes.Count: > 0 } log)
                        {
                            return TextCommandResult.Success("no scripts have changed anything on this server");
                        }

                        return TextCommandResult.Success(
                            $"{log.Changes.Count} change(s) applied at startup:\n"
                            + string.Join("\n", log.Changes)
                            + Refusals(log));
                    }))
                .EndSubCommand()
                .BeginSubCommand("check")
                    .WithDescription("Re-run every script and report what it would change, changing nothing")
                    .HandleWith(_ => Answered(api.Logger, "check", () =>
                    {
                        // A check is a dry run, so its handlers are never called and its
                        // interpreter goes with it rather than joining the live one.
                        using var run = ScriptRun.Execute(
                            api, ScriptEngine.Create(ScriptEngine.DEFAULT),
                            ScriptLibrary.ScriptsPathFor(), new RecipeRegistry(api),
                            new ScriptEvents(api), new ScriptCommands(api, commandNames),
                            new ScriptTimers(api), config.UndoHistory, plugins);

                        if (run.Failure is { } failure) return TextCommandResult.Error(failure.Message);
                        if (run.Scripts.Count == 0) return TextCommandResult.Success("no scripts to check");

                        var lines = string.Join("\n", run.Describe());
                        return TextCommandResult.Success(
                            $"{run.Scripts.Count} script(s) ran, {run.Log.Pending.Count} change(s) would be made"
                            + (lines.Length == 0 ? "" : "\n" + lines)
                            + "\nnothing was applied; restart the server for changes to take effect");
                    }))
                .EndSubCommand()
                .BeginSubCommand("plugins")
                    .WithDescription("List the plugins whose bindings this server's scripts reach")
                    .HandleWith(_ => Answered(api.Logger, "plugins", () =>
                        TextCommandResult.Success(boundPlugins.Count == 0
                            ? "no plugins are bound on this server"
                            : string.Join("\n", boundPlugins.Select(Describe)))))
                .EndSubCommand()
                .BeginSubCommand("export")
                    .WithDescription("Rewrite the asset codes an editor suggests, from the live registries")
                    .HandleWith(_ => Answered(api.Logger, "export", () =>
                    {
                        // Read first and report from that, rather than from what the
                        // write returned: a forced write always happens, so its result
                        // would only be the same sets behind a null the type still carries.
                        var sets = AssetCodeLibrary.SetsOf(api.World);
                        AssetCodeLibrary.Install(folder, sets, force: true);
                        return TextCommandResult.Success(
                            $"wrote {EditorSupport.LIBRARY_FOLDER}/{AssetCodeLibrary.FILE_NAME} "
                            + $"with {AssetCodeLibrary.Describe(sets)}");
                    }))
                .EndSubCommand();
        });
    }

    /// <inheritdoc/>
    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api is not ICoreServerAPI server) return;

        string folder;
        string scriptsFolder;

        try
        {
            folder = ScriptLibrary.PathFor();
            scriptsFolder = ScriptLibrary.ScriptsPathFor();
        }
        catch (Exception unreachable)
        {
            // The scripts, the settings and the editor support all live in there, so
            // a folder that cannot be opened leaves nothing further to attempt.
            server.Logger.Error("[moontweaks] the {0} folder could not be opened, so no scripts ran: {1}",
                ScriptLibrary.FOLDER_NAME, unreachable.Message);
            return;
        }

        var config = Settings(folder, server.Logger);

        // Bound before anything is written, so the editor support describes exactly
        // the plugins the run holds and a refused one never describes itself. A
        // refused plugin is an operator's problem rather than an author's, and nothing
        // runs until it is resolved: a script written against that plugin has no way
        // to tell a refusal from a typo.
        var registry = new RecipeRegistry(server);
        events = new ScriptEvents(server);
        var commands = new ScriptCommands(server);
        var timers = new ScriptTimers(server);
        var prepared = Discovered(server)
            ? Prepared(server, config, scriptsFolder, registry, events, commands, timers)
            : null;

        // What an author writes scripts with rather than what runs them, so a folder
        // that will not take it costs a server its editor support and nothing else.
        Attempt(server.Logger, "writing the editor support", () =>
        {
            var bound = prepared?.Plugins.Select(plugin => plugin.Plugin) ?? [];
            EditorSupport.Install(folder, Libraries(server.Logger, bound), server.Logger);

            // The registries are populated by now, so the codes an author may write are
            // exactly the ones an editor can offer.
            if (AssetCodeLibrary.Install(folder, server.World) is { } sets)
            {
                server.Logger.Notification("[moontweaks] wrote {0}/{1} with {2}",
                    EditorSupport.LIBRARY_FOLDER, AssetCodeLibrary.FILE_NAME, AssetCodeLibrary.Describe(sets));
            }

            foreach (var misplaced in ScriptLibrary.Misplaced(folder))
            {
                server.Logger.Warning(
                    "[moontweaks] {0} sits beside the {1} folder rather than in it, so it does not run",
                    misplaced, ScriptLibrary.SCRIPTS_FOLDER);
            }
        });

        if (prepared is null)
        {
            server.Logger.Error("[moontweaks] no scripts ran");
            return;
        }

        var run = prepared.Run();

        if (run.Scripts.Count == 0)
        {
            run.Dispose();
            server.Logger.Notification("[moontweaks] no scripts in {0}", scriptsFolder);
            return;
        }

        if (run.Failure is { } failure)
        {
            run.Dispose();
            // Nothing has been applied yet, so abandoning the run leaves the
            // registries exactly as the vanilla loader left them.
            server.Logger.Error("[moontweaks] {0}", failure.Message);
            server.Logger.Error("[moontweaks] no changes were applied");
            return;
        }

        var affected = run.Log.Apply(server, server.Logger);
        applied = run.Log;
        boundPlugins = run.Plugins;
        // Kept rather than disposed: a script may have left a handler behind, and it
        // is only callable while the interpreter that made it is alive.
        host = run.Host;
        RecipeBase.CollectiblePreSearchResultsCache.Clear();

        // The run succeeded and its handlers are the live ones, so this is where the
        // game is actually subscribed to and the timers are started. A check never
        // reaches here, so it does neither twice.
        Attempt(server.Logger, "subscribing the event handlers", () =>
            Report(server.Logger, events.Activate()));
        Attempt(server.Logger, "starting the timers", () =>
            Report(server.Logger, timers.Activate()));
        Register(server, commands);

        // Nothing downstream reports this: a surface takes the first recipe whose
        // identifier matches, and saves that identifier with the block, so a
        // collision is wrong quietly and stays wrong across restarts.
        if (registry.DuplicateIds() is { Count: > 0 } duplicates)
        {
            server.Logger.Error(
                "[moontweaks] recipe identifier(s) {0} are held by more than one recipe; "
                + "a surface can resolve a player's choice to the wrong one",
                string.Join(", ", duplicates));
        }

        // Grid recipes hang off the world rather than the registry, so they are the
        // one kind counted here rather than asked for.
        var held = registry.Tally().Select(kind => $"{kind.Value} {kind.Key}")
            .Prepend($"{server.World.GridRecipes.Count} grid");

        server.Logger.Notification(
            "[moontweaks] {0} script(s) on {1}, {2} change(s), {3} affected; {4} recipes now",
            run.Scripts.Count, ScriptEngine.DEFAULT, run.Log.Changes.Count, affected, string.Join(", ", held));

        if (run.Log.Refused.Count > 0)
        {
            server.Logger.Error("[moontweaks] {0} change(s) the game would not accept were skipped",
                run.Log.Refused.Count);
        }

        if (run.Plugins.Count > 0)
        {
            server.Logger.Notification("[moontweaks] {0} plugin(s) bound: {1}",
                run.Plugins.Count, string.Join("; ", run.Plugins.Select(Describe)));
        }

        if (events.Count > 0)
        {
            server.Logger.Notification("[moontweaks] {0} event handler(s) listening", events.Count);
        }

        if (timers.Count > 0)
        {
            server.Logger.Notification("[moontweaks] {0} timer(s) running", timers.Count);
        }
    }

    /// <summary>
    /// Finds this server's plugins, keeping them for every run. Answers whether all
    /// of them could be taken; a refusal is reported here and acted on by the caller.
    /// </summary>
    private bool Discovered(ICoreServerAPI server)
    {
        try
        {
            plugins = Plugins.Discover(server.ModLoader);
            return true;
        }
        catch (PluginError refused)
        {
            plugins = [];
            server.Logger.Error("[moontweaks] {0}", refused.Message);
            return false;
        }
    }

    /// <summary>
    /// One library per set of bindings: MoonTweaks's own, then each plugin's. All
    /// rendered the same way from the assemblies the server actually loaded, so
    /// what an editor completes is what the interpreter binds.
    /// </summary>
    /// <param name="logger">Where a plugin shipping no documentation is reported.</param>
    /// <param name="bound">The plugins actually bound, which are the ones described.</param>
    private IReadOnlyList<Library> Libraries(ILogger logger, IEnumerable<Plugin> bound)
    {
        var libraries = new List<Library>
        {
            Library.Of("moontweaks.lua", "MoonTweaks", Mod.Info?.Version ?? "", typeof(MoonTweaksSystem).Assembly),
        };

        foreach (var plugin in bound)
        {
            var library = Library.Of(
                plugin.LibraryFile, plugin.Mod.Info?.Name ?? plugin.Name, plugin.Mod.Info?.Version ?? "",
                plugin.Assembly);

            if (!library.Documented)
            {
                logger.Warning(
                    "[moontweaks] {0} ships no XML documentation beside its assembly, so {1} carries no descriptions",
                    plugin.Describe(), library.FileName);
            }

            libraries.Add(library);
        }

        return libraries;
    }

    /// <summary>One plugin as the log and the list command name it.</summary>
    private static string Describe(BoundPlugin bound) =>
        $"{bound.Plugin.Describe()} at {string.Join(", ", bound.Paths)}";

    /// <summary>
    /// Binds every module and reads the folder, or reports why the scripts could not
    /// be run at all and answers nothing.
    /// </summary>
    /// <remarks>
    /// A script that fails is the run's own answer and is reported by the caller.
    /// This is for what happens before the scripts: reading the folder, building the
    /// interpreter, and taking the plugins.
    /// </remarks>
    private ScriptRun.Prepared? Prepared(
        ICoreServerAPI server,
        MoonTweaksConfig config,
        string scriptsFolder,
        RecipeRegistry registry,
        ScriptEvents events,
        ScriptCommands commands,
        ScriptTimers timers)
    {
        try
        {
            return ScriptRun.Prepare(
                server, ScriptEngine.Create(ScriptEngine.DEFAULT), scriptsFolder,
                registry, events, commands, timers, config.UndoHistory, plugins);
        }
        catch (PluginError refused)
        {
            server.Logger.Error("[moontweaks] {0}", refused.Message);
            return null;
        }
        catch (Exception unreadable)
        {
            server.Logger.Error("[moontweaks] the scripts in {0} could not be run: {1}",
                scriptsFolder, unreadable.Message);
            return null;
        }
    }

    /// <summary>
    /// Puts this run's commands on the server, once every other mod has taken its own.
    /// </summary>
    /// <remarks>
    /// Deferred to the run phase the game enters after it has started every mod,
    /// which decides who pays for a clash. Registered any earlier, a name a
    /// content mod also wants would be ours first and theirs second, and the game
    /// refuses the second — inside that mod's own start, which the game answers by
    /// dropping the whole mod. Registered here, the clash lands in
    /// <see cref="ScriptCommands.Activate"/>, where it costs the one command and is
    /// reported against the script line that asked for it.
    ///
    /// That is also why nothing may escape: the game dispatches a run phase with no
    /// handler of its own around it, and what gets out of one stops the server
    /// starting rather than being logged and stepped over.
    /// </remarks>
    private void Register(ICoreServerAPI server, ScriptCommands commands) =>
        server.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, () =>
            Attempt(server.Logger, "registering the commands", () =>
            {
                var registered = commands.Activate();
                // Taken from what the server accepted rather than from what the run
                // asked for, so a later check reports a refused name as the clash it
                // is instead of mistaking it for one of this mod's own.
                commandNames = registered.Added;

                Report(server.Logger, registered.Refused);

                if (registered.Added.Count > 0)
                {
                    server.Logger.Notification("[moontweaks] command(s) added: {0}",
                        string.Join(", ", registered.Added.Select(name => $"/{name}")));
                }
            }));

    /// <summary>
    /// Carries out one of the things a successful run asked for, reporting a refusal
    /// rather than letting it out.
    /// </summary>
    /// <remarks>
    /// Each is attempted whatever the ones before it did. Only the game can refuse a
    /// subscription, a timer or a command, and it does so long after the script that
    /// asked for it finished and after every recipe has already been applied. Letting
    /// that out of here takes the mod down mid-startup with a stack trace, leaving a
    /// server whose recipes changed, whose timers never started, and with nothing in
    /// the log a script author could act on.
    /// </remarks>
    /// <param name="logger">Where a refusal is reported.</param>
    /// <param name="what">
    /// What was being done, as a phrase that completes "… could not be completed".
    /// </param>
    /// <param name="act">The work to attempt.</param>
    private static void Attempt(ILogger logger, string what, Action act)
    {
        try
        {
            act();
        }
        catch (Exception refused)
        {
            logger.Error("[moontweaks] {0} could not be completed: {1}", what, refused.Message);
        }
    }

    /// <summary>
    /// Answers a <c>/moontweaks</c> command, telling whoever ran it when it failed.
    /// </summary>
    /// <remarks>
    /// These reach the live registries and the filesystem, so any of them can fail on
    /// a server where a script or an operator has left something in a state the
    /// command cannot read. Somebody is standing there waiting for an answer, and an
    /// exception out of a command handler gives them silence and puts a stack trace
    /// somewhere only the server operator will ever look.
    /// </remarks>
    private static TextCommandResult Answered(
        ILogger logger, string name, Func<TextCommandResult> answer)
    {
        try
        {
            return answer();
        }
        catch (Exception failure)
        {
            logger.Error("[moontweaks] /moontweaks {0} failed: {1}", name, failure);
            return TextCommandResult.Error(
                $"/moontweaks {name} failed ({failure.GetType().Name}): {failure.Message}");
        }
    }

    /// <summary>Writes one line per thing the game would not take.</summary>
    private static void Report(ILogger logger, IReadOnlyList<string> refused)
    {
        foreach (var problem in refused) logger.Error("[moontweaks] {0}", problem);
    }

    /// <summary>
    /// What a report of applied changes says about the ones that were refused, so a
    /// player reading the list is not shown a shorter list than their scripts asked
    /// for with nothing to explain the difference.
    /// </summary>
    private static string Refusals(MutationLog log) =>
        log.Refused.Count == 0 ? "" : $"\n{log.Refused.Count} change(s) were refused:\n"
            + string.Join("\n", log.Refused);

    /// <summary>Reads the settings, or hands back the ones already read.</summary>
    private MoonTweaksConfig Settings(string folder, ILogger logger) =>
        settings ??= MoonTweaksConfig.Load(folder, logger);

    /// <inheritdoc/>
    /// <remarks>
    /// The interpreter is deliberately left alone. A mod is told to shut down before
    /// the server has finished shutting down, and the game goes on raising events
    /// afterwards — it saves the world after this returns, which is an event scripts
    /// listen for. Handlers are only callable while the interpreter that made them is
    /// alive, so disposing it here hands the game a handler it cannot call. The
    /// process is ending regardless, and what a run of scripts holds goes with it.
    /// </remarks>
    public override void Dispose()
    {
        host = null;
        base.Dispose();
    }
}
