namespace MoonTweaks.Api;

// The closed sets a script writes as strings. Declared here rather than taken from
// the game, so this layer names no game type; the layer that applies them matches
// these to the game's own by name. A set belonging to one small
// part of the surface is declared beside that part's own shapes instead; these are
// the ones an item or a block is described with.

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

/// <summary>
/// How a range picks the numbers it hands out. Every quantity written as an average
/// and a variance reads this, so a drop, a crushing yield and a span of hours all
/// spread the same way.
/// </summary>
public enum EnumSpreadKind
{
    /// <summary>Anywhere in the range, each value as likely as any other.</summary>
    Uniform,

    /// <summary>Nearer the average more often, falling away in a straight line.</summary>
    Triangle,

    /// <summary>Nearer the average more often, falling away on a bell curve.</summary>
    Gaussian,

    /// <summary>As <c>gaussian</c>, gathered more tightly around the average.</summary>
    NarrowGaussian,

    /// <summary>As <c>narrowgaussian</c>, tighter again.</summary>
    VeryNarrowGaussian,

    /// <summary>The ends of the range more often than the middle.</summary>
    InverseGaussian,

    /// <summary>As <c>inversegaussian</c>, favouring the ends more strongly.</summary>
    NarrowInverseGaussian,

    /// <summary>From the average upwards, the average itself being likeliest.</summary>
    Invexp,

    /// <summary>As <c>invexp</c>, dropping away faster.</summary>
    StrongInvexp,

    /// <summary>As <c>stronginvexp</c>, dropping away faster again.</summary>
    StrongerInvexp,

    /// <summary>Once anywhere in the range, and nothing at all every time after.</summary>
    Dirac,
}

/// <summary>Which volume control a sound is played under.</summary>
public enum EnumSoundKind
{
    /// <summary>An ordinary sound effect.</summary>
    Sound,

    /// <summary>Music.</summary>
    Music,

    /// <summary>Part of the background of a place.</summary>
    Ambient,

    /// <summary>Weather.</summary>
    Weather,

    /// <summary>Something alive.</summary>
    Entity,

    /// <summary>Music, unaffected by temporal instability.</summary>
    MusicGlitchunaffected,

    /// <summary>Background, unaffected by temporal instability.</summary>
    AmbientGlitchunaffected,

    /// <summary>A sound effect, unaffected by temporal instability.</summary>
    SoundGlitchunaffected,
}

/// <summary>Which of the three soil nutrients a crop feeds on.</summary>
public enum EnumNutrientKind
{
    /// <summary>Nitrogen.</summary>
    N,

    /// <summary>Phosphorus.</summary>
    P,

    /// <summary>Potassium.</summary>
    K,
}
