using MoonTweaks.Api;
using MoonTweaks.Scripting;

namespace MoonTweaks.Events;

/// <summary>
/// Things that happen while a server runs, which a script may react to. Every
/// handler is given one table describing what happened.
/// </summary>
/// <remarks>
/// Only events the game raises on its main thread appear here. The interpreter is
/// not thread safe, so the ones it raises on its chunk, spawn and physics threads
/// are deliberately absent rather than offered and unsafe.
/// </remarks>
[LuaModule("moontweaks.events")]
public sealed class EventDomain(ScriptEvents events)
{
    /// <summary>
    /// Called after a player uses a block, with <c>player</c>, <c>playerName</c>,
    /// <c>block</c> and the <c>x</c>, <c>y</c> and <c>z</c> of what they used.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didUseBlock")]
    public void DidUseBlock(ScriptOrigin origin, ScriptValue.Func handler) =>
        events.On(ScriptEvents.DidUseBlock, origin, handler, events.SubscribeDidUseBlock);

    /// <summary>
    /// Called after a player breaks a block, with <c>player</c>, <c>playerName</c>,
    /// <c>block</c> and the <c>x</c>, <c>y</c> and <c>z</c> of what they broke.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didBreakBlock")]
    public void DidBreakBlock(ScriptOrigin origin, ScriptValue.Func handler) =>
        events.On(ScriptEvents.DidBreakBlock, origin, handler, events.SubscribeDidBreakBlock);

    /// <summary>
    /// Called when a player joins, with <c>player</c> and <c>playerName</c>.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerJoin")]
    public void PlayerJoin(ScriptOrigin origin, ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerJoin, origin, handler, events.SubscribePlayerJoin);

    /// <summary>
    /// Called when a player dies, with <c>player</c> and <c>playerName</c>.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerDeath")]
    public void PlayerDeath(ScriptOrigin origin, ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerDeath, origin, handler, events.SubscribePlayerDeath);

    /// <summary>
    /// Called when a player respawns, with <c>player</c> and <c>playerName</c>.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerRespawn")]
    public void PlayerRespawn(ScriptOrigin origin, ScriptValue.Func handler) =>
        events.On(ScriptEvents.PlayerRespawn, origin, handler, events.SubscribePlayerRespawn);
}
