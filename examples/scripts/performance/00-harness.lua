-- What MoonTweaks costs to use, measured on the server it is running on.
--
-- The figures in the project's README came from this suite on one machine. The ones
-- it prints here came from yours, which makes them the ones worth believing: a call
-- costs whatever the hardware under it costs.
--
-- Every reading is a pass repeated until it has run long enough to divide. The only
-- clock a script has is `server.elapsedMs`, which counts whole milliseconds, so one
-- pass of anything fast measures the clock's rounding rather than the work. Passes
-- are repeated until the total crosses a floor, which puts that rounding well under
-- a percent, and the total is then divided by the operations that filled it.
--
-- An empty loop is measured the same way and taken off every reading after it, so
-- each figure is the operation rather than the loop that drove it.
--
-- Read the results with `grep '\[perf\]'` over the server log, or type `/perf`.

local server = moontweaks.server
local log    = moontweaks.log

perf = {}

--- How long a reading must run before its total is worth dividing. Larger is
--- steadier and slower; a hundred milliseconds costs the startup little and leaves
--- the clock's rounding at a tenth of a percent.
perf.FLOOR_MS = 100

--- The standing figure for each reading, by name, and the order the names were
--- first seen in. Taking a reading again replaces the one before it, so `/perf`
--- re-run under different conditions reads as it stands now rather than showing
--- every attempt.
perf.results = {}
perf.order = {}

--- What one turn of an empty loop costs, taken off every reading after it. Measured
--- rather than assumed, because it is the interpreter's speed on this machine.
perf.loopNs = 0

--- How long to leave the server alone after the world comes up before measuring
--- anything. Spawn chunks are still being generated at that point, on threads of
--- their own, and a reading taken beside that work measures the work.
perf.SETTLE_MS = 15000

--- Every reading, in the order its file declared it. Nothing is measured while the
--- scripts are running: the world does not exist yet, and a figure taken during
--- startup is a figure taken against a busy machine.
perf.deferred = {}

--- Pads a name so a column of them lines up in the log.
local function pad(text, width)
  local short = width - #text
  return short > 0 and text .. string.rep(" ", short) or text
end

--- Times one pass.
local function once(pass, index)
  local began = server.elapsedMs()
  pass(index)
  return server.elapsedMs() - began
end

--- Repeats a pass until the total is long enough to trust, then records what one
--- operation inside it cost.
---
--- A pass must do the same amount of work every time it is called: the total is
--- divided by `perPass` times the number of passes, and nothing checks that claim.
---@param name string what was measured
---@param perPass integer how many operations one pass performs
---@param pass fun(index: integer) the work, handed the number of the pass
---@param options? { raw?: boolean, minus?: number } what to take off each operation:
--- nothing under `raw`, the figure named by `minus` where this reading's own driver
--- was measured, and the empty loop otherwise
---@return number ns what one operation cost, in nanoseconds
function perf.measure(name, perPass, pass, options)
  -- One pass thrown away before the clock starts. .NET compiles a method properly
  -- only once it has seen it run, so the first time through anything is several
  -- times slower than the same code a moment later, and a reading that keeps it is
  -- measuring the compiler.
  pass(0)

  local passes, total = 0, 0

  -- A ceiling as well as a floor: a pass far slower than expected stops the reading
  -- rather than holding the whole server while it insists on its hundred milliseconds.
  repeat
    passes = passes + 1
    total = total + once(pass, passes)
  until total >= perf.FLOOR_MS or passes >= 2000

  -- What the loop around the work cost, which is not what the reading is about. The
  -- empty loop answers for a bare `for`; a reading driven by something heavier than
  -- that measures its own driver and names it here instead.
  local minus = perf.loopNs
  if options and options.raw then minus = 0 end
  if options and options.minus then minus = options.minus end

  local ops = passes * perPass
  local ns = total / ops * 1000000 - minus

  if not perf.results[name] then perf.order[#perf.order + 1] = name end
  perf.results[name] = { ns = ns, ops = ops, ms = total }

  log.info(("[perf] %s %s ns   (%d ops over %d pass(es), %.0f ms)")
    :format(pad(name, 30), pad(("%.0f"):format(ns), 7), ops, passes, total))

  return ns
end

--- Establishes what the loop itself costs, before anything is measured through one.
--- Called once, by the first group of readings.
function perf.calibrate()
  perf.loopNs = 0
  perf.loopNs = perf.measure("an empty loop", 100000, function()
    for _ = 1, 100000 do end
  end, { raw = true })
end

--- Queues a reading for the settled server. Every file declares its readings this
--- way, so they all run under the same conditions and in the order the files sit in.
function perf.later(name, fn)
  perf.deferred[#perf.deferred + 1] = { name = name, run = fn }
end

--- A column near spawn that the server is holding in memory, or nothing when it
--- holds none. Every world reading needs one, and a world reading taken outside
--- loaded chunks measures the refusal rather than the work.
---
--- The search starts at the middle of the map, because that is where a Vintage Story
--- world puts its spawn and the chunks a server holds with nobody connected are the
--- ones around it.
---@return { x: integer, y: integer, z: integer }?
function perf.spot()
  local world = moontweaks.world
  local info  = server.info()
  local middleX, middleZ = math.floor(info.mapSizeX / 2), math.floor(info.mapSizeZ / 2)

  for _, step in ipairs({ 0, 16, -16, 32, -32, 64, -64, 128, -128 }) do
    local x, z = middleX + step, middleZ + step
    local surface = world.surfaceAt(x, z)

    if surface and surface > 0 and world.isLoaded(x, surface, z) then
      return { x = x, y = surface, z = z }
    end
  end
end

--- The whole picture, as lines. Handed back rather than logged, so the same figures
--- reach the log at startup and a player's screen through `/perf`.
function perf.lines()
  local out = { "cost of one operation, measured on this server:" }

  for _, name in ipairs(perf.order) do
    out[#out + 1] = ("  %s %s ns"):format(pad(name, 30), pad(("%.0f"):format(perf.results[name].ns), 7))
  end

  out[#out + 1] = "figures are rough: they move with the hardware, the world and who is connected."
  return out
end

--- Puts the picture in the log under a heading, which is what each phase ends with.
function perf.report(phase)
  log.info(("[perf] ---- %s ----"):format(phase))
  for _, line in ipairs(perf.lines()) do log.info("[perf] " .. line) end
end

-- The world comes up shortly after the scripts have run, and the readings wait a
-- while longer than that. Nothing here is urgent, and everything here is spoiled by
-- a machine with other work on it.
moontweaks.events.worldgenStartup(function()
  log.info(("[perf] world is up; measuring in %d seconds, once it has settled")
    :format(perf.SETTLE_MS / 1000))

  server.after(perf.SETTLE_MS, function()
    -- Everything is measured twice and only the second reading kept. .NET compiles
    -- a method properly once it has watched it run for a while, and it does that on
    -- another thread while the first reading is still going: a figure taken on the
    -- way through that is several times the one taken after it.
    for round = 1, 2 do
      for _, entry in ipairs(perf.deferred) do
        local ok, why = pcall(entry.run)
        if not ok then log.warn(("[perf] %s could not be measured -- %s"):format(entry.name, tostring(why))) end
      end

      if round == 1 then log.info("[perf] that was the warm-up; taking every reading again") end
    end

    perf.report("what this server costs")
  end)
end)
