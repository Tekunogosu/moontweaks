-- What this server is, and the rules it is running under.

local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players
local server   = moontweaks.server

commands.add {
  name = "serverinfo",
  description = "Say what this server is and how long it has been up",
  handler = function()
    local it = server.info()

    return ("%s — %d/%d players, up %.1f hours, world %s (seed %d), %dx%dx%d, sea level %d")
      :format(it.name, it.players, it.maxPlayers, it.uptimeMs / 3600000,
              it.worldName, it.seed,
              it.mapSizeX, it.mapSizeY, it.mapSizeZ, it.seaLevel)
  end,
}

-- Rules are settings rather than world state: a change takes effect at once and is
-- written back to the server's own configuration, so it survives a restart. A script
-- that means a change to be temporary has to put it back itself.
commands.add {
  name = "peacetime",
  description = "Turn combat between players on or off",
  privilege = "controlserver",
  args = { { name = "on", type = "bool" } },
  handler = function(e)
    server.setRules { pvp = not e.args.on }
    players.announce(e.args.on
      and "Peace is declared. Nobody can hurt anybody."
      or "Combat between players is allowed again.")
    return "Done."
  end,
}

-- Only the keys written change, so this leaves combat exactly as it found it.
commands.add {
  name = "safeworld",
  description = "Stop fire spreading and blocks falling",
  privilege = "controlserver",
  handler = function()
    server.setRules { fireSpread = false, fallingBlocks = false }

    local now = server.rules()
    return ("pvp %s, fire spread %s, falling blocks %s")
      :format(tostring(now.pvp), tostring(now.fireSpread), tostring(now.fallingBlocks))
  end,
}

-- Announce the rules as people arrive, so nobody has to discover them.
events.playerReady(function(e)
  local rules = server.rules()
  if not rules.pvp then
    players.say(e.player, "Combat between players is off on this server.")
  end
end)
