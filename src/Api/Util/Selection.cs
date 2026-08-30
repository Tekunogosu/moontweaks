using System.Linq;
using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

/// <summary>
/// What a script named to act on, for the shapes that pick something by a code, by
/// the tags it carries, or by both. A utility rather than a system: it reaches
/// nothing and holds nothing.
/// </summary>
/// <remarks>
/// Sole owner of two questions those shapes both ask — whether the script named
/// anything at all, and how to say back what it named. Recipes and assets are chosen
/// the same way and each carried its own copy of both, which is how the two came to
/// disagree about how a report spells a code.
/// </remarks>
public static class Selection
{
    /// <summary>
    /// Refuses a selection naming neither. Either key alone is enough and neither is
    /// optional together: a table with neither in it matches everything there is,
    /// which is not what a script writing one meant by it.
    /// </summary>
    /// <param name="code">Code the script named, if it named one.</param>
    /// <param name="tags">Tags the script named, if it named any.</param>
    /// <param name="doing">What would be done, as the message should read it.</param>
    /// <param name="origin">Script line that wrote the selection.</param>
    public static void MustName(string? code, string[]? tags, string doing, ScriptOrigin origin)
    {
        if (code is null && tags is null)
        {
            throw new ScriptError(origin,
                $"neither a 'code' nor any 'tags' names what to {doing}, so nothing would be");
        }
    }

    /// <summary>What the script named, for a message that says it back.</summary>
    public static string Describe(string? code, string[]? tags = null) => (code, tags) switch
    {
        ({ } named, null) => $"'{named}'",
        (null, { } carried) => $"tags {Listed(carried)}",
        ({ } named, { } carried) => $"'{named}' with tags {Listed(carried)}",
        _ => "nothing",
    };

    /// <summary>Tag names as a message lists them.</summary>
    private static string Listed(string[] tags) =>
        string.Join(", ", tags.Select(tag => $"'{tag}'"));
}
