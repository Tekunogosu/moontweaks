-- The weather the world is actually having.
--
-- `moontweaks.world.climateAt` says what a place is like in general — how wet a year
-- is there, how warm. This says what is happening right now: whether it is raining,
-- how hard, and what is falling.
--
-- The weather system belongs to the mods that ship with the game rather than to the
-- game's own API, so a server running without them has none of this. `available()`
-- is how a script that must run either way asks first; everything else fails by name
-- rather than answering a guess.

local weather = moontweaks.weather

if not weather.available() then
  moontweaks.log.warn("no weather system on this server; the weather script does nothing")
  return
end

-- Telling somebody what it is doing where they stand.
moontweaks.commands.add {
  name = "sky",
  description = "Say what the weather is doing here",
  requiresPlayer = true,
  handler = function(e)
    local at = moontweaks.players.position(e.player)
    local falling = weather.falling(at.x, at.y, at.z)

    if falling.level <= 0 then
      return ("Clear. It would be %s if it were doing anything."):format(falling.kind)
    end

    return ("%s, coming down at %.0f%%."):format(falling.kind, falling.level * 100)
  end,
}

-- Taking the sky over, and handing it back. An override holds the whole world at one
-- level until something clears it, so whatever sets one is responsible for lifting
-- it: nothing else will.
--
-- Named `rainfall` rather than `weather` on purpose. A script's commands are declared
-- while the server loads, before the content mods declare theirs, and the survival
-- content already wants `/weather` — a name taken here is one it cannot have, and the
-- server stops on the collision rather than starting without it. Pick names nothing
-- else is likely to want.
moontweaks.commands.add {
  name = "rainfall",
  description = "Hold the world's weather at a level, or let it run itself",
  privilege = "controlserver",
  args = {
    { name = "level", type = "number", optional = true },
  },
  handler = function(e)
    if not e.args.level then
      weather.clearPrecipitation()
      return "The weather is its own again."
    end

    weather.setPrecipitation(e.args.level)
    return ("Held at %.2f everywhere. Run this with no number to let it go."):format(e.args.level)
  end,
}

-- A storm worth watching: lightning over whoever is out in the worst of it. The
-- flash is a flash rather than a strike — nothing is set alight and nobody is hurt —
-- so this is a spectacle rather than a hazard.
moontweaks.server.every(30000, function()
  for _, who in ipairs(moontweaks.players.all()) do
    local at = moontweaks.players.position(who)
    if weather.precipitation(at.x, at.y, at.z) > 0.8 then
      weather.lightning(at.x, at.y + 25, at.z)
    end
  end
end)

-- How wet the ground has been over the days just gone, which is a different question
-- from whether it is raining now.
moontweaks.events.didPlaceBlock(function(e)
  if e.block ~= "game:farmland-dry-medium" then return end

  local soaked = weather.wetness(e.x, e.y, e.z, 3)
  if soaked > 0.5 then
    moontweaks.players.say(e.player, "This ground has had plenty of rain lately.")
  end
end)
