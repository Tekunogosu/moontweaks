-- Sets of slots: what a player carries, and what a container in the world holds.
--
-- A place to look is either somebody's bag, an entity's, or a block standing at a
-- position. Only the first is reachable without a player, and even that needs one to
-- be connected — so the whole module waits for `/diag player`.
--
-- The checks work on the backpack rather than the hotbar, and put back whatever they
-- move. Anything the suite adds it takes away again in the same check, so a run
-- leaves an inventory holding exactly what it held.

local inventory = moontweaks.inventory

--- The bag the checks work in, which everybody has.
local function bag(who)
  return { player = who, which = "backpack" }
end

--- A slot in that bag a check may write into, or nothing where the bag has none.
---
--- Found by number rather than off `list`, which reports what is standing in the
--- slots and leaves the empty ones out — so a bag with room shows up there as a
--- short list, never as a hole to write into.
---
--- A bag with nothing worn in it is all bag slots, which take bags and nothing else,
--- and writing a stick into one would put an item where the game never would. Asking
--- the game to put one in first is what tells the two apart: it lands only where the
--- slot accepts it, and taking it straight back out leaves the bag as it was.
local function emptySlot(who)
  local where = bag(who)

  if inventory.put(where, { code = "game:stick", quantity = 1 }) == 0 then return end
  inventory.take(where, { code = "game:stick", quantity = 1 })

  -- Backwards, because the slots a worn bag adds come after the ones it is worn in.
  for slot = inventory.size(where), 1, -1 do
    if not inventory.slot(where, slot) then return slot end
  end
end

--- What every check needing a free slot says when the bag has none.
local NO_ROOM = "no slot in your backpack a check may write into; wear a bag with a free slot"

diag.onPlayer("inventory.size", function(who)
  local slots = inventory.size(bag(who))
  assert(slots > 0, "a backpack with no slots in it")

  return ("%d slot(s) in your backpack"):format(slots)
end)

