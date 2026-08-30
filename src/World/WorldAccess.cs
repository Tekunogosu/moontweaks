using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Players;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace MoonTweaks.World;

/// <summary>
/// Reaching the blocks and entities of a loaded world. Sole owner of turning the
/// codes a script writes into the identifiers the world stores.
/// </summary>
/// <remarks>
/// Only meaningful once a world exists, which is why nothing here is reachable from
/// a script's load-time body: chunks are not loaded when scripts run, so a change
/// made then would be written into nothing. These are for handlers, which run while
/// people are playing.
/// </remarks>
/// <param name="api">The running server.</param>
/// <param name="players">
/// How an identifier becomes a player. Borrowed rather than repeated: a highlight is
/// drawn for somebody, and there should be one answer to what an unknown identifier
/// means however a script arrives at asking it.
/// </param>
public sealed class WorldAccess(ICoreServerAPI api, PlayerAccess players)
{
    private readonly IWorldAccessor world = api.World;

    /// <summary>
    /// Writes queued here rather than one at a time. Each single write relights and
    /// re-sends the chunk it touched, so a script filling a shape one block at a time
    /// pays that cost per block; queued writes pay it once at the commit.
    /// </summary>
    /// <remarks>
    /// Asked for on first use rather than when this is built. Every run builds one of
    /// these as it binds its modules, which is while the server is still loading and
    /// before there is a world to write into; a dry run builds one too and never
    /// queues a block in it. Most scripts never queue a block at all, and one that
    /// does is running in a handler, by which time the world is certainly there.
    /// </remarks>
    private IBulkBlockAccessor? bulk;

    /// <inheritdoc cref="bulk"/>
    private IBulkBlockAccessor Bulk => bulk ??= api.World.GetBlockAccessorBulkUpdate(true, true);

    /// <summary>
    /// What a code names, kept so the same one is looked up once however often it is
    /// written. A script filling a shape names a handful of blocks tens of thousands
    /// of times, and each lookup otherwise parses the code into a location and
    /// searches the registry for it. Identifiers do not change while a world is
    /// loaded, so what is remembered cannot go stale.
    /// </summary>
    private readonly Dictionary<string, int> ids = [];

    /// <summary>The block identifier a code names, which the world stores rather than the code.</summary>
    public int IdOf(string code, ScriptOrigin origin)
    {
        if (ids.TryGetValue(code, out var known)) return known;

        var id = world.GetBlock(new AssetLocation(code))?.BlockId
            ?? throw new ScriptError(origin, $"'{code}' is not a known block");

        ids[code] = id;
        return id;
    }

    /// <summary>The code of whatever stands at a position, or nothing where there is air.</summary>
    public string? CodeAt(int x, int y, int z) =>
        world.BlockAccessor.GetBlock(new BlockPos(x, y, z))?.Code?.ToString();

    /// <summary>Puts a block somewhere, taking effect at once.</summary>
    public void Set(int blockId, int x, int y, int z) =>
        world.BlockAccessor.SetBlock(blockId, new BlockPos(x, y, z));

    /// <summary>Queues a block, to be written when <see cref="Commit"/> is called.</summary>
    public void Queue(int blockId, int x, int y, int z) =>
        Bulk.SetBlock(blockId, new BlockPos(x, y, z));

    /// <summary>Writes everything queued, relighting and sending each chunk once.</summary>
    public int Commit()
    {
        // Nothing was ever queued, so there is nothing to write and no reason to ask
        // the world for an accessor to write it with.
        if (bulk is null) return 0;

        var queued = bulk.StagedBlocks.Count;
        bulk.Commit();
        return queued;
    }

