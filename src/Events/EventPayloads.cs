using MoonTweaks.Api;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace MoonTweaks.Events;

// One shape per kind of thing an event says happened. Sole owner of what a handler
// is given: the table a script reads is written from these fields, and so is the
// reference an editor completes against, so the two cannot describe different events.

/// <summary>Something that happened to one player.</summary>
/// <param name="player">Player it happened to.</param>
[LuaTable("PlayerEvent", Given = true)]
public class PlayerEventPayload(IServerPlayer player)
{
    /// <summary>
    /// Identifier of the player it happened to, which every <c>moontweaks.players</c>
    /// function takes and which stays the same across their sessions.
    /// </summary>
    [LuaField("player")]
    public string Player { get; } = player.PlayerUID;

    /// <summary>Name the player is displayed under, which they may change.</summary>
    [LuaField("playerName")]
    public string PlayerName { get; } = player.PlayerName;
}

/// <summary>Something a player did to one block, and where it stood.</summary>
/// <param name="player">Player who did it.</param>
/// <param name="at">Where the block stood.</param>
/// <param name="block">
/// Block the event is about, supplied rather than looked up: which block that is
/// depends on what the event did to it, and a broken one no longer stands there.
/// </param>
[LuaTable("BlockEvent", Given = true)]
public sealed class BlockEventPayload(IServerPlayer player, BlockPos? at, Block? block)
    : PlayerEventPayload(player)
{
    /// <summary>
    /// Code of the block the event is about. Nil for a block whose code could not be
    /// read, which is what a position outside the loaded world leaves behind.
    /// </summary>
    [LuaField("block")]
    [LuaSuggests(SuggestionSets.AssetCode)]
    public string? Block { get; } = block?.Code?.ToString();

    /// <summary>Where the block stood, east to west.</summary>
    [LuaField("x")]
    public int X { get; } = at?.X ?? 0;

    /// <summary>Where the block stood, from the world's floor upwards.</summary>
    [LuaField("y")]
    public int Y { get; } = at?.Y ?? 0;

    /// <summary>Where the block stood, north to south.</summary>
    [LuaField("z")]
    public int Z { get; } = at?.Z ?? 0;
}
