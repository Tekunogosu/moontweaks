namespace MoonTweaks.Api;

// What a script writes to reach into the world, and what it is told back.

/// <summary>
/// How fast something is moving and which way. Left out entirely, a dropped stack
/// simply falls where it was put.
/// </summary>
/// <remarks>
/// Measured per physics step rather than per second, so the numbers are far smaller
/// than the distance travelled suggests. The game throws a stack a player drops at
/// roughly <c>0.1</c>, which lands it a step away; <c>1</c> is a hard fling and
/// anything much beyond that leaves the map. Multiply what
/// <c>moontweaks.players.facing</c> gives by the speed wanted rather than guessing
/// at each part separately.
/// </remarks>
[LuaTable("Velocity")]
public sealed class VelocitySpec
{
    /// <summary>East for a positive number, west for a negative one.</summary>
    [LuaField("x", Default = "0")]
    public double X { get; set; }

    /// <summary>Upwards for a positive number. A little of this makes a stack arc rather than skid.</summary>
    [LuaField("y", Default = "0")]
    public double Y { get; set; }

    /// <summary>South for a positive number, north for a negative one.</summary>
    [LuaField("z", Default = "0")]
    public double Z { get; set; }
}

/// <summary>A stack put into the world as a thing lying on the ground.</summary>
[LuaTable("Drop")]
public sealed class DropSpec
{
    /// <summary>What to drop.</summary>
    [LuaField("stack", Required = true)]
    public ItemStackSpec Stack { get; set; } = new();

    /// <summary>Where to drop it, east to west.</summary>
    [LuaField("x", Required = true)]
    public double X { get; set; }

    /// <summary>Where to drop it, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public double Y { get; set; }

    /// <summary>Where to drop it, north to south.</summary>
    [LuaField("z", Required = true)]
    public double Z { get; set; }

    /// <summary>
    /// How fast to throw it, and which way. Left out, it appears where it was put and
    /// falls straight down, which lands it close enough to walk back into.
    /// </summary>
    [LuaField("velocity")]
    public VelocitySpec? Velocity { get; set; }

    /// <summary>
    /// Player this came from, who then cannot pick it up for a second. This is what
    /// stops a stack dropped at somebody's feet going straight back into their hands,
    /// and it is what the game does when a player throws something down. Left out,
    /// anybody standing there collects it at once.
    /// </summary>
    [LuaField("owner")]
    public string? Owner { get; set; }
}

/// <summary>
/// A point or a direction, as three numbers. Whether it is somewhere or which way
/// depends on what handed it over, and both read the same.
/// </summary>
/// <param name="x">East for a positive number, west for a negative one.</param>
/// <param name="y">Upwards for a positive number.</param>
/// <param name="z">South for a positive number, north for a negative one.</param>
[LuaTable("Vector", Given = true)]
public sealed class VectorPayload(double x, double y, double z)
{
    /// <summary>East to west.</summary>
    [LuaField("x")]
    public double X { get; } = x;

    /// <summary>Up and down.</summary>
    [LuaField("y")]
    public double Y { get; } = y;

    /// <summary>North to south.</summary>
    [LuaField("z")]
    public double Z { get; } = z;
}

/// <summary>What the weather and the ground are like somewhere.</summary>
/// <remarks>
/// These are the numbers worldgen decided a place by and the weather moves through,
/// rather than anything a player sees directly. They are what a script reads to make
/// something depend on where it happened: a recipe that only works somewhere warm, a
/// spawn that only happens in a forest.
/// </remarks>
[LuaTable("Climate", Given = true)]
public sealed class ClimatePayload
{
    /// <summary>How warm it is right now, in degrees.</summary>
    [LuaField("temperature")]
    public double Temperature { get; init; }

    /// <summary>How wet it is right now, from 0 to 1.</summary>
    [LuaField("rainfall")]
    public double Rainfall { get; init; }

    /// <summary>
    /// The temperature this place was generated for, which the seasons swing either
    /// side of. Read this rather than <c>temperature</c> to ask what somewhere is
    /// like rather than what it is like today.
    /// </summary>
    [LuaField("worldgenTemperature")]
    public double WorldgenTemperature { get; init; }

