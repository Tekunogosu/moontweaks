using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MoonTweaks.Api;
using MoonTweaks.Recipes;
using Vintagestory.API.Common;

namespace MoonTweaks.Host;

/// <summary>
/// One set of values an editor offers inside a string, as the generated library
/// declares it. <see cref="Name"/> is what a member's <see cref="LuaSuggestsAttribute"/>
/// points at, so an annotation and the declaration it relies on cannot name
/// different types.
/// </summary>
/// <param name="Name">Type the alias is declared under.</param>
/// <param name="Label">Plural noun for the values, for a line reporting what was written.</param>
/// <param name="Summary">What the set is, written as the comment above the alias.</param>
/// <param name="Values">Every value, in the order they are offered.</param>
public sealed record SuggestionSet(
    string Name, string Label, string Summary, IReadOnlyList<string> Values);

/// <summary>
/// The values a server's registries actually hold, written as the sets an editor
/// suggests inside a string. Generated from the running game rather than shipped, so
/// the suggestions cover whatever mods a server loads.
/// </summary>
public static class AssetCodeLibrary
{
    /// <summary>Name of the generated file inside the library folder.</summary>
    public const string FILE_NAME = "codes.lua";

    /// <summary>
    /// Every set the library declares. Values are read from <paramref name="world"/>,
    /// or left empty when there is none: a checkout still needs the aliases declared
    /// so it type-checks without a game to read them out of.
    /// </summary>
    /// <remarks>
    /// Sole owner of what the library contains. A registry the game keeps and a script
    /// writes as a bare string becomes one more entry here and one more
    /// <see cref="SuggestionSets"/> constant, and needs nothing else.
    /// </remarks>
    public static IReadOnlyList<SuggestionSet> SetsOf(IWorldAccessor? world) =>
    [
        new(SuggestionSets.ASSET_CODE, "codes",
            "An asset code. The values below are what this server's registries held\n"
            + "when this file was written; any other string is still accepted.",
            world is null ? [] : CodesOf(world)),

        new(SuggestionSets.ASSET_TAG, "tags",
            "A tag an item or block carries, naming what it is rather than what it is\n"
            + "called. Any other string is still accepted.",
            world is null ? [] : TagsOf(world)),

        new(SuggestionSets.ASSET_TRAIT, "traits",
            "A character trait, which a recipe may demand of whoever crafts it.\n"
            + "Any other string is still accepted.",
            world is null ? [] : TraitsOf(world)),

        new(SuggestionSets.ENTITY_TAG, "entity tags",
            "A tag a creature carries, naming what it is rather than what it is called.\n"
            + "A separate set from the item and block tags above: the game keeps the two\n"
            + "in registries of their own. Any other string is still accepted.",
            world is null ? [] : EntityTagsOf(world)),
    ];

    /// <summary>
    /// Writes the file unless what is already listed there is this server's. Returns
    /// the sets it wrote, or null when nothing needed writing.
    /// </summary>
    public static IReadOnlyList<SuggestionSet>? Install(string folder, IWorldAccessor world) =>
        Install(folder, SetsOf(world));

    /// <inheritdoc cref="Install(string, IWorldAccessor)"/>
    public static IReadOnlyList<SuggestionSet>? Install(
        string folder, IReadOnlyList<SuggestionSet> sets, bool force = false)
    {
        var contents = Render(sets);
        var target = Path.Combine(folder, EditorSupport.LIBRARY_FOLDER, FILE_NAME);

        if (!force && File.Exists(target)
            && LibraryHeader.MarkerIn(File.ReadLines(target)) is { } marker
            && marker == LibraryHeader.MarkerIn(contents.Split('\n')))
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, contents);
        return sets;
    }

    /// <summary>How many values each set holds, for a line reporting what was written.</summary>
    public static string Describe(IEnumerable<SuggestionSet> sets) =>
        string.Join(", ", sets.Select(set => $"{set.Values.Count} {set.Label}"));

    /// <summary>Every tag any item or block carries, in one sorted list.</summary>
    public static IReadOnlyList<string> TagsOf(IWorldAccessor world)
    {
        var registry = world.Api.CollectibleTagRegistry;
        return world.Items.Cast<CollectibleObject>().Concat(world.Blocks)
            .Where(asset => !asset.IsMissing)
            .SelectMany(asset => registry.SlowEnumerateTagNames(asset.Tags))
            .Distinct()
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Every tag any creature type carries, in one sorted list. Read off the types
    /// rather than off what is alive: a world holds every type it could spawn and only
    /// the handful of creatures actually standing in the loaded chunks.
    /// </summary>
    public static IReadOnlyList<string> EntityTagsOf(IWorldAccessor world)
    {
        var registry = world.Api.EntityTagRegistry;
        return world.EntityTypes
            .SelectMany(type => registry.SlowEnumerateTagNames(type.Tags))
            .Distinct()
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Every code in the item and block registries, in one sorted list.</summary>
    public static IReadOnlyList<string> CodesOf(IWorldAccessor world) =>
        world.Items.Cast<CollectibleObject>().Concat(world.Blocks)
            .Where(asset => !asset.IsMissing && asset.Code is not null)
            .Select(asset => asset.Code.ToString())
            .Distinct()
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

    /// <summary>Every character trait this server's assets define, in one sorted list.</summary>
    public static IReadOnlyList<string> TraitsOf(IWorldAccessor world) =>
        new TraitRegistry(world.Api).Codes
            .OrderBy(trait => trait, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Renders each set as an open alias. They widen <c>string</c> rather than closing
    /// over their values, so an editor offers them without rejecting one that reached
    /// the game after this file was written.
    /// </summary>
    public static string Render(IReadOnlyList<SuggestionSet> sets)
    {
        var body = new StringBuilder();

        foreach (var set in sets)
        {
            if (body.Length > 0) body.AppendLine();
            foreach (var line in set.Summary.Split('\n')) body.AppendLine($"--- {line}");
            body.AppendLine($"---@alias {set.Name} string");
            foreach (var value in set.Values) body.AppendLine($"---| \"{value}\"");
        }

        var output = new StringBuilder();
        output.AppendLine("---@meta");
        output.AppendLine($"--- Values this server offers: {Describe(sets)}.");
        output.AppendLine("--- Generated from the running game; do not edit.");
        output.AppendLine($"{LibraryHeader.BUILD_MARKER}{LibraryHeader.Fingerprint(body.ToString())}");
        output.AppendLine();
        output.Append(body);
        return output.ToString();
    }
}
