using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>Builds the recipes a barrel mixes or seals, from the shape scripts declare.</summary>
public sealed class BarrelRecipeFactory(IWorldAccessor world)
{
    private readonly RecipeAssets assets = new(world);

    /// <summary>
    /// Translates one spec, expands whatever wildcards it names and resolves the
    /// result, so a recipe that reaches the log is one the game has already accepted.
    /// </summary>
    public IReadOnlyList<BarrelRecipe> Build(BarrelRecipeSpec spec, ScriptOrigin origin)
    {
        var built = Create(spec, origin);
        built.OnParsed(world);

        var resolved = built.GenerateRecipesForAllIngredientCombinations(world)
            .OfType<BarrelRecipe>()
            .Where(variant => variant.Resolve(world, $"moontweaks {origin}"))
            .ToList();

        if (resolved.Count == 0)
        {
            throw new ScriptError(origin,
                $"no recipe resolved for {spec.OutputCode}; check that every code exists");
        }

        return resolved;
    }

    /// <summary>Translates one spec, rejecting what a barrel could never hold.</summary>
    private BarrelRecipe Create(BarrelRecipeSpec spec, ScriptOrigin origin)
    {
        if (spec.Ingredients.Length == 0)
        {
            throw new ScriptError(origin, "ingredients is empty, so nothing could ever fill the barrel");
        }

        foreach (var (ingredient, index) in spec.Ingredients.Select((value, index) => (value, index)))
        {
            var at = $"ingredients[{index + 1}]";

            // The game refuses this when it registers the recipe, naming neither the
            // ingredient nor the script. Refusing it here does both.
            if (ingredient.ConsumeQuantity > ingredient.Quantity)
            {
                throw new ScriptError(origin,
                    $"{at} consumes {ingredient.ConsumeQuantity} but only requires {ingredient.Quantity} to be present");
            }

            if (ingredient.ConsumeLitres > ingredient.Litres)
            {
                throw new ScriptError(origin,
                    $"{at} consumes {ingredient.ConsumeLitres} litres but only requires {ingredient.Litres}");
            }
        }

        if (spec.SealHours < 0) throw new ScriptError(origin, "sealHours cannot be negative");

        return assets.Recipe(new BarrelRecipe
        {
            Code = spec.Code,
            SealHours = spec.SealHours,
            Ingredients = spec.Ingredients
                .Select((ingredient, index) =>
                    assets.BarrelIngredient(ingredient, origin, $"ingredients[{index + 1}]"))
                .ToArray(),
            Output = assets.BarrelOutput(spec.Output, origin),
        }, spec, origin);
    }
}
