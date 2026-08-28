using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace MoonTweaks.Recipes;

/// <summary>
/// What one recipe makes, in the two forms a selector matches against: the code the
/// recipe names, and the stack that code resolved to.
/// </summary>
/// <param name="Code">Code the recipe's output names, absent on a kind that states none.</param>
/// <param name="Stack">Stack the code resolved to, which carries the product's tags.</param>
public readonly record struct RecipeProduct(AssetLocation? Code, ItemStack? Stack);

/// <summary>
/// Which recipes a script asked for. Sole owner of that question, so every kind
/// answers it the same way whether its recipes live on the world or on a mod system.
/// </summary>
public sealed class RecipeSelector
{
    private readonly AssetLocation? pattern;
    private readonly ComplexTagCondition<TagSet> condition;

    /// <summary>Reads a selector, refusing one that names nothing to match.</summary>
    public RecipeSelector(RecipeSelectorSpec spec, AssetStacks stacks, ScriptOrigin origin)
    {
        if (spec.Code is null && spec.Tags is null)
        {
            throw new ScriptError(origin,
                "neither a 'code' nor any 'tags' names what to remove, so nothing would be");
        }

        pattern = spec.Code is null ? null : new AssetLocation(spec.Code);
        condition = stacks.Condition(spec.Tags, origin, "tags");
        Described = Describe(spec);
    }

    /// <summary>What the script named, for a report that says it back.</summary>
    public string Described { get; }

    /// <summary>
    /// Whether one recipe is among them. Tags are read off the product the recipe
    /// resolved to rather than its code, which is what lets them reach an output no
    /// wildcard would have caught.
    /// </summary>
    public bool Matches(RecipeProduct made)
    {
        if (pattern is not null && (made.Code is null || !WildcardUtil.Match(pattern, made.Code)))
        {
            return false;
        }

        if (condition.IsEmpty) return true;

        return made.Stack?.Collectible is { } product && condition.Matches(product.Tags);
    }

    private static string Describe(RecipeSelectorSpec spec) => (spec.Code, spec.Tags) switch
    {
        ({ } code, null) => code,
        (null, { } tags) => $"tags {string.Join(", ", tags.Select(tag => $"'{tag}'"))}",
        ({ } code, { } tags) => $"{code} with tags {string.Join(", ", tags.Select(tag => $"'{tag}'"))}",
        _ => "nothing",
    };
}
