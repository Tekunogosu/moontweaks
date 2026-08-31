using System;
using System.Collections.Generic;
using MoonTweaks.Api;
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

    /// <summary>Performs the change, returning how many things it affected.</summary>
    int Apply(ICoreServerAPI api);

    /// <summary>
    /// What this change counts, singular. Most changes are to recipes; the ones that
    /// are not say so, rather than having the report call an item a recipe.
    /// </summary>
    string Counts => "recipe";
}

/// <summary>
/// A change a disabled recipe asked for. The recipe was built, expanded and resolved
/// like any other, so a mistake in one is reported on the run that declared it rather
/// than on the day it is switched back on; only the registration is withheld.
/// </summary>
/// <remarks>
/// The game honours <c>enabled</c> in its recipe loader, which reads it before it
/// parses anything and which scripted recipes do not pass through. Nothing reads it
/// again once a recipe is registered, so this is the only place the field can mean
/// what it says.
/// </remarks>
public sealed class DisabledRecipe(IMutation change) : IMutation
{
    /// <inheritdoc/>
    public ScriptOrigin Origin => change.Origin;

    /// <inheritdoc/>
    public string Counts => change.Counts;

    /// <inheritdoc/>
    public string Describe() => $"{change.Describe()} — disabled, so nothing is registered";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api) => 0;
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

    /// <summary>
    /// Records a change, unless the recipe asking for it is disabled, in which case
    /// the change is kept for the report and never applied. Sole owner of what
    /// <c>enabled = false</c> means, so a kind bound later cannot register a disabled
    /// recipe by forgetting to ask.
    /// </summary>
    public void Record(RecipeSpec spec, IMutation mutation) =>
        Record(spec.Enabled ? mutation : new DisabledRecipe(mutation));

    /// <summary>
    /// What one applied change turned out to affect, kept so a server can report
    /// afterwards what its scripts actually did.
    /// </summary>
    public sealed record Applied(ScriptOrigin Origin, string Description, int Affected, string Counts)
    {
        /// <inheritdoc/>
        public override string ToString() => $"{Origin}: {Description} ({Affected} {Counts}(s))";
    }

    private readonly List<Applied> applied = [];
    private readonly List<string> refused = [];

    /// <summary>Changes already applied, in the order they were performed.</summary>
    public IReadOnlyList<Applied> Changes => applied;

    /// <summary>
    /// Changes the game would not accept, in the order they were attempted, so a
    /// server can be told what its scripts asked for and did not get.
    /// </summary>
    public IReadOnlyList<string> Refused => refused;

    /// <summary>Applies every recorded change, reporting each one to the log.</summary>
    /// <remarks>
    /// One change refused costs that change and nothing else. A change is performed
    /// against a live registry long after the script that asked for it finished, and
    /// letting a refusal out of here abandons every change recorded after it — which
    /// is a server whose recipes are edited partway, with nothing in the log naming
    /// what was skipped.
    /// </remarks>
    public int Apply(ICoreServerAPI api, ILogger logger)
    {
        var affected = 0;
        applied.Clear();
        refused.Clear();

        foreach (var mutation in pending)
        {
            int count;

            try
            {
                count = mutation.Apply(api);
            }
            catch (Exception failure)
            {
                var problem = $"{mutation.Origin}: {mutation.Describe()} could not be applied "
                    + $"({failure.GetType().Name}): {failure.Message}";
                refused.Add(problem);
                logger.Error("[moontweaks] {0}", problem);
                continue;
            }

            affected += count;

            var change = new Applied(mutation.Origin, mutation.Describe(), count, mutation.Counts);
            applied.Add(change);
            logger.Notification("[moontweaks] {0}", change);
        }

        return affected;
    }
}
