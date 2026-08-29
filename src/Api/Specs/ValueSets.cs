namespace MoonTweaks.Api;

// The closed sets a script writes as strings. Declared here rather than taken
// from the game, so this layer names no game type; the layer that applies them
// matches these to the game's own by name.

/// <summary>Which registry an asset code names.</summary>
public enum ResourceKind
{
    /// <summary>An item, the default for codes that resolve in both registries.</summary>
    Item,

    /// <summary>A block.</summary>
    Block,
}

/// <summary>Which kind of heating turns something into something else.</summary>
public enum EnumSmeltKind
{
    /// <summary>Melted in a crucible or bloomery.</summary>
    Smelt,

    /// <summary>Cooked in a pot or on a firepit.</summary>
    Cook,

    /// <summary>Baked in an oven.</summary>
    Bake,

    /// <summary>Turned into something else by heat without melting.</summary>
    Convert,

    /// <summary>Burned away.</summary>
    Fire,
}

/// <summary>Which part of a diet a food counts towards.</summary>
public enum EnumFoodKind
{
    /// <summary>Nourishes nothing.</summary>
    NoNutrition,

    /// <summary>Fruit.</summary>
    Fruit,

    /// <summary>Vegetable.</summary>
    Vegetable,

    /// <summary>Protein.</summary>
    Protein,

    /// <summary>Grain.</summary>
    Grain,

    /// <summary>Dairy.</summary>
    Dairy,
}

/// <summary>What a block is made of, which decides how fast a tool breaks it.</summary>
public enum EnumBlockKind
{
    /// <summary>Nothing at all.</summary>
    Air,

    /// <summary>Earth and dirt.</summary>
    Soil,

    /// <summary>Loose gravel.</summary>
    Gravel,

    /// <summary>Loose sand.</summary>
    Sand,

    /// <summary>Timber and planks.</summary>
    Wood,

    /// <summary>Foliage.</summary>
    Leaves,

    /// <summary>Rock.</summary>
    Stone,

    /// <summary>Rock carrying metal.</summary>
    Ore,

    /// <summary>Water.</summary>
    Water,

    /// <summary>Snow.</summary>
    Snow,

    /// <summary>Ice.</summary>
    Ice,

    /// <summary>Worked metal.</summary>
    Metal,

    /// <summary>The mantle beneath the world.</summary>
    Mantle,

    /// <summary>Growing plants.</summary>
    Plant,

    /// <summary>Glass.</summary>
    Glass,

    /// <summary>Fired clay.</summary>
    Ceramic,

    /// <summary>Cloth.</summary>
    Cloth,

    /// <summary>Lava.</summary>
    Lava,

    /// <summary>Brick.</summary>
    Brick,

    /// <summary>Fire.</summary>
    Fire,

    /// <summary>Blocks the world uses rather than the player.</summary>
    Meta,

    /// <summary>Anything else.</summary>
    Other,
}

/// <summary>Which inventory something may be carried in.</summary>
public enum EnumStorageKind
{
    /// <summary>An ordinary slot.</summary>
    General,

    /// <summary>A backpack slot.</summary>
    Backpack,

    /// <summary>A metallurgy slot.</summary>
    Metallurgy,

    /// <summary>A jewellery slot.</summary>
    Jewellery,

    /// <summary>An alchemy slot.</summary>
    Alchemy,

    /// <summary>An agriculture slot.</summary>
    Agriculture,

    /// <summary>A currency slot.</summary>
    Currency,

    /// <summary>An outfit slot.</summary>
    Outfit,

    /// <summary>The off hand.</summary>
    Offhand,

    /// <summary>A quiver slot.</summary>
    Arrow,

    /// <summary>A skill slot.</summary>
    Skill,
}

/// <summary>What wears something down as it is used.</summary>
public enum EnumDamageKind
{
    /// <summary>Breaking blocks with it.</summary>
    BlockBreaking,

    /// <summary>Attacking with it.</summary>
    Attacking,

    /// <summary>Fire reaching it.</summary>
    Fire,
}

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

/// <summary>What becoming stale turns something into.</summary>
public enum EnumTransitionKind
{
    /// <summary>Nothing in particular, which is what the game's own meal recipes say.</summary>
    None,

    /// <summary>Rots.</summary>
    Perish,

    /// <summary>Dries out.</summary>
    Dry,

    /// <summary>Burns away.</summary>
    Burn,

    /// <summary>Cures, as meat left in a barrel of brine does.</summary>
    Cure,

    /// <summary>Turns into something else outright.</summary>
    Convert,

    /// <summary>Ripens.</summary>
    Ripen,

    /// <summary>Melts.</summary>
    Melt,

    /// <summary>Sets hard, as hot glue does.</summary>
    Harden,
}

/// <summary>What kind of value a command reads from what was typed after its name.</summary>
public enum ArgumentKind
{
    /// <summary>One word.</summary>
    Word,

    /// <summary>A whole number.</summary>
    Int,

    /// <summary>A number, whole or not.</summary>
    Number,

