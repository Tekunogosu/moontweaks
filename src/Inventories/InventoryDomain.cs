using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Inventories;

/// <summary>
/// The slots of a world: what a player carries, what a chest holds, and what
/// something walking around is carrying.
/// </summary>
/// <remarks>
/// Every function here takes the same first argument, a table naming where the slots
/// are — a player and which of their inventories, a block position, or an entity. One
/// shape rather than three families of function, because everything a script does to
/// a chest it also does to a backpack; only where the slots are differs.
///
/// Slots are numbered from 1, as everything in Lua is.
///
/// These act on a loaded world, so they belong in a handler rather than in a script's
/// body: when scripts run, the recipes exist but the world does not.
/// </remarks>
/// <example>
/// <code>
/// local inventory = moontweaks.inventory
///
/// moontweaks.events.playerJoin(function(e)
///   local held = inventory.held(e.player)
///   if held then
///     moontweaks.log.info(("%s is holding %d x %s"):format(e.playerName, held.quantity, held.code))
///   end
///
///   -- Into their bags rather than into their hand, and it says how much fitted.
///   local given = inventory.put({ player = e.player }, { code = "game:bread-spelt-perfect", quantity = 2 })
///   moontweaks.log.info(("%d loaf/loaves fitted"):format(given))
/// end)
///
/// -- Walking what somebody is carrying. A slot is nil where nothing is in it.
/// moontweaks.commands.add {
///   name = "bags",
///   description = "List what you are carrying",
///   requiresPlayer = true,
///   handler = function(e)
///     local lines = {}
///     for _, slot in ipairs(inventory.list { player = e.player }) do
///       lines[#lines + 1] = ("%d: %d x %s"):format(slot.slot, slot.quantity, slot.code)
///     end
///     return #lines > 0 and table.concat(lines, "\n") or "You are carrying nothing."
///   end,
/// }
///
/// -- A chest is named by where it stands rather than by whose it is.
/// local chest = { x = 500, y = 110, z = 500 }
/// moontweaks.log.info(("chest holds %d gear"):format(inventory.count(chest, "game:gear-rusty")))
///
/// -- Charging for something: take says how much it actually got.
/// local paid = inventory.take({ player = "someone" }, { code = "game:gear-rusty", quantity = 2 })
/// if paid == 2 then
///   inventory.put({ player = "someone" }, "game:cloth-plain")
/// end
/// </code>
/// </example>
[LuaModule("moontweaks.inventory")]
public sealed class InventoryDomain(
    InventoryAccess inventories, AssetStacks stacks, IWorldAccessor world)
{
    /// <summary>How many slots there are, full or not.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="where">Whose slots to count.</param>
    [LuaFunction("size")]
    public int Size(ScriptOrigin origin, WhereSpec where) =>
        InventoryAccess.Size(inventories.Of(where, origin));

    /// <summary>
    /// Everything standing in the slots, in slot order. Empty slots are left out, so
    /// the list is what is there rather than a row of holes — read <c>slot</c> to ask
    /// about one place in particular.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="where">Whose slots to read.</param>
    [LuaFunction("list")]
    public IReadOnlyList<SlotPayload> List(ScriptOrigin origin, WhereSpec where) =>
        InventoryAccess.List(inventories.Of(where, origin));

    /// <summary>
    /// How many of something is held, added up across every slot. The code may be a
    /// <c>*</c> wildcard, so <c>game:ingot-*</c> counts a whole family in one call.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="where">Whose slots to count.</param>
    /// <param name="code">What to count, which may be a wildcard.</param>
    [LuaFunction("count")]
    public int Count(
        ScriptOrigin origin,
        WhereSpec where,
        [LuaSuggests(SuggestionSets.ASSET_CODE)] string code) =>
        InventoryAccess.Count(inventories.Of(where, origin), code);

    /// <summary>What is standing in one slot, or nil where it is empty.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="where">Whose slots to read.</param>
    /// <param name="slot">Which slot, counting from 1.</param>
    [LuaFunction("slot")]
    public SlotPayload? Slot(ScriptOrigin origin, WhereSpec where, int slot) =>
        InventoryAccess.Describe(
            InventoryAccess.At(inventories.Of(where, origin), slot, origin), slot - 1);

    /// <summary>
    /// Puts a stack into one slot, replacing whatever was there. Whatever it replaced
    /// is gone rather than moved, so read the slot first if it mattered.
    /// </summary>
    /// <param name="origin">Script line setting it.</param>
    /// <param name="where">Whose slots to write.</param>
    /// <param name="slot">Which slot, counting from 1.</param>
    /// <param name="stack">What to put there, which a bare code names one of.</param>
    [LuaFunction("setSlot")]
    public void SetSlot(ScriptOrigin origin, WhereSpec where, int slot, ItemStackSpec stack)
    {
        var target = InventoryAccess.At(inventories.Of(where, origin), slot, origin);

        target.Itemstack = stacks.Resolved(stack, origin, "stack");
        target.MarkDirty();
    }

    /// <summary>Empties one slot, and says whether anything was in it.</summary>
    /// <param name="origin">Script line clearing it.</param>
    /// <param name="where">Whose slots to write.</param>
    /// <param name="slot">Which slot, counting from 1.</param>
    [LuaFunction("clearSlot")]
    public bool ClearSlot(ScriptOrigin origin, WhereSpec where, int slot)
    {
        var target = InventoryAccess.At(inventories.Of(where, origin), slot, origin);
        if (target.Empty) return false;

        target.Itemstack = null;
        target.MarkDirty();
        return true;
    }

    /// <summary>
    /// Takes what it can of a stack out, and says how many it got. The code may be a
    /// <c>*</c> wildcard, so a charge may be paid in any ingot rather than one kind.
    /// </summary>
    /// <remarks>
    /// Getting less than was asked for is ordinary rather than a failure, so a script
    /// charging somebody for something reads what came back rather than assuming it
    /// took the lot. Taking nothing and taking half are told apart by that number and
    /// by nothing else.
    /// </remarks>
    /// <param name="origin">Script line taking it.</param>
    /// <param name="where">Whose slots to take from.</param>
    /// <param name="stack">What to take, and how much of it.</param>
    [LuaFunction("take")]
    public int Take(ScriptOrigin origin, WhereSpec where, ItemStackSpec stack) =>
        InventoryAccess.Take(inventories.Of(where, origin), stack.Code!, stack.Quantity);

    /// <summary>
    /// Puts what it can of a stack in, and says how many fitted. Merged into
    /// part-full slots before empty ones are used, the way the game does when a player
    /// picks something up.
    /// </summary>
    /// <remarks>
    /// Anything that did not fit is simply not placed. A script that must not lose it
    /// compares what it asked for against what came back and drops the difference on
    /// the floor with <c>moontweaks.world.dropItem</c>.
    /// </remarks>
    /// <param name="origin">Script line putting it in.</param>
    /// <param name="where">Whose slots to put it in.</param>
    /// <param name="stack">What to put in, and how much of it.</param>
    [LuaFunction("put")]
    public int Put(ScriptOrigin origin, WhereSpec where, ItemStackSpec stack) =>
        InventoryAccess.Put(
            inventories.Of(where, origin), stacks.Resolved(stack, origin, "stack"), world);

    /// <summary>
    /// Moves what it can of a stack from one set of slots to another, and says how
    /// many arrived. The code may be a <c>*</c> wildcard, so a levy may be collected
    /// in any ingot rather than one kind.
    /// </summary>
    /// <remarks>
    /// This is what a move should be written as. <c>take</c> and <c>put</c> can be
    /// paired to the same end and two things go wrong when they are. Whatever the
    /// destination could not hold has to be put back by hand, and a script that
    /// forgets destroys it. And the two describe a stack rather than carrying it, so
    /// a worn axe arrives sharp and a labelled crock arrives blank; this moves the
    /// stacks themselves, so what arrives is what left.
    ///
    /// Getting less than was asked for is ordinary rather than a failure. A full
    /// destination simply leaves the rest where it was — nothing is taken out and
    /// dropped — so a script reads what came back and decides what to do about the
    /// difference.
    ///
    /// The pair are still bound, for the cases that are genuinely one half of a move:
    /// charging somebody for something that then ceases to exist, or handing out
    /// something that did not exist a moment ago.
    /// </remarks>
    /// <param name="origin">Script line moving it.</param>
    /// <param name="from">Whose slots to take from.</param>
    /// <param name="to">Whose slots to put it in.</param>
    /// <param name="stack">What to move, and how much of it.</param>
    [LuaFunction("move")]
    public int Move(ScriptOrigin origin, WhereSpec from, WhereSpec to, ItemStackSpec stack) =>
        InventoryAccess.Move(
            inventories.Of(from, origin), inventories.Of(to, origin),
            stack.Code!, stack.Quantity, world);

    /// <summary>Empties every slot, and says how many held anything.</summary>
    /// <param name="origin">Script line clearing them.</param>
    /// <param name="where">Whose slots to empty.</param>
    [LuaFunction("clear")]
    public int Clear(ScriptOrigin origin, WhereSpec where) =>
        InventoryAccess.Clear(inventories.Of(where, origin));

    /// <summary>
    /// What a player is holding, or nil where their hand is empty. This is the active
    /// hotbar slot rather than an inventory of its own, which is why it is named here
    /// rather than reached through a <c>where</c>.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("held")]
    public SlotPayload? Held(ScriptOrigin origin, string player)
    {
        var slot = inventories.Held(player, origin);
        // Inventory is declared non-nullable and is null on a slot belonging to none,
        // as a DummySlot is.
        return InventoryAccess.Describe(slot, slot.Inventory?.GetSlotId(slot) ?? 0);
    }

    /// <summary>Puts something in a player's hand, replacing whatever was there.</summary>
    /// <param name="origin">Script line setting it.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="stack">What to put there, which a bare code names one of.</param>
    [LuaFunction("setHeld")]
    public void SetHeld(ScriptOrigin origin, string player, ItemStackSpec stack)
    {
        var slot = inventories.Held(player, origin);

        slot.Itemstack = stacks.Resolved(stack, origin, "stack");
        slot.MarkDirty();
    }

    /// <summary>Empties a player's hand, and says whether anything was in it.</summary>
    /// <param name="origin">Script line clearing it.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("clearHeld")]
    public bool ClearHeld(ScriptOrigin origin, string player)
    {
        var slot = inventories.Held(player, origin);
        if (slot.Empty) return false;

        slot.Itemstack = null;
        slot.MarkDirty();
        return true;
    }
}
