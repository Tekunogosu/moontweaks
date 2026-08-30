using MoonTweaks.Api;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace MoonTweaks.Events;

// One shape per kind of thing an event says happened. Sole owner of what a handler
// is given: the table a script reads is written from these fields, and so is the
// reference an editor completes against, so the two cannot describe different events.

/// <summary>
/// What one event says happened, as the handler is given it. Every shape below is
/// one of these, so what may be raised is what has a shape describing it.
/// </summary>
public abstract class EventPayload;

/// <summary>
/// Something that happened to the server rather than to anything in it, which is
/// the whole of what there is to say about it.
/// </summary>
[LuaTable("ServerEvent", Given = true)]
public sealed class ServerEventPayload : EventPayload
{
    /// <summary>The one instance, since the shape holds nothing to tell apart.</summary>
    public static readonly ServerEventPayload Instance = new();

    private ServerEventPayload()
    {
    }
}

/// <summary>Something that happened to one player.</summary>
/// <param name="player">Player it happened to.</param>
[LuaTable("PlayerEvent", Given = true)]
public class PlayerEventPayload(IServerPlayer player) : EventPayload
{
    /// <summary>
    /// Identifier of the player it happened to, which every <c>moontweaks.players</c>
    /// function takes and which stays the same across their sessions.
    /// </summary>
    [LuaField("player")]
    public string Player { get; } = player.PlayerUID;

    /// <summary>
    /// Name the player is displayed under, which they may change. Falls back to their
    /// identifier where the game can no longer say the name, so this is always
    /// something that can be printed.
    /// </summary>
    /// <remarks>
    /// The game reads a player's name off the connection they are on, and answers
    /// null once that connection has gone. A player quitting is the case that reaches
    /// here: <c>playerLeave</c> is raised as the connection is being taken down, and
    /// a handler saying who left would otherwise be handed nothing to say it with.
    /// </remarks>
    [LuaField("playerName")]
    public string PlayerName { get; } = player.PlayerName ?? player.PlayerUID;
}

/// <summary>Something a player did to one block, and where it stood.</summary>
/// <param name="player">Player who did it.</param>
/// <param name="at">Where the block stood.</param>
/// <param name="block">
/// Block the event is about, supplied rather than looked up: which block that is
/// depends on what the event did to it, and a broken one no longer stands there.
/// </param>
[LuaTable("BlockEvent", Given = true)]
public class BlockEventPayload(IServerPlayer player, BlockPos? at, Block? block)
    : PlayerEventPayload(player)
{
    /// <summary>
    /// Code of the block the event is about. Nil for a block whose code could not be
    /// read, which is what a position outside the loaded world leaves behind.
    /// </summary>
    [LuaField("block")]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
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

/// <summary>
/// A block a player put down, and what stood there before it.
/// </summary>
/// <param name="player">Player who placed it.</param>
/// <param name="at">Where it went.</param>
/// <param name="block">Block that now stands there.</param>
/// <param name="replaced">
/// Block it went over, which is air wherever nothing was standing. Supplied rather
/// than looked up: it has already gone by the time a handler runs.
/// </param>
[LuaTable("BlockPlacedEvent", Given = true)]
public sealed class BlockPlacedEventPayload(
    IServerPlayer player, BlockPos? at, Block? block, Block? replaced)
    : BlockEventPayload(player, at, block)
{
    /// <summary>
    /// Code of what stood there before, which is <c>game:air</c> where nothing did.
    /// Nil where the code could not be read.
    /// </summary>
    [LuaField("replaced")]
    [LuaSuggests(SuggestionSets.ASSET_CODE)]
    public string? Replaced { get; } = replaced?.Code?.ToString();
}

/// <summary>One column of chunks the server brought in.</summary>
/// <remarks>
/// A column is every chunk at one place on the map, from the world's floor to its
/// ceiling, so it has no height of its own. Given in both the chunk coordinates the
/// game counts in and the block coordinates everything a script writes counts in.
/// </remarks>
/// <param name="chunkX">Which column, east to west, counted in chunks.</param>
/// <param name="chunkZ">Which column, north to south, counted in chunks.</param>
[LuaTable("ChunkColumnEvent", Given = true)]
public class ChunkColumnEventPayload(int chunkX, int chunkZ) : EventPayload
{
    /// <summary>Which column, east to west, counted in chunks.</summary>
    [LuaField("chunkX")]
    public int ChunkX { get; } = chunkX;

    /// <summary>Which column, north to south, counted in chunks.</summary>
    [LuaField("chunkZ")]
    public int ChunkZ { get; } = chunkZ;

    /// <summary>
    /// The block position of the column's lowest corner, east to west, which is what
    /// every other <c>moontweaks.world</c> function counts in.
    /// </summary>
    [LuaField("x")]
    public int X { get; } = chunkX * GlobalConstants.ChunkSize;

    /// <summary>
    /// The block position of the column's lowest corner, north to south, which is what
    /// every other <c>moontweaks.world</c> function counts in.
    /// </summary>
    [LuaField("z")]
    public int Z { get; } = chunkZ * GlobalConstants.ChunkSize;

    /// <summary>How wide the column is, in blocks, which is the same in both directions.</summary>
    [LuaField("size")]
    public int Size { get; } = GlobalConstants.ChunkSize;
}

/// <summary>One chunk the server let go, which is one layer of a column.</summary>
/// <remarks>
/// The game raises its unload event once per layer rather than once per column,
/// whatever its own name for it suggests, so this carries the height the layer sat at
/// and is named for what actually happens.
/// </remarks>
/// <param name="chunkX">Which chunk, east to west, counted in chunks.</param>
/// <param name="chunkY">Which layer, from the world's floor upwards, counted in chunks.</param>
/// <param name="chunkZ">Which chunk, north to south, counted in chunks.</param>
[LuaTable("ChunkEvent", Given = true)]
public sealed class ChunkEventPayload(int chunkX, int chunkY, int chunkZ)
    : ChunkColumnEventPayload(chunkX, chunkZ)
{
    /// <summary>Which layer, from the world's floor upwards, counted in chunks.</summary>
    [LuaField("chunkY")]
    public int ChunkY { get; } = chunkY;

    /// <summary>The block position of the chunk's lowest corner, from the world's floor upwards.</summary>
    [LuaField("y")]
    public int Y { get; } = chunkY * GlobalConstants.ChunkSize;
}

