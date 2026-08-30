using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>
/// Turns one recipe a script declared into every recipe the game will actually hold:
/// the wildcards it names are expanded into a recipe per matching variant, and each
/// of those is resolved against the registries.
/// </summary>
/// <remarks>
/// Sole owner of that sequence. Grid, voxel and barrel recipes each declared their
/// own copy of it, which is how the three came to disagree about what to say when a
/// recipe expanded into nothing. Alloys and cooking recipes do not come here at all:
/// neither expands, and both say so in their own factory.
/// </remarks>
public static class RecipeExpansion
{
    /// <summary>
    /// Every recipe one declaration turns into, so a recipe that reaches the log is
    /// one the game has already accepted. Expanding into none is refused rather than
    /// registered: a recipe that resolved to nothing is one no surface will ever
    /// offer, and it would otherwise be reported as a change that did something.
    /// </summary>
    public static IReadOnlyList<TRecipe> Resolve<TRecipe>(
        TRecipe built, IWorldAccessor world, string outputCode, ScriptOrigin origin)
        where TRecipe : RecipeBase
    {
        built.OnParsed(world);

        var resolved = built.GenerateRecipesForAllIngredientCombinations(world)
            .OfType<TRecipe>()
            .Where(variant => variant.Resolve(world, $"moontweaks {origin}"))
            .ToList();

        if (resolved.Count == 0)
        {
            throw new ScriptError(origin,
                $"no recipe resolved for {outputCode}; check that every code it names exists");
        }

        return resolved;
    }
}
