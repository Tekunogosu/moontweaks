using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>Builds the alloys a crucible smelts, from the shape scripts declare.</summary>
/// <remarks>
/// Unlike every other kind, an alloy expands into nothing: the game holds it as a
/// plain list of metals and their shares rather than as a recipe, so it offers no
/// way to generate one per matched variant. A family code is refused here for that
/// reason rather than registered as a recipe nothing would ever match.
/// </remarks>
public sealed class AlloyRecipeFactory(IWorldAccessor world)
{
    /// <summary>How far a total may fall outside one before it is called impossible.</summary>
    /// <remarks>
    /// Shares are written as decimals and summed, so a mix meant to reach exactly one
    /// lands a hair either side of it. The tolerance covers that and nothing wider:
    /// the game rounds shares to four places before comparing them.
    /// </remarks>
    private const double Rounding = 1e-6;

    private readonly RecipeAssets assets = new(world);

    /// <summary>
    /// Translates one spec and resolves the result, so an alloy that reaches the log
    /// is one the game has already accepted.
    /// </summary>
    public AlloyRecipe Build(AlloyRecipeSpec spec, ScriptOrigin origin)
    {
        var built = Create(spec, origin);

        foreach (var (ingredient, index) in built.Ingredients.Select((value, index) => (value, index)))
        {
            Resolve(ingredient, origin, $"ingredients[{index + 1}]");
        }

        Resolve(built.Output, origin, "output");
        return built;
    }

    /// <summary>Translates one spec, rejecting a mix a crucible could never pour.</summary>
    private AlloyRecipe Create(AlloyRecipeSpec spec, ScriptOrigin origin)
    {
        if (spec.Ingredients.Length == 0)
        {
            throw new ScriptError(origin, "ingredients is empty, so there is no mix to smelt");
        }

        Concrete(spec.Output.Code!, origin, "output.code");

        foreach (var (ingredient, index) in spec.Ingredients.Select((value, index) => (value, index)))
        {
            Shares(ingredient, origin, $"ingredients[{index + 1}]");
        }

        Total(spec.Ingredients, origin);

        return new AlloyRecipe
        {
            Enabled = spec.Enabled,
            Ingredients = spec.Ingredients
                .Select((ingredient, index) =>
                    assets.AlloyIngredient(ingredient, origin, $"ingredients[{index + 1}]"))
                .ToArray(),
            Output = assets.AlloyOutput(spec.Output, origin),
        };
    }

    /// <summary>Checks one metal's share of the mix, and that the metal is a single one.</summary>
    private static void Shares(AlloyIngredientSpec ingredient, ScriptOrigin origin, string at)
    {
        Concrete(ingredient.Code!, origin, $"{at}.code");

        if (ingredient.MinRatio < 0 || ingredient.MaxRatio > 1)
        {
            throw new ScriptError(origin,
                $"{at} asks for a share outside 0 to 1, which no part of a mix can be");
        }

        if (ingredient.MinRatio > ingredient.MaxRatio)
        {
            throw new ScriptError(origin,
                $"{at} has a minRatio of {ingredient.MinRatio} above its maxRatio of {ingredient.MaxRatio}");
        }
    }

    /// <summary>
    /// Checks that the shares can add up to a whole mix, and that no metal is named
    /// twice.
    /// </summary>
    /// <remarks>
    /// A crucible measures each metal as a share of everything it holds, so those
    /// shares total one by construction. Least shares summing above one, or greatest
    /// shares summing below it, describe a mix that can therefore never be poured.
    /// A metal named twice is the same dead end reached another way: the crucible
    /// counts every stack of one metal as a single share, leaving the second entry
    /// unsatisfied whatever is in the pot.
    /// </remarks>
    private static void Total(IReadOnlyList<AlloyIngredientSpec> ingredients, ScriptOrigin origin)
    {
        var repeated = ingredients
            .GroupBy(ingredient => ingredient.Code)
            .FirstOrDefault(sharing => sharing.Count() > 1);

        if (repeated is not null)
        {
            throw new ScriptError(origin,
                $"ingredients names '{repeated.Key}' more than once, so one of them could never be satisfied");
        }

        var least = ingredients.Sum(ingredient => ingredient.MinRatio);
        if (least > 1 + Rounding)
        {
            throw new ScriptError(origin,
                $"the smallest shares add up to {least:0.###}, so the mix is always short of a metal");
        }

        var greatest = ingredients.Sum(ingredient => ingredient.MaxRatio);
        if (greatest < 1 - Rounding)
        {
            throw new ScriptError(origin,
                $"the largest shares add up to {greatest:0.###}, so the mix always has a metal to spare");
        }
    }

    /// <summary>
    /// Refuses a code naming a family. Every other kind expands one into a recipe per
    /// variant; an alloy has nothing to expand into, so a wildcard here would resolve
    /// to nothing and register an alloy no crucible could match.
    /// </summary>
    private static void Concrete(string code, ScriptOrigin origin, string at)
    {
        if (AssetKindResolver.IsPattern(code))
        {
            throw new ScriptError(origin,
                $"{at} names '{code}', and an alloy names one metal rather than a family of them");
        }
    }

    /// <summary>
    /// Resolves one stack against the registries, failing loudly. The game's own
    /// resolve reports nothing back, so each stack is resolved here instead and the
    /// one that failed is named.
    /// </summary>
    private void Resolve(JsonItemStack stack, ScriptOrigin origin, string at)
    {
        if (!stack.Resolve(world, $"moontweaks {origin}") || stack.ResolvedItemstack is null)
        {
            throw new ScriptError(origin, $"{at} names '{stack.Code}', which resolved to nothing");
        }
    }
}
