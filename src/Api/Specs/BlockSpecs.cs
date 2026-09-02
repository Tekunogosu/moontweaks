using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

// What only a block standing in the world is: how hard it is to break, what it
// leaves behind, and what it gives off. An item is none of these things, which is
// why they are declared apart from the properties the two share.

/// <summary>
/// A colour a block gives off, written the way the game stores it: a hue and a
/// saturation naming the colour, and a brightness deciding how far it reaches.
/// </summary>
/// <remarks>
/// Each part runs from 0 to 255 and is written into a single byte, so anything
/// outside that range is refused rather than quietly wrapping round to a colour
/// nobody asked for. Brightness is what makes a block a light source at all: zero
/// gives off nothing whatever the other two say.
/// </remarks>
[LuaTable("Light")]
public sealed class LightSpec
{
    /// <summary>Which colour it is, around the wheel from red through green to blue.</summary>
    [LuaField("hue", Default = "0")]
    public int Hue { get; set; }

    /// <summary>How strong that colour is, from grey to full.</summary>
    [LuaField("saturation", Default = "0")]
    public int Saturation { get; set; }

    /// <summary>How far the light reaches, in blocks. Zero gives off no light at all.</summary>
    [LuaField("brightness", Required = true)]
    public int Brightness { get; set; }
}

/// <summary>
/// One thing a block leaves behind when it is broken. A block may list several, and
/// each is rolled for on its own.
/// </summary>
/// <remarks>
/// Writing any drops replaces every drop the block had, which is the only honest
/// spelling: a list has no key to merge on, so a script adding to one would have to
/// say which of the old entries it meant to keep.
/// </remarks>
[LuaTable("BlockDrop", Shorthand = "code")]
public sealed class BlockDropSpec : AssetSpec
{
    /// <summary>Asset code of what is dropped, such as <c>game:stick</c>.</summary>
    [LuaField("code", Required = true)]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public override string? Code { get; set; }

    /// <summary>
    /// How many are dropped, which varies within a range. One every time when
    /// omitted; an average below one is how a chance of nothing is spelled.
    /// </summary>
    [LuaField("quantity")]
    public SpreadSpec? Quantity { get; set; }

    /// <summary>
    /// Tool this only drops for, such as <c>axe</c>. Dropped whatever broke it when
    /// omitted.
    /// </summary>
    [LuaField("tool")]
    public EnumToolKind? Tool { get; set; }

    /// <summary>
    /// Stops the roll here when this one drops, so nothing later in the list is
    /// rolled for. This is how one block offers alternatives rather than a handful.
    /// </summary>
    [LuaField("lastDrop", Default = "false")]
    public bool LastDrop { get; set; }

    /// <summary>
    /// Arbitrary data the dropped stack carries, written as a Lua table and stored
    /// as JSON.
    /// </summary>
    [LuaField("attributes")]
    public ScriptValue? Attributes { get; set; }
}

/// <summary>
/// One sound a block makes, named by the asset that holds it. A bare string names
/// the sound and leaves everything else as the game had it. How loudly and how far a
/// sound carries is filled in per kind of sound as the game loads.
/// </summary>
/// <remarks>
/// A sound is an asset rather than an item or a block, so nothing completes the path
/// and nothing checks it: a path the server has no sound for is silence, which is
/// what the game does with one of its own. The <c>sounds/</c> the assets sit under is
/// added where it is missing, exactly as the game adds it, so
/// <c>survival:block/planks</c> and <c>survival:sounds/block/planks</c> name the same
/// sound.
/// </remarks>
[LuaTable("BlockSound", Shorthand = "path")]
public sealed class BlockSoundSpec
{
    /// <summary>Asset path of the sound, such as <c>survival:block/planks</c>.</summary>
    [LuaField("path", Required = true)]
    public string Path { get; set; } = "";

    /// <summary>How far away it can be heard, in blocks.</summary>
    [LuaField("range")]
    public double? Range { get; set; }

    /// <summary>Which volume control it plays under.</summary>
    [LuaField("type")]
    public EnumSoundKind? Type { get; set; }

    /// <summary>How its pitch varies each time it plays. One every time when omitted.</summary>
    [LuaField("pitch")]
    public SpreadSpec? Pitch { get; set; }

    /// <summary>How its volume varies each time it plays. One every time when omitted.</summary>
    [LuaField("volume")]
    public SpreadSpec? Volume { get; set; }
}

