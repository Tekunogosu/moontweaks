using MoonTweaks.Api;
using MoonTweaks.Scripting;

namespace MoonTweaks.Events;

/// <summary>
/// Things that happen while a server runs, which a script may react to. Every
/// handler is given one table describing what happened, whose shape each function
/// below names.
/// </summary>
/// <remarks>
/// A handler runs on the thread the server ticks on, whichever thread the game
/// raised the event on. One that the game raises elsewhere — a creature spawned by
/// chunk generation, a packet arriving from a rider — is therefore called on the
/// tick after it happened, and each says so.
/// </remarks>
/// <example>
/// <code>
/// local events = moontweaks.events
///
/// events.playerJoin(function(e)
///   moontweaks.players.say(e.player, "welcome back, " .. e.playerName)
/// end)
///
/// events.didBreakBlock(function(e)
///   if e.block == "game:crock-burned" then
///     moontweaks.log.info(("%s broke a crock at %d %d %d"):format(e.playerName, e.x, e.y, e.z))
///   end
/// end)
/// </code>
/// </example>
[LuaModule("moontweaks.events")]
public sealed class EventDomain(ScriptEvents events)
{
    /// <summary>
    /// Called before the server lets somebody act on a place, and answered by what the
    /// handler returns: one of the same words <c>world.testAccess</c> reads back, or
    /// nothing to leave the decision alone.
    /// </summary>
    /// <remarks>
    /// The one event a handler decides rather than watches. It is asked for every
    /// block a player breaks and every block they use, after the land claim check and
    /// with that check's answer on <c>e.allowed</c>, so a handler is the last word
    /// rather than the first. Answering <c>"granted"</c> therefore overrides a claim
    /// and opens somebody's land to whoever asked — a handler meaning only to refuse
    /// should return nothing wherever it does not mean to refuse.
    ///
    /// The server asks this constantly, so a handler here is on a hot path in a way no
    /// other event is: keep it to arithmetic and a table lookup, and read nothing that
    /// searches the world.
    ///
    /// Another mod may ask the same question from a thread the server does not tick
    /// on, and a script cannot answer there. Those asks are left to the server and
    /// reported once, so a protection written here holds for players and may not hold
    /// against another mod reaching past them.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time somebody is tested, and answers.</param>
    [LuaFunction("testBlockAccess")]
    public void TestBlockAccess(
        ScriptOrigin origin,
        [LuaPayload(typeof(AccessTestEventPayload), Returns = "EnumAccessResponse|nil")]
        ScriptValue.Func handler) =>
        events.OnTestBlockAccess(origin, handler);

    /// <summary>
    /// Called before anybody sees what a player said, and answered by what the handler
    /// returns: a string to say something else instead, <c>false</c> to say nothing at
    /// all, <c>true</c> to say it after all, or nothing to leave it alone.
    /// </summary>
    /// <remarks>
    /// This is how chat is filtered, prefixed, muted or routed somewhere else. The
    /// message a handler is given is what the handler before it left rather than what
    /// was typed, and the last answer stands — the game's own rule, followed exactly.
    ///
    /// That rule has a sharp edge worth planning around. Answering <c>true</c> puts
    /// back a message an earlier handler swallowed, so a script that mutes players can
    /// be undone by a later script that knows nothing about muting. Scripts run in
    /// name order, so anything that must have the last word belongs in a file that
    /// sorts last, and a handler that does not mean to interfere should return nothing
    /// rather than <c>true</c>.
    ///
    /// The message reaches the group named on the event, so a handler that means to
    /// send it somewhere else swallows it and calls <c>moontweaks.groups.say</c>.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time somebody says something, and answers.</param>
    [LuaFunction("playerChat")]
    public void PlayerChat(
        ScriptOrigin origin,
        [LuaPayload(typeof(ChatEventPayload), Returns = "string|boolean|nil")]
        ScriptValue.Func handler) =>
        events.OnPlayerChat(origin, handler);

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

    /// <summary>
    /// Called once a region of the map has come in, whether it was read from disk or
    /// generated. A region is a square of chunk columns holding the maps a world is
    /// grown from, so this is raised far less often than <c>chunkColumnLoaded</c> and
    /// covers far more ground.
    /// </summary>
    /// <remarks>
    /// Called once per region rather than once per column standing on it: the game
    /// raises its own event every time a column asks for the region beneath it, and
    /// only the ask that brought the region in reaches a handler here.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("mapRegionLoaded")]
    public void MapRegionLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(MapRegionEventPayload))] ScriptValue.Func handler) =>
        events.OnMapRegionLoaded(origin, handler);

    /// <summary>
    /// Called as a region of the map is let go, which is where anything remembered
    /// about that stretch of the world should be forgotten.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("mapRegionUnloaded")]
    public void MapRegionUnloaded(
        ScriptOrigin origin, [LuaPayload(typeof(MapRegionEventPayload))] ScriptValue.Func handler) =>
        events.OnMapRegionUnloaded(origin, handler);

    // The seven below reach things that are not players. The game raises them wherever
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
    /// returning rather than a new one appearing, so this is where whatever was
    /// remembered about it is put back.
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

    /// <summary>
    /// Called when a mount's rider changes the pace it is ridden at. The table
    /// describes the mount rather than the rider, so <c>id</c> is the horse and
    /// <c>gait</c> is what it is now doing.
    /// </summary>
    /// <remarks>
    /// Raised only on a change. A rider's client reports its mount's gait several
    /// times a second whether or not it moved, and a report saying what the last one
    /// said goes no further, so a handler here is called once per change of pace
    /// rather than once per packet.
    ///
    /// Only a mount whose rider's own client reports its position raises this at all,
    /// so the pace is one a rider chose rather than one the server worked out. Nothing is raised for a creature the server is walking itself.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("mountGaitChanged")]
    public void MountGaitChanged(
        ScriptOrigin origin, [LuaPayload(typeof(MountGaitEventPayload))] ScriptValue.Func handler) =>
        events.OnMountGaitChanged(origin, handler);

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
