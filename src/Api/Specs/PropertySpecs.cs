using System.Collections.Generic;
using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

// What an item or block both are, rather than what is made from them. The
// properties only a block standing in the world has are in BlockSpecs.cs beside
// this, split the same way the two appliers that read them are.

/// <summary>How much of something a step yields, which varies within a range.</summary>
[LuaTable("Spread")]
public sealed class SpreadSpec
{
    /// <summary>The middle of the range.</summary>
    [LuaField("avg", Required = true)]
    public double Average { get; set; }

    /// <summary>How far either side of the middle it reaches. Zero yields the average every time.</summary>
    [LuaField("var", Default = "0")]
    public double Variance { get; set; }

    /// <summary>
    /// How the range picks within itself. Left alone, anywhere in it is as likely as
    /// anywhere else, as a bare average and variance means in the game's own recipe
    /// files.
    /// </summary>
    [LuaField("dist", Default = "\"uniform\"")]
    public EnumSpreadKind Distribution { get; set; } = EnumSpreadKind.Uniform;
}

/// <summary>
/// How something changes once it stops being fresh: what it becomes, how long it
/// keeps, and how long the change itself takes.
/// </summary>
[LuaTable("TransitionableProperties")]
public sealed class TransitionableSpec
{
    /// <summary>
    /// What kind of change it is. Left out, it is the same nothing-in-particular the
    /// game's own meal recipes leave it as.
    /// </summary>
    [LuaField("type", Default = "\"none\"")]
    public EnumTransitionKind Type { get; set; } = EnumTransitionKind.None;

    /// <summary>How long it stays fresh, in in-game hours.</summary>
    [LuaField("freshHours", Required = true)]
    public SpreadSpec FreshHours { get; set; } = new();

    /// <summary>How long the change itself takes once freshness runs out, in in-game hours.</summary>
    [LuaField("transitionHours", Required = true)]
    public SpreadSpec TransitionHours { get; set; } = new();

    /// <summary>What it becomes.</summary>
    [LuaField("transitionedStack", Required = true)]
    public ResultStackSpec TransitionedStack { get; set; } = new();

    /// <summary>How many of it one of these becomes. One for one when left alone.</summary>
    [LuaField("transitionRatio", Default = "1")]
    public double TransitionRatio { get; set; } = 1;
}

/// <summary>
/// What burning or smelting this does. Only the keys a script writes change, so a
/// script may raise a melting point without restating everything else about the fire.
/// </summary>
[LuaTable("CombustibleProperties")]
public sealed class CombustibleSpec
{
    /// <summary>How hot it burns, in degrees. Zero if it does not burn.</summary>
    [LuaField("burnTemperature")]
    public int? BurnTemperature { get; set; }

    /// <summary>How long it burns for, in seconds.</summary>
    [LuaField("burnDuration")]
    public double? BurnDuration { get; set; }

    /// <summary>How much smoke it gives off while burning.</summary>
    [LuaField("smokeLevel")]
    public double? SmokeLevel { get; set; }

    /// <summary>How hot it must get before it melts or cooks.</summary>
    [LuaField("meltingPoint")]
    public int? MeltingPoint { get; set; }

    /// <summary>How long it takes to melt or cook once hot enough.</summary>
    [LuaField("meltingDuration")]
    public double? MeltingDuration { get; set; }

    /// <summary>How hot it can get before it is ruined.</summary>
    [LuaField("maxTemperature")]
    public int? MaxTemperature { get; set; }

    /// <summary>How well it resists heat reaching it.</summary>
    [LuaField("heatResistance")]
    public int? HeatResistance { get; set; }

    /// <summary>How many of it one unit of the smelted result takes.</summary>
    [LuaField("smeltedRatio")]
    public int? SmeltedRatio { get; set; }

    /// <summary>Whether it needs a crucible or other vessel rather than sitting in the fire.</summary>
    [LuaField("requiresContainer")]
    public bool? RequiresContainer { get; set; }

    /// <summary>Which kind of heating this is, which decides where it may be done.</summary>
    [LuaField("smeltingType")]
    public EnumSmeltKind? SmeltingType { get; set; }

    /// <summary>What it becomes once melted or cooked.</summary>
    [LuaField("smeltedStack")]
    public OutputSpec? SmeltedStack { get; set; }
}

/// <summary>What eating this does.</summary>
[LuaTable("NutritionProperties")]
public sealed class NutritionSpec
{
    /// <summary>Which part of a diet it counts towards.</summary>
    [LuaField("foodCategory")]
    public EnumFoodKind? FoodCategory { get; set; }

