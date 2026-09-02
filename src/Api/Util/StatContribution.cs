using Vintagestory.API.Common;

namespace MoonTweaks.Api;

/// <summary>
/// One named change to an ability, on whatever carries it. A utility rather than a
/// system: it reaches nothing and holds nothing.
/// </summary>
/// <remarks>
/// Sole owner of the three operations the game keeps on an entity's abilities.
/// Players and entities reach them by different identifiers but arrive at the same
/// store, and each carried its own copy of all three. Naming it once is what keeps a
/// contribution set on a player and one set on an entity holding the same prefix and
/// blending the same way.
/// </remarks>
public static class StatContribution
{
    /// <summary>What an ability comes to with every contribution added up.</summary>
    /// <param name="stats">Abilities of whatever carries them.</param>
    /// <param name="stat">Which ability, such as <c>walkspeed</c>.</param>
    public static float Blended(EntityStats stats, string stat) => stats.GetBlended(stat);

    /// <summary>Adds or replaces one named contribution to an ability.</summary>
    /// <param name="stats">Abilities of whatever carries them.</param>
    /// <param name="stat">Which ability, such as <c>walkspeed</c>.</param>
    /// <param name="name">Name the script holds this change under.</param>
    /// <param name="value">How much to add.</param>
    /// <param name="persistent">Whether it survives a restart.</param>
    public static void Set(EntityStats stats, string stat, string name, double value, bool persistent) =>
        stats.Set(stat, ModKey.For(name), (float)value, persistent);

    /// <summary>Takes back one named contribution, leaving every other alone.</summary>
    /// <param name="stats">Abilities of whatever carries them.</param>
    /// <param name="stat">Which ability it was set on.</param>
    /// <param name="name">Name it was set under.</param>
    public static void Clear(EntityStats stats, string stat, string name) =>
        stats.Remove(stat, ModKey.For(name));
}
