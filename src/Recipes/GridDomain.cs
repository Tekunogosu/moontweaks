using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>Shaped and shapeless crafting grid recipes.</summary>
[LuaModule("moontweaks.recipes.grid")]
public sealed class GridDomain(MutationLog log, IWorldAccessor world)
{
    private readonly GridRecipeFactory factory = new(world);

    /// <summary>
    /// Registers a new grid recipe. An ingredient whose code contains a wildcard
    /// expands into one recipe per matching variant, with the variant substituted
    /// into any <c>{name}</c> placeholder in the output code.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, GridRecipeSpec recipe)
    {
        var built = factory.Create(recipe, origin);
        built.OnParsed(world);

        var resolved = new List<GridRecipe>();
        foreach (var variant in built.GenerateRecipesForAllIngredientCombinations(world))
        {
            if (variant is GridRecipe grid && grid.Resolve(world, $"moontweaks {origin}")) resolved.Add(grid);
        }

        if (resolved.Count == 0)
        {
            throw new ScriptError(origin,
                $"no recipe resolved for {recipe.Output.Code}; check that every ingredient code exists");
        }

        log.Record(new AddGridRecipe(origin, recipe.Output.Code, resolved));
    }

    /// <summary>
    /// Removes every grid recipe producing the given output code. The code may
    /// contain a <c>*</c> wildcard, so <c>"game:axe-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="outputCode">Output code to match, such as <c>game:axe-flint</c>.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, string outputCode) =>
        log.Record(new RemoveGridRecipes(origin, outputCode));

    /// <summary>
    /// Counts the grid recipes currently registered. Reads the registry as it stood
    /// before this run's changes, which are applied only once every script has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => world.GridRecipes.Count;
}
