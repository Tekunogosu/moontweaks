using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>
/// The recipe lists Vintage Story keeps outside the world. Grid recipes hang off
/// the world itself, but every other kind lives on a mod system, so this is the
/// sole owner of reaching it and no domain looks it up for itself.
/// </summary>
public sealed class RecipeRegistry
{
    private readonly RecipeRegistrySystem system;

    /// <summary>Resolves the registry once, failing loudly when the survival mod is absent.</summary>
    public RecipeRegistry(ICoreAPI api) =>
        system = api.ModLoader.GetModSystem<RecipeRegistrySystem>()
            ?? throw new InvalidOperationException(
                "the survival mod's recipe registry is not loaded, so its recipes cannot be reached");

    /// <summary>Every knapping recipe currently registered.</summary>
    public List<KnappingRecipe> Knapping => system.KnappingRecipes;

    /// <summary>
    /// Adds a knapping recipe through the game's own entry point, so whatever
    /// bookkeeping it performs still happens, then gives it an identifier nothing
    /// else holds. Removal has no counterpart there and edits
    /// <see cref="Knapping"/> directly.
    /// </summary>
    /// <remarks>
    /// The game numbers a new recipe by how many the list already holds, which
    /// collides with a surviving recipe whenever one was removed first. A knapping
    /// surface resolves the recipe a player picked by that identifier, so a
    /// duplicate silently hands them another recipe's output.
    /// </remarks>
    public void Register(KnappingRecipe recipe)
    {
        system.RegisterKnappingRecipe(recipe);
        recipe.RecipeId = NextIdAfter(Knapping.Select(entry => entry.RecipeId));
    }

    /// <summary>An identifier past every one already taken.</summary>
    public static int NextIdAfter(IEnumerable<int> taken) => taken.DefaultIfEmpty(0).Max() + 1;
}
