-- Changing what a player is capable of: how fast they walk, how quickly they heal,
-- how fast they get hungry.
--
-- The game keeps each ability as a set of named contributions rather than as one
-- number, and adds them to a base of 1. So a value of 0.5 makes somebody half again
-- as fast, -0.5 makes them half as fast, and 0 changes nothing. Two mods may hold a
-- contribution to the same ability at once without either losing its own, which is
-- the whole reason each one is named.
--
-- Name every change your script makes, and clear it by the same name. That is what
-- makes an effect something you can take back rather than something you have to
-- guess your way out of.

local events  = moontweaks.events
local players = moontweaks.players

-- A well-fed player moves better. Set on every respawn and every join, so it follows
-- them rather than being a one-off.
local function refreshVigour(player)
  local full = players.satiety(player) / players.maxSatiety(player)

  if full > 0.75 then
    players.setStat { player = player, stat = "walkspeed", name = "wellfed", value = 0.15 }
  else
    players.clearStat(player, "walkspeed", "wellfed")
  end
end

events.playerReady(function(e) refreshVigour(e.player) end)
events.playerRespawn(function(e) refreshVigour(e.player) end)

-- Reading one back gives the blended total rather than any single contribution, so
-- this is what the player actually gets. An ability nothing has touched reads 1.
events.playerReady(function(e)
  players.say(e.player, ("You walk at %.2f times the usual pace.")
    :format(players.stat(e.player, "walkspeed")))
end)

-- A curse that outlives the session. `persistent` writes it with the player, so it
-- is still there after a restart and has to be cleared deliberately.
events.playerDeath(function(e)
  local deaths = (players.getWorldData(e.player, "deaths") or 0) + 1
  players.setWorldData(e.player, "deaths", deaths)

  if deaths >= 5 then
    players.setStat {
      player = e.player,
      stat = "healingeffectivness",
      name = "scarred",
      value = -0.25,
      persistent = true,
    }
    players.warn(e.player, "Five deaths have left their mark. You heal more slowly.")
  end
end)

-- Somewhere to undo it. Clearing takes back exactly the contribution named and
-- leaves every other one alone, including any another mod is holding.
moontweaks.commands.add {
  name = "unscar",
  description = "Clear the slow-healing curse from a player",
  privilege = "controlserver",
  args = { { name = "who", type = "player" } },
  handler = function(e)
    players.clearStat(e.args.who, "healingeffectivness", "scarred")
    players.setWorldData(e.args.who, "deaths", 0)
    return ("%s heals normally again."):format(players.name(e.args.who))
  end,
}
