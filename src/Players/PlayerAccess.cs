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
}
