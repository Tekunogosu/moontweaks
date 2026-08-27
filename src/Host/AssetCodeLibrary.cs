using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;

namespace MoonTweaks.Host;

/// <summary>
/// The asset codes and tags a server's registries actually hold, written as the
/// values an editor suggests inside a string. Generated from the running game
/// rather than shipped, so the suggestions cover whatever mods a server loads.
/// </summary>
public static class AssetCodeLibrary
{
    /// <summary>Name of the generated file inside the library folder.</summary>
    public const string FileName = "codes.lua";

    /// <summary>Type name the generated reference points a code field at.</summary>
    public const string AliasName = "AssetCode";

    /// <summary>Type name the generated reference points a tags field at.</summary>
    public const string TagAliasName = "AssetTag";

    /// <summary>
    /// Writes the file unless the codes already listed there are this server's.
    /// Returns how many codes it lists, or null when nothing needed writing.
    /// </summary>
    public static int? Install(string folder, IWorldAccessor world, bool force = false) =>
        Install(folder, CodesOf(world), TagsOf(world), force);

    /// <inheritdoc cref="Install(string, IWorldAccessor, bool)"/>
    public static int? Install(
        string folder, IReadOnlyList<string> codes, IReadOnlyList<string> tags, bool force = false)
    {
        var contents = Render(codes, tags);
        var target = Path.Combine(folder, EditorSupport.LibraryFolder, FileName);

        if (!force && File.Exists(target)
            && LibraryHeader.MarkerIn(File.ReadLines(target)) is { } marker
            && marker == LibraryHeader.MarkerIn(contents.Split('\n')))
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, contents);
        return codes.Count;
    }

    /// <summary>Every tag any item or block carries, in one sorted list.</summary>
    public static IReadOnlyList<string> TagsOf(IWorldAccessor world)
    {
        var registry = world.Api.CollectibleTagRegistry;
        return world.Items.Cast<CollectibleObject>().Concat(world.Blocks)
            .Where(asset => !asset.IsMissing)
            .SelectMany(asset => registry.SlowEnumerateTagNames(asset.Tags))
            .Distinct()
            .OrderBy(tag => tag, System.StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Every code in the item and block registries, in one sorted list.</summary>
    public static IReadOnlyList<string> CodesOf(IWorldAccessor world) =>
        world.Items.Cast<CollectibleObject>().Concat(world.Blocks)
            .Where(asset => !asset.IsMissing && asset.Code is not null)
            .Select(asset => asset.Code.ToString())
            .Distinct()
            .OrderBy(code => code, System.StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Renders the codes as an open alias. It widens <c>string</c> rather than
    /// closing over these values, so an editor offers them without rejecting a code
    /// that reached the game after this file was written.
    /// </summary>
    public static string Render(IReadOnlyList<string> codes, IReadOnlyList<string> tags)
    {
        var body = new StringBuilder();
        body.AppendLine("--- An asset code. The values below are what this server's registries held");
        body.AppendLine("--- when this file was written; any other string is still accepted.");
        body.AppendLine($"---@alias {AliasName} string");
        foreach (var code in codes) body.AppendLine($"---| \"{code}\"");
        body.AppendLine();
        body.AppendLine("--- A tag an item or block carries, naming what it is rather than what it is");
        body.AppendLine("--- called. Any other string is still accepted.");
        body.AppendLine($"---@alias {TagAliasName} string");
        foreach (var tag in tags) body.AppendLine($"---| \"{tag}\"");

        var output = new StringBuilder();
        output.AppendLine("---@meta");
        output.AppendLine($"--- Asset codes and tags this server offers: {codes.Count} codes, {tags.Count} tags.");
        output.AppendLine("--- Generated from the running game; do not edit.");
        output.AppendLine($"{LibraryHeader.BuildMarker}{LibraryHeader.Fingerprint(body.ToString())}");
        output.AppendLine();
        output.Append(body);
        return output.ToString();
    }
}
