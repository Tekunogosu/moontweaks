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
    public const string LibraryFolder = "library";

    /// <summary>Folder the shipped examples are written to, for copying into scripts.</summary>
    public const string ExamplesFolder = "examples";

    private const string LibraryResource = "MoonTweaks.library.moontweaks.lua";
    private const string ExamplePrefix = "MoonTweaks.examples.";

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
        yield return new(LibraryResource, $"{LibraryFolder}/moontweaks.lua", Upkeep.Stamped);
        yield return new("MoonTweaks.luarc.json", ".luarc.json", Upkeep.Seeded);
        yield return new("MoonTweaks.vscode.extensions.json", ".vscode/extensions.json", Upkeep.Seeded);

        foreach (var resource in Resources(ExamplePrefix))
        {
            yield return new(resource, $"{ExamplesFolder}/{resource[ExamplePrefix.Length..]}", Upkeep.Mirrored);
        }
    }

    /// <summary>Writes every scaffolded file the folder does not already have current.</summary>
    public static void Install(string folder, ILogger logger)
    {
        if (Read(LibraryResource) is null)
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

        if (written.Count == 0)
        {
            logger.Notification("[moontweaks] editor support in {0} is up to date", folder);
            return;
        }

        logger.Notification("[moontweaks] editor support in {0}: wrote {1}", folder, string.Join(", ", written));
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
