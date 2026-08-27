using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Recipes;
using MoonTweaks.Scripting;
using Vintagestory.API.Server;

namespace MoonTweaks.Host;

/// <summary>
/// What running every script asked for, and the failure that stopped it if one
/// did. Nothing has been applied: a run records changes, it does not perform them.
/// </summary>
public sealed record ScriptRun(IReadOnlyList<ScriptFile> Scripts, MutationLog Log, ScriptError? Failure)
{
    /// <summary>Whether every script ran to completion.</summary>
    public bool Succeeded => Failure is null;

    /// <summary>
    /// Runs every script in the folder against a fresh interpreter. A server's
    /// startup and the check command both go through here, so what a check reports
    /// is exactly what a start would do.
    /// </summary>
    public static ScriptRun Execute(ICoreServerAPI server, string scriptsFolder, RecipeRegistry registry)
    {
        var log = new MutationLog();
        var scripts = ScriptLibrary.Discover(scriptsFolder);

        using var host = new MoonSharpHost();
        host.Bind(DomainBinder.Bind(new GridDomain(log, server.World)));
        host.Bind(DomainBinder.Bind(new KnappingDomain(log, server.World, registry)));
        host.Bind(DomainBinder.Bind(new LogDomain(server.Logger)));

        foreach (var script in scripts)
        {
            try
            {
                host.Run(script);
            }
            catch (ScriptError error)
            {
                return new ScriptRun(scripts, log, error);
            }
        }

        return new ScriptRun(scripts, log, null);
    }

    /// <summary>One line per change, for reporting a run that was not applied.</summary>
    public IEnumerable<string> Describe()
    {
        foreach (var mutation in Log.Pending) yield return $"{mutation.Origin}: {mutation.Describe()}";
    }
}
