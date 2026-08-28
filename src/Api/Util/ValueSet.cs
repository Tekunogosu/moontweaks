using System;

namespace MoonTweaks.Api;

/// <summary>
/// Matches a value set this mod declares to the game's own by name. A utility rather
/// than a system: it reaches nothing and holds nothing.
/// </summary>
/// <remarks>
/// The scripting layer declares its own copies of the game's enumerations so that
/// nothing above it names a game type. That leaves two sets per concept, and matching
/// them by name rather than by position is what stops them drifting apart silently
/// when either gains a value.
/// </remarks>
public static class ValueSet
{
    /// <summary>The counterpart of a value in another set, matched by name.</summary>
    public static TOther As<TOther>(Enum named) where TOther : struct, Enum =>
        Enum.TryParse<TOther>(named.ToString(), out var same)
            ? same
            : throw new InvalidOperationException(
                $"{named.GetType().Name}.{named} has no counterpart in {typeof(TOther).Name}");
}
