using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.MathTools;

namespace MoonTweaks.World;

/// <summary>
/// The blocks and items of a world people are playing in.
/// </summary>
/// <remarks>
/// These act on a loaded world, so they belong in an event handler rather than in a
/// script's body: when scripts run, the recipes exist but the world does not.
/// </remarks>
/// <example>
/// <code>
/// local world = moontweaks.world
/// local ground = world.surfaceAt(500, 500)
///
/// if ground then
///   world.setBlock("game:cobblestone-granite", 500, ground + 1, 500)
/// end
///
/// -- One call for a whole box, where walking it would cost a call per block.
/// moontweaks.log.info(("%d ore block(s) below"):format(world.countBlocks {
///   x = 480, y = 20, z = 480, toX = 520, toY = 80, toZ = 520, code = "game:ore-*",
/// }))
/// </code>
/// </example>
[LuaModule("moontweaks.world")]
public sealed class WorldDomain(WorldAccess world, AssetStacks stacks)
{
    /// <summary>
    /// The code of the block standing at a position, or nil where nothing does.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The block to look at, east to west.</param>
    /// <param name="y">The block to look at, from the world's floor upwards.</param>
    /// <param name="z">The block to look at, north to south.</param>
    [LuaFunction("blockAt")]
    public string? BlockAt(ScriptOrigin origin, int x, int y, int z) => world.CodeAt(x, y, z);

    /// <summary>
    /// Puts a block somewhere, taking effect immediately. Each call relights and
    /// re-sends the chunk it touched, so use <c>queueBlock</c> for more than a few.
    /// </summary>
    /// <param name="origin">Script line placing it.</param>
    /// <param name="code">Block to place, or <c>game:air</c> to clear.</param>
    /// <param name="x">Where it goes, east to west.</param>
    /// <param name="y">Where it goes, from the world's floor upwards.</param>
    /// <param name="z">Where it goes, north to south.</param>
    [LuaFunction("setBlock")]
    public void SetBlock(
        ScriptOrigin origin,
        [LuaSuggests(SuggestionSets.ASSET_CODE)] string code, int x, int y, int z) =>
        world.Set(world.IdOf(code, origin), x, y, z, origin);

    /// <summary>
    /// Queues a block without writing it yet. Nothing appears until <c>commit</c>,
    /// which then relights and sends each touched chunk once however many blocks
    /// were queued in it.
    /// </summary>
    /// <param name="origin">Script line queueing it.</param>
    /// <param name="code">Block to place, or <c>game:air</c> to clear.</param>
    /// <param name="x">Where it goes, east to west.</param>
    /// <param name="y">Where it goes, from the world's floor upwards.</param>
    /// <param name="z">Where it goes, north to south.</param>
    [LuaFunction("queueBlock")]
    public void QueueBlock(
        ScriptOrigin origin,
        [LuaSuggests(SuggestionSets.ASSET_CODE)] string code, int x, int y, int z) =>
        world.Queue(world.IdOf(code, origin), x, y, z, origin);

    /// <summary>Writes everything queued, and says how many blocks that was.</summary>
    /// <remarks>
    /// This is also what closes one step of the history <c>undo</c> walks back
    /// through, so a shape queued and committed is taken back in one call however
    /// many blocks it holds. Committing with nothing queued writes nothing and
    /// records no step.
    /// </remarks>
    /// <param name="origin">Script line committing.</param>
    [LuaFunction("commit")]
    public int Commit(ScriptOrigin origin) => world.Commit(origin);

    /// <summary>
    /// Puts back what this script's last write changed, and says how many blocks that
    /// was. Nothing left to undo answers zero.
    /// </summary>
    /// <remarks>
    /// One step is one <c>setBlock</c>, or one <c>commit</c> however many blocks were
    /// queued into it — so a structure built through <c>queueBlock</c> is taken back
    /// whole, and a loop of <c>setBlock</c> is taken back one block at a time. That is
    /// the second reason to queue.
    ///
    /// The history belongs to the script that wrote it. A script undoes what it wrote
    /// and cannot reach what another script wrote underneath it, so an undo means
    /// something an author can predict from their own file.
    ///
    /// What it covers is what was written: <c>setBlock</c> and <c>queueBlock</c>.
    /// <c>breakBlock</c> and <c>exchangeBlock</c> are outside it — a break has already
    /// scattered its drops and played its sound, and neither can be taken back by
    /// putting the block where it stood.
    ///
    /// How far back it goes is the server's <c>undoHistory</c> setting, because every
    /// step held is the blocks of that step kept in memory. Past that depth the oldest
    /// step is dropped.
    /// </remarks>
    /// <param name="origin">Script line undoing.</param>
    [LuaFunction("undo")]
    public int Undo(ScriptOrigin origin) => world.Undo(origin);

