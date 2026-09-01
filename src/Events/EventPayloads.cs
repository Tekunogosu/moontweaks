using MoonTweaks.Api;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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
/// Something a player said, before anybody has seen it. A handler is asked before the
/// message is delivered and what it answers decides what is delivered, or whether
/// anything is.
/// </summary>
/// <param name="player">Player who said it.</param>
/// <param name="group">Channel they said it in.</param>
/// <param name="message">
/// What will be delivered as this handler is asked, which is what the handler before
/// it left rather than necessarily what was typed.
/// </param>
/// <param name="consumed">Whether anybody is currently going to see it.</param>
[LuaTable("ChatEvent", Given = true)]
public sealed class ChatEventPayload(IServerPlayer player, int group, string message, bool consumed)
    : PlayerEventPayload(player)
{
    /// <summary>
    /// Channel it was said in. Zero is general chat; anything else is a group, and is
    /// the number <c>moontweaks.groups.of</c> reports beside that group's name.
    /// </summary>
    [LuaField("group")]
    public int Group { get; } = group;

    /// <summary>
    /// What will be said. Handlers are asked in turn and each is given what the one
    /// before it left, so this is the message as it stands rather than as it was
    /// typed.
    /// </summary>
    [LuaField("message")]
    public string Message { get; } = message;

    /// <summary>
    /// Whether anybody is still going to see it. False once a handler has swallowed
    /// it, which a later handler may undo by answering <c>true</c>.
    /// </summary>
    [LuaField("delivered")]
    public bool Delivered { get; } = !consumed;
}

