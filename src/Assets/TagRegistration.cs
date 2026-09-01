using System.Linq;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MoonTweaks.Assets;

/// <summary>
/// Declaring tag names, and putting them on assets. Sole owner of both, so a name a
/// script declares and a name it applies are checked against the same registry.
/// </summary>
/// <remarks>
/// The registry is open only while assets load and is locked immediately after the
/// phase this mod runs scripts in — <c>ServerMain</c> locks it between the mod phase
/// and finalising assets. So a script's body may declare tags and a handler never
/// can, and the failure a handler gets says so rather than reporting the error the
/// game hands back.
///
/// Nothing has to reach the client for this to work. The server sends its whole tag
/// table in the assets packet, names ordered by the handle each was given, and the
/// client registers them in that order — so a name declared here is a name every
/// player's game knows, with the same handle, without anybody installing anything.
/// </remarks>
public static class TagRegistration
{
    /// <summary>
    /// Declares tag names, so that assets may carry them and conditions may ask for
    /// them. A name already known is left as it is rather than being an error: two
    /// scripts declaring the same tag both meant it to exist.
    /// </summary>
    /// <param name="registry">Registry the names are declared in.</param>
    /// <param name="names">Names to declare.</param>
    /// <param name="origin">Script line declaring them.</param>
    public static void Declare(
        ITagRegistry<TagSet> registry, string[] names, ScriptOrigin origin)
    {
        if (names.Length == 0) throw new ScriptError(origin, "names no tags to declare");

        foreach (var name in names.Where(string.IsNullOrWhiteSpace))
        {
            throw new ScriptError(origin, "names an empty tag, which nothing could carry");
        }

        switch (registry.TryRegister(names))
        {
            case TagRegistryError.None:
                return;

            // The window is one startup phase wide, so this is what a handler gets
            // rather than a script's body. Said as the timing problem it is.
            case TagRegistryError.RegistryLocked:
                throw new ScriptError(origin,
                    "declares a tag after the server closed its tag registry. Tags may only be "
                    + "declared while scripts load, which means from a script's body rather than "
                    + "from inside a handler or a timer");

            case TagRegistryError.RegistryAtCapacity:
                throw new ScriptError(origin,
                    "declares a tag the server has no room for: its tag registry is full");

            default:
                throw new ScriptError(origin, "declares a tag the server refused");
        }
    }

    /// <summary>
    /// The tags an asset should carry after a script's change: what it already carries
    /// with more added, or exactly what the script named.
    /// </summary>
    /// <remarks>
    /// Adding reads the current names back out of the set, because a set holds handles
    /// rather than names and there is no union of two sets to reach for. That read is
    /// the registry's own <c>SlowEnumerateTagNames</c>, which is as slow as it says and
    /// is why this happens once per asset while the server loads rather than ever again.
    /// </remarks>
    /// <param name="registry">Registry the names are looked up in.</param>
    /// <param name="carried">What the asset carries now.</param>
    /// <param name="added">Names to add to those, or null to add none.</param>
    /// <param name="replaced">Names to carry instead of those, or null to replace none.</param>
    /// <param name="origin">Script line making the change.</param>
    public static TagSet Wanted(
        ITagRegistry<TagSet> registry,
        TagSet carried,
        string[]? added,
        string[]? replaced,
        ScriptOrigin origin)
    {
        var names = replaced ?? [.. registry.SlowEnumerateTagNames(carried).Concat(added ?? [])];

        if (registry.TryCreateTagSet(out var wanted, names) == TagRegistryError.SomeTagsNotFound)
        {
            var unknown = names.Where(name => !Known(registry, name)).ToArray();
            throw new ScriptError(origin,
                $"names {Listed(unknown)}, which this server has no tag called. "
                + "Declare it with moontweaks.tags.add before putting it on anything");
        }

        return wanted;
    }

    /// <summary>Whether the registry holds a name, asked one at a time to report the ones it does not.</summary>
    private static bool Known(ITagRegistry<TagSet> registry, string name) =>
        registry.TryCreateTagSet(out _, name) == TagRegistryError.None;

    /// <summary>Names as a failure should read them.</summary>
    private static string Listed(string[] names) =>
        string.Join(", ", names.Select(name => $"'{name}'"));
}
