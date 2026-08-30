using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Recipes;

/// <summary>
/// Recipes of any kind at all, reached by the code the kind is registered under.
/// </summary>
/// <remarks>
/// The modules beneath this one — <c>moontweaks.recipes.grid</c> and its siblings —
/// each know one kind properly: what it makes, how it is written, what a mistake in
/// one looks like. They are what a script should reach for.
///
/// This is for the kinds they do not cover: one a mod added for itself, which nothing
/// here knows the shape of. It can say how many there are and it can take them away,
/// matching on what a recipe's output resolved to. It cannot add one, because
/// building a recipe means knowing its shape.
/// </remarks>
[LuaModule("moontweaks.recipes")]
public sealed class RecipeKindDomain(MutationLog log, IWorldAccessor world)
{
    private readonly RecipeKinds kinds = new(world);
    private readonly AssetStacks stacks = new(world);

    /// <summary>
    /// Every recipe kind this server holds, by the code it is registered under. This
    /// is how a script finds out what another mod called its own, since the name is
    /// the mod's to choose.
    /// </summary>
    /// <remarks>
    /// The game's own kinds are in here too, under the codes the survival mod
    /// registered them as: <c>knappingrecipes</c>, <c>clayformingrecipes</c>,
    /// <c>smithingrecipes</c>, <c>barrelrecipes</c>, <c>alloyrecipes</c> and
    /// <c>cookingrecipes</c>. Reaching those through their own modules is better in
    /// every way; they are listed because leaving them out would be a lie about what
    /// the server holds.
    /// </remarks>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("kinds")]
    public IReadOnlyList<string> Kinds(ScriptOrigin origin) => kinds.Names();

    /// <summary>
    /// How many recipes one kind holds. Reads the list as it stood before this run's
    /// changes, which are applied only once every script has run.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="kind">Code the kind is registered under.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin, string kind) => kinds.Of(kind, origin).Count;

    /// <summary>
    /// Removes every recipe of one kind whose output the selector names. The way to
    /// take out a recipe belonging to a mod this one knows nothing about.
    /// </summary>
    /// <remarks>
    /// Matches on the stack a recipe's output resolved to rather than on any code the
    /// recipe declared, since a kind this mod has never seen keeps that code wherever
    /// it likes. For the six kinds with modules of their own, those modules match more
    /// precisely and should be used instead.
    /// </remarks>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="selector">Which kind to remove from, and which of its recipes.</param>
    [LuaFunction("remove")]
    public void Remove(ScriptOrigin origin, KindSelectorSpec selector) =>
        log.Record(new RemoveKindRecipes(
            origin,
            selector.Kind,
            new RecipeSelector(
                new RecipeSelectorSpec { Code = selector.Code, Tags = selector.Tags }, stacks, origin),
            kinds));
}
