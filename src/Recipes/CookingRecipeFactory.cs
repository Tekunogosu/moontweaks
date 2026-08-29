using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>Builds the meals a pot cooks, from the shape scripts declare.</summary>
/// <remarks>
/// A cooking recipe expands into nothing. A wildcard among its accepted stacks stays
/// a wildcard and the pot matches against it as it cooks, which is why one is kept
/// here rather than turned into a recipe per variant.
/// </remarks>
public sealed class CookingRecipeFactory(IWorldAccessor world)
{
    /// <summary>How many slots a pot holds, which bounds what an ingredient may ask for.</summary>
    /// <remarks>
    /// The game builds the cooking inventory as four slots. That bounds how many any
    /// one ingredient may ask for and how many they may all require at once; it does
    /// not bound how many ingredients a recipe lists, since most of them are
    /// alternatives that a given pot never holds together. The game's own meat stew
    /// names nine.
    /// </remarks>
    private const int PotSlots = 4;

    private readonly AssetStacks stacks = new(world);

    /// <summary>
    /// Translates one spec and resolves the result, so a recipe that reaches the log
    /// is one the game has already accepted.
    /// </summary>
    public CookingRecipe Build(CookingRecipeSpec spec, ScriptOrigin origin)
    {
        var built = Create(spec, origin);
        var offered = built.Ingredients!.Select(ingredient => ingredient.ValidStacks.Length).ToList();

        built.Resolve((IServerWorldAccessor)world, $"moontweaks {origin}");

        // The game's own resolve drops a stack it could not resolve and says nothing,
        // which would quietly shrink what an ingredient accepts. Every code was
        // checked against a registry above, so a stack going missing here means
        // something else, and is worth stopping for rather than shipping.
        foreach (var (ingredient, index) in built.Ingredients!.Select((value, index) => (value, index)))
        {
            if (ingredient.ValidStacks.Length == offered[index]) continue;

            throw new ScriptError(origin,
                $"ingredients[{index + 1}].validStacks lost "
                + $"{offered[index] - ingredient.ValidStacks.Length} of {offered[index]} entries when the "
                + "game resolved them, so the ingredient would accept less than it names");
        }

        if (built.CooksInto is { ResolvedItemstack: null })
        {
            throw new ScriptError(origin, $"cooksInto names '{built.CooksInto.Code}', which resolved to nothing");
        }

        return built;
    }

    /// <summary>Translates one spec, rejecting a meal a pot could never cook.</summary>
    private CookingRecipe Create(CookingRecipeSpec spec, ScriptOrigin origin)
    {
        if (spec.Ingredients.Length == 0)
        {
            throw new ScriptError(origin, "ingredients is empty, so there is nothing to cook");
        }

        Slots(spec, origin);

        return new CookingRecipe
        {
            Code = spec.Code,
            Enabled = spec.Enabled,
            Shape = new CompositeShape { Base = new AssetLocation(spec.Shape) },
            PerishableProps = stacks.Transitionable(spec.PerishableProps, origin, "perishableProps"),
            CooksInto = spec.CooksInto is { } into ? stacks.Stack(into, origin, "cooksInto") : null,
            IsFood = spec.IsFood,
            Ingredients = spec.Ingredients
                .Select((ingredient, index) => Ingredient(ingredient, origin, $"ingredients[{index + 1}]"))
                .ToArray(),
        };
    }

    /// <summary>
    /// Checks that everything the recipe requires at once can fit in the pot. What
    /// each ingredient asks for at least is what a pot must hold together, so those
    /// totalling more than the slots there are describes a meal nobody could cook.
    /// The largest are not bounded the same way: they are alternatives, and no pot
    /// holds all of them.
    /// </summary>
    private static void Slots(CookingRecipeSpec spec, ScriptOrigin origin)
    {
        var least = spec.Ingredients.Sum(ingredient => ingredient.MinQuantity);
        if (least > PotSlots)
        {
            throw new ScriptError(origin,
                $"the fewest slots the ingredients need add up to {least}, and a pot holds {PotSlots}");
        }
    }

    /// <summary>Translates one ingredient, rejecting a slot count no pot could satisfy.</summary>
    private CookingRecipeIngredient Ingredient(
        CookingIngredientSpec spec, ScriptOrigin origin, string at)
    {
        if (spec.ValidStacks.Length == 0)
        {
            throw new ScriptError(origin, $"{at}.validStacks is empty, so nothing could ever fill it");
        }

        if (spec.MinQuantity < 0)
        {
            throw new ScriptError(origin, $"{at}.minQuantity is negative, and a pot cannot hold fewer than no slots");
        }

        if (spec.MaxQuantity < spec.MinQuantity)
        {
            throw new ScriptError(origin,
                $"{at} has a maxQuantity of {spec.MaxQuantity} below its minQuantity of {spec.MinQuantity}");
        }

        if (spec.MaxQuantity > PotSlots)
        {
            throw new ScriptError(origin,
                $"{at}.maxQuantity asks for {spec.MaxQuantity} slots, and a pot holds {PotSlots}");
        }

        return new CookingRecipeIngredient
        {
            Code = spec.Code,
            MinQuantity = spec.MinQuantity,
            MaxQuantity = spec.MaxQuantity,
            PortionSizeLitres = (float)spec.PortionSizeLitres,
            TypeName = spec.TypeName ?? "unknown",
            ValidStacks = spec.ValidStacks
                .Select((stack, index) => Accepted(stack, origin, $"{at}.validStacks[{index + 1}]"))
                .ToArray(),
        };
    }

    /// <summary>One thing an ingredient accepts, and how the pot should draw it.</summary>
    private CookingRecipeStack Accepted(CookingStackSpec spec, ScriptOrigin origin, string at)
    {
        if (spec.TextureMapping is { Length: not 2 } mapping)
        {
            throw new ScriptError(origin,
                $"{at}.textureMapping has {mapping.Length} entries, and a mapping is a shape code and a texture");
        }

        return new CookingRecipeStack
        {
            // Checked against a registry rather than left to resolve: a wildcard that
            // matches nothing and a code that does not exist both cook into silence.
            Type = stacks.Resolve(spec.Code!, spec.Type, origin, at),
            Code = new AssetLocation(spec.Code),
            StackSize = 1,
            Attributes = AssetStacks.Attributes(spec.Attributes),
            ShapeElement = spec.ShapeElement,
            TextureMapping = spec.TextureMapping,
            CookedStack = spec.CookedStack is { } cooked
                ? stacks.Stack(cooked, origin, $"{at}.cookedStack")
                : null,
        };
    }
}
