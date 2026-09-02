using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace MoonTweaks.Recipes;

/// <summary>
/// Every recipe kind a server holds, reached by the code it was registered under
/// rather than by its type. Sole owner of that lookup.
/// </summary>
/// <remarks>
/// <see cref="RecipeRegistry"/> reaches the six kinds this mod knows the types of,
/// and knows what each of them means: which field carries its output code, whether it
/// is numbered, how a new one is registered. This reaches any kind at all, including
/// one belonging to a mod nobody here has heard of, and knows correspondingly less —
/// it can count them and it can take them away, and nothing more.
///
/// The two are views of the same lists rather than two stores. The survival mod
/// registers its knapping recipes under <c>knappingrecipes</c> and keeps the same
/// list this reaches, so a recipe removed through either is gone from both.
/// </remarks>
public sealed class RecipeKinds(IWorldAccessor world)
{
    /// <summary>Name of the field a registry keeps its recipes in.</summary>
    /// <remarks>
    /// <c>RecipeRegistryGeneric</c> declares it, and a mod registering a kind of its
    /// own almost always uses that type. One that does not is refused by name rather
    /// than answered with nothing.
    /// </remarks>
    private const string RECIPES_FIELD = "Recipes";

    /// <summary>
    /// Every kind this server holds, by the code it is registered under. Sorted, so a
    /// script reading the list and a message listing it agree.
    /// </summary>
    /// <remarks>
    /// The game keeps these in a dictionary it does not offer through
    /// <see cref="IWorldAccessor"/>, so this reads the field that dictionary lives in.
    /// Nothing here depends on that reaching anything: a build where it does not
    /// answers with no kinds, which costs a listing and an error message its detail
    /// and costs the rest of this class nothing.
    /// </remarks>
    public IReadOnlyList<string> Names() =>
        world is GameMain game
            ? [.. game.recipeRegistries.Keys.OrderBy(code => code, System.StringComparer.Ordinal)]
            : [];

    /// <summary>
    /// The recipes one kind holds, as the list the game keeps them in. Written
    /// through rather than copied: taking something out of it takes it out of the game.
    /// </summary>
    public IList Of(string kind, ScriptOrigin origin)
    {
        if (world.GetRecipeRegistry(kind) is not { } registry)
        {
            throw new ScriptError(origin,
                $"no recipe kind on this server is registered as '{kind}'{Suggest()}");
        }

        if (registry.GetType().GetField(RECIPES_FIELD) is not { } held)
        {
            throw new ScriptError(origin,
                $"'{kind}' keeps its recipes somewhere this cannot reach, "
                + $"so they cannot be counted or removed");
        }

        return held.GetValue(registry) as IList
            ?? throw new ScriptError(origin, $"'{kind}' holds no list of recipes");
    }

    /// <summary>
    /// Removes every recipe of one kind whose output the selector names, and says how
    /// many went.
    /// </summary>
    /// <remarks>
    /// Walked from the end so that taking one out does not move the ones not yet
    /// looked at. An entry that is not a recipe the game describes the same way as
    /// every other is left alone: nothing can be said about what it produces, so
    /// nothing can be matched against it.
    /// </remarks>
    public int Remove(string kind, RecipeSelector selector, ScriptOrigin origin)
    {
        var recipes = Of(kind, origin);
        var removed = 0;

        for (var index = recipes.Count - 1; index >= 0; index--)
        {
            if (recipes[index] is not IRecipeBase recipe || !selector.Matches(ProductOf(recipe))) continue;

            recipes.RemoveAt(index);
            removed++;
        }

        return removed;
    }

    /// <summary>
    /// What one recipe of an unknown kind produces. Only the resolved stack is
    /// reachable — the code a recipe declared sits on a field each kind names for
    /// itself — so the code matched against is the one that stack turned out to hold.
    /// </summary>
    private static RecipeProduct ProductOf(IRecipeBase recipe) =>
        recipe.RecipeOutput?.ResolvedItemStack is { } made
            ? new RecipeProduct(made.Collectible?.Code, made)
            : new RecipeProduct(null, null);

    /// <summary>The kinds this server does have, for a message naming one it has not.</summary>
    private string Suggest() =>
        Names() is { Count: > 0 } known ? $"; this server has {string.Join(", ", known)}" : "";
}

/// <summary>Removes every recipe of one kind whose output a selector names.</summary>
public sealed class RemoveKindRecipes(
    ScriptOrigin origin, string kind, RecipeSelector selector, RecipeKinds kinds) : IMutation
{
    /// <inheritdoc/>
    public ScriptOrigin Origin { get; } = origin;

    /// <inheritdoc/>
    public string Describe() => $"remove '{kind}' recipes producing {selector.Described}";

    /// <inheritdoc/>
    public int Apply(ICoreServerAPI api) => kinds.Remove(kind, selector, Origin);
}
