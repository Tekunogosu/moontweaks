namespace MoonTweaks.Api;

// What a script writes to reach a living thing, and what it is told back about one.

/// <summary>
/// A stack of something, as a script is told about it rather than as it writes one.
/// </summary>
/// <param name="code">What it is.</param>
/// <param name="quantity">How many.</param>
[LuaTable("Stack", Given = true)]
public sealed class StackPayload(string code, int quantity)
{
    /// <summary>Asset code of what the stack holds.</summary>
    [LuaField("code")]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public string Code { get; } = code;

    /// <summary>How many of it there are.</summary>
    [LuaField("quantity")]
    public int Quantity { get; } = quantity;
}

/// <summary>
/// A part of the world to look in, as a box around a point.
/// </summary>
/// <remarks>
/// The box is measured outwards from the point rather than corner to corner, so a
/// <c>range</c> of 8 reaches 8 blocks in every horizontal direction. Height is
/// separate because what a script usually wants is a wide, shallow slice: everything
/// on this floor rather than everything in this cube.
/// </remarks>
[LuaTable("Area")]
public sealed class AreaSpec
{
    /// <summary>Middle of the box, east to west.</summary>
    [LuaField("x", Required = true)]
    public double X { get; set; }

    /// <summary>Middle of the box, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public double Y { get; set; }

    /// <summary>Middle of the box, north to south.</summary>
    [LuaField("z", Required = true)]
    public double Z { get; set; }

    /// <summary>How far out to look, in blocks, horizontally.</summary>
    [LuaField("range", Required = true)]
    public double Range { get; set; }

    /// <summary>
    /// How far up and down to look. The same as <c>range</c> when omitted, which
    /// makes the box a cube.
    /// </summary>
    [LuaField("height")]
    public double? Height { get; set; }

    /// <summary>
    /// Tags everything found must carry, such as <c>{ "animal" }</c>. Matches on what
    /// a creature is rather than what it is called, so one search reaches a modded
    /// wolf as readily as a vanilla one. Used alone, or alongside <c>code</c> to narrow
    /// a wildcard further. A bare list asks for every tag in it; the keys of a
    /// <c>TagCondition</c> ask for anything richer than that.
    /// </summary>
    /// <remarks>
    /// Creatures carry tags from a registry of their own rather than the one items and
    /// blocks share, so a name here is not a name <c>moontweaks.items.set</c> selects
    /// by and the two cannot be mixed. <c>library/codes.lua</c> lists both sets, the
    /// creature one under <c>EntityTag</c>.
    ///
    /// An editor offers the item and block names inside this key rather than the
    /// creature ones, because a tag condition is one shape wherever it is written and
    /// names its suggestions once. Every name is still accepted and one the server does
    /// not know is refused by name, so the wrong suggestion costs a glance at
    /// <c>codes.lua</c> rather than a silent mismatch.
    /// </remarks>
    [LuaField("tags")]
    public TagConditionSpec? Tags { get; set; }

