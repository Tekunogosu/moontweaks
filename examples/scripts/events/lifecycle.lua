-- A join and a leave each raise more than one event, and which one a handler wants
-- depends on what it is for. This script listens to all of them and says what
-- separates them.

local events  = moontweaks.events
local players = moontweaks.players

-- JOINING, in the order the events arrive.

-- `playerCreate` fires once in a player's life on this world, before they are
-- welcomed, and never again however often they come back. Anything given out once
-- belongs here rather than in `playerJoin`, which would hand it over every time.
events.playerCreate(function(e)
  players.setWorldData(e.player, "firstSeen", { name = e.playerName })
  players.say(e.player, "Welcome to the world for the first time, " .. e.playerName .. ".")
end)

-- `playerJoin` fires on every join, this one included.
events.playerJoin(function(e)
  local visits = (players.getWorldData(e.player, "visits") or 0) + 1
  players.setWorldData(e.player, "visits", visits)
end)

-- `playerNowPlaying` fires once they are actually in the world and the server has
-- welcomed them.
events.playerNowPlaying(function(e)
  moontweaks.log.info(e.playerName .. " is now playing")
end)

-- `playerReady` is last: the client has reported that it finished joining, so this
-- is the safe place to speak to someone and be sure they see it.
events.playerReady(function(e)
  local visits = players.getWorldData(e.player, "visits") or 1
  players.say(e.player, ("Visit number %d. You are in %s mode.")
    :format(visits, players.gameMode(e.player)))
end)

-- LEAVING, where the difference matters more.

-- `playerLeave` fires only for someone who quit deliberately. A player who was
-- kicked, or whose connection dropped, never reaches this.
events.playerLeave(function(e)
  moontweaks.log.info(e.playerName .. " quit")
end)

-- `playerDisconnect` fires however they went — a quit, a kick, a lost connection —
-- so anything that must happen exactly once per departure goes here, not above.
events.playerDisconnect(function(e)
  local at = players.position(e.player)
  players.setWorldData(e.player, "leftAt", { x = at.x, y = at.y, z = at.z })
end)

-- `playerSwitchGameMode` fires after the change, so asking gives the new mode
-- rather than the old one.
events.playerSwitchGameMode(function(e)
  moontweaks.log.info(("%s switched to %s mode"):format(e.playerName, players.gameMode(e.player)))
end)
