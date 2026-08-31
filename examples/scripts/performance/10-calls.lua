-- What it costs to call from a script into the mod.
--
-- Every function MoonTweaks binds is reached across the same boundary: the
-- interpreter hands its arguments over, the mod converts them into what C# expects,
-- does the work, and converts the answer back. These readings take functions that do
-- almost nothing on the far side, so what is left is the crossing itself.
--
-- They are ordered by how much crosses. `elapsedMs` takes nothing and returns one
-- number, which is the floor. `info` returns a table of a dozen fields, every one of
-- which has to be written into the interpreter, which is the same work an event
-- handler is paid for before it runs.

local server  = moontweaks.server
local blocks  = moontweaks.blocks
local mods    = moontweaks.mods
local recipes = moontweaks.recipes

--- Queued rather than run here, like every group: readings are taken once the
--- server has settled, and `/perf calls` takes them again whenever you like.
function perf.calls()
  perf.calibrate()

  perf.measure("server.elapsedMs()", 20000, function()
    for _ = 1, 20000 do server.elapsedMs() end
  end)

  perf.measure("blocks.count()", 20000, function()
    for _ = 1, 20000 do blocks.count() end
  end)

  perf.measure("mods.isEnabled(id)", 20000, function()
    for _ = 1, 20000 do mods.isEnabled("game") end
  end)

  perf.measure("recipes.count(kind)", 20000, function()
    for _ = 1, 20000 do recipes.count("gridrecipes") end
  end)

  -- A dozen fields written out into a fresh table on every call. This is the shape
  -- of what a handler is handed when an event fires, so it is the closest thing here
  -- to what an event costs before the handler's own first line runs.
  perf.measure("server.info()", 5000, function()
    for _ = 1, 5000 do server.info() end
  end)
end

perf.later("the crossing readings", perf.calls)
