using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

/// <summary>Recipes chipped from a stone laid on a knapping surface.</summary>
[LuaModule("moontweaks.recipes.knapping")]
public sealed class KnappingDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
{
    private const string Kind = "knapping";
    private readonly AssetStacks stacks = new(world);
    private readonly VoxelRecipeFactory factory = new(world);

    /// <summary>
    /// Registers a new knapping recipe. An ingredient whose code contains a wildcard
    /// expands into one recipe per matching variant, with the variant substituted
    /// into any <c>{name}</c> placeholder in the output code.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, KnappingRecipeSpec recipe) =>
        log.Record(recipe, new AddRecipes<KnappingRecipe>(
            origin, Kind, recipe.OutputCode, factory.Build<KnappingRecipe>(recipe, origin), registry));

    /// <summary>
    /// Removes every knapping recipe producing the given output code. The code may
    /// contain a <c>*</c> wildcard, so <c>"game:knifeblade-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        log.Record(new RemoveRecipes<KnappingRecipe>(origin, Kind, new RecipeSelector(selector, stacks, origin), registry));

    /// <summary>
    /// Counts the knapping recipes currently registered. Reads the registry as it
    /// stood before this run's changes, which are applied only once every script
    /// has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => registry.Knapping.Count;
}

/// <summary>Recipes raised layer by layer from a lump of clay.</summary>
[LuaModule("moontweaks.recipes.clayforming")]
public sealed class ClayFormingDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
{
    private const string Kind = "clay forming";
    private readonly AssetStacks stacks = new(world);
    private readonly VoxelRecipeFactory factory = new(world);

    /// <summary>
    /// Registers a new clay forming recipe. An ingredient whose code contains a
    /// wildcard expands into one recipe per matching variant, with the variant
    /// substituted into any <c>{name}</c> placeholder in the output code.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, ClayFormingRecipeSpec recipe) =>
        log.Record(recipe, new AddRecipes<ClayFormingRecipe>(
            origin, Kind, recipe.OutputCode, factory.Build<ClayFormingRecipe>(recipe, origin), registry));

    /// <summary>
    /// Removes every clay forming recipe producing the given output code. The code
    /// may contain a <c>*</c> wildcard, so <c>"game:bowl-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        log.Record(new RemoveRecipes<ClayFormingRecipe>(origin, Kind, new RecipeSelector(selector, stacks, origin), registry));

    /// <summary>
    /// Counts the clay forming recipes currently registered. Reads the registry as
    /// it stood before this run's changes, which are applied only once every script
    /// has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => registry.ClayForming.Count;
}

/// <summary>Recipes hammered from a hot ingot on an anvil.</summary>
[LuaModule("moontweaks.recipes.smithing")]
public sealed class SmithingDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
{
    private const string Kind = "smithing";
    private readonly AssetStacks stacks = new(world);
    private readonly VoxelRecipeFactory factory = new(world);

    /// <summary>
    /// Registers a new smithing recipe. An ingredient whose code contains a wildcard
    /// expands into one recipe per matching variant, with the variant substituted
    /// into any <c>{name}</c> placeholder in the output code and in <c>code</c>.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, SmithingRecipeSpec recipe)
    {
        var built = factory.Build<SmithingRecipe>(recipe, origin);
        foreach (var smithing in built)
        {
            // Left as the game leaves it when a script names none: the anvil groups
            // by this, and the output code is what it would otherwise show.
            smithing.Code = new AssetLocation(recipe.Code ?? smithing.Output.Code.ToString());
        }

        log.Record(recipe, new AddRecipes<SmithingRecipe>(
            origin, Kind, recipe.OutputCode, built, registry));
    }

    /// <summary>
    /// Removes every smithing recipe producing the given output code. The code may
    /// contain a <c>*</c> wildcard, so <c>"game:axehead-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        log.Record(new RemoveRecipes<SmithingRecipe>(origin, Kind, new RecipeSelector(selector, stacks, origin), registry));

    /// <summary>
    /// Counts the smithing recipes currently registered. Reads the registry as it
    /// stood before this run's changes, which are applied only once every script
    /// has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => registry.Smithing.Count;
}

/// <summary>Recipes a barrel mixes on the spot or seals for a while.</summary>
[LuaModule("moontweaks.recipes.barrel")]
public sealed class BarrelDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
{
    private const string Kind = "barrel";
    private readonly AssetStacks stacks = new(world);
    private readonly BarrelRecipeFactory factory = new(world);

    /// <summary>
    /// Registers a new barrel recipe. An ingredient whose code contains a wildcard
    /// expands into one recipe per matching variant, with the variant substituted
    /// into any <c>{name}</c> placeholder in the output code.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, BarrelRecipeSpec recipe) =>
        log.Record(recipe, new AddRecipes<BarrelRecipe>(
            origin, Kind, recipe.OutputCode, factory.Build(recipe, origin), registry));

    /// <summary>
    /// Removes every barrel recipe producing the given output code. The code may
    /// contain a <c>*</c> wildcard, so <c>"game:*-cured"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        log.Record(new RemoveRecipes<BarrelRecipe>(origin, Kind, new RecipeSelector(selector, stacks, origin), registry));

    /// <summary>
    /// Counts the barrel recipes currently registered. Reads the registry as it stood
    /// before this run's changes, which are applied only once every script has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => registry.Barrel.Count;
}
