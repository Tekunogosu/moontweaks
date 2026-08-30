using System.Collections.Generic;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

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
    public int Apply(ICoreServerAPI api) =>
        api.World.GridRecipes.RemoveAll(recipe =>
            selector.Matches(new RecipeProduct(recipe.Output?.Code, recipe.Output?.ResolvedItemStack)));
}
