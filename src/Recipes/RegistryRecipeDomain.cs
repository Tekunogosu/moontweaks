using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>
/// What every recipe kind the registry holds does with what a script asked for:
/// record the addition, record the removal, and name the kind while doing it.
/// </summary>
/// <remarks>
/// Sole owner of that plumbing, so a kind bound later cannot reach the registry a
/// slightly different way. Nothing here is public, so none of it is bound: a domain's
/// scriptable surface is exactly the methods it annotates itself.
///
/// Grid recipes are the one kind with no base here. They hang off the world rather
/// than off the registry, so they are added, removed and counted through different
/// machinery entirely, and a base holding a registry they never use would be a
/// dependency invented to share code.
/// </remarks>
public abstract class RegistryRecipeDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
{
    /// <summary>Where a change is recorded, which is never where it is performed.</summary>
    protected MutationLog Log { get; } = log;

    /// <summary>The lists the game keeps this kind's recipes in.</summary>
    protected RecipeRegistry Registry { get; } = registry;

    /// <summary>The assets this kind's recipes name, which every one of them reads.</summary>
    protected AssetStacks Stacks { get; } = new(world);

    /// <summary>What this kind is called in a change report.</summary>
    protected abstract string Kind { get; }

    /// <summary>
    /// Records every recipe one declaration expanded into, unless the declaration is
    /// disabled, in which case the change is kept for the report and never applied.
    /// </summary>
    /// <param name="spec">What the script declared, which says whether it is enabled.</param>
    /// <param name="named">What the report calls the recipe, which is what it makes.</param>
    /// <param name="built">Every recipe that declaration turned into.</param>
    /// <param name="origin">Script line requesting the change.</param>
    protected void RecordAddition<TRecipe>(
        RecipeSpec spec, string named, IReadOnlyList<TRecipe> built, ScriptOrigin origin)
        where TRecipe : class =>
        Log.Record(spec, new AddRecipes<TRecipe>(origin, Kind, named, built, Registry));

    /// <summary>Records the removal of every recipe of this kind the selector names.</summary>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    /// <param name="origin">Script line requesting the change.</param>
    protected void RecordRemoval<TRecipe>(RecipeSelectorSpec selector, ScriptOrigin origin)
        where TRecipe : class =>
        Log.Record(new RemoveRecipes<TRecipe>(
            origin, Kind, new RecipeSelector(selector, Stacks, origin), Registry));
}
