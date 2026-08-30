using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace MoonTweaks.Recipes;

// One module per way the game holds a recipe. Each is a list of what a script may
// ask for; how any of it reaches the registry belongs to RegistryRecipeDomain.

/// <summary>Recipes chipped from a stone laid on a knapping surface.</summary>
[LuaModule("moontweaks.recipes.knapping")]
public sealed class KnappingDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
    : RegistryRecipeDomain(log, world, registry)
{
    private readonly VoxelRecipeFactory factory = new(world);

    /// <inheritdoc/>
    protected override string Kind => "knapping";

    /// <summary>
    /// Registers a new knapping recipe. An ingredient whose code contains a wildcard
    /// expands into one recipe per matching variant, with the variant substituted
    /// into any <c>{name}</c> placeholder in the output code.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, KnappingRecipeSpec recipe) =>
        RecordAddition(recipe, recipe.OutputCode, factory.Build<KnappingRecipe>(recipe, origin), origin);

    /// <summary>
    /// Removes every knapping recipe producing the given output code. The code may
    /// contain a <c>*</c> wildcard, so <c>"game:knifeblade-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        RecordRemoval<KnappingRecipe>(selector, origin);

    /// <summary>
    /// Counts the knapping recipes currently registered. Reads the registry as it
    /// stood before this run's changes, which are applied only once every script
    /// has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => Registry.Knapping.Count;
}

/// <summary>Recipes raised layer by layer from a lump of clay.</summary>
[LuaModule("moontweaks.recipes.clayforming")]
public sealed class ClayFormingDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
    : RegistryRecipeDomain(log, world, registry)
{
    private readonly VoxelRecipeFactory factory = new(world);

    /// <inheritdoc/>
    protected override string Kind => "clay forming";

    /// <summary>
    /// Registers a new clay forming recipe. An ingredient whose code contains a
    /// wildcard expands into one recipe per matching variant, with the variant
    /// substituted into any <c>{name}</c> placeholder in the output code.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, ClayFormingRecipeSpec recipe) =>
        RecordAddition(recipe, recipe.OutputCode, factory.Build<ClayFormingRecipe>(recipe, origin), origin);

    /// <summary>
    /// Removes every clay forming recipe producing the given output code. The code
    /// may contain a <c>*</c> wildcard, so <c>"game:bowl-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        RecordRemoval<ClayFormingRecipe>(selector, origin);

    /// <summary>
    /// Counts the clay forming recipes currently registered. Reads the registry as
    /// it stood before this run's changes, which are applied only once every script
    /// has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => Registry.ClayForming.Count;
}

/// <summary>Recipes hammered from a hot ingot on an anvil.</summary>
[LuaModule("moontweaks.recipes.smithing")]
public sealed class SmithingDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
    : RegistryRecipeDomain(log, world, registry)
{
    private readonly VoxelRecipeFactory factory = new(world);

    /// <inheritdoc/>
    protected override string Kind => "smithing";

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

        RecordAddition(recipe, recipe.OutputCode, built, origin);
    }

    /// <summary>
    /// Removes every smithing recipe producing the given output code. The code may
    /// contain a <c>*</c> wildcard, so <c>"game:axehead-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        RecordRemoval<SmithingRecipe>(selector, origin);

    /// <summary>
    /// Counts the smithing recipes currently registered. Reads the registry as it
    /// stood before this run's changes, which are applied only once every script
    /// has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => Registry.Smithing.Count;
}

