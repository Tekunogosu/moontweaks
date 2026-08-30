-- Putting things into the world and taking them out again.
--
-- Three ways out, and they are not the same:
--
--   kill      it dies as anything killing it would, and its drops land.
--   despawn   it is simply gone. Nothing drops and nothing notices.
--   nothing   it wanders off and the world forgets it in its own time.
--
-- `spawn` is one of the few things acting on a live world that still refuses a bad
-- code: the entity type is looked up as the call is made, so a code the server does
-- not have names itself rather than quietly spawning nothing.

local commands = moontweaks.commands
local entities = moontweaks.entities
local events   = moontweaks.events
local players  = moontweaks.players
local world    = moontweaks.world

-- A herd rather than a heap. `spread` scatters them, and `herd` marks them as one
-- group so they move together afterwards rather than wandering apart the moment they
-- land. Identifiers come back one per thing, in the order they were made.
commands.add {
  name = "flock",
  description = "Put a flock of hens where you are standing",
  privilege = "controlserver",
  requiresPlayer = true,
  args = { { name = "howmany", type = "int", optional = true } },
  handler = function(e)
    local at = players.position(e.player)
    local wanted = math.min(e.args.howmany or 4, 20)

    local ids = entities.spawn {
      code = "game:chicken-hen",
      x = at.x, y = at.y, z = at.z,
      quantity = wanted,
      spread = 3,
    }

    -- Remembering them means they can be cleared up later without a search finding
    -- somebody else's animals too.
    players.setWorldData(e.player, "flock", ids)
    return ("%d hen(s) put down."):format(#ids)
  end,
}

-- Clearing up after itself. Every identifier is checked before it is used, because
-- most of them will have stopped naming anything: the birds may have been eaten, or
-- their chunk may simply have gone away.
commands.add {
  name = "unflock",
  description = "Remove the flock you last put down",
  privilege = "controlserver",
  requiresPlayer = true,
  handler = function(e)
    local ids = players.getWorldData(e.player, "flock")
    if not ids then return { error = "you have not put a flock down." } end

    local removed, gone = 0, 0
    for _, id in ipairs(ids) do
      if entities.isLoaded(id) then
        entities.despawn(id)
        removed = removed + 1
      else
        gone = gone + 1
      end
    end

    players.setWorldData(e.player, "flock", nil)
    return ("Removed %d. %d were already out of reach."):format(removed, gone)
  end,
}

-- Spawning on the ground rather than at head height. `surfaceAt` reads the height of
-- a column from the map the world already keeps, so this costs one call rather than
-- looking down a block at a time.
commands.add {
  name = "wolfat",
  description = "Put a wolf on the surface of a column",
  privilege = "controlserver",
  args = {
    { name = "x", type = "int" },
    { name = "z", type = "int" },
  },
  handler = function(e)
    local ground = world.surfaceAt(e.args.x, e.args.z)
    if not ground then return { error = "that column is not loaded." } end

    local ids = entities.spawn {
      code = "game:wolf-eurasian-adult-male",
      x = e.args.x + 0.5, y = ground, z = e.args.z + 0.5,
    }

    return ("Wolf %d is at %d %d %d."):format(ids[1], e.args.x, ground, e.args.z)
  end,
}

-- The difference between the two ways out, where it shows. Breaking a hay block near
-- a hen kills it, so its drops land; breaking one near a chick simply removes it.
events.didBreakBlock(function(e)
  if e.block ~= "game:drygrass" then return end

  for _, it in ipairs(entities.around { x = e.x, y = e.y, z = e.z, range = 3 }) do
    if it.code == "game:chicken-hen" then
      entities.kill(it.id)
    elseif it.code == "game:chicken-henpoult" then
      entities.despawn(it.id)
    end
  end
end)