    /// <summary>
    /// Puts back what this script's last <c>undo</c> took away, and says how many
    /// blocks that was. Nothing to redo answers zero.
    /// </summary>
    /// <remarks>
    /// Writing anything new after an undo throws away what could have been redone,
    /// as every editor does and for the same reason: the history is a line rather
    /// than a tree.
    /// </remarks>
    /// <param name="origin">Script line redoing.</param>
    [LuaFunction("redo")]
    public int Redo(ScriptOrigin origin) => world.Redo(origin);

    /// <summary>
    /// Drops a stack into the world, as a broken block would. A <c>velocity</c>
    /// throws it rather than letting it fall where it was put, and an <c>owner</c>
    /// keeps that player from collecting it for a second, together are what let
    /// a script put something down without it going straight back where it came from.
    /// </summary>
    /// <param name="origin">Script line dropping it.</param>
    /// <param name="drop">What to drop, where, and how hard.</param>
    [LuaFunction("dropItem")]
    public void DropItem(ScriptOrigin origin, DropSpec drop) =>
        world.Drop(
            stacks.Resolved(drop.Stack, origin, "stack"),
            new Vec3d(drop.X, drop.Y, drop.Z),
            drop.Velocity is { } thrown ? new Vec3d(thrown.X, thrown.Y, thrown.Z) : null,
            drop.Owner);

    /// <summary>
    /// Breaks a block properly: its drops land, its sound plays, and whatever stood
    /// on it is told. <c>setBlock</c> to <c>game:air</c> removes it instead, silently
    /// and with nothing left behind, which is the one to use for clearing ground.
    /// </summary>
    /// <param name="origin">Script line breaking it.</param>
    /// <param name="broken">Which block to break, for whom, and how much it pays out.</param>
    [LuaFunction("breakBlock")]
    public void BreakBlock(ScriptOrigin origin, BreakSpec broken) =>
        world.Break(broken.X, broken.Y, broken.Z, broken.Player, broken.DropMultiplier, origin);

    /// <summary>
    /// Swaps the block at a position for another, leaving whatever stands in it
    /// alone. A chest exchanged this way keeps what was inside it, where the same
    /// chest written with <c>setBlock</c> comes back empty.
    /// </summary>
    /// <param name="origin">Script line swapping it.</param>
    /// <param name="code">Block to put there instead.</param>
    /// <param name="x">The block to swap, east to west.</param>
    /// <param name="y">The block to swap, from the world's floor upwards.</param>
    /// <param name="z">The block to swap, north to south.</param>
    [LuaFunction("exchangeBlock")]
    public void ExchangeBlock(
        ScriptOrigin origin,
        [LuaSuggests(SuggestionSets.ASSET_CODE)] string code, int x, int y, int z) =>
        world.Exchange(world.IdOf(code, origin), x, y, z);

    /// <summary>
    /// Whether the chunk holding a position is loaded. Writing to one that is not
    /// does nothing at all and says nothing about it, so anything acting far from a
    /// player should ask first.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The position asked about, east to west.</param>
    /// <param name="y">The position asked about, from the world's floor upwards.</param>
    /// <param name="z">The position asked about, north to south.</param>
    [LuaFunction("isLoaded")]
    public bool IsLoaded(ScriptOrigin origin, int x, int y, int z) => world.IsLoaded(x, y, z);

    /// <summary>
    /// Asks for the chunk holding a position to be brought in, and says whether it was
    /// already there. Answering false is the ordinary case rather than a failure: it
    /// means the request was made.
    /// </summary>
    /// <remarks>
    /// The one way a script reaches somewhere nobody is standing. Everything that
    /// writes to the world does nothing at all in a chunk that is not loaded, and says
    /// nothing about it, so a script acting far from a player asks for the chunk, waits
    /// a tick or two, and checks <c>isLoaded</c> before it writes:
    ///
    /// <code>
    /// if not world.loadChunk(x, z) then
    ///   moontweaks.server.after(2000, function()
    ///     if world.isLoaded(x, 64, z) then build(x, z) end
    ///   end)
    /// end
    /// </code>
    ///
    /// The chunk is not held open once it arrives. It unloads again on the game's own
    /// terms, so a script must not assume a chunk it asked for an hour ago is still
    /// there.
    /// </remarks>
    /// <param name="origin">Script line asking for it.</param>
    /// <param name="x">Any block in the column, east to west.</param>
    /// <param name="z">Any block in the column, north to south.</param>
    [LuaFunction("loadChunk")]
    public bool LoadChunk(ScriptOrigin origin, int x, int z) => world.Load(x, z);

