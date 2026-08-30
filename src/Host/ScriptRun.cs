using System;
using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Commands;
using MoonTweaks.Entities;
using MoonTweaks.Events;
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
public sealed record ScriptRun(
    IReadOnlyList<ScriptFile> Scripts, MutationLog Log, ScriptError? Failure, IScriptHost Host)
    : IDisposable
{
    /// <summary>Throws the interpreter away, for a run whose handlers are not wanted.</summary>
    public void Dispose() => Host.Dispose();

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
    /// </remarks>
    public static ScriptRun Execute(
        ICoreServerAPI server,
        IScriptHost host,
        string scriptsFolder,
        RecipeRegistry registry,
        ScriptEvents events,
        ScriptCommands commands,
        ScriptTimers timers)
    {
        var log = new MutationLog();
        var scripts = ScriptLibrary.Discover(scriptsFolder);
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

        host.Bind(DomainBinder.Bind(new GridDomain(log, server.World)));
        host.Bind(DomainBinder.Bind(new KnappingDomain(log, server.World, registry)));
        host.Bind(DomainBinder.Bind(new ClayFormingDomain(log, server.World, registry)));
        host.Bind(DomainBinder.Bind(new SmithingDomain(log, server.World, registry)));
        host.Bind(DomainBinder.Bind(new BarrelDomain(log, server.World, registry)));
        host.Bind(DomainBinder.Bind(new AlloyDomain(log, server.World, registry)));
        host.Bind(DomainBinder.Bind(new CookingDomain(log, server.World, registry)));
        host.Bind(DomainBinder.Bind(new ItemDomain(log, server.World)));
        host.Bind(DomainBinder.Bind(new BlockDomain(log, server.World)));
        host.Bind(DomainBinder.Bind(new LogDomain(server.Logger)));
        host.Bind(DomainBinder.Bind(new ServerDomain(server, timers)));
        host.Bind(DomainBinder.Bind(new EventDomain(events)));
        host.Bind(DomainBinder.Bind(new CommandDomain(commands)));
        host.Bind(DomainBinder.Bind(new ModDomain(server.ModLoader)));
        host.Bind(DomainBinder.Bind(new EntityDomain(creatures, stacks)));
        host.Bind(DomainBinder.Bind(new InventoryDomain(
            new InventoryAccess(server, players, creatures), stacks, server.World)));
        host.Bind(DomainBinder.Bind(new CalendarDomain(server.World)));
        host.Bind(DomainBinder.Bind(new PlayerDomain(players, stacks)));
        host.Bind(DomainBinder.Bind(
            new WorldDomain(new WorldAccess(server, players), stacks)));

        foreach (var script in scripts)
        {
            try
            {
                host.Run(script);
            }
            catch (ScriptError error)
            {
                return new ScriptRun(scripts, log, error, host);
            }
        }

        return new ScriptRun(scripts, log, null, host);
    }

    /// <summary>One line per change, for reporting a run that was not applied.</summary>
    public IEnumerable<string> Describe()
    {
        foreach (var mutation in Log.Pending) yield return $"{mutation.Origin}: {mutation.Describe()}";
    }
}
