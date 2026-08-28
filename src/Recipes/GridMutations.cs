using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace MoonTweaks.Recipes;

/// <summary>
/// Adds recipes that were built, expanded and resolved when the script ran, so
/// applying them cannot fail on anything the author could have got wrong.
/// </summary>
public sealed class AddGridRecipe(ScriptOrigin origin, string outputCode, IReadOnlyList<GridRecipe> resolved)
    : IMutation
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Describe() => $"add grid recipe for {outputCode}";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api)
    {
        foreach (var recipe in resolved) api.RegisterCraftingRecipe(recipe);
        return resolved.Count;
    }
}

/// <summary>Removes every grid recipe whose output code matches a pattern.</summary>
public sealed class RemoveGridRecipes(ScriptOrigin origin, RecipeSelector selector) : IMutation
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Describe() => $"remove grid recipes producing {selector.Described}";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api)
    {
        var doomed = api.World.GridRecipes
            .Where(recipe => selector.Matches(recipe.Output?.Code, recipe.Output?.ResolvedItemStack))
            .ToList();

        foreach (var recipe in doomed) api.World.GridRecipes.Remove(recipe);
        return doomed.Count;
    }
}
