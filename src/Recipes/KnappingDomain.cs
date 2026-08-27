using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>Recipes chipped from a stone laid on a knapping surface.</summary>
[LuaModule("moontweaks.recipes.knapping")]
public sealed class KnappingDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
{
    private readonly KnappingRecipeFactory factory = new(world);

    /// <summary>
    /// Registers a new knapping recipe. An ingredient whose code contains a wildcard
    /// expands into one recipe per matching variant, with the variant substituted
    /// into any <c>{name}</c> placeholder in the output code.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, KnappingRecipeSpec recipe)
    {
        var built = factory.Create(recipe, origin);
        built.OnParsed(world);

        var resolved = new List<KnappingRecipe>();
        foreach (var variant in built.GenerateRecipesForAllIngredientCombinations(world))
        {
            if (variant is KnappingRecipe knapping && knapping.Resolve(world, $"moontweaks {origin}"))
            {
                resolved.Add(knapping);
            }
        }

        if (resolved.Count == 0)
        {
            throw new ScriptError(origin,
                $"no recipe resolved for {recipe.Output.Code}; check that every code exists");
        }

        log.Record(new AddKnappingRecipe(origin, recipe.Output.Code, resolved, registry));
    }

    /// <summary>
    /// Removes every knapping recipe producing the given output code. The code may
    /// contain a <c>*</c> wildcard, so <c>"game:knifeblade-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="outputCode">Output code to match, such as <c>game:knifeblade-flint</c>.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, string outputCode) =>
        log.Record(new RemoveKnappingRecipes(origin, outputCode, registry));

    /// <summary>
    /// Counts the knapping recipes currently registered. Reads the registry as it
    /// stood before this run's changes, which are applied only once every script
    /// has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => registry.Knapping.Count;
}
