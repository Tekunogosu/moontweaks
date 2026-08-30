
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace MoonTweaks.Recipes;

/// <summary>
/// Which cooking recipes a script asked for. A meal is named by what went into it
/// rather than by one product, so these are chosen by the code the recipe carries
/// where every other kind is chosen by what it makes.
/// </summary>
public sealed class CookingSelector
{
    private readonly AssetLocation pattern;

    /// <summary>Reads a selector, taking its code as the pattern to match.</summary>
    /// <remarks>
    /// A recipe code is a plain name rather than an asset code, so it is wrapped in a
    /// location purely to reuse the game's wildcard matching, and matched against the
    /// recipe's code wrapped the same way.
    /// </remarks>
    public CookingSelector(CookingSelectorSpec spec, ScriptOrigin origin)
    {
        if (string.IsNullOrWhiteSpace(spec.Code))
        {
            throw new ScriptError(origin, "code names nothing to remove, so nothing would be");
        }

        pattern = new AssetLocation(spec.Code);
        Described = Selection.Describe(spec.Code);
    }

    /// <summary>What the script named, for a report that says it back.</summary>
    public string Described { get; }

    /// <summary>Whether one recipe is among them.</summary>
    public bool Matches(string? code) =>
        code is not null && WildcardUtil.Match(pattern, new AssetLocation(code));
}

/// <summary>Removes every cooking recipe whose code matches a pattern.</summary>
public sealed class RemoveCookingRecipes(
    ScriptOrigin origin, CookingSelector selector, RecipeRegistry registry) : IMutation
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Describe() => $"remove cooking recipes coded {selector.Described}";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api) =>
        registry.Cooking.RemoveAll(recipe => selector.Matches(recipe.Code));
}
