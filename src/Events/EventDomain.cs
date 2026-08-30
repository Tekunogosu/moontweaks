using MoonTweaks.Api;
using MoonTweaks.Scripting;

namespace MoonTweaks.Events;

/// <summary>
/// Things that happen while a server runs, which a script may react to. Every
/// handler is given one table describing what happened, whose shape each function
/// below names.
/// </summary>
/// <remarks>
/// Only events the game raises on its main thread appear here. The interpreter is
/// not thread safe, so the ones it raises on its chunk, spawn and physics threads
/// are deliberately absent rather than offered and unsafe.
/// </remarks>
[LuaModule("moontweaks.events")]
public sealed class EventDomain(ScriptEvents events)
{
    /// <summary>Called after a player uses a block, which is left standing.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didUseBlock")]
    public void DidUseBlock(
        ScriptOrigin origin, [LuaPayload(typeof(BlockEventPayload))] ScriptValue.Func handler) =>
        events.OnDidUseBlock(origin, handler);

    /// <summary>
    /// Called after a player breaks a block. The block is the one that stood there:
    /// it has already gone by the time a handler runs.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didBreakBlock")]
    public void DidBreakBlock(
        ScriptOrigin origin, [LuaPayload(typeof(BlockEventPayload))] ScriptValue.Func handler) =>
        events.OnDidBreakBlock(origin, handler);

    /// <summary>
    /// Called after a player puts a block down. <c>block</c> is what now stands there
    /// and <c>replaced</c> is what it went over, which is air where nothing was.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didPlaceBlock")]
    public void DidPlaceBlock(
        ScriptOrigin origin, [LuaPayload(typeof(BlockPlacedEventPayload))] ScriptValue.Func handler) =>
        events.OnDidPlaceBlock(origin, handler);

