using System.Collections.Generic;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

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
public sealed class WorldAccess(IWorldAccessor world)
{
    /// <summary>
    /// Writes queued here rather than one at a time. Each single write relights and
    /// re-sends the chunk it touched, so a script filling a shape one block at a time
    /// pays that cost per block; queued writes pay it once at the commit.
    /// </summary>
    private readonly IBulkBlockAccessor bulk = world.GetBlockAccessorBulkUpdate(true, true);

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
        bulk.SetBlock(blockId, new BlockPos(x, y, z));

    /// <summary>Writes everything queued, relighting and sending each chunk once.</summary>
    public int Commit()
    {
        var queued = bulk.StagedBlocks.Count;
        bulk.Commit();
        return queued;
    }

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
}