    /// <summary>
    /// The height of the ground in one column, or nil where that column is not
    /// loaded. Read from the map the world already keeps rather than by looking down
    /// a block at a time, so this costs one call where the obvious way costs a
    /// hundred.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">Which column, east to west.</param>
    /// <param name="z">Which column, north to south.</param>
    [LuaFunction("surfaceAt")]
    public int? SurfaceAt(ScriptOrigin origin, int x, int z) => world.Surface(x, z);

    /// <summary>
    /// Where this world puts somebody who has no spawn of their own, as a table of
    /// <c>x</c>, <c>y</c> and <c>z</c>, or nil where the game cannot work one out.
    /// Read <c>moontweaks.players.spawn</c> for where one particular player would
    /// land, which is this only when nothing nearer to them has an answer.
    /// </summary>
    /// <remarks>
    /// A centre rather than a block, in both senses: the position is the middle of the
    /// block, and the server scatters arrivals across the radius its configuration
    /// names rather than standing them all on it.
    /// </remarks>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("spawn")]
    public VectorPayload? Spawn(ScriptOrigin origin) => world.Spawn();

    /// <summary>
    /// Moves the world's own spawn, so that anybody with no spawn of their own arrives
    /// here instead. Saved with the world.
    /// </summary>
    /// <remarks>
    /// This is where a player who has never set a spawn starts, and where clearing a
    /// player's own spawn returns them to. Moving it moves nobody who already has one
    /// of their own — <c>moontweaks.players.clearSpawn</c> is what gives them back to
    /// the world's.
    /// </remarks>
    /// <param name="origin">Script line moving it.</param>
    /// <param name="x">Where it goes, east to west.</param>
    /// <param name="y">Where it goes, from the world's floor upwards.</param>
    /// <param name="z">Where it goes, north to south.</param>
    [LuaFunction("setSpawn")]
    public void SetSpawn(ScriptOrigin origin, int x, int y, int z) => world.SetSpawn(x, y, z);

    /// <summary>
    /// How much light of one kind reaches a position, from 0 in the dark to the
    /// world's brightest.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="kind">Which light to count.</param>
    /// <param name="x">The position asked about, east to west.</param>
    /// <param name="y">The position asked about, from the world's floor upwards.</param>
    /// <param name="z">The position asked about, north to south.</param>
    [LuaFunction("lightAt")]
    public int LightAt(ScriptOrigin origin, EnumLightKind kind, int x, int y, int z) =>
        world.Light(x, y, z, kind);

    /// <summary>
    /// What the weather and the ground are like at a position. This is how something
    /// is made to depend on where it happened rather than only on what happened.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The position asked about, east to west.</param>
    /// <param name="y">The position asked about, from the world's floor upwards.</param>
    /// <param name="z">The position asked about, north to south.</param>
    [LuaFunction("climateAt")]
    public ClimatePayload ClimateAt(ScriptOrigin origin, int x, int y, int z) =>
        world.Climate(x, y, z);

    /// <summary>
    /// Which way the wind blows at a position and how hard, as a direction whose
    /// length is its speed.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The position asked about, east to west.</param>
    /// <param name="y">The position asked about, from the world's floor upwards.</param>
    /// <param name="z">The position asked about, north to south.</param>
    [LuaFunction("windAt")]
    public VectorPayload WindAt(ScriptOrigin origin, int x, int y, int z) => world.Wind(x, y, z);

