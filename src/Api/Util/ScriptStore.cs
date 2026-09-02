using System;
using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

/// <summary>
/// What a script remembers, held in one of the game's stores. A utility rather than a
/// system: it reaches nothing and holds nothing.
/// </summary>
/// <remarks>
/// Sole owner of the three steps every store shares — the name goes under this mod's
/// prefix, the value is written as JSON, and what comes back is read as a script
/// value again. Four stores answer for different lifetimes: a player's world data, a
/// player's account, an entity, and the save game. They differ only in which pair of
/// string calls holds the JSON, which each caller supplies.
///
/// Keeping the round trip here is what stops the four drifting apart. A value written
/// against a player and one written against the world are the same value, and a
/// script that stored a table reads a table back whichever store it chose.
/// </remarks>
public static class ScriptStore
{
    /// <summary>Writes a value into a store under this mod's prefix.</summary>
    /// <param name="key">Name the script stored it under.</param>
    /// <param name="value">What the script stored.</param>
    /// <param name="write">How this store holds a string against a name.</param>
    public static void Write(string key, ScriptValue value, Action<string, string> write) =>
        write(ModKey.For(key), ScriptJson.Write(value));

    /// <summary>What a store holds under a name, or nil where it holds nothing.</summary>
    /// <param name="key">Name the script stored it under.</param>
    /// <param name="read">How this store answers for a name.</param>
    public static ScriptValue Read(string key, Func<string, string?> read) =>
        ScriptJson.Parse(read(ModKey.For(key)));
}