    /// <summary>
    /// Only count what this code names, which may be a <c>*</c> wildcard such as
    /// <c>game:wolf-*</c>. Everything is counted when omitted.
    /// </summary>
    [LuaField("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Whether to skip players. On by default, because a script asking what is nearby
    /// almost always means the wildlife, and <c>moontweaks.players</c> is the better
    /// way to reach a person.
    /// </summary>
    [LuaField("skipPlayers", Default = "true")]
    public bool SkipPlayers { get; set; } = true;

    /// <summary>Whether to skip anything already dead. On by default.</summary>
    [LuaField("aliveOnly", Default = "true")]
    public bool AliveOnly { get; set; } = true;
}

/// <summary>Something to put into the world.</summary>
/// <remarks>
/// Spawning is one of the few things here that refuses its code outright: an entity
/// type is looked up as the call is made, so a code the server does not have names
/// itself rather than silently spawning nothing.
/// </remarks>
[LuaTable("Spawn")]
public sealed class SpawnSpec
{
    /// <summary>
    /// Entity code, such as <c>game:chicken-hen</c>. Names one type: a wildcard
    /// belongs in a search rather than here, since this has to pick exactly one.
    /// </summary>
    [LuaField("code", Required = true)]
    public string Code { get; set; } = "";

    /// <summary>Where to put it, east to west.</summary>
    [LuaField("x", Required = true)]
    public double X { get; set; }

    /// <summary>Where to put it, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public double Y { get; set; }

    /// <summary>Where to put it, north to south.</summary>
    [LuaField("z", Required = true)]
    public double Z { get; set; }

    /// <summary>How many to put there.</summary>
    [LuaField("quantity", Default = "1")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// How far apart to scatter them, in blocks. Everything lands on the same spot
    /// when omitted.
    /// </summary>
    [LuaField("spread", Default = "0")]
    public double Spread { get; set; }

    /// <summary>
    /// Which way it faces, in degrees. Whether anything keeps facing that way is up
    /// to what it is: something with a mind of its own turns as soon as it thinks.
    /// </summary>
    [LuaField("yaw", Default = "0")]
    public double Yaw { get; set; }

    /// <summary>
    /// Whether everything spawned belongs to one herd, which makes them move together
    /// rather than wander apart. Only means anything to creatures.
    /// </summary>
    [LuaField("herd", Default = "true")]
    public bool Herd { get; set; } = true;
}

/// <summary>A change to one of an entity's abilities, held under a name.</summary>
/// <remarks>
/// The same shape and the same rules as <c>moontweaks.players.setStat</c>: the game
/// holds each ability as a set of named contributions added to a base of 1, so a
/// value of 0.5 makes something half again as capable and -0.5 halves it.
/// </remarks>
[LuaTable("EntityStat")]
public sealed class EntityStatSpec
{
    /// <summary>Identifier of the entity, as a search gives it.</summary>
    [LuaField("entity", Required = true)]
    public double Entity { get; set; }

    /// <summary>Which ability to change, such as <c>walkspeed</c>.</summary>
    [LuaField("stat", Required = true)]
    public string Stat { get; set; } = "";

    /// <summary>Name to hold this change under, for replacing or removing it later.</summary>
    [LuaField("name", Required = true)]
    public string Name { get; set; } = "";

    /// <summary>How much to add. Zero changes nothing.</summary>
    [LuaField("value", Required = true)]
    public double Value { get; set; }

    /// <summary>Whether it survives a restart. Lasts only while loaded when omitted.</summary>
    [LuaField("persistent", Default = "false")]
    public bool Persistent { get; set; }
}

/// <summary>What a script is told about one living thing.</summary>
[LuaTable("Entity", Given = true)]
public sealed class EntityPayload
{
    /// <summary>
    /// The server's own identifier for it, which every function here takes. Good only
    /// while the entity is loaded: one remembered across a restart, or while its chunk
    /// was unloaded, may name nothing by the time it is used.
    /// </summary>
    [LuaField("id")]
    public double Id { get; init; }

    /// <summary>Entity code, such as <c>game:wolf-male</c>.</summary>
    [LuaField("code")]
    public string Code { get; init; } = "";

    /// <summary>What it is called, which is its name tag where it has one.</summary>
    [LuaField("name")]
    public string Name { get; init; } = "";

    /// <summary>Where it is, east to west.</summary>
    [LuaField("x")]
    public double X { get; init; }

    /// <summary>Where it is, from the world's floor upwards.</summary>
    [LuaField("y")]
    public double Y { get; init; }

    /// <summary>Where it is, north to south.</summary>
    [LuaField("z")]
    public double Z { get; init; }

    /// <summary>Which way it faces, in degrees.</summary>
    [LuaField("yaw")]
    public double Yaw { get; init; }

    /// <summary>Whether it is still alive.</summary>
    [LuaField("alive")]
    public bool Alive { get; init; }

    /// <summary>Whether it is burning.</summary>
    [LuaField("onFire")]
    public bool OnFire { get; init; }

    /// <summary>Whether it is standing on something rather than falling.</summary>
    [LuaField("onGround")]
    public bool OnGround { get; init; }

    /// <summary>Whether it is in water deep enough to swim in.</summary>
    [LuaField("swimming")]
    public bool Swimming { get; init; }

    /// <summary>How much health it has, or nil where it has none to have.</summary>
    [LuaField("health")]
    public double? Health { get; init; }

    /// <summary>How much health it can have, or nil where it has none to have.</summary>
    [LuaField("maxHealth")]
    public double? MaxHealth { get; init; }

    /// <summary>
    /// Identifier of the player this is, or nil where it is not one. A player's body
    /// is an entity like any other, so a search can turn one up; this is what says so,
    /// and what hands the identifier <c>moontweaks.players</c> takes.
    /// </summary>
    [LuaField("player")]
    public string? Player { get; init; }

    /// <summary>
    /// What it is carrying, where it is a stack lying on the ground. Nil for anything
    /// that is not one, which is everything alive.
    /// </summary>
    [LuaField("stack")]
    public StackPayload? Stack { get; init; }
}

/// <summary>Why something left the world.</summary>
public enum EnumDespawnKind
{
    /// <summary>It was killed.</summary>
    Death,

    /// <summary>It burned up.</summary>
    Combusted,

    /// <summary>Nobody was near enough to keep it.</summary>
    OutOfRange,

    /// <summary>Somebody picked it up, which is how a dropped stack goes.</summary>
    PickedUp,

    /// <summary>Its chunk left memory. It is not gone from the world, only out of reach.</summary>
    Unload,

    /// <summary>A player left, taking their body with them.</summary>
    Disconnect,

    /// <summary>It timed out, as a dropped stack does when nobody collects it.</summary>
    Expire,

    /// <summary>Something took it out deliberately, as <c>despawn</c> does.</summary>
    Removed,
}

/// <summary>What hurt something, as the game classes it.</summary>
public enum EnumHurtKind
{
    /// <summary>A fall.</summary>
    Gravity,

    /// <summary>Burning.</summary>
    Fire,

    /// <summary>A blunt blow.</summary>
    BluntAttack,

    /// <summary>A cut.</summary>
    SlashingAttack,

    /// <summary>A stab.</summary>
    PiercingAttack,

    /// <summary>No air.</summary>
    Suffocation,

    /// <summary>Healing, which the game counts as damage of a kind.</summary>
    Heal,

    /// <summary>Poison.</summary>
    Poison,

    /// <summary>Starvation.</summary>
    Hunger,

    /// <summary>Being crushed.</summary>
    Crushing,

    /// <summary>Cold.</summary>
    Frost,

    /// <summary>Electricity.</summary>
    Electricity,

    /// <summary>Heat.</summary>
    Heat,

    /// <summary>An injury with no more particular cause.</summary>
    Injury,

    /// <summary>Acid.</summary>
    Acid,
}

