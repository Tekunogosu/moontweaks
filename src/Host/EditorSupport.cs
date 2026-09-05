using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonTweaks.Reference;
using Vintagestory.API.Common;

namespace MoonTweaks.Host;

/// <summary>
/// Turns the MoonTweaks folder into a workspace an editor understands: one LuaCATS
/// library per set of bindings the server exposes, MoonTweaks's own and every
/// plugin's, the configuration that points a language server at them, and a worked
/// example of every recipe kind. A server that has started once needs no further
/// setup.
/// </summary>
public static class EditorSupport
{
    /// <summary>Folder the library is written to, beside the scripts folder.</summary>
    public const string LIBRARY_FOLDER = "library";

    /// <summary>Folder the shipped examples are written to, for copying into scripts.</summary>
    public const string EXAMPLES_FOLDER = "examples";

    private const string EXAMPLE_PREFIX = "MoonTweaks.examples.";

    /// <summary>
    /// What a plugin's library is named, so one left behind by a plugin since removed
    /// is told apart from a file an author put in the folder themselves.
    /// </summary>
    private static readonly string PLUGIN_LIBRARY_PATTERN = $"{Api.PluginContract.ROOT}.*.lua";

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
    /// <param name="Location">Where it goes, relative to the MoonTweaks folder.</param>
    /// <param name="Contents">What it holds.</param>
    /// <param name="Upkeep">What decides whether it is rewritten.</param>
    private sealed record Scaffold(string Location, string Contents, Upkeep Upkeep);

    /// <summary>
    /// Everything this server writes. A library is stamped, so a restart reads one
    /// header line rather than the whole file. Examples are small and carry no
    /// marker of their own, so they are compared outright.
    /// </summary>
    private static IEnumerable<Scaffold> Scaffolds(IEnumerable<Library> libraries)
    {
        foreach (var library in libraries)
        {
            yield return new($"{LIBRARY_FOLDER}/{library.FileName}", library.Contents, Upkeep.Stamped);
        }

        if (Read("MoonTweaks.luarc.json") is { } luarc) yield return new(".luarc.json", luarc, Upkeep.Seeded);
        if (Read("MoonTweaks.vscode.extensions.json") is { } extensions)
        {
            yield return new(".vscode/extensions.json", extensions, Upkeep.Seeded);
        }

        foreach (var resource in Resources(EXAMPLE_PREFIX))
        {
            if (Read(resource) is { } example) yield return new(LocationOf(resource), example, Upkeep.Mirrored);
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
    /// <param name="folder">The MoonTweaks folder.</param>
    /// <param name="libraries">Every library this server describes its bindings with.</param>
    /// <param name="logger">Where what was written is reported.</param>
    public static void Install(string folder, IReadOnlyList<Library> libraries, ILogger logger)
    {
        var written = new List<string>();
        var scaffolds = Scaffolds(libraries).ToList();

        foreach (var file in scaffolds)
        {
            var target = Path.Combine(folder, file.Location.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(target) && IsCurrent(file.Upkeep, target, file.Contents)) continue;

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, file.Contents);
            written.Add(file.Location);
        }

        var shipped = new HashSet<string>(scaffolds.Select(file => file.Location), System.StringComparer.Ordinal);
        var removed = Prune(folder, shipped).Concat(PruneLibraries(folder, shipped)).ToList();

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

    /// <summary>
    /// Deletes a plugin library whose plugin this server no longer holds. Only files
    /// named as a plugin's are candidates: MoonTweaks's own is always shipped, and
    /// anything else in the folder is the author's.
    /// </summary>
    private static IReadOnlyList<string> PruneLibraries(string folder, HashSet<string> shipped)
    {
        var library = Path.Combine(folder, LIBRARY_FOLDER);
        if (!Directory.Exists(library)) return [];

        var removed = new List<string>();

        foreach (var path in Directory.EnumerateFiles(library, PLUGIN_LIBRARY_PATTERN))
        {
            var location = $"{LIBRARY_FOLDER}/{Path.GetFileName(path)}";
            if (shipped.Contains(location)) continue;

            File.Delete(path);
            removed.Add(location);
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
