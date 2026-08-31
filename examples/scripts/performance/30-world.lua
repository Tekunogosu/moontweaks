-- What it costs to read and write the world.
--
-- Three ways of putting a block down, measured against each other. `setBlock` writes
-- one block and then relights and re-sends the chunk it touched before the next call
-- runs. `queueBlock` only stages the write, and `commit` pays that chunk cost once
-- for everything staged. The gap between them is the difference between a script
-- that builds a house and one that stalls the server building it.
--
-- Everything here happens in a box of air well above spawn, and every block in that
-- box is read before it is touched and written back afterwards, so the world is left
-- as it was found. Nobody is connected when this runs at startup, which flatters the
-- immediate write: a chunk is re-sent to every player in range of it, and there are
-- none. `/perf world` takes the same readings with you standing there.

local world = moontweaks.world
local log   = moontweaks.log

local SIDE   = 16   -- footprint, in blocks
local LAYERS = 8    -- how many of them are stacked
local TALL   = 64   -- and how many the one large batch is stacked
local ABOVE  = 30   -- how far above the ground the box sits

local PER_PASS = SIDE * SIDE * LAYERS
local PER_TALL = SIDE * SIDE * TALL

-- Two codes, alternated pass by pass, so every write in every pass genuinely changes
-- the block it lands on rather than measuring one the server can see is a no-op.
local CODES = { "game:cobblestone-granite", "game:planks-oak-ud" }
local AIR = "game:air"

--- Calls `fn(x, y, z)` for every position in a box `layers` tall.
local function over(at, layers, fn)
  for dy = 0, layers - 1 do
    for dx = 0, SIDE - 1 do
      for dz = 0, SIDE - 1 do
        fn(at.x + dx, at.y + ABOVE + dy, at.z + dz)
      end
    end
  end
end

--- The box every reading but one is taken in.
local function overBox(at, fn) over(at, LAYERS, fn) end

--- The readings, taken against a box the server is holding. Split out so `/perf
--- world` can take them again around whoever asked.
---@param at { x: integer, y: integer, z: integer } the ground corner to build above
function perf.world(at)
  -- The far corner decides whether the whole box sits in loaded chunks. A box
  -- reaching past them would measure the refusal rather than the write.
  if not world.isLoaded(at.x + SIDE - 1, at.y + ABOVE, at.z + SIDE - 1) then
    log.warn("[perf] the box does not fit in the chunks this server holds; skipping the world readings")
    return
  end

  -- The tall box where it fits, so the large batch has somewhere to go; the short one
  -- otherwise, which costs that one reading and nothing else.
  local tall = world.isLoaded(at.x, at.y + ABOVE + TALL - 1, at.z) and TALL or LAYERS

  -- What was there, so it can be put back. Air above spawn is the usual answer, but
  -- a tree is not, and neither is anything anybody built up there.

  local original, count = {}, 0
  over(at, tall, function(x, y, z)
    count = count + 1
    original[count] = world.blockAt(x, y, z) or AIR
  end)

  -- Every reading below is driven by the same nested walk over the box, which costs
  -- more per position than the bare loop the other groups use. It is measured once
  -- and taken off each of them, so what is left is the call rather than the walk.
  local walk = perf.measure("the walk these are taken over", PER_PASS, function()
    -- Three parameters like every closure below, because handing arguments over is
    -- part of what a call costs and this is the figure that stands in for the call.
    overBox(at, function(_, _, _) end)
  end)

  perf.measure("world.blockAt", PER_PASS, function()
    overBox(at, function(x, y, z) world.blockAt(x, y, z) end)
  end, { minus = walk })

  perf.measure("world.setBlock", PER_PASS, function(pass)
    local code = CODES[pass % 2 + 1]
    overBox(at, function(x, y, z) world.setBlock(code, x, y, z) end)
  end, { minus = walk })

  perf.measure("world.queueBlock", PER_PASS, function(pass)
    local code = CODES[pass % 2 + 1]
    overBox(at, function(x, y, z) world.queueBlock(code, x, y, z) end)
  end, { minus = walk })

  -- Everything that reading staged and never wrote. Cleared rather than measured, so
  -- the next reading starts from an empty queue as the first one did.
  world.commit()

  perf.measure(("queueBlock then commit (%d)"):format(PER_PASS), PER_PASS, function(pass)
    local code = CODES[pass % 2 + 1]
    overBox(at, function(x, y, z) world.queueBlock(code, x, y, z) end)
    world.commit()
  end, { minus = walk })

  -- The same box counted two ways: one call that walks it inside the mod, against
  -- one call per block from the script. Both figures are per block, so what stands
  -- between them is what a bulk call is worth. Nothing is taken off this one — the
  -- script runs no loop for it at all.
  perf.measure("countBlocks, per block", PER_PASS, function()
    world.countBlocks {
      x = at.x, y = at.y + ABOVE, z = at.z,
      toX = at.x + SIDE - 1, toY = at.y + ABOVE + LAYERS - 1, toZ = at.z + SIDE - 1,
    }
  end, { raw = true })

  -- One commit of sixteen thousand blocks rather than two, which is the same work
  -- spread over eight times as many of them. A commit relights and re-sends each
  -- chunk it touched once, so what it costs per block falls as the batch grows, and
  -- this is the figure a script building anything large should plan against.
  if tall == TALL then
    perf.measure(("queueBlock then commit (%d)"):format(PER_TALL), PER_TALL, function(pass)
      local code = CODES[pass % 2 + 1]
      over(at, TALL, function(x, y, z) world.queueBlock(code, x, y, z) end)
      world.commit()
    end, { minus = walk })
  end

  -- Put the box back exactly as it was found, staged so it costs one write per chunk.
  local index = 0
  over(at, tall, function(x, y, z)
    index = index + 1
    world.queueBlock(original[index], x, y, z)
  end)
  log.info(("[perf] put back %d block(s)"):format(world.commit()))
end

perf.later("the world readings", function()
  local at = perf.spot()

  if not at then
    log.warn("[perf] the server is holding no chunk near spawn; skipping the world readings")
    return
  end

  perf.world(at)
end)
