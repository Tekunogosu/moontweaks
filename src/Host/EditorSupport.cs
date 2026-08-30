using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;

namespace MoonTweaks.Host;

/// <summary>
/// Turns the MoonTweaks folder into a workspace an editor understands: the LuaCATS
/// library describing this build's bindings, the configuration that points a
/// language server at it, and a worked example of every recipe kind. A server that
/// has started once needs no further setup.
/// </summary>
public static class EditorSupport
{
    /// <summary>Folder the library is written to, beside the scripts folder.</summary>
    public const string LIBRARY_FOLDER = "library";

    /// <summary>Folder the shipped examples are written to, for copying into scripts.</summary>
    public const string EXAMPLES_FOLDER = "examples";

    private const string LIBRARY_RESOURCE = "MoonTweaks.library.moontweaks.lua";
    private const string EXAMPLE_PREFIX = "MoonTweaks.examples.";

    /// <summary>How a scaffolded file is kept current.</summary>
    private enum Upkeep
    {
        /// <summary>Rewritten when the build marker in its header is not this build's.</summary>
        Stamped,

        /// <summary>Rewritten when its contents are not this build's.</summary>
        Mirrored,

        /// <summary>Written once, and the author's file from then on.</summary>
        Seeded,
    }

    /// <summary>One file the mod writes into its folder.</summary>
    /// <param name="Resource">Embedded resource holding the contents.</param>
    /// <param name="Location">Where it goes, relative to the MoonTweaks folder.</param>
    /// <param name="Upkeep">What decides whether it is rewritten.</param>
    private sealed record Scaffold(string Resource, string Location, Upkeep Upkeep);

    /// <summary>
    /// Everything this build carries. The library is stamped, so a restart reads one
    /// header line rather than the whole file. Examples are small and carry no
    /// marker of their own, so they are compared outright.
    /// </summary>
    private static IEnumerable<Scaffold> Scaffolds()
    {
        yield return new(LIBRARY_RESOURCE, $"{LIBRARY_FOLDER}/moontweaks.lua", Upkeep.Stamped);
        yield return new("MoonTweaks.luarc.json", ".luarc.json", Upkeep.Seeded);
        yield return new("MoonTweaks.vscode.extensions.json", ".vscode/extensions.json", Upkeep.Seeded);

        foreach (var resource in Resources(EXAMPLE_PREFIX))
        {
            yield return new(resource, LocationOf(resource), Upkeep.Mirrored);
        }
    }

    /// <summary>
    /// Where one embedded example goes, read back out of its resource name. A resource
    /// name has only dots to spell a path with, so the folders an example sits in are
    /// dot segments and the last two are always its own name and its extension.
    /// </summary>
    /// <remarks>
    /// This and the <c>LogicalName</c> in the project file are two halves of one
    /// format. Neither a folder nor an example may carry a dot of its own, since a
    /// name is all either side has to go on.
    /// </remarks>
    private static string LocationOf(string resource)
    {
        var parts = resource[EXAMPLE_PREFIX.Length..].Split('.');
        var file = string.Join('.', parts[^2..]);

        return string.Join('/', parts[..^2].Prepend(EXAMPLES_FOLDER).Append(file));
    }

    /// <summary>Writes every scaffolded file the folder does not already have current.</summary>
    public static void Install(string folder, ILogger logger)
    {
        if (Read(LIBRARY_RESOURCE) is null)
        {
            // A build that skipped the reference generator still runs scripts; it
            // just cannot describe itself to an editor.
            logger.Warning("[moontweaks] this build embeds no scripting library, so {0} has no editor support",
                folder);
            return;
        }

        var written = new List<string>();

        foreach (var file in Scaffolds())
        {
            var target = Path.Combine(folder, file.Location.Replace('/', Path.DirectorySeparatorChar));
            if (Read(file.Resource) is not { } contents) continue;
            if (File.Exists(target) && IsCurrent(file.Upkeep, target, contents)) continue;

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, contents);
            written.Add(file.Location);
        }

        var removed = Prune(folder, [.. Scaffolds().Select(file => file.Location)]);

        if (written.Count == 0 && removed.Count == 0)
        {
            logger.Notification("[moontweaks] editor support in {0} is up to date", folder);
            return;
        }

        if (written.Count > 0)
        {
            logger.Notification("[moontweaks] editor support in {0}: wrote {1}",
                folder, string.Join(", ", written));
        }

        if (removed.Count > 0)
        {
            logger.Notification("[moontweaks] editor support in {0}: removed {1}",
                folder, string.Join(", ", removed));
        }
    }

    /// <summary>
    /// Deletes examples this build no longer ships, and any folder left empty by
    /// doing so.
    /// </summary>
    /// <remarks>
    /// The examples folder is this mod's rather than the author's: every file in it is
    /// rewritten whenever its contents differ from what the build carries, so an edit
    /// made there does not survive a restart anyway. Left alone, a renamed or
    /// regrouped example would leave its old copy behind for good, going on
    /// demonstrating an API that may no longer exist. Nothing outside
    /// <see cref="EXAMPLES_FOLDER"/> is touched, and neither is anything that is not a
    /// script.
    /// </remarks>
    private static IReadOnlyList<string> Prune(string folder, HashSet<string> shipped)
    {
        var examples = Path.Combine(folder, EXAMPLES_FOLDER);
        if (!Directory.Exists(examples)) return [];

        var removed = new List<string>();

        foreach (var path in Directory.EnumerateFiles(examples, "*.lua", SearchOption.AllDirectories))
        {
            var location = Path.GetRelativePath(folder, path).Replace(Path.DirectorySeparatorChar, '/');
            if (shipped.Contains(location)) continue;

            File.Delete(path);
            removed.Add(location);
        }

        // Deepest first, so a folder emptied by clearing the one inside it goes too.
        foreach (var directory in Directory
                     .EnumerateDirectories(examples, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (Directory.EnumerateFileSystemEntries(directory).Any()) continue;
            Directory.Delete(directory);
        }

        removed.Sort(System.StringComparer.Ordinal);
        return removed;
    }

    /// <summary>Whether the file already on disk is the one this build would write.</summary>
    private static bool IsCurrent(Upkeep upkeep, string target, string contents) => upkeep switch
    {
        Upkeep.Seeded => true,
        Upkeep.Mirrored => File.ReadAllText(target) == contents,
        _ => LibraryHeader.MarkerIn(File.ReadLines(target)) is { } marker
             && marker == LibraryHeader.MarkerIn(contents.Split('\n')),
    };

    /// <summary>Embedded resources under one prefix, in name order so writes are predictable.</summary>
    private static IEnumerable<string> Resources(string prefix) =>
        typeof(EditorSupport).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, System.StringComparison.Ordinal))
            .OrderBy(name => name, System.StringComparer.Ordinal);

    /// <summary>Reads one embedded resource, or null when this build carries none.</summary>
    private static string? Read(string name)
    {
        using var stream = typeof(EditorSupport).Assembly.GetManifestResourceStream(name);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
