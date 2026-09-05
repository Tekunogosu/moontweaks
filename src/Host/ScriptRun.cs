using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Commands;
using MoonTweaks.Entities;
using MoonTweaks.Events;
using MoonTweaks.GameSystems;
using MoonTweaks.Inventories;
using MoonTweaks.Players;
using MoonTweaks.World;
using MoonTweaks.Recipes;
using MoonTweaks.Scripting;
using Vintagestory.API.Server;

namespace MoonTweaks.Host;

/// <summary>
/// What running every script asked for, and the failure that stopped it if one
/// did. Nothing has been applied: a run records changes, it does not perform them.
/// </summary>
/// <param name="Scripts">Every script the folder held, in the order they ran.</param>
/// <param name="Log">What the scripts asked to change.</param>
/// <param name="Failure">The script failure that stopped the run, or null.</param>
/// <param name="Host">The interpreter the scripts ran on.</param>
/// <param name="Plugins">Every plugin bound, with the paths scripts reached it at.</param>
public sealed record ScriptRun(
    IReadOnlyList<ScriptFile> Scripts,
    MutationLog Log,
    ScriptError? Failure,
    IScriptHost Host,
    IReadOnlyList<BoundPlugin> Plugins)
    : IDisposable
{
    /// <summary>Throws the interpreter away, for a run whose handlers are not wanted.</summary>
    public void Dispose() => Host.Dispose();

    /// <summary>
    /// Every binding hung on a fresh interpreter, with the scripts not yet run. What
    /// sits between binding and running is a server's editor support: written from
    /// the plugins actually bound, so a plugin refused here never describes itself
    /// to an editor.
    /// </summary>
    /// <param name="Scripts">Every script the folder held, in the order they will run.</param>
    /// <param name="Log">Where the scripts will record what they ask to change.</param>
    /// <param name="Host">The interpreter, with every module bound.</param>
    /// <param name="Plugins">Every plugin bound, with the paths scripts reach it at.</param>
    public sealed record Prepared(
        IReadOnlyList<ScriptFile> Scripts,
        MutationLog Log,
        IScriptHost Host,
        IReadOnlyList<BoundPlugin> Plugins)
    {
        /// <summary>Runs every script in order, stopping at the first that fails.</summary>
        public ScriptRun Run()
        {
            foreach (var script in Scripts)
            {
                try
                {
                    Host.Run(script);
                }
                catch (ScriptError error)
                {
                    return new ScriptRun(Scripts, Log, error, Host, Plugins);
                }
                catch (Exception failure)
                {
                    // Everything a script can get wrong reaches here as a ScriptError, so
                    // anything else is this mod's own mistake rather than an author's. It
                    // is still reported as the run failing on that script: the alternative
                    // is a stack trace out of a startup phase, which says nothing about
                    // which of a server's scripts stopped the rest from running.
                    return new ScriptRun(Scripts, Log,
                        new ScriptError(new ScriptOrigin(script.Name, 0),
                            $"failed unexpectedly ({failure.GetType().Name}): {failure.Message}"),
                        Host, Plugins);
                }
            }

            return new ScriptRun(Scripts, Log, null, Host, Plugins);
        }
    }

    /// <summary>
    /// Runs every script in the folder against a fresh interpreter. A server's
    /// startup and the check command both go through here, so what a check reports
    /// is exactly what a start would do.
    /// </summary>
    /// <remarks>
    /// The interpreter is handed in rather than built here, so which engine a run
    /// uses is the caller's to decide and this stays the one place the bindings are
    /// hung on it. The host is returned rather than disposed here. A script may leave a function
    /// behind for an event to call, and that function is only callable while the
    /// interpreter that made it lives, so whoever wants those callbacks keeps the
    /// host and whoever does not disposes it.
    ///
    /// A plugin that cannot be bound is a <see cref="PluginError"/> out of here
    /// before any script runs, rather than a failure on the first script to reach
    /// it: the plugin is wrong for every script, and the operator rather than an
    /// author is who can act on it.
    /// </remarks>
    public static ScriptRun Execute(
        ICoreServerAPI server,
        IScriptHost host,
        string scriptsFolder,
        RecipeRegistry registry,
        ScriptEvents events,
        ScriptCommands commands,
        ScriptTimers timers,
        int undoHistory,
        IReadOnlyList<Plugin> plugins) =>
        Prepare(server, host, scriptsFolder, registry, events, commands, timers, undoHistory, plugins).Run();

    /// <summary>
    /// Binds everything a run needs and reads the folder, running nothing yet. The
    /// arguments are those of <see cref="Execute"/>, which is this followed by
    /// <see cref="Prepared.Run"/>.
    /// </summary>
    public static Prepared Prepare(
        ICoreServerAPI server,
        IScriptHost host,
        string scriptsFolder,
        RecipeRegistry registry,
        ScriptEvents events,
        ScriptCommands commands,
        ScriptTimers timers,
        int undoHistory,
        IReadOnlyList<Plugin> plugins)
    {
        var log = new MutationLog();
        var scripts = ScriptLibrary.Discover(scriptsFolder);
        // Every path bound so far, so a plugin asking for one already taken is
        // refused rather than quietly stacked on top of it.
        var taken = new HashSet<string>(StringComparer.Ordinal);

        void Bind(ModuleBinding module)
        {
            taken.Add(module.Path);
            host.Bind(module);
        }

        // One lookup shared by both domains that reach a player, so an identifier
        // naming nobody is reported the same way whichever of them was asked.
        var players = new PlayerAccess(server);
        // Shared for the same reason: an entity identifier naming nothing loaded
        // should read the same whether a script asked the entity domain or reached an
        // inventory through one.
        var creatures = new EntityAccess(server);
        // One translation from what a script names to what the game holds, so a code
        // that resolves for a recipe resolves the same way for a stack handed over.
        var stacks = new AssetStacks(server.World);

        Bind(DomainBinder.Bind(new RecipeKindDomain(log, server.World)));
        Bind(DomainBinder.Bind(new GridDomain(log, server.World)));
        Bind(DomainBinder.Bind(new KnappingDomain(log, server.World, registry)));
        Bind(DomainBinder.Bind(new ClayFormingDomain(log, server.World, registry)));
        Bind(DomainBinder.Bind(new SmithingDomain(log, server.World, registry)));
        Bind(DomainBinder.Bind(new BarrelDomain(log, server.World, registry)));
        Bind(DomainBinder.Bind(new AlloyDomain(log, server.World, registry)));
        Bind(DomainBinder.Bind(new CookingDomain(log, server.World, registry)));
        Bind(DomainBinder.Bind(new ItemDomain(log, server.World)));
        Bind(DomainBinder.Bind(new BlockDomain(log, server.World)));
        Bind(DomainBinder.Bind(new TagDomain(server.World)));
        Bind(DomainBinder.Bind(new LogDomain(server.Logger)));
        Bind(DomainBinder.Bind(new ServerDomain(server, timers)));
        Bind(DomainBinder.Bind(events));
        Bind(DomainBinder.Bind(new CommandDomain(commands)));
        Bind(DomainBinder.Bind(new ModDomain(server.ModLoader)));
        Bind(DomainBinder.Bind(new EntityDomain(creatures, stacks)));
        Bind(DomainBinder.Bind(new InventoryDomain(
            new InventoryAccess(server, players, creatures), stacks, server.World)));
        Bind(DomainBinder.Bind(new CalendarDomain(server.World)));
        Bind(DomainBinder.Bind(new PlayerDomain(players, stacks)));
        Bind(DomainBinder.Bind(new GroupDomain(server, players, new GroupAccess(server, players))));
        Bind(DomainBinder.Bind(
            new WorldDomain(new WorldAccess(server, players, undoHistory), stacks)));
        Bind(DomainBinder.Bind(new ClaimDomain(new ClaimAccess(server, players))));
        // Everything below reaches inside another mod rather than the game's own API.
        // One lookup shared by all three, so a server missing one of those mods is
        // told the same thing whichever domain was asked.
        var mods = new GameSystems.GameSystems(server);
        Bind(DomainBinder.Bind(new WeatherDomain(mods)));
        Bind(DomainBinder.Bind(new StabilityDomain(mods)));
        Bind(DomainBinder.Bind(new ReinforceDomain(mods, players)));

        // After every binding of MoonTweaks's own, so a plugin's path is checked
        // against all of them and none can shadow one.
        var bound = plugins.Select(plugin => MoonTweaks.Host.Plugins.Bind(plugin, taken, Bind)).ToList();

        return new Prepared(scripts, log, host, bound);
    }

    /// <summary>One line per change, for reporting a run that was not applied.</summary>
    public IEnumerable<string> Describe()
    {
        foreach (var mutation in Log.Pending) yield return $"{mutation.Origin}: {mutation.Describe()}";
    }
}
