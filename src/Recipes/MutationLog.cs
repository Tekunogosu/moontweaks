using System.Collections.Generic;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Recipes;

/// <summary>One recorded change, able to describe itself before it is applied.</summary>
public interface IMutation
{
    /// <summary>Script line that requested the change.</summary>
    ScriptOrigin Origin { get; }

    /// <summary>One-line summary for the change report.</summary>
    string Describe();

    /// <summary>Performs the change, returning how many recipes it affected.</summary>
    int Apply(ICoreServerAPI api);
}

/// <summary>
/// Changes requested by scripts, applied in one pass once every script has run.
/// A script that fails partway therefore contributes nothing rather than leaving
/// the registries half-edited.
/// </summary>
public sealed class MutationLog
{
    private readonly List<IMutation> pending = [];

    /// <summary>Changes recorded so far, in the order scripts requested them.</summary>
    public IReadOnlyList<IMutation> Pending => pending;

    /// <summary>Records a change without performing it.</summary>
    public void Record(IMutation mutation) => pending.Add(mutation);

    /// <summary>Applies every recorded change, reporting each one to the log.</summary>
    public int Apply(ICoreServerAPI api, ILogger logger)
    {
        var affected = 0;

        foreach (var mutation in pending)
        {
            var count = mutation.Apply(api);
            affected += count;
            logger.Notification("[moontweaks] {0}: {1} ({2} recipe(s))",
                mutation.Origin, mutation.Describe(), count);
        }

        return affected;
    }
}