/// <summary>Recipes a barrel mixes on the spot or seals for a while.</summary>
[LuaModule("moontweaks.recipes.barrel")]
public sealed class BarrelDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
    : RegistryRecipeDomain(log, world, registry)
{
    private readonly BarrelRecipeFactory factory = new(world);

    /// <inheritdoc/>
    protected override string Kind => "barrel";

    /// <summary>
    /// Registers a new barrel recipe. An ingredient whose code contains a wildcard
    /// expands into one recipe per matching variant, with the variant substituted
    /// into any <c>{name}</c> placeholder in the output code.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The recipe to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, BarrelRecipeSpec recipe) =>
        RecordAddition(recipe, recipe.OutputCode, factory.Build(recipe, origin), origin);

    /// <summary>
    /// Removes every barrel recipe producing the given output code. The code may
    /// contain a <c>*</c> wildcard, so <c>"game:*-cured"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        RecordRemoval<BarrelRecipe>(selector, origin);

    /// <summary>
    /// Counts the barrel recipes currently registered. Reads the registry as it stood
    /// before this run's changes, which are applied only once every script has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => Registry.Barrel.Count;
}

/// <summary>Alloys a crucible smelts from a mix of metals.</summary>
[LuaModule("moontweaks.recipes.alloy")]
public sealed class AlloyDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
    : RegistryRecipeDomain(log, world, registry)
{
    private readonly AlloyRecipeFactory factory = new(world);

    /// <inheritdoc/>
    protected override string Kind => "alloy";

    /// <summary>
    /// Registers a new alloy. Unlike every other kind, an alloy names one metal per
    /// ingredient rather than a family: the game holds it as a list of shares rather
    /// than as a recipe, so a wildcard has nothing to expand into and is refused.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The alloy to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, AlloyRecipeSpec recipe) =>
        RecordAddition(recipe, recipe.OutputCode, [factory.Build(recipe, origin)], origin);

    /// <summary>
    /// Removes every alloy smelting into the given output code. The code may contain
    /// a <c>*</c> wildcard, so <c>"game:ingot-*"</c> removes the whole family.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which alloys to remove, by output code or by tags.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, RecipeSelectorSpec selector) =>
        RecordRemoval<AlloyRecipe>(selector, origin);

    /// <summary>
    /// Counts the alloys currently registered. Reads the registry as it stood before
    /// this run's changes, which are applied only once every script has run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => Registry.Alloy.Count;
}

/// <summary>Meals a pot cooks over a fire.</summary>
/// <remarks>
/// The odd one out. A cooking recipe makes no single product: what comes out is a
/// container of servings named after what went in, so a recipe is identified by the
/// code it carries and that is what selects one. Nothing expands either — a wildcard
/// among the stacks an ingredient accepts stays a wildcard, and the pot matches
/// against it as it cooks.
/// </remarks>
[LuaModule("moontweaks.recipes.cooking")]
public sealed class CookingDomain(MutationLog log, IWorldAccessor world, RecipeRegistry registry)
    : RegistryRecipeDomain(log, world, registry)
{
    private readonly CookingRecipeFactory factory = new(world);

    /// <inheritdoc/>
    protected override string Kind => "cooking";

    /// <summary>
    /// Registers a new meal. Its <c>code</c> names the recipe and the meal both, and
    /// no two recipes should carry the same one: the game resolves a meal by taking
    /// the first code that matches, so a second would never be reached.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="recipe">The meal to add.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, CookingRecipeSpec recipe) =>
        // Named by the code it carries rather than by what it makes, since a meal has
        // no single output and that code is what removing one matches against.
        RecordAddition(recipe, recipe.Code, [factory.Build(recipe, origin)], origin);

    /// <summary>
    /// Removes every cooking recipe carrying the given code. The code may contain a
    /// <c>*</c> wildcard, so <c>"*stew"</c> removes every stew. This is the recipe's
    /// own code rather than an asset code, because a meal has no single output to
    /// name it by.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which recipes to remove, by the code they carry.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, CookingSelectorSpec selector) =>
        Log.Record(new RemoveCookingRecipes(origin, new CookingSelector(selector, origin), Registry));

    /// <summary>
    /// Counts the cooking recipes currently registered. Reads the registry as it
    /// stood before this run's changes, which are applied only once every script has
    /// run.
    /// </summary>
    /// <param name="origin">Script line requesting the count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin) => Registry.Cooking.Count;
}
