using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>Builds Vintage Story grid recipes from the shape scripts declare.</summary>
public sealed class GridRecipeFactory(IWorldAccessor world)
{
    private readonly AssetKindResolver kinds = new(world);

    /// <summary>Translates one spec, rejecting patterns that cannot describe a grid.</summary>
    public GridRecipe Create(GridRecipeSpec spec, ScriptOrigin origin)
    {
        if (spec.Pattern.Length == 0) throw new ScriptError(origin, "pattern has no rows");

        var width = spec.Pattern[0].Length;
        if (width == 0) throw new ScriptError(origin, "pattern row 1 is empty");

        foreach (var (row, index) in spec.Pattern.Select((row, index) => (row, index)))
        {
            if (row.Length != width)
            {
                throw new ScriptError(origin,
                    $"pattern row {index + 1} is {row.Length} cell(s) wide but row 1 is {width}");
            }
        }

        var used = spec.Pattern.SelectMany(row => row).Where(cell => cell != '_').Select(cell => cell.ToString());
        foreach (var cell in used.Distinct())
        {
            if (!spec.Ingredients.ContainsKey(cell))
            {
                throw new ScriptError(origin, $"pattern uses '{cell}' but ingredients has no such key");
            }
        }

        foreach (var key in spec.Ingredients.Keys.Where(key => !spec.Pattern.Any(row => row.Contains(key))))
        {
            throw new ScriptError(origin, $"ingredients declares '{key}' but the pattern never uses it");
        }

        return new GridRecipe
        {
            Name = new AssetLocation(spec.Name ?? $"moontweaks:{spec.Output.Code}"),
            IngredientPattern = string.Join(",", spec.Pattern),
            Width = width,
            Height = spec.Pattern.Length,
            Shapeless = spec.Shapeless,
            CopyAttributesFrom = spec.CopyAttributesFrom,
            Ingredients = spec.Ingredients.ToDictionary(
                entry => entry.Key,
                entry => Ingredient(entry.Value, origin, $"ingredients.{entry.Key}")),
            Output = Output(spec.Output, origin),
        };
    }

    private CraftingRecipeIngredient Ingredient(IngredientSpec spec, ScriptOrigin origin, string path) => new()
    {
        Type = kinds.Resolve(spec.Code, spec.Type, origin, path),
        Code = new AssetLocation(spec.Code),
        Quantity = spec.Quantity,
        Name = spec.Name,
        AllowedVariants = spec.AllowedVariants,
        SkipVariants = spec.SkipVariants,
        IsTool = spec.IsTool,
        ToolDurabilityCost = spec.ToolDurabilityCost,
    };

    private CraftingRecipeIngredient Output(OutputSpec spec, ScriptOrigin origin) => new()
    {
        Type = kinds.Resolve(spec.Code, spec.Type, origin, "output"),
        Code = new AssetLocation(spec.Code),
        Quantity = spec.Quantity,
    };
}
