namespace MoonTweaks.Api;

// What a script writes to declare an alloy. The kind that is not a recipe at all:
// the game holds it as a list of metals and the share each must make up, so nothing
// here carries a pattern, a quantity or a name.

/// <summary>
/// One metal an alloy is mixed from, and the share of the mix it must make up.
/// </summary>
/// <remarks>
/// A crucible matches an ingredient by the exact stack its code resolves to, so
/// neither a wildcard nor a tag reaches one, and neither a quantity nor any
/// attributes narrow it: the shares alone decide what mixes.
/// </remarks>
[LuaTable("AlloyIngredient")]
public sealed class AlloyIngredientSpec : AssetSpec
{
    /// <summary>Asset code of the metal, such as <c>game:ingot-copper</c>.</summary>
    [LuaField("code", Required = true)]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public override string? Code { get; set; }

    /// <summary>
    /// Least of the mix this metal may be, as a fraction of one. Measured against
    /// what the crucible holds after every ore has been counted as the metal it
    /// smelts into, so ore and ingot of the same metal count towards one share.
    /// </summary>
    [LuaField("minRatio", Required = true)]
    public double MinRatio { get; set; }

    /// <summary>Most of the mix this metal may be, as a fraction of one.</summary>
    [LuaField("maxRatio", Required = true)]
    public double MaxRatio { get; set; }
}

/// <summary>
/// The metal an alloy yields. Carries no quantity: a crucible pours as much as went
/// into it, so how much comes out is decided by the mix rather than by the recipe.
/// </summary>
[LuaTable("AlloyOutput", Shorthand = "code")]
public sealed class AlloyOutputSpec : StackSpec
{
    /// <summary>
    /// Asset code of the metal poured, such as <c>game:ingot-brass</c>. Names one
    /// metal: an alloy has no variants to expand into, so neither a wildcard nor a
    /// <c>{name}</c> placeholder belongs here.
    /// </summary>
    [LuaField("code", Required = true)]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public override string? Code { get; set; }
}

/// <summary>
/// An alloy a crucible smelts, named by the share each metal makes up rather than
/// by any arrangement of them.
/// </summary>
[LuaTable("AlloyRecipe")]
public sealed class AlloyRecipeSpec : RecipeSpec
{
    /// <summary>
    /// The metals the mix is made of, each with the share of it that it must be.
    /// Every one of them must be present for the alloy to smelt.
    /// </summary>
    [LuaField("ingredients", Required = true)]
    public AlloyIngredientSpec[] Ingredients { get; set; } = [];

    /// <summary>What the mix smelts into.</summary>
    [LuaField("output", Required = true)]
    public AlloyOutputSpec Output { get; set; } = new();

    /// <inheritdoc/>
    public override string OutputCode => Output.Code!;
}
