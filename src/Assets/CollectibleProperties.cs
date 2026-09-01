using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoonTweaks.Assets;

/// <summary>
/// The properties a script may change on an item or a block, written onto the
/// registry objects the game already holds. Sole owner of that translation, so
/// nothing else decides what a key means or which of them a script left alone.
/// </summary>
/// <remarks>
/// These reach players as well as the server. The item and block registries are
/// sent to every client in one packet built at run phase <c>WorldReady</c>, which is
/// long after scripts run, so what a script changes here is what a client is told
/// and what its tooltip shows. Names and descriptions are not among them: those are
/// looked up in each side's own language files against a code that never changes.
/// </remarks>
public static class CollectibleProperties
{
    /// <summary>Applies every property a script named, leaving the rest as they were.</summary>
    public static void ApplyTo(
        CollectibleObject asset, AssetPropertiesSpec spec, AssetStacks stacks, ScriptOrigin origin)
    {
        if (spec.Durability is { } durability) asset.Durability = durability;
        if (spec.MaxStackSize is { } stack) asset.MaxStackSize = stack;
        if (spec.AttackPower is { } power) asset.AttackPower = (float)power;
        if (spec.AttackRange is { } range) asset.AttackRange = (float)range;
        if (spec.ToolTier is { } tier) asset.ToolTier = tier;
        if (spec.Tool is { } tool) asset.Tool = ValueSet.As<EnumTool>(tool);
        if (spec.MaterialDensity is { } density) asset.MaterialDensity = density;
        if (spec.MiningSpeed is { } speeds) asset.MiningSpeed = Speeds(speeds);
        if (spec.Attributes is not null) asset.Attributes = AssetStacks.Attributes(spec.Attributes);
        if (spec.StorageFlags is { } storage) asset.StorageFlags = Flags(storage);
        if (spec.DamagedBy is { } sources) asset.DamagedBy = Sources(sources);
        if (spec.Combustible is { } fire) Burn(asset, fire, stacks, origin);
        if (spec.Nutrition is { } food) Eat(asset, food, stacks, origin);
        if (spec.Grinding is { } grind) Grind(asset, grind, stacks, origin);
        if (spec.Crushing is { } crush) Crush(asset, crush, stacks, origin);
        if (spec.TransitionableProps is { } changes) asset.TransitionableProps = Stale(changes, stacks, origin);
        if (spec.CreativeInventoryTabs is { } tabs) asset.CreativeInventoryTabs = tabs;
        if (spec.CreativeInventoryStacks is { } shown) asset.CreativeInventoryStacks = Shown(shown, stacks, origin);

        // Last, and asked as one question rather than two: adding reads what is
        // carried now, so a spec naming both would otherwise depend on which ran first.
        if (spec.AddTags is not null || spec.SetTags is not null)
        {
            asset.Tags = TagRegistration.Wanted(
                stacks.World.Api.CollectibleTagRegistry, asset.Tags, spec.AddTags, spec.SetTags, origin);
        }
    }

    /// <summary>
    /// How something changes once it stops being fresh, as the game holds it. A list
    /// rather than one, because meat that both rots and cures carries two, and which
    /// applies is decided by where it is kept.
    /// </summary>
    private static TransitionableProperties[] Stale(
        TransitionableSpec[] specs, AssetStacks stacks, ScriptOrigin origin) =>
        [.. specs.Select((spec, index) =>
            stacks.Transitionable(spec, origin, $"transitionableProps[{index + 1}]"))];

    /// <summary>
    /// The creative entries something appears as. Each stack is resolved here rather
    /// than when a tab is drawn: an unresolved one is a gap in the creative menu, and
    /// the server that could have said so is long gone by then.
    /// </summary>
    private static CreativeTabAndStackList[] Shown(
        CreativeStacksSpec[] specs, AssetStacks stacks, ScriptOrigin origin) =>
        [.. specs.Select((spec, index) => new CreativeTabAndStackList
        {
            Tabs = spec.Tabs,
            Stacks = [.. spec.Stacks.Select((stack, which) => Stack(
                stack, stacks, origin, $"creativeInventoryStacks[{index + 1}].stacks[{which + 1}]"))],
        })];

