-- Reading the world, writing to it, and putting it back.
--
-- Every check here needs a chunk the server is holding in memory. An unloaded one is
-- stepped over rather than answered for, so a reading taken from one proves nothing
-- about the binding that took it — which is why each check asks for a spot first and
-- fails plainly rather than reporting a pass it did not earn.
--
-- The writing checks all work on one column of air a little above the ground, and
-- each puts back what it found. A server watching its log through a run of this sees
-- a block appear and vanish above the spawn, and nothing else.

local world = moontweaks.world

-- High enough above the surface to be air, low enough to be in the same chunk.
local HEIGHT = 8

--- The spot the writing checks share, raised into the air above the ground.
---@return Spot?
local function target()
  local spot = diag.loadedSpot()
  if not spot then return end

  return { x = spot.x, y = spot.y + HEIGHT, z = spot.z }
end

--- The spot, or a failure saying why there is none. Every check below opens with it.
---@return Spot
local function airAbove()
  local at = target()
  assert(at, "the server is holding no chunk near spawn, so there is nowhere to read or write")

  return at
end

diag.later("world.isLoaded", function()
  local at = airAbove()
  assert(world.isLoaded(at.x, at.y, at.z), "the column found by surfaceAt reports itself unloaded")

  return ("%d %d %d is loaded"):format(at.x, at.y, at.z)
end)

diag.later("world.surfaceAt", function()
  local spot = diag.loadedSpot()
  assert(spot, "no loaded column")

  local surface = world.surfaceAt(spot.x, spot.z)
  assert(surface and surface > 0, "no surface height")

  return ("ground at %d %d %d"):format(spot.x, surface, spot.z)
end)

-- The world's own spawn, which is where the game sends anybody who has none of their
-- own. Read once the world is up rather than at startup: it resolves against the
-- terrain map, which is not there while the assets are still loading.
diag.later("world.spawn", function()
  local at = world.spawn()
  assert(at, "this world worked out no spawn at all")
  assert(type(at.x) == "number" and type(at.y) == "number" and type(at.z) == "number",
    "a spawn came back without three numbers in it")

  return ("the world spawn is %d %d %d")
    :format(math.floor(at.x), math.floor(at.y), math.floor(at.z))
end)

-- Moving it, and moving it back. Written as a round trip rather than as a write that
-- changes nothing, because the whole question is whether the write lands: setting the
-- spawn to where it already is would pass whether or not anything happened.
diag.later("world.setSpawn", function()
  local before = world.spawn()
  assert(before, "this world worked out no spawn to move")

  local x, y, z = math.floor(before.x), math.floor(before.y), math.floor(before.z)
  world.setSpawn(x + 8, y, z + 8)

  local moved = world.spawn()
  assert(moved, "the world worked out no spawn after one was written")

  -- Put back before asserting, so a mismatch does not leave the world spawn moved.
  world.setSpawn(x, y, z)

  assert(math.floor(moved.x) == x + 8 and math.floor(moved.z) == z + 8,
    ("wrote %d %d and read back %d %d"):format(x + 8, z + 8, math.floor(moved.x), math.floor(moved.z)))

  return ("moved the world spawn 8 blocks and put it back at %d %d %d"):format(x, y, z)
end)

diag.later("world.loadChunk", function()
  local at = airAbove()

  -- Already loaded, so this asks the server to hold on to what it is holding. A
  -- false here is the server declining, which is what it does when nobody is nearby.
  return ("asked for the column at %d %d: %s"):format(at.x, at.z, tostring(world.loadChunk(at.x, at.z)))
end)

diag.later("world.blockAt", function()
  local at = airAbove()

  local code = world.blockAt(at.x, at.y, at.z)
  assert(type(code) == "string", "expected a code, got " .. type(code))

  return ("%s at %d %d %d"):format(code, at.x, at.y, at.z)
end)

diag.later("world.setBlock", function()
  local at = airAbove()

  return diag.roundTrip(
    function() return world.blockAt(at.x, at.y, at.z) or "game:air" end,
    function(code) world.setBlock(code, at.x, at.y, at.z) end,
    "game:rock-granite")
end)

diag.later("world.exchangeBlock", function()
  local at = airAbove()

  return diag.roundTrip(
    function() return world.blockAt(at.x, at.y, at.z) or "game:air" end,
    function(code) world.exchangeBlock(code, at.x, at.y, at.z) end,
    "game:rock-granite")
end)

-- Queued writes are held until `commit`, which is what makes a large build one
-- update to each chunk rather than one per block. So the two are checked together:
-- nothing changes until the commit, and the count it hands back says how much did.
diag.later("world.queueBlock", function()
  local at = airAbove()
  local before = world.blockAt(at.x, at.y, at.z) or "game:air"

  world.queueBlock("game:rock-granite", at.x, at.y, at.z)

  local written = world.commit()
  local held = world.blockAt(at.x, at.y, at.z)
  world.setBlock(before, at.x, at.y, at.z)

  diag.used("world.commit")
  assert(written > 0, "the commit wrote nothing")
  assert(held == "game:rock-granite", "the queued block did not land: " .. tostring(held))

  return ("queued one, committed %d, put %s back"):format(written, before)
end)

