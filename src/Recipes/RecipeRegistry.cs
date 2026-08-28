using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>
/// The recipe lists Vintage Story keeps outside the world. Grid recipes hang off
/// the world itself, but every other kind lives on a mod system, so this is the
/// sole owner of reaching it and no domain looks it up for itself.
/// </summary>
public sealed class RecipeRegistry
{
    private readonly RecipeRegistrySystem system;

    /// <summary>Resolves the registry once, failing loudly when the survival mod is absent.</summary>
    public RecipeRegistry(ICoreAPI api) =>
        system = api.ModLoader.GetModSystem<RecipeRegistrySystem>()
            ?? throw new InvalidOperationException(
                "the survival mod's recipe registry is not loaded, so its recipes cannot be reached");

    /// <summary>Every knapping recipe currently registered.</summary>
    public List<KnappingRecipe> Knapping => system.KnappingRecipes;

    /// <summary>Every clay forming recipe currently registered.</summary>
    public List<ClayFormingRecipe> ClayForming => system.ClayFormingRecipes;

    /// <summary>Every smithing recipe currently registered.</summary>
    public List<SmithingRecipe> Smithing => system.SmithingRecipes;

    /// <summary>Every barrel recipe currently registered.</summary>
    public List<BarrelRecipe> Barrel => system.BarrelRecipes;

    /// <summary>
    /// The list one kind of recipe lives in. Sole owner of which list that is, so the
    /// domains and the changes they record stay generic over the kind.
    /// </summary>
    public List<TRecipe> ListOf<TRecipe>() where TRecipe : RecipeBase =>
        (List<TRecipe>)(object)(
            typeof(TRecipe) == typeof(KnappingRecipe) ? Knapping
            : typeof(TRecipe) == typeof(ClayFormingRecipe) ? ClayForming
            : typeof(TRecipe) == typeof(SmithingRecipe) ? Smithing
            : typeof(TRecipe) == typeof(BarrelRecipe) ? Barrel
            : throw new InvalidOperationException($"{typeof(TRecipe).Name} is not a registry this reaches"));

    /// <summary>
    /// Adds a recipe of any kind this reaches. Removal has no counterpart here and
    /// edits the list from <see cref="ListOf{TRecipe}"/> directly.
    /// </summary>
    public void Register<TRecipe>(TRecipe recipe) where TRecipe : RecipeBase
    {
        switch (recipe)
        {
            case KnappingRecipe knapping:
                Renumbered(knapping, system.RegisterKnappingRecipe, Knapping);
                break;
            case ClayFormingRecipe clay:
                Renumbered(clay, system.RegisterClayFormingRecipe, ClayForming);
                break;
            case SmithingRecipe smithing:
                Renumbered(smithing, system.RegisterSmithingRecipe, Smithing);
                break;
            // Barrel recipes are identified by the code the game requires them to
            // carry and are never numbered, so there is nothing here to renumber.
            case BarrelRecipe barrel:
                system.RegisterBarrelRecipe(barrel);
                break;
            default:
                throw new InvalidOperationException($"{recipe.GetType().Name} has no registry here");
        }
    }

    /// <summary>
    /// Adds a recipe through the game's own entry point, so whatever bookkeeping it
    /// performs still happens, then gives it an identifier nothing else holds.
    /// </summary>
    /// <remarks>
    /// The game numbers a new recipe by how many the list already holds, which
    /// collides with a surviving recipe whenever one was removed first. Knapping,
    /// clay forming and smithing all number that way, and every one of their
    /// surfaces resolves the recipe a player picked by taking the first identifier
    /// that matches — so a duplicate hands them another recipe's output, and the
    /// surface saves the identifier, so it does so again after a restart. Cooking
    /// and barrel recipes are identified by their code instead and never come here.
    ///
    /// Renumbering after the game has added the recipe is correct even though the
    /// list already holds the identifier it just assigned: one past the largest in
    /// use is an identifier no entry can hold, whichever entries were counted.
    /// </remarks>
    private static void Renumbered<TRecipe>(
        TRecipe recipe, Action<TRecipe> register, IReadOnlyList<TRecipe> registered)
        where TRecipe : RecipeBase
    {
        register(recipe);
        recipe.RecipeId = NextIdAfter(registered.Select(entry => entry.RecipeId));
    }

    /// <summary>
    /// Removes every recipe of one kind whose output code matches, and says how many
    /// went. Sole owner of how a kind names what it produces: the output the recipe
    /// base shares exposes a resolved stack and no code, so each kind has to be asked
    /// for itself.
    /// </summary>
    public int Remove<TRecipe>(string outputCode) where TRecipe : RecipeBase
    {
        var pattern = new AssetLocation(outputCode);
        var registered = ListOf<TRecipe>();
        var doomed = registered
            .Where(recipe => OutputCodeOf(recipe) is { } code && WildcardUtil.Match(pattern, code))
            .ToList();

        foreach (var recipe in doomed) registered.Remove(recipe);
        return doomed.Count;
    }

    /// <summary>The code a recipe's output names, whichever kind of recipe it is.</summary>
    private static AssetLocation? OutputCodeOf(RecipeBase recipe) => recipe switch
    {
        LayeredVoxelRecipe voxel => voxel.Output?.Code,
        BarrelRecipe barrel => barrel.Output?.Code,
        _ => null,
    };

    /// <summary>
    /// Identifiers held by more than one recipe, across every kind this registry
    /// numbers. Empty on a healthy registry, and the only way the collision this
    /// class exists to prevent can be seen: a surface resolves a player's choice by
    /// taking the first identifier that matches and says nothing when two do.
    /// </summary>
    public IReadOnlyList<int> DuplicateIds() =>
        [.. Duplicates(Knapping), .. Duplicates(ClayForming), .. Duplicates(Smithing)];

    /// <summary>Identifiers appearing more than once in one kind's list.</summary>
    private static IReadOnlyList<int> Duplicates<TRecipe>(IReadOnlyList<TRecipe> registered)
        where TRecipe : RecipeBase =>
        registered.GroupBy(recipe => recipe.RecipeId)
            .Where(sharing => sharing.Count() > 1)
            .Select(sharing => sharing.Key)
            .OrderBy(id => id)
            .ToList();

    /// <summary>An identifier past every one already taken.</summary>
    public static int NextIdAfter(IEnumerable<int> taken) => taken.DefaultIfEmpty(0).Max() + 1;
}