    /// <summary>
    /// Every block in a box that a code names, and where each stands. One call
    /// whatever the box holds, where walking it with <c>blockAt</c> costs a call per
    /// block — which is why this exists rather than being left to a loop.
    /// </summary>
    /// <remarks>
    /// Chunks that are not loaded hold nothing to look at and are stepped over, so a
    /// box reaching past what is loaded answers for the part of it that is loaded.
    /// Ask <c>isLoaded</c> about a corner first where that matters.
    ///
    /// The search stops once it has <c>limit</c> matches, so a box holding more than
    /// that is not read to the end. Raise it deliberately: every match is a table.
    /// </remarks>
    /// <param name="origin">Script line asking.</param>
    /// <param name="region">Which box to search, and what to look for in it.</param>
    [LuaFunction("findBlocks")]
    public IReadOnlyList<BlockAtPayload> FindBlocks(ScriptOrigin origin, RegionSpec region) =>
        world.Find(region, out _);

    /// <summary>
    /// How many blocks in a box a code names. Cheaper than counting what
    /// <c>findBlocks</c> hands back, since nothing is described that is only going to
    /// be counted.
    /// </summary>
    /// <inheritdoc cref="FindBlocks" path="/remarks"/>
    /// <param name="origin">Script line asking.</param>
    /// <param name="region">Which box to search, and what to count in it.</param>
    [LuaFunction("countBlocks")]
    public int CountBlocks(ScriptOrigin origin, RegionSpec region) => world.Count(region, out _);

    /// <summary>
    /// Whether a player may build at a place, and what stops them where they may not.
    /// Reading it enforces nothing; it is how a script refuses politely rather than
    /// writing over somebody's claim.
    /// </summary>
    /// <remarks>
    /// <c>setBlock</c> and the rest do not ask this on a script's behalf. They write
    /// as the server rather than as a player, which is right for a script laying out
    /// terrain and wrong for one acting on what a player asked for — so the choice is
    /// left here, where the script knows which it is doing.
    /// </remarks>
    /// <param name="origin">Script line asking.</param>
    /// <param name="access">Who wants to act, where, and what they want to do.</param>
    [LuaFunction("testAccess")]
    public EnumAccessResponse TestAccess(ScriptOrigin origin, AccessSpec access) =>
        world.Access(access, origin);

    /// <summary>
    /// Plays one of the game's own sounds at a place, which everybody near enough
    /// hears. Nothing needs installing on their machine.
    /// </summary>
    /// <param name="origin">Script line playing it.</param>
    /// <param name="sound">Which sound, where, how loud and how far.</param>
    [LuaFunction("playSound")]
    public void PlaySound(ScriptOrigin origin, SoundSpec sound) => world.Play(sound);

    /// <summary>
    /// Throws off particles at a place, drawn on every screen near enough to see them.
    /// Given one point they appear there; given two they fill the box between.
    /// </summary>
    /// <param name="origin">Script line spawning them.</param>
    /// <param name="particles">Where they appear, how many, what colour and how they move.</param>
    [LuaFunction("spawnParticles")]
    public void SpawnParticles(ScriptOrigin origin, ParticlesSpec particles) =>
        world.Particles(particles, origin);

    /// <summary>
    /// Outlines a set of blocks on one player's screen. Nothing needs installing on
    /// their machine: this is the game's own area-selection drawing, which a server
    /// may point at whatever it likes.
    /// </summary>
    /// <remarks>
    /// A slot holds one set until it is given another, so passing an empty list of
    /// blocks under the same slot is how a drawing is taken back.
    /// </remarks>
    /// <param name="origin">Script line drawing them.</param>
    /// <param name="highlight">Who to draw for, which blocks, and in what color.</param>
    [LuaFunction("highlight")]
    public void Highlight(ScriptOrigin origin, HighlightSpec highlight) =>
        world.Highlight(highlight, origin);

    /// <summary>
    /// Remembers something about the world, saved with the save game rather than with
    /// any player and so still there after a restart. The counterpart of
    /// <c>moontweaks.players.setWorldData</c>, and the only home for anything counted
    /// across everybody rather than for each of them.
    /// </summary>
    /// <param name="origin">Script line storing it.</param>
    /// <param name="key">Name to store it under.</param>
    /// <param name="value">The value to store. Any value a script can write, a table included.</param>
    [LuaFunction("setData")]
    public void SetData(ScriptOrigin origin, string key, ScriptValue value) =>
        world.Remember(key, value, origin);

    /// <summary>What was remembered about the world under a name, or nil when nothing was.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="key">Name it was stored under.</param>
    [LuaFunction("getData")]
    public ScriptValue GetData(ScriptOrigin origin, string key) => world.Recall(key, origin);
}
