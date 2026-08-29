using System;
using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Events;
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

    /// <summary>Whether every script ran to completion.</summary>
    public bool Succeeded => Failure is null;

    /// <summary>
    /// Runs every script in the folder against a fresh interpreter. A server's
    /// startup and the check command both go through here, so what a check reports
    /// is exactly what a start would do.
    /// </summary>
    /// <remarks>
    /// The host is returned rather than disposed here. A script may leave a function
    /// behind for an event to call, and that function is only callable while the
    /// interpreter that made it lives, so whoever wants those callbacks keeps the
    /// host and whoever does not disposes it.
    /// </remarks>
    public static ScriptRun Execute(
        ICoreServerAPI server, string scriptsFolder, RecipeRegistry registry, ScriptEvents events)
    {
        var log = new MutationLog();
        var scripts = ScriptLibrary.Discover(scriptsFolder);

        var host = new MoonSharpHost();
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
        host.Bind(DomainBinder.Bind(new EventDomain(events)));
        host.Bind(DomainBinder.Bind(new PlayerDomain(new PlayerAccess(server))));
        host.Bind(DomainBinder.Bind(new WorldDomain(new WorldAccess(server.World))));

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
