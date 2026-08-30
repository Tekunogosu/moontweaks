using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace MoonTweaks.Events;

/// <summary>
/// The game events a script asked to hear about, and the only place a script's
/// function is called back. Sole owner of what happens when one of them fails.
/// </summary>
/// <remarks>
/// One method per event, each naming that event and subscribing to the game's own in
/// the same breath. Written that way on purpose: a name and a subscription declared
/// apart have to be paired by hand at every call, and a pair that is wrong, is wrong
/// silently — handlers for one event would be called when another happened.
///
/// Subscriptions are taken out once, when the first handler for an event arrives, and
/// the handlers are held here. Only events the game raises on its main thread are
/// offered: the interpreter is not thread safe, and nothing here serialises calls
/// into it.
/// </remarks>
public sealed class ScriptEvents(ICoreServerAPI api)
{
    private readonly Dictionary<string, List<Handler>> handlers = [];
    private readonly List<Action> pending = [];

    /// <summary>One script function listening for one event.</summary>
    private sealed record Handler(ScriptOrigin Origin, ScriptValue.Func Call);

    /// <summary>Hands one occurrence of an event to whoever is listening for it.</summary>
    private delegate void Occurred(EventPayload about);

    /// <summary>How many handlers are listening, for the startup report.</summary>
    public int Count => handlers.Values.Sum(listening => listening.Count);

