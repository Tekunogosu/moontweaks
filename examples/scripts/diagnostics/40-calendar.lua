-- The world's clock, its seasons and the light the sky gives.
--
-- None of this answers while the assets are loading: the calendar is built with the
-- save game, a little after the scripts have run, so every check here waits. Calling
-- `calendar.now` from a script's own body raises rather than answering, which is
-- worth knowing before writing one that does.
--
-- The clock is the one thing here that cannot be read and put back: `add` moves it
-- and nothing moves it to a stated time. So the check moves it by a hundredth of an
-- hour and moves it back by the same, and reports both readings — a world that ends
-- a few seconds from where it started, rather than one that ends where it started.

local calendar = moontweaks.calendar

diag.later("calendar.now", function()
  local now = calendar.now()
  assert(type(now) == "table", "expected a reading")
  assert(now.totalDays and now.totalDays >= 0, "no total days")

  return ("%s -- day %d of %d, %s, year %d, moon %s at %.2f")
    :format(now.pretty, now.dayOfYear, now.daysPerYear, now.month, now.year,
            now.moonPhase, now.moonBrightness)
end)

diag.later("calendar.seasonAt", function()
  local spot = diag.loadedSpot()
  assert(spot, "no loaded column to read a season at")

  local season = calendar.seasonAt(spot.x, spot.y, spot.z)
  assert(season.season, "no season")

  return ("%s, %.0f%% through, %sern hemisphere")
    :format(season.season, season.progress * 100, season.hemisphere)
end)

diag.later("calendar.daylightAt", function()
  local light = calendar.daylightAt(0, 0)
  assert(type(light) == "number", "expected a number")

  return ("%.3f at the origin"):format(light)
end)

diag.later("calendar.add", function()
  local before = calendar.now().totalHours
  calendar.add(0.01)
  local moved = calendar.now().totalHours
  calendar.add(-0.01)
  local back = calendar.now().totalHours

  assert(moved > before, ("adding did not move the clock: %.4f then %.4f"):format(before, moved))

  return ("%.4f -> %.4f -> %.4f total hours"):format(before, moved, back)
end)

-- A named speed sits alongside whatever else is slowing or hurrying the world, and
-- clearing it by the same name takes only that one away. So the pair is a genuine
-- round trip even though nothing reads the speed back.
diag.later("calendar.setSpeed", function()
  calendar.setSpeed("moontweaks-diagnostic", 2)
  return "set a named speed of 2"
end)

diag.later("calendar.clearSpeed", function()
  calendar.clearSpeed("moontweaks-diagnostic")
  return "took the named speed back off"
end)

diag.later("calendar.setSeason", function()
  local spot = diag.loadedSpot()
  assert(spot, "no loaded column to read a season at")

  local before = calendar.seasonAt(spot.x, spot.y, spot.z)
  calendar.setSeason(0.5)
  local held = calendar.seasonAt(spot.x, spot.y, spot.z)
  calendar.clearSeason()
  local after = calendar.seasonAt(spot.x, spot.y, spot.z)

  assert(held.season, "no season while one was held")

  return ("%s -> held at %s -> %s"):format(before.season, held.season, after.season)
end)

diag.later("calendar.clearSeason", function()
  calendar.clearSeason()
  return "cleared, so the season follows the calendar again"
end)
