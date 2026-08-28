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

    /// <summary>The block identifier a code names, which the world stores rather than the code.</summary>
    public int IdOf(string code, ScriptOrigin origin) =>
        world.GetBlock(new AssetLocation(code))?.BlockId
        ?? throw new ScriptError(origin, $"'{code}' is not a known block");

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

    /// <summary>Drops an item into the world, as a block would when broken.</summary>
    public void Drop(ItemStack stack, double x, double y, double z) =>
        world.SpawnItemEntity(stack, new Vec3d(x, y, z));

    /// <summary>A stack of something, by the code that names it.</summary>
    public ItemStack Stack(string code, int quantity, ScriptOrigin origin)
    {
        var location = new AssetLocation(code);

        if (world.GetItem(location) is { } item) return new ItemStack(item, quantity);
        if (world.GetBlock(location) is { } block) return new ItemStack(block, quantity);

        throw new ScriptError(origin, $"'{code}' is neither a known item nor a known block");
    }
}
