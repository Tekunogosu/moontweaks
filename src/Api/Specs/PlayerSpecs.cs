namespace MoonTweaks.Api;

// What a script writes to change a player, and what it is told back about one.

/// <summary>Which mode a player is playing in.</summary>
public enum EnumPlayKind
{
    /// <summary>Visiting without a character of their own.</summary>
    Guest,

    /// <summary>Playing the game as it is meant.</summary>
    Survival,

    /// <summary>Building freely, with everything to hand.</summary>
    Creative,

    /// <summary>Watching without taking part.</summary>
    Spectator,
}

/// <summary>Which side of a block is meant.</summary>
public enum EnumFaceKind
{
    /// <summary>Facing north, towards decreasing z.</summary>
    North,

    /// <summary>Facing east, towards increasing x.</summary>
    East,

    /// <summary>Facing south, towards increasing z.</summary>
    South,

    /// <summary>Facing west, towards decreasing x.</summary>
    West,

    /// <summary>The top face.</summary>
    Up,

    /// <summary>The bottom face.</summary>
    Down,
}

/// <summary>
/// A change to one of a player's abilities, held under a name so it can be taken
/// back.
/// </summary>
/// <remarks>
/// The game keeps these as a set of named contributions rather than one number, and
/// adds them to a base of 1 to arrive at what a player actually gets. So a
/// <c>value</c> of 0.5 on <c>walkspeed</c> makes them half again as fast, -0.5 makes
/// them half as fast, and 0 changes nothing. Two scripts may hold a contribution to
/// the same ability at once without either losing its own, which is the whole reason
/// each is named.
///
/// The abilities themselves are the game's rather than this mod's, so what is worth
/// setting depends on the version being run. <c>walkspeed</c>, <c>healingeffectivness</c>,
/// <c>hungerrate</c>, <c>rangedWeaponsAcc</c>, <c>rangedWeaponsSpeed</c>,
/// <c>animalLootDropRate</c>, <c>animalHarvestingTime</c>, <c>forageDropRate</c>,
/// <c>wholeVesselLootChance</c>, <c>oreDropRate</c>, <c>rustyGearDropRate</c> and
/// <c>mechanicalsDamage</c> are the ones the survival mod reads.
/// </remarks>
[LuaTable("Stat")]
public sealed class StatSpec
{
    /// <summary>Identifier of the player, as an event gives it.</summary>
    [LuaField("player", Required = true)]
    public string Player { get; set; } = "";

    /// <summary>Which ability to change, such as <c>walkspeed</c>.</summary>
    [LuaField("stat", Required = true)]
    public string Stat { get; set; } = "";

    /// <summary>
    /// Name to hold this change under, so the same script can replace or remove its
    /// own without disturbing anybody else's contribution to the same ability.
    /// </summary>
    [LuaField("name", Required = true)]
    public string Name { get; set; } = "";

    /// <summary>
    /// How much to add. Zero changes nothing, 0.5 is half again as much, and -0.5 is
    /// half as much.
    /// </summary>
    [LuaField("value", Required = true)]
    public double Value { get; set; }

    /// <summary>
    /// Whether it survives a restart. Left off, it lasts until the player logs out,
    /// which is what anything temporary wants.
    /// </summary>
    [LuaField("persistent", Default = "false")]
    public bool Persistent { get; set; }
}

/// <summary>The block a player has their cursor on.</summary>
[LuaTable("Looking", Given = true)]
public sealed class LookingPayload(int x, int y, int z, string? block, EnumFaceKind? face)
{
    /// <summary>Where that block is, east to west.</summary>
    [LuaField("x")]
    public int X { get; } = x;

    /// <summary>Where that block is, from the world's floor upwards.</summary>
    [LuaField("y")]
    public int Y { get; } = y;

    /// <summary>Where that block is, north to south.</summary>
    [LuaField("z")]
    public int Z { get; } = z;

    /// <summary>Code of the block, or nil where the selection no longer names one.</summary>
    [LuaField("block")]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public string? Block { get; } = block;

    /// <summary>Which side of it they are pointing at, or nil where the game did not say.</summary>
    [LuaField("face")]
    public EnumFaceKind? Face { get; } = face;
}
