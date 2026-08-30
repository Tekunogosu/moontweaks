using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

/// <summary>
/// One part of a colour, checked against the single byte the game packs it into.
/// A utility rather than a system: it reaches nothing and holds nothing.
/// </summary>
/// <remarks>
/// Sole owner of that range. Both places a script writes a colour — the light a block
/// gives off, and the outline drawn on somebody's screen — pack their parts into
/// bytes, and a part written past 255 would silently wrap round into a colour nobody
/// asked for. Naming it is the whole point, so the two cannot come to disagree about
/// what is out of range or how to say so.
/// </remarks>
public static class ColourChannel
{
    /// <summary>The brightest a channel goes, which is what one byte holds.</summary>
    public const int MOST = 255;

    /// <summary>One channel as the byte it is, or a failure naming the part that is out of range.</summary>
    public static byte Of(int value, ScriptOrigin origin, string path) =>
        value is >= 0 and <= MOST
            ? (byte)value
            : throw new ScriptError(origin, $"{path} must be between 0 and {MOST}, got {value}");
}