    /// <summary>
    /// Every block in a box that a code names, and where each stands. The search runs
    /// inside the game: one crossing whatever the box holds, where asking block by
    /// block costs one per block.
    /// </summary>
    /// <remarks>
    /// Chunks that are not loaded hold nothing to look at and are passed over, so a
    /// box reaching past what is loaded answers for the part that is. The caller is
    /// told how many were missed rather than left to wonder why a box came back empty.
    /// </remarks>
    public IReadOnlyList<BlockAtPayload> Find(RegionSpec region, out int unloaded)
    {
        var wanted = region.Code is null ? null : new AssetLocation(region.Code);
        var found = new List<BlockAtPayload>();
        var missed = 0;

        Search(region, wanted, (block, at) =>
        {
            // The accessor reuses one position between calls, so what is wanted from
            // it is read now rather than kept.
            found.Add(new BlockAtPayload(at.X, at.Y, at.Z, block.Code!.ToString()));
            return found.Count < region.Limit;
        }, () => missed++);

        unloaded = missed;
        return found;
    }

    /// <summary>
    /// How many blocks in a box a code names. Counts rather than describes, so nothing
    /// is built for a question answered by a number.
    /// </summary>
    /// <inheritdoc cref="Find" path="/remarks"/>
    public int Count(RegionSpec region, out int unloaded)
    {
        var wanted = region.Code is null ? null : new AssetLocation(region.Code);
        var counted = 0;
        var missed = 0;

        Search(region, wanted, (_, _) => ++counted < region.Limit, () => missed++);

        unloaded = missed;
        return counted;
    }

    /// <summary>
    /// Walks a box, handing over every block a code names. Sole owner of that walk, so
    /// counting and listing read the same box in the same order and agree about which
    /// blocks are in it.
    /// </summary>
    /// <remarks>
    /// The corners are sorted rather than trusted: the game requires the lower one
    /// first and quietly finds nothing when given them the other way round, which a
    /// script writing a box from two remembered positions would hit constantly.
    /// </remarks>
    private void Search(
        RegionSpec region, AssetLocation? wanted, ActionConsumable<Block, BlockPos> onMatch, Action onMissing)
    {
        world.BlockAccessor.SearchBlocks(
            new BlockPos(Math.Min(region.X, region.ToX), Math.Min(region.Y, region.ToY), Math.Min(region.Z, region.ToZ)),
            new BlockPos(Math.Max(region.X, region.ToX), Math.Max(region.Y, region.ToY), Math.Max(region.Z, region.ToZ)),
            (block, at) =>
                // A block the game holds no code for is nothing a script can name, so
                // it matches nothing and is stepped over. The game null-checks this
                // field itself, whatever the declared type says.
                block?.Code is { } code && (wanted is null || WildcardUtil.Match(wanted, code))
                    ? onMatch(block, at)
                    : true,
            (_, _, _) => onMissing());
    }

    /// <summary>
    /// Whether a player may act on a place, and what stops them where they may not.
    /// Answers rather than enforces: reading it changes nothing.
    /// </summary>
    public EnumAccessResponse Access(AccessSpec spec, ScriptOrigin origin) =>
        ValueSet.As<EnumAccessResponse>(api.World.Claims.TestAccess(
            players.Find(spec.Player, origin),
            new BlockPos(spec.X, spec.Y, spec.Z),
            ValueSet.As<EnumBlockAccessFlags>(spec.What)));

    /// <summary>Plays one of the game's own sounds at a place, for everybody near enough.</summary>
    /// <remarks>
    /// Two calls rather than one because the game offers two: naming a pitch asks for
    /// exactly that pitch, and naming none lets the game vary it a little each time,
    /// which is what stops a repeated sound reading as a loop.
    /// </remarks>
    public void Play(SoundSpec sound)
    {
        var played = new AssetLocation(sound.Sound);

        if (sound.Pitch is { } pitch)
        {
            world.PlaySoundAt(
                played, sound.X, sound.Y, sound.Z, null, (float)pitch,
                (float)sound.Range, (float)sound.Volume);
            return;
        }

        world.PlaySoundAt(
            played, sound.X, sound.Y, sound.Z,
            range: (float)sound.Range, volume: (float)sound.Volume);
    }

