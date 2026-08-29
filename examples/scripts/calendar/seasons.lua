-- Where in the year somewhere is.
--
-- Season is asked of a place rather than of the world, because the two halves of the
-- world are half a year apart: the same date is summer in one and winter in the
-- other. There is no answer to give without a position, which is why `seasonAt` takes
-- one and `now` does not offer a season at all.

local calendar = moontweaks.calendar
local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players
local world    = moontweaks.world

commands.add {
  name = "season",
  description = "Say what the season is where you are standing",
  requiresPlayer = true,
  handler = function(e)
    local at = players.position(e.player)
    local here = calendar.seasonAt(math.floor(at.x), math.floor(at.y), math.floor(at.z))

    return ("%s, %.0f%% through the year, in the %sern hemisphere.")
      :format(here.season, here.progress * 100, here.hemisphere)
  end,
}

-- Read `progress` rather than `season` for anything that should change gradually
-- rather than in four steps. It runs from 0 at the start of spring to 1 at the end of
-- winter, so it is a curve rather than a switch.
events.playerReady(function(e)
  local at = players.position(e.player)
  local here = calendar.seasonAt(math.floor(at.x), math.floor(at.y), math.floor(at.z))
  local climate = world.climateAt(math.floor(at.x), math.floor(at.y), math.floor(at.z))

  -- Deep winter somewhere already cold is worth a word of warning.
  if here.season == "winter" and climate.temperature < -5 then
    players.warn(e.player, ("It is %.0f degrees out there. Take a coat.")
      :format(climate.temperature))
  end
end)

-- Daylight is a separate reading from season: how bright the sky is at a place right
-- now, from 0 in the dark to 1 at noon. It is the sky's own strength rather than what
-- reaches the ground, so somewhere deep underground reads the same as the surface
-- above it — use `world.lightAt` for what actually arrives.
events.didUseBlock(function(e)
  if e.block ~= "game:sign-ground-north" then return end

  players.say(e.player, ("Daylight is at %.0f%% strength.")
    :format(calendar.daylightAt(e.x, e.z) * 100))
end)

-- Holding the world at one point in the year, whatever the date says. Summer sits
-- around 0.25 and winter around 0.75.
commands.add {
  name = "endlesssummer",
  description = "Hold the world in summer, or let the seasons run again",
  privilege = "controlserver",
  args = { { name = "on", type = "bool" } },
  handler = function(e)
    if e.args.on then
      calendar.setSeason(0.25)
      return "Summer, until somebody says otherwise."
    end

    calendar.clearSeason()
    return "The seasons run with the calendar again."
  end,
}