/// <summary>
/// What a block sounds like. Only the sounds a script names change, so a block may be
/// given a new breaking sound without restating what it sounds like underfoot.
/// </summary>
[LuaTable("BlockSounds")]
public sealed class BlockSoundsSpec
{
    /// <summary>Walked over.</summary>
    [LuaField("walk")]
    public BlockSoundSpec? Walk { get; set; }

    /// <summary>Stood inside, as tall grass is.</summary>
    [LuaField("inside")]
    public BlockSoundSpec? Inside { get; set; }

    /// <summary>
    /// Broken. The game calls this field <c>break</c>, which Lua keeps as a keyword,
    /// so it carries the name the action goes by instead.
    /// </summary>
    [LuaField("breaking")]
    public BlockSoundSpec? Breaking { get; set; }

    /// <summary>Placed.</summary>
    [LuaField("place")]
    public BlockSoundSpec? Place { get; set; }

    /// <summary>Struck while being mined, which repeats until it breaks.</summary>
    [LuaField("hit")]
    public BlockSoundSpec? Hit { get; set; }

    /// <summary>
    /// Given off continuously while it stands there, as a beehive hums. Distinct from
    /// the rest in being a place's sound rather than an action's.
    /// </summary>
    [LuaField("ambient")]
    public BlockSoundSpec? Ambient { get; set; }

    /// <summary>
    /// How many of the block have to stand together before the ambient sound is
    /// played at full volume.
    /// </summary>
    [LuaField("ambientBlockCount")]
    public double? AmbientBlockCount { get; set; }
}

/// <summary>
/// A box measured within the block it belongs to, where 0 is one face and 1 is the
/// opposite one. A full cube runs 0 to 1 in all three directions; a slab standing on
/// the floor is <c>y2 = 0.5</c>.
/// </summary>
[LuaTable("Box")]
public sealed class BoxSpec
{
    /// <summary>Where the box starts, east to west.</summary>
    [LuaField("x1", Default = "0")]
    public double X1 { get; set; }

    /// <summary>Where the box starts, from the block's floor upwards.</summary>
    [LuaField("y1", Default = "0")]
    public double Y1 { get; set; }

    /// <summary>Where the box starts, north to south.</summary>
    [LuaField("z1", Default = "0")]
    public double Z1 { get; set; }

    /// <summary>Where the box ends, east to west.</summary>
    [LuaField("x2", Default = "1")]
    public double X2 { get; set; } = 1;

    /// <summary>Where the box ends, from the block's floor upwards.</summary>
    [LuaField("y2", Default = "1")]
    public double Y2 { get; set; } = 1;

    /// <summary>Where the box ends, north to south.</summary>
    [LuaField("z2", Default = "1")]
    public double Z2 { get; set; } = 1;
}

/// <summary>
/// How a crop grows on farmland. Only the keys a script names change, so a crop may
/// be made to take longer without restating what it feeds on.
/// </summary>
[LuaTable("CropProperties")]
public sealed class CropSpec
{
    /// <summary>Which soil nutrient it feeds on.</summary>
    [LuaField("requiredNutrient")]
    public EnumNutrientKind? RequiredNutrient { get; set; }

    /// <summary>How much of that nutrient it takes from the soil over its whole life.</summary>
    [LuaField("nutrientConsumption")]
    public double? NutrientConsumption { get; set; }

    /// <summary>How many stages it goes through before it is ripe.</summary>
    [LuaField("growthStages")]
    public int? GrowthStages { get; set; }

    /// <summary>How many in-game days it takes to ripen, on soil that suits it.</summary>
    [LuaField("totalGrowthDays")]
    public double? TotalGrowthDays { get; set; }

    /// <summary>
    /// How many in-game months it takes to ripen, which the game reads in place of
    /// the days where a crop names one.
    /// </summary>
    [LuaField("totalGrowthMonths")]
    public double? TotalGrowthMonths { get; set; }

    /// <summary>Whether it may be harvested more than once rather than being pulled up.</summary>
    [LuaField("multipleHarvests")]
    public bool? MultipleHarvests { get; set; }

    /// <summary>How many stages it falls back by when harvested, where it may be harvested again.</summary>
    [LuaField("harvestGrowthStageLoss")]
    public int? HarvestGrowthStageLoss { get; set; }

    /// <summary>The temperature below which the cold begins to hurt it.</summary>
    [LuaField("coldDamageBelow")]
    public double? ColdDamageBelow { get; set; }

    /// <summary>The temperature above which the heat begins to hurt it.</summary>
    [LuaField("heatDamageAbove")]
    public double? HeatDamageAbove { get; set; }

