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
    /// omitted, which is what most blocks do.
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
}
