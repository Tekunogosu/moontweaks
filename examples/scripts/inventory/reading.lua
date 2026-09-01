-- Looking at what is in a set of slots.
--
-- Every function in this module takes the same first argument: a table naming where
-- the slots are. There are three ways to name them and exactly one is written:
--
--   { player = e.player, which = "backpack" }   what somebody carries
--   { x = 10, y = 100, z = 20 }                 whatever container stands there
--   { entity = id }                             what a creature is carrying
--
-- One shape rather than three families of function, because everything a script does
-- to a chest it also does to a backpack. Only where the slots are differs.
--
-- Slots are numbered from 1, as everything in Lua is.

local commands  = moontweaks.commands
local events    = moontweaks.events
local inventory = moontweaks.inventory
local players   = moontweaks.players

-- `which` picks one of a player's several inventories. Left out it is their bags,
-- which is what "their inventory" usually means.
--
--   hotbar        the quick slots along the bottom
--   backpack      what their bags hold
--   character     what they are wearing
--   craftinggrid  the grid they have open
--   mouse         what they have picked up and not put down
--   creative      the creative inventory, in creative mode
commands.add {
  name = "pockets",
  description = "List what you are carrying",
  requiresPlayer = true,
  args = { { name = "which", type = "word", optional = true,
             values = { "hotbar", "backpack", "character" } } },
  handler = function(e)
    local where = { player = e.player, which = e.args.which or "backpack" }

    -- `list` skips empty slots, so what comes back is what is there rather than a
    -- row of holes. Ask `slot` when one place in particular matters.
    local held = inventory.list(where)
    if #held == 0 then
      return ("Your %s is empty (%d slots)."):format(e.args.which or "backpack", inventory.size(where))
    end

    local lines = {}
    for _, slot in ipairs(held) do
      lines[#lines + 1] = ("  %2d  %s x%d"):format(slot.slot, slot.name, slot.quantity)
    end

    return ("%d of %d slot(s) in use:\n%s")
      :format(#held, inventory.size(where), table.concat(lines, "\n"))
  end,
}

-- Counting adds up across every slot, so it answers "do they have enough" without
-- the script walking the list itself. The code takes a wildcard, so one call counts a
-- whole family — any iron ingot rather than one particular kind.
commands.add {
  name = "haveyou",
  description = "Say how many of something you are carrying",
  requiresPlayer = true,
  args = { { name = "code", type = "word" } },
  handler = function(e)
    local bags = inventory.count({ player = e.player, which = "backpack" }, e.args.code)
    local belt = inventory.count({ player = e.player, which = "hotbar" }, e.args.code)

    return ("%d in your bags, %d on your belt."):format(bags, belt)
  end,
}

-- What somebody is holding. This is the active hotbar slot rather than an inventory
-- of its own, which is why it is named here rather than reached through a `where`.
events.didUseBlock(function(e)
  if e.block ~= "game:sign-ground-north" then return end

  local hand = inventory.held(e.player)
  if not hand then
    players.say(e.player, "You are holding nothing.")
    return
  end

  players.say(e.player, ("You are holding %s x%d, in slot %d of your belt.")
    :format(hand.name, hand.quantity, hand.slot))
end)

-- One slot in particular. Asking for a slot outside the inventory is refused rather
-- than answered with nothing, because asking for slot 40 of a nine-slot bag is a
-- mistake rather than a discovery that it is empty.
events.didBreakBlock(function(e)
  if e.block ~= "game:sign-ground-north" then return end

  local first = inventory.slot({ player = e.player, which = "hotbar" }, 1)
  players.say(e.player, "First belt slot: " .. (first and first.name or "empty"))
end)

-- ## What one stack has left
--
-- Durability sits on the stack rather than on the kind, so two axes in one bag answer
-- differently. `durability` is what this one has left and `maxDurability` is what its
-- kind has when new — which is the figure `moontweaks.items.set` writes.
--
-- Both are nil for anything that does not wear out, which is most things, so a script
-- reading them checks before dividing by one.
commands.add {
  name = "worn",
  description = "List what in your bags is wearing out",
  requiresPlayer = true,
  handler = function(e)
    local worn = {}

    for _, slot in ipairs(inventory.list { player = e.player, which = "backpack" }) do
      if slot.durability and slot.maxDurability then
        local left = slot.durability / slot.maxDurability

        if left < 0.25 then
          worn[#worn + 1] = ("%s (%d%%)"):format(slot.name, math.floor(left * 100))
        end
      end
    end

    if #worn == 0 then
      return "Nothing in your bags is close to breaking."
    end

    return "Nearly worn out: " .. table.concat(worn, ", ")
  end,
}
