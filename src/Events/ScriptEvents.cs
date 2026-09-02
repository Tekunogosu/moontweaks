using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
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
/// the handlers are held here. There is one interpreter for the whole server and it
/// is not thread safe, so an event the game raises anywhere but the thread it ticks
/// on is subscribed through <see cref="OnAnyThread"/> and delivered on the next tick
/// of that one; <see cref="On"/> is for the events known to arrive there already.
/// </remarks>
public sealed class ScriptEvents(ICoreServerAPI api)
{
    private readonly Dictionary<string, List<Handler>> handlers = [];
    private readonly List<(string Event, Action Subscribe)> pending = [];

    /// <summary>
    /// The thread the server ticks on, remembered as the subscriptions are taken out.
    /// An event that must be answered where it was asked cannot be marshalled onto
    /// this thread — the answer would arrive after the decision — so an answering
    /// event compares against this and declines to run a script anywhere else.
    /// </summary>
    /// <remarks>
    /// Read from <see cref="Activate"/> rather than from the constructor: both run
    /// while the server is loading and on the thread it ticks on, and the later of the
    /// two is the one that cannot be reached from anywhere else.
    /// </remarks>
    private int mainThread;

    /// <summary>
    /// The pace each mount was last reported being ridden at, so a stream of reports
    /// saying the same thing raises one event. Written from the network thread the
    /// reports land on, which is why it is a concurrent one.
    /// </summary>
    private readonly ConcurrentDictionary<long, string?> gaits = new();

    /// <summary>
    /// Which map regions are in memory, so a region asked for again raises nothing.
    /// Holds what the server holds: an entry arrives with the region and leaves with
    /// it. Read and written on the main thread alone, which is where the game raises
    /// both halves.
    /// </summary>
    private readonly HashSet<(int X, int Z)> regions = [];

    /// <summary>
    /// Answering events already reported as having been asked from another thread, so
    /// a check the server makes constantly says so once rather than filling the log.
    /// </summary>
    private readonly HashSet<string> offThread = [];

    /// <summary>One script function listening for one event.</summary>
    private sealed record Handler(ScriptOrigin Origin, ScriptValue.Func Call);

    /// <summary>Hands one occurrence of an event to whoever is listening for it.</summary>
    private delegate void Occurred(EventPayload about);

    /// <summary>
    /// Hands one occurrence to whoever is listening and gives back what they decided,
    /// or null where nobody decided anything.
    /// </summary>
    private delegate EnumAccessResponse? Answered(EventPayload about);

    /// <summary>
    /// Hands something a player said to whoever is listening and gives back what
    /// should be said instead and whether anybody should see it.
    /// </summary>
    private delegate (string Message, bool Consumed) Said(
        IServerPlayer player, int group, string message, bool consumed);

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
    /// The game hands over what stood there, and a handler is told the same.
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

