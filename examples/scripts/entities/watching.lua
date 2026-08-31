-- Being told what the world did, rather than asking it.
--
-- These seven events cover everything alive that is not a player. They are unlike the
-- player events in one way that changes how they are written.
--
-- The game raises them wherever it happens to be. Chunk generation spawns creatures
-- on its own thread, and there is one Lua interpreter for the whole server, so a
-- handler cannot be called there. It is called on the next tick of the main thread
-- instead. Two things follow:
--
--   1. What you are told is what was true when it happened, not now. By the time the
--      handler runs the creature may have moved, died, or gone with its chunk.
--   2. Anything you reach for wants checking first. `entities.isLoaded(e.id)` is the
--      guard, and it is not optional.
--
-- Worldgen fills a chunk with creatures at once, so these arrive in bursts. Decide
-- whether you care about the code before doing anything that costs.

local entities = moontweaks.entities
local events   = moontweaks.events
local players  = moontweaks.players
local world    = moontweaks.world

-- Keeping a lid on something. The guard is what makes this correct: a wolf generated
-- into a chunk that nobody stayed near is already gone by the time this runs, and
-- despawning it would fail rather than do nothing.
events.entitySpawn(function(e)
  if not e.code:find("^game:wolf") then return end

  local wolves = (world.getData("wolves") or 0) + 1
  world.setData("wolves", wolves)

  if wolves > 200 and entities.isLoaded(e.id) then
    entities.despawn(e.id)
    world.setData("wolves", wolves - 1)
  end
end)

-- Counting what dies. `byPlayer` names whoever is responsible rather than what struck
-- the blow, so an arrow names the archer.
events.entityDeath(function(e)
  if e.player then return end -- a player's own death has its own event

  local killed = world.getData("killed") or {}
  killed[e.code] = (killed[e.code] or 0) + 1
  world.setData("killed", killed)

  if e.byPlayer and e.cause then
    players.say(e.byPlayer, ("You killed %s (%s)."):format(e.name ~= "" and e.name or e.code, e.cause))
  end
end)

-- `unload` is the reason worth checking for. The creature is not gone from the world,
-- only out of reach until its chunk comes back — so this is not the place to forget
-- what you remembered about it.
events.entityDespawn(function(e)
  if e.reason == "unload" then return end

  local gone = (world.getData("gone") or 0) + 1
  world.setData("gone", gone)
end)

-- The other half of an unload. Anything stored against an entity is saved with it, so
-- it comes back too — this is where it is put back to work.
events.entityLoaded(function(e)
  if not entities.isLoaded(e.id) then return end

  if entities.getData(e.id, "tamed") then
    entities.setName(e.id, "Yours")
  end
end)

-- `id` is whoever climbed on; `mount` is what they climbed onto.
events.entityMounted(function(e)
  if not e.player or not e.mount then return end

  players.say(e.player, ("You are riding %s."):format(entities.name(e.mount)))
end)

events.entityUnmounted(function(e)
  if e.player then players.say(e.player, "Back on your feet.") end
end)

-- The pace a mount is being ridden at, once its rider changes it. The table is the
-- mount rather than the rider, so `id` is the horse. A rider's client reports the
-- gait continuously and the mod raises this only on a change, so a handler here runs
-- once per change of pace rather than once per packet.
events.mountGaitChanged(function(e)
  moontweaks.log.info(("%s is now at %s"):format(e.code, tostring(e.gait)))
end)
