using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>
/// The assets a recipe names, translated from the shapes scripts write into the
/// records Vintage Story resolves. Sole owner of that translation, so no recipe
/// domain spells out a code, a registry and a variant list for itself.
/// </summary>
public sealed class RecipeAssets(IWorldAccessor world)
{
    private readonly AssetStacks stacks = new(world);
    private readonly TraitRegistry traits = new(world.Api);

    /// <summary>
    /// The fields every recipe kind carries, set on the recipe a factory has built.
    /// Sole owner of that translation, so a kind bound later cannot quietly leave one
    /// of them unread.
    /// </summary>
    public TRecipe Recipe<TRecipe>(TRecipe recipe, RecipeSpec spec, ScriptOrigin origin)
        where TRecipe : RecipeBase
    {
        // Checked whatever the server does with it, so a misspelled trait is the same
        // error everywhere rather than one that surfaces on some servers only.
        var trait = Trait(spec.RequiresTrait, origin);

        recipe.Name = new AssetLocation(spec.Name ?? $"moontweaks:{spec.OutputCode}");
        recipe.Enabled = spec.Enabled;
        recipe.Attributes = AssetStacks.Attributes(spec.Attributes);
        // The game drops the trait from every recipe it loads on a server that does
        // not run class-exclusive recipes, so a scripted recipe gates exactly where a
        // vanilla one does.
        recipe.RequiresTrait = world.Config.GetBool("classExclusiveRecipes", true) ? trait : null;
        return recipe;
    }

    /// <summary>
    /// Checks a trait name against the ones this server defines. Naming a trait
    /// nothing holds would gate a recipe behind a class no player can pick, which is
    /// indistinguishable from the recipe simply not working.
    /// </summary>
    private string? Trait(string? trait, ScriptOrigin origin)
    {
        if (trait is null || traits.Codes.Contains(trait)) return trait;

        throw new ScriptError(origin,
            $"requiresTrait names '{trait}', which is not a character trait this server defines");
    }

    /// <summary>A material a recipe is worked from, named by code, by tags, or by both.</summary>
    public CraftingRecipeIngredient Ingredient(MaterialSpec spec, ScriptOrigin origin, string path) =>
        Material(new CraftingRecipeIngredient(), spec, origin, path);

    /// <summary>
    /// Fills in what every ingredient carries, whatever kind of ingredient it is.
    /// Written onto one the caller made rather than returned fresh, so a kind with
    /// more to say — a barrel measuring litres — starts from its own type.
    /// </summary>
    private TIngredient Material<TIngredient>(
        TIngredient ingredient, MaterialSpec spec, ScriptOrigin origin, string path)
        where TIngredient : CraftingRecipeIngredient
    {
        if (spec.Code is null && spec.Tags is null)
        {
            throw new ScriptError(origin, $"{path} names neither a 'code' nor any 'tags', so nothing can match it");
        }

        Fill(ingredient, spec, origin, path);
        return ingredient;
    }

    /// <summary>Writes the shared fields onto an ingredient of any kind.</summary>
    private void Fill(CraftingRecipeIngredient ingredient, MaterialSpec spec, ScriptOrigin origin, string path)
    {
        // A tags-only ingredient matches on what an asset is, so it has no code to
        // look a registry up by and stays an item unless told otherwise.
        ingredient.Type = spec.Code is { } kind
            ? stacks.Resolve(kind, spec.Type, origin, path)
            : spec.Type == ResourceKind.Block ? EnumItemClass.Block : EnumItemClass.Item;
        ingredient.Name = spec.Name;
        ingredient.AllowedVariants = spec.AllowedVariants;
        ingredient.SkipVariants = spec.SkipVariants;
        ingredient.Tags = stacks.Condition(spec.Tags, origin, path);
        ingredient.Attributes = AssetStacks.Attributes(spec.Attributes);

        // Assigned only when a script names one, so a tags-only ingredient keeps the
        // wildcard code the game initialises it with. A null there is not the same
        // thing: the shapeless matcher hands the code to WildcardUtil.Match, which
        // answers true for that wildcard and dereferences a null, while the shaped
        // matcher guards against one. Only the wildcard is safe on both paths.
        if (spec.Code is { } code) ingredient.Code = new AssetLocation(code);
    }

    /// <summary>A material a recipe also consumes, in a quantity and possibly as a tool.</summary>
    public CraftingRecipeIngredient Ingredient(IngredientSpec spec, ScriptOrigin origin, string path)
    {
        var ingredient = Ingredient((MaterialSpec)spec, origin, path);
        ingredient.Quantity = spec.Quantity;
        ingredient.IsTool = spec.IsTool;
        ingredient.ToolDurabilityCost = spec.ToolDurabilityCost;
        ingredient.ReturnedStack = spec.ReturnedStack is { } returned
            ? Stack(returned, origin, $"{path}.returnedStack")
            : null;
        return ingredient;
    }

    /// <summary>
    /// A material a barrel holds, which it measures in items or in litres, and may
    /// take less of than it requires to be present.
    /// </summary>
    public BarrelRecipeIngredient BarrelIngredient(BarrelIngredientSpec spec, ScriptOrigin origin, string path)
    {
        var ingredient = Material(new BarrelRecipeIngredient(), spec, origin, path);
        ingredient.Quantity = spec.Quantity;
        ingredient.Litres = (float)spec.Litres;
        ingredient.ConsumeQuantity = spec.ConsumeQuantity;
        ingredient.ConsumeLitres = spec.ConsumeLitres is { } litres ? (float)litres : null;
        return ingredient;
    }

    /// <summary>A product a barrel yields, which may be a liquid.</summary>
    public BarrelOutputStack BarrelOutput(BarrelOutputSpec spec, ScriptOrigin origin) => new()
    {
        Type = stacks.Resolve(spec.Code!, spec.Type, origin, "output"),
        Code = new AssetLocation(spec.Code),
        StackSize = spec.Quantity,
        Litres = (float)spec.Litres,
        Attributes = AssetStacks.Attributes(spec.Attributes),
    };

    /// <summary>A product, for the recipe kinds that describe one as an ingredient.</summary>
    public CraftingRecipeIngredient Output(OutputSpec spec, ScriptOrigin origin) => new()
    {
        Type = stacks.Resolve(spec.Code!, spec.Type, origin, "output"),
        Code = new AssetLocation(spec.Code),
        Quantity = spec.Quantity,
        Attributes = AssetStacks.Attributes(spec.Attributes),
    };

    /// <summary>A named asset and how many of it, as the game holds it.</summary>
    public JsonItemStack Stack(StackSpec spec, ScriptOrigin origin, string path) =>
        stacks.Stack(spec, origin, path);
}
