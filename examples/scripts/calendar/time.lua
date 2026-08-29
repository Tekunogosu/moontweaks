-- The world's clock, which is not the server's.
--
-- `moontweaks.server.elapsedMs` measures real time, so the difference between two of
-- them is how long something actually took. `moontweaks.calendar` measures the
-- world's, where an in-game hour is a few real minutes and a server may change even
-- that. Anything about daylight, seasons or spoilage belongs here; anything about how
-- long a job ran belongs there.

local calendar = moontweaks.calendar
local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players

-- Everything the clock reads, in one call, answered from the same tick. Reading it
-- field by field would be a call apiece for numbers that all come from one place.
commands.add {
  name = "when",
  description = "Say what the world's clock reads",
  handler = function()
    local now = calendar.now()

    -- Moon brightness dips a shade below zero either side of a new moon, so clamp
    -- it rather than telling somebody the moon is minus ten per cent lit.
    local lit = math.max(0, now.moonBrightness) * 100

    return ("%s (day %d of %d) — %.1f o'clock, moon %s, %.0f%% lit")
      :format(now.pretty, now.dayOfYear, now.daysPerYear, now.hourOfDay,
              now.moonPhase, lit)
  end,
}

-- `totalHours` is the one to remember and subtract from later: unlike `hourOfDay` it
-- never goes backwards, so it is what an "how long since" is measured with.
events.didUseBlock(function(e)
  if e.block ~= "game:bed-wood-head-north" then return end

  local now = calendar.now()
  local last = players.getData(e.player, "lastSlept")

  if last then
    players.say(e.player, ("You last slept %.1f in-game hours ago.")
      :format(now.totalHours - last))
  end

  players.setData(e.player, "lastSlept", now.totalHours)
end)

-- Moving the clock moves time itself rather than skipping to an hour, so everything
-- that ages by the clock ages with it: crops grow, food spoils, the season advances
-- by exactly what was added.
commands.add {
  name = "skip",
  description = "Move the world's clock forward",
  privilege = "controlserver",
  args = { { name = "hours", type = "number" } },
  handler = function(e)
    calendar.add(e.args.hours)
    players.announce(("Time moves on by %.1f hours."):format(e.args.hours))
    return calendar.now().pretty
  end,
}

-- Changing how fast time passes, under a name. Named rather than set outright so two
-- scripts changing the speed do not silently undo each other: each holds its own, the
-- game combines them, and clearing takes back exactly the one it was given.
-- One timer for the life of the server, started here in the body rather than in the
-- handler. A handler that starts a timer starts another every time it is run, and
-- nothing takes the old one back — so a switch like this flips a flag the timer
-- reads instead.
local slowNights = false

-- Checked every real minute rather than every tick: the hour of day does not change
-- fast enough to be worth asking more often than that.
moontweaks.server.every(1000 * 60, function()
  if slowNights then
    local hour = calendar.now().hourOfDay
    calendar.setSpeed("nightlength", (hour < 6 or hour > 20) and 0.5 or 1.0)
  end
end)

commands.add {
  name = "nightsarelong",
  description = "Slow the clock at night and let it run by day",
  privilege = "controlserver",
  args = { { name = "on", type = "bool" } },
  handler = function(e)
    slowNights = e.args.on

    if not slowNights then
      calendar.clearSpeed("nightlength")
      return "The clock runs normally again."
    end

    return "Nights will now pass at half speed."
  end,
}
