using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>
/// Builds the recipe kinds the game shapes voxel by voxel. Knapping, clay forming
/// and smithing differ only in how many layers they work and what a filled cell
/// means, so one factory serves all three.
/// </summary>
public sealed class VoxelRecipeFactory(IWorldAccessor world)
{
    /// <summary>Side of the square grid every one of these kinds is worked on.</summary>
    private const int SurfaceSize = 16;

    private readonly RecipeAssets assets = new(world);

    /// <summary>
    /// Translates one spec, expands whatever wildcards it names and resolves the
    /// result, so a recipe that reaches the log is one the game has already accepted.
    /// </summary>
    public IReadOnlyList<TRecipe> Build<TRecipe>(VoxelRecipeSpec spec, ScriptOrigin origin)
        where TRecipe : LayeredVoxelRecipe, new()
    {
        var built = Create<TRecipe>(spec, origin);
        built.OnParsed(world);

        var resolved = built.GenerateRecipesForAllIngredientCombinations(world)
            .OfType<TRecipe>()
            .Where(variant => variant.Resolve(world, $"moontweaks {origin}"))
            .ToList();

        if (resolved.Count == 0)
        {
            throw new ScriptError(origin,
                $"no recipe resolved for {spec.OutputCode}; check that every code exists");
        }

        return resolved;
    }

    /// <summary>Translates one spec, rejecting patterns the surface cannot hold.</summary>
    private TRecipe Create<TRecipe>(VoxelRecipeSpec spec, ScriptOrigin origin)
        where TRecipe : LayeredVoxelRecipe, new()
    {
        // Constructed first because the layer limit is the recipe's own answer, which
        // is what makes one factory able to check every kind against its own surface.
        var recipe = new TRecipe
        {
            Pattern = [],
            // Set through the array rather than the Ingredient property, which writes
            // into an array this recipe does not have yet.
            Ingredients = [assets.Ingredient(spec.Ingredient, origin, "ingredient")],
            Output = assets.Stack(spec.Output, origin, "output"),
        };

        recipe.Pattern = Layers(spec.Pattern, recipe.QuantityLayers, recipe.RecipeCategoryCode, origin);
        return assets.Recipe(recipe, spec, origin);
    }

    /// <summary>
    /// Checks the layers describe a surface this kind can work. The game requires
    /// every layer to be the same size as the first, so a ragged one is refused here
    /// with the layer named rather than thrown from inside the game's own parser.
    /// </summary>
    private static string[][] Layers(string[][] pattern, int allowed, string kind, ScriptOrigin origin)
    {
        if (pattern.Length == 0) throw new ScriptError(origin, "pattern has no layers");
        if (pattern.Length > allowed)
        {
            throw new ScriptError(origin,
                $"pattern has {pattern.Length} layers but {kind} works at most {allowed}");
        }

        var depth = pattern[0].Length;
        if (depth == 0) throw new ScriptError(origin, "pattern layer 1 has no rows");
        if (depth > SurfaceSize)
        {
            throw new ScriptError(origin,
                $"pattern is {depth} rows deep but the surface is {SurfaceSize}");
        }

        var width = pattern[0][0].Length;
        if (width == 0) throw new ScriptError(origin, "pattern row 1 is empty");
        if (width > SurfaceSize)
        {
            throw new ScriptError(origin,
                $"pattern row 1 is {width} cells wide but the surface is {SurfaceSize}");
        }

        foreach (var (layer, index) in pattern.Select((layer, index) => (layer, index)))
        {
            var at = pattern.Length == 1 ? "pattern" : $"pattern layer {index + 1}";

            if (layer.Length != depth)
            {
                throw new ScriptError(origin,
                    $"{at} is {layer.Length} rows deep but layer 1 is {depth}; "
                    + "every layer has to be the same size");
            }

            foreach (var (row, line) in layer.Select((row, line) => (row, line)))
            {
                if (row.Length != width)
                {
                    throw new ScriptError(origin,
                        $"{at} row {line + 1} is {row.Length} cell(s) wide but row 1 is {width}");
                }

                // The game reads anything that is not '_' as filled, which would turn a
                // typo into a voxel. Naming the character is more use to an author.
                var stray = row.FirstOrDefault(cell => cell is not ('#' or '_'));
                if (stray != default)
                {
                    throw new ScriptError(origin,
                        $"{at} row {line + 1} contains '{stray}'; rows use '#' for material and '_' for empty");
                }
            }
        }

        if (!pattern.Any(layer => layer.Any(row => row.Contains('#'))))
        {
            throw new ScriptError(origin, "pattern leaves nothing, so the recipe can never be completed");
        }

        return pattern;
    }
}
