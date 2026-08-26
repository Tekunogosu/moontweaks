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

/// <summary>One input to a recipe.</summary>
[LuaTable("Ingredient", Shorthand = "code")]
public sealed class IngredientSpec
{
    /// <summary>Asset code such as <c>game:stick</c>. May contain a <c>*</c> wildcard.</summary>
    [LuaField("code", Required = true)]
    public string Code { get; set; } = "";

    /// <summary>Registry the code names. Inferred by looking the code up when omitted.</summary>
    [LuaField("type")]
    public ResourceKind? Type { get; set; }

    /// <summary>How many the recipe consumes.</summary>
    [LuaField("quantity", Default = "1")]
    public int Quantity { get; set; } = 1;

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

    /// <summary>Used as a tool: not consumed, loses durability instead.</summary>
    [LuaField("isTool", Default = "false")]
    public bool IsTool { get; set; }

    /// <summary>Durability removed when this is used as a tool.</summary>
    [LuaField("toolDurabilityCost", Default = "0")]
    public int ToolDurabilityCost { get; set; }
}

/// <summary>What a recipe produces.</summary>
[LuaTable("Output", Shorthand = "code")]
public sealed class OutputSpec
{
    /// <summary>
    /// Asset code of the product. May contain <c>{name}</c> placeholders naming a
    /// wildcard ingredient, which expands into one recipe per matched variant.
    /// </summary>
    [LuaField("code", Required = true)]
    public string Code { get; set; } = "";

    /// <summary>Registry the code names. Inferred by looking the code up when omitted.</summary>
    [LuaField("type")]
    public ResourceKind? Type { get; set; }

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