/// <summary>
/// Somebody about to act on a place, and what the server has decided so far. A
/// handler is asked before the act happens and its answer is what decides it.
/// </summary>
/// <param name="player">Player asking to act.</param>
/// <param name="at">Where they want to act.</param>
/// <param name="what">What they want to do there.</param>
/// <param name="allowed">
/// What the server has decided so far: the land claim check, and every handler asked
/// before this one. A handler answering nothing leaves this standing.
/// </param>
/// <param name="claimant">Who is holding the place, where anybody is.</param>
[LuaTable("AccessTestEvent", Given = true)]
public sealed class AccessTestEventPayload(
    IServerPlayer player, BlockPos? at, EnumAccessKind what, EnumAccessResponse allowed, string? claimant)
    : PlayerEventPayload(player)
{
    /// <summary>Which block they want to act on, east to west.</summary>
    [LuaField("x")]
    public int X { get; } = at?.X ?? 0;

    /// <summary>Which block, from the world's floor upwards.</summary>
    [LuaField("y")]
    public int Y { get; } = at?.Y ?? 0;

    /// <summary>Which block, north to south.</summary>
    [LuaField("z")]
    public int Z { get; } = at?.Z ?? 0;

    /// <summary>What they are asking to do there.</summary>
    [LuaField("what")]
    public EnumAccessKind What { get; } = what;

    /// <summary>
    /// What the answer is at the moment this handler is asked, which is the claim
    /// check and every handler already asked. Read it rather than assuming a refusal
    /// is this handler's to make: answering <c>granted</c> here overrides a land
    /// claim.
    /// </summary>
    [LuaField("allowed")]
    public EnumAccessResponse Allowed { get; } = allowed;

    /// <summary>
    /// Name of whoever is holding the place, where a claim is what stopped it. Nil
    /// where nothing is holding it or where the game could not say who.
    /// </summary>
    [LuaField("claimant")]
    public string? Claimant { get; } = claimant;
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

/// <summary>One region of the map the server brought in or let go.</summary>
/// <remarks>
/// A region is a square of chunk columns holding the maps a world is generated from
/// — where forests, ores and climate fall — so it covers far more ground than a
/// chunk and there are correspondingly few of them. Given in both the region
/// coordinates the game counts in and the block coordinates everything a script
/// writes counts in.
/// </remarks>
/// <param name="regionX">Which region, east to west, counted in regions.</param>
/// <param name="regionZ">Which region, north to south, counted in regions.</param>
/// <param name="size">How wide a region is, in blocks, which the server decides.</param>
[LuaTable("MapRegionEvent", Given = true)]
public sealed class MapRegionEventPayload(int regionX, int regionZ, int size) : EventPayload
{
    /// <summary>Which region, east to west, counted in regions.</summary>
    [LuaField("regionX")]
    public int RegionX { get; } = regionX;

    /// <summary>Which region, north to south, counted in regions.</summary>
    [LuaField("regionZ")]
    public int RegionZ { get; } = regionZ;

    /// <summary>
    /// The block position of the region's lowest corner, east to west, which is what
    /// every other <c>moontweaks.world</c> function counts in.
    /// </summary>
    [LuaField("x")]
    public int X { get; } = regionX * size;

    /// <summary>
    /// The block position of the region's lowest corner, north to south, which is what
    /// every other <c>moontweaks.world</c> function counts in.
    /// </summary>
    [LuaField("z")]
    public int Z { get; } = regionZ * size;

    /// <summary>How wide the region is, in blocks, which is the same in both directions.</summary>
    [LuaField("size")]
    public int Size { get; } = size;
}

/// <summary>
/// Something alive, or something lying on the ground, that the world did something
/// with.
/// </summary>
/// <remarks>
/// Read from the entity when the event happened rather than when the handler runs.
/// These events may arrive on a thread of the game's own, and the handler is called on
/// the next tick of the main one, by which time the entity may have moved or gone —
/// so what a handler is told is what was true at the moment, and <c>id</c> is what it
/// asks about now.
/// </remarks>
/// <param name="entity">The entity it happened to.</param>
[LuaTable("EntityEvent", Given = true)]
public class EntityEventPayload(Entity entity) : EventPayload
{
    /// <summary>
    /// The server's own identifier for it, which every <c>moontweaks.entities</c>
    /// function takes. Ask <c>entities.isLoaded</c> before reaching for it: a handler
    /// runs after the event, and the entity may already be gone.
    /// </summary>
    [LuaField("id")]
    public double Id { get; } = entity.EntityId;

    /// <summary>Entity code, such as <c>game:wolf-male</c>.</summary>
    [LuaField("code")]
    public string Code { get; } = entity.Code?.ToString() ?? "";

    /// <summary>What it was called, which is its name tag where it had one.</summary>
    [LuaField("name")]
    public string Name { get; } = entity.GetName() ?? "";

    /// <summary>Where it was, east to west.</summary>
    [LuaField("x")]
    public double X { get; } = entity.Pos.X;

    /// <summary>Where it was, from the world's floor upwards.</summary>
    [LuaField("y")]
    public double Y { get; } = entity.Pos.Y;

    /// <summary>Where it was, north to south.</summary>
    [LuaField("z")]
    public double Z { get; } = entity.Pos.Z;

    /// <summary>
    /// Identifier of the player this was, or nil where it was not one. A player's body
    /// is an entity like any other, so these events see them too.
    /// </summary>
    [LuaField("player")]
    public string? Player { get; } = (entity as EntityPlayer)?.PlayerUID;
}

/// <summary>Something that died, and what killed it.</summary>
/// <param name="entity">The entity that died.</param>
/// <param name="cause">What killed it, where the game said.</param>
[LuaTable("EntityDeathEvent", Given = true)]
public sealed class EntityDeathEventPayload(Entity entity, DamageSource? cause)
    : EntityEventPayload(entity)
{
    // Whoever is answerable for the damage. The game fills CauseEntity in for a
    // projectile alone, naming whoever threw it, and leaves it null for a melee blow,
    // which names the attacker in SourceEntity instead. Neither field answers on its
    // own; GetCauseEntity is the one that answers for both.
    private readonly Entity? killer = cause?.GetCauseEntity();

    /// <summary>What killed it, or nil where the game named nothing.</summary>
    [LuaField("cause")]
    public EnumHurtKind? Cause { get; } =
        cause is null ? null : ValueSet.As<EnumHurtKind>(cause.Type);

    /// <summary>
    /// Identifier of the player that killed it, or nil where no player did. This is
    /// whoever is responsible rather than what struck the blow, so an arrow names the
    /// archer.
    /// </summary>
    [LuaField("byPlayer")]
    public string? ByPlayer => (killer as EntityPlayer)?.PlayerUID;

    /// <summary>
    /// Identifier of the entity that killed it, or nil where nothing did. Names the
    /// responsible creature rather than the arrow it fired.
    /// </summary>
    [LuaField("byEntity")]
    public double? ByEntity => killer?.EntityId;
}

/// <summary>Something that left the world, and why.</summary>
/// <param name="entity">The entity that left.</param>
/// <param name="reason">Why it left.</param>
[LuaTable("EntityDespawnEvent", Given = true)]
public sealed class EntityDespawnEventPayload(Entity entity, EntityDespawnData? reason)
    : EntityEventPayload(entity)
{
    /// <summary>
    /// Why it went. <c>unload</c> is the one worth checking for: the entity is not gone
    /// from the world, only out of reach until its chunk comes back.
    /// </summary>
    [LuaField("reason")]
    public EnumDespawnKind? Reason { get; } =
        reason is null ? null : ValueSet.As<EnumDespawnKind>(reason.Reason);
}

/// <summary>A mount whose rider changed the pace it is being ridden at.</summary>
/// <param name="mount">The mount itself, rather than whoever is riding it.</param>
/// <param name="gait">The pace it has changed to.</param>
[LuaTable("MountGaitEvent", Given = true)]
public sealed class MountGaitEventPayload(Entity mount, string? gait) : EntityEventPayload(mount)
{
    /// <summary>
    /// What it is now doing, such as <c>walk</c> or <c>gallop</c>. Which paces exist
    /// is the mount's own business, so this is whatever its rideable behaviour named
    /// them, and nil where it named none.
    /// </summary>
    [LuaField("gait")]
    public string? Gait { get; } = gait;
}

/// <summary>Something that got on or off something else.</summary>
/// <param name="entity">The one that mounted or dismounted.</param>
/// <param name="seat">The seat it took, which belongs to whatever it climbed onto.</param>
[LuaTable("EntityMountEvent", Given = true)]
public sealed class EntityMountEventPayload(Entity entity, IMountableSeat? seat)
    : EntityEventPayload(entity)
{
    /// <summary>
    /// Identifier of what it climbed onto — the boat or the animal rather than the
    /// seat. Nil where the game named none.
    /// </summary>
    [LuaField("mount")]
    public double? Mount { get; } = seat?.Entity?.EntityId;

    /// <summary>Which seat of it was taken, where the mount has more than one.</summary>
    [LuaField("seat")]
    public string? Seat { get; } = seat?.SeatId;
}

