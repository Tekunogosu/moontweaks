-- Events run while the server is playing, rather than while it is loading. A script
-- registers a handler once at startup and the handler is called for as long as the
-- server runs, so the interpreter that read this file stays alive behind it.
--
-- A handler that throws is logged with the line that registered it and is not called
-- again: these run inside the game's own dispatch, where an error would otherwise
-- take down whatever raised it.

local events  = moontweaks.events
local players = moontweaks.players

-- The goal: sleeping in a bed becomes where you wake up. Vintage Story does not do
-- this on its own, so the last bed a player used becomes their spawn.
events.didUseBlock(function(e)
  if e.block and e.block:find("bed") then
    players.setSpawn(e.player, e.x, e.y, e.z)
    players.say(e.player, "Your spawn is now this bed.")
  end
end)

-- Every handler is given one table describing what happened. A block event carries
-- who did it, what they did it to, and where.
--
-- Logging the code is the quickest way to learn what to write: a code compared in a
-- handler is only a string, so one the game does not have never matches and the
-- handler silently does nothing.
events.didBreakBlock(function(e)
  moontweaks.log.info(("%s broke %s at %d %d %d")
    :format(e.playerName, e.block or "nothing", e.x, e.y, e.z))
end)

-- Player events carry who it happened to.
events.playerJoin(function(e)
  players.say(e.player, "Welcome back, " .. e.playerName .. ".")
end)

-- What a script remembers about a player is saved with them, so it is still there
-- after a restart. Any value can be stored, a table included.
events.playerDeath(function(e)
  local deaths = (players.getData(e.player, "deaths") or 0) + 1
  players.setData(e.player, "deaths", deaths)

  local at = players.position(e.player)
  players.setData(e.player, "diedAt", { x = at.x, y = at.y, z = at.z })

  moontweaks.log.info(("%s has died %d time(s)"):format(e.playerName, deaths))
end)

-- Reading and changing a player: where they are, how they are, and how they play.
events.playerRespawn(function(e)
  local where = players.getData(e.player, "diedAt")
  if where then
    players.say(e.player, ("You died at %d %d %d."):format(where.x, where.y, where.z))
  end

  -- Respawning hungry is unkind, so top them up and heal them fully.
  players.setSatiety(e.player, players.maxSatiety(e.player))
  players.setHealth(e.player, players.maxHealth(e.player))

  moontweaks.log.info(("%s respawned in %s mode"):format(e.playerName, players.gameMode(e.player)))
end)
