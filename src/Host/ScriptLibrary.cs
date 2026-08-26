using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace MoonTweaks.Host;

/// <summary>The folder of scripts a server runs, and how they are ordered.</summary>
public static class ScriptLibrary
{
    /// <summary>Folder under ModConfig that scripts are read from.</summary>
    public const string FolderName = "moontweaks";

    /// <summary>Absolute path of the script folder, created if a server has none yet.</summary>
    public static string PathFor(ICoreAPI api)
    {
        var folder = Path.Combine(GamePaths.ModConfig, FolderName);
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Every script in the folder, in filename order so authors can control precedence.</summary>
    public static IReadOnlyList<ScriptFile> Discover(ICoreAPI api) =>
        Directory.EnumerateFiles(PathFor(api), "*.lua")
            .OrderBy(path => Path.GetFileName(path), System.StringComparer.Ordinal)
            .Select(path => new ScriptFile(Path.GetFileName(path), File.ReadAllText(path)))
            .ToList();
}
