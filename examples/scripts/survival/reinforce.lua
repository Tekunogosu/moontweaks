-- Block reinforcement: what a player has protected, and what a script may protect.
--
-- This is the survival mod's own protection rather than the land claims
-- `moontweaks.world.testAccess` asks about. A claim covers a region and answers who
-- may build in it; a reinforcement sits on one block and answers how much work it
-- takes to get through. The two are enforced separately, so a script that cares
-- about both asks both.
--
-- Only a block the game lets a player reinforce can be reinforced here. `strengthen`
-- answers false on one that cannot rather than protecting it anyway.

local reinforce = moontweaks.reinforce

if not reinforce.available() then
  moontweaks.log.warn("no block reinforcement on this server; the reinforce script does nothing")
  return
end

-- Reading what is already there. Anyone tapping a protected block is told whose it
-- is, which is the question a shared server asks most often.
moontweaks.events.didUseBlock(function(e)
  local held = reinforce.at(e.x, e.y, e.z)
  if not held then return end

  moontweaks.players.say(e.player, ("Protected by %s: %d strength left%s.")
    :format(tostring(held.playerName), held.strength, held.locked and ", and locked" or ""))
end)

-- Protecting what a script builds. A structure put up for somebody should belong to
-- them, and this is how that is said — the player is who it belongs to rather than
-- who is standing there, so nobody has to be.
local function protect(who, x, y, z, strength)
  if not reinforce.strengthen(x, y, z, who, strength) then return false end
  return true
end

moontweaks.commands.add {
  name = "shelter",
  description = "Put up a small protected shelter where you stand",
  requiresPlayer = true,
  handler = function(e)
    local at = moontweaks.players.position(e.player)
    local x, y, z = math.floor(at.x), math.floor(at.y), math.floor(at.z)
    local put, held = 0, 0

    -- Queued rather than placed one at a time, so the whole shelter is one update to
    -- the chunk — and one step of history, which `world.undo` takes back whole.
    for dx = -1, 1 do
      for dz = -1, 1 do
        moontweaks.world.queueBlock("game:planks-oak-ud", x + dx, y + 3, z + dz)
        put = put + 1
      end
    end
    moontweaks.world.commit()

    for dx = -1, 1 do
      for dz = -1, 1 do
        if protect(e.player, x + dx, y + 3, z + dz, 100) then held = held + 1 end
      end
    end

    return ("Put %d block(s) up and protected %d of them."):format(put, held)
  end,
}

-- Taking it down again, which is the other half of anything a script builds. The
-- protection has to come off before the blocks do, or the blocks are the easy part.
moontweaks.commands.add {
  name = "unshelter",
  description = "Take back the last shelter this script built",
  requiresPlayer = true,
  handler = function(e)
    local at = moontweaks.players.position(e.player)
    local x, y, z = math.floor(at.x), math.floor(at.y), math.floor(at.z)

    for dx = -1, 1 do
      for dz = -1, 1 do
        reinforce.clear(x + dx, y + 3, z + dz)
      end
    end

    -- One call, because the shelter went up as one commit.
    local back = moontweaks.world.undo()
    return ("Cleared the protection and put %d block(s) back."):format(back)
  end,
}

-- Wearing protection down rather than clearing it outright, which is what a siege or
-- a decay rule would do.
moontweaks.commands.add {
  name = "weaken",
  description = "Take 10 strength off the block you are looking at",
  requiresPlayer = true,
  privilege = "controlserver",
  handler = function(e)
    local at = moontweaks.players.looking(e.player)
    if not at then return { error = "you are not looking at a block." } end

    if not reinforce.isReinforced(at.x, at.y, at.z) then
      return { error = "that block is not protected." }
    end

    reinforce.consume(at.x, at.y, at.z, 10)
    local left = reinforce.at(at.x, at.y, at.z)

    return left and ("%d strength left."):format(left.strength) or "It gave way."
  end,
}