    /// <summary>How much damage slows its growth, as a multiplier.</summary>
    [LuaField("damageGrowthStuntMul")]
    public double? DamageGrowthStuntMul { get; set; }

    /// <summary>How much harder the cold is on it once it is ripe, as a multiplier.</summary>
    [LuaField("coldDamageRipeMul")]
    public double? ColdDamageRipeMul { get; set; }
}

/// <summary>
/// Properties of a block, changed on whatever a code matches. Everything an item
/// carries is here too, since a block is an item as far as a hand holding one is
/// concerned; the keys below it are the ones only a block standing in the world has.
/// </summary>
/// <remarks>
/// These reach players as well as the server, in the same registry packet the item
/// properties travel in, so a retuned hardness is what a client's own break timer
/// counts down.
/// </remarks>
[LuaTable("BlockProperties")]
public sealed class BlockPropertiesSpec : AssetPropertiesSpec
{
    /// <summary>
    /// How long it takes to break, before any tool is taken into account. The game's
    /// stone sits around 3 and its soil around 1, so halving this halves the time.
    /// </summary>
    [LuaField("resistance")]
    public double? Resistance { get; set; }

    /// <summary>
    /// Which tier of tool it takes to break this at all. A tool below it still
    /// breaks the block, but drops nothing for the trouble.
    /// </summary>
    [LuaField("requiredMiningTier")]
    public int? RequiredMiningTier { get; set; }

    /// <summary>
    /// What it is made of, which decides which of a tool's <c>miningSpeed</c> entries
    /// applies to it and what it sounds like underfoot.
    /// </summary>
    [LuaField("blockMaterial")]
    public EnumBlockKind? BlockMaterial { get; set; }

    /// <summary>
    /// What it leaves behind when broken. Replaces every drop it had, so a list
    /// says exactly what comes out of it; an empty list makes it drop nothing.
    /// </summary>
    [LuaField("drops")]
    public BlockDropSpec[]? Drops { get; set; }

    /// <summary>
    /// The colour it gives off. A brightness of zero puts it out, which is how a
    /// light source is turned back into an ordinary block.
    /// </summary>
    [LuaField("light")]
    public LightSpec? Light { get; set; }

    /// <summary>
    /// How much light it stops passing through it, from 0 for glass to the game's
    /// maximum for solid rock.
    /// </summary>
    [LuaField("lightAbsorption")]
    public int? LightAbsorption { get; set; }

    /// <summary>
    /// How readily something else may be built over it. Grass and snow sit high, so
    /// a placed block simply replaces them; anything solid sits at zero.
    /// </summary>
    [LuaField("replaceable")]
    public int? Replaceable { get; set; }

    /// <summary>How well crops grow on it. Zero is barren.</summary>
    [LuaField("fertility")]
    public int? Fertility { get; set; }

    /// <summary>
    /// How fast somebody walks over it, as a multiplier. One is ordinary ground and
    /// anything below it slows them down, as deep snow does.
    /// </summary>
    [LuaField("walkSpeedMultiplier")]
    public double? WalkSpeedMultiplier { get; set; }

    /// <summary>
    /// How much it slows something moving through it, as a multiplier. This is what
    /// makes water heavy going.
    /// </summary>
    [LuaField("dragMultiplier")]
    public double? DragMultiplier { get; set; }

    /// <summary>Whether somebody may climb it, as they climb a ladder.</summary>
    [LuaField("climbable")]
    public bool? Climbable { get; set; }

    /// <summary>Whether rain falls through it rather than being stopped by it.</summary>
    [LuaField("rainPermeable")]
    public bool? RainPermeable { get; set; }

    /// <summary>What it sounds like to walk on, break, place and stand inside.</summary>
    [LuaField("sounds")]
    public BlockSoundsSpec? Sounds { get; set; }

    /// <summary>
    /// The boxes something walking into it is stopped by. Replaces every box it had,
    /// so a list says exactly what shape it is; an empty list makes it walk-through.
    /// </summary>
    [LuaField("collisionBoxes")]
    public BoxSpec[]? CollisionBoxes { get; set; }

    /// <summary>
    /// The boxes a player's cursor picks it out by, and what draws the outline around
    /// it. Replaces every box it had; an empty list makes it unselectable.
    /// </summary>
    [LuaField("selectionBoxes")]
    public BoxSpec[]? SelectionBoxes { get; set; }

    /// <summary>
    /// How it grows as a crop. Only meaningful on a block the game already farms:
    /// this changes how a crop behaves and does not turn a block into one.
    /// </summary>
    [LuaField("cropProps")]
    public CropSpec? CropProps { get; set; }
}
