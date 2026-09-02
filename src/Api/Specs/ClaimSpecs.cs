namespace MoonTweaks.Api;

// What a script writes to claim land, and what it is told about the claims already
// standing. A claim is the game's own protection: the server holds it, saves it with
// the world and sends it to every client, so a script that adds one has protected
// land for players who have installed nothing.

/// <summary>A stretch of land somebody has claimed, as a script is told about it.</summary>
/// <remarks>
/// A claim is named by its owner and its number among that owner's claims, which is
/// the same numbering the game's own <c>/land list</c> shows a player. Both travel
/// together on every claim read back, and both are what <c>claims.remove</c> takes.
///
/// That number is a position rather than a name: removing a claim moves every later
/// claim of that owner down one. A script removing more than one should read the
/// claims again between removals, or work from the highest number downwards.
/// </remarks>
[LuaTable("Claim", Given = true)]
public sealed class ClaimPayload
{
    /// <summary>Identifier of the player who owns it.</summary>
    [LuaField("owner")]
    public string Owner { get; init; } = "";

    /// <summary>
    /// What that player was called when the claim was made. The name the game shows
    /// on the claim rather than a name to look a player up by, since somebody may have
    /// been renamed since.
    /// </summary>
    [LuaField("ownerName")]
    public string OwnerName { get; init; } = "";

    /// <summary>
    /// Its number among its owner's claims, counting from zero, as <c>/land list</c>
    /// shows it to them. This and <c>owner</c> together are what names it.
    /// </summary>
    [LuaField("index")]
    public int Index { get; init; }

    /// <summary>What the owner called it, which is empty where they called it nothing.</summary>
    [LuaField("description")]
    public string Description { get; init; } = "";

    /// <summary>
    /// How strongly it is held. The game compares this against a player's own
    /// privilege level to decide whether they may act on the land anyway.
    /// </summary>
    [LuaField("protectionLevel")]
    public int ProtectionLevel { get; init; }

    /// <summary>
    /// The areas it covers. A claim may hold several, so a claim that is not a single
    /// box is still one claim and is removed as one.
    /// </summary>
    [LuaField("areas")]
    public ClaimAreaPayload[] Areas { get; init; } = [];

    /// <summary>Whether anybody at all may operate what stands on it.</summary>
    [LuaField("allowUseEveryone")]
    public bool AllowUseEveryone { get; init; }

    /// <summary>Whether anybody at all may walk through it.</summary>
    [LuaField("allowTraverseEveryone")]
    public bool AllowTraverseEveryone { get; init; }

    /// <summary>Players the owner has let in, and what each of them may do.</summary>
    [LuaField("permitted")]
    public PermitPayload[] Permitted { get; init; } = [];
}

/// <summary>One area of a claim, given as two opposite corners.</summary>
[LuaTable("ClaimArea", Given = true)]
public sealed class ClaimAreaPayload
{
    /// <summary>The lower corner, east to west.</summary>
    [LuaField("x")]
    public int X { get; init; }

    /// <summary>The lower corner, from the world's floor upwards.</summary>
    [LuaField("y")]
    public int Y { get; init; }

    /// <summary>The lower corner, north to south.</summary>
    [LuaField("z")]
    public int Z { get; init; }

    /// <summary>The upper corner, east to west.</summary>
    [LuaField("toX")]
    public int ToX { get; init; }

    /// <summary>The upper corner, from the world's floor upwards.</summary>
    [LuaField("toY")]
    public int ToY { get; init; }

    /// <summary>The upper corner, north to south.</summary>
    [LuaField("toZ")]
    public int ToZ { get; init; }
}

/// <summary>One player an owner has let onto their claim, and what they may do there.</summary>
/// <remarks>
/// Three separate answers rather than one, because the game holds them as flags a
/// player may carry any combination of: somebody may be allowed to walk through and
/// to operate what stands there without being allowed to build.
/// </remarks>
[LuaTable("Permit", Given = true)]
public sealed class PermitPayload
{
    /// <summary>Identifier of the player let in.</summary>
    [LuaField("player")]
    public string Player { get; init; } = "";

    /// <summary>What they were called when they were let in.</summary>
    [LuaField("name")]
    public string Name { get; init; } = "";

    /// <summary>Whether they may place blocks there or take them away.</summary>
    [LuaField("mayBuild")]
    public bool MayBuild { get; init; }

    /// <summary>Whether they may operate what stands there without changing it.</summary>
    [LuaField("mayUse")]
    public bool MayUse { get; init; }

    /// <summary>Whether they may walk through it.</summary>
    [LuaField("mayTraverse")]
    public bool MayTraverse { get; init; }
}

/// <summary>A claim a script is making, over one box of land.</summary>
/// <remarks>
/// One box per claim here, where the game allows a claim to hold several. Nothing has
/// asked to build a claim out of several boxes from a script, and a claim made of one
/// is removed and read back exactly like any other.
/// </remarks>
[LuaTable("NewClaim")]
public sealed class ClaimSpec
{
    /// <summary>
    /// Identifier of the player it belongs to, as an event gives it. Their claim
    /// whether or not they are online, and the name shown on it is whatever this
    /// server last knew them as.
    /// </summary>
    [LuaField("owner", Required = true)]
    public string Owner { get; set; } = "";

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

    /// <summary>Name for the claim, which the game shows its owner.</summary>
    [LuaField("description", Default = "\"\"")]
    public string Description { get; set; } = "";

    /// <summary>
    /// How strongly to hold it. The game compares this against a player's privilege
    /// level, so a claim made at a level above what any player holds is one only the
    /// console can build on.
    /// </summary>
    [LuaField("protectionLevel", Default = "1")]
    public int ProtectionLevel { get; set; } = 1;

    /// <summary>Whether anybody at all may operate what stands on it.</summary>
    [LuaField("allowUseEveryone", Default = "false")]
    public bool AllowUseEveryone { get; set; }

    /// <summary>Whether anybody at all may walk through it.</summary>
    [LuaField("allowTraverseEveryone", Default = "false")]
    public bool AllowTraverseEveryone { get; set; }
}
