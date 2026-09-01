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

local groups  = moontweaks.groups
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

-- Where they wake up, as a round trip: what `setSpawn` wrote is what `spawn` reads
-- back, and clearing it leaves the world's own spawn rather than nothing.
--
-- `spawn` answers with the centre of a block, so a spawn written at 100 reads back as
-- 100.5 and the comparison floors both sides.
diag.onPlayer("players.setSpawn", function(who)
  local at = players.position(who)
  players.setSpawn(who, math.floor(at.x), math.floor(at.y), math.floor(at.z))

  return ("set your spawn to %d %d %d"):format(math.floor(at.x), math.floor(at.y), math.floor(at.z))
end)

diag.onPlayer("players.spawn", function(who)
  local at = players.position(who)
  local mine = players.spawn(who)
  assert(mine, "no spawn was worked out for a player standing in the world")
  assert(type(mine.x) == "number" and type(mine.y) == "number" and type(mine.z) == "number",
    "a spawn came back without three numbers in it")

  -- Whether it matches what `setSpawn` just wrote is reported rather than asserted: a
  -- role carrying a forced spawn outranks a player's own, so a server configured that
  -- way answers with the role's and is not wrong to.
  local written = math.floor(mine.x) == math.floor(at.x) and math.floor(mine.z) == math.floor(at.z)

  return ("%s, at %d %d %d"):format(
    written and "reads back the spawn just written" or "reads back a spawn outranking the one just written",
    math.floor(mine.x), math.floor(mine.y), math.floor(mine.z))
end)

diag.onPlayer("players.clearSpawn", function(who)
  players.clearSpawn(who)

  -- Cleared falls through to the world's own rather than to nothing, which is the
  -- half of the cascade a script cannot see from the position alone.
  local fallback = players.spawn(who)
  assert(fallback, "clearing a spawn left the player with none at all")

  return ("cleared it again; the world's own spawn at %d %d %d is yours once more")
    :format(math.floor(fallback.x), math.floor(fallback.y), math.floor(fallback.z))
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

-- Chat groups. Reading only: making one and taking one away are unbound, and speaking
-- into a group nobody is in is the one call here that reaches nobody by design rather
-- than by failing.
diag.onPlayer("groups.of", function(who)
  local mine = moontweaks.groups.of(who)
  assert(type(mine) == "table", "expected a list of memberships")

  for _, membership in ipairs(mine) do
    assert(type(membership.group) == "number", "a membership came back without a group number")
    assert(membership.standing, "a membership came back without a standing")
  end

  return ("%d group(s)"):format(#mine)
end)

-- The whole of a group's life, in order: make one, speak into it, put somebody in,
-- take them out, take it away. Each check leans on the one before it, and the last
-- leaves the server holding exactly the groups it started with.
local GROUP = "moontweaksdiagnostics"

diag.check("groups.find", function()
  -- A name nothing could ever create, so this says the same thing on every server:
  -- asking for a group there is none of answers nothing rather than raising.
  assert(groups.find("moontweaksnosuchgroupanywhere") == nil,
    "a group nothing created was found")

  return "a group this server has not got reads back as nothing"
end)

diag.check("groups.add", function()
  -- A run that failed partway may have left one standing. Clearing it first is what
  -- keeps a single bad run from failing every run after it.
  if groups.find(GROUP) then groups.remove(GROUP) end

  local made = groups.add { name = GROUP, joinPolicy = "inviteonly" }

  assert(made.group > 0, ("a new group was given the number %d"):format(made.group))
  assert(made.name == GROUP, "a group came back under another name")
  assert(made.joinPolicy == "inviteonly", "a group came back with a policy nobody asked for")
  assert(groups.find(GROUP), "the group just made could not be found again")

  return ("made '%s', given the number %d"):format(GROUP, made.group)
end)

diag.check("groups.setJoinPolicy", function()
  groups.setJoinPolicy(GROUP, "everyone")
  assert(groups.find(GROUP).joinPolicy == "everyone", "the policy did not change")

  groups.setJoinPolicy(GROUP, "inviteonly")
  assert(groups.find(GROUP).joinPolicy == "inviteonly", "the policy did not change back")

  return "opened the group and closed it again"
end)

diag.check("groups.say", function()
  -- Nobody is in it yet, so this reaches nobody. That is the answer rather than a
  -- failure: delivery is decided by who holds a membership.
  groups.say(GROUP, "[diag] the diagnostics suite is loaded")
  return "said something in a group nobody is in, which reached nobody"
end)

diag.check("groups.add (refuses a name the game will not take)", function()
  local ok, why = pcall(groups.add, { name = "not a valid name" })
  assert(not ok, "a group name with spaces in it was accepted")

  local taken = pcall(groups.add, { name = GROUP })
  assert(not taken, "two groups were allowed the same name")

  return ("refused a bad name and a taken one: %s"):format(tostring(why))
end)

-- Last of the load-time group checks, so the server ends holding exactly the groups it
-- started with. A group left standing would be found by `groups.add` on the next run,
-- which clears it rather than failing — but leaving one is still the wrong answer.
diag.check("groups.remove", function()
  groups.remove(GROUP)
  assert(groups.find(GROUP) == nil, "the group is still there after being removed")

  return ("removed '%s', leaving the server as it was"):format(GROUP)
end)

-- Putting somebody in and taking them out, against a group of this check's own. Kept
-- apart from the group above because these wait for a player and that one does not:
-- the load-time lifecycle has already finished and taken its group away by the time
-- anybody types `/diag player`.
--
-- Both halves are one check, so a failure between them cannot leave a group standing.
diag.onPlayer("groups.join", function(who)
  local PARTY = "moontweaksdiagparty"
  if groups.find(PARTY) then groups.remove(PARTY) end

  groups.add { name = PARTY }

  local ok, why = pcall(function()
    groups.join(who, PARTY, "op")

    local found
    for _, membership in ipairs(groups.of(who)) do
      if membership.name == PARTY then found = membership end
    end

    assert(found, "the group just joined was not listed among the player's")
    assert(found.standing == "op",
      ("joined as '%s' rather than as op"):format(tostring(found.standing)))

    groups.leave(who, PARTY)

    for _, membership in ipairs(groups.of(who)) do
      assert(membership.name ~= PARTY, "the group is still listed after leaving it")
    end
  end)

  -- Whatever happened above, the group goes. A check that left one behind would fail
  -- its own `groups.add` on the next run.
  groups.remove(PARTY)
  diag.used("groups.leave")

  assert(ok, tostring(why))

  return "joined a group as op and left it again"
end)

-- `players.kick` is exercised by nobody. Disconnecting whoever is running the suite is
-- the opposite of a check that puts back what it moved, and there is no second player
-- to disconnect instead. It is listed here so that the coverage figure counts it as
-- decided rather than missed.
diag.skip("players.kick", "disconnecting the player running the checks is not a check")