    /// <summary>
    /// How much hunger it settles. The game calls this satiety on a food and
    /// saturation on a player; it is one quantity, and is satiety throughout here.
    /// </summary>
    [LuaField("satiety")]
    public double? Satiety { get; set; }

    /// <summary>How much health eating it restores, or costs when negative.</summary>
    [LuaField("health")]
    public double? Health { get; set; }

    /// <summary>How long before that satiety begins to fall.</summary>
    [LuaField("satietyLossDelay")]
    public double? SatietyLossDelay { get; set; }

    /// <summary>How strongly it intoxicates.</summary>
    [LuaField("intoxication")]
    public double? Intoxication { get; set; }

    /// <summary>What is left in hand once it is eaten, such as an empty bowl.</summary>
    [LuaField("eatenStack")]
    public OutputSpec? EatenStack { get; set; }
}

/// <summary>What grinding this in a quern yields.</summary>
[LuaTable("GrindingProperties")]
public sealed class GrindingSpec
{
    /// <summary>What it grinds down into.</summary>
    [LuaField("groundStack", Required = true)]
    public OutputSpec GroundStack { get; set; } = new();
}

/// <summary>What crushing this in a pulveriser yields.</summary>
[LuaTable("CrushingProperties")]
public sealed class CrushingSpec
{
    /// <summary>What it crushes down into.</summary>
    [LuaField("crushedStack", Required = true)]
    public OutputSpec CrushedStack { get; set; } = new();

    /// <summary>How hard a pulveriser cap it takes to crush this.</summary>
    [LuaField("hardnessTier")]
    public int? HardnessTier { get; set; }

    /// <summary>How much it yields, which varies within a range.</summary>
    [LuaField("quantity")]
    public SpreadSpec? Quantity { get; set; }
}

/// <summary>
/// Where something appears in the creative inventory, and as what. A tab it is listed
/// under is enough for most things; the stacks are for an asset that should appear
/// there more than once, each carrying different data.
/// </summary>
[LuaTable("CreativeStacks")]
public sealed class CreativeStacksSpec
{
    /// <summary>Tabs these stacks are listed under, such as <c>general</c> or <c>items</c>.</summary>
    [LuaField("tabs", Required = true)]
    public string[] Tabs { get; set; } = [];

    /// <summary>
    /// The stacks themselves, each of which appears as its own entry. A bare code
    /// names one of something; attributes are how two entries for one asset differ.
    /// </summary>
    [LuaField("stacks", Required = true)]
    public ItemStackSpec[] Stacks { get; set; } = [];
}

/// <summary>
/// Properties of an item or a block, changed on whatever a code matches. Every key
/// is optional and only the ones a script writes are changed, so a script says what
/// it means to alter and nothing else moves.
/// </summary>
[LuaTable("AssetProperties")]
public class AssetPropertiesSpec
{
    /// <summary>
    /// Code of what to change. May contain a <c>*</c> wildcard, so
    /// <c>"game:axe-*"</c> changes the whole family at once. Required unless
    /// <c>tags</c> names what to change instead.
    /// </summary>
    [LuaField("code")]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public string? Code { get; set; }

    /// <summary>
    /// Tags everything changed must carry, such as <c>{ "tool-axe" }</c>. Matches on
    /// what an asset is rather than what it is called, so one entry reaches a modded
    /// axe as readily as a vanilla one. Used alone, or alongside <c>code</c> to
    /// narrow a wildcard further. A bare list asks for every tag in it; the keys of a
    /// <c>TagCondition</c> ask for anything richer than that.
    /// </summary>
    [LuaField("tags")]
    public TagConditionSpec? Tags { get; set; }

    /// <summary>
    /// Tags to put on everything changed, on top of whatever it already carries. Each
    /// has to be a name the server knows: one of the game's own, or one
    /// <c>moontweaks.tags.add</c> declared earlier in this run. A name already carried
    /// is not carried twice.
    /// </summary>
    [LuaField("addTags")]
    public string[]? AddTags { get; set; }

    /// <summary>
    /// Tags to put on everything changed, in place of whatever it already carries.
    /// Read <c>addTags</c> for the usual case: replacing takes away the tags the game
    /// gave an asset, and those are what the game's own recipes select it by.
    /// </summary>
    [LuaField("setTags")]
    public string[]? SetTags { get; set; }

    /// <summary>How much use it takes before breaking. Only meaningful on something that wears out.</summary>
    [LuaField("durability")]
    public int? Durability { get; set; }

