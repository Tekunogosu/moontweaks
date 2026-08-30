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
[LuaModule("moontweaks.world")]
public sealed class WorldDomain(WorldAccess world, AssetStacks stacks)
{
    /// <summary>
    /// The code of the block standing at a position, or nil where nothing does.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">Where to look.</param>
    /// <param name="y">Where to look.</param>
    /// <param name="z">Where to look.</param>
    [LuaFunction("blockAt")]
    public string? BlockAt(ScriptOrigin origin, int x, int y, int z) => world.CodeAt(x, y, z);

    /// <summary>
    /// Puts a block somewhere, taking effect immediately. Each call relights and
    /// re-sends the chunk it touched, so use <c>queueBlock</c> for more than a few.
    /// </summary>
    /// <param name="origin">Script line placing it.</param>
    /// <param name="code">Block to place, or <c>game:air</c> to clear.</param>
    /// <param name="x">Where to put it.</param>
    /// <param name="y">Where to put it.</param>
    /// <param name="z">Where to put it.</param>
    [LuaFunction("setBlock")]
    public void SetBlock(
        ScriptOrigin origin,
        [LuaSuggests(SuggestionSets.ASSET_CODE)] string code, int x, int y, int z) =>
        world.Set(world.IdOf(code, origin), x, y, z);

    /// <summary>
    /// Queues a block without writing it yet. Nothing appears until <c>commit</c>,
    /// which then relights and sends each touched chunk once however many blocks
    /// were queued in it.
    /// </summary>
    /// <param name="origin">Script line queueing it.</param>
    /// <param name="code">Block to place, or <c>game:air</c> to clear.</param>
    /// <param name="x">Where to put it.</param>
    /// <param name="y">Where to put it.</param>
    /// <param name="z">Where to put it.</param>
    [LuaFunction("queueBlock")]
    public void QueueBlock(
        ScriptOrigin origin,
        [LuaSuggests(SuggestionSets.ASSET_CODE)] string code, int x, int y, int z) =>
        world.Queue(world.IdOf(code, origin), x, y, z);

    /// <summary>Writes everything queued, and says how many blocks that was.</summary>
    /// <param name="origin">Script line committing.</param>
    [LuaFunction("commit")]
    public int Commit(ScriptOrigin origin) => world.Commit();

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
    /// <param name="x">Which block to swap.</param>
    /// <param name="y">Which block to swap.</param>
    /// <param name="z">Which block to swap.</param>
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
    /// <param name="x">Where to ask.</param>
    /// <param name="y">Where to ask.</param>
    /// <param name="z">Where to ask.</param>
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
    /// <param name="x">Any block position in the column wanted, east to west.</param>
    /// <param name="z">Any block position in the column wanted, north to south.</param>
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
    /// How much light of one kind reaches a position, from 0 in the dark to the
    /// world's brightest.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="kind">Which light to count.</param>
    /// <param name="x">Where to ask.</param>
    /// <param name="y">Where to ask.</param>
    /// <param name="z">Where to ask.</param>
    [LuaFunction("lightAt")]
    public int LightAt(ScriptOrigin origin, EnumLightKind kind, int x, int y, int z) =>
        world.Light(x, y, z, kind);

    /// <summary>
    /// What the weather and the ground are like at a position. This is how something
    /// is made to depend on where it happened rather than only on what happened.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">Where to ask.</param>
    /// <param name="y">Where to ask.</param>
    /// <param name="z">Where to ask.</param>
    [LuaFunction("climateAt")]
    public ClimatePayload ClimateAt(ScriptOrigin origin, int x, int y, int z) =>
        world.Climate(x, y, z);

    /// <summary>
    /// Which way the wind blows at a position and how hard, as a direction whose
    /// length is its speed.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">Where to ask.</param>
    /// <param name="y">Where to ask.</param>
    /// <param name="z">Where to ask.</param>
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
    /// <param name="value">What to store. Any value a script can write, a table included.</param>
    [LuaFunction("setData")]
    public void SetData(ScriptOrigin origin, string key, ScriptValue value) =>
        world.Remember(key, value, origin);

    /// <summary>What was remembered about the world under a name, or nil when nothing was.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="key">Name it was stored under.</param>
    [LuaFunction("getData")]
    public ScriptValue GetData(ScriptOrigin origin, string key) => world.Recall(key, origin);
}
