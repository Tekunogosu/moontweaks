-- Every event the mod binds, watched.
--
-- Registering is checked here and now: a handler this server refuses to take fails
-- on this line. Whether it ever fires is a different question, and one no script can
-- answer on its own — somebody has to break a block, or die, or log out. So each is
-- watched, the first firing is remembered with a line describing it, and the report
-- says which are still quiet.
--
-- That list is the point of the file. A quiet event is not a failure: some fire once
-- at startup, some need a player, and two of them only ever fire on a save game's
-- very first run. The step-by-step guide beside this suite says which is which and
-- what to do to raise the rest.

local events = moontweaks.events

--- What a block event says: which block, where, and who did it.
local function block(e)
  return ("%s at %d %d %d by %s"):format(e.block, e.x, e.y, e.z, tostring(e.playerName))
end

--- What an entity event says.
local function entity(e)
  return ("%s (%s) at %.0f %.0f %.0f"):format(e.code, tostring(e.name), e.x, e.y, e.z)
end

--- What a player event says.
local function player(e)
  return tostring(e.playerName)
end

--- What an event carrying nothing says.
local function bare()
  return "fired"
end

-- Somebody has to do these: right-click a block, mine one, place one, scroll the
-- hotbar.
diag.watch("didUseBlock", events.didUseBlock, block)
diag.watch("didBreakBlock", events.didBreakBlock, block)
diag.watch("didPlaceBlock", events.didPlaceBlock, function(e)
  return ("%s over %s at %d %d %d"):format(e.block, tostring(e.replaced), e.x, e.y, e.z)
end)
diag.watch("playerChangeSlot", events.playerChangeSlot, player)

-- These follow whoever is walking about, as the world is read in and let go again.
diag.watch("chunkColumnLoaded", events.chunkColumnLoaded, function(e)
  return ("column %d %d"):format(e.chunkX, e.chunkZ)
end)
diag.watch("chunkUnloaded", events.chunkUnloaded, function(e)
  return ("chunk %d %d %d"):format(e.chunkX, e.chunkY, e.chunkZ)
end)

-- The suite raises the first three itself when it spawns and clears its hen, so
-- these should be filled in by the time the world checks have finished.
diag.watch("entitySpawn", events.entitySpawn, entity)
diag.watch("entityLoaded", events.entityLoaded, entity)
diag.watch("entityDeath", events.entityDeath, function(e)
  return ("%s died of %s"):format(e.code, tostring(e.cause))
end)
diag.watch("entityDespawn", events.entityDespawn, function(e)
  return ("%s left because %s"):format(e.code, tostring(e.reason))
end)
diag.watch("entityMounted", events.entityMounted, function(e)
  return ("%s got on %s"):format(e.code, tostring(e.mount))
end)
diag.watch("entityUnmounted", events.entityUnmounted, function(e)
  return ("%s got off %s"):format(e.code, tostring(e.mount))
end)

-- The whole of a player's time on the server, from connecting to disconnecting.
-- `playerCreate` fires once ever for each person, so a returning player never
-- raises it.
diag.watch("playerJoin", events.playerJoin, player)
diag.watch("playerCreate", events.playerCreate, player)
diag.watch("playerNowPlaying", events.playerNowPlaying, player)
diag.watch("playerReady", events.playerReady, player)
diag.watch("playerDeath", events.playerDeath, player)
diag.watch("playerRespawn", events.playerRespawn, player)
diag.watch("playerSwitchGameMode", events.playerSwitchGameMode, player)
diag.watch("playerLeave", events.playerLeave, player)
diag.watch("playerDisconnect", events.playerDisconnect, player)

-- The server's own life. Two of these fire during the startup that runs this file,
-- and one of them only on the run that makes the save game.
diag.watch("saveGameLoaded", events.saveGameLoaded, bare)
diag.watch("saveGameCreated", events.saveGameCreated, bare)
diag.watch("worldgenStartup", events.worldgenStartup, bare)
diag.watch("gameWorldSave", events.gameWorldSave, bare)
diag.watch("serverResume", events.serverResume, bare)
