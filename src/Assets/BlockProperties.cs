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
        if (spec.Sounds is { } sounds) block.Sounds = Heard(block.Sounds, sounds);
        if (spec.CollisionBoxes is { } collision) block.CollisionBoxes = Boxes(collision);
        if (spec.SelectionBoxes is { } selection) block.SelectionBoxes = Boxes(selection);
        if (spec.CropProps is { } crop) Grow(block, crop);
    }

    /// <summary>
    /// What a block sounds like, merged into whatever it already said, so a script
    /// naming a breaking sound moves that and nothing else.
    /// </summary>
    /// <remarks>
    /// Copied before it is written to. A block type describes a family and the game
    /// hands the same sounds to every variant of it, so writing through the one it
    /// arrived with would change every block that shares it — which is not what
    /// naming a single code asked for.
    /// </remarks>
    private static BlockSounds Heard(BlockSounds? already, BlockSoundsSpec spec)
    {
        var sounds = already?.Clone() ?? new BlockSounds();

        if (spec.Walk is { } walk) sounds.Walk = Sound(sounds.Walk, walk);
        if (spec.Inside is { } inside) sounds.Inside = Sound(sounds.Inside, inside);
        if (spec.Breaking is { } breaking) sounds.Break = Sound(sounds.Break, breaking);
        if (spec.Place is { } place) sounds.Place = Sound(sounds.Place, place);
        if (spec.Hit is { } hit) sounds.Hit = Sound(sounds.Hit, hit);
        if (spec.Ambient is { } ambient) sounds.Ambient = Asset(ambient.Path);
        if (spec.AmbientBlockCount is { } together) sounds.AmbientBlockCount = (float)together;

        return sounds;
    }

    /// <summary>
    /// One sound, keeping whatever the block already said about how it is played.
    /// </summary>
    /// <remarks>
    /// The range a sound carries decides whether it is heard at all, and the game
    /// fills it in per kind of sound as it loads — 16 blocks for breaking, 12 for
    /// walking. A fresh one would carry zero, so a script naming only a code would
    /// silence the block it meant to give a voice to.
    /// </remarks>
    private static SoundAttributes Sound(SoundAttributes already, BlockSoundSpec spec)
    {
        var sound = already;

        sound.Location = Asset(spec.Path);
        if (spec.Range is { } range) sound.Range = (float)range;
        if (spec.Type is { } kind) sound.Type = ValueSet.As<EnumSoundType>(kind);
        if (spec.Pitch is { } pitch) sound.Pitch = AssetStacks.Range(pitch);
        if (spec.Volume is { } volume) sound.Volume = AssetStacks.Range(volume);

        return sound;
    }

    /// <summary>
    /// One sound asset, under the folder the game keeps sounds in. Added here where a
    /// script left it off, which is what the game's own loader does with the paths in
    /// a block's JSON — and its own assets are written both ways.
    /// </summary>
    private static AssetLocation Asset(string path) =>
        new AssetLocation(path).WithPathPrefixOnce("sounds/");

    /// <summary>
    /// The boxes a block occupies, as the game holds them. Built fresh rather than
    /// written into: a block that has never had boxes of its own points at one array
    /// the whole registry shares, and writing through it would reshape every block in
    /// the game.
    /// </summary>
    private static Cuboidf[] Boxes(BoxSpec[] specs) =>
    [
        .. specs.Select(box => new Cuboidf(
            (float)box.X1, (float)box.Y1, (float)box.Z1,
            (float)box.X2, (float)box.Y2, (float)box.Z2)),
    ];

    /// <summary>
    /// How a crop grows, merged into whatever it already said. A block the game does
    /// not farm has none, and is given one rather than refused: the properties are
    /// what a growth behaviour reads, so a block without that behaviour is unchanged
    /// by them either way.
    /// </summary>
    private static void Grow(Block block, CropSpec spec)
    {
        var props = block.CropProps ??= new BlockCropProperties();

        if (spec.RequiredNutrient is { } nutrient)
        {
            props.RequiredNutrient = ValueSet.As<EnumSoilNutrient>(nutrient);
        }
        if (spec.NutrientConsumption is { } consumption) props.NutrientConsumption = (float)consumption;
        if (spec.GrowthStages is { } stages) props.GrowthStages = stages;
        if (spec.TotalGrowthDays is { } days) props.TotalGrowthDays = (float)days;
        if (spec.TotalGrowthMonths is { } months) props.TotalGrowthMonths = (float)months;
        if (spec.MultipleHarvests is { } repeatable) props.MultipleHarvests = repeatable;
        if (spec.HarvestGrowthStageLoss is { } loss) props.HarvestGrowthStageLoss = loss;
        if (spec.ColdDamageBelow is { } cold) props.ColdDamageBelow = (float)cold;
        if (spec.HeatDamageAbove is { } heat) props.HeatDamageAbove = (float)heat;
        if (spec.DamageGrowthStuntMul is { } stunt) props.DamageGrowthStuntMul = (float)stunt;
        if (spec.ColdDamageRipeMul is { } ripe) props.ColdDamageRipeMul = (float)ripe;
    }

    /// <summary>
    /// A colour as the three bytes the game packs it into. Checked rather than
    /// truncated: a brightness of 256 written into a byte is a block that gives off
    /// nothing, which looks like the change having been ignored.
    /// </summary>
    private static byte[] Colour(LightSpec light, ScriptOrigin origin) =>
    [
        ColourChannel.Of(light.Hue, origin, "light.hue"),
        ColourChannel.Of(light.Saturation, origin, "light.saturation"),
        ColourChannel.Of(light.Brightness, origin, "light.brightness"),
    ];

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
            Quantity = spec.Quantity is { } many ? AssetStacks.Range(many) : NatFloat.One,
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
