using System.Collections.Generic;
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
    public static void MustName(string? code, TagConditionSpec? tags, string doing, ScriptOrigin origin)
    {
        if (code is null && tags is null)
        {
            throw new ScriptError(origin,
                $"neither a 'code' nor any 'tags' names what to {doing}, so nothing would be");
        }
    }

    /// <summary>What the script named, for a message that says it back.</summary>
    public static string Describe(string? code, TagConditionSpec? tags = null) => (code, tags) switch
    {
        ({ } named, null) => $"'{named}'",
        (null, { } carried) => $"tags {Asked(carried)}",
        ({ } named, { } carried) => $"'{named}' with tags {Asked(carried)}",
        _ => "nothing",
    };

    /// <summary>
    /// What a condition asks for, as a message says it back. Read off what the script
    /// wrote rather than off what it was built into: a report naming the shape an
    /// author can go and edit is worth more than one naming the groups the game
    /// ended up holding.
    /// </summary>
    private static string Asked(TagConditionSpec tags) => string.Join(" and ", Clauses(tags));

    /// <summary>One phrase per key the condition names.</summary>
    private static IEnumerable<string> Clauses(TagConditionSpec tags)
    {
        if (tags.AllOf is { Names: { } every }) yield return $"all of {Listed(every)}";
        if (tags.AllOf is { Groups: { } all }) yield return $"all of {Grouped(all, allOf: false)}";
        if (tags.AnyOf is { Names: { } some }) yield return $"any of {Listed(some)}";
        if (tags.AnyOf is { Groups: { } any }) yield return $"any of {Grouped(any, allOf: true)}";
        if (tags.NoneOf is { Length: > 0 } forbidden) yield return $"none of {Listed(forbidden)}";
    }

    /// <summary>Groups as a message lists them, each by the key its junction leaves it.</summary>
    private static string Grouped(TagGroupSpec[] groups, bool allOf) => string.Join(", ", groups
        .Select(group => (allOf ? group.AllOf : group.AnyOf) ?? [])
        .Select(names => $"({Listed(names)})"));

    /// <summary>Tag names as a message lists them.</summary>
    private static string Listed(string[] tags) =>
        string.Join(", ", tags.Select(tag => $"'{tag}'"));
}