    /// <summary>Called once a region of the map has come in.</summary>
    /// <remarks>
    /// Raised on the main thread: the game generates a region on the thread it
    /// supplies chunks from and hands the event to the main one itself, so there is
    /// nothing left here to marshal.
    ///
    /// What the game raises is not an arrival, though. Every chunk column asks for
    /// the region it sits in, and the event is raised on each of those asks, so a
    /// region already in memory raises it again for every column loaded over it.
    /// Which regions are in memory is remembered here and only the first ask for one
    /// goes further, which makes this the arrival its name claims — and pairs it with
    /// the unload, which is raised once for real.
    /// </remarks>
    public void OnMapRegionLoaded(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("mapRegionLoaded", origin, handler, occurred =>
        {
            api.Event.MapRegionLoaded += (at, _) =>
            {
                if (regions.Add((at.X, at.Y))) occurred(Region(at));
            };

            // What is remembered above is forgotten here rather than in the unload
            // binding beside it: the two are subscribed independently, and a script
            // listening only for arrivals would otherwise remember every region the
            // server ever read.
            api.Event.MapRegionUnloaded += (at, _) => regions.Remove((at.X, at.Y));
        });

    /// <summary>Called as a region of the map is let go.</summary>
    /// <remarks>
    /// Raised once per region, where the server ticks and again as it shuts down,
    /// both on the main thread — so a handler runs while the event is happening
    /// rather than a tick later, so one at shutdown runs at all.
    /// </remarks>
    public void OnMapRegionUnloaded(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("mapRegionUnloaded", origin, handler, occurred =>
            api.Event.MapRegionUnloaded += (at, _) => occurred(Region(at)));

    /// <summary>Called after a player changes which hotbar slot they are holding.</summary>
    public void OnPlayerChangeSlot(ScriptOrigin origin, ScriptValue.Func handler) =>
        On("playerChangeSlot", origin, handler, occurred =>
            api.Event.AfterActiveSlotChanged += (player, _) => occurred(new PlayerEventPayload(player)));

    /// <summary>Called when something is put into the world.</summary>
    /// <remarks>
    /// Raised wherever the game happened to spawn it, chunk generation included, so
    /// this is delivered on the following tick.
    /// </remarks>
    public void OnEntitySpawn(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnAnyThread("entitySpawn", origin, handler, occurred =>
            api.Event.OnEntitySpawn += entity => occurred(new EntityEventPayload(entity)));

    /// <summary>Called when something comes back with the chunk it was saved in.</summary>
    /// <remarks>
    /// The counterpart of a despawn for <c>unload</c>: the same creature, returning
    /// rather than appearing. Raised on the chunk's own thread, so delivered late.
    /// </remarks>
    public void OnEntityLoaded(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnAnyThread("entityLoaded", origin, handler, occurred =>
            api.Event.OnEntityLoaded += entity => occurred(new EntityEventPayload(entity)));

    /// <summary>Called when something dies, for anything alive rather than players alone.</summary>
    public void OnEntityDeath(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnAnyThread("entityDeath", origin, handler, occurred =>
            api.Event.OnEntityDeath += (entity, cause) =>
                occurred(new EntityDeathEventPayload(entity, cause)));

    /// <summary>Called when something leaves the world, however it went.</summary>
    public void OnEntityDespawn(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnAnyThread("entityDespawn", origin, handler, occurred =>
            api.Event.OnEntityDespawn += (entity, reason) =>
                occurred(new EntityDespawnEventPayload(entity, reason)));

    /// <summary>Called when a mount's rider changes the pace it is ridden at.</summary>
    /// <remarks>
    /// Named for the change rather than for the packet the game named it after,
    /// because the change is what this raises. A client sends its mount's gait with
    /// every position update it sends, several times a second and whether or not
    /// anything about it moved, so what the game raises is a stream rather than an
    /// event; the pace each mount was last reported at is remembered here and a
    /// report saying the same thing again goes no further.
    ///
    /// Remembered where the packet lands, which is a network thread rather than the
    /// one the server ticks on. Two packets for one mount arriving at once may both
    /// be reported, which costs a script a repeated call and nothing else.
    /// </remarks>
    public void OnMountGaitChanged(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnAnyThread("mountGaitChanged", origin, handler, occurred =>
            api.Event.MountGaitReceived += (mount, gait) =>
            {
                if (Changed(mount.EntityId, gait)) occurred(new MountGaitEventPayload(mount, gait));
            });

    /// <summary>Called when something climbs onto something else.</summary>
    public void OnEntityMounted(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnAnyThread("entityMounted", origin, handler, occurred =>
            api.Event.EntityMounted += (entity, seat) =>
                occurred(new EntityMountEventPayload(entity, seat)));

    /// <summary>Called when something gets off what it was riding.</summary>
    public void OnEntityUnmounted(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnAnyThread("entityUnmounted", origin, handler, occurred =>
            api.Event.EntityUnmounted += (entity, seat) =>
                occurred(new EntityMountEventPayload(entity, seat)));

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
    /// Adds a handler for an event the game may raise on any thread it likes, and
    /// calls it on the next tick of the one the server runs on.
    /// </summary>
    /// <remarks>
    /// There is one interpreter for the whole server and it is not thread safe. Most
    /// of the game's events arrive on the thread it ticks on, and those are added
    /// through <see cref="On"/> and called where they land. The rest do not: a chunk
    /// being generated spawns creatures on the generation thread, and calling a script
    /// from there while the main thread is already inside the interpreter is a race.
    ///
    /// So the payload is built where the event happened — a snapshot of plain numbers
    /// and strings, as every shape here already is — and only the call is
    /// handed across. Two things follow, and both reach script authors rather than
    /// staying here. The handler runs a tick late, so what it is told may already have
    /// changed and anything it reaches for wants checking first. And the game decides
    /// when to run the queue, so a burst of events — worldgen filling a chunk with
    /// creatures — arrives as a burst of calls.
    ///
    /// This is the safe default. Reaching for <see cref="On"/> instead is worth doing
    /// only for an event known to arrive on the main thread, and worth the checking
    /// that claim needs.
    /// </remarks>
    /// <summary>
    /// Called before the server lets somebody act on a place, with the answer it has
    /// arrived at so far, and answered by whatever a handler returns.
    /// </summary>
    /// <remarks>
    /// The one event here a handler decides rather than observes. The game asks every
    /// mod in turn, handing each the answer the one before it gave, and takes the last
    /// answer as the decision — so a handler may refuse what the claim allowed and
    /// may allow what the claim refused. Both directions are the game's own behaviour
    /// and both are offered, which means a script here can open a player's claim to
    /// somebody else; a handler that means only to refuse should answer nothing
    /// wherever it does not mean to refuse, rather than answering <c>granted</c>.
    ///
    /// Answered where it is asked. The server's own two callers — a player breaking a
    /// block and a player using one — ask on the thread it ticks on, but another mod
    /// calling the same check may ask from any thread, and an answer marshalled onto
    /// the main thread would arrive after the decision was made. So a call arriving
    /// anywhere else leaves the answer exactly as it found it, and says so once.
    /// </remarks>
    public void OnTestBlockAccess(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnAnswered("testBlockAccess", origin, handler, answer =>
            api.Event.OnTestBlockAccess += (player, selection, kind, ref claimant, response) =>
            {
                // Not an IServerPlayer for a caller that supplied none, and every
                // payload here is built from one. Nothing to tell a script about.
                if (player is not IServerPlayer asked) return response;

                var about = new AccessTestEventPayload(asked, selection?.Position,
                    ValueSet.As<EnumAccessKind>(kind), ValueSet.As<EnumAccessResponse>(response),
                    claimant);

                return answer(about) is { } decided
                    ? ValueSet.As<EnumWorldAccessResponse>(decided)
                    : response;
            });

    /// <summary>
    /// Called before anybody sees what a player said, and answered by what the handler
    /// returns: a string to say something else instead, <c>false</c> to say nothing,
    /// <c>true</c> to say it after all, or nothing at all to leave it alone.
    /// </summary>
    /// <remarks>
    /// Handlers are asked in turn and each is given what the one before it left, so a
    /// rewrite chains and the last word stands. That is the game's own rule for this
    /// event and it is followed exactly, which is worth knowing in one direction
    /// particularly: a handler answering <c>true</c> undoes a swallow an earlier one
    /// asked for, so a script that mutes players and a script that rewrites messages
    /// have to be ordered deliberately rather than dropped into the folder in any
    /// order.
    ///
    /// Raised on the thread the server ticks on, where the answer is needed, so the
    /// same guard applies as to the other answering event: asked from anywhere else,
    /// the message is left exactly as it was found.
    /// </remarks>
    public void OnPlayerChat(ScriptOrigin origin, ScriptValue.Func handler) =>
        OnChat("playerChat", origin, handler, said =>
            api.Event.PlayerChat += (IServerPlayer player, int group, ref string message, ref string data, BoolRef consumed) =>
            {
                var (spoken, swallowed) = said(player, group, message, consumed.value);
                message = spoken;
                consumed.value = swallowed;
            });

    private void OnAnyThread(
        string name, ScriptOrigin origin, ScriptValue.Func handler, Action<Occurred> subscribe) =>
        On(name, origin, handler, occurred => subscribe(
            about => api.Event.EnqueueMainThreadTask(() => occurred(about), $"moontweaks:{name}")));

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
            pending.Add((name, () => subscribe(about => Raise(name, about))));
        }

        listening.Add(new Handler(origin, handler));
    }

    /// <summary>
    /// Takes out the subscriptions this run asked for. Called once, by the run whose
    /// handlers are meant to be live, and never by one whose results are discarded.
    /// </summary>
    /// <remarks>
    /// One subscription refused costs that event and nothing else. Each is taken out
    /// against the game rather than against anything checkable here, so a refusal
    /// would otherwise silently leave every event after it unsubscribed while the
    /// scripts that asked for them are reported as having run.
    /// </remarks>
    /// <returns>What each refused subscription was, for whoever is going to report them.</returns>
    public IReadOnlyList<string> Activate()
    {
        mainThread = Environment.CurrentManagedThreadId;
        var refused = new List<string>();

        foreach (var (name, subscribe) in pending)
        {
            try
            {
                subscribe();
            }
            catch (Exception failure)
            {
                refused.Add($"the game refused a subscription to '{name}', "
                    + $"so nothing listens for it ({failure.GetType().Name}): {failure.Message}");
            }
        }

        pending.Clear();
        return refused;
    }

    /// <summary>
    /// Adds a handler for an event whose answer decides something, subscribing the
    /// first time one arrives. The same bookkeeping <see cref="On"/> does, differing
    /// only in that what a handler returns is carried back rather than dropped.
    /// </summary>
    private void OnAnswered(
        string name, ScriptOrigin origin, ScriptValue.Func handler, Action<Answered> subscribe)
    {
        if (!handlers.TryGetValue(name, out var listening))
        {
            handlers[name] = listening = [];
            pending.Add((name, () => subscribe(about => RaiseAnswered(name, about))));
        }

        listening.Add(new Handler(origin, handler));
    }

    /// <summary>
    /// Adds a handler for the chat event, subscribing the first time one arrives. The
    /// same bookkeeping <see cref="On"/> does, differing in what a handler is given
    /// back the chance to change.
    /// </summary>
    private void OnChat(
        string name, ScriptOrigin origin, ScriptValue.Func handler, Action<Said> subscribe)
    {
        if (!handlers.TryGetValue(name, out var listening))
        {
            handlers[name] = listening = [];
            pending.Add((name, () => subscribe(
                (player, group, message, consumed) => RaiseChat(name, player, group, message, consumed))));
        }

        listening.Add(new Handler(origin, handler));
    }

    /// <summary>
    /// Calls every handler for something said, each given what the one before it left,
    /// and carries back what should be said and whether anybody should see it.
    /// </summary>
    /// <remarks>
    /// A fresh table per handler, because what a handler is told has to be what the
    /// handler before it decided rather than what was typed. Chat is a thing people do
    /// rather than a thing the server does, so that cost is paid a few times a minute.
    ///
    /// Refuses to run anywhere but the thread the server ticks on, for the reason
    /// <see cref="RaiseAnswered"/> gives: the answer is needed where it was asked.
    /// </remarks>
    private (string Message, bool Consumed) RaiseChat(
        string name, IServerPlayer player, int group, string message, bool consumed)
    {
        if (handlers.GetValueOrDefault(name) is not { Count: > 0 } listening) return (message, consumed);

        if (Environment.CurrentManagedThreadId != mainThread)
        {
            if (offThread.Add(name))
            {
                api.Logger.Warning(
                    "[moontweaks] '{0}' was raised on another thread and was left alone. "
                    + "A script cannot answer it there, so handlers for it did not run.",
                    name);
            }
            return (message, consumed);
        }

        foreach (var handler in listening.ToArray())
        {
            try
            {
                var about = new ChatEventPayload(player, group, message, consumed);

                switch (handler.Call.Call([PayloadWriter.Table(about)]))
                {
                    // A string is what should be said instead. Whether anybody sees it
                    // is a separate question, and this answer does not touch it.
                    case ScriptValue.Str said:
                        message = said.Value;
                        break;

                    // false swallows it and true puts it back, which is the game's own
                    // pair of answers rather than this mod's.
                    case ScriptValue.Bool deliver:
                        consumed = !deliver.Value;
                        break;
                }
            }
            catch (Exception failure)
            {
                listening.Remove(handler);
                api.Logger.Error(
                    "[moontweaks] {0}: handler for '{1}' failed and will not be called again: {2}",
                    handler.Origin, name, failure.Message);
            }
        }

        return (message, consumed);
    }

    /// <summary>
    /// Calls every handler for one event and carries back the last answer any of them
    /// gave, or null where none of them gave one.
    /// </summary>
    /// <remarks>
    /// Last rather than first, because that is what the game does with the mods it
    /// asks: each is handed the answer so far and the last word stands. A handler
    /// wanting the earlier answer reads it off the event.
    ///
    /// Refuses to run anywhere but the thread the server ticks on. The interpreter is
    /// not thread safe and the answer is needed where it was asked, so there is
    /// nothing to do off that thread but leave the decision alone. Said once per
    /// event rather than per call: whatever is asking off-thread will ask again.
    /// </remarks>
    private EnumAccessResponse? RaiseAnswered(string name, EventPayload about)
    {
        if (handlers.GetValueOrDefault(name) is not { Count: > 0 } listening) return null;

        if (Environment.CurrentManagedThreadId != mainThread)
        {
            if (offThread.Add(name))
            {
                api.Logger.Warning(
                    "[moontweaks] '{0}' was asked from another thread and was left to the server to answer. "
                    + "A script cannot answer it there, so handlers for it do not run for whatever asked.",
                    name);
            }
            return null;
        }

        var payload = PayloadWriter.Table(about);
        EnumAccessResponse? decided = null;

        foreach (var handler in listening.ToArray())
        {
            try
            {
                if (handler.Call.Call([payload]) is ScriptValue.Str answer)
                {
                    decided = ValueSet.Named<EnumAccessResponse>(answer.Value, handler.Origin, name);
                }
            }
            catch (Exception failure)
            {
                listening.Remove(handler);
                api.Logger.Error(
                    "[moontweaks] {0}: handler for '{1}' failed and will not be called again: {2}",
                    handler.Origin, name, failure.Message);
            }
        }

        return decided;
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

    /// <summary>
    /// One map region, in both the coordinates it is counted in and the ones a script
    /// writes. How wide a region is belongs to the server rather than to the game, so
    /// it is read here rather than assumed.
    /// </summary>
    private MapRegionEventPayload Region(Vec2i at) =>
        new(at.X, at.Y, api.WorldManager.RegionSize);

    /// <summary>
    /// Whether a mount is being ridden at a pace it was not last reported at, and
    /// remembers the new one. One entry per mount reported since the server started,
    /// which is a number of horses rather than a number of packets.
    /// </summary>
    private bool Changed(long mount, string? gait)
    {
        if (gaits.TryGetValue(mount, out var reported) && reported == gait) return false;

        gaits[mount] = gait;
        return true;
    }
}