    /// <summary>Throws off particles at a place, drawn on every screen near enough to see.</summary>
    public void Particles(ParticlesSpec spec, ScriptOrigin origin)
    {
        var from = new Vec3d(spec.X, spec.Y, spec.Z);
        var to = new Vec3d(spec.ToX ?? spec.X, spec.ToY ?? spec.Y, spec.ToZ ?? spec.Z);
        var slowest = Speed(spec.Velocity);
        var fastest = spec.ToVelocity is null ? slowest : Speed(spec.ToVelocity);

        world.SpawnParticles(
            (float)spec.Quantity,
            spec.Colour is { } colour ? Packed(colour, origin) : OPAQUE,
            from, to, slowest, fastest,
            (float)spec.Life, (float)spec.Gravity, (float)spec.Size,
            ValueSet.As<EnumParticleModel>(spec.Model));
    }

    /// <summary>An opaque white, for particles a script gave no colour.</summary>
    private const int OPAQUE = unchecked((int)0xFFFFFFFF);

    /// <summary>A velocity as the game holds it, or stillness where a script named none.</summary>
    private static Vec3f Speed(VelocitySpec? velocity) =>
        velocity is null
            ? new Vec3f(0, 0, 0)
            : new Vec3f((float)velocity.X, (float)velocity.Y, (float)velocity.Z);

    /// <summary>
    /// Drops a stack into the world, thrown if a velocity says so and marked as
    /// somebody's if an owner does.
    /// </summary>
    /// <remarks>
    /// An owner is what keeps a stack on the ground for the moment after it lands.
    /// The game refuses to let whoever dropped something collect it for a second
    /// afterwards, and refuses nobody when it came from nobody, so a stack dropped at
    /// a player's feet with no owner is back in their hands before they see it fall.
    /// </remarks>
    public void Drop(ItemStack stack, Vec3d at, Vec3d? velocity, string? owner)
    {
        var dropped = world.SpawnItemEntity(stack, at, velocity);

        if (owner is not null && dropped is EntityItem thing) thing.ByPlayerUid = owner;
    }

    /// <summary>
    /// Breaks a block the way breaking it does: its drops land, its sound plays, and
    /// whatever depended on it is told. Distinct from writing air over it, which does
    /// none of those things.
    /// </summary>
    public void Break(int x, int y, int z, string? player, double dropMultiplier, ScriptOrigin origin) =>
        world.BlockAccessor.BreakBlock(
            new BlockPos(x, y, z),
            player is null ? null : players.Find(player, origin),
            (float)dropMultiplier);

    /// <summary>
    /// Swaps one block for another without disturbing what stands in it. A chest
    /// exchanged for another chest keeps what was inside; the same chest set outright
    /// is a new empty one.
    /// </summary>
    public void Exchange(int blockId, int x, int y, int z) =>
        world.BlockAccessor.ExchangeBlock(blockId, new BlockPos(x, y, z));

    /// <summary>
    /// Asks for the chunk column holding a position to be brought in, and says whether
    /// it was already there.
    /// </summary>
    /// <remarks>
    /// Taken in block coordinates like everything else here, and turned into the chunk
    /// coordinates the game wants. Asking is all this does: the column arrives over the
    /// following ticks, so a script that wants to write there checks
    /// <see cref="IsLoaded"/> from a later tick rather than on the next line.
    ///
    /// The column is not held open. It unloads again on the game's own terms once
    /// nothing is keeping it, which is what stops a script quietly pinning the world
    /// into memory a chunk at a time.
    /// </remarks>
    public bool Load(int x, int z)
    {
        if (IsLoaded(x, 0, z)) return true;

        api.WorldManager.LoadChunkColumn(
            x / GlobalConstants.ChunkSize, z / GlobalConstants.ChunkSize);
        return false;
    }

    /// <summary>Whether the chunk holding a position is loaded, and so whether writing there does anything.</summary>
    public bool IsLoaded(int x, int y, int z) =>
        world.BlockAccessor.GetChunkAtBlockPos(new BlockPos(x, y, z)) is not null;

