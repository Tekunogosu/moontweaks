using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Recipes;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Assets;

/// <summary>
/// Changes properties on every item or block a code matches. Recorded like any other
/// change and applied in the same pass, so a script that fails partway alters no
/// asset any more than it alters a recipe.
/// </summary>
public sealed class SetAssetProperties(
    ScriptOrigin origin,
    string kind,
    AssetPropertiesSpec spec,
    IReadOnlyList<CollectibleObject> matched,
    AssetStacks stacks) : IMutation
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Counts => kind;

    /// <inheritdoc/>
    public string Describe() => $"set {kind} properties on {AssetSearch.Named(spec)}";

    /// <inheritdoc/>
    /// <remarks>
    /// Two owners rather than one, because a block is two things at once: an item
    /// while it is carried, and a block once it is placed. The shared half is applied
    /// to everything matched, and the block half only to what a script wrote the block
    /// shape for.
    /// </remarks>
    public int Apply(ICoreServerAPI api)
    {
        foreach (var asset in matched)
        {
            CollectibleProperties.ApplyTo(asset, spec, stacks, Origin);

            if (spec is BlockPropertiesSpec blocks && asset is Block block)
            {
                BlockProperties.ApplyTo(block, blocks, stacks, Origin);
            }
        }

        return matched.Count;
    }
}
