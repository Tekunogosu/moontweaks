-- Everything alive that is not a player.
--
-- One hen is spawned above the ground, put through every function the module binds,
-- and taken away again. It is remembered in `diag.spawned` the moment it exists, so
-- a check that fails halfway still leaves nothing standing: the harness clears the
-- list after the world checks and `/diag cleanup` clears it again.
--
-- Spawning needs the world, so all of this waits like the rest of it. Asking for an
-- entity while the assets are still loading raises rather than answering.

local entities = moontweaks.entities

--- The hen every check below works on, spawned once and shared.
local hen

local function henId()
  assert(hen, "nothing was spawned, so there is nothing to ask about")
  return hen
end

--- Where the hen stands, and the box the finding checks look in.
local function area(range)
  local spot = diag.loadedSpot()
  assert(spot, "the server is holding no chunk near spawn, so there is nowhere to stand anything")

  return { x = spot.x + 0.5, y = spot.y + 1, z = spot.z + 0.5, range = range or 8 }
end

--- The hen as the world holds it now, which every reading check needs to exist.
local function henNow()
  local it = entities.get(henId())
  assert(it, "the hen went missing between one check and the next")

  return it
end

diag.later("entities.spawn", function()
  local where = area()
  local made = entities.spawn { code = "game:chicken-hen", x = where.x, y = where.y, z = where.z }

  assert(type(made) == "table" and #made > 0, "spawning handed back nothing")
  hen = made[1]

  for _, id in ipairs(made) do diag.spawned[#diag.spawned + 1] = id end

  return ("spawned %d hen, id %s"):format(#made, tostring(hen))
end)

diag.later("entities.get", function()
  local it = entities.get(henId())
  assert(it, "the hen just spawned cannot be found by its id")

  return ("%s '%s' at %.1f %.1f %.1f, alive %s, on the ground %s")
    :format(it.code, it.name, it.x, it.y, it.z, tostring(it.alive), tostring(it.onGround))
end)

diag.later("entities.isLoaded", function()
  assert(entities.isLoaded(henId()), "the hen just spawned reports itself unloaded")
  assert(not entities.isLoaded(-1), "an id nothing holds reports itself loaded")

  return "true for the hen, false for an id nothing holds"
end)

diag.later("entities.count", function()
  local counted = entities.count(area(16))
  assert(counted > 0, "counted nothing in a box the hen is standing in")

  return ("%d entity(s) within 16"):format(counted)
end)

diag.later("entities.around", function()
  local found = entities.around(area(16))
  assert(#found > 0, "found nothing in a box the hen is standing in")

  return ("%d nearby, first is %s"):format(#found, found[1].code)
end)

-- Selecting by what a creature is rather than what it is called. The tags are read
-- off a registry of their own, separate from the one items and blocks share, so this
-- is also the check that the two are not being confused for each other.
diag.later("entities.around (by tag)", function()
  local at = diag.origin()

  -- Asserting a count would be asserting what wanders past. What is checked is that
  -- the condition is accepted and answers a number, and that a tag nothing carries
  -- narrows rather than widens.
  local everything = entities.count { x = at.x, y = at.y, z = at.z, range = 64 }
  local tagged = entities.count {
    x = at.x, y = at.y, z = at.z, range = 64,
    tags = { anyOf = { "animal", "predator" } },
  }

  assert(type(tagged) == "number", "a tag condition answered something other than a count")
  assert(tagged <= everything,
    ("a tag narrowed to %d out of %d, which is more than there were"):format(tagged, everything))

  return ("%d of %d creature(s) within 64 blocks are animals or predators"):format(tagged, everything)
end)

diag.later("entities.around (refuses an unknown tag)", function()
  local at = diag.origin()
  local ok, why = pcall(entities.count, {
    x = at.x, y = at.y, z = at.z, range = 8,
    tags = { "moontweaks:no-such-creature-tag" },
  })

  assert(not ok, "a creature tag nothing declared was accepted")
  assert(tostring(why):find("no%-such%-creature%-tag"),
    ("the refusal did not name the tag: %s"):format(tostring(why)))

  return "refused an unknown creature tag and named it"
end)

diag.later("entities.nearest", function()
  local it = entities.nearest(area(16))
  assert(it, "nothing is nearest in a box the hen is standing in")

  return ("%s at %.1f %.1f %.1f"):format(it.code, it.x, it.y, it.z)
end)

diag.later("entities.name", function()
  local name = entities.name(henId())
  assert(type(name) == "string", "expected a name")

  return ("'%s'"):format(name)
end)

-- Only something the game gave a name tag to can be renamed, and a hen is not one of
-- them. So the refusal is the expected answer here, and the check reads it as such
-- rather than as a failure: what is being proved is that the binding reaches the
-- game and comes back with the game's own answer.
diag.later("entities.setName", function()
  local ok, why = pcall(entities.setName, henId(), "Diagnostic Hen")

  if ok then
    local name = entities.name(henId())
    assert(name == "Diagnostic Hen", "the name did not stick: " .. tostring(name))
    return "renamed the hen, which means this server's hens carry name tags"
  end

  assert(tostring(why):find("cannot carry a name"),
    "refused for a reason this check does not know: " .. tostring(why))

  return "refused, because a hen carries no name tag -- which is the game's answer, not a fault"
end)

diag.later("entities.health", function()
  local health = entities.health(henId())
  assert(type(health) == "number", "expected a number")

  return ("%.1f health"):format(health)
end)

diag.later("entities.maxHealth", function()
  local most = entities.maxHealth(henId())
  assert(most > 0, "a living hen has no maximum health")

  return ("%.1f at most"):format(most)
end)

diag.later("entities.setHealth", function()
  return diag.roundTrip(
    function() return entities.health(henId()) end,
    function(value) entities.setHealth(henId(), value) end,
    entities.maxHealth(henId()))
end)

diag.later("entities.damage", function()
  local before = entities.health(henId())
  local landed = entities.damage(henId(), 0.5)
  local after = entities.health(henId())

  entities.setHealth(henId(), before)

  return ("%.1f -> %.1f (%s), healed back to %.1f")
    :format(before, after, tostring(landed), entities.health(henId()))
end)

diag.later("entities.ignite", function()
  entities.ignite(henId())
  return ("on fire: %s"):format(tostring(henNow().onFire))
end)

diag.later("entities.extinguish", function()
  entities.extinguish(henId())
  return ("on fire: %s"):format(tostring(henNow().onFire))
end)

diag.later("entities.teleport", function()
  local before = henNow()
  entities.teleport(henId(), before.x, before.y + 2, before.z)

  return ("%.1f -> %.1f in y"):format(before.y, henNow().y)
end)

diag.later("entities.give", function()
  local taken = entities.give(henId(), { code = "game:grain-flax", quantity = 1 })
  return ("a hen took a grain: %s"):format(tostring(taken))
end)

diag.later("entities.setData", function()
  entities.setData(henId(), "diagnostics", { checked = true })
  return "wrote a table against the hen"
end)

diag.later("entities.getData", function()
  local held = entities.getData(henId(), "diagnostics")

  assert(type(held) == "table", "expected a table back")
  assert(held.checked == true, "what was written did not come back")

  return "read back what was written"
end)

-- A stat is a number the game builds from a base and whatever is adding to it, so a
-- named contribution is set and then cleared by the same name. Reading it back in
-- between is what shows the contribution landed.
diag.later("entities.stat", function()
  local walk = entities.stat(henId(), "walkspeed")
  assert(type(walk) == "number", "expected a number")

  return ("walkspeed %.3f"):format(walk)
end)

diag.later("entities.setStat", function()
  local before = entities.stat(henId(), "walkspeed")

  entities.setStat {
    entity = henId(), stat = "walkspeed",
    name = "moontweaks-diagnostic", value = 0.5, persistent = false,
  }

  local held = entities.stat(henId(), "walkspeed")
  entities.clearStat(henId(), "walkspeed", "moontweaks-diagnostic")

  assert(held ~= before, ("the contribution did not land: %.3f both times"):format(before))
  return ("%.3f -> %.3f -> %.3f"):format(before, held, entities.stat(henId(), "walkspeed"))
end)

diag.later("entities.clearStat", function()
  entities.clearStat(henId(), "walkspeed", "moontweaks-diagnostic")
  return ("cleared, back to %.3f"):format(entities.stat(henId(), "walkspeed"))
end)

-- Last, because both of them end the hen. Killing leaves a body that the game clears
-- up on its own; despawning takes it away outright, which is what the harness does
-- to anything left in `diag.spawned`.
diag.later("entities.kill", function()
  entities.kill(henId())
  return ("alive: %s"):format(tostring(henNow().alive))
end)

diag.later("entities.despawn", function()
  entities.despawn(henId())
  local gone = entities.get(henId())

  assert(not gone or not gone.alive, "the hen is still standing there")
  return "taken out of the world"
end)
