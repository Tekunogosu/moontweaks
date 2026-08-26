using System.Text.RegularExpressions;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>
/// Decides whether an asset code names an item or a block. Sole owner of that
/// question, so no recipe domain has to guess at it independently.
/// </summary>
public sealed partial class AssetKindResolver(IWorldAccessor world)
{
    /// <summary>Resolves the registry a code belongs to, using the declared kind when given.</summary>
    public EnumItemClass Resolve(string code, ResourceKind? declared, ScriptOrigin origin, string path)
    {
        if (declared is { } kind) return kind == ResourceKind.Block ? EnumItemClass.Block : EnumItemClass.Item;

        var location = new AssetLocation(code);

        if (IsPattern(code))
        {
            // Output placeholders are not yet filled in, so search on a plain wildcard.
            var wildcard = new AssetLocation(PlaceholderPattern().Replace(code, "*"));
            if (world.SearchItems(wildcard).Length > 0) return EnumItemClass.Item;
            if (world.SearchBlocks(wildcard).Length > 0) return EnumItemClass.Block;
        }
        else
        {
            if (world.GetItem(location) is not null) return EnumItemClass.Item;
            if (world.GetBlock(location) is not null) return EnumItemClass.Block;
        }

        throw new ScriptError(origin,
            $"{path} names '{code}', which is neither a known item nor a known block");
    }

    /// <summary>Whether a code needs searching rather than a direct lookup.</summary>
    private static bool IsPattern(string code) => code.Contains('*') || code.Contains('{');

    [GeneratedRegex(@"\{[^}]*\}")]
    private static partial Regex PlaceholderPattern();
}