-- Listing leaves the empty slots out, so an empty bag listing nothing is the right
-- answer rather than a failure. What is checked is that every entry it did hand back
-- holds something and names a slot that exists.
diag.onPlayer("inventory.list", function(who)
  local slots = inventory.list(bag(who))
  local size = inventory.size(bag(who))

  for _, slot in ipairs(slots) do
    assert(slot.code and slot.quantity > 0,
      ("slot %d was listed holding nothing"):format(slot.slot))
    assert(slot.slot >= 1 and slot.slot <= size,
      ("listed slot %d, which is outside the %d slot(s) there are"):format(slot.slot, size))
  end

  return ("%d of %d slot(s) hold something"):format(#slots, size)
end)

diag.onPlayer("inventory.slot", function(who)
  local first = inventory.slot(bag(who), 1)

  if not first or not first.code then return "slot 1 is empty, which is an answer" end
  return ("slot 1 holds %d %s (stacks to %d)"):format(first.quantity, first.code, first.maxStackSize)
end)

diag.onPlayer("inventory.setSlot", function(who)
  local where = emptySlot(who)
  assert(where, NO_ROOM)

  inventory.setSlot(bag(who), where, { code = "game:stick", quantity = 2 })
  local held = inventory.slot(bag(who), where)
  inventory.clearSlot(bag(who), where)

  assert(held and held.code == "game:stick", "what was put in slot " .. where .. " is not there")
  return ("put 2 sticks in slot %d and took them out again"):format(where)
end)

diag.onPlayer("inventory.clearSlot", function(who)
  local where = emptySlot(who)
  assert(where, NO_ROOM)

  inventory.setSlot(bag(who), where, { code = "game:stick", quantity = 1 })
  local cleared = inventory.clearSlot(bag(who), where)
  local after = inventory.slot(bag(who), where)

  assert(cleared, "clearing a slot with something in it answered false")
  assert(not after or not after.code, "the slot still holds something")

  return ("filled slot %d and emptied it again"):format(where)
end)

diag.onPlayer("inventory.count", function(who)
  local where = emptySlot(who)
  assert(where, NO_ROOM)

  local before = inventory.count(bag(who), "game:stick")
  inventory.setSlot(bag(who), where, { code = "game:stick", quantity = 3 })
  local held = inventory.count(bag(who), "game:stick")
  inventory.clearSlot(bag(who), where)

  assert(held == before + 3, ("counted %d, expected %d"):format(held, before + 3))
  return ("%d sticks, then %d, then %d"):format(before, held, inventory.count(bag(who), "game:stick"))
end)

-- Putting and taking are the pair a script moves things with, and each says how much
-- it managed rather than assuming it managed all of it.
diag.onPlayer("inventory.put", function(who)
  local placed = inventory.put(bag(who), { code = "game:stick", quantity = 2 })
  local taken = inventory.take(bag(who), { code = "game:stick", quantity = placed })

  diag.used("inventory.take")
  assert(placed > 0, "nothing could be put in; wear a bag with a free slot")
  assert(taken == placed, ("put %d in and got %d back out"):format(placed, taken))

  return ("put %d stick(s) in and took the same %d out"):format(placed, taken)
end)

diag.onPlayer("inventory.take", function(who)
  local placed = inventory.put(bag(who), { code = "game:stick", quantity = 1 })
  local taken = inventory.take(bag(who), { code = "game:stick", quantity = placed })

  return ("took %d of the %d put in"):format(taken, placed)
end)

-- Moving carries the stack itself rather than describing it, so this checks that
-- what arrives is what left. The two ends are the player's bag and their hotbar,
-- which is the only pair of inventories a check can be sure exists.
diag.onPlayer("inventory.move", function(who)
  local from = bag(who)
  local to = { player = who, which = "hotbar" }

  local placed = inventory.put(from, { code = "game:stick", quantity = 2 })
  assert(placed > 0, NO_ROOM)

  local moved = inventory.move(from, to, { code = "game:stick", quantity = placed })
  local back = inventory.move(to, from, { code = "game:stick", quantity = moved })

  -- Whatever never made it across is still in the bag it started in, so it goes out
  -- of there rather than being left behind by the check.
  local left = inventory.take(from, { code = "game:stick", quantity = placed })

  assert(moved > 0, "nothing moved to the hotbar; it may be full")
  assert(back == moved, ("moved %d over and got %d back"):format(moved, back))
  assert(left == placed, ("put %d in and took %d out"):format(placed, left))

  return ("moved %d stick(s) to the hotbar and %d back"):format(moved, back)
end)

-- What is in their hand, which is its own question rather than a slot number.
diag.onPlayer("inventory.held", function(who)
  local hand = inventory.held(who)

  if not hand or not hand.code then return "your hand is empty, which is an answer" end
  return ("holding %d %s"):format(hand.quantity, hand.code)
end)

diag.onPlayer("inventory.setHeld", function(who)
  local hand = inventory.held(who)
  local before = hand and hand.code and { code = hand.code, quantity = hand.quantity } or nil

  inventory.setHeld(who, { code = "game:stick", quantity = 1 })
  local held = inventory.held(who)

  if before then
    inventory.setHeld(who, before)
  else
    inventory.clearHeld(who)
  end

  assert(held and held.code == "game:stick", "what was put in your hand is not there")
  return ("put a stick in your hand and put %s back"):format(before and before.code or "nothing")
end)

diag.onPlayer("inventory.clearHeld", function(who)
  local hand = inventory.held(who)
  local before = hand and hand.code and { code = hand.code, quantity = hand.quantity } or nil

  inventory.setHeld(who, { code = "game:stick", quantity = 1 })
  local cleared = inventory.clearHeld(who)

  if before then inventory.setHeld(who, before) end

  assert(cleared, "clearing a hand with something in it answered false")
  return "filled your hand, emptied it, and put back what was there"
end)

-- Emptying a whole set of slots. Deliberately not run against anything a player
-- owns: it would take everything they are carrying and nothing here could give it
-- back. A container in the world is what `/diag container` is for.
diag.skip("inventory.clear",
  "it would empty whatever it is pointed at with nothing to put back; use /diag container")
