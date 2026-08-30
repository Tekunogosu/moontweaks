namespace MoonTweaks.Api;

/// <summary>
/// Names this mod holds things under in stores the game shares with everybody: what
/// a script remembers against a player, an entity or the world, the contributions it
/// makes to an ability, and the speed changes it holds on the clock.
/// </summary>
/// <remarks>
/// A utility rather than a system: it reaches nothing and holds nothing. Sole owner
/// of the prefix, so a script cannot read or overwrite what the game or another mod
/// put in the same store, and a key written by one domain reads back the same from
/// another.
/// </remarks>
public static class ModKey
{
    /// <summary>Prefix every name this mod stores carries.</summary>
    public const string PREFIX = "moontweaks";

    /// <summary>The name a script wrote, under this mod's own prefix.</summary>
    public static string For(string name) => $"{PREFIX}:{name}";
}