-- Undo and redo walk the history each commit closes, so the pair is checked as one
-- round trip: write a block, take it back, put it back again, and leave the spot as
-- it was found. The history belongs to this script alone, so nothing another script
-- wrote is touched by any of it.
diag.later("world.undo", function()
  local at = airAbove()
  local before = world.blockAt(at.x, at.y, at.z) or "game:air"

  world.queueBlock("game:rock-granite", at.x, at.y, at.z)
  world.commit()

  local written = world.blockAt(at.x, at.y, at.z)
  local undone = world.undo()
  local after = world.blockAt(at.x, at.y, at.z)

  local redone = world.redo()
  local again = world.blockAt(at.x, at.y, at.z)
  world.setBlock(before, at.x, at.y, at.z)

  diag.used("world.redo")
  assert(written == "game:rock-granite", "the commit did not land: " .. tostring(written))
  assert(undone > 0, "undo put nothing back")
  assert(after == before, ("undo left %s where %s stood"):format(tostring(after), before))
  assert(redone > 0, "redo put nothing back")
  assert(again == "game:rock-granite", "redo left " .. tostring(again))

  return ("wrote, undid %d, redid %d, put %s back"):format(undone, redone, before)
end)

diag.later("world.breakBlock", function()
  local at = airAbove()
  local before = world.blockAt(at.x, at.y, at.z) or "game:air"

  world.setBlock("game:rock-granite", at.x, at.y, at.z)
  world.breakBlock { x = at.x, y = at.y, z = at.z, dropMultiplier = 0 }

  local after = world.blockAt(at.x, at.y, at.z)
  world.setBlock(before, at.x, at.y, at.z)

  assert(after == "game:air", "breaking left " .. tostring(after))
  return ("placed a block, broke it, left %s, put %s back"):format(after, before)
end)

-- The one write with nothing to put back: an item on the ground is a thing in the
-- world rather than a change to a block. It is dropped with nothing in it worth
-- keeping and disappears on the game's own timer.
diag.later("world.dropItem", function()
  local at = airAbove()

  world.dropItem {
    stack = { code = "game:stick", quantity = 1 },
    x = at.x + 0.5, y = at.y, z = at.z + 0.5,
  }

  return ("dropped one stick at %d %d %d, which despawns on its own"):format(at.x, at.y, at.z)
end)

diag.later("world.lightAt", function()
  local at = airAbove()
  local readings = {}

  for _, kind in ipairs({ "onlyblocklight", "onlysunlight", "maxlight", "sunbrightness" }) do
    readings[#readings + 1] = ("%s %d"):format(kind, world.lightAt(kind, at.x, at.y, at.z))
  end

  return table.concat(readings, ", ")
end)

diag.later("world.climateAt", function()
  local at = airAbove()

  local climate = world.climateAt(at.x, at.y, at.z)
  assert(type(climate) == "table", "expected a reading")

  return ("%.1fC, rainfall %.2f, fertility %.2f, forest %.2f")
    :format(climate.temperature, climate.rainfall, climate.fertility, climate.forestDensity)
end)

diag.later("world.windAt", function()
  local at = airAbove()

  local wind = world.windAt(at.x, at.y, at.z)
  assert(type(wind) == "table", "expected a vector")

  return ("%.3f %.3f %.3f"):format(wind.x, wind.y, wind.z)
end)

-- Searching a box crosses into the mod once however large the box is, which is the
-- whole reason both of these exist rather than a loop over `blockAt`.
diag.later("world.countBlocks", function()
  local at = airAbove()

  local counted = world.countBlocks {
    x = at.x - 4, y = at.y - 12, z = at.z - 4,
    toX = at.x + 4, toY = at.y + 4, toZ = at.z + 4,
  }

  assert(counted > 0, "a box around loaded ground counted nothing")
  return ("%d block(s) in a 9x17x9 box"):format(counted)
end)

diag.later("world.findBlocks", function()
  local at = airAbove()

  local found = world.findBlocks {
    x = at.x - 4, y = at.y - 12, z = at.z - 4,
    toX = at.x + 4, toY = at.y + 4, toZ = at.z + 4,
    code = "game:air",
    limit = 16,
  }

  assert(#found > 0, "no air in a box that reaches above the ground")
  assert(found[1].block and found[1].x, "a match carried neither a code nor a position")

  return ("%d air block(s), first at %d %d %d"):format(#found, found[1].x, found[1].y, found[1].z)
end)

-- Both of these are sent to whoever is near enough to notice, so on an empty server
-- they reach nobody. The check is that the call is accepted and the arguments are
-- understood; whether anything was heard or seen wants somebody standing there.
diag.later("world.playSound", function()
  local at = airAbove()

  world.playSound {
    sound = "game:sounds/block/dirt",
    x = at.x, y = at.y, z = at.z, volume = 1, range = 32,
  }

  return "sent a sound; nobody may have been near enough to hear it"
end)

diag.later("world.spawnParticles", function()
  local at = airAbove()

  world.spawnParticles {
    x = at.x, y = at.y, z = at.z,
    quantity = 8, size = 0.4, life = 1, model = "quad",
    colour = { red = 120, green = 200, blue = 255, alpha = 200 },
  }

  return "sent particles; nobody may have been near enough to see them"
end)

-- The two that need somebody standing in the world are declared here so this file
-- holds the whole module, and run by `/diag player`.
diag.onPlayer("world.testAccess", function(who)
  local at = moontweaks.players.position(who)

  local answer = world.testAccess {
    player = who,
    x = math.floor(at.x), y = math.floor(at.y), z = math.floor(at.z),
    what = "buildorbreak",
  }

  assert(answer, "no answer")
  return ("building where you stand: %s"):format(answer)
end)

diag.onPlayer("world.highlight", function(who)
  local at = moontweaks.players.position(who)
  local blocks = {}

  for step = 0, 3 do
    blocks[#blocks + 1] = {
      x = math.floor(at.x) + step,
      y = math.floor(at.y),
      z = math.floor(at.z),
    }
  end

  world.highlight {
    player = who, slot = 61, blocks = blocks,
    colour = { red = 90, green = 230, blue = 160, alpha = 150 },
  }

  return "outlined four blocks beside you; /diag cleanup takes it back"
end)
