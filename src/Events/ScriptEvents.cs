using System;
using System.Collections.Generic;
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
    /// Calls every handler for one event with the table describing it.
    /// </summary>
    /// <remarks>
    /// A handler that throws is logged and dropped rather than allowed out: these run
    /// inside the game's own event dispatch, where an exception would take down
    /// whatever raised it, and a handler that failed once will fail every time.
    /// </remarks>
    public void Raise(string name, IReadOnlyDictionary<string, ScriptValue> about)
    {
        if (!handlers.TryGetValue(name, out var listening) || listening.Count == 0) return;

        var payload = new ScriptValue.Map(about);

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
        api.Event.DidUseBlock += (player, selection) =>
            Raise(DidUseBlock, Where(player, selection?.Position, Standing(selection?.Position)));

    /// <summary>Subscribes to the game's own block-broken event.</summary>
    /// <remarks>
    /// Breaking a block removes it before this runs, so the position now holds air.
    /// The game hands over what stood there, and that is what a handler is told.
    /// </remarks>
    public void SubscribeDidBreakBlock() =>
        api.Event.DidBreakBlock += (player, brokenId, selection) =>
            Raise(DidBreakBlock, Where(player, selection?.Position, api.World.GetBlock(brokenId)));

    /// <summary>Subscribes to the game's own player-joined event.</summary>
    public void SubscribePlayerJoin() =>
        api.Event.PlayerJoin += player => Raise(PlayerJoin, Who(player));

    /// <summary>Subscribes to the game's own player-died event.</summary>
    public void SubscribePlayerDeath() =>
        api.Event.PlayerDeath += (player, _) => Raise(PlayerDeath, Who(player));

    /// <summary>Subscribes to the game's own player-respawned event.</summary>
    public void SubscribePlayerRespawn() =>
        api.Event.PlayerRespawn += player => Raise(PlayerRespawn, Who(player));

    /// <summary>Who something happened to, as a script reads it.</summary>
    private static Dictionary<string, ScriptValue> Who(IServerPlayer player) => new()
    {
        ["player"] = new ScriptValue.Str(player.PlayerUID),
        ["playerName"] = new ScriptValue.Str(player.PlayerName),
    };

    /// <summary>
    /// Who something happened to, and to which block where. The block is supplied
    /// rather than looked up, because which block an event is about depends on what
    /// the event did to it.
    /// </summary>
    private static Dictionary<string, ScriptValue> Where(
        IServerPlayer player, BlockPos? at, Block? block)
    {
        var about = Who(player);

        about["block"] = block?.Code is { } code
            ? new ScriptValue.Str(code.ToString())
            : ScriptValue.Nil.Instance;
        about["x"] = new ScriptValue.Num(at?.X ?? 0);
        about["y"] = new ScriptValue.Num(at?.Y ?? 0);
        about["z"] = new ScriptValue.Num(at?.Z ?? 0);
        return about;
    }

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
