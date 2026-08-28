using MoonTweaks.Api;
using MoonTweaks.Scripting;

namespace MoonTweaks.World;

/// <summary>
/// The blocks and items of a world people are playing in.
/// </summary>
/// <remarks>
/// These act on a loaded world, so they belong in an event handler rather than in a
/// script's body: when scripts run, the recipes exist but the world does not.
/// </remarks>
[LuaModule("moontweaks.world")]
public sealed class WorldDomain(WorldAccess world)
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
        [LuaSuggests(SuggestionSets.AssetCode)] string code, int x, int y, int z) =>
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
        [LuaSuggests(SuggestionSets.AssetCode)] string code, int x, int y, int z) =>
        world.Queue(world.IdOf(code, origin), x, y, z);

    /// <summary>Writes everything queued, and says how many blocks that was.</summary>
    /// <param name="origin">Script line committing.</param>
    [LuaFunction("commit")]
    public int Commit(ScriptOrigin origin) => world.Commit();

    /// <summary>Drops a stack into the world, as a broken block would.</summary>
    /// <param name="origin">Script line dropping it.</param>
    /// <param name="code">What to drop.</param>
    /// <param name="quantity">How many.</param>
    /// <param name="x">Where to drop it.</param>
    /// <param name="y">Where to drop it.</param>
    /// <param name="z">Where to drop it.</param>
    [LuaFunction("dropItem")]
    public void DropItem(
        ScriptOrigin origin,
        [LuaSuggests(SuggestionSets.AssetCode)] string code,
        int quantity, double x, double y, double z) =>
        world.Drop(world.Stack(code, quantity, origin), x, y, z);
}
