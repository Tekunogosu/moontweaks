using System;
using System.Linq;
using MoonTweaks.Scripting;

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
    /// <remarks>
    /// Matched without regard to case, because the two sets are written to different
    /// conventions: this layer names its values as C# names them and the game spells
    /// some of its own in capitals. Nothing is made ambiguous by it — no enumeration
    /// on either side holds two names differing only in case.
    /// </remarks>
    public static TOther As<TOther>(Enum named) where TOther : struct, Enum =>
        Enum.TryParse<TOther>(named.ToString(), ignoreCase: true, out var same)
            ? same
            : throw new InvalidOperationException(
                $"{named.GetType().Name}.{named} has no counterpart in {typeof(TOther).Name}");

    /// <summary>
    /// The value of <paramref name="set"/> a script named, or a failure naming what it
    /// could have written instead. Sole owner of reading a value set out of a script,
    /// so a name written into a spec and a name returned from a handler are read the
    /// same way and refused with the same sentence.
    /// </summary>
    /// <param name="set">Value set the name has to belong to.</param>
    /// <param name="named">What the script wrote.</param>
    /// <param name="origin">Script line that wrote it.</param>
    /// <param name="path">What to call it in a failure, as the script author knows it.</param>
    public static object Named(Type set, string named, ScriptOrigin origin, string path)
    {
        var match = Enum.GetNames(set)
            .FirstOrDefault(candidate => candidate.Equals(named, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var allowed = string.Join(", ", Enum.GetNames(set).Select(name => $"'{name.ToLowerInvariant()}'"));
            throw new ScriptError(origin, $"{path} must be one of {allowed}, got '{named}'");
        }

        return Enum.Parse(set, match);
    }

    /// <summary>The value of <typeparamref name="TSet"/> a script named.</summary>
    public static TSet Named<TSet>(string named, ScriptOrigin origin, string path)
        where TSet : struct, Enum =>
        (TSet)Named(typeof(TSet), named, origin, path);
}
