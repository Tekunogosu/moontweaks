namespace MoonTweaks.Api;

// The chat groups a server keeps, and what a script is told about them. A group is
// the game's own channel: players join one, talk in it, and the server addresses it
// as a whole, so a message reaches some players and not others.

/// <summary>Who may put themselves into a group.</summary>
public enum EnumJoinPolicy
{
    /// <summary>
    /// Only somebody already in it may bring anybody else in. What a group carries
    /// when nothing says otherwise.
    /// </summary>
    InviteOnly,

    /// <summary>Anybody may walk in, with the game's own <c>/group join</c>.</summary>
    Everyone,
}

/// <summary>How much say a player has in a group.</summary>
public enum EnumGroupStanding
{
    /// <summary>In the group, and no more than that.</summary>
    Member,

    /// <summary>Runs the group day to day.</summary>
    Op,

    /// <summary>Made the group, and the last word in it.</summary>
    Owner,
}

/// <summary>One group a player belongs to, and what they are in it.</summary>
[LuaTable("GroupMembership", Given = true)]
public sealed class GroupMembershipPayload
{
    /// <summary>Number the group is addressed by, which <c>groups.say</c> takes.</summary>
    [LuaField("group")]
    public int Group { get; init; }

    /// <summary>What the group is called.</summary>
    [LuaField("name")]
    public string Name { get; init; } = "";

    /// <summary>What this player is in it.</summary>
    [LuaField("standing")]
    public EnumGroupStanding Standing { get; init; }
}

/// <summary>A chat group, as a script is told about it.</summary>
[LuaTable("Group", Given = true)]
public sealed class GroupPayload
{
    /// <summary>Number the group is addressed by, which <c>groups.say</c> takes.</summary>
    [LuaField("group")]
    public int Group { get; init; }

    /// <summary>What it is called.</summary>
    [LuaField("name")]
    public string Name { get; init; } = "";

    /// <summary>Identifier of the player who made it, which may be nobody.</summary>
    [LuaField("owner")]
    public string? Owner { get; init; }

    /// <summary>
    /// Identifiers of the members who are online. A message sent to the group reaches
    /// every member who is connected, which is these; it says nothing about the
    /// members who are not.
    /// </summary>
    [LuaField("online")]
    public string[] Online { get; init; } = [];

    /// <summary>
    /// Whether anybody may walk in. This decides one thing and one thing only: whether
    /// the game's own <c>/group join</c> lets a player in. It does not gate
    /// <c>groups.join</c>, which is the server acting rather than a player asking.
    /// </summary>
    [LuaField("joinPolicy")]
    public EnumJoinPolicy JoinPolicy { get; init; }
}

/// <summary>A group a script is making.</summary>
[LuaTable("NewGroup")]
public sealed class GroupSpec
{
    /// <summary>
    /// What to call it, which is also what every other function here takes to find it
    /// again. Letters, numbers and underscores only, and no two groups may share one:
    /// both are the game's own rules and both are refused by name here.
    /// </summary>
    [LuaField("name", Required = true)]
    public string Name { get; set; } = "";

    /// <summary>
    /// Identifier of the player who owns it, which decides who the game lets rename or
    /// disband it. Left out, nobody owns it and only a script can take it away.
    /// </summary>
    [LuaField("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// Whether anybody may walk in with <c>/group join</c>. Invite only when omitted,
    /// as the game makes a group in its own command.
    /// </summary>
    [LuaField("joinPolicy", Default = "\"inviteonly\"")]
    public EnumJoinPolicy JoinPolicy { get; set; } = EnumJoinPolicy.InviteOnly;
}
