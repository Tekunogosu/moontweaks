-- The server itself, and what it says about the machine the mod is running on.
--
-- Everything here answers while the assets are still loading, which is when scripts
-- run, so all of it is checked immediately rather than queued. That is worth knowing
-- on its own: these are the functions a script may call from its own body without
-- waiting for anything.

local log     = moontweaks.log
local mods    = moontweaks.mods
local server  = moontweaks.server
local world   = moontweaks.world

-- The two the rest of the suite reports through. Checked first, so a run where the
-- log itself is broken says so on its first line rather than falling silent.
diag.check("log.info", function()
  log.info("[diag] the log is reachable")
  return "wrote a notification"
end)

diag.check("log.warn", function()
  log.warn("[diag] a warning looks like this; one failure below would too")
  return "wrote a warning"
end)

diag.check("server.info", function()
  local info = server.info()
  assert(type(info) == "table", "expected a table")
  assert(info.seaLevel and info.seaLevel > 0, "no sea level")

  return ("%s, %d/%d player(s), sea level %d, map %dx%dx%d")
    :format(info.worldName, info.players, info.maxPlayers, info.seaLevel,
            info.mapSizeX, info.mapSizeY, info.mapSizeZ)
end)

diag.check("server.elapsedMs", function()
  local first = server.elapsedMs()
  assert(type(first) == "number" and first > 0, "expected a positive number of milliseconds")

  return ("%.0fms since the server started"):format(first)
end)

-- The rules are read, changed, read back and put back, which is the shape every
-- setter in this suite is checked with. Nothing is left changed: `roundTrip` restores
-- what it found whether or not the write worked.
diag.check("server.rules", function()
  local rules = server.rules()
  assert(type(rules) == "table", "expected a table")

  -- Every key the setter takes has to come back from the reader, or a script cannot
  -- put back what it moved.
  for _, key in ipairs({
    "pvp", "fireSpread", "fallingBlocks",
    "entitySpawning", "blockTickInterval", "randomBlockTicksPerChunk", "spawnCapPlayerScaling",
  }) do
    assert(rules[key] ~= nil, ("rules() answered nothing for '%s'"):format(key))
  end

  return ("pvp %s, fire spread %s, falling blocks %s, spawning %s, block ticks every %dms")
    :format(tostring(rules.pvp), tostring(rules.fireSpread), tostring(rules.fallingBlocks),
      tostring(rules.entitySpawning), rules.blockTickInterval)
end)

diag.check("server.setRules", function()
  return diag.roundTrip(
    function() return server.rules().pvp end,
    function(value) server.setRules { pvp = value } end,
    not server.rules().pvp)
end)

-- The rule kept with the world rather than with the server. Round-tripped separately
-- because it is written somewhere else and a check that only moved `pvp` would not
-- notice if that path were wrong.
diag.check("server.setRules (world rule)", function()
  return diag.roundTrip(
    function() return server.rules().entitySpawning end,
    function(value) server.setRules { entitySpawning = value } end,
    not server.rules().entitySpawning)
end)

-- And a number rather than a flag, so the conversion out of Lua's one number type is
-- exercised too.
diag.check("server.setRules (numbers)", function()
  return diag.roundTrip(
    function() return server.rules().blockTickInterval end,
    function(value) server.setRules { blockTickInterval = value } end,
    server.rules().blockTickInterval + 100)
end)

-- Declaring a privilege, which nothing at startup can then ask after: reading one
-- back needs a player, and nobody is connected while the assets load. So the check is
-- that the call is accepted; `/diag player` is where a privilege is read.
--
-- The declaration lasts as long as the server runs and no longer, so declaring one
-- here leaves nothing behind for the next start to trip over.
diag.check("server.addPrivilege", function()
  server.addPrivilege("moontweaks.diagnostics", "Granted by the MoonTweaks diagnostics suite")
  return "declared moontweaks.diagnostics for this run"
end)

-- What else is loaded beside this mod. A server with only the game's own mods still
-- answers all three, so none of this is conditional on anything being installed.
diag.check("mods.all", function()
  local all = mods.all()
  assert(#all > 0, "no mods at all, which cannot be right")

  local names = {}
  for i, mod in ipairs(all) do names[i] = ("%s %s"):format(mod.id, mod.version) end

  return table.concat(names, ", ")
end)

diag.check("mods.get", function()
  local self = mods.get("moontweaks")
  assert(self, "this mod cannot see itself")

  return ("%s %s"):format(self.name, self.version)
end)

diag.check("mods.isEnabled", function()
  assert(mods.isEnabled("moontweaks"), "this mod reports itself as not enabled")
  assert(not mods.isEnabled("a-mod-that-does-not-exist"), "a mod nobody has is reported as enabled")

  return "true for this mod, false for one that does not exist"
end)

-- What the save game remembers between restarts. This pair is deliberately not put
-- back afterwards: a counter that survives is the only evidence the save game is
-- actually being written to, and reading a number greater than one on the second
-- start is what proves it. Everything else the suite writes is restored.
local before = world.getData("diagnostics")
local runs = (before and before.runs or 0) + 1

diag.check("world.setData", function()
  world.setData("diagnostics", { runs = runs })
  return ("recorded run %d"):format(runs)
end)

diag.check("world.getData", function()
  local held = world.getData("diagnostics")

  assert(type(held) == "table", "expected a table back")
  assert(held.runs == runs, ("wrote %d and read %s"):format(runs, tostring(held.runs)))

  if runs == 1 then
    return "run 1: start the server again and this should read 2, which is what proves it persists"
  end

  return ("run %d, so what run %d wrote survived a restart"):format(runs, runs - 1)
end)