    /// <summary>How much light of one kind reaches a position.</summary>
    public int Light(int x, int y, int z, EnumLightKind kind) =>
        world.BlockAccessor.GetLightLevel(new BlockPos(x, y, z), ValueSet.As<EnumLightLevelType>(kind));

    /// <summary>
    /// The height of the ground in one column, or nothing where that column is not
    /// loaded. Read from the map rather than by looking down block by block, which is
    /// what makes it worth having: the same answer for one call instead of hundreds.
    /// </summary>
    public int? Surface(int x, int z) => api.WorldManager.GetSurfacePosY(x, z);

    /// <summary>What the weather and the ground are like at a position, as it stands now.</summary>
    public ClimatePayload Climate(int x, int y, int z)
    {
        var at = world.BlockAccessor.GetClimateAt(new BlockPos(x, y, z));

        return new ClimatePayload
        {
            Temperature = at.Temperature,
            Rainfall = at.Rainfall,
            WorldgenTemperature = at.WorldGenTemperature,
            WorldgenRainfall = at.WorldgenRainfall,
            Fertility = at.Fertility,
            ForestDensity = at.ForestDensity,
            ShrubDensity = at.ShrubDensity,
            GeologicActivity = at.GeologicActivity,
        };
    }

    /// <summary>Which way the wind is blowing at a position, and how hard.</summary>
    public VectorPayload Wind(int x, int y, int z)
    {
        var wind = world.BlockAccessor.GetWindSpeedAt(new BlockPos(x, y, z));
        return new VectorPayload(wind.X, wind.Y, wind.Z);
    }

    /// <summary>Draws a set of blocks on one player's screen, replacing whatever that slot held.</summary>
    public void Highlight(HighlightSpec spec, ScriptOrigin origin)
    {
        var blocks = spec.Blocks.Select(at => new BlockPos(at.X, at.Y, at.Z)).ToList();
        var player = players.Find(spec.Player, origin);

        if (spec.Colour is not { } colour)
        {
            world.HighlightBlocks(player, spec.Slot, blocks);
            return;
        }

        var packed = Enumerable.Repeat(Packed(colour, origin), blocks.Count).ToList();
        world.HighlightBlocks(player, spec.Slot, blocks, packed);
    }

    /// <summary>
    /// A colour as the single number the game draws with, in the order it packs one:
    /// alpha highest and red lowest.
    /// </summary>
    private static int Packed(ColourSpec colour, ScriptOrigin origin) =>
        ColourChannel.Of(colour.Alpha, origin, "colour.alpha") << 24
        | ColourChannel.Of(colour.Blue, origin, "colour.blue") << 16
        | ColourChannel.Of(colour.Green, origin, "colour.green") << 8
        | ColourChannel.Of(colour.Red, origin, "colour.red");

    /// <summary>
    /// What the world itself remembers, saved with the save game rather than with any
    /// player. The counterpart of what a script remembers about a player, and the only
    /// home for anything counted or tracked across everybody.
    /// </summary>
    public void Remember(string key, ScriptValue value, ScriptOrigin origin) =>
        Save(origin).StoreData(ModKey.For(key), ScriptJson.Write(value));

    /// <summary>What the world remembered under a name, or nil where nothing was.</summary>
    public ScriptValue Recall(string key, ScriptOrigin origin) =>
        ScriptJson.Parse(Save(origin).GetData<string?>(ModKey.For(key)));

    /// <summary>
    /// The save game, which only exists once there is a world. Named in the failure
    /// rather than thrown through, because a script reaching this from its body rather
    /// than from a handler has made the one mistake this whole domain warns about.
    /// </summary>
    private ISaveGame Save(ScriptOrigin origin) =>
        api.WorldManager?.SaveGame
        ?? throw new ScriptError(origin,
            "there is no world yet, so nothing can be remembered against it; "
            + "this belongs in an event handler rather than in a script's body");
}
