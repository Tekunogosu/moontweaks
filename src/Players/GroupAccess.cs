using System.Linq;
using System.Text.RegularExpressions;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace MoonTweaks.Players;

/// <summary>
/// Reaching the chat groups a server keeps. Sole owner of how a script names one —
/// by the name players call it, which is the only handle the game lets a group be
/// looked up by.
/// </summary>
/// <remarks>
/// The game assigns a group its number as it is added and offers no way to find one
/// by that number afterwards, so the number is something a script is told rather than
/// something it uses. Everything here therefore takes the name.
///
/// A group this makes is the same object the game's own <c>/group</c> command makes,
/// filled in the same way and saved by the same code, so the two are interchangeable:
/// a script may disband a group a player created and a player may rename one a script
/// created.
/// </remarks>
/// <param name="api">The running server.</param>
/// <param name="players">
/// How an identifier becomes a player. Borrowed rather than repeated, so an identifier
/// naming nobody reads the same here as everywhere else.
/// </param>
public sealed class GroupAccess(ICoreServerAPI api, PlayerAccess players)
{
    /// <summary>
    /// Characters the game allows in a group name. Refused here rather than at the
    /// point a player next types the name, which is where the game would refuse it.
    /// </summary>
    private static readonly Regex Allowed =
        new($"^[{GlobalConstants.AllowedChatGroupChars}]+$", RegexOptions.Compiled);

    /// <summary>The group of a given name, or nothing where this server keeps none.</summary>
    public PlayerGroup? Find(string name) => api.Groups.GetPlayerGroupByName(name);

    /// <summary>The group of a given name, or a failure naming it.</summary>
    public PlayerGroup Require(string name, ScriptOrigin origin) =>
        Find(name) ?? throw new ScriptError(origin, $"this server has no chat group called '{name}'");

    /// <summary>A group as a script is told about it.</summary>
    public GroupPayload Described(PlayerGroup group) => new()
    {
        Group = group.Uid,
        Name = group.Name ?? "",
        Owner = group.OwnerUID,
        Online = [.. group.OnlinePlayers.Select(member => member.PlayerUID)],
        // Held as a bare string the game compares against one spelling, so anything
        // that is not exactly that spelling means invite only — including nothing,
        // as a group made through the game's own command carries.
        JoinPolicy = group.JoinPolicy == Policy(EnumJoinPolicy.Everyone)
            ? EnumJoinPolicy.Everyone
            : EnumJoinPolicy.InviteOnly,
    };

    /// <summary>Makes a group and answers with it, number and all.</summary>
    public GroupPayload Add(GroupSpec spec, ScriptOrigin origin)
    {
        if (!Allowed.IsMatch(spec.Name))
        {
            throw new ScriptError(origin,
                $"'{spec.Name}' is not a name the game allows a chat group: letters, numbers and "
                + "underscores only");
        }

        if (Find(spec.Name) is not null)
        {
            throw new ScriptError(origin, $"this server already keeps a chat group called '{spec.Name}'");
        }

        var owner = spec.Owner is null ? null : players.Account(spec.Owner, origin).PlayerUID;

        var group = new PlayerGroup
        {
            Name = spec.Name,
            OwnerUID = owner,
            JoinPolicy = Policy(spec.JoinPolicy),
        };

        // Assigns the number itself, overwriting whatever it was handed, which is why
        // nothing above sets one.
        api.Groups.AddPlayerGroup(group);

        // Written after the add because it is built from the number the add assigned.
        // The game does the same thing in the same order for the same reason.
        group.Md5Identifier = GameMath.Md5Hash(group.Uid + (owner ?? ""));

        return Described(group);
    }

    /// <summary>
    /// Takes a group away, and takes it off everybody the server can reach.
    /// </summary>
    /// <remarks>
    /// The memberships of players who are not connected are left behind, because
    /// nothing enumerates the players a server has ever seen. The game's own disband
    /// leaves the same ones, and what reads a membership skips one naming a group that
    /// is gone, so the leftovers cost a stale entry in a file rather than anything a
    /// player sees.
    /// </remarks>
    public void Remove(string name, ScriptOrigin origin)
    {
        var group = Require(name, origin);

        api.Groups.RemovePlayerGroup(group);

        foreach (var player in api.World.AllOnlinePlayers.OfType<IServerPlayer>())
        {
            player.ServerData.PlayerGroupMemberships.Remove(group.Uid);
        }

        group.OnlinePlayers.Clear();
    }

    /// <summary>Puts a player in a group at a given standing, or moves the one they hold.</summary>
    public void Join(string player, string name, EnumGroupStanding standing, ScriptOrigin origin)
    {
        var group = Require(name, origin);
        var data = players.Account(player, origin);

        data.PlayerGroupMemberships[group.Uid] = new PlayerGroupMembership
        {
            GroupUid = group.Uid,
            GroupName = group.Name,
            Level = ValueSet.As<EnumPlayerGroupMemberShip>(standing),
        };

        // Kept in step for the game's own commands, which read this list rather than
        // the memberships when they announce something to a group.
        if (api.World.PlayerByUid(player) is IServerPlayer online && !group.OnlinePlayers.Contains(online))
        {
            group.OnlinePlayers.Add(online);
        }
    }

    /// <summary>Takes a player out of a group, whether or not they were in it.</summary>
    public void Leave(string player, string name, ScriptOrigin origin)
    {
        var group = Require(name, origin);

        players.Account(player, origin).PlayerGroupMemberships.Remove(group.Uid);
        group.OnlinePlayers.RemoveAll(member => member.PlayerUID == player);
    }

    /// <summary>Changes who may walk into a group.</summary>
    public void SetJoinPolicy(string name, EnumJoinPolicy policy, ScriptOrigin origin) =>
        Require(name, origin).JoinPolicy = Policy(policy);

    /// <summary>
    /// The policy as the game spells it. One spelling is compared for and everything
    /// else means invite only, so the two are written out here rather than left to a
    /// name conversion that would agree only by luck.
    /// </summary>
    private static string Policy(EnumJoinPolicy policy) =>
        policy == EnumJoinPolicy.Everyone ? "everyone" : "inviteonly";
}
