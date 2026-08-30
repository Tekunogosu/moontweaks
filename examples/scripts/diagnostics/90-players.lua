-- A player's body, their standing on the server, and what is remembered about them.
--
-- None of this can run at startup: every function here takes a player identifier and
-- nobody is connected while the assets are loading. So all of it waits for `/diag
-- player`, which hands each check the identifier of whoever typed it.
--
-- Everything that changes a body is a round trip. Health, satiety, tiredness and
-- game mode are read, moved, read back and put back, so somebody running this on
-- themselves ends where they started — including if a check fails partway, since the
-- restore happens either way.

local players = moontweaks.players

-- Read on their own, needing nobody but whoever typed the command.
diag.check("players.all", function()
  local everybody = players.all()
  assert(type(everybody) == "table", "expected a list")

  return ("%d player(s) online"):format(#everybody)
end)

diag.check("players.announce", function()
  players.announce("[diag] the diagnostics suite is loaded")
  return "announced to everybody, which may be nobody"
end)

diag.onPlayer("players.name", function(who)
  local name = players.name(who)
  assert(type(name) == "string" and #name > 0, "no name")

  return ("'%s'"):format(name)
end)

diag.onPlayer("players.uidOf", function(who)
  local found = players.uidOf(players.name(who))
  assert(found == who, ("looked up '%s' and got %s"):format(players.name(who), tostring(found)))

  return "a name resolves back to the identifier it came from"
end)

diag.onPlayer("players.isOnline", function(who)
  assert(players.isOnline(who), "somebody who just typed a command reports as offline")
  return "true for whoever asked"
end)

diag.onPlayer("players.position", function(who)
  local at = players.position(who)
  return ("%.1f %.1f %.1f"):format(at.x, at.y, at.z)
end)

diag.onPlayer("players.facing", function(who)
  local way = players.facing(who)
  return ("%.2f %.2f %.2f"):format(way.x, way.y, way.z)
end)

diag.onPlayer("players.looking", function(who)
  local at = players.looking(who)
  if not at then return "not looking at any block within reach" end

  return ("%s at %d %d %d, face %s"):format(at.block, at.x, at.y, at.z, tostring(at.face))
end)

diag.onPlayer("players.lookingAtEntity", function(who)
  local id = players.lookingAtEntity(who)
  if not id then return "not looking at anything alive" end

  return ("entity %s"):format(tostring(id))
end)

diag.onPlayer("players.say", function(who)
  players.say(who, "[diag] this line was sent to you alone")
  return "sent a line to whoever asked"
end)

diag.onPlayer("players.warn", function(who)
  players.warn(who, "[diag] and this one as a warning")
  return "sent a warning to whoever asked"
end)

diag.onPlayer("players.give", function(who)
  local took = players.give(who, { code = "game:stick", quantity = 1 })
  return ("handed over a stick: %s -- drop it if you would rather not keep it"):format(tostring(took))
end)

-- The body. Each of these moves something and puts it back.
diag.onPlayer("players.health", function(who)
  return ("%.1f of %.1f"):format(players.health(who), players.maxHealth(who))
end)

diag.onPlayer("players.maxHealth", function(who)
  local most = players.maxHealth(who)
  assert(most > 0, "no maximum health")

  return ("%.1f"):format(most)
end)

diag.onPlayer("players.setHealth", function(who)
  return diag.roundTrip(
    function() return players.health(who) end,
    function(value) players.setHealth(who, value) end,
    players.maxHealth(who))
end)

diag.onPlayer("players.satiety", function(who)
  return ("%.1f of %.1f"):format(players.satiety(who), players.maxSatiety(who))
end)

diag.onPlayer("players.maxSatiety", function(who)
  local most = players.maxSatiety(who)
  assert(most > 0, "no maximum satiety")

  return ("%.1f"):format(most)
end)

diag.onPlayer("players.setSatiety", function(who)
  return diag.roundTrip(
    function() return players.satiety(who) end,
    function(value) players.setSatiety(who, value) end,
    players.maxSatiety(who))
end)

diag.onPlayer("players.tiredness", function(who)
  return ("%.1f"):format(players.tiredness(who))
end)

diag.onPlayer("players.setTiredness", function(who)
  return diag.roundTrip(
    function() return players.tiredness(who) end,
    function(value) players.setTiredness(who, value) end,
    0)
end)

diag.onPlayer("players.isSleeping", function(who)
  return tostring(players.isSleeping(who))
end)

diag.onPlayer("players.nutrition", function(who)
  local held = players.nutrition(who)
  assert(held, "no nutrition reading")

  local parts = {}
  for kind, level in pairs(held) do parts[#parts + 1] = ("%s %.0f"):format(kind, level) end
  table.sort(parts)

  return table.concat(parts, ", ")
end)

diag.onPlayer("players.gameMode", function(who)
  return tostring(players.gameMode(who))
end)

diag.onPlayer("players.setGameMode", function(who)
  local was = players.gameMode(who)

  return diag.roundTrip(
    function() return players.gameMode(who) end,
    function(mode) players.setGameMode(who, mode) end,
    was == "creative" and "survival" or "creative")
end)

diag.onPlayer("players.teleport", function(who)
  local at = players.position(who)
  players.teleport(who, at.x, at.y + 1, at.z)
  local moved = players.position(who)
  players.teleport(who, at.x, at.y, at.z)

  return ("%.1f -> %.1f -> %.1f in y"):format(at.y, moved.y, players.position(who).y)
end)

-- Where they wake up. Read through no getter, so the check is that both calls are
-- accepted and the spawn is cleared afterwards, leaving the world's own spawn.
diag.onPlayer("players.setSpawn", function(who)
  local at = players.position(who)
  players.setSpawn(who, math.floor(at.x), math.floor(at.y), math.floor(at.z))

  return ("set your spawn to %d %d %d"):format(math.floor(at.x), math.floor(at.y), math.floor(at.z))
end)

diag.onPlayer("players.clearSpawn", function(who)
  players.clearSpawn(who)
  return "cleared it again, so the world's own spawn is yours once more"
end)

-- What is remembered about them, in the two places it can be kept.
diag.onPlayer("players.setWorldData", function(who)
  players.setWorldData(who, "diagnostics", { checked = true })
  return "wrote against this save game"
end)

diag.onPlayer("players.getWorldData", function(who)
  local held = players.getWorldData(who, "diagnostics")
  assert(held and held.checked == true, "what was written did not come back")

  return "read back what was written"
end)

diag.onPlayer("players.setAccountData", function(who)
  players.setAccountData(who, "diagnostics", { checked = true })
  return "wrote against the account, which follows them between save games"
end)

diag.onPlayer("players.getAccountData", function(who)
  local held = players.getAccountData(who, "diagnostics")
  assert(held and held.checked == true, "what was written did not come back")

  return "read back what was written"
end)

-- Their standing on the server. Read only: granting a privilege is deliberately not
-- bound, since a script that could grant one could grant itself anything.
diag.onPlayer("players.privileges", function(who)
  local held = players.privileges(who)
  assert(type(held) == "table", "expected a list")

  return ("%d privilege(s): %s"):format(#held, table.concat(held, ", "))
end)

diag.onPlayer("players.hasPrivilege", function(who)
  local held = players.privileges(who)
  assert(#held > 0, "nobody holds no privileges at all")
  assert(players.hasPrivilege(who, held[1]), ("holds '%s' but says otherwise"):format(held[1]))
  assert(not players.hasPrivilege(who, "a-privilege-nobody-defines"), "holds a privilege nothing defines")

  return ("true for '%s', false for one that does not exist"):format(held[1])
end)

-- A stat is a base the game keeps and whatever is named as adding to it, so a named
-- contribution is set, read and cleared by the same name.
diag.onPlayer("players.stat", function(who)
  local walk = players.stat(who, "walkspeed")
  assert(type(walk) == "number", "expected a number")

  return ("walkspeed %.3f"):format(walk)
end)

diag.onPlayer("players.setStat", function(who)
  local before = players.stat(who, "walkspeed")

  players.setStat {
    player = who, stat = "walkspeed",
    name = "moontweaks-diagnostic", value = 0.2, persistent = false,
  }

  local held = players.stat(who, "walkspeed")
  players.clearStat(who, "walkspeed", "moontweaks-diagnostic")

  assert(held ~= before, ("the contribution did not land: %.3f both times"):format(before))
  return ("%.3f -> %.3f -> %.3f"):format(before, held, players.stat(who, "walkspeed"))
end)

diag.onPlayer("players.clearStat", function(who)
  players.clearStat(who, "walkspeed", "moontweaks-diagnostic")
  return ("cleared, back to %.3f"):format(players.stat(who, "walkspeed"))
end)
