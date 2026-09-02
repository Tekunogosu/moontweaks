using MoonTweaks.Api;
using MoonTweaks.Players;
using MoonTweaks.Scripting;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace MoonTweaks.GameSystems;

/// <summary>
/// Block reinforcement: what a player has protected, how much of that protection is
/// left, and what a script may add or take away.
/// </summary>
/// <remarks>
/// This is the survival mod's own protection rather than the land claims
/// <c>moontweaks.world.testAccess</c> asks about. A claim covers a region and answers
/// who may build in it; a reinforcement is on one block and answers how much work it
/// takes to get through, and the two are enforced separately.
///
/// Only a block the game lets a player reinforce can be reinforced by a script — the
/// game decides that by the behaviour on the block, so <c>strengthen</c> answers
/// false on a block that has none rather than protecting it anyway.
/// </remarks>
/// <example>
/// <code>
/// local reinforce = moontweaks.reinforce
///
/// -- Protecting what a script builds, in the name of whoever asked for it.
/// moontweaks.commands.add {
///   name = "claimblock",
///   description = "Reinforce the block you are looking at",
///   requiresPlayer = true,
///   handler = function(e)
///     local at = moontweaks.players.looking(e.player)
///     if not at then return { error = "you are not looking at a block." } end
///
///     if reinforce.strengthen(at.x, at.y, at.z, e.player, 100) then
///       return "Reinforced."
///     end
///     return { error = "that block cannot be reinforced." }
///   end,
/// }
///
/// -- Reading what is already there.
/// moontweaks.events.didUseBlock(function(e)
///   local held = reinforce.at(e.x, e.y, e.z)
///   if held then
///     moontweaks.players.say(e.player,
///       ("That belongs to %s and has %d strength left."):format(held.playerName, held.strength))
///   end
/// end)
/// </code>
/// </example>
[LuaModule("moontweaks.reinforce")]
public sealed class ReinforceDomain(GameSystems systems, PlayerAccess players)
{
    /// <summary>Whether this server has the block reinforcement system at all.</summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("available")]
    public bool Available(ScriptOrigin origin) => systems.Has<ModSystemBlockReinforcement>();

    /// <summary>
    /// What protects a block, or nil where nothing does. Nil is also the answer for a
    /// block that was reinforced and has since been worn down to nothing.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The block, east to west.</param>
    /// <param name="y">The block, from the world's floor upwards.</param>
    /// <param name="z">The block, north to south.</param>
    [LuaFunction("at")]
    public ReinforcementPayload? At(ScriptOrigin origin, int x, int y, int z) =>
        Reinforcement("reinforce.at", origin).GetReinforcment(new BlockPos(x, y, z)) is { } held
            ? new ReinforcementPayload(held)
            : null;

    /// <summary>Whether anything at all protects a block.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The block, east to west.</param>
    /// <param name="y">The block, from the world's floor upwards.</param>
    /// <param name="z">The block, north to south.</param>
    [LuaFunction("isReinforced")]
    public bool IsReinforced(ScriptOrigin origin, int x, int y, int z) =>
        Reinforcement("reinforce.isReinforced", origin).IsReinforced(new BlockPos(x, y, z));

    /// <summary>
    /// Protects a block in a player's name, and says whether it took. False means the
    /// game does not let that block be reinforced, or that it already is.
    /// </summary>
    /// <remarks>
    /// The player is who it belongs to rather than who is doing it, so a script may
    /// protect something on somebody's behalf without them standing there. Nothing is
    /// taken out of their inventory for it: the material a player would have spent is
    /// the game's own rule for its own command, and a script deciding to charge for
    /// this charges through <c>moontweaks.inventory</c>.
    /// </remarks>
    /// <param name="origin">Script line reinforcing it.</param>
    /// <param name="x">The block, east to west.</param>
    /// <param name="y">The block, from the world's floor upwards.</param>
    /// <param name="z">The block, north to south.</param>
    /// <param name="player">Identifier of the player it belongs to.</param>
    /// <param name="strength">How much protection to put on it.</param>
    [LuaFunction("strengthen")]
    public bool Strengthen(
        ScriptOrigin origin, int x, int y, int z, string player, int strength) =>
        Reinforcement("reinforce.strengthen", origin).StrengthenBlock(
            new BlockPos(x, y, z), players.Find(player, origin), strength);

