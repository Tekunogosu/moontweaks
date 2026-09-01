-- Waking up whole: heal a player and fill them up when they respawn.
--
-- Dying in Vintage Story leaves you at full health but empty, which means the first
-- thing a fresh respawn does is go looking for food. This gives them back both, and
-- tells them where they died so they can go and fetch their things.

local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players

-- Remember where they fell, before anything moves them.
events.playerDeath(function(e)
  local at = players.position(e.player)
  players.setWorldData(e.player, "diedAt", { x = at.x, y = at.y, z = at.z })
end)

events.playerRespawn(function(e)
  -- Ask for the maximum rather than assuming one: a mod, a trait or a temporal
  -- effect may have moved it, and reading it back is always right.
  players.setHealth(e.player, players.maxHealth(e.player))
  players.setSatiety(e.player, players.maxSatiety(e.player))

  -- Waking up exhausted as well as dead is unkind.
  players.setTiredness(e.player, 0)

  players.say(e.player, "You wake up whole.")

  local where = players.getWorldData(e.player, "diedAt")
  if where then
    players.say(e.player, ("Your things are near %d, %d, %d.")
      :format(where.x, where.y, where.z))
  end
end)

-- What was remembered is saved with the player, so it survives a restart: a player
-- who dies, leaves and comes back days later is still told where to look.
events.playerJoin(function(e)
  local where = players.getWorldData(e.player, "diedAt")
  if where then
    players.say(e.player, ("You last died at %d, %d, %d.")
      :format(where.x, where.y, where.z))
  end
end)

-- Where a player will wake up is the server's to answer, not a script's to remember.
-- `spawn` reads back whatever would actually be used — the bed they last slept in,
-- the spawn their role forces on them, or the world's own — so sending somebody home
-- needs nothing kept alongside it.
--
-- It answers with the centre of the block rather than its corner, so a spawn set at
-- 100 reads back as 100.5. Floor it before printing it. It answers nil where the game
-- cannot work a spawn out at all, which is a spawn stored without a height in terrain
-- that has never been generated.
commands.add {
  name = "home",
  description = "Go to where you would respawn",
  requiresPlayer = true,
  handler = function(e)
    local at = players.spawn(e.player)
    if not at then
      return "This server cannot work out where you would respawn."
    end

    players.teleport(e.player, at.x, at.y, at.z)

    return ("Sent you home, to %d, %d, %d.")
      :format(math.floor(at.x), math.floor(at.y), math.floor(at.z))
  end,
}

-- The world's own spawn is a separate question, and the answer everybody with no
-- spawn of their own gets. It is a centre in the other sense too: the server scatters
-- arrivals across the radius its configuration names, so two players sent here by the
-- game land near each other rather than on each other.
commands.add {
  name = "worldspawn",
  description = "Say where this world puts somebody with no spawn of their own",
  privilege = "controlserver",
  handler = function()
    local at = moontweaks.world.spawn()
    if not at then
      return "This world has no spawn this server can work out."
    end

    return ("The world spawn is %d, %d, %d.")
      :format(math.floor(at.x), math.floor(at.y), math.floor(at.z))
  end,
}

-- And moving it. This decides where a new player starts and where anybody who clears
-- their own spawn goes back to. It moves nobody who already has one of their own.
commands.add {
  name = "setworldspawn",
  description = "Move the world spawn to where you are standing",
  privilege = "controlserver",
  requiresPlayer = true,
  handler = function(e)
    local at = players.position(e.player)
    local x, y, z = math.floor(at.x), math.floor(at.y), math.floor(at.z)

    moontweaks.world.setSpawn(x, y, z)

    return ("The world spawn is now %d, %d, %d."):format(x, y, z)
  end,
}
