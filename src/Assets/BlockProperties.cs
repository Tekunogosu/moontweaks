using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoonTweaks.Assets;

/// <summary>
/// The properties only a block standing in the world has, written onto the blocks the
/// game already holds. Sole owner of that translation, in the same way
/// <see cref="CollectibleProperties"/> owns the ones a block shares with an item.
/// </summary>
/// <remarks>
/// Split from its sibling rather than folded into it because the two answer different
/// questions. Durability and stack size describe a thing in a hand, and a block in a
/// hand is an item; hardness, drops and light describe a thing standing in the world,
/// which an item never is. One shape carrying both would offer every item key a block
/// cannot use.
/// </remarks>
public static class BlockProperties
{
    /// <summary>Applies every block property a script named, leaving the rest as they were.</summary>
    public static void ApplyTo(
        Block block, BlockPropertiesSpec spec, AssetStacks stacks, ScriptOrigin origin)
    {
        if (spec.Resistance is { } resistance) block.Resistance = (float)resistance;
        if (spec.RequiredMiningTier is { } tier) block.RequiredMiningTier = tier;
        if (spec.BlockMaterial is { } material) block.BlockMaterial = ValueSet.As<EnumBlockMaterial>(material);
        if (spec.LightAbsorption is { } absorption) block.LightAbsorption = absorption;
        if (spec.Replaceable is { } replaceable) block.Replaceable = replaceable;
        if (spec.Fertility is { } fertility) block.Fertility = fertility;
        if (spec.WalkSpeedMultiplier is { } walk) block.WalkSpeedMultiplier = (float)walk;
        if (spec.DragMultiplier is { } drag) block.DragMultiplier = (float)drag;
        if (spec.Climbable is { } climbable) block.Climbable = climbable;
        if (spec.RainPermeable is { } permeable) block.RainPermeable = permeable;
        if (spec.Light is { } light) block.LightHsv = Colour(light, origin);
        if (spec.Drops is { } drops) block.Drops = Dropped(block, drops, stacks, origin);
    }

    /// <summary>
    /// A colour as the three bytes the game packs it into. Checked rather than
    /// truncated: a brightness of 256 written into a byte is a block that gives off
    /// nothing, which looks like the change having been ignored.
    /// </summary>
    private static byte[] Colour(LightSpec light, ScriptOrigin origin) =>
    [
        Byte(light.Hue, origin, "light.hue"),
        Byte(light.Saturation, origin, "light.saturation"),
        Byte(light.Brightness, origin, "light.brightness"),
    ];

    /// <summary>One part of a colour, refused rather than wrapped when out of range.</summary>
    private static byte Byte(int value, ScriptOrigin origin, string path) =>
        value is >= 0 and <= 255
            ? (byte)value
            : throw new ScriptError(origin, $"{path} must be between 0 and 255, got {value}");

    /// <summary>
    /// What a block leaves behind, as the game holds it. Each is resolved here rather
    /// than when it is first rolled for: an unresolved drop is silently nothing at
    /// all, which a player only discovers by breaking the block.
    /// </summary>
    private static BlockDropItemStack[] Dropped(
        Block block, BlockDropSpec[] specs, AssetStacks stacks, ScriptOrigin origin) =>
        [.. specs.Select((spec, index) => Drop(block, spec, stacks, origin, $"drops[{index + 1}]"))];

    /// <inheritdoc cref="Dropped"/>
    private static BlockDropItemStack Drop(
        Block block, BlockDropSpec spec, AssetStacks stacks, ScriptOrigin origin, string path)
    {
        var drop = new BlockDropItemStack
        {
            Type = stacks.Resolve(spec.Code!, spec.Type, origin, path),
            Code = new AssetLocation(spec.Code),
            Quantity = spec.Quantity is { } many
                ? NatFloat.createUniform((float)many.Average, (float)many.Variance)
                : NatFloat.One,
            LastDrop = spec.LastDrop,
            Tool = spec.Tool is { } tool ? ValueSet.As<EnumTool>(tool) : null,
            Attributes = AssetStacks.Attributes(spec.Attributes),
        };

        if (!drop.Resolve(stacks.World, $"moontweaks {origin}", block.Code))
        {
            throw new ScriptError(origin, $"{path} names '{spec.Code}', which resolved to nothing");
        }

        return drop;
    }
}
