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
