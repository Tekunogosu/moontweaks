using System;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace MoonTweaks.Assets;

/// <summary>
/// The assets a script names, translated into what the game holds: a stack, and the
/// JSON it stores arbitrary data as. Sole owner of both, so a recipe and an item
/// property describing the same thing describe it the same way.
/// </summary>
public sealed class AssetStacks(IWorldAccessor world)
{
    private readonly AssetKindResolver kinds = new(world);

    /// <summary>The world these assets come from, for the stacks that must resolve against it.</summary>
    public IWorldAccessor World => world;

    /// <summary>Which registry a code names, for the shapes that have to say.</summary>
    public EnumItemClass Resolve(string code, ResourceKind? declared, ScriptOrigin origin, string path) =>
        kinds.Resolve(code, declared, origin, path);

    /// <summary>A named asset and how many of it, for the fields the game holds as a stack.</summary>
    public JsonItemStack Stack(StackSpec spec, ScriptOrigin origin, string path) => new()
    {
        Type = kinds.Resolve(spec.Code!, spec.Type, origin, path),
        Code = new AssetLocation(spec.Code),
        StackSize = spec.StackSize,
        Attributes = Attributes(spec.Attributes),
    };

    /// <summary>
    /// A named asset as the stack the game hands out, attributes and all. Sole owner
    /// of turning what a script wrote into a stack something can be given: the code
    /// is checked against the registries and the attributes are applied exactly as
    /// the game applies its own, so a scripted pie is the pie a recipe would make.
    /// </summary>
    public ItemStack Resolved(StackSpec spec, ScriptOrigin origin, string path)
    {
        var stack = Stack(spec, origin, path);

        if (!stack.Resolve(world, $"moontweaks {origin}") || stack.ResolvedItemstack is null)
        {
            throw new ScriptError(origin, $"{path} names '{spec.Code}', which resolved to nothing");
        }

        return stack.ResolvedItemstack;
    }

    /// <summary>
    /// How something changes once it stops being fresh, as the game holds it. Sole
    /// owner of that translation: a meal spoiling and an item spoiling are the same
    /// shape, and the game reads them through the same type.
    /// </summary>
    public TransitionableProperties Transitionable(
        TransitionableSpec spec, ScriptOrigin origin, string path) => new()
    {
        Type = ValueSet.As<EnumTransitionType>(spec.Type),
        FreshHours = Hours(spec.FreshHours),
        TransitionHours = Hours(spec.TransitionHours),
        TransitionedStack = Stack(spec.TransitionedStack, origin, $"{path}.transitionedStack"),
        TransitionRatio = (float)spec.TransitionRatio,
    };

    /// <summary>A span of in-game hours, which the game holds as a range it draws from.</summary>
    private static NatFloat Hours(SpreadSpec spread) =>
        NatFloat.createUniform((float)spread.Average, (float)spread.Variance);

    /// <summary>
    /// Turns a list of tag names into the condition an ingredient matches against.
    /// One condition requiring every tag: an asset carrying only some of them does
    /// not match.
    /// </summary>
    /// <remarks>
    /// The flag reads backwards and decides which of two meanings
    /// <c>RequiredTags</c> has. Disjunctive asks whether the tags are all contained
    /// in the asset's; conjunctive asks only whether the two sets overlap, so a
    /// single condition built that way accepts an asset carrying any one of them.
    /// This is the shape the game's own converter builds for a bare tag array, so a
    /// script's <c>tags</c> and a recipe file's mean the same thing.
    /// </remarks>
    /// <param name="tags">Tag names the script wrote.</param>
    /// <param name="origin">Script line that wrote them.</param>
    /// <param name="path">
    /// Where the tags themselves sit, as a failure should name them — the whole path
    /// including the key, not the shape holding it.
    /// </param>
    public ComplexTagCondition<TagSet> Condition(string[]? tags, ScriptOrigin origin, string path)
    {
        if (tags is null || tags.Length == 0) return default;

        var registry = world.Api.CollectibleTagRegistry;

        // The registry reports which names it did not know rather than guessing, so
        // a misspelled tag names itself instead of silently matching nothing.
        if (registry.TryCreateTagSet(out var required, tags) is var error && error != TagRegistryError.None)
        {
            var unknown = tags.Where(tag =>
                registry.TryCreateTagSet(out _, [tag]) != TagRegistryError.None).ToList();

            throw new ScriptError(origin, unknown.Count > 0
                ? $"{path} names {string.Join(", ", unknown.Select(tag => $"'{tag}'"))}, "
                  + "which no item or block carries"
                : $"{path} could not be read ({error})");
        }

        return new ComplexTagCondition<TagSet>
        {
            conditions = [new ComplexTagCondition<TagSet>.Condition { RequiredTags = required }],
            isDisjunctive = true,
        };
    }

    /// <summary>
    /// Whether an asset carries what a condition asks for. An empty condition matches
    /// everything, which is what makes tags optional beside a code.
    /// </summary>
    public static bool Matches(ComplexTagCondition<TagSet> condition, CollectibleObject asset) =>
        condition.IsEmpty || condition.Matches(asset.Tags);

    /// <summary>
    /// A Lua table as the JSON object the game stores arbitrary data in. The
    /// conversion itself belongs to <see cref="ScriptJson"/>; this only wraps it in
    /// the type the game holds.
    /// </summary>
    public static JsonObject? Attributes(ScriptValue? value) =>
        value is null or ScriptValue.Nil ? null : new JsonObject(ScriptJson.Token(value));
}
