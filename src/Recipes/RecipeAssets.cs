using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MoonTweaks.Recipes;

/// <summary>
/// The assets a recipe names, translated from the shapes scripts write into the
/// records Vintage Story resolves. Sole owner of that translation, so no recipe
/// domain spells out a code, a registry and a variant list for itself.
/// </summary>
public sealed class RecipeAssets(IWorldAccessor world)
{
    private readonly AssetKindResolver kinds = new(world);

    /// <summary>A material a recipe is worked from, named by code, by tags, or by both.</summary>
    public CraftingRecipeIngredient Ingredient(MaterialSpec spec, ScriptOrigin origin, string path)
    {
        if (spec.Code is null && spec.Tags is null)
        {
            throw new ScriptError(origin, $"{path} names neither a 'code' nor any 'tags', so nothing can match it");
        }

        return new CraftingRecipeIngredient
        {
            // A tags-only ingredient matches on what an asset is, so it has no code
            // to look a registry up by and stays an item unless told otherwise.
            Type = spec.Code is { } code
                ? kinds.Resolve(code, spec.Type, origin, path)
                : spec.Type == ResourceKind.Block ? EnumItemClass.Block : EnumItemClass.Item,
            Code = spec.Code is null ? null : new AssetLocation(spec.Code),
            Name = spec.Name,
            AllowedVariants = spec.AllowedVariants,
            SkipVariants = spec.SkipVariants,
            Tags = Condition(spec.Tags, origin, path),
        };
    }

    /// <summary>
    /// Turns a list of tag names into the condition an ingredient matches against.
    /// One condition requiring every tag: an asset carrying only some of them does
    /// not match.
    /// </summary>
    private ComplexTagCondition<TagSet> Condition(string[]? tags, ScriptOrigin origin, string path)
    {
        if (tags is null || tags.Length == 0) return default;

        var registry = world.Api.CollectibleTagRegistry;

        // The registry reports which names it did not know rather than guessing, so
        // a misspelled tag names itself instead of silently matching nothing.
        if (registry.TryCreateTagSet(out var required, tags) is var error && error != TagRegistryError.None)
        {
            var unknown = tags.Where(tag =>
                registry.TryCreateTagSet(out _, [tag]) != TagRegistryError.None).ToList();

            throw new ScriptError(origin, unknown.Count > 0
                ? $"{path}.tags names {string.Join(", ", unknown.Select(tag => $"'{tag}'"))}, "
                  + "which no item or block carries"
                : $"{path}.tags could not be read ({error})");
        }

        return new ComplexTagCondition<TagSet>
        {
            conditions = [new ComplexTagCondition<TagSet>.Condition { RequiredTags = required }],
            isDisjunctive = false,
        };
    }

    /// <summary>A material a recipe also consumes, in a quantity and possibly as a tool.</summary>
    public CraftingRecipeIngredient Ingredient(IngredientSpec spec, ScriptOrigin origin, string path)
    {
        var ingredient = Ingredient((MaterialSpec)spec, origin, path);
        ingredient.Quantity = spec.Quantity;
        ingredient.IsTool = spec.IsTool;
        ingredient.ToolDurabilityCost = spec.ToolDurabilityCost;
        return ingredient;
    }

    /// <summary>A product, for the recipe kinds that describe one as an ingredient.</summary>
    public CraftingRecipeIngredient Output(OutputSpec spec, ScriptOrigin origin) => new()
    {
        Type = kinds.Resolve(spec.Code!, spec.Type, origin, "output"),
        Code = new AssetLocation(spec.Code),
        Quantity = spec.Quantity,
    };

    /// <summary>A product, for the recipe kinds that describe one as a stack.</summary>
    public JsonItemStack Stack(OutputSpec spec, ScriptOrigin origin) => new()
    {
        Type = kinds.Resolve(spec.Code!, spec.Type, origin, "output"),
        Code = new AssetLocation(spec.Code),
        StackSize = spec.Quantity,
    };
}
