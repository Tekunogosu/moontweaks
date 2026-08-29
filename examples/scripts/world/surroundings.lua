-- Asking what a place is like: its weather, its light, its wind, and where its
-- ground is.
--
-- These are how something is made to depend on where it happened rather than only on
-- what happened. All of them read a loaded world, so they belong in a handler rather
-- than in a script's body.

local events  = moontweaks.events
local players = moontweaks.players
local world   = moontweaks.world

-- Climate carries two pairs of numbers worth telling apart. `temperature` and
-- `rainfall` are what it is like now, which the seasons and the weather move.
-- `worldgenTemperature` and `worldgenRainfall` are what the place is like generally,
-- which nothing moves — so ask the first to know whether it is cold today and the
-- second to know whether this is a cold place.
local function describe(player, x, y, z)
  local here = world.climateAt(x, y, z)

  players.say(player, ("%.1f degrees, %.0f%% rain, %.0f%% forest.")
    :format(here.temperature, here.rainfall * 100, here.forestDensity * 100))

  if here.worldgenTemperature < 0 then
    players.say(player, "This is a cold place whatever today is doing.")
  end

  if here.fertility > 0.6 then
    players.say(player, "Things grow well here.")
  end
end

-- Light: which light to count is the whole question. `onlyblocklight` ignores the
-- sky, so it answers "is this lit by something we built" — which is what a check for
-- somewhere mobs will spawn actually needs. `maxtimeofdaylight` answers "can I see",
-- which changes through the day.
local function lit(x, y, z)
  return world.lightAt("onlyblocklight", x, y, z)
end

events.didUseBlock(function(e)
  if e.block ~= "game:sign-ground-north" then return end

  describe(e.player, e.x, e.y, e.z)

  local torchlight = lit(e.x, e.y + 1, e.z)
  players.say(e.player, torchlight >= 8
    and ("Well lit here: %d."):format(torchlight)
    or ("Dark enough for trouble: %d."):format(torchlight))

  -- Wind is a direction whose length is its speed, so multiplying it by a number is
  -- how something is thrown downwind.
  local wind = world.windAt(e.x, e.y + 1, e.z)
  players.say(e.player, ("Wind %.2f %.2f %.2f."):format(wind.x, wind.y, wind.z))

  -- Where the ground is, without looking for it.
  local ground = world.surfaceAt(e.x, e.z)
  if ground then
    players.say(e.player, ("The surface here is at %d, and you are at %d.")
      :format(ground, e.y))
  end
end)