    /// <summary>How many fit in one slot.</summary>
    [LuaField("maxStackSize")]
    public int? MaxStackSize { get; set; }

    /// <summary>Damage it deals when swung.</summary>
    [LuaField("attackPower")]
    public double? AttackPower { get; set; }

    /// <summary>How far that swing reaches, in blocks.</summary>
    [LuaField("attackRange")]
    public double? AttackRange { get; set; }

    /// <summary>Which tier of block it is hard enough to break.</summary>
    [LuaField("toolTier")]
    public int? ToolTier { get; set; }

    /// <summary>
    /// Which class of tool this counts as, such as <c>axe</c>. Separate from
    /// <c>toolTier</c>, which says how hard a block it may break rather than what
    /// kind of tool it is: a recipe asking for an axe and a block dropping only for
    /// an axe both read this.
    /// </summary>
    [LuaField("tool")]
    public EnumToolKind? Tool { get; set; }

    /// <summary>How heavy it is, which decides what it sinks through and how it is thrown.</summary>
    [LuaField("materialDensity")]
    public int? MaterialDensity { get; set; }

    /// <summary>
    /// How fast it breaks each kind of block, keyed by material such as <c>wood</c> or
    /// <c>stone</c>. Replaces the whole table, so it mines quickly exactly the
    /// materials listed here.
    /// </summary>
    [LuaField("miningSpeed")]
    public Dictionary<EnumBlockKind, double>? MiningSpeed { get; set; }

    /// <summary>
    /// Which inventories it may be put in, such as <c>backpack</c> or <c>offhand</c>.
    /// Replaces the whole set, so it fits exactly the ones listed here.
    /// </summary>
    [LuaField("storageFlags")]
    public EnumStorageKind[]? StorageFlags { get; set; }

    /// <summary>
    /// What wears it down, such as <c>attacking</c> or <c>blockbreaking</c>. Replaces
    /// the whole set, so exactly the ones listed here damage it.
    /// </summary>
    [LuaField("damagedBy")]
    public EnumDamageKind[]? DamagedBy { get; set; }

    /// <summary>
    /// Arbitrary data the game reads off this item or block, written as a Lua table
    /// and stored as JSON, merged into whatever it already carries. A key named here
    /// replaces the value under that key; a key left out keeps the value the game
    /// gave it. Tables merge the same way at every depth, so naming one key inside
    /// <c>handbook</c> moves that key and nothing else beside it. A list replaces the
    /// list under its key whole.
    /// </summary>
    [LuaField("attributes")]
    public ScriptValue? Attributes { get; set; }

    /// <summary>
    /// Arbitrary data the game reads off this item or block, in place of whatever it
    /// already carries. Read <c>attributes</c> for the usual case: replacing takes
    /// away every key the game gave an asset, and this is the only way to take one
    /// away, since a Lua table cannot hold a nil.
    /// </summary>
    [LuaField("setAttributes")]
    public ScriptValue? SetAttributes { get; set; }

    /// <summary>What burning or smelting it does.</summary>
    [LuaField("combustible")]
    public CombustibleSpec? Combustible { get; set; }

    /// <summary>What eating it does.</summary>
    [LuaField("nutrition")]
    public NutritionSpec? Nutrition { get; set; }

    /// <summary>What grinding it yields.</summary>
    [LuaField("grinding")]
    public GrindingSpec? Grinding { get; set; }

    /// <summary>What crushing it yields.</summary>
    [LuaField("crushing")]
    public CrushingSpec? Crushing { get; set; }

    /// <summary>
    /// How it changes once it stops being fresh. A list, because one thing may change
    /// in more than one way — meat both rots and cures, and which happens depends on
    /// where it is kept. Replaces every change it had, since a list has no key to
    /// merge on.
    /// </summary>
    [LuaField("transitionableProps")]
    public TransitionableSpec[]? TransitionableProps { get; set; }

    /// <summary>
    /// Tabs it is listed under in the creative inventory, such as <c>general</c>.
    /// Replaces the whole set; an empty list takes it out of creative altogether.
    /// </summary>
    [LuaField("creativeInventoryTabs")]
    public string[]? CreativeInventoryTabs { get; set; }

    /// <summary>
    /// The creative entries it appears as, where being listed under a tab is not
    /// enough — a barrel of each liquid, say, rather than one empty barrel. Replaces
    /// whatever it had.
    /// </summary>
    [LuaField("creativeInventoryStacks")]
    public CreativeStacksSpec[]? CreativeInventoryStacks { get; set; }
}
