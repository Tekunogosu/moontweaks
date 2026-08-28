using System.Collections.Generic;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Recipes;

/// <summary>
/// Adds recipes that were built, expanded and resolved when the script ran, so
/// applying them cannot fail on anything the author could have got wrong.
/// </summary>
public sealed class AddRecipes<TRecipe>(
    ScriptOrigin origin,
    string kind,
    string outputCode,
    IReadOnlyList<TRecipe> resolved,
    RecipeRegistry registry) : IMutation
    where TRecipe : RecipeBase
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Describe() => $"add {kind} recipe for {outputCode}";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api)
    {
        foreach (var recipe in resolved) registry.Register(recipe);
        return resolved.Count;
    }
}

/// <summary>Removes every recipe of one kind whose output code matches a pattern.</summary>
public sealed class RemoveRecipes<TRecipe>(
    ScriptOrigin origin, string kind, string outputCode, RecipeRegistry registry) : IMutation
    where TRecipe : RecipeBase
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Describe() => $"remove {kind} recipes producing {outputCode}";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api) => registry.Remove<TRecipe>(outputCode);
}
