-- Waking up whole: heal a player and fill them up when they respawn.
--
-- Dying in Vintage Story leaves you at full health but empty, which means the first
-- thing a fresh respawn does is go looking for food. This gives them back both, and
-- tells them where they died so they can go and fetch their things.

local events  = moontweaks.events
local players = moontweaks.players

-- Remember where they fell, before anything moves them.
events.playerDeath(function(e)
  local at = players.position(e.player)
  players.setData(e.player, "diedAt", { x = at.x, y = at.y, z = at.z })
end)

events.playerRespawn(function(e)
  -- Ask for the maximum rather than assuming one: a mod, a trait or a temporal
  -- effect may have moved it, and reading it back is always right.
  players.setHealth(e.player, players.maxHealth(e.player))
  players.setSatiety(e.player, players.maxSatiety(e.player))

  -- Waking up exhausted as well as dead is unkind.
  players.setTiredness(e.player, 0)

  players.say(e.player, "You wake up whole.")

  local where = players.getData(e.player, "diedAt")
  if where then
    players.say(e.player, ("Your things are near %d, %d, %d.")
      :format(where.x, where.y, where.z))
  end
end)

-- What was remembered is saved with the player, so it survives a restart: a player
-- who dies, leaves and comes back days later is still told where to look.
events.playerJoin(function(e)
  local where = players.getData(e.player, "diedAt")
  if where then
    players.say(e.player, ("You last died at %d, %d, %d.")
      :format(where.x, where.y, where.z))
  end
end)
