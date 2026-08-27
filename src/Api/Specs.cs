using System.Collections.Generic;

namespace MoonTweaks.Api;

/// <summary>Which registry an asset code names.</summary>
public enum ResourceKind
{
    /// <summary>An item, the default for codes that resolve in both registries.</summary>
    Item,

    /// <summary>A block.</summary>
    Block,
}

/// <summary>
/// Names one asset. Not written by scripts directly: every shape below is this
/// plus whatever that shape does with the asset it names.
/// </summary>
public abstract class AssetSpec
{
    /// <summary>
    /// Asset code such as <c>game:stick</c>. May contain a <c>*</c> wildcard.
    /// Required unless <c>tags</c> names what to match instead.
    /// </summary>
    [LuaField("code", Suggests = "AssetCode")]
    public virtual string? Code { get; set; }

    /// <summary>Registry the code names. Inferred by looking the code up when omitted.</summary>
    [LuaField("type")]
    public ResourceKind? Type { get; set; }
}

/// <summary>
/// A material a recipe is worked from, which a wildcard may name as a family.
/// </summary>
[LuaTable("Material", Shorthand = "code")]
public class MaterialSpec : AssetSpec
{
    /// <summary>
    /// Names this wildcard so the matched variant can be substituted into the
    /// output as <c>{name}</c>. Required only when <c>code</c> contains a wildcard
    /// and the output depends on which variant matched.
    /// </summary>
    [LuaField("name")]
    public string? Name { get; set; }

    /// <summary>Restricts a wildcard to these variants. Every variant is allowed when omitted.</summary>
    [LuaField("allowedVariants")]
    public string[]? AllowedVariants { get; set; }

    /// <summary>Excludes these variants from a wildcard.</summary>
    [LuaField("skipVariants")]
    public string[]? SkipVariants { get; set; }

    /// <summary>
    /// Tags every match must carry, such as <c>tool-axe</c>. Matches on what an
    /// asset is rather than what it is called, so one entry accepts a modded axe
    /// as readily as a vanilla one. Used alone, or alongside <c>code</c> to narrow
    /// a wildcard further.
    /// </summary>
    [LuaField("tags", Suggests = "AssetTag")]
    public string[]? Tags { get; set; }
}

/// <summary>
/// One input a recipe consumes: a material, in a quantity, possibly as a tool.
/// </summary>
[LuaTable("Ingredient", Shorthand = "code")]
public sealed class IngredientSpec : MaterialSpec
{
    /// <summary>How many the recipe consumes.</summary>
    [LuaField("quantity", Default = "1")]
    public int Quantity { get; set; } = 1;

    /// <summary>Used as a tool: not consumed, loses durability instead.</summary>
    [LuaField("isTool", Default = "false")]
    public bool IsTool { get; set; }

    /// <summary>Durability removed when this is used as a tool.</summary>
    [LuaField("toolDurabilityCost", Default = "0")]
    public int ToolDurabilityCost { get; set; }
}

/// <summary>What a recipe produces.</summary>
[LuaTable("Output", Shorthand = "code")]
public sealed class OutputSpec : AssetSpec
{
    /// <summary>
    /// Asset code of the product. May contain <c>{name}</c> placeholders naming a
    /// wildcard ingredient, which expands into one recipe per matched variant.
    /// </summary>
    [LuaField("code", Required = true, Suggests = "AssetCode")]
    public override string? Code { get; set; }

    /// <summary>How many the recipe yields.</summary>
    [LuaField("quantity", Default = "1")]
    public int Quantity { get; set; } = 1;
}

/// <summary>A crafting grid recipe.</summary>
[LuaTable("GridRecipe")]
public sealed class GridRecipeSpec
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

    /// <summary>Identifies the recipe in logs and in the handbook. Defaults to the output code.</summary>
    [LuaField("name")]
    public string? Name { get; set; }

    /// <summary>Ingredients may be placed in any arrangement rather than the pattern shown.</summary>
    [LuaField("shapeless", Default = "false")]
    public bool Shapeless { get; set; }

    /// <summary>Copies item attributes from the ingredient under this pattern character onto the output.</summary>
    [LuaField("copyAttributesFrom")]
    public string? CopyAttributesFrom { get; set; }
}

/// <summary>A knapping recipe, chipped from a stone laid on a knapping surface.</summary>
[LuaTable("KnappingRecipe")]
public sealed class KnappingRecipeSpec
{
    /// <summary>
    /// Rows of the knapping surface, one string per row and one character per
    /// column. <c>#</c> leaves stone in place and <c>_</c> chips it away. The
    /// surface is 16 by 16 and a smaller pattern leaves the rest of it untouched.
    /// Knapping shapes one layer, so the rows are written directly.
    /// </summary>
    [LuaField("pattern", Required = true)]
    public string[][] Pattern { get; set; } = [];

    /// <summary>The stone being knapped, which decides where the recipe is offered.</summary>
    [LuaField("ingredient", Required = true)]
    public MaterialSpec Ingredient { get; set; } = new();

    /// <summary>What the recipe produces.</summary>
    [LuaField("output", Required = true)]
    public OutputSpec Output { get; set; } = new();

    /// <summary>Identifies the recipe in logs and in the handbook. Defaults to the output code.</summary>
    [LuaField("name")]
    public string? Name { get; set; }
}