    /// <summary>Called after a player uses a block, which is left standing.</summary>
    /// <remarks>Using a block leaves it standing, so what stands there is what was used.</remarks>
    public void OnDidUseBlock(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("didUseBlock", origin, handler, occurred =>
            api.Event.DidUseBlock += (player, selection) => occurred(
                new BlockEventPayload(player, selection?.Position, Standing(selection?.Position))));

    /// <summary>Called after a player breaks a block.</summary>
    /// <remarks>
    /// Breaking a block removes it before this runs, so the position now holds air.
    /// The game hands over what stood there, and that is what a handler is told.
    /// </remarks>
    public void OnDidBreakBlock(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("didBreakBlock", origin, handler, occurred =>
            api.Event.DidBreakBlock += (player, brokenId, selection) => occurred(
                new BlockEventPayload(player, selection?.Position, api.World.GetBlock(brokenId))));

    /// <summary>Called after a player puts a block down.</summary>
    /// <remarks>
    /// Placing leaves the new block standing, so what stands there is what was placed.
    /// What it went over has already gone, and the game hands that over separately.
    /// </remarks>
    public void OnDidPlaceBlock(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("didPlaceBlock", origin, handler, occurred =>
            api.Event.DidPlaceBlock += (player, replacedId, selection, _) => occurred(
                new BlockPlacedEventPayload(
                    player, selection?.Position, Standing(selection?.Position),
                    api.World.GetBlock(replacedId))));

    /// <summary>Called when a column of chunks has been brought in.</summary>
    /// <remarks>
    /// Raised on the thread the server ticks on, once the column is ready rather than
    /// while it is being read, so what a handler does to those blocks is safe to do.
    /// </remarks>
    public void OnChunkColumnLoaded(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("chunkColumnLoaded", origin, handler, occurred =>
            api.Event.ChunkColumnLoaded += (at, _) => occurred(new ChunkColumnEventPayload(at.X, at.Y)));

    /// <summary>Called as one chunk is let go.</summary>
    /// <remarks>
    /// Named for what it does rather than for what the game calls it. The game raises
    /// this once per layer of a column rather than once for the column, so a column
    /// going out of memory calls a handler once for every chunk stacked at that place.
    ///
    /// The blocks are on their way out, so this is for forgetting what was remembered
    /// about them rather than for reaching them.
    /// </remarks>
    public void OnChunkUnloaded(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("chunkUnloaded", origin, handler, occurred =>
            api.Event.ChunkColumnUnloaded += at => occurred(new ChunkEventPayload(at.X, at.Y, at.Z)));

    /// <summary>Called after a player changes which hotbar slot they are holding.</summary>
    public void OnPlayerChangeSlot(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerChangeSlot", origin, handler, occurred =>
            api.Event.AfterActiveSlotChanged += (player, _) => occurred(new PlayerEventPayload(player)));

    /// <summary>Called when a player joins.</summary>
    public void OnPlayerJoin(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerJoin", origin, handler, occurred =>
            api.Event.PlayerJoin += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called when a player dies.</summary>
    public void OnPlayerDeath(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerDeath", origin, handler, occurred =>
            api.Event.PlayerDeath += (player, _) => occurred(new PlayerEventPayload(player)));

    /// <summary>Called when a player respawns.</summary>
    public void OnPlayerRespawn(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerRespawn", origin, handler, occurred =>
            api.Event.PlayerRespawn += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called the first time a player ever joins this world.</summary>
    /// <remarks>
    /// Raised only for a player the world has never seen, and before the welcome
    /// message, so a starter kit handed out here arrives with them.
    /// </remarks>
    public void OnPlayerCreate(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerCreate", origin, handler, occurred =>
            api.Event.PlayerCreate += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called once the player is in the world and has been welcomed.</summary>
    public void OnPlayerNowPlaying(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerNowPlaying", origin, handler, occurred =>
            api.Event.PlayerNowPlaying += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called when a joining player's client reports that it has finished.</summary>
    /// <remarks>The last of the three events a join raises.</remarks>
    public void OnPlayerReady(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerReady", origin, handler, occurred =>
            api.Event.PlayerReady += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called when a player quits of their own accord, before they are removed.</summary>
    /// <remarks>
    /// A player who was kicked or who dropped raises only <see cref="OnPlayerDisconnect"/>.
    /// </remarks>
    public void OnPlayerLeave(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerLeave", origin, handler, occurred =>
            api.Event.PlayerLeave += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called as a player is removed, however they went.</summary>
    /// <remarks>
    /// A quit, a kick and a lost connection all reach here, so this is the one that
    /// always runs.
    /// </remarks>
    public void OnPlayerDisconnect(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerDisconnect", origin, handler, occurred =>
            api.Event.PlayerDisconnect += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called after a player changes game mode.</summary>
    /// <remarks>Raised after the change, so asking the player their mode gives the new one.</remarks>
    public void OnPlayerSwitchGameMode(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerSwitchGameMode", origin, handler, occurred =>
            api.Event.PlayerSwitchGameMode += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called once the save game has been read, which is after every script has run.</summary>
    public void OnSaveGameLoaded(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("saveGameLoaded", origin, handler, occurred =>
            api.Event.SaveGameLoaded += () => occurred(ServerEventPayload.Instance));

    /// <summary>Called on the one start where the world is brand new.</summary>
    public void OnSaveGameCreated(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("saveGameCreated", origin, handler, occurred =>
            api.Event.SaveGameCreated += () => occurred(ServerEventPayload.Instance));

    /// <summary>Called as the world is written to disk.</summary>
    public void OnGameWorldSave(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("gameWorldSave", origin, handler, occurred =>
            api.Event.GameWorldSave += () => occurred(ServerEventPayload.Instance));

    /// <summary>Called once the world generators are starting.</summary>
    public void OnWorldgenStartup(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("worldgenStartup", origin, handler, occurred =>
            api.Event.WorldgenStartup += () => occurred(ServerEventPayload.Instance));

    /// <summary>Called when a server that had suspended itself wakes up again.</summary>
    public void OnServerResume(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("serverResume", origin, handler, occurred =>
            api.Event.ServerResume += () => occurred(ServerEventPayload.Instance));

    /// <summary>
    /// Adds a handler, and remembers to subscribe to the game's event the first time
    /// one arrives so a server whose scripts listen to nothing pays for nothing.
    /// </summary>
    /// <remarks>
    /// Nothing is subscribed here. A run only records what it wants, the same way it
    /// records the recipes it would register, and <see cref="Activate"/> is what
    /// carries that out. A run that is thrown away — a check, or one that failed
    /// partway — therefore leaves the game's own events exactly as it found them,
    /// where subscribing as each handler arrived would have left every one of them
    /// listening twice.
    /// </remarks>
    private void On(string name, ScriptOrigin origin, ScriptValue.Func handler, Action<Occurred> subscribe)
    {
        if (!handlers.TryGetValue(name, out var listening))
        {
            handlers[name] = listening = [];
            pending.Add(() => subscribe(about => Raise(name, about)));
        }

        listening.Add(new Handler(origin, handler));
    }

    /// <summary>
    /// Takes out the subscriptions this run asked for. Called once, by the run whose
    /// handlers are meant to be live, and never by one whose results are discarded.
    /// </summary>
    public void Activate()
    {
        foreach (var subscribe in pending) subscribe();
        pending.Clear();
    }

    /// <summary>
    /// Calls every handler for one event with the shape describing it, written into
    /// the table a script reads.
    /// </summary>
    /// <remarks>
    /// A handler that throws is logged and dropped rather than allowed out: these run
    /// inside the game's own event dispatch, where an exception would take down
    /// whatever raised it, and a handler that failed once will fail every time.
    /// </remarks>
    private void Raise(string name, EventPayload about)
    {
        if (handlers.GetValueOrDefault(name) is not { Count: > 0 } listening) return;

        var payload = PayloadWriter.Table(about);

        foreach (var handler in listening.ToArray())
        {
            try
            {
                handler.Call.Call([payload]);
            }
            catch (Exception failure)
            {
                listening.Remove(handler);
                api.Logger.Error(
                    "[moontweaks] {0}: handler for '{1}' failed and will not be called again: {2}",
                    handler.Origin, name, failure.Message);
            }
        }
    }

    /// <summary>Whatever stands at a position, for the events that leave it standing.</summary>
    private Block? Standing(BlockPos? at) =>
        at is null ? null : api.World.BlockAccessor.GetBlock(at);
}