    /// <summary>On or off.</summary>
    Bool,

    /// <summary>Everything left on the line, spaces included.</summary>
    Text,

    /// <summary>A player who is online, which a handler is given the identifier of.</summary>
    Player,
}

/// <summary>Which class of tool an item counts as, which decides what it may be used for.</summary>
public enum EnumToolKind
{
    /// <summary>A knife.</summary>
    Knife,

    /// <summary>A pickaxe.</summary>
    Pickaxe,

    /// <summary>An axe.</summary>
    Axe,

    /// <summary>A sword.</summary>
    Sword,

    /// <summary>A shovel.</summary>
    Shovel,

    /// <summary>A hammer.</summary>
    Hammer,

    /// <summary>A spear.</summary>
    Spear,

    /// <summary>A bow.</summary>
    Bow,

    /// <summary>Shears.</summary>
    Shears,

    /// <summary>A sickle.</summary>
    Sickle,

    /// <summary>A hoe.</summary>
    Hoe,

    /// <summary>A saw.</summary>
    Saw,

    /// <summary>A chisel.</summary>
    Chisel,

    /// <summary>A scythe.</summary>
    Scythe,

    /// <summary>A sling.</summary>
    Sling,

    /// <summary>A wrench.</summary>
    Wrench,

    /// <summary>A prospecting probe.</summary>
    Probe,

    /// <summary>A meter.</summary>
    Meter,

    /// <summary>A drill.</summary>
    Drill,

    /// <summary>A firearm.</summary>
    Firearm,

    /// <summary>A crossbow.</summary>
    Crossbow,

    /// <summary>A javelin.</summary>
    Javelin,

    /// <summary>A pike.</summary>
    Pike,

    /// <summary>A shield.</summary>
    Shield,

    /// <summary>A club.</summary>
    Club,

    /// <summary>A mace.</summary>
    Mace,

    /// <summary>A warhammer.</summary>
    Warhammer,

    /// <summary>A poleaxe.</summary>
    Poleaxe,

    /// <summary>A halberd.</summary>
    Halberd,

    /// <summary>A polearm.</summary>
    Polearm,

    /// <summary>A staff.</summary>
    Staff,

    /// <summary>Tongs.</summary>
    Tongs,

    /// <summary>A crowbar.</summary>
    Crowbar,
}

/// <summary>One of the four seasons a place is in.</summary>
public enum EnumSeasonKind
{
    /// <summary>Spring.</summary>
    Spring,

    /// <summary>Summer.</summary>
    Summer,

    /// <summary>Autumn.</summary>
    Fall,

    /// <summary>Winter.</summary>
    Winter,
}

/// <summary>Which month of the world's year it is.</summary>
public enum EnumMonthName
{
    /// <summary>January.</summary>
    January,

    /// <summary>February.</summary>
    February,

    /// <summary>March.</summary>
    March,

    /// <summary>April.</summary>
    April,

    /// <summary>May.</summary>
    May,

    /// <summary>June.</summary>
    June,

    /// <summary>July.</summary>
    July,

    /// <summary>August.</summary>
    August,

    /// <summary>September.</summary>
    September,

    /// <summary>October.</summary>
    October,

    /// <summary>November.</summary>
    November,

    /// <summary>December.</summary>
    December,
}

/// <summary>How much of the moon is lit, from dark through full and back.</summary>
public enum EnumMoonKind
{
    /// <summary>New moon, giving no light at all.</summary>
    Empty,

    /// <summary>Waxing, a sliver lit.</summary>
    Grow1,

    /// <summary>Waxing, half lit.</summary>
    Grow2,

    /// <summary>Waxing, nearly full.</summary>
    Grow3,

    /// <summary>Full moon, at its brightest.</summary>
    Full,

    /// <summary>Waning, nearly full.</summary>
    Shrink1,

    /// <summary>Waning, half lit.</summary>
    Shrink2,

    /// <summary>Waning, a sliver lit.</summary>
    Shrink3,
}

/// <summary>Which half of the world a place is in, which decides when its seasons fall.</summary>
public enum EnumHemisphereKind
{
    /// <summary>North of the equator.</summary>
    North,

    /// <summary>South of the equator, where the seasons are the other way round.</summary>
    South,
}

/// <summary>Which light a reading counts.</summary>
public enum EnumLightKind
{
    /// <summary>Light cast by blocks alone, ignoring the sky.</summary>
    OnlyBlockLight,

    /// <summary>Light from the sky alone, at its brightest rather than as it is now.</summary>
    OnlySunLight,

    /// <summary>Whichever of the two is brighter, ignoring the time of day.</summary>
    MaxLight,

    /// <summary>Whichever of the two is brighter, as it stands at this hour.</summary>
    MaxTimeOfDayLight,

    /// <summary>Light from the sky as it stands at this hour.</summary>
    TimeOfDaySunLight,

    /// <summary>How bright the sun itself is, rather than what it reaches.</summary>
    Sunbrightness,
}
