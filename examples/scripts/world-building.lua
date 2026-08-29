-- Building with blocks, and what it costs.
--
-- These act on a loaded world, so they belong in a handler rather than in a script's
-- body: when a script is read the recipes exist but the world does not.
--
-- Two ways to write a block. `setBlock` takes effect at once, which means the chunk
-- it touched is relit and re-sent before the next call runs. `queueBlock` stages the
-- write and `commit` performs the lot, paying that cost once per chunk however many
-- blocks landed in it. For anything past a handful the second is the one to use.

local events  = moontweaks.events
local world   = moontweaks.world
local players = moontweaks.players

-- The block that sets all this off. A code in a handler is only a string, so an
-- unknown one is not refused the way `items.set` would refuse it — it simply never
-- matches, and the handler quietly does nothing. Check a code against
-- `library/codes.lua`, or log what you broke to find out what to write.
local TRIGGER = "game:genericwood"

-- Break the trigger block to raise a hollow tower where it stood.
events.didBreakBlock(function(e)
  if e.block ~= TRIGGER then return end

  local started = 0
  local radius, height = 3, 12

  for y = 0, height - 1 do
    for x = -radius, radius do
      for z = -radius, radius do
        local edge = math.abs(x) == radius or math.abs(z) == radius
        -- Hollow: only the shell, and a doorway at the bottom of one side.
        local doorway = y < 2 and z == -radius and math.abs(x) < 2
        if edge and not doorway then
          world.queueBlock("game:rock-granite", e.x + x, e.y + y, e.z + z)
          started = started + 1
        end
      end
    end
  end

  -- Nothing above has touched the world yet; this is where it all lands.
  local written = world.commit()
  players.say(e.player, ("Raised a tower: %d blocks in %d writes."):format(started, written))

  -- Leave something behind for the trouble. Naming the player as the owner keeps
  -- them from collecting it for a second, so it is seen landing rather than
  -- vanishing into their hands the instant it appears.
  world.dropItem {
    stack = { code = "game:rock-granite", quantity = 16 },
    x = e.x, y = e.y + height, z = e.z,
    owner = e.player,
  }
end)

-- Reading is cheap and needs no commit: this walks down from where a player is
-- standing until it finds something solid, and says what it landed on.
events.didUseBlock(function(e)
  if e.block ~= TRIGGER then return end

  local at = players.position(e.player)
  local x, z = math.floor(at.x), math.floor(at.z)

  for y = math.floor(at.y), 1, -1 do
    local below = world.blockAt(x, y - 1, z)
    if below and below ~= "game:air" then
      players.say(e.player, ("Standing on %s, %d blocks down."):format(below, math.floor(at.y) - y + 1))
      return
    end
  end
end)