    /// <summary>How wet this place is generally, rather than at this moment.</summary>
    [LuaField("worldgenRainfall")]
    public double WorldgenRainfall { get; init; }

    /// <summary>How well things grow here, from 0 to 1.</summary>
    [LuaField("fertility")]
    public double Fertility { get; init; }

    /// <summary>How thickly trees stand here, from 0 to 1.</summary>
    [LuaField("forestDensity")]
    public double ForestDensity { get; init; }

    /// <summary>How thickly bushes stand here, from 0 to 1.</summary>
    [LuaField("shrubDensity")]
    public double ShrubDensity { get; init; }

    /// <summary>How restless the rock is here, which decides where hot springs and ore sit.</summary>
    [LuaField("geologicActivity")]
    public double GeologicActivity { get; init; }
}

/// <summary>One block position.</summary>
[LuaTable("Point")]
public sealed class PointSpec
{
    /// <summary>Where, east to west.</summary>
    [LuaField("x", Required = true)]
    public int X { get; set; }

    /// <summary>Where, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public int Y { get; set; }

    /// <summary>Where, north to south.</summary>
    [LuaField("z", Required = true)]
    public int Z { get; set; }
}

/// <summary>A block broken as a player would break it, rather than simply removed.</summary>
/// <remarks>
/// The difference from <c>setBlock</c> to air is everything the game does around the
/// removal: the block's drops land, its breaking sound plays, and whatever it was
/// standing on is told to check itself. A script clearing ground wants <c>setBlock</c>;
/// a script harvesting something wants this.
/// </remarks>
[LuaTable("Break")]
public sealed class BreakSpec
{
    /// <summary>Which block to break, east to west.</summary>
    [LuaField("x", Required = true)]
    public int X { get; set; }

    /// <summary>Which block to break, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public int Y { get; set; }

    /// <summary>Which block to break, north to south.</summary>
    [LuaField("z", Required = true)]
    public int Z { get; set; }

    /// <summary>
    /// Player credited with breaking it, whose tool and privileges the game then
    /// takes into account. Broken by nobody in particular when omitted, which drops
    /// whatever the block gives to a bare hand.
    /// </summary>
    [LuaField("player")]
    public string? Player { get; set; }

    /// <summary>
    /// Multiplies how much it drops. Two gives twice the usual and zero gives
    /// nothing, which is how a block is broken properly without paying it out.
    /// </summary>
    [LuaField("dropMultiplier", Default = "1")]
    public double DropMultiplier { get; set; } = 1;
}

/// <summary>A colour, as the three parts a screen mixes it from.</summary>
[LuaTable("Colour")]
public sealed class ColourSpec
{
    /// <summary>How much red, from 0 to 255.</summary>
    [LuaField("red", Default = "255")]
    public int Red { get; set; } = 255;

    /// <summary>How much green, from 0 to 255.</summary>
    [LuaField("green", Default = "255")]
    public int Green { get; set; } = 255;

    /// <summary>How much blue, from 0 to 255.</summary>
    [LuaField("blue", Default = "255")]
    public int Blue { get; set; } = 255;

    /// <summary>How solid it is, from invisible at 0 to opaque at 255.</summary>
    [LuaField("alpha", Default = "128")]
    public int Alpha { get; set; } = 128;
}

/// <summary>Blocks drawn to one player as a coloured outline.</summary>
/// <remarks>
/// The one thing a server-side script can draw on somebody's screen. The client needs
/// nothing installed: highlighting is a facility the game already ships for its own
/// area-selection tools, and the server simply tells a client which boxes to draw.
///
/// Slots are what let two of these coexist. A slot holds one set of blocks until it
/// is given another, so a script that highlights under its own slot number can replace
/// or clear its own drawing without disturbing anybody else's.
/// </remarks>
[LuaTable("Highlight")]
public sealed class HighlightSpec
{
    /// <summary>Player to draw them for. Nobody else sees them.</summary>
    [LuaField("player", Required = true)]
    public string Player { get; set; } = "";

