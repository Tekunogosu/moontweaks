using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>Builds Vintage Story knapping recipes from the shape scripts declare.</summary>
public sealed class KnappingRecipeFactory(IWorldAccessor world)
{
    /// <summary>Side of the square voxel grid a knapping surface offers.</summary>
    private const int SurfaceSize = 16;

    /// <summary>Knapping shapes the surface itself, which is a single layer of voxels.</summary>
    private const int Layers = 1;

    private readonly RecipeAssets assets = new(world);

    /// <summary>Translates one spec, rejecting patterns a knapping surface cannot hold.</summary>
    public KnappingRecipe Create(KnappingRecipeSpec spec, ScriptOrigin origin)
    {
        var rows = OnlyLayer(spec.Pattern, origin);

        return new KnappingRecipe
        {
            Name = new AssetLocation(spec.Name ?? $"moontweaks:{spec.Output.Code}"),
            Pattern = [rows],
            // Set through the array rather than the Ingredient property, which
            // writes into an array this recipe does not have yet.
            Ingredients = [assets.Ingredient(spec.Ingredient, origin, "ingredient")],
            Output = assets.Stack(spec.Output, origin),
        };
    }

    /// <summary>Takes the single layer knapping shapes, and checks it describes a surface.</summary>
    private static string[] OnlyLayer(string[][] pattern, ScriptOrigin origin)
    {
        if (pattern.Length == 0) throw new ScriptError(origin, "pattern has no rows");
        if (pattern.Length > Layers)
        {
            throw new ScriptError(origin,
                $"pattern has {pattern.Length} layers but knapping shapes a single surface");
        }

        var rows = pattern[0];
        if (rows.Length == 0) throw new ScriptError(origin, "pattern has no rows");
        if (rows.Length > SurfaceSize)
        {
            throw new ScriptError(origin,
                $"pattern is {rows.Length} rows deep but a knapping surface is {SurfaceSize}");
        }

        var width = rows[0].Length;
        if (width == 0) throw new ScriptError(origin, "pattern row 1 is empty");
        if (width > SurfaceSize)
        {
            throw new ScriptError(origin,
                $"pattern row 1 is {width} cells wide but a knapping surface is {SurfaceSize}");
        }

        foreach (var (row, index) in rows.Select((row, index) => (row, index)))
        {
            if (row.Length != width)
            {
                throw new ScriptError(origin,
                    $"pattern row {index + 1} is {row.Length} cell(s) wide but row 1 is {width}");
            }

            // The game reads anything that is not '_' as stone, which would turn a
            // typo into a voxel. Naming the character is more use to an author.
            var stray = row.FirstOrDefault(cell => cell is not ('#' or '_'));
            if (stray != default)
            {
                throw new ScriptError(origin,
                    $"pattern row {index + 1} contains '{stray}'; knapping rows use '#' for stone and '_' for empty");
            }
        }

        if (!rows.Any(row => row.Contains('#')))
        {
            throw new ScriptError(origin, "pattern leaves no stone, so the recipe can never be completed");
        }

        return rows;
    }
}
