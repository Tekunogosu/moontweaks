using System;
using System.Collections.Generic;
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
/// Subscriptions are taken out once, when the first handler for an event arrives,
/// and the handlers are held here. Only events the game raises on its main thread
/// are offered: the interpreter is not thread safe, and nothing here serialises
/// calls into it.
/// </remarks>
public sealed class ScriptEvents(ICoreServerAPI api)
{
    /// <summary>Name a script listens for a block being used under.</summary>
    public const string DidUseBlock = "didUseBlock";

    /// <summary>Name a script listens for a block being broken under.</summary>
    public const string DidBreakBlock = "didBreakBlock";

    /// <summary>Name a script listens for a player joining under.</summary>
    public const string PlayerJoin = "playerJoin";

    /// <summary>Name a script listens for a player dying under.</summary>
    public const string PlayerDeath = "playerDeath";

    /// <summary>Name a script listens for a player respawning under.</summary>
    public const string PlayerRespawn = "playerRespawn";

    /// <summary>Name a script listens for a player's first ever join under.</summary>
    public const string PlayerCreate = "playerCreate";

    /// <summary>Name a script listens for a player entering the world under.</summary>
    public const string PlayerNowPlaying = "playerNowPlaying";

    /// <summary>Name a script listens for a player's client finishing joining under.</summary>
    public const string PlayerReady = "playerReady";

    /// <summary>Name a script listens for a player quitting under.</summary>
    public const string PlayerLeave = "playerLeave";

    /// <summary>Name a script listens for a player being removed under.</summary>
    public const string PlayerDisconnect = "playerDisconnect";

    /// <summary>Name a script listens for a player changing game mode under.</summary>
    public const string PlayerSwitchGameMode = "playerSwitchGameMode";

    /// <summary>Name a script listens for the save game being loaded under.</summary>
    public const string SaveGameLoaded = "saveGameLoaded";

    /// <summary>Name a script listens for a world being created under.</summary>
    public const string SaveGameCreated = "saveGameCreated";

    /// <summary>Name a script listens for the world being saved under.</summary>
    public const string GameWorldSave = "gameWorldSave";

    /// <summary>Name a script listens for the world generators starting under.</summary>
    public const string WorldgenStartup = "worldgenStartup";

    /// <summary>Name a script listens for the server waking from standby under.</summary>
    public const string ServerResume = "serverResume";

    private readonly Dictionary<string, List<Handler>> handlers = [];

    /// <summary>One script function listening for one event.</summary>
    private sealed record Handler(ScriptOrigin Origin, ScriptValue.Func Call);

