using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Entities;
using MoonTweaks.Players;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MoonTweaks.Inventories;

/// <summary>
/// Reaching a set of slots, wherever it is, and moving stacks in and out of one.
/// Sole owner of both: a chest and a backpack are the same problem once the slots are
/// in hand, and answering "where are the slots" three times is how one of the three
/// ends up behaving differently.
/// </summary>
/// <remarks>
/// Slots are counted from 1 here and from 0 in the game. Lua counts from 1 and every
/// other list a script handles does too, so the conversion happens at this boundary
/// and nowhere else.
/// </remarks>
public sealed class InventoryAccess(
    ICoreServerAPI api, PlayerAccess players, EntityAccess entities)
{
    /// <summary>
    /// The slots a script named. Refuses the three ways of naming none: naming
    /// nothing at all, naming more than one thing at once, and naming a place that
    /// holds nothing with slots in it.
    /// </summary>
    public IInventory Of(WhereSpec where, ScriptOrigin origin)
    {
        var position = (where.X, where.Y, where.Z) is (not null, not null, not null);
        var named = new[] { where.Player is not null, where.Entity is not null, position }
            .Count(given => given);

        if (named == 0)
        {
            throw new ScriptError(origin,
                "no 'player', 'entity' or 'x'/'y'/'z' names whose slots to act on");
        }

        if (named > 1)
        {
            throw new ScriptError(origin,
                "name a 'player', an 'entity' or an 'x'/'y'/'z', but only one of them");
        }

        if (where.Player is { } player) return OfPlayer(player, where, origin);

        // Beside anything but a player, 'which' names nothing. Refused rather than
        // ignored: a script that wrote it meant something by it, and silently acting
        // on a different set of slots is the one outcome it cannot notice.
        if (where.Which is not null)
        {
            throw new ScriptError(origin,
                "'which' names one of a player's own inventories, "
                + "so it means nothing beside an 'entity' or a position");
        }

        if (where.Entity is { } entity) return OfEntity(entity, origin);

        return OfBlock(where.X!.Value, where.Y!.Value, where.Z!.Value, origin);
    }

    /// <summary>One of a player's own inventories, named by which of them is meant.</summary>
    private IInventory OfPlayer(string player, WhereSpec where, ScriptOrigin origin)
    {
        var bag = where.Which ?? EnumBagKind.Backpack;

        return players.Find(player, origin).InventoryManager.GetOwnInventory(ClassNameOf(bag))
            ?? throw new ScriptError(origin, $"'{player}' has no {bag.ToString().ToLowerInvariant()} inventory");
    }

    /// <summary>
    /// What an entity is carrying, for the ones that carry anything. Most do not, and
    /// say so rather than answering with an empty set that would look like an empty bag.
    /// </summary>
    private IInventory OfEntity(double entity, ScriptOrigin origin)
    {
        var found = entities.Find(entity, origin);

        return found.GetBehavior<EntityBehaviorContainer>()?.Inventory
            ?? throw new ScriptError(origin, $"'{found.Code}' carries no inventory");
    }

    /// <summary>Whatever container stands at a position.</summary>
    private IInventory OfBlock(int x, int y, int z, ScriptOrigin origin)
    {
        var at = new BlockPos(x, y, z);

        if (api.World.BlockAccessor.GetBlockEntity(at) is not IBlockEntityContainer container)
        {
            var standing = api.World.BlockAccessor.GetBlock(at)?.Code?.ToString() ?? "nothing";
            throw new ScriptError(origin, $"'{standing}' at {x} {y} {z} holds nothing with slots in it");
        }

        return container.Inventory;
    }

    /// <summary>The name the game keeps one of a player's inventories under.</summary>
    private static string ClassNameOf(EnumBagKind bag) => bag switch
    {
        EnumBagKind.Hotbar => GlobalConstants.hotBarInvClassName,
        EnumBagKind.Character => GlobalConstants.characterInvClassName,
        EnumBagKind.CraftingGrid => GlobalConstants.craftingInvClassName,
        EnumBagKind.Mouse => GlobalConstants.mousecursorInvClassName,
        EnumBagKind.Creative => GlobalConstants.creativeInvClassName,
        _ => GlobalConstants.backpackInvClassName,
    };

    /// <summary>What is standing in one slot, or nothing where it is empty.</summary>
    public static SlotPayload? Describe(ItemSlot slot, int index) =>
        slot.Itemstack is not { } stack
            ? null
            : new SlotPayload
            {
                Slot = index + 1,
                Code = stack.Collectible?.Code?.ToString() ?? "",
                Quantity = stack.StackSize,
                MaxStackSize = stack.Collectible?.MaxStackSize ?? 1,
                // Reads the name off Collectible, which an unresolved stack has none
                // of, whatever the declared type says.
                Name = stack.GetName() ?? "",
                Durability = Wear(stack, remaining: true),
                MaxDurability = Wear(stack, remaining: false),
            };

    /// <summary>
    /// How much use a stack has left, or how much its kind has when new, and nil for
    /// anything that does not wear out.
    /// </summary>
    /// <remarks>
    /// Asked of the collectible rather than read off the stack's attributes, because
    /// the game lets a behaviour answer for both and reading the attribute directly
    /// would miss whatever a mod does with it. Something with no maximum has no
    /// durability at all, which is a different answer from having none left.
    /// </remarks>
    private static int? Wear(ItemStack stack, bool remaining)
    {
        if (stack.Collectible is not { } kind) return null;

        var maximum = kind.GetMaxDurability(stack);
        return maximum <= 0 ? null : remaining ? kind.GetRemainingDurability(stack) : maximum;
    }

    /// <summary>Everything standing in an inventory, in slot order, skipping the empty ones.</summary>
    public static IReadOnlyList<SlotPayload> List(IInventory inventory) =>
        [.. inventory.Select((slot, index) => Describe(slot, index)).OfType<SlotPayload>()];

    /// <summary>
    /// One slot by the number a script wrote, counting from 1. Out of range is
    /// refused rather than answered with nothing, since a script asking for slot 40 of
    /// a nine-slot bag has made a mistake rather than found an empty one.
    /// </summary>
    /// <remarks>
    /// An inventory numbers its slots from 0 and walking it hands them over in that
    /// same order, so the number is turned into an index rather than the whole set
    /// being read out to count along.
    /// </remarks>
    public static ItemSlot At(IInventory inventory, int slot, ScriptOrigin origin) =>
        slot >= 1 && slot <= inventory.Count
            ? inventory[slot - 1]!
            : throw new ScriptError(origin,
                $"slot {slot} is outside this inventory, "
                + $"which has {inventory.Count} slot(s) numbered from 1");

    /// <summary>How many slots an inventory has, full or not.</summary>
    public static int Size(IInventory inventory) => inventory.Count;

    /// <summary>
    /// How many of something an inventory holds, added up across every slot. The code
    /// may be a <c>*</c> wildcard, so one call counts a whole family.
    /// </summary>
    public static int Count(IInventory inventory, string code)
    {
        var wanted = new AssetLocation(code);

        return inventory
            .Where(slot => Matches(slot, wanted))
            .Sum(slot => slot.Itemstack!.StackSize);
    }

    /// <summary>Whether a slot holds something a code names.</summary>
    private static bool Matches(ItemSlot slot, AssetLocation wanted) =>
        slot.Itemstack?.Collectible?.Code is { } code && WildcardUtil.Match(wanted, code);

    /// <summary>
    /// Takes what it can of a stack out of an inventory, and says how many it got.
    /// </summary>
    /// <remarks>
    /// Answering less than was asked for is ordinary rather than a failure: a script
    /// charging somebody for something checks the count that came back rather than
    /// assuming it got everything.
    /// </remarks>
    public static int Take(IInventory inventory, string code, int quantity)
    {
        var wanted = new AssetLocation(code);
        var left = quantity;
        var taken = 0;

        foreach (var slot in inventory)
        {
            if (left <= 0) break;
            if (!Matches(slot, wanted)) continue;

            var going = System.Math.Min(left, slot.Itemstack!.StackSize);
            slot.TakeOut(going);
            slot.MarkDirty();

            taken += going;
            left -= going;
        }

        return taken;
    }

    /// <summary>
    /// Puts what it can of a stack into an inventory, and says how many fitted.
    /// </summary>
    /// <remarks>
    /// Merged into part-full slots before empty ones are used, as the game does when
    /// a player picks something up. Whatever did not fit stays in the
    /// caller's hands rather than being lost, so a script that must not lose it drops
    /// the remainder on the floor.
    /// </remarks>
    public static int Put(IInventory inventory, ItemStack stack, IWorldAccessor world) =>
        Give(inventory, new DummySlot(stack), stack.StackSize, world);

    /// <summary>
    /// Moves what it can of a stack out of one set of slots and into another, and says
    /// how many arrived. Sole owner of a move: nothing else may take from one place
    /// and put in another, because whatever did not fit has to go back where it came
    /// from and there is exactly one correct way to do that.
    /// </summary>
    /// <remarks>
    /// The stacks themselves move, rather than being described and rebuilt. A worn axe
    /// arrives worn and a labelled crock arrives labelled, which taking a code out of
    /// one place and putting a fresh one into another cannot do.
    ///
    /// Nothing is ever taken out and dropped: a stack is only ever removed by being
    /// put down somewhere, so a destination that fills up leaves the rest exactly where
    /// it was.
    /// </remarks>
    public static int Move(
        IInventory from, IInventory to, string code, int quantity, IWorldAccessor world)
    {
        var wanted = new AssetLocation(code);
        var moved = 0;

        foreach (var source in from)
        {
            if (moved >= quantity) break;
            if (!Matches(source, wanted)) continue;

            moved += Give(to, source, quantity - moved, world);
        }

        return moved;
    }

    /// <summary>
    /// Offers up to a limit out of one slot to a whole inventory, and says how many
    /// were taken. Sole owner of the order they are offered in.
    /// </summary>
    /// <remarks>
    /// Two passes rather than one. A single pass in slot order fills the first empty
    /// slot it reaches, so a stack that would have merged into a part-full one further
    /// along takes a whole slot for itself instead — which is how a bag with room comes
    /// back full.
    /// </remarks>
    private static int Give(IInventory inventory, ItemSlot source, int limit, IWorldAccessor world)
    {
        var before = source.StackSize;

        var taken = Offer(inventory, source, world, limit, slot => !slot.Empty);
        taken += Offer(inventory, source, world, limit - taken, slot => slot.Empty);

        // Marked once rather than per slot, and only where something actually left it.
        // A dummy slot has no inventory to tell, and answers this harmlessly.
        if (source.StackSize != before) source.MarkDirty();

        return taken;
    }

    /// <summary>Moves as much as will go into the slots one pass is interested in.</summary>
    private static int Offer(
        IInventory inventory,
        ItemSlot source,
        IWorldAccessor world,
        int limit,
        System.Func<ItemSlot, bool> wanted)
    {
        var taken = 0;

        foreach (var slot in inventory)
        {
            if (taken >= limit || source.Itemstack is not { StackSize: > 0 }) break;

            // A move within one inventory would otherwise offer a slot to itself,
            // which the game answers by quietly emptying it.
            if (slot == source || !wanted(slot)) continue;

            var went = source.TryPutInto(
                world, slot, System.Math.Min(limit - taken, source.Itemstack.StackSize));

            // Marked only where something actually landed, so a pass over a full bag
            // does not send every slot in it to the player again.
            if (went > 0)
            {
                slot.MarkDirty();
                taken += went;
            }
        }

        return taken;
    }

    /// <summary>Empties every slot, and says how many held anything.</summary>
    public static int Clear(IInventory inventory)
    {
        var emptied = 0;

        foreach (var slot in inventory)
        {
            if (slot.Empty) continue;

            slot.Itemstack = null;
            slot.MarkDirty();
            emptied++;
        }

        return emptied;
    }

    /// <summary>The slot a player is holding, which is whichever of the hotbar is active.</summary>
    public ItemSlot Held(string player, ScriptOrigin origin) =>
        players.Find(player, origin).InventoryManager.ActiveHotbarSlot
        ?? throw new ScriptError(origin, $"'{player}' is holding nothing and has no hand to hold it in");
}