    /// <summary>
    /// What burning or smelting it does, merged into whatever it already said. Merged
    /// rather than replaced so the rule holds one level down as well: a script that
    /// names a melting point moves that and nothing else.
    /// </summary>
    private static void Burn(
        CollectibleObject asset, CombustibleSpec spec, AssetStacks stacks, ScriptOrigin origin)
    {
        var props = asset.CombustibleProps ??= new CombustibleProperties();

        if (spec.BurnTemperature is { } temperature) props.BurnTemperature = temperature;
        if (spec.BurnDuration is { } duration) props.BurnDuration = (float)duration;
        if (spec.SmokeLevel is { } smoke) props.SmokeLevel = (float)smoke;
        if (spec.MeltingPoint is { } melting) props.MeltingPoint = melting;
        if (spec.MeltingDuration is { } melts) props.MeltingDuration = (float)melts;
        if (spec.MaxTemperature is { } maximum) props.MaxTemperature = maximum;
        if (spec.HeatResistance is { } resistance) props.HeatResistance = resistance;
        if (spec.SmeltedRatio is { } ratio) props.SmeltedRatio = ratio;
        if (spec.RequiresContainer is { } container) props.RequiresContainer = container;
        if (spec.SmeltingType is { } kind) props.SmeltingType = ValueSet.As<EnumSmeltType>(kind);
        if (spec.SmeltedStack is { } smelted)
        {
            props.SmeltedStack = Stack(smelted, stacks, origin, "combustible.smeltedStack");
        }
    }

    /// <summary>What eating it does, merged into whatever it already said.</summary>
    private static void Eat(
        CollectibleObject asset, NutritionSpec spec, AssetStacks stacks, ScriptOrigin origin)
    {
        var props = asset.NutritionProps ??= new FoodNutritionProperties();

        if (spec.FoodCategory is { } category) props.FoodCategory = ValueSet.As<EnumFoodCategory>(category);
        if (spec.Satiety is { } satiety) props.Satiety = (float)satiety;
        if (spec.Health is { } health) props.Health = (float)health;
        if (spec.SatietyLossDelay is { } delay) props.SaturationLossDelay = (float)delay;
        if (spec.Intoxication is { } intoxication) props.Intoxication = (float)intoxication;
        if (spec.EatenStack is { } eaten)
        {
            props.EatenStack = Stack(eaten, stacks, origin, "nutrition.eatenStack");
        }
    }

    /// <summary>What grinding it yields.</summary>
    private static void Grind(
        CollectibleObject asset, GrindingSpec spec, AssetStacks stacks, ScriptOrigin origin)
    {
        asset.GrindingProps ??= new GrindingProperties();
        asset.GrindingProps.GroundStack =
            Stack(spec.GroundStack, stacks, origin, "grinding.groundStack");
    }

    /// <summary>What crushing it yields, merged into whatever it already said.</summary>
    private static void Crush(
        CollectibleObject asset, CrushingSpec spec, AssetStacks stacks, ScriptOrigin origin)
    {
        var props = asset.CrushingProps ??= new CrushingProperties();

        props.CrushedStack = Stack(spec.CrushedStack, stacks, origin, "crushing.crushedStack");
        if (spec.HardnessTier is { } tier) props.HardnessTier = tier;
        if (spec.Quantity is { } spread) props.Quantity = AssetStacks.Range(spread);
    }

    /// <summary>
    /// A stack the game can hand out. These are resolved as their owner is loaded,
    /// which has already happened by the time a script names one, so an unresolved
    /// stack would reach a player as nothing at all.
    /// </summary>
    private static JsonItemStack Stack(
        StackSpec spec, AssetStacks stacks, ScriptOrigin origin, string path) =>
        stacks.Resolve(stacks.Stack(spec, origin, path), origin, path);

    /// <summary>
    /// The keys that say what to change rather than what to change it to. Everything
    /// else on a spec is a property, which is what lets the question below be asked
    /// of the shape itself rather than of a list kept by hand.
    /// </summary>
    private static readonly string[] Selectors = ["code", "tags"];

    /// <summary>
    /// Whether a script named anything at all to change, asked of whichever shape it
    /// wrote — the block shape carries every item key and its own besides.
    /// </summary>
    /// <remarks>
    /// Read off the spec rather than listed here, so a property added above cannot be
    /// forgotten below. A forgotten one fails quietly and in the worst direction: the
    /// script is told it asked for nothing, on a line where it plainly asked for
    /// something.
    /// </remarks>
    public static bool ChangesAnything(AssetPropertiesSpec spec) =>
        SpecBinder.FieldsOf(spec.GetType())
            .Where(field => !Selectors.Contains(field.Key))
            .Any(field => field.Value.GetValue(spec) is not null);

    /// <summary>Which inventories something may be put in, as the flags the game holds.</summary>
    private static EnumItemStorageFlags Flags(EnumStorageKind[] kinds) =>
        kinds.Select(kind => ValueSet.As<EnumItemStorageFlags>(kind))
            .Aggregate(default(EnumItemStorageFlags), (all, flag) => all | flag);

    /// <summary>What wears something down, as the sources the game holds.</summary>
    private static EnumItemDamageSource[] Sources(EnumDamageKind[] kinds) =>
        [.. kinds.Select(kind => ValueSet.As<EnumItemDamageSource>(kind))];

    /// <summary>Mining speeds keyed by the block material each applies to.</summary>
    private static Dictionary<EnumBlockMaterial, float> Speeds(Dictionary<EnumBlockKind, double> speeds) =>
        speeds.ToDictionary(each => ValueSet.As<EnumBlockMaterial>(each.Key), each => (float)each.Value);
}
