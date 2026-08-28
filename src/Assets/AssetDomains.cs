using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Recipes;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Assets;

/// <summary>Properties of the items a server holds.</summary>
[LuaModule("moontweaks.items")]
public sealed class ItemDomain(MutationLog log, IWorldAccessor world)
{
    private readonly AssetStacks stacks = new(world);

    /// <summary>
    /// Changes properties on every item the code matches, which may be a whole family
    /// through a <c>*</c> wildcard. Only the keys the script writes change; the rest
    /// are left as the game loaded them.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="properties">What to change, and on what.</param>
    [LuaFunction("set")]
    public void Set(ScriptOrigin origin, AssetPropertiesSpec properties) =>
        log.Record(AssetSearch.Change(
            origin, "item", properties, stacks,
            code => world.SearchItems(code).Cast<CollectibleObject>(), world.Items));

    /// <summary>Counts the items the registry holds.</summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => world.Items.Count;
}

/// <summary>Properties of the blocks a server holds.</summary>
[LuaModule("moontweaks.blocks")]
public sealed class BlockDomain(MutationLog log, IWorldAccessor world)
{
    private readonly AssetStacks stacks = new(world);

    /// <summary>
    /// Changes properties on every block the code matches, which may be a whole family
    /// through a <c>*</c> wildcard. Only the keys the script writes change; the rest
    /// are left as the game loaded them.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="properties">What to change, and on what.</param>
    [LuaFunction("set")]
    public void Set(ScriptOrigin origin, AssetPropertiesSpec properties) =>
        log.Record(AssetSearch.Change(
            origin, "block", properties, stacks,
            code => world.SearchBlocks(code).Cast<CollectibleObject>(), world.Blocks));

    /// <summary>Counts the blocks the registry holds.</summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => world.Blocks.Count;
}

/// <summary>
/// Turns what a script named into the change it asked for, refusing the three ways
/// of asking for nothing: naming neither a code nor a tag, naming no property to
/// change, and naming something no asset answers to.
/// </summary>
internal static class AssetSearch
{
    /// <inheritdoc cref="AssetSearch"/>
    internal static SetAssetProperties Change(
        ScriptOrigin origin,
        string kind,
        AssetPropertiesSpec spec,
        AssetStacks stacks,
        System.Func<AssetLocation, IEnumerable<CollectibleObject>> byCode,
        IEnumerable<CollectibleObject> everything)
    {
        if (spec.Code is null && spec.Tags is null)
        {
            throw new ScriptError(origin,
                "neither a 'code' nor any 'tags' names what to change, so nothing would be");
        }

        if (!CollectibleProperties.ChangesAnything(spec))
        {
            throw new ScriptError(origin,
                $"{Named(spec)} names no property to change, so this would do nothing");
        }

        // A code searches the registry, which is what narrows the scan; tags alone
        // have nothing to narrow by and so are asked of everything.
        var condition = stacks.Condition(spec.Tags, origin, "tags");
        var candidates = spec.Code is { } code ? byCode(new AssetLocation(code)) : everything;

        var matched = candidates
            .Where(asset => !asset.IsMissing && AssetStacks.Matches(condition, asset))
            .ToList();

        if (matched.Count == 0)
        {
            throw new ScriptError(origin, $"no {kind} matches {Named(spec)}");
        }

        return new SetAssetProperties(origin, kind, spec, matched, stacks);
    }

    /// <summary>What a script said to change, for a message naming it back.</summary>
    internal static string Named(AssetPropertiesSpec spec) => (spec.Code, spec.Tags) switch
    {
        ({ } code, null) => $"'{code}'",
        (null, { } tags) => $"tags {string.Join(", ", tags.Select(tag => $"'{tag}'"))}",
        ({ } code, { } tags) => $"'{code}' with tags {string.Join(", ", tags.Select(tag => $"'{tag}'"))}",
        _ => "nothing",
    };
}