    /// <summary>
    /// Adds a handler, subscribing to the game's event the first time one arrives so
    /// a server whose scripts listen to nothing pays for nothing.
    /// </summary>
    public void On(string name, ScriptOrigin origin, ScriptValue.Func handler, Action subscribe)
    {
        if (!handlers.TryGetValue(name, out var listening))
        {
            handlers[name] = listening = [];
            subscribe();
        }

        listening.Add(new Handler(origin, handler));
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
    public void Raise(string name, EventPayload about)
    {
        if (!handlers.TryGetValue(name, out var listening) || listening.Count == 0) return;

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

    /// <summary>Subscribes to the game's own block-used event.</summary>
    /// <remarks>
    /// Using a block leaves it standing, so what stands there is what was used.
    /// </remarks>
    public void SubscribeDidUseBlock() =>
        api.Event.DidUseBlock += (player, selection) => Raise(DidUseBlock,
            new BlockEventPayload(player, selection?.Position, Standing(selection?.Position)));

    /// <summary>Subscribes to the game's own block-broken event.</summary>
    /// <remarks>
    /// Breaking a block removes it before this runs, so the position now holds air.
    /// The game hands over what stood there, and that is what a handler is told.
    /// </remarks>
    public void SubscribeDidBreakBlock() =>
        api.Event.DidBreakBlock += (player, brokenId, selection) => Raise(DidBreakBlock,
            new BlockEventPayload(player, selection?.Position, api.World.GetBlock(brokenId)));

    /// <summary>Subscribes to the game's own player-joined event.</summary>
    public void SubscribePlayerJoin() =>
        api.Event.PlayerJoin += player => Raise(PlayerJoin, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own player-died event.</summary>
    public void SubscribePlayerDeath() =>
        api.Event.PlayerDeath += (player, _) => Raise(PlayerDeath, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own player-respawned event.</summary>
    public void SubscribePlayerRespawn() =>
        api.Event.PlayerRespawn += player => Raise(PlayerRespawn, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own first-ever-join event.</summary>
    /// <remarks>
    /// Raised only for a player the world has never seen, and before the welcome
    /// message, so a starter kit handed out here arrives with them.
    /// </remarks>
    public void SubscribePlayerCreate() =>
        api.Event.PlayerCreate += player => Raise(PlayerCreate, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own now-playing event.</summary>
    /// <remarks>Raised once the player is in the world and has been welcomed.</remarks>
    public void SubscribePlayerNowPlaying() =>
        api.Event.PlayerNowPlaying += player => Raise(PlayerNowPlaying, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own player-ready event.</summary>
    /// <remarks>
    /// Raised when the player's client reports that it has finished joining, which
    /// is the last of the three events a join raises.
    /// </remarks>
    public void SubscribePlayerReady() =>
        api.Event.PlayerReady += player => Raise(PlayerReady, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own player-left event.</summary>
    /// <remarks>
    /// Raised for a player who quit of their own accord, before they are removed.
    /// A player who was kicked or who dropped raises only <see cref="PlayerDisconnect"/>.
    /// </remarks>
    public void SubscribePlayerLeave() =>
        api.Event.PlayerLeave += player => Raise(PlayerLeave, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own player-disconnected event.</summary>
    /// <remarks>
    /// Raised as the player is removed, however they went: a quit, a kick or a lost
    /// connection all reach here, so this is the one that always runs.
    /// </remarks>
    public void SubscribePlayerDisconnect() =>
        api.Event.PlayerDisconnect += player => Raise(PlayerDisconnect, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own game-mode-changed event.</summary>
    /// <remarks>
    /// Raised after the change, so asking the player their mode gives the new one.
    /// </remarks>
    public void SubscribePlayerSwitchGameMode() =>
        api.Event.PlayerSwitchGameMode += player =>
            Raise(PlayerSwitchGameMode, new PlayerEventPayload(player));

    /// <summary>Subscribes to the game's own save-loaded event.</summary>
    public void SubscribeSaveGameLoaded() =>
        api.Event.SaveGameLoaded += () => Raise(SaveGameLoaded, ServerEventPayload.Instance);

    /// <summary>Subscribes to the game's own save-created event.</summary>
    public void SubscribeSaveGameCreated() =>
        api.Event.SaveGameCreated += () => Raise(SaveGameCreated, ServerEventPayload.Instance);

    /// <summary>Subscribes to the game's own world-being-saved event.</summary>
    public void SubscribeGameWorldSave() =>
        api.Event.GameWorldSave += () => Raise(GameWorldSave, ServerEventPayload.Instance);

    /// <summary>Subscribes to the game's own worldgen-startup event.</summary>
    public void SubscribeWorldgenStartup() =>
        api.Event.WorldgenStartup += () => Raise(WorldgenStartup, ServerEventPayload.Instance);

    /// <summary>Subscribes to the game's own server-resumed event.</summary>
    public void SubscribeServerResume() =>
        api.Event.ServerResume += () => Raise(ServerResume, ServerEventPayload.Instance);

    /// <summary>Whatever stands at a position, for the events that leave it standing.</summary>
    private Block? Standing(BlockPos? at) =>
        at is null ? null : api.World.BlockAccessor.GetBlock(at);

    /// <summary>How many handlers are listening, for the startup report.</summary>
    public int Count
    {
        get
        {
            var total = 0;
            foreach (var listening in handlers.Values) total += listening.Count;
            return total;
        }
    }
}
