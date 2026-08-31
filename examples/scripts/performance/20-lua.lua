-- What the interpreter costs on its own, for scale.
--
-- A call into the mod is only expensive next to something, and the something is the
-- Lua the script is already running. These readings touch nothing outside the
-- interpreter, so putting them beside the crossing readings answers the question
-- worth asking: is this loop slow because of what it calls, or because of how many
-- times it goes round?

local counter = 0
local bag = {}

local function add(a, b) return a + b end

--- Queued for the settled server, like every group.
function perf.interpreter()
  perf.measure("arithmetic", 100000, function()
    for i = 1, 100000 do counter = counter + i * 2 end
  end)

  perf.measure("a table write and read", 100000, function()
    for i = 1, 100000 do
      bag[1] = i
      counter = bag[1]
    end
  end)

  perf.measure("a call to a Lua function", 100000, function()
    for i = 1, 100000 do counter = add(counter, i) end
  end)

  -- Allocating, unlike the three above it, so this is the one that makes the
  -- collector work. A handler building a string per block is doing this.
  perf.measure("building a string", 20000, function()
    for i = 1, 20000 do counter = #("block " .. i) end
  end)
end

perf.later("the interpreter readings", perf.interpreter)