    /// <summary>
    /// Takes protection off a block, as breaking through it does. Wearing it down to
    /// nothing leaves the block unprotected.
    /// </summary>
    /// <param name="origin">Script line wearing it down.</param>
    /// <param name="x">The block, east to west.</param>
    /// <param name="y">The block, from the world's floor upwards.</param>
    /// <param name="z">The block, north to south.</param>
    /// <param name="strength">How much protection to take off.</param>
    [LuaFunction("consume")]
    public void Consume(ScriptOrigin origin, int x, int y, int z, int strength) =>
        Reinforcement("reinforce.consume", origin).ConsumeStrength(new BlockPos(x, y, z), strength);

    /// <summary>
    /// Takes every trace of protection off a block at once, whoever it belonged to.
    /// </summary>
    /// <param name="origin">Script line clearing it.</param>
    /// <param name="x">The block, east to west.</param>
    /// <param name="y">The block, from the world's floor upwards.</param>
    /// <param name="z">The block, north to south.</param>
    [LuaFunction("clear")]
    public void Clear(ScriptOrigin origin, int x, int y, int z) =>
        Reinforcement("reinforce.clear", origin).ClearReinforcement(new BlockPos(x, y, z));

    /// <summary>
    /// Whether a block is locked against a player opening or using it. A block that
    /// is merely reinforced is not locked: locking is the separate thing a lock is put
    /// on it for.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The block, east to west.</param>
    /// <param name="y">The block, from the world's floor upwards.</param>
    /// <param name="z">The block, north to south.</param>
    /// <param name="player">Identifier of the player asking about.</param>
    [LuaFunction("isLockedFor")]
    public bool IsLockedFor(ScriptOrigin origin, int x, int y, int z, string player) =>
        Reinforcement("reinforce.isLockedFor", origin)
            .IsLockedForInteract(new BlockPos(x, y, z), players.Find(player, origin));

    /// <summary>The reinforcement system, or a failure naming the mod that declares it.</summary>
    private ModSystemBlockReinforcement Reinforcement(string what, ScriptOrigin origin) =>
        systems.Required<ModSystemBlockReinforcement>("survival", what, origin);
}

/// <summary>What protects one block.</summary>
/// <param name="held">What the reinforcement system said.</param>
[LuaTable("Reinforcement", Given = true)]
public sealed class ReinforcementPayload(BlockReinforcement held)
{
    /// <summary>How much protection is left on it.</summary>
    [LuaField("strength")]
    public int Strength { get; } = held.Strength;

    /// <summary>
    /// Identifier of the player it belongs to, which every <c>moontweaks.players</c>
    /// function takes. Nil where it belongs to a group rather than a person.
    /// </summary>
    [LuaField("player")]
    public string? Player { get; } = held.PlayerUID;

    /// <summary>The name that player was last seen under, for saying who it belongs to.</summary>
    [LuaField("playerName")]
    public string? PlayerName { get; } = held.LastPlayername;

    /// <summary>Whether a lock has been put on it as well, so it cannot be opened.</summary>
    [LuaField("locked")]
    public bool Locked { get; } = held.Locked;

    /// <summary>Code of the key that opens it, or nil where it is not locked.</summary>
    [LuaField("lockedBy")]
    public string? LockedBy { get; } = held.LockedByItemCode;

    /// <summary>The group it belongs to, or 0 where it belongs to a player.</summary>
    [LuaField("group")]
    public int Group { get; } = held.GroupUid;

    /// <summary>The name that group was last seen under.</summary>
    [LuaField("groupName")]
    public string? GroupName { get; } = held.LastGroupname;
}
