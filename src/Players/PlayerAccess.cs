using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace MoonTweaks.Players;

/// <summary>
/// Reaching a player and the parts of them the game keeps elsewhere. Sole owner of
/// that lookup, so the binding surface above it is a list of what a script may do
/// rather than a repetition of how to find anything.
/// </summary>
/// <remarks>
/// A player is named by identifier rather than held, because a handler may run long
/// after the event that gave it one and the player may be gone. Health, hunger and
/// tiredness are behaviours on their entity rather than properties of the player,
/// and an entity may legitimately carry none of them.
/// </remarks>
public sealed class PlayerAccess(ICoreServerAPI api)
{
    /// <summary>The player an identifier names, or a failure saying it names nobody.</summary>
    public IServerPlayer Find(string player, ScriptOrigin origin) =>
        api.World.PlayerByUid(player) as IServerPlayer
        ?? throw new ScriptError(origin, $"no player is connected with the identifier '{player}'");

    /// <summary>
    /// Hands a stack to a player, and says whether all of it reached them. The game
    /// puts it wherever it fits; nowhere to put it is a full inventory rather than a
    /// mistake, so the caller decides what to do about the rest.
    /// </summary>
    /// <remarks>
    /// What the game returns is not the answer. <c>TryGiveItemstack</c> reports
    /// whether it was able to try at all, and says yes even when no slot would take
    /// anything, so it is true for a full inventory as readily as for an empty one.
    /// What it does do is take from the stack it was handed as slots accept it, so
    /// what is left in that stack afterwards is how much never arrived.
    /// </remarks>
    public bool Give(string player, ItemStack stack, ScriptOrigin origin)
    {
        Find(player, origin).InventoryManager.TryGiveItemstack(stack, true);
        return stack.StackSize == 0;
    }

    /// <summary>How much punishment a player can take, and has taken.</summary>
    public EntityBehaviorHealth Health(string player, ScriptOrigin origin) =>
        Behaviour<EntityBehaviorHealth>(player, origin, "health");

    /// <summary>How well fed a player is.</summary>
    public EntityBehaviorHunger Hunger(string player, ScriptOrigin origin) =>
        Behaviour<EntityBehaviorHunger>(player, origin, "hunger");

    /// <summary>How much sleep a player needs.</summary>
    public EntityBehaviorTiredness Tiredness(string player, ScriptOrigin origin) =>
        Behaviour<EntityBehaviorTiredness>(player, origin, "tiredness");

    /// <summary>
    /// One behaviour of a player's entity. Named in the failure rather than returned
    /// as nothing, because a script asking for hunger on something that cannot eat
    /// has made a mistake worth reading.
    /// </summary>
    private TBehaviour Behaviour<TBehaviour>(string player, ScriptOrigin origin, string what)
        where TBehaviour : EntityBehavior =>
        Find(player, origin).Entity.GetBehavior<TBehaviour>()
        ?? throw new ScriptError(origin, $"'{player}' has no {what} to read or change");

    /// <summary>
    /// Every player on the server right now, as the identifiers everything else here
    /// takes. This is the only source of one that is not an event, so it is what lets
    /// anything be addressed to everybody rather than to whoever happened to act.
    /// </summary>
    public IReadOnlyList<string> Online() =>
        [.. api.World.AllOnlinePlayers.Select(player => player.PlayerUID)];

    /// <summary>
    /// Whether an identifier names somebody who is here. A script holding one it
    /// remembered earlier asks this before doing anything that needs them present.
    /// </summary>
    public bool IsOnline(string player) => api.World.PlayerByUid(player) is not null;

    /// <summary>
    /// The identifier of whoever last went by a name, whether or not they are here
    /// now, or nil where the server has never seen that name.
    /// </summary>
    /// <remarks>
    /// Names are how people refer to each other and identifiers are how the server
    /// does, so this is the bridge a command typed by hand needs. It answers for
    /// somebody offline, which most of what can be done with the answer does not:
    /// what is stored against them can still be read and written, but nothing that
    /// touches their body can.
    /// </remarks>
    public string? UidOf(string name) =>
        api.PlayerData.GetPlayerDataByLastKnownName(name)?.PlayerUID;

    /// <summary>Sends one message to every chat group on the server, so everybody sees it once.</summary>
    public void Announce(string message) =>
        api.BroadcastMessageToAllGroups(message, EnumChatType.Notification);

    /// <summary>
    /// What a player's abilities currently come to, after everything contributing to
    /// them is added up. An ability nothing has touched reads 1.
    /// </summary>
    public float Stat(string player, string stat, ScriptOrigin origin) =>
        Find(player, origin).Entity.Stats.GetBlended(stat);

    /// <summary>Adds or replaces one named contribution to an ability.</summary>
    public void SetStat(StatSpec spec, ScriptOrigin origin) =>
        Find(spec.Player, origin).Entity.Stats
            .Set(spec.Stat, Scoped(spec.Name), (float)spec.Value, spec.Persistent);

    /// <summary>Takes back one named contribution, leaving every other alone.</summary>
    public void ClearStat(string player, string stat, string name, ScriptOrigin origin) =>
        Find(player, origin).Entity.Stats.Remove(stat, Scoped(name));

    /// <summary>
    /// Contributions are held under this mod's own prefix, so a script cannot take
    /// back one the game or another mod is holding.
    /// </summary>
    private static string Scoped(string name) => $"moontweaks:{name}";

    /// <summary>The block a player has their cursor on, or nothing where they point at nothing.</summary>
    public LookingPayload? Looking(string player, ScriptOrigin origin)
    {
        if (Find(player, origin).CurrentBlockSelection is not { Position: { } at } selection) return null;

        return new LookingPayload(
            at.X, at.Y, at.Z,
            selection.Block?.Code?.ToString(),
            Face(selection.Face));
    }

    /// <summary>
    /// Which side of a block a selection names. The game holds a face as an object
    /// rather than as one of a set, so it is matched back by the name it carries.
    /// </summary>
    private static EnumFaceKind? Face(Vintagestory.API.MathTools.BlockFacing? facing) =>
        facing is not null && Enum.TryParse<EnumFaceKind>(facing.Code, ignoreCase: true, out var side)
            ? side
            : null;
}
