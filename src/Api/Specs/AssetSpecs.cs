using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

// What a script writes to name a thing in the world, and how much of it.

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
    [LuaField("code")]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
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
    /// Tags every match must carry, such as <c>{ "tool-axe" }</c>. Matches on what
    /// an asset is rather than what it is called, so one entry accepts a modded axe
    /// as readily as a vanilla one. Used alone, or alongside <c>code</c> to narrow
    /// a wildcard further. A bare list asks for every tag in it; the keys of a
    /// <c>TagCondition</c> ask for anything richer than that.
    /// </summary>
    [LuaField("tags")]
    public TagConditionSpec? Tags { get; set; }

    /// <summary>
    /// Arbitrary data a match must carry to satisfy this material, written as a Lua
    /// table and stored as JSON. What a key means is the game's business rather
    /// than this mod's: liquid ingredients need one, and a mod reads what it wrote.
    /// </summary>
    [LuaField("attributes")]
    public ScriptValue? Attributes { get; set; }
}

/// <summary>
/// A named asset a recipe produces. Not written by scripts directly: every shape
/// below is this plus whatever that shape says about how much of it there is.
/// </summary>
public abstract class StackSpec : AssetSpec
{
    /// <summary>
    /// Asset code of the stack. May contain <c>{name}</c> placeholders naming a
    /// wildcard ingredient, which expands into one recipe per matched variant.
    /// </summary>
    [LuaField("code", Required = true)]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public override string? Code { get; set; }

    /// <summary>
    /// Arbitrary data the stack is created carrying, written as a Lua table and
    /// stored as JSON. What a key means is the game's business rather than this
    /// mod's: liquid ingredients need one, and a mod reads what it wrote.
    /// </summary>
    [LuaField("attributes")]
    public ScriptValue? Attributes { get; set; }

    /// <summary>
    /// How many the stack holds. Not a key scripts write: a shape whose count the
    /// game works out for itself has none to offer, and answers with the one item
    /// every stack holds at least.
    /// </summary>
    public virtual int StackSize => 1;
}

/// <summary>
/// A named asset and how many of it. Not written by scripts directly: the game's own
/// recipe files give this one shape two names, and <see cref="OutputSpec"/> and
/// <see cref="ReturnedStackSpec"/> are those names, so a recipe ported from JSON
/// reads the same here as it did there.
/// </summary>
public abstract class CountedStackSpec : StackSpec
{
    /// <summary>How many the stack holds.</summary>
    [LuaField("quantity", Default = "1")]
    public int Quantity { get; set; } = 1;

    /// <inheritdoc/>
    public override int StackSize => Quantity;
}

/// <summary>What a recipe produces.</summary>
[LuaTable("Output", Shorthand = "code")]
public sealed class OutputSpec : CountedStackSpec;

/// <summary>
/// What an ingredient hands back when the recipe consumes it, such as the empty
/// bucket left by a bucket of milk. The same shape as an <c>Output</c>, under the
/// name the game's own recipe files use for it.
/// </summary>
[LuaTable("ReturnedStack", Shorthand = "code")]
public sealed class ReturnedStackSpec : CountedStackSpec;

/// <summary>
/// A stack of something to hand over or put in the world. A bare string names the
/// asset, so <c>"game:stick"</c> stands in for one of them.
/// </summary>
[LuaTable("ItemStack", Shorthand = "code")]
public sealed class ItemStackSpec : CountedStackSpec
{
    /// <summary>
    /// Asset code of the stack, such as <c>game:stick</c>. Names one asset: nothing
    /// here expands a family, so neither a wildcard nor a <c>{name}</c> placeholder
    /// belongs in it.
    /// </summary>
    [LuaField("code", Required = true)]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public override string? Code { get; set; }
}

/// <summary>
/// A stack the game turns something into: what a meal cooks into, or what food
/// becomes once it stops being fresh.
/// </summary>
[LuaTable("ResultStack", Shorthand = "code")]
public sealed class ResultStackSpec : CountedStackSpec
{
    /// <summary>
    /// Asset code of the stack, such as <c>game:rot</c>. Names one asset: nothing
    /// here expands a family, so neither a wildcard nor a <c>{name}</c> placeholder
    /// belongs in it.
    /// </summary>
    [LuaField("code", Required = true)]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public override string? Code { get; set; }
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
    public int? ToolDurabilityCost { get; set; }

    /// <summary>
    /// Taken by the craft. Setting this to <c>false</c> keeps the ingredient where it
    /// is, which is how a schematic is used: present to craft from, and not spent
    /// doing so. Says what <c>isTool</c> says without claiming the ingredient is a
    /// tool that wears, so a recipe writes one spelling or the other and not both.
    /// </summary>
    [LuaField("consume", Default = "true")]
    public bool? Consume { get; set; }

    /// <summary>
    /// Durability the craft costs this, as the negative number the game stores, so
    /// <c>-5</c> spends five points. Stands to <c>consume</c> as
    /// <c>toolDurabilityCost</c> stands to <c>isTool</c>. A positive number is
    /// refused: nothing in the game's crafting repairs an ingredient, so it would
    /// read as a repair and do nothing.
    /// </summary>
    [LuaField("durabilityChange", Default = "0")]
    public int? DurabilityChange { get; set; }

    /// <summary>
    /// Destroyed once a craft leaves it at no durability. Setting this to
    /// <c>false</c> leaves the worn-out item in the grid instead. The game's own
    /// recipe files spell this <c>break</c>, which Lua keeps as a keyword, so it
    /// carries the name the game gives the same field once loaded.
    /// </summary>
    [LuaField("breakOnZeroDurability", Default = "true")]
    public bool BreakOnZeroDurability { get; set; } = true;

    /// <summary>
    /// Handed back to the crafter once this ingredient is consumed. Nothing is
    /// handed back when omitted.
    /// </summary>
    [LuaField("returnedStack")]
    public ReturnedStackSpec? ReturnedStack { get; set; }
}
