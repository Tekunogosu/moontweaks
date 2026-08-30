namespace MoonTweaks.Api;

// What a script writes to declare a recipe a barrel mixes or seals. Kept apart from
// the kinds worked by hand: a barrel has no grid and no surface, so it lists its
// ingredients rather than arranging them, and measures them in litres as well as in
// items.

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
public sealed class BarrelOutputSpec : CountedStackSpec
{
    /// <summary>How much of a liquid the recipe yields. Zero for anything counted in items.</summary>
    [LuaField("litres", Default = "0")]
    public double Litres { get; set; }
}

/// <summary>
/// A barrel recipe, either mixed on the spot or left to seal for a while.
/// </summary>
[LuaTable("BarrelRecipe")]
public sealed class BarrelRecipeSpec : CraftingRecipeSpec
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
