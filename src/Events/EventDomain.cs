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
        events.On(ScriptEvents.DidUseBlock, origin, handler, events.SubscribeDidUseBlock);

    /// <summary>
    /// Called after a player breaks a block. The block is the one that stood there:
    /// it has already gone by the time a handler runs.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didBreakBlock")]
    public void DidBreakBlock(
        ScriptOrigin origin, [LuaPayload(typeof(BlockEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.DidBreakBlock, origin, handler, events.SubscribeDidBreakBlock);

    /// <summary>Called when a player joins.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerJoin")]
    public void PlayerJoin(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerJoin, origin, handler, events.SubscribePlayerJoin);

    /// <summary>Called when a player dies.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerDeath")]
    public void PlayerDeath(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerDeath, origin, handler, events.SubscribePlayerDeath);

    /// <summary>Called when a player respawns.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerRespawn")]
    public void PlayerRespawn(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerRespawn, origin, handler, events.SubscribePlayerRespawn);

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
        events.On(ScriptEvents.PlayerCreate, origin, handler, events.SubscribePlayerCreate);

    /// <summary>Called once a joining player is in the world and has been welcomed.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerNowPlaying")]
    public void PlayerNowPlaying(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerNowPlaying, origin, handler, events.SubscribePlayerNowPlaying);

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
        events.On(ScriptEvents.PlayerReady, origin, handler, events.SubscribePlayerReady);

    /// <summary>
    /// Called when a player quits of their own accord, before they are removed. One
    /// who was kicked or who lost their connection raises <c>playerDisconnect</c> only.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerLeave")]
    public void PlayerLeave(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerLeave, origin, handler, events.SubscribePlayerLeave);

    /// <summary>
    /// Called as a player is removed, however they went: a quit, a kick and a lost
    /// connection all reach here, so this is the one that always runs.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerDisconnect")]
    public void PlayerDisconnect(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerDisconnect, origin, handler, events.SubscribePlayerDisconnect);

    /// <summary>
    /// Called after a player changes game mode, so asking them their mode gives the
    /// one they changed to.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerSwitchGameMode")]
    public void PlayerSwitchGameMode(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerSwitchGameMode, origin, handler, events.SubscribePlayerSwitchGameMode);

    /// <summary>
    /// Called once the save game has been read, which is after every script has run.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("saveGameLoaded")]
    public void SaveGameLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.SaveGameLoaded, origin, handler, events.SubscribeSaveGameLoaded);

    /// <summary>
    /// Called on the one start where the world is brand new, immediately before
    /// <c>saveGameLoaded</c>. Never called again for that world.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("saveGameCreated")]
    public void SaveGameCreated(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.SaveGameCreated, origin, handler, events.SubscribeSaveGameCreated);

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
        events.On(ScriptEvents.GameWorldSave, origin, handler, events.SubscribeGameWorldSave);

    /// <summary>
    /// Called once the world generators are starting, which is the last thing a
    /// server does before it begins ticking.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("worldgenStartup")]
    public void WorldgenStartup(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.WorldgenStartup, origin, handler, events.SubscribeWorldgenStartup);

    /// <summary>
    /// Called when a server that had suspended itself for want of players wakes up
    /// again. Servers that never stand by never raise it.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("serverResume")]
    public void ServerResume(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        events.On(ScriptEvents.ServerResume, origin, handler, events.SubscribeServerResume);
}