    /// <summary>
    /// Called after a player changes which hotbar slot they are holding. Ask
    /// <c>moontweaks.inventory.held</c> what is now in their hand — the event says who
    /// changed it rather than carrying the slot, since what is in it is the useful part.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerChangeSlot")]
    public void PlayerChangeSlot(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerChangeSlot(origin, handler);

    /// <summary>
    /// Called once a column of chunks has been brought in and its blocks can be
    /// reached. This is what lets a script act on a place nobody is standing, together
    /// with <c>moontweaks.world.loadChunk</c>.
    /// </summary>
    /// <remarks>
    /// A busy server loads columns constantly, so a handler here runs often. Keep it
    /// short, and decide whether the column is one worth acting on before doing
    /// anything that costs.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("chunkColumnLoaded")]
    public void ChunkColumnLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(ChunkColumnEventPayload))] ScriptValue.Func handler) =>
        events.OnChunkColumnLoaded(origin, handler);

    /// <summary>
    /// Called as one chunk is let go, which is where anything remembered about its
    /// blocks should be forgotten. The blocks themselves are on their way out and
    /// should not be reached.
    /// </summary>
    /// <remarks>
    /// Raised once per chunk rather than once per column, so a column leaving memory
    /// calls this once for every chunk stacked at that place. <c>chunkY</c> says which.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("chunkUnloaded")]
    public void ChunkUnloaded(
        ScriptOrigin origin, [LuaPayload(typeof(ChunkEventPayload))] ScriptValue.Func handler) =>
        events.OnChunkUnloaded(origin, handler);

    // The six below reach things that are not players. The game raises them wherever
    // it happens to be — chunk generation spawns creatures on its own thread — so a
    // handler is called on the tick after the event rather than during it. What it is
    // told is what was true at the moment; what it reaches for wants checking with
    // moontweaks.entities.isLoaded first.

    /// <summary>
    /// Called when something is put into the world, however it got there: generated
    /// with a chunk, bred, or spawned by a script.
    /// </summary>
    /// <remarks>
    /// Runs on the tick after the spawn, so the thing it describes may already be gone
    /// — a creature generated into a chunk nobody stayed near, for instance. Ask
    /// <c>moontweaks.entities.isLoaded</c> before reaching for it.
    ///
    /// Worldgen fills a chunk with creatures at once, so a busy server calls this in
    /// bursts. Decide whether the code is one worth caring about before doing anything
    /// that costs.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entitySpawn")]
    public void EntitySpawn(
        ScriptOrigin origin, [LuaPayload(typeof(EntityEventPayload))] ScriptValue.Func handler) =>
        events.OnEntitySpawn(origin, handler);

    /// <summary>
    /// Called when something comes back with the chunk it was saved in. The
    /// counterpart of a despawn whose reason was <c>unload</c>: the same creature
    /// returning rather than a new one appearing, which is what makes this the place
    /// to put back whatever was remembered about it.
    /// </summary>
    /// <inheritdoc cref="EntitySpawn" path="/remarks"/>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityLoaded")]
    public void EntityLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(EntityEventPayload))] ScriptValue.Func handler) =>
        events.OnEntityLoaded(origin, handler);

    /// <summary>
    /// Called when anything alive dies, rather than players alone. <c>byPlayer</c>
    /// names whoever is responsible where one is, so an arrow names the archer rather
    /// than the arrow.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityDeath")]
    public void EntityDeath(
        ScriptOrigin origin, [LuaPayload(typeof(EntityDeathEventPayload))] ScriptValue.Func handler) =>
        events.OnEntityDeath(origin, handler);

    /// <summary>
    /// Called when something leaves the world, however it went. Read <c>reason</c>
    /// before concluding anything is gone for good: <c>unload</c> means its chunk left
    /// memory and it will be back.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityDespawn")]
    public void EntityDespawn(
        ScriptOrigin origin, [LuaPayload(typeof(EntityDespawnEventPayload))] ScriptValue.Func handler) =>
        events.OnEntityDespawn(origin, handler);

    /// <summary>
    /// Called when something climbs onto something else. <c>id</c> is whoever climbed
    /// on and <c>mount</c> is what they climbed onto.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityMounted")]
    public void EntityMounted(
        ScriptOrigin origin, [LuaPayload(typeof(EntityMountEventPayload))] ScriptValue.Func handler) =>
        events.OnEntityMounted(origin, handler);

    /// <summary>Called when something gets off what it was riding.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityUnmounted")]
    public void EntityUnmounted(
        ScriptOrigin origin, [LuaPayload(typeof(EntityMountEventPayload))] ScriptValue.Func handler) =>
        events.OnEntityUnmounted(origin, handler);

    /// <summary>Called when a player joins.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerJoin")]
    public void PlayerJoin(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerJoin(origin, handler);

    /// <summary>Called when a player dies.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerDeath")]
    public void PlayerDeath(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerDeath(origin, handler);

    /// <summary>Called when a player respawns.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerRespawn")]
    public void PlayerRespawn(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerRespawn(origin, handler);

    /// <summary>
    /// Called the first time a player ever joins this world, before they are welcomed.
    /// Every later join raises <c>playerJoin</c> alone, so this is where anything
    /// given once belongs.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerCreate")]
    public void PlayerCreate(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerCreate(origin, handler);

    /// <summary>Called once a joining player is in the world and has been welcomed.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerNowPlaying")]
    public void PlayerNowPlaying(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerNowPlaying(origin, handler);

    /// <summary>
    /// Called when a joining player's client reports that it has finished. The last
    /// of the three events a join raises, and the one after which the player is
    /// certainly able to be spoken to.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerReady")]
    public void PlayerReady(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerReady(origin, handler);

    /// <summary>
    /// Called when a player quits of their own accord, before they are removed. One
    /// who was kicked or who lost their connection raises <c>playerDisconnect</c> only.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerLeave")]
    public void PlayerLeave(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerLeave(origin, handler);

    /// <summary>
    /// Called as a player is removed, however they went: a quit, a kick and a lost
    /// connection all reach here, so this is the one that always runs.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerDisconnect")]
    public void PlayerDisconnect(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerDisconnect(origin, handler);

    /// <summary>
    /// Called after a player changes game mode, so asking them their mode gives the
    /// one they changed to.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerSwitchGameMode")]
    public void PlayerSwitchGameMode(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.OnPlayerSwitchGameMode(origin, handler);

    /// <summary>
    /// Called once the save game has been read, which is after every script has run.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("saveGameLoaded")]
    public void SaveGameLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.OnSaveGameLoaded(origin, handler);

    /// <summary>
    /// Called on the one start where the world is brand new, immediately before
    /// <c>saveGameLoaded</c>. Never called again for that world.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("saveGameCreated")]
    public void SaveGameCreated(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.OnSaveGameCreated(origin, handler);

    /// <summary>
    /// Called as the world is written to disk, which a server does periodically and
    /// again as it shuts down. Anything a script wants saved with the world should be
    /// written by the time this returns.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("gameWorldSave")]
    public void GameWorldSave(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.OnGameWorldSave(origin, handler);

    /// <summary>
    /// Called once the world generators are starting, which is the last thing a
    /// server does before it begins ticking.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("worldgenStartup")]
    public void WorldgenStartup(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.OnWorldgenStartup(origin, handler);

    /// <summary>
    /// Called when a server that had suspended itself for want of players wakes up
    /// again. Servers that never stand by never raise it.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("serverResume")]
    public void ServerResume(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.OnServerResume(origin, handler);
}
