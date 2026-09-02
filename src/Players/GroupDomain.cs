using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Players;

/// <summary>
/// The chat groups a server keeps: reading them, making them, putting players in
/// them, and speaking into one.
/// </summary>
/// <remarks>
/// A group is the game's own channel rather than anything this mod invents. Players
/// make them with <c>/group</c>, join them and talk in them, and the server addresses
/// one as a whole — so this reaches some players and not others, where
/// <c>moontweaks.players.announce</c> reaches everybody and
/// <c>moontweaks.players.say</c> reaches one.
///
/// A group is named by its name here, not by its number. The game assigns the number
/// as a group is made and then offers no way to look a group up by it, so the number
/// is something a script is told rather than something it uses. Two groups cannot
/// share a name, so the name serves as a handle.
///
/// A group this makes is the same object the game's own command makes, so the two are
/// interchangeable: a player may rename or disband one a script created, and a script
/// may take away one a player created.
///
/// One thing does not reach the player. The game tells a client about a group as it
/// joins them to one, through a packet this mod cannot send, so somebody put into a
/// group by <c>join</c> may have no tab for it until they next connect. Messages sent
/// to the group reach them either way — delivery is decided by the membership, which
/// is written immediately.
/// </remarks>
/// <example>
/// <code>
/// local groups = moontweaks.groups
///
/// -- Made once, in a script's body, and left alone if it is already there.
/// if not groups.find("staff") then
///   groups.add { name = "staff", joinPolicy = "inviteonly" }
/// end
///
/// moontweaks.events.playerJoin(function(e)
///   if moontweaks.players.hasPrivilege(e.player, "controlserver") then
///     groups.join(e.player, "staff", "op")
///     groups.say("staff", ("%s is on."):format(moontweaks.players.name(e.player)))
///   end
/// end)
/// </code>
/// </example>
[LuaModule("moontweaks.groups")]
public sealed class GroupDomain(ICoreServerAPI api, PlayerAccess players, GroupAccess groups)
{
    /// <summary>
    /// The groups a player belongs to, and what they are in each. Empty for somebody
    /// who has joined none, which is most players.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("of")]
    public IReadOnlyList<GroupMembershipPayload> Of(ScriptOrigin origin, string player) =>
    [
        .. players.Find(player, origin).Groups
            // The game holds "not a member" as a membership at the lowest level rather
            // than as an absent entry, and a script asking what somebody belongs to
            // does not mean that.
            .Where(membership => membership.Level != EnumPlayerGroupMemberShip.None)
            .Select(membership => new GroupMembershipPayload
            {
                Group = membership.GroupUid,
                Name = membership.GroupName ?? "",
                Standing = ValueSet.As<EnumGroupStanding>(membership.Level),
            }),
    ];

    /// <summary>
    /// The group of a given name, or nil where this server keeps none. This is how a
    /// script asks whether the group it wants is already there.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="name">Name of the group.</param>
    [LuaFunction("find")]
    public GroupPayload? Find(ScriptOrigin origin, string name) =>
        groups.Find(name) is { } group ? groups.Described(group) : null;

    /// <summary>
    /// Makes a chat group and answers with it, including the number the server gave
    /// it. Saved with the rest of the player data, so it survives a restart and a
    /// script's body should ask <c>find</c> before making one again.
    /// </summary>
    /// <remarks>
    /// Nobody is in it. <c>join</c> is what puts players there, including whoever the
    /// group is said to belong to.
    /// </remarks>
    /// <param name="origin">Script line making it.</param>
    /// <param name="group">What to call it, who owns it, and who may walk in.</param>
    [LuaFunction("add")]
    public GroupPayload Add(ScriptOrigin origin, GroupSpec group) => groups.Add(group, origin);

    /// <summary>
    /// Takes a chat group away, and takes it off everybody who is connected.
    /// </summary>
    /// <remarks>
    /// A member who is offline keeps a membership naming a group that is gone. The
    /// game's own disband leaves the same ones behind, and what reads a membership
    /// skips one it cannot resolve, so this costs a stale line in a file rather than
    /// anything a player sees.
    /// </remarks>
    /// <param name="origin">Script line taking it away.</param>
    /// <param name="name">Name of the group.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, string name) => groups.Remove(name, origin);

    /// <summary>
    /// Puts a player in a group, or changes what they are in one they are already in.
    /// </summary>
    /// <remarks>
    /// This is the server putting them there rather than them asking, so
    /// <c>joinPolicy</c> does not gate it: an invite-only group takes whoever a script
    /// puts in it.
    ///
    /// Answers for a player who is not connected, so a script may sort out a group
    /// before the people in it arrive. Somebody put in while they are connected reads
    /// messages sent to the group at once, but may not have a tab to read them in
    /// until they next connect.
    /// </remarks>
    /// <param name="origin">Script line putting them there.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="name">Name of the group.</param>
    /// <param name="standing">What they are in it. A plain member when omitted.</param>
    [LuaFunction("join")]
    public void Join(ScriptOrigin origin, string player, string name, EnumGroupStanding? standing) =>
        groups.Join(player, name, standing ?? EnumGroupStanding.Member, origin);

    /// <summary>
    /// Takes a player out of a group. Somebody who was not in it is left as they were
    /// rather than being an error.
    /// </summary>
    /// <param name="origin">Script line taking them out.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="name">Name of the group.</param>
    [LuaFunction("leave")]
    public void Leave(ScriptOrigin origin, string player, string name) =>
        groups.Leave(player, name, origin);

    /// <summary>
    /// Changes who may walk into a group with the game's own <c>/group join</c>.
    /// </summary>
    /// <remarks>
    /// This decides that and nothing else. It does not gate <c>join</c> here, nor
    /// invites, nor who may read what is said in the group.
    /// </remarks>
    /// <param name="origin">Script line changing it.</param>
    /// <param name="name">Name of the group.</param>
    /// <param name="policy">Whether anybody may walk in.</param>
    [LuaFunction("setJoinPolicy")]
    public void SetJoinPolicy(ScriptOrigin origin, string name, EnumJoinPolicy policy) =>
        groups.SetJoinPolicy(name, policy, origin);

    /// <summary>
    /// Says something in a group's channel, which every member who is connected reads.
    /// </summary>
    /// <remarks>
    /// Delivery is decided by who holds a membership rather than by who has the group
    /// open, so this reaches somebody a script joined a moment ago. Everybody at once
    /// is <c>moontweaks.players.announce</c>; one person is
    /// <c>moontweaks.players.say</c>.
    /// </remarks>
    /// <param name="origin">Script line saying it.</param>
    /// <param name="name">Name of the group.</param>
    /// <param name="message">Text to send.</param>
    [LuaFunction("say")]
    public void Say(ScriptOrigin origin, string name, string message) =>
        api.SendMessageToGroup(groups.Require(name, origin).Uid, message, EnumChatType.Notification);
}
