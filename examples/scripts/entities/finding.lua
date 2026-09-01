-- Finding what is around, and what an identifier is worth.
--
-- An entity is named by the identifier a search hands back, in the same way a player
-- is named by theirs. The two are not alike in one way worth knowing: a player's
-- identifier outlives everything, where an entity's is good only while the entity is
-- loaded. Ask `isLoaded` before reaching for one a script remembered rather than one
-- it has just found.

local entities = moontweaks.entities
local events   = moontweaks.events
local players  = moontweaks.players
local world    = moontweaks.world

local SURVEY = "game:sign-ground-north"

-- A search is a box around a point, measured outwards: `range` reaches that many
-- blocks in every horizontal direction, and `height` the same up and down. Height is
-- separate because what is usually wanted is a wide, shallow slice — everything on
-- this floor rather than everything in this cube.
events.didUseBlock(function(e)
  if e.block ~= SURVEY then return end

  local nearby = entities.around { x = e.x, y = e.y, z = e.z, range = 20, height = 6 }
  players.say(e.player, ("%d thing(s) within twenty blocks."):format(#nearby))

  -- The list comes back nearest first, so taking the first few is taking the closest.
  for i = 1, math.min(#nearby, 5) do
    local it = nearby[i]
    players.say(e.player, ("  %s (%s) at %d %d %d, %s")
      :format(it.name, it.code, it.x, it.y, it.z,
              it.health and ("%.0f/%.0f hp"):format(it.health, it.maxHealth) or "no health"))
  end
end)

-- `code` narrows the search and takes a wildcard, so one call counts a whole family.
-- `count` is the cheaper way to ask when only the number matters: nothing gets
-- described that is only going to be counted.
events.didBreakBlock(function(e)
  if e.block ~= SURVEY then return end

  local wolves = entities.count { x = e.x, y = e.y, z = e.z, range = 30, code = "game:wolf-*" }
  if wolves > 0 then
    players.warn(e.player, ("%d wolf/wolves within thirty blocks."):format(wolves))
  end
end)

-- Players are skipped by default, since a script wanting a person has a better module
-- to ask. Asking for them anyway is how a script finds who is standing near something
-- that is not a player — and the entity it finds carries their player identifier, so
-- `moontweaks.players` picks up where this leaves off.
events.didUseBlock(function(e)
  if e.block ~= "game:chest-east" then return end

  local company = entities.around {
    x = e.x, y = e.y, z = e.z, range = 12, skipPlayers = false,
  }

  for _, it in ipairs(company) do
    if it.player and it.player ~= e.player then
      players.say(it.player, players.name(e.player) .. " is at the chest near you.")
    end
  end
end)

-- Dead things are skipped by default too. Turning that off is how a script finds what
-- it just killed, or the stacks lying on the floor — an item on the ground is an
-- entity like any other, and carries what it holds under `stack`.
events.didBreakBlock(function(e)
  local litter = entities.around {
    x = e.x, y = e.y, z = e.z, range = 4, aliveOnly = false,
  }

  local loose = 0
  for _, it in ipairs(litter) do
    if it.stack then loose = loose + it.stack.quantity end
  end

  if loose > 0 then
    moontweaks.log.info(("%d item(s) lying near %d %d %d"):format(loose, e.x, e.y, e.z))
  end
end)

-- An identifier a script kept is worth checking before it is used. This one is
-- remembered against the player, so it survives a restart while the entity it names
-- almost certainly does not.
moontweaks.commands.add {
  name = "myquarry",
  description = "Say whether the thing you last marked is still about",
  requiresPlayer = true,
  handler = function(e)
    local marked = players.getWorldData(e.player, "quarry")
    if not marked then return { error = "you have not marked anything." } end

    if not entities.isLoaded(marked) then
      return "Whatever you marked is no longer loaded. It may still exist; it is out of reach."
    end

    -- Loaded a moment ago and gone now is a real answer rather than an impossible
    -- one: anything can die or despawn between two calls.
    local it = entities.get(marked)
    if not it then return "Whatever you marked is no longer there." end

    local at = players.position(e.player)
    local away = math.sqrt((it.x - at.x) ^ 2 + (it.z - at.z) ^ 2)

    return ("%s is %.0f blocks away, %s."):format(it.name, away, it.alive and "alive" or "dead")
  end,
}

-- Marking one: whatever the player is pointing at.
events.didUseBlock(function(e)
  if e.block ~= SURVEY then return end

  local looking = players.lookingAtEntity(e.player)
  if not looking then return end

  players.setWorldData(e.player, "quarry", looking)
  players.say(e.player, "Marked " .. entities.name(looking) .. ".")
end)

-- ## Finding by what something is
--
-- A code is a name, and a mod's wolf is not called `game:wolf-anything`. Creatures
-- carry tags the same way items do, so one search written against a tag reaches a
-- modded creature as readily as a vanilla one.
--
-- These are a separate set from the item and block tags: the game keeps the two in
-- registries of their own, and `library/codes.lua` lists the creature ones under
-- `EntityTag`. A name from the wrong set is refused rather than matching nothing.
moontweaks.commands.add {
  name = "whatsabout",
  description = "Count the creatures around you by what they are",
  privilege = "controlserver",
  requiresPlayer = true,
  handler = function(e)
    local at = players.position(e.player)

    local hunters = entities.count {
      x = at.x, y = at.y, z = at.z,
      range = 32,
      tags = { "predator" },
    }

    -- The same condition grammar items use, so `anyOf`, `allOf` and `noneOf` all read
    -- the same here as they do in `items.set`.
    local rest = entities.count {
      x = at.x, y = at.y, z = at.z,
      range = 32,
      tags = { allOf = { "animal" }, noneOf = { "predator" } },
      skipPlayers = true,
    }

    return ("%d predator(s) and %d other animal(s) within 32 blocks."):format(hunters, rest)
  end,
}
