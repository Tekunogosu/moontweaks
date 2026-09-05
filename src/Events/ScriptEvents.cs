using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

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
///
/// One method per event, each naming that event, describing it to a script author and
/// subscribing to the game's own in the same breath. Written that way on purpose: a
/// name and a subscription declared apart have to be paired by hand at every call, and
/// a pair that is wrong, is wrong silently — handlers for one event would be called
/// when another happened. A description kept apart drifts the same way, saying one
/// thing to a script author while the subscription beside it does another.
///
/// Subscriptions are taken out once, when the first handler for an event arrives, and
/// the handlers are held here. There is one interpreter for the whole server and it
/// is not thread safe, so an event the game raises anywhere but the thread it ticks
/// on is subscribed through <see cref="OnAnyThread"/> and delivered on the next tick
/// of that one; <see cref="On"/> is for the events known to arrive there already.
/// </remarks>
/// <example>
/// <code>
/// local events = moontweaks.events
///
/// -- Players coming and going.
/// events.playerJoin(function(e)
///   moontweaks.players.say(e.player, "welcome back, " .. e.playerName)
/// end)
///
/// events.playerDeath(function(e)
///   moontweaks.players.announce(e.playerName .. " has died.")
/// end)
///
/// -- What people do to blocks. A place event also says what stood there before.
/// events.didBreakBlock(function(e)
///   if e.block == "game:crock-red-fired" then
///     moontweaks.log.info(("%s broke a crock at %d %d %d"):format(e.playerName, e.x, e.y, e.z))
///   end
/// end)
///
/// events.didPlaceBlock(function(e)
///   if e.replaced then
///     moontweaks.log.info(("%s built over %s"):format(e.playerName, e.replaced))
///   end
/// end)
///
/// -- Creatures. A death says what killed it, where it was known.
/// events.entityDeath(function(e)
///   if e.byPlayer then
///     moontweaks.players.say(e.byPlayer, ("You killed a %s."):format(e.name))
///   end
/// end)
///
/// -- Answering rather than watching: a handler that returns decides the outcome.
/// events.testBlockAccess(function(e)
///   if e.y &lt; 20 and not moontweaks.players.hasPrivilege(e.player, "gamemode") then
///     return "noprivilege"
///   end
/// end)
///
/// events.playerChat(function(e)
///   if e.message:find("badword") then return false end
/// end)
///
/// -- The server's own lifetime, for anything that has to be set up once.
/// events.saveGameLoaded(function()
///   moontweaks.log.info("the world is open")
/// end)
/// </code>
/// </example>
[LuaModule("moontweaks.events")]
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
    /// <param name="Origin">Script line that added it, for naming it in a failure.</param>
    /// <param name="Call">The function itself.</param>
    /// <param name="Filter">
    /// Output code this handler asked to hear about, matched before the interpreter is
    /// entered. Null on an event that hands every occurrence to every handler; the
    /// recipe events require one, because they are asked often enough that deciding
    /// in Lua would be the whole cost.
    /// </param>
    private sealed record Handler(ScriptOrigin Origin, ScriptValue.Func Call, AssetLocation? Filter = null);

    /// <summary>Hands one occurrence of an event to whoever is listening for it.</summary>
    private delegate void Occurred(EventPayload about);

    /// <summary>
    /// Hands one occurrence to whoever is listening and gives back what they decided,
    /// or null where nobody decided anything.
    /// </summary>
    private delegate EnumAccessResponse? Answered(EventPayload about);

    /// <summary>
    /// An amount handed to every handler in turn, each told what the one before it
    /// left, and carried back changed or not.
    /// </summary>
    /// <param name="describe">The shape for one amount, built afresh per handler.</param>
    /// <param name="amount">What the game was about to apply.</param>
    private delegate double Amended(System.Func<double, EventPayload> describe, double amount);

    /// <summary>
    /// Every health behaviour already hooked, so an entity that both spawned and was
    /// reported loaded is listened to once. Weak, so a despawned entity is let go.
    /// </summary>
    private readonly ConditionalWeakTable<EntityBehaviorHealth, object> hooked = [];

    /// <summary>What amends damage, once a script asked; null while none has.</summary>
    private Amended? damaged;

    /// <summary>What amends healing, once a script asked; null while none has.</summary>
    private Amended? healed;

    /// <summary>Whether the spawn and load listeners that hook each entity are in place.</summary>
    private bool hooking;

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
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didUseBlock")]
    public void OnDidUseBlock(
        ScriptOrigin origin, [LuaPayload(typeof(BlockEventPayload))] ScriptValue.Func handler) =>
        On("didUseBlock", origin, handler, occurred =>
            api.Event.DidUseBlock += (player, selection) => occurred(
                new BlockEventPayload(player, selection?.Position, Standing(selection?.Position))));

    /// <summary>
    /// Called after a player breaks a block. The block is the one that stood there:
    /// it has already gone by the time a handler runs.
    /// </summary>
    /// <remarks>
    /// Breaking a block removes it before this runs, so the position now holds air.
    /// The game hands over what stood there, and a handler is told the same.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didBreakBlock")]
    public void OnDidBreakBlock(
        ScriptOrigin origin, [LuaPayload(typeof(BlockEventPayload))] ScriptValue.Func handler) =>
        On("didBreakBlock", origin, handler, occurred =>
            api.Event.DidBreakBlock += (player, brokenId, selection) => occurred(
                new BlockEventPayload(player, selection?.Position, api.World.GetBlock(brokenId))));

    /// <summary>
    /// Called after a player puts a block down. <c>block</c> is what now stands there
    /// and <c>replaced</c> is what it went over, which is air where nothing was.
    /// </summary>
    /// <remarks>
    /// Placing leaves the new block standing, so what stands there is what was placed.
    /// What it went over has already gone, and the game hands that over separately.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("didPlaceBlock")]
    public void OnDidPlaceBlock(
        ScriptOrigin origin, [LuaPayload(typeof(BlockPlacedEventPayload))] ScriptValue.Func handler) =>
        On("didPlaceBlock", origin, handler, occurred =>
            api.Event.DidPlaceBlock += (player, replacedId, selection, _) => occurred(
                new BlockPlacedEventPayload(
                    player, selection?.Position, Standing(selection?.Position),
                    api.World.GetBlock(replacedId))));

    /// <summary>
    /// Called once a column of chunks has been brought in and its blocks can be
    /// reached. This is what lets a script act on a place nobody is standing, together
    /// with <c>moontweaks.world.loadChunk</c>.
    /// </summary>
    /// <remarks>
    /// Raised on the thread the server ticks on, once the column is ready rather than
    /// while it is being read, so what a handler does to those blocks is safe to do.
    ///
    /// A busy server loads columns constantly, so a handler here runs often. Keep it
    /// short, and decide whether the column is one worth acting on before doing
    /// anything that costs.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("chunkColumnLoaded")]
    public void OnChunkColumnLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(ChunkColumnEventPayload))] ScriptValue.Func handler) =>
        On("chunkColumnLoaded", origin, handler, occurred =>
            api.Event.ChunkColumnLoaded += (at, _) => occurred(new ChunkColumnEventPayload(at.X, at.Y)));

    /// <summary>
    /// Called as one chunk is let go, which is where anything remembered about its
    /// blocks should be forgotten. The blocks themselves are on their way out and
    /// should not be reached.
    /// </summary>
    /// <remarks>
    /// Named for what it does rather than for what the game calls it. The game raises
    /// this once per layer of a column rather than once for the column, so a column
    /// going out of memory calls a handler once for every chunk stacked at that place.
    /// <c>chunkY</c> says which.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("chunkUnloaded")]
    public void OnChunkUnloaded(
        ScriptOrigin origin, [LuaPayload(typeof(ChunkEventPayload))] ScriptValue.Func handler) =>
        On("chunkUnloaded", origin, handler, occurred =>
            api.Event.ChunkColumnUnloaded += at => occurred(new ChunkEventPayload(at.X, at.Y, at.Z)));

    /// <summary>
    /// Called once a region of the map has come in, whether it was read from disk or
    /// generated. A region is a square of chunk columns holding the maps a world is
    /// grown from, so this is raised far less often than <c>chunkColumnLoaded</c> and
    /// covers far more ground.
    /// </summary>
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
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("mapRegionLoaded")]
    public void OnMapRegionLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(MapRegionEventPayload))] ScriptValue.Func handler) =>
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

    /// <summary>
    /// Called as a region of the map is let go, which is where anything remembered
    /// about that stretch of the world should be forgotten.
    /// </summary>
    /// <remarks>
    /// Raised once per region, where the server ticks and again as it shuts down,
    /// both on the main thread — so a handler runs while the event is happening
    /// rather than a tick later, so one at shutdown runs at all.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("mapRegionUnloaded")]
    public void OnMapRegionUnloaded(
        ScriptOrigin origin, [LuaPayload(typeof(MapRegionEventPayload))] ScriptValue.Func handler) =>
        On("mapRegionUnloaded", origin, handler, occurred =>
            api.Event.MapRegionUnloaded += (at, _) => occurred(Region(at)));

    /// <summary>
    /// Called after a player changes which hotbar slot they are holding. Ask
    /// <c>moontweaks.inventory.held</c> what is now in their hand — the event says who
    /// changed it rather than carrying the slot, since what is in it is the useful part.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerChangeSlot")]
    public void OnPlayerChangeSlot(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerChangeSlot", origin, handler, occurred =>
            api.Event.AfterActiveSlotChanged += (player, _) => occurred(new PlayerEventPayload(player)));

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
    /// Raised wherever the game happened to spawn it, chunk generation included, so
    /// this is delivered on the following tick — and the thing it describes may
    /// already be gone, as a creature generated into a chunk nobody stayed near is.
    /// Ask <c>moontweaks.entities.isLoaded</c> before reaching for it.
    ///
    /// Worldgen fills a chunk with creatures at once, so a busy server calls this in
    /// bursts. Decide whether the code is one worth caring about before doing anything
    /// that costs.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entitySpawn")]
    public void OnEntitySpawn(
        ScriptOrigin origin, [LuaPayload(typeof(EntityEventPayload))] ScriptValue.Func handler) =>
        OnAnyThread("entitySpawn", origin, handler, occurred =>
            api.Event.OnEntitySpawn += entity => occurred(new EntityEventPayload(entity)));

    /// <summary>
    /// Called when something comes back with the chunk it was saved in. The
    /// counterpart of a despawn whose reason was <c>unload</c>: the same creature
    /// returning rather than a new one appearing, so this is where whatever was
    /// remembered about it is put back.
    /// </summary>
    /// <inheritdoc cref="OnEntitySpawn" path="/remarks"/>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityLoaded")]
    public void OnEntityLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(EntityEventPayload))] ScriptValue.Func handler) =>
        OnAnyThread("entityLoaded", origin, handler, occurred =>
            api.Event.OnEntityLoaded += entity => occurred(new EntityEventPayload(entity)));

    /// <summary>
    /// Called when anything alive dies, rather than players alone. <c>byPlayer</c>
    /// names whoever is responsible where one is, so an arrow names the archer rather
    /// than the arrow.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityDeath")]
    public void OnEntityDeath(
        ScriptOrigin origin, [LuaPayload(typeof(EntityDeathEventPayload))] ScriptValue.Func handler) =>
        OnAnyThread("entityDeath", origin, handler, occurred =>
            api.Event.OnEntityDeath += (entity, cause) =>
                occurred(new EntityDeathEventPayload(entity, cause)));

    /// <summary>
    /// Called before anything alive takes damage, with the amount the game is about to
    /// apply, and answered by what the handler returns: a number to apply that much
    /// instead, or nothing to leave it alone. Zero prevents the hurt entirely.
    /// </summary>
    /// <remarks>
    /// The game raises this per entity rather than for the world, so each entity is
    /// listened to as it spawns or comes back with its chunk. Everything that hurts
    /// through the game's own damage path arrives here, which is every weapon, fall,
    /// fire and creature; a mod writing health directly does not.
    ///
    /// Handlers are asked in turn and each is told what the one before it left, so a
    /// script halving damage and a later one adding two both have their say and the
    /// last word stands. Raised on the thread the server ticks on, where the answer
    /// is needed, so the same guard applies as to the other answering events: asked
    /// from anywhere else, the amount is left as it was found.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens, and may answer.</param>
    [LuaFunction("entityDamaged")]
    public void OnEntityDamaged(
        ScriptOrigin origin,
        [LuaPayload(typeof(EntityDamagedEventPayload), Returns = "number|nil")] ScriptValue.Func handler) =>
        OnAmended("entityDamaged", origin, handler, amend => HookHealth(damaged = amend));

    /// <summary>
    /// Called before anything alive is healed, with the amount the game is about to
    /// apply, and answered by what the handler returns: a number to apply that much
    /// instead, or nothing to leave it alone.
    /// </summary>
    /// <remarks>
    /// Listened to per entity exactly as <c>entityDamaged</c> is, and covering the same
    /// path: a poultice, a bandage, a respawn, and anything a mod heals the way the
    /// game does. Natural regeneration writes health directly and never arrives here.
    /// The game does not say who applied a heal, so a script crediting a healer reads
    /// who is nearby, sneaking and looking at the entity, which the player module
    /// answers.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens, and may answer.</param>
    [LuaFunction("entityHealed")]
    public void OnEntityHealed(
        ScriptOrigin origin,
        [LuaPayload(typeof(EntityHealedEventPayload), Returns = "number|nil")] ScriptValue.Func handler) =>
        OnAmended("entityHealed", origin, handler, amend => HookHealth(healed = amend));

    /// <summary>
    /// Called when something leaves the world, however it went. Read <c>reason</c>
    /// before concluding anything is gone for good: <c>unload</c> means its chunk left
    /// memory and it will be back.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityDespawn")]
    public void OnEntityDespawn(
        ScriptOrigin origin, [LuaPayload(typeof(EntityDespawnEventPayload))] ScriptValue.Func handler) =>
        OnAnyThread("entityDespawn", origin, handler, occurred =>
            api.Event.OnEntityDespawn += (entity, reason) =>
                occurred(new EntityDespawnEventPayload(entity, reason)));

    /// <summary>
    /// Called when a mount's rider changes the pace it is ridden at. The table
    /// describes the mount rather than the rider, so <c>id</c> is the horse and
    /// <c>gait</c> is what it is now doing.
    /// </summary>
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
    ///
    /// Only a mount whose rider's own client reports its position raises this at all,
    /// so the pace is one a rider chose rather than one the server worked out. Nothing
    /// is raised for a creature the server is walking itself.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("mountGaitChanged")]
    public void OnMountGaitChanged(
        ScriptOrigin origin, [LuaPayload(typeof(MountGaitEventPayload))] ScriptValue.Func handler) =>
        OnAnyThread("mountGaitChanged", origin, handler, occurred =>
            api.Event.MountGaitReceived += (mount, gait) =>
            {
                if (Changed(mount.EntityId, gait)) occurred(new MountGaitEventPayload(mount, gait));
            });

    /// <summary>
    /// Called when something climbs onto something else. <c>id</c> is whoever climbed
    /// on and <c>mount</c> is what they climbed onto.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityMounted")]
    public void OnEntityMounted(
        ScriptOrigin origin, [LuaPayload(typeof(EntityMountEventPayload))] ScriptValue.Func handler) =>
        OnAnyThread("entityMounted", origin, handler, occurred =>
            api.Event.EntityMounted += (entity, seat) =>
                occurred(new EntityMountEventPayload(entity, seat)));

    /// <summary>Called when something gets off what it was riding.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("entityUnmounted")]
    public void OnEntityUnmounted(
        ScriptOrigin origin, [LuaPayload(typeof(EntityMountEventPayload))] ScriptValue.Func handler) =>
        OnAnyThread("entityUnmounted", origin, handler, occurred =>
            api.Event.EntityUnmounted += (entity, seat) =>
                occurred(new EntityMountEventPayload(entity, seat)));

    /// <summary>Called when a player joins.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerJoin")]
    public void OnPlayerJoin(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerJoin", origin, handler, occurred =>
            api.Event.PlayerJoin += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called when a player dies.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerDeath")]
    public void OnPlayerDeath(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerDeath", origin, handler, occurred =>
            api.Event.PlayerDeath += (player, _) => occurred(new PlayerEventPayload(player)));

    /// <summary>Called when a player respawns.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerRespawn")]
    public void OnPlayerRespawn(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerRespawn", origin, handler, occurred =>
            api.Event.PlayerRespawn += player => occurred(new PlayerEventPayload(player)));

    /// <summary>
    /// Called the first time a player ever joins this world, before they are welcomed.
    /// Every later join raises <c>playerJoin</c> alone, so this is where anything
    /// given once belongs.
    /// </summary>
    /// <remarks>
    /// Raised only for a player the world has never seen, and before the welcome
    /// message, so a starter kit handed out here arrives with them.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerCreate")]
    public void OnPlayerCreate(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerCreate", origin, handler, occurred =>
            api.Event.PlayerCreate += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called once a joining player is in the world and has been welcomed.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerNowPlaying")]
    public void OnPlayerNowPlaying(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerNowPlaying", origin, handler, occurred =>
            api.Event.PlayerNowPlaying += player => occurred(new PlayerEventPayload(player)));

    /// <summary>
    /// Called when a joining player's client reports that it has finished. The last
    /// of the three events a join raises, and the one after which the player is
    /// certainly able to be spoken to.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerReady")]
    public void OnPlayerReady(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerReady", origin, handler, occurred =>
            api.Event.PlayerReady += player => occurred(new PlayerEventPayload(player)));

    /// <summary>
    /// Called when a player quits of their own accord, before they are removed. One
    /// who was kicked or who lost their connection raises <c>playerDisconnect</c> only.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerLeave")]
    public void OnPlayerLeave(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerLeave", origin, handler, occurred =>
            api.Event.PlayerLeave += player => occurred(new PlayerEventPayload(player)));

    /// <summary>
    /// Called as a player is removed, however they went: a quit, a kick and a lost
    /// connection all reach here, so this is the one that always runs.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerDisconnect")]
    public void OnPlayerDisconnect(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerDisconnect", origin, handler, occurred =>
            api.Event.PlayerDisconnect += player => occurred(new PlayerEventPayload(player)));

    /// <summary>
    /// Called after a player changes game mode, so asking them their mode gives the
    /// one they changed to.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("playerSwitchGameMode")]
    public void OnPlayerSwitchGameMode(
        ScriptOrigin origin, [LuaPayload(typeof(PlayerEventPayload))] ScriptValue.Func handler) =>
        On("playerSwitchGameMode", origin, handler, occurred =>
            api.Event.PlayerSwitchGameMode += player => occurred(new PlayerEventPayload(player)));

    /// <summary>Called once the save game has been read, which is after every script has run.</summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("saveGameLoaded")]
    public void OnSaveGameLoaded(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        On("saveGameLoaded", origin, handler, occurred =>
            api.Event.SaveGameLoaded += () => occurred(ServerEventPayload.Instance));

    /// <summary>
    /// Called on the one start where the world is brand new, immediately before
    /// <c>saveGameLoaded</c>. Never called again for that world.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("saveGameCreated")]
    public void OnSaveGameCreated(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        On("saveGameCreated", origin, handler, occurred =>
            api.Event.SaveGameCreated += () => occurred(ServerEventPayload.Instance));

    /// <summary>
    /// Called as the world is written to disk, which a server does periodically and
    /// again as it shuts down. Anything a script wants saved with the world should be
    /// written by the time this returns.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("gameWorldSave")]
    public void OnGameWorldSave(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        On("gameWorldSave", origin, handler, occurred =>
            api.Event.GameWorldSave += () => occurred(ServerEventPayload.Instance));

    /// <summary>
    /// Called once the world generators are starting, which is the last thing a
    /// server does before it begins ticking.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("worldgenStartup")]
    public void OnWorldgenStartup(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
        On("worldgenStartup", origin, handler, occurred =>
            api.Event.WorldgenStartup += () => occurred(ServerEventPayload.Instance));

    /// <summary>
    /// Called when a server that had suspended itself for want of players wakes up
    /// again. Servers that never stand by never raise it.
    /// </summary>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time it happens.</param>
    [LuaFunction("serverResume")]
    public void OnServerResume(
        ScriptOrigin origin, [LuaPayload(typeof(ServerEventPayload))] ScriptValue.Func handler) =>
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
    /// Called before the server lets somebody act on a place, and answered by what the
    /// handler returns: one of the same words <c>world.testAccess</c> reads back, or
    /// nothing to leave the decision alone.
    /// </summary>
    /// <remarks>
    /// The one event here a handler decides rather than observes. It is asked for every
    /// block a player breaks and every block they use, after the land claim check and
    /// with that check's answer on <c>e.allowed</c>, so a handler is the last word
    /// rather than the first. The game asks every mod in turn, handing each the answer
    /// the one before it gave, and takes the last answer as the decision — so a handler
    /// may refuse what the claim allowed and may allow what the claim refused. Both
    /// directions are the game's own behaviour and both are offered, which means
    /// answering <c>"granted"</c> overrides a claim and opens somebody's land to
    /// whoever asked; a handler meaning only to refuse should return nothing wherever
    /// it does not mean to refuse.
    ///
    /// The server asks this constantly, so a handler here is on a hot path in a way no
    /// other event is: keep it to arithmetic and a table lookup, and read nothing that
    /// searches the world.
    ///
    /// Answered where it is asked. The server's own two callers — a player breaking a
    /// block and a player using one — ask on the thread it ticks on, but another mod
    /// calling the same check may ask from any thread, and an answer marshalled onto
    /// the main thread would arrive after the decision was made. So a call arriving
    /// anywhere else leaves the answer exactly as it found it, and says so once: a
    /// protection written here holds for players and may not hold against another mod
    /// reaching past them.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time somebody is tested, and answers.</param>
    [LuaFunction("testBlockAccess")]
    public void OnTestBlockAccess(
        ScriptOrigin origin,
        [LuaPayload(typeof(AccessTestEventPayload), Returns = "EnumAccessResponse|nil")]
        ScriptValue.Func handler) =>
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
    /// This is how chat is filtered, prefixed, muted or routed somewhere else. Handlers
    /// are asked in turn and each is given what the one before it left, so a rewrite
    /// chains and the last word stands. That is the game's own rule for this event and
    /// it is followed exactly, which is worth knowing in one direction particularly: a
    /// handler answering <c>true</c> undoes a swallow an earlier one asked for, so a
    /// script that mutes players can be undone by a later script that knows nothing
    /// about muting. Scripts run in name order, so anything that must have the last
    /// word belongs in a file that sorts last, and a handler that does not mean to
    /// interfere should return nothing rather than <c>true</c>.
    ///
    /// The message reaches the group named on the event, so a handler that means to
    /// send it somewhere else swallows it and calls <c>moontweaks.groups.say</c>.
    ///
    /// Raised on the thread the server ticks on, where the answer is needed, so the
    /// same guard applies as to the other answering event: asked from anywhere else,
    /// the message is left exactly as it was found.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="handler">Called each time somebody says something, and answers.</param>
    [LuaFunction("playerChat")]
    public void OnPlayerChat(
        ScriptOrigin origin,
        [LuaPayload(typeof(ChatEventPayload), Returns = "string|boolean|nil")]
        ScriptValue.Func handler) =>
        OnChat("playerChat", origin, handler, said =>
            api.Event.PlayerChat += (IServerPlayer player, int group, ref string message, ref string data, BoolRef consumed) =>
            {
                var (spoken, swallowed) = said(player, group, message, consumed.value);
                message = spoken;
                consumed.value = swallowed;
            });

    /// <summary>
    /// Called as the game tests whether a set of ingredients laid out in a grid makes
    /// one particular recipe, and answered by what the handler returns: <c>false</c> to
    /// refuse the recipe, or nothing to leave the game's own answer alone.
    /// </summary>
    /// <remarks>
    /// This is how a recipe is gated on something the recipe itself cannot say: who is
    /// crafting, where they are standing, what the server has decided about them. The
    /// recipe stays in the book and stays craftable by anybody the handler does not
    /// refuse, which is what separates this from removing it outright with
    /// <c>moontweaks.recipes.grid.remove</c>.
    ///
    /// A handler refuses and cannot permit. The game asks this before it checks the
    /// ingredients against the recipe at all and takes a <c>false</c> as the whole
    /// answer, so refusing stops a recipe that would otherwise have been made, while
    /// answering <c>true</c> only lets the game go on to decide for itself. There is
    /// no way from here to make an arrangement produce something it does not match.
    ///
    /// <c>output</c> is required and is not a convenience. The game asks this once per
    /// candidate recipe every time somebody moves an item in a crafting grid, so a
    /// handler called for every recipe would put an interpreter call on one of the
    /// busiest paths the server has. What is named here is matched before any of that,
    /// so a script watching one recipe costs a wildcard match per candidate and a call
    /// only for the recipes it asked about. It takes a <c>*</c> wildcard, so
    /// <c>"game:bread-*"</c> reaches a family.
    ///
    /// Answering does not consume anything: this decides whether the arrangement makes
    /// the recipe, and is asked again for every rearrangement, so a handler must not
    /// treat being called as somebody having crafted something. It is asked on the
    /// thread the server ticks on and answered there; the last handler to answer wins,
    /// which is the game's own rule.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="output">
    /// Code of what a recipe makes, which decides the recipes this handler hears about.
    /// Takes a <c>*</c> wildcard.
    /// </param>
    /// <param name="handler">Called for each matching recipe tested, and answers.</param>
    [LuaFunction("matchesGridRecipe")]
    public void OnMatchesGridRecipe(
        ScriptOrigin origin,
        [LuaSuggests(SuggestionSets.ASSET_CODE)] string output,
        [LuaPayload(typeof(RecipeMatchEventPayload), Returns = "false|nil")]
        ScriptValue.Func handler) =>
        OnRecipeMatch("matchesGridRecipe", output, origin, handler, () =>
            api.Event.MatchesGridRecipe += (player, recipe, ingredients, gridWidth) =>
                RaiseRecipeMatch("matchesGridRecipe", recipe,
                    new RecipeMatchEventPayload(player, recipe, ingredients, gridWidth), true));

    /// <summary>
    /// Called as the game tests whether a set of ingredients makes one particular
    /// recipe of a kind that is not laid out in a grid — a barrel's, an anvil's, a clay
    /// form's or a knapping surface's — and answered the same way
    /// <c>matchesGridRecipe</c> is: <c>false</c> to refuse, nothing to leave it alone.
    /// </summary>
    /// <remarks>
    /// The same event as <c>matchesGridRecipe</c> in everything but which kinds it
    /// covers, down to refusing rather than permitting, and <c>output</c> is required
    /// here for the same reason. It is asked less often, being raised as somebody works
    /// a barrel or an anvil rather than as they rearrange a crafting grid, but the two
    /// share a shape so that a script gating a recipe writes the same handler whichever
    /// kind makes it.
    ///
    /// <c>gridWidth</c> is zero here, since none of these kinds has a layout.
    /// </remarks>
    /// <param name="origin">Script line adding the handler.</param>
    /// <param name="output">
    /// Code of what a recipe makes, which decides the recipes this handler hears about.
    /// Takes a <c>*</c> wildcard.
    /// </param>
    /// <param name="handler">Called for each matching recipe tested, and answers.</param>
    [LuaFunction("matchesRecipe")]
    public void OnMatchesRecipe(
        ScriptOrigin origin,
        [LuaSuggests(SuggestionSets.ASSET_CODE)] string output,
        [LuaPayload(typeof(RecipeMatchEventPayload), Returns = "false|nil")]
        ScriptValue.Func handler) =>
        OnRecipeMatch("matchesRecipe", output, origin, handler, () =>
            api.Event.MatchesRecipe += (player, recipe, ingredients) =>
                RaiseRecipeMatch("matchesRecipe", recipe,
                    new RecipeMatchEventPayload(player, recipe, ingredients, 0), true));

    private void OnAnyThread(
        string name, ScriptOrigin origin, ScriptValue.Func handler, Action<Occurred> subscribe) =>
        On(name, origin, handler, occurred => subscribe(
            about => api.Event.EnqueueMainThreadTask(() => occurred(about), $"moontweaks:{name}")));

    /// <summary>
    /// Asks whoever is listening whether one recipe may be made, and gives back the
    /// last answer. A handler that answers nothing leaves the recipe as it found it.
    /// </summary>
    /// <remarks>
    /// The game asks this while somebody arranges ingredients, once per candidate
    /// recipe per rearrangement, which is a different order of frequency from any
    /// other event bound here. Three things keep that affordable, in the order they
    /// are reached:
    ///
    /// Nothing is subscribed until a script asks, so a server whose scripts want none
    /// of this pays nothing at all. Then the output code each handler named is matched
    /// here, in C#, before a payload is built or the interpreter entered — so a script
    /// watching one recipe pays a wildcard match per candidate rather than a call.
    /// Only what survives both crosses into Lua.
    ///
    /// The filter is required rather than offered, which is why it is on
    /// <see cref="Handler"/> rather than left to a handler's own first line: a script
    /// cannot ask for every recipe by omitting it, and so cannot put an interpreter
    /// call on this path without naming what it is for.
    /// </remarks>
    private bool RaiseRecipeMatch(string name, IRecipeBase recipe, EventPayload about, bool matched)
    {
        if (handlers.GetValueOrDefault(name) is not { Count: > 0 } listening) return matched;

        // Answered where it is asked, for the reason RaiseAnswered gives: the game
        // wants the answer now, and one marshalled onto the main thread would arrive
        // after the recipe was decided.
        if (Environment.CurrentManagedThreadId != mainThread)
        {
            if (offThread.Add(name))
            {
                api.Logger.Warning(
                    "[moontweaks] '{0}' was asked from another thread and was left to the server to answer. "
                    + "A script cannot answer it there, so handlers for it do not run for whatever asked.",
                    name);
            }

            return matched;
        }

        var made = recipe.RecipeOutput?.ResolvedItemStack?.Collectible?.Code;
        ScriptValue.Map? payload = null;
        var decided = matched;

        foreach (var handler in listening.ToArray())
        {
            // The whole point of the filter: a recipe this handler did not ask about
            // costs a wildcard match and nothing else.
            if (handler.Filter is { } wanted && (made is null || !WildcardUtil.Match(wanted, made)))
            {
                continue;
            }

            // Built once, and only for a recipe something actually asked about.
            payload ??= PayloadWriter.Table(about);

            try
            {
                // Only a refusal is acted on. The game asks this before it matches the
                // ingredients at all and takes a false as the whole answer, so there is
                // nothing a true could mean here that leaving it alone does not already
                // mean, and reading one would promise a script something this event
                // cannot do.
                if (handler.Call.Call([payload]) is ScriptValue.Bool { Value: false }) decided = false;
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
    /// Adds a handler that hears only about recipes making what it named, and
    /// remembers to subscribe the first time one arrives.
    /// </summary>
    /// <remarks>
    /// Subscribing is deferred to <see cref="Activate"/> exactly as <see cref="On"/>
    /// defers it, and for the same reason: a run that is thrown away leaves the game's
    /// own events as it found them. Each event takes out its own subscription, so a
    /// script asking for one of the two does not put a handler on the other.
    /// </remarks>
    private void OnRecipeMatch(
        string name, string output, ScriptOrigin origin, ScriptValue.Func handler, Action subscribe)
    {
        if (!handlers.TryGetValue(name, out var listening))
        {
            handlers[name] = listening = [];
            pending.Add((name, subscribe));
        }

        listening.Add(new Handler(origin, handler, new AssetLocation(output)));
    }

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
    /// Adds a handler for an event whose answer is an amount, subscribing the first
    /// time one arrives. The same bookkeeping <see cref="On"/> does, differing only in
    /// that a number a handler returns is carried back rather than dropped.
    /// </summary>
    private void OnAmended(
        string name, ScriptOrigin origin, ScriptValue.Func handler, Action<Amended> subscribe)
    {
        if (!handlers.TryGetValue(name, out var listening))
        {
            handlers[name] = listening = [];
            pending.Add((name, () => subscribe((describe, amount) => RaiseAmended(name, describe, amount))));
        }

        listening.Add(new Handler(origin, handler));
    }

    /// <summary>
    /// Puts the listeners in place that hook every entity's health as it appears,
    /// once for both events: the game raises damage and healing through one delegate
    /// per entity, and which of the two a call is only shows once it arrives.
    /// </summary>
    private void HookHealth(Amended _)
    {
        if (hooking) return;
        hooking = true;

        api.Event.OnEntitySpawn += Hook;
        api.Event.OnEntityLoaded += Hook;
    }

    /// <summary>Hooks one entity's health, unless it has none or is hooked already.</summary>
    private void Hook(Entity entity)
    {
        if (entity.GetBehavior<EntityBehaviorHealth>() is not { } health) return;
        if (hooked.TryGetValue(health, out _)) return;

        hooked.Add(health, health);
        health.onDamaged += (amount, cause) => Amend(entity, cause, amount);
    }

    /// <summary>
    /// What the game should apply, after every handler for the kind of call this is
    /// has had its say. Healing is damage of a kind to the game, so the type on the
    /// source is what tells the two apart.
    /// </summary>
    private float Amend(Entity entity, DamageSource cause, float amount)
    {
        var healing = cause.Type == EnumDamageType.Heal;
        var amend = healing ? healed : damaged;
        if (amend is null) return amount;

        System.Func<double, EventPayload> describe = healing
            ? value => new EntityHealedEventPayload(entity, cause, value)
            : value => new EntityDamagedEventPayload(entity, cause, value);

        return (float)amend(describe, amount);
    }

    /// <summary>
    /// Calls every handler for an amount, each told what the one before it left, and
    /// carries back what should be applied. A handler answering a number below zero
    /// is read as zero, since a negative heal is not a hurt and the game would treat
    /// it as one.
    /// </summary>
    /// <remarks>
    /// A fresh table per handler, for the reason chat rebuilds its own: what a handler
    /// is told has to be what the handler before it decided. Refuses to run anywhere
    /// but the thread the server ticks on, for the reason <see cref="RaiseAnswered"/>
    /// gives: the answer is needed where it was asked.
    /// </remarks>
    private double RaiseAmended(string name, System.Func<double, EventPayload> describe, double amount)
    {
        if (handlers.GetValueOrDefault(name) is not { Count: > 0 } listening) return amount;

        if (Environment.CurrentManagedThreadId != mainThread)
        {
            if (offThread.Add(name))
            {
                api.Logger.Warning(
                    "[moontweaks] '{0}' was asked from another thread and was left to the server to answer. "
                    + "A script cannot answer it there, so handlers for it do not run for whatever asked.",
                    name);
            }
            return amount;
        }

        var decided = amount;

        foreach (var handler in listening.ToArray())
        {
            try
            {
                if (handler.Call.Call([PayloadWriter.Table(describe(decided))]) is ScriptValue.Num answer)
                {
                    decided = Math.Max(0, answer.Value);
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
