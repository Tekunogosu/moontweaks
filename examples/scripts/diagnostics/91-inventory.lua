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

--- A slot in that bag with nothing in it, so nothing is displaced by a check.
local function emptySlot(who)
  for _, slot in ipairs(inventory.list(bag(who))) do
    if not slot.code or slot.quantity == 0 then return slot.slot end
  end
end

diag.onPlayer("inventory.size", function(who)
  local slots = inventory.size(bag(who))
  assert(slots > 0, "a backpack with no slots in it")

  return ("%d slot(s) in your backpack"):format(slots)
end)

diag.onPlayer("inventory.list", function(who)
  local slots = inventory.list(bag(who))
  assert(#slots > 0, "listed nothing")

  local held = 0
  for _, slot in ipairs(slots) do
    if slot.code and slot.quantity > 0 then held = held + 1 end
  end

  return ("%d slot(s), %d of them holding something"):format(#slots, held)
end)

diag.onPlayer("inventory.slot", function(who)
  local first = inventory.slot(bag(who), 1)

  if not first or not first.code then return "slot 1 is empty, which is an answer" end
  return ("slot 1 holds %d %s (stacks to %d)"):format(first.quantity, first.code, first.maxStackSize)
end)

diag.onPlayer("inventory.setSlot", function(who)
  local where = emptySlot(who)
  assert(where, "no empty slot in your backpack to work in; free one and try again")

  inventory.setSlot(bag(who), where, { code = "game:stick", quantity = 2 })
  local held = inventory.slot(bag(who), where)
  inventory.clearSlot(bag(who), where)

  assert(held and held.code == "game:stick", "what was put in slot " .. where .. " is not there")
  return ("put 2 sticks in slot %d and took them out again"):format(where)
end)

diag.onPlayer("inventory.clearSlot", function(who)
  local where = emptySlot(who)
  assert(where, "no empty slot in your backpack to work in")

  inventory.setSlot(bag(who), where, { code = "game:stick", quantity = 1 })
  local cleared = inventory.clearSlot(bag(who), where)
  local after = inventory.slot(bag(who), where)

  assert(cleared, "clearing a slot with something in it answered false")
  assert(not after or not after.code, "the slot still holds something")

  return ("filled slot %d and emptied it again"):format(where)
end)

diag.onPlayer("inventory.count", function(who)
  local where = emptySlot(who)
  assert(where, "no empty slot in your backpack to work in")

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
  assert(placed > 0, "nothing could be put in; is your backpack full?")
  assert(taken == placed, ("put %d in and got %d back out"):format(placed, taken))

  return ("put %d stick(s) in and took the same %d out"):format(placed, taken)
end)

diag.onPlayer("inventory.take", function(who)
  local placed = inventory.put(bag(who), { code = "game:stick", quantity = 1 })
  local taken = inventory.take(bag(who), { code = "game:stick", quantity = placed })

  return ("took %d of the %d put in"):format(taken, placed)
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
