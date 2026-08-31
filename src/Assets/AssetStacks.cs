using MoonTweaks.Api;
using MoonTweaks.Scripting;
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
    /// Resolves a stack against the registries, failing loudly. Sole owner of that
    /// step: the game's own resolve reports a failure only to the log and leaves the
    /// stack behind holding nothing, which reaches a player as an item that silently
    /// never arrives. Named here instead, against the field the script wrote.
    /// </summary>
    public JsonItemStack Resolve(JsonItemStack stack, ScriptOrigin origin, string path)
    {
        if (!stack.Resolve(world, $"moontweaks {origin}") || stack.ResolvedItemstack is null)
        {
            throw new ScriptError(origin, $"{path} names '{stack.Code}', which resolved to nothing");
        }

        return stack;
    }

    /// <summary>
    /// A named asset as the stack the game hands out, attributes and all. Sole owner
    /// of turning what a script wrote into a stack something can be given: the code
    /// is checked against the registries and the attributes are applied exactly as
    /// the game applies its own, so a scripted pie is the pie a recipe would make.
    /// </summary>
    public ItemStack Resolved(StackSpec spec, ScriptOrigin origin, string path) =>
        Resolve(Stack(spec, origin, path), origin, path).ResolvedItemstack!;

    /// <summary>
    /// How something changes once it stops being fresh, as the game holds it. Sole
    /// owner of that translation: a meal spoiling and an item spoiling are the same
    /// shape, and the game reads them through the same type.
    /// </summary>
    public TransitionableProperties Transitionable(
        TransitionableSpec spec, ScriptOrigin origin, string path) => new()
    {
        Type = ValueSet.As<EnumTransitionType>(spec.Type),
        FreshHours = Range(spec.FreshHours),
        TransitionHours = Range(spec.TransitionHours),
        TransitionedStack = Stack(spec.TransitionedStack, origin, $"{path}.transitionedStack"),
        TransitionRatio = (float)spec.TransitionRatio,
    };

    /// <summary>
    /// A range the game draws a number from. Sole owner of that translation: a
    /// crushing yield, a block drop and a span of hours are one shape written three
    /// times, and a distribution added to one of them belongs to all three.
    /// </summary>
    public static NatFloat Range(SpreadSpec spread) => NatFloat.create(
        ValueSet.As<EnumDistribution>(spread.Distribution),
        (float)spread.Average,
        (float)spread.Variance);

    /// <summary>
    /// The condition an asset is matched against, for the shapes that select by what
    /// something is. The grammar itself belongs to <see cref="TagConditions"/>; this
    /// is where the world hands over the registry that knows the tag names.
    /// </summary>
    /// <param name="tags">Condition the script wrote, if it wrote one.</param>
    /// <param name="origin">Script line that wrote it.</param>
    /// <param name="path">
    /// Where the tags themselves sit, as a failure should name them — the whole path
    /// including the key, not the shape holding it.
    /// </param>
    public ComplexTagCondition<TagSet> Condition(
        TagConditionSpec? tags, ScriptOrigin origin, string path) =>
        TagConditions.Build(world.Api.CollectibleTagRegistry, tags, origin, path);

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
