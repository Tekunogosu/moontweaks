using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>
/// Adds recipes that were built, expanded and resolved when the script ran, so
/// applying them cannot fail on anything the author could have got wrong.
/// </summary>
public sealed class AddKnappingRecipe(
    ScriptOrigin origin,
    string outputCode,
    IReadOnlyList<KnappingRecipe> resolved,
    RecipeRegistry registry) : IMutation
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Describe() => $"add knapping recipe for {outputCode}";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api)
    {
        foreach (var recipe in resolved) registry.Register(recipe);
        return resolved.Count;
    }
}

/// <summary>Removes every knapping recipe whose output code matches a pattern.</summary>
public sealed class RemoveKnappingRecipes(ScriptOrigin origin, string outputCode, RecipeRegistry registry)
    : IMutation
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Describe() => $"remove knapping recipes producing {outputCode}";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api)
    {
        var pattern = new AssetLocation(outputCode);
        var doomed = registry.Knapping
            .Where(recipe => recipe.Output?.Code is { } code && WildcardUtil.Match(pattern, code))
            .ToList();

        foreach (var recipe in doomed) registry.Knapping.Remove(recipe);
        return doomed.Count;
    }
}
