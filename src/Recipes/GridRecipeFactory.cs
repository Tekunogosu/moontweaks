using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>Builds Vintage Story grid recipes from the shape scripts declare.</summary>
public sealed class GridRecipeFactory(IWorldAccessor world)
{
    private readonly RecipeAssets assets = new(world);

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

        Cells(spec, "copyAttributesFrom", spec.CopyAttributesFrom is { } copied ? [copied] : [], origin);
        Cells(spec, "mergeAttributesFrom", spec.MergeAttributesFrom ?? [], origin);

        return assets.Recipe(new GridRecipe
        {
            IngredientPattern = string.Join(",", spec.Pattern),
            Width = width,
            Height = spec.Pattern.Length,
            Shapeless = spec.Shapeless,
            CopyAttributesFrom = spec.CopyAttributesFrom,
            MergeAttributesFrom = spec.MergeAttributesFrom ?? [],
            ShowInCreatedBy = spec.ShowInCreatedBy,
            AverageDurability = spec.AverageDurability,
            Ingredients = spec.Ingredients.ToDictionary(
                entry => entry.Key,
                entry => assets.Ingredient(entry.Value, origin, $"ingredients.{entry.Key}")),
            Output = assets.Output(spec.Output, origin),
        }, spec, origin);
    }

    /// <summary>
    /// Checks the pattern characters a field names against the ones the recipe
    /// declares. The game looks an ingredient up by that character and skips a miss
    /// in silence, so a misspelling costs the output its attributes and says nothing.
    /// </summary>
    private static void Cells(GridRecipeSpec spec, string field, string[] cells, ScriptOrigin origin)
    {
        if (cells.FirstOrDefault(cell => !spec.Ingredients.ContainsKey(cell)) is { } missing)
        {
            throw new ScriptError(origin, $"{field} names '{missing}' but ingredients has no such key");
        }
    }
}