    /// <summary>
    /// Which set this is, so one script's highlights replace their own rather than
    /// each other's. Any number will do as long as it is used consistently.
    /// </summary>
    [LuaField("slot", Default = "0")]
    public int Slot { get; set; }

    /// <summary>
    /// Which blocks to outline. An empty list clears whatever this slot was drawing,
    /// which is how a highlight is taken back.
    /// </summary>
    [LuaField("blocks", Required = true)]
    public PointSpec[] Blocks { get; set; } = [];

    /// <summary>What colour to draw them. A translucent white when omitted.</summary>
    [LuaField("colour")]
    public ColourSpec? Colour { get; set; }
}

/// <summary>
/// A box of blocks to look through, given as two opposite corners. Either corner may
/// be the lower one: the box is worked out from both rather than assumed.
/// </summary>
/// <remarks>
/// A search runs inside the game rather than in Lua. Walking the same box with
/// <c>blockAt</c> costs a crossing into a binding for every block; this costs one,
/// however many blocks the box holds.
/// </remarks>
[LuaTable("Region")]
public sealed class RegionSpec
{
    /// <summary>One corner, east to west.</summary>
    [LuaField("x", Required = true)]
    public int X { get; set; }

    /// <summary>One corner, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public int Y { get; set; }

    /// <summary>One corner, north to south.</summary>
    [LuaField("z", Required = true)]
    public int Z { get; set; }

    /// <summary>The opposite corner, east to west.</summary>
    [LuaField("toX", Required = true)]
    public int ToX { get; set; }

    /// <summary>The opposite corner, from the world's floor upwards.</summary>
    [LuaField("toY", Required = true)]
    public int ToY { get; set; }

    /// <summary>The opposite corner, north to south.</summary>
    [LuaField("toZ", Required = true)]
    public int ToZ { get; set; }

    /// <summary>
    /// Only count what this code names, which may be a <c>*</c> wildcard such as
    /// <c>game:ore-*</c>. Every block is counted when omitted, air included.
    /// </summary>
    [LuaField("code")]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public string? Code { get; set; }

    /// <summary>
    /// How many matches to stop after. A bound rather than a promise: a box holding
    /// more than this many is not fully searched, and the search stops the moment it
    /// has this many rather than reading the rest of the box.
    /// </summary>
    [LuaField("limit", Default = "4096")]
    public int Limit { get; set; } = 4096;
}

/// <summary>One block a search found, and where it stands.</summary>
[LuaTable("BlockAt", Given = true)]
public sealed class BlockAtPayload(int x, int y, int z, string block)
{
    /// <summary>Where it stands, east to west.</summary>
    [LuaField("x")]
    public int X { get; } = x;

    /// <summary>Where it stands, from the world's floor upwards.</summary>
    [LuaField("y")]
    public int Y { get; } = y;

    /// <summary>Where it stands, north to south.</summary>
    [LuaField("z")]
    public int Z { get; } = z;

    /// <summary>Code of the block standing there.</summary>
    [LuaField("block")]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public string Block { get; } = block;
}

/// <summary>Whether one player may act on one place.</summary>
[LuaTable("Access")]
public sealed class AccessSpec
{
    /// <summary>Identifier of the player asking, as an event gives it.</summary>
    [LuaField("player", Required = true)]
    public string Player { get; set; } = "";

    /// <summary>Which block they want to act on, east to west.</summary>
    [LuaField("x", Required = true)]
    public int X { get; set; }

    /// <summary>Which block, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public int Y { get; set; }

    /// <summary>Which block, north to south.</summary>
    [LuaField("z", Required = true)]
    public int Z { get; set; }

    /// <summary>What they want to do there. Building or breaking when omitted.</summary>
    [LuaField("what", Default = "\"buildorbreak\"")]
    public EnumAccessKind What { get; set; } = EnumAccessKind.BuildOrBreak;
}

