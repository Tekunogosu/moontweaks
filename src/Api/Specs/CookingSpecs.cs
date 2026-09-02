namespace MoonTweaks.Api;

// What a script writes to declare a meal a pot cooks. The kind with no single
// product: what comes out is named after what went in, so a recipe carries a code of
// its own, and that code selects it.

/// <summary>
/// A cooked form one of a valid stack's inputs may also take, so a recipe accepting
/// raw meat accepts the cooked kind without listing it twice.
/// </summary>
[LuaTable("CookedStack", Shorthand = "code")]
public sealed class CookedStackSpec : StackSpec
{
    /// <summary>Asset code of the cooked form, such as <c>game:bushmeat-cooked</c>.</summary>
    [LuaField("code", Required = true)]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public override string? Code { get; set; }
}

/// <summary>
/// One thing a cooking ingredient accepts. Carries no quantity: how much of it a pot
/// needs is the ingredient's business rather than this one's.
/// </summary>
[LuaTable("CookingStack", Shorthand = "code")]
public sealed class CookingStackSpec : StackSpec
{
    /// <summary>
    /// Asset code this accepts, such as <c>game:vegetable-carrot</c>. May contain a
    /// <c>*</c> wildcard, which stays a wildcard: a pot matches against it as it
    /// cooks rather than the recipe being expanded into one per variant.
    /// </summary>
    [LuaField("code", Required = true)]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public override string? Code { get; set; }

    /// <summary>
    /// Part of the meal's shape this ingredient fills in, such as
    /// <c>bowl/vegetable base 1/*</c>. Left out, the ingredient is not drawn.
    /// </summary>
    [LuaField("shapeElement")]
    public string? ShapeElement { get; set; }

    /// <summary>
    /// Texture to draw that shape element with, written as the code the shape uses
    /// and the texture to put there. Exactly two entries.
    /// </summary>
    [LuaField("textureMapping")]
    public string[]? TextureMapping { get; set; }

    /// <summary>
    /// Cooked form this also accepts, so a recipe taking a raw ingredient takes the
    /// cooked one without a second entry.
    /// </summary>
    [LuaField("cookedStack")]
    public CookedStackSpec? CookedStack { get; set; }
}

/// <summary>
/// One thing a cooking recipe needs, and how much of it. A pot holds four slots, and
/// an ingredient says how many of them it may fill and what may go in them.
/// </summary>
[LuaTable("CookingIngredient")]
public sealed class CookingIngredientSpec
{
    /// <summary>
    /// Names this ingredient within the recipe, such as <c>vegetable-base</c>. Not an
    /// asset code: it identifies the slot rather than what goes in it, and the game
    /// reads it when it names the meal.
    /// </summary>
    [LuaField("code", Required = true)]
    public string Code { get; set; } = "";

    /// <summary>What may fill this ingredient's slots. At least one.</summary>
    [LuaField("validStacks", Required = true)]
    public CookingStackSpec[] ValidStacks { get; set; } = [];

    /// <summary>Fewest of the pot's slots this may fill. Zero makes the ingredient optional.</summary>
    [LuaField("minQuantity", Required = true)]
    public int MinQuantity { get; set; }

    /// <summary>Most of the pot's slots this may fill.</summary>
    [LuaField("maxQuantity", Required = true)]
    public int MaxQuantity { get; set; }

    /// <summary>
    /// How much of a liquid one slot of this holds, in litres. Zero for anything
    /// counted in items rather than poured.
    /// </summary>
    [LuaField("portionSizeLitres", Default = "0")]
    public double PortionSizeLitres { get; set; }

    /// <summary>
    /// What this ingredient is called when the game names the meal, such as
    /// <c>vegetable</c>. Left out, the game calls it unknown.
    /// </summary>
    [LuaField("typeName")]
    public string? TypeName { get; set; }
}

/// <summary>
/// A meal a pot cooks, named by the ingredients that went into it rather than by one
/// output: what comes out is a container of servings unless <c>cooksInto</c> says
/// otherwise.
/// </summary>
[LuaTable("CookingRecipe")]
public sealed class CookingRecipeSpec : RecipeSpec
{
    /// <summary>
    /// Identifies the recipe and names the meal it makes, such as <c>soup</c>.
    /// Unrelated to the codes that name assets, and no two recipes should share one.
    /// </summary>
    [LuaField("code", Required = true)]
    public string Code { get; set; } = "";

    /// <summary>What the pot must hold, and how much of each.</summary>
    [LuaField("ingredients", Required = true)]
    public CookingIngredientSpec[] Ingredients { get; set; } = [];

    /// <summary>
    /// Shape the meal is drawn with, as the path to it in the game's assets, such as
    /// <c>block/food/meal/soup</c>. Required: a meal with no shape cannot be drawn,
    /// and the server refuses to describe itself to a client without one.
    /// </summary>
    [LuaField("shape", Required = true)]
    public string Shape { get; set; } = "";

    /// <summary>
    /// How the meal spoils. Required for the same reason the shape is: the server
    /// writes it out for every client, and has nothing to write without it.
    /// </summary>
    [LuaField("perishableProps", Required = true)]
    public TransitionableSpec PerishableProps { get; set; } = new();

    /// <summary>
    /// Item the pot yields instead of a container of servings, as hot glue rather
    /// than a meal. Left out, the recipe makes a meal.
    /// </summary>
    [LuaField("cooksInto")]
    public ResultStackSpec? CooksInto { get; set; }

    /// <summary>Whether what comes out is eaten. Left alone for anything that is not.</summary>
    [LuaField("isFood", Default = "false")]
    public bool IsFood { get; set; }

    /// <inheritdoc/>
    public override string OutputCode => CooksInto?.Code ?? Code;
}

/// <summary>
/// Which cooking recipes to act on, by the code the recipe carries rather than by
/// anything it produces: a meal is named by its ingredients, so there is no one
/// output code to match. A bare string is taken as that code.
/// </summary>
[LuaTable("CookingSelector", Shorthand = "code")]
public sealed class CookingSelectorSpec
{
    /// <summary>
    /// Recipe code to match, such as <c>soup</c>. May contain a <c>*</c> wildcard, so
    /// <c>"*stew"</c> reaches every stew.
    /// </summary>
    [LuaField("code", Required = true)]
    public string Code { get; set; } = "";
}
