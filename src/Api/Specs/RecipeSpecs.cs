using System.Collections.Generic;
using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

// One shape per way the game makes something, over the asset shapes above.

/// <summary>
/// What every recipe kind carries, whatever it is worked on. Mirrors the base the
/// game's own recipes share, so a field it reads for every kind is declared once
/// rather than once per kind.
/// </summary>
public abstract class RecipeSpec
{
    /// <summary>Identifies the recipe in logs and in the handbook. Defaults to the output code.</summary>
    [LuaField("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Whether the recipe is registered. A disabled recipe is still built and
    /// checked, so a mistake in one is reported now rather than on the day it is
    /// switched back on.
    /// </summary>
    [LuaField("enabled", Default = "true")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Character trait a player must hold to use this recipe, such as
    /// <c>clothier</c>. Servers that turn the <c>classExclusiveRecipes</c> world
    /// configuration off ignore it, on this recipe exactly as on the game's own.
    /// </summary>
    [LuaField("requiresTrait")]
    [LuaSuggests(SuggestionSets.AssetTrait)]
    public string? RequiresTrait { get; set; }


    /// <summary>
    /// Arbitrary data carried on the recipe itself, written as a Lua table and
    /// stored as JSON. What a key means is the game's business rather than this
    /// mod's: liquid ingredients need one, and a mod reads what it wrote.
    /// </summary>
    [LuaField("attributes")]
    public ScriptValue? Attributes { get; set; }

    /// <summary>
    /// Code of what this recipe produces, which names the recipe when nothing else
    /// does. Not a key scripts write: every kind already declares its own output,
    /// whose <c>code</c> is required, so a bound recipe always has one.
    /// </summary>
    public abstract string OutputCode { get; }
}

/// <summary>A crafting grid recipe.</summary>
[LuaTable("GridRecipe")]
public sealed class GridRecipeSpec : RecipeSpec
{
    /// <summary>
    /// Rows of the crafting grid, one string per row and one character per column.
    /// <c>_</c> marks an empty cell. Width and height are taken from the rows, so
    /// <c>{ "T", "B" }</c> is one column by two rows and <c>{ "TB" }</c> is two by one.
    /// </summary>
    [LuaField("pattern", Required = true)]
    public string[] Pattern { get; set; } = [];

    /// <summary>Maps each character used in <c>pattern</c> to an ingredient.</summary>
    [LuaField("ingredients", Required = true)]
    public Dictionary<string, IngredientSpec> Ingredients { get; set; } = [];

    /// <summary>What the recipe produces.</summary>
    [LuaField("output", Required = true)]
    public OutputSpec Output { get; set; } = new();

    /// <summary>
    /// Ingredients may be placed in any arrangement rather than the pattern shown.
    /// The pattern still says which ingredients the recipe takes and how many, and
    /// its width and height still bound it: a grid narrower or shorter than the
    /// pattern never matches, however the ingredients are arranged. Write one
    /// compactly for that reason — four ingredients as two rows of two rather than
    /// one row of four, which a three-wide grid could never satisfy.
    /// </summary>
    [LuaField("shapeless", Default = "false")]
    public bool Shapeless { get; set; }

    /// <summary>Copies item attributes from the ingredient under this pattern character onto the output.</summary>
    [LuaField("copyAttributesFrom")]
    public string? CopyAttributesFrom { get; set; }

    /// <summary>
    /// Gives the product the durability the tools that made it averaged. Bound on
    /// this kind alone: it is read as the product lands in the crafting output slot,
    /// which no other kind passes through.
    /// </summary>
    [LuaField("averageDurability", Default = "true")]
    public bool AverageDurability { get; set; } = true;

    /// <inheritdoc/>
    public override string OutputCode => Output.Code!;
}

/// <summary>
/// Which recipes to act on, by the code they produce, by the tags that output
/// carries, or by both. A bare string is taken as the code, so a selector that names
/// only a code may be written as that code.
/// </summary>
[LuaTable("RecipeSelector", Shorthand = "code")]
public sealed class RecipeSelectorSpec
{
    /// <summary>
    /// Output code to match. May contain a <c>*</c> wildcard, so <c>"game:axe-*"</c>
    /// reaches the whole family. Required unless <c>tags</c> names what to match.
    /// </summary>
    [LuaField("code")]
    [LuaSuggests(SuggestionSets.AssetCode)]
    public string? Code { get; set; }

    /// <summary>
    /// Tags the output must carry, such as <c>tool-axe</c>. Matches on what the
    /// product is rather than what it is called, so one entry reaches a modded axe as
    /// readily as a vanilla one. Used alone, or alongside <c>code</c> to narrow a
    /// wildcard further.
    /// </summary>
    [LuaField("tags")]
    [LuaSuggests(SuggestionSets.AssetTag)]
    public string[]? Tags { get; set; }
}

/// <summary>
/// A recipe shaped voxel by voxel rather than by arranging items: the same pattern,
/// material and output whether it is chipped from stone, raised in clay or hammered
/// on an anvil. What differs is how many layers deep the kind works, which its own
/// <c>pattern</c> says.
/// </summary>
public abstract class VoxelRecipeSpec : RecipeSpec
{
    /// <summary>
    /// Rows of the working surface, one string per row and one character per column.
    /// <c>#</c> keeps material and <c>_</c> leaves the cell empty. The surface is 16
    /// by 16 and a smaller pattern is centred on it.
    /// </summary>
    [LuaField("pattern", Required = true)]
    public virtual string[][] Pattern { get; set; } = [];

    /// <summary>The material being worked, which decides where the recipe is offered.</summary>
    [LuaField("ingredient", Required = true)]
    public MaterialSpec Ingredient { get; set; } = new();

    /// <summary>What the recipe produces.</summary>
    [LuaField("output", Required = true)]
    public OutputSpec Output { get; set; } = new();

    /// <inheritdoc/>
    public override string OutputCode => Output.Code!;
}

/// <summary>A knapping recipe, chipped from a stone laid on a knapping surface.</summary>
[LuaTable("KnappingRecipe")]
public sealed class KnappingRecipeSpec : VoxelRecipeSpec
{
    /// <summary>
    /// Rows of the knapping surface, one string per row and one character per column.
    /// <c>#</c> leaves stone in place and <c>_</c> chips it away. The surface is 16 by
    /// 16 and a smaller pattern is centred on it. Knapping shapes one layer, so the
    /// rows are written directly.
    /// </summary>
    [LuaField("pattern", Required = true)]
    public override string[][] Pattern { get; set; } = [];
}

/// <summary>A clay forming recipe, raised layer by layer from a clay lump.</summary>
[LuaTable("ClayFormingRecipe")]
public sealed class ClayFormingRecipeSpec : VoxelRecipeSpec
{
    /// <summary>
    /// Layers of the clay, bottom first, each a list of rows. <c>#</c> places clay and
    /// <c>_</c> leaves the cell empty. The surface is 16 by 16 and clay forming builds
    /// up to 16 layers, every one of which must be the same size as the first.
    /// </summary>
    [LuaField("pattern", Required = true)]
    public override string[][] Pattern { get; set; } = [];
}

/// <summary>A smithing recipe, hammered from a hot ingot on an anvil.</summary>
[LuaTable("SmithingRecipe")]
public sealed class SmithingRecipeSpec : VoxelRecipeSpec
{
    /// <summary>
    /// Layers of the work, bottom first, each a list of rows. <c>#</c> keeps metal and
    /// <c>_</c> is metal to be cut or hammered away. The surface is 16 by 16 and an
    /// anvil works up to 6 layers, every one of which must be the same size as the
    /// first.
    /// </summary>
    [LuaField("pattern", Required = true)]
    public override string[][] Pattern { get; set; } = [];

    /// <summary>
    /// Groups the recipe on the anvil's selection dialog, and may contain
    /// <c>{name}</c> placeholders naming a wildcard ingredient. Defaults to the
    /// output code.
    /// </summary>
    [LuaField("code")]
    public string? Code { get; set; }
}

/// <summary>
/// One input a barrel recipe consumes, measured in items, in litres, or in both.
/// </summary>
[LuaTable("BarrelIngredient", Shorthand = "code")]
public sealed class BarrelIngredientSpec : MaterialSpec
{
    /// <summary>How many items the barrel must hold. Left at one for a liquid, which <c>litres</c> measures instead.</summary>
    [LuaField("quantity", Default = "1")]
    public int Quantity { get; set; } = 1;

    /// <summary>How much of a liquid the barrel must hold. Zero for anything counted in items.</summary>
    [LuaField("litres", Default = "0")]
    public double Litres { get; set; }

    /// <summary>How many items the recipe takes, when it takes fewer than it needs present. Takes all of them when omitted.</summary>
    [LuaField("consumeQuantity")]
    public int? ConsumeQuantity { get; set; }

    /// <summary>How much liquid the recipe takes, when it takes less than it needs present. Takes all of it when omitted.</summary>
    [LuaField("consumeLitres")]
    public double? ConsumeLitres { get; set; }
}

/// <summary>What a barrel recipe produces, which may be a liquid.</summary>
[LuaTable("BarrelOutput", Shorthand = "code")]
public sealed class BarrelOutputSpec : StackSpec
{
    /// <summary>How much of a liquid the recipe yields. Zero for anything counted in items.</summary>
    [LuaField("litres", Default = "0")]
    public double Litres { get; set; }
}

/// <summary>
/// A barrel recipe, either mixed on the spot or left to seal for a while.
/// </summary>
[LuaTable("BarrelRecipe")]
public sealed class BarrelRecipeSpec : RecipeSpec
{
    /// <summary>
    /// Identifies the recipe to the game, which requires every barrel recipe to carry
    /// one and leaves the choice open. Unrelated to the codes that name assets.
    /// </summary>
    [LuaField("code", Required = true)]
    public string Code { get; set; } = "";

    /// <summary>
    /// What the barrel must hold, listed rather than keyed: a barrel has no grid, so
    /// nothing places an ingredient anywhere in particular.
    /// </summary>
    [LuaField("ingredients", Required = true)]
    public BarrelIngredientSpec[] Ingredients { get; set; } = [];

    /// <summary>What the recipe produces.</summary>
    [LuaField("output", Required = true)]
    public BarrelOutputSpec Output { get; set; } = new();

    /// <summary>
    /// How long the barrel must stay sealed, in in-game hours. Left at zero the
    /// recipe mixes the moment its ingredients are in, which is how the game tells
    /// the two kinds of barrel recipe apart.
    /// </summary>
    [LuaField("sealHours", Default = "0")]
    public double SealHours { get; set; }

    /// <inheritdoc/>
    public override string OutputCode => Output.Code!;
}
