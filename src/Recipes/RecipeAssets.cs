using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Newtonsoft.Json.Linq;
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
    private readonly AssetKindResolver kinds = new(world);
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
        recipe.Attributes = Attributes(spec.Attributes);
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
            ? kinds.Resolve(kind, spec.Type, origin, path)
            : spec.Type == ResourceKind.Block ? EnumItemClass.Block : EnumItemClass.Item;
        ingredient.Name = spec.Name;
        ingredient.AllowedVariants = spec.AllowedVariants;
        ingredient.SkipVariants = spec.SkipVariants;
        ingredient.Tags = Condition(spec.Tags, origin, path);
        ingredient.Attributes = Attributes(spec.Attributes);

        // Assigned only when a script names one, so a tags-only ingredient keeps the
        // wildcard code the game initialises this with. Writing null over it reads as
        // the same thing and is not: the shapeless matcher passes the code to
        // WildcardUtil.Match, which returns true for that wildcard and dereferences a
        // null. The shaped matcher guards, which is why only shapeless recipes took
        // the server down.
        if (spec.Code is { } code) ingredient.Code = new AssetLocation(code);
    }

    /// <summary>
    /// Turns a list of tag names into the condition an ingredient matches against.
    /// One condition requiring every tag: an asset carrying only some of them does
    /// not match.
    /// </summary>
    /// <remarks>
    /// The flag reads backwards and decides which of two meanings
    /// <c>RequiredTags</c> has. Disjunctive asks whether the tags are all contained
    /// in the asset's; conjunctive asks only whether the two sets overlap, so a
    /// single condition built that way accepts an asset carrying any one of them.
    /// This is the shape the game's own converter builds for a bare tag array, so a
    /// script's <c>tags</c> and a recipe file's mean the same thing.
    /// </remarks>
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
            isDisjunctive = true,
        };
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
        Type = kinds.Resolve(spec.Code!, spec.Type, origin, "output"),
        Code = new AssetLocation(spec.Code),
        StackSize = spec.Quantity,
        Litres = (float)spec.Litres,
        Attributes = Attributes(spec.Attributes),
    };

    /// <summary>A product, for the recipe kinds that describe one as an ingredient.</summary>
    public CraftingRecipeIngredient Output(OutputSpec spec, ScriptOrigin origin) => new()
    {
        Type = kinds.Resolve(spec.Code!, spec.Type, origin, "output"),
        Code = new AssetLocation(spec.Code),
        Quantity = spec.Quantity,
        Attributes = Attributes(spec.Attributes),
    };

    /// <summary>A named asset and how many of it, for the fields the game holds as a stack.</summary>
    public JsonItemStack Stack(StackSpec spec, ScriptOrigin origin, string path) => new()
    {
        Type = kinds.Resolve(spec.Code!, spec.Type, origin, path),
        Code = new AssetLocation(spec.Code),
        StackSize = spec.Quantity,
        Attributes = Attributes(spec.Attributes),
    };

    /// <summary>
    /// A Lua table as the JSON the game stores arbitrary recipe data in. Sole owner
    /// of that translation: the ingredient, the output and the recipe of every kind
    /// each carry one of these, and every one of them arrives here.
    /// </summary>
    private static JsonObject? Attributes(ScriptValue? value) =>
        value is null or ScriptValue.Nil ? null : new JsonObject(Token(value));

    /// <summary>
    /// One script value as the JSON token it already is. The tree maps onto JSON
    /// exactly, so nothing here decides anything: a list is an array, a table is an
    /// object, and the scalars are themselves.
    /// </summary>
    private static JToken Token(ScriptValue value) => value switch
    {
        ScriptValue.Str text => new JValue(text.Value),
        ScriptValue.Bool flag => new JValue(flag.Value),
        // Lua has one number type, so a whole number reaches here as a double and
        // would be written as one. The game turns these into typed attributes, and a
        // JSON recipe's 3 becomes a long there: writing 3.0 would hand it a double
        // instead, where a script and a recipe file saying the same thing should
        // leave the game holding the same thing.
        ScriptValue.Num number => new JValue(Whole(number.Value)),
        ScriptValue.List list => new JArray(list.Items.Select(Token).ToArray<object>()),
        ScriptValue.Map map => new JObject(
            map.Entries.Select(entry => new JProperty(entry.Key, Token(entry.Value))).ToArray<object>()),
        _ => JValue.CreateNull(),
    };

    /// <summary>A number as the integer it is, or as itself when it is not one.</summary>
    private static object Whole(double number) =>
        number % 1 == 0 && number is >= long.MinValue and <= long.MaxValue ? (long)number : number;
}
