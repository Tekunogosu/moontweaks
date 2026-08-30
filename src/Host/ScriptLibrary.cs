using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonTweaks.Scripting;
using Vintagestory.API.Config;

namespace MoonTweaks.Host;

/// <summary>The folder of scripts a server runs, and how they are ordered.</summary>
public static class ScriptLibrary
{
    /// <summary>Folder under ModConfig holding everything MoonTweaks owns.</summary>
    public const string FOLDER_NAME = "moontweaks";

    /// <summary>Folder scripts are read from, beneath <see cref="FOLDER_NAME"/>.</summary>
    public const string SCRIPTS_FOLDER = "scripts";

    /// <summary>The MoonTweaks folder for this install, created if there is none yet.</summary>
    public static string PathFor()
    {
        var folder = Path.Combine(GamePaths.ModConfig, FOLDER_NAME);
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>The scripts folder for this install, created if there is none yet.</summary>
    public static string ScriptsPathFor()
    {
        var folder = Path.Combine(PathFor(), SCRIPTS_FOLDER);
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    /// Every script beneath <paramref name="scriptsFolder"/>, at any depth, ordered
    /// by the path each is named after. A subfolder therefore groups related scripts
    /// into a package that one numeric prefix orders as a whole, while a prefix
    /// inside it orders that package's own members.
    /// </summary>
    public static IReadOnlyList<ScriptFile> Discover(string scriptsFolder) =>
        Directory.EnumerateFiles(scriptsFolder, "*.lua", SearchOption.AllDirectories)
            .Select(path => new ScriptFile(NameOf(scriptsFolder, path), File.ReadAllText(path)))
            .OrderBy(script => script.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Scripts sitting in <paramref name="folder"/> itself rather than under
    /// <see cref="SCRIPTS_FOLDER"/>. These do not run, so a server is told about them
    /// rather than left to wonder why nothing happened.
    /// </summary>
    public static IReadOnlyList<string> Misplaced(string folder) =>
        Directory.EnumerateFiles(folder, "*.lua", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Path a script is known by: relative to the scripts folder and always written
    /// with forward slashes, so both failure messages and run order read the same on
    /// every platform.
    /// </summary>
    private static string NameOf(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
}