/// <summary>A sound played at a place, which everybody near enough hears.</summary>
/// <remarks>
/// Nothing needs installing on a player's machine: the sound is one the game already
/// ships, named by its asset path, and the server tells each client to play it.
/// </remarks>
[LuaTable("Sound")]
public sealed class SoundSpec
{
    /// <summary>
    /// Path to the sound in the game's assets, such as <c>game:sounds/block/dirt</c>.
    /// A path the game has no sound for plays nothing and says nothing.
    /// </summary>
    [LuaField("sound", Required = true)]
    public string Sound { get; set; } = "";

    /// <summary>Where it comes from, east to west.</summary>
    [LuaField("x", Required = true)]
    public double X { get; set; }

    /// <summary>Where it comes from, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public double Y { get; set; }

    /// <summary>Where it comes from, north to south.</summary>
    [LuaField("z", Required = true)]
    public double Z { get; set; }

    /// <summary>How far away it can still be heard, in blocks.</summary>
    [LuaField("range", Default = "32")]
    public double Range { get; set; } = 32;

    /// <summary>How loud it is, where 1 is the sound as it was recorded.</summary>
    [LuaField("volume", Default = "1")]
    public double Volume { get; set; } = 1;

    /// <summary>
    /// How high it sounds, where 1 is unaltered. Left out, the game varies it slightly
    /// each time, so a repeated sound does not read as a loop.
    /// </summary>
    [LuaField("pitch")]
    public double? Pitch { get; set; }
}

/// <summary>Particles thrown off at a place, drawn on every screen near enough to see.</summary>
/// <remarks>
/// The second thing a server-side script can draw on somebody's screen, alongside
/// <c>highlight</c>. Given one point they appear there; given two they fill the box
/// between, making a cloud rather than a spot.
/// </remarks>
[LuaTable("Particles")]
public sealed class ParticlesSpec
{
    /// <summary>Where they appear, east to west.</summary>
    [LuaField("x", Required = true)]
    public double X { get; set; }

    /// <summary>Where they appear, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public double Y { get; set; }

    /// <summary>Where they appear, north to south.</summary>
    [LuaField("z", Required = true)]
    public double Z { get; set; }

    /// <summary>Far corner of the box they fill, east to west. The near one when omitted.</summary>
    [LuaField("toX")]
    public double? ToX { get; set; }

    /// <summary>Far corner, from the world's floor upwards. The near one when omitted.</summary>
    [LuaField("toY")]
    public double? ToY { get; set; }

    /// <summary>Far corner, north to south. The near one when omitted.</summary>
    [LuaField("toZ")]
    public double? ToZ { get; set; }

    /// <summary>How many to throw off.</summary>
    [LuaField("quantity", Default = "8")]
    public double Quantity { get; set; } = 8;

    /// <summary>What colour they are. An opaque white when omitted.</summary>
    [LuaField("colour")]
    public ColourSpec? Colour { get; set; }

    /// <summary>How fast they move and which way. Still when omitted.</summary>
    [LuaField("velocity")]
    public VelocitySpec? Velocity { get; set; }

    /// <summary>
    /// The fastest they may move, so each particle is drawn somewhere between this and
    /// <c>velocity</c>. The same as <c>velocity</c> when omitted, which moves them all
    /// alike.
    /// </summary>
    [LuaField("toVelocity")]
    public VelocitySpec? ToVelocity { get; set; }

    /// <summary>How long each lasts, in seconds.</summary>
    [LuaField("life", Default = "1")]
    public double Life { get; set; } = 1;

    /// <summary>
    /// How strongly they fall, where 1 falls as a dropped stack does and 0 hangs where
    /// it appeared. Negative makes them rise, as smoke does.
    /// </summary>
    [LuaField("gravity", Default = "1")]
    public double Gravity { get; set; } = 1;

    /// <summary>How big each is, where 1 is the ordinary size.</summary>
    [LuaField("size", Default = "1")]
    public double Size { get; set; } = 1;

    /// <summary>Which shape each is drawn as.</summary>
    [LuaField("model", Default = "\"quad\"")]
    public EnumParticleKind Model { get; set; } = EnumParticleKind.Quad;
}

