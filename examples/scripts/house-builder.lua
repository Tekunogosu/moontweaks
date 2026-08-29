-- Building things from a command, and measuring what it costs.
--
-- The interesting number here is not how many blocks a script can place but how it
-- places them. `setBlock` writes one block and relights and re-sends the chunk it
-- touched before the next call runs; `queueBlock` stages writes for a `commit` that
-- pays that once per chunk instead. `/build bench` measures the difference on your
-- own hardware rather than taking anyone's word for it.

local commands = moontweaks.commands
local players  = moontweaks.players
local world    = moontweaks.world
local server   = moontweaks.server
local log      = moontweaks.log

-- Says the same thing twice on purpose. What a handler returns is shown to whoever
-- ran the command and then gone; a measurement is worth keeping, so it goes to the
-- server log as well, where it can be read back afterwards.
local function report(text)
  log.info(text)
  return text
end

local FOUNDATION = "game:cobblestone-granite"
local WALL       = "game:planks-oak-ud"
local WINDOW     = "game:glass-plain"
local ROOF       = "game:planks-oak-ns"
local EMPTY      = "game:air"

local SIZE    = 7   -- footprint, in blocks
local HEIGHT  = 4   -- wall height
local SPACING = 9   -- gap between houses in the grid, centre to centre

-- One house, staged rather than written. Returns how many blocks it queued, so the
-- caller can total them without counting twice.
local function house(ox, oy, oz)
  local queued = 0
  local last = SIZE - 1

  local function put(code, x, y, z)
    world.queueBlock(code, x, y, z)
    queued = queued + 1
  end

  for dx = 0, last do
    for dz = 0, last do
      put(FOUNDATION, ox + dx, oy, oz + dz)
      put(ROOF, ox + dx, oy + HEIGHT + 1, oz + dz)
    end
  end

  -- Walls, skipping the middle of the near face for a doorway and putting a window
  -- in the middle of each of the other three.
  local middle = math.floor(SIZE / 2)

  for dy = 1, HEIGHT do
    for dx = 0, last do
      for dz = 0, last do
        local edge = dx == 0 or dx == last or dz == 0 or dz == last
        if edge then
          local door   = dz == 0 and dx == middle and dy <= 2
          local window = dy == 2 and not door
            and ((dx == middle and dz == last) or (dz == middle and (dx == 0 or dx == last)))

          if door then
            put(EMPTY, ox + dx, oy + dy, oz + dz)
          elseif window then
            put(WINDOW, ox + dx, oy + dy, oz + dz)
          else
            put(WALL, ox + dx, oy + dy, oz + dz)
          end
        end
      end
    end
  end

  return queued
end

-- Which way the player is looking, snapped to a compass point, so a row of houses
-- lines up with the world rather than sitting at whatever angle they happened to
-- be facing. Returns the step away from them and the step to their right.
local function bearing(player)
  local dir = players.facing(player)

  if math.abs(dir.x) > math.abs(dir.z) then
    local away = dir.x > 0 and 1 or -1
    return { x = away, z = 0 }, { x = 0, z = away }
  end

  local away = dir.z > 0 and 1 or -1
  return { x = 0, z = away }, { x = -away, z = 0 }
end

-- Lays out n houses as a filled square growing away from the player: one sits
-- straight ahead, five sit three across and two behind them.
local function plan(n, at, player)
  local away, right = bearing(player)
  local width = math.ceil(math.sqrt(n))
  local spots = {}

  for index = 0, n - 1 do
    local row = math.floor(index / width)
    local col = index % width
    -- Rows start a little ahead so the first one is not underfoot, and the columns
    -- are centred on the line the player is looking down.
    local forward = (row + 1) * SPACING
    local across  = (col - (width - 1) / 2) * SPACING

    spots[#spots + 1] = {
      x = math.floor(at.x + away.x * forward + right.x * across),
      z = math.floor(at.z + away.z * forward + right.z * across),
    }
  end

  return spots, width
end

commands.add {
  name = "build",
  description = "Build things, and measure what it costs",
  privilege = "controlserver",
  requiresPlayer = true,

  subcommands = {
    {
      name = "house",
      description = "Put one small house in front of you",
      handler = function(e)
        local at = players.position(e.player)
        local spots = plan(1, at, e.player)
        local began = server.elapsedMs()

        local queued = house(spots[1].x, math.floor(at.y), spots[1].z)
        local written = world.commit()

        return report(("one house: %d blocks staged, %d written, %.0f ms")
          :format(queued, written, server.elapsedMs() - began))
      end,
    },

    {
      name = "many",
      description = "Build several houses in a square growing away from you",
      args = { { name = "count", type = "int" } },
      handler = function(e)
        -- No ceiling on purpose. This is here to find where it falls over, and a
        -- limit chosen in advance would only hide that.
        local n = e.args.count
        if n < 1 then return { error = "count must be at least 1" } end

        local at = players.position(e.player)
        local spots, width = plan(n, at, e.player)
        local floor = math.floor(at.y)

        -- Everything is staged first and written once, so the cost of committing is
        -- paid per chunk rather than per house.
        local began = server.elapsedMs()
        local queued = 0
        for _, spot in ipairs(spots) do
          queued = queued + house(spot.x, floor, spot.z)
        end
        local staged = server.elapsedMs()

        local written = world.commit()
        local done = server.elapsedMs()

        return report(
          ("%d houses (%d wide): %d blocks staged in %.0f ms, %d written in %.0f ms; %.0f blocks/sec overall")
          :format(n, width, queued, staged - began, written, done - staged,
                  queued / math.max(done - began, 1) * 1000))
      end,
    },

    {
      name = "spread",
      description = "Build houses a slice per tick, so the server never stops for it",
      args = { { name = "count", type = "int" } },
      handler = function(e)
        local n = e.args.count
        if n < 1 then return { error = "count must be at least 1" } end

        local at    = players.position(e.player)
        local spots = plan(n, at, e.player)
        local floor = math.floor(at.y)
        local began = server.elapsedMs()

        -- How long to work before handing the tick back, rather than how many houses
        -- to build. A count has to be guessed against hardware the script knows
        -- nothing about; a deadline measures the hardware as it goes. The server
        -- ticks about every 33ms and calls itself overloaded past 500ms, so a budget
        -- somewhat under a tick fills each one without ever running long.
        local BUDGET_MS = 25

        local next_, blocks, slices = 1, 0, 0

        server.every(0, function()
          local deadline = server.elapsedMs() + BUDGET_MS

          -- At least one house per slice however slow the machine, so a budget set
          -- too low still finishes rather than never advancing.
          repeat
            blocks = blocks + house(spots[next_].x, floor, spots[next_].z)
            next_ = next_ + 1
          until next_ > #spots or server.elapsedMs() >= deadline

          slices = slices + 1

          -- Committing per slice rather than once at the end keeps the write small
          -- too, and a chunk touched by two slices is simply written twice.
          world.commit()

          if next_ > #spots then
            report(("%d houses spread over %d ticks: %d blocks in %.0f ms")
              :format(n, slices, blocks, server.elapsedMs() - began))
            return false
          end
        end)

        return ("building %d houses, %dms of work a tick; watch the log for the total")
          :format(n, BUDGET_MS)
      end,
    },

    {
      name = "bench",
      description = "Compare writing blocks one at a time against staging them (slow on purpose)",
      args = { { name = "blocks", type = "int", optional = true } },
      handler = function(e)
        -- A handler runs on the main thread, so the server does nothing else while
        -- this is going, and the one-at-a-time half is the slow one. No ceiling
        -- regardless: finding where that becomes intolerable is the point.
        local count = e.args.blocks or 200
        if count < 1 then return { error = "blocks must be at least 1" } end

        local at = players.position(e.player)
        local away = bearing(e.player)
        -- Well clear of the player, and clear of each other, so neither run is
        -- measuring the other's chunk being re-sent.
        local ax = math.floor(at.x + away.x * 30)
        local az = math.floor(at.z + away.z * 30)
        local y = math.floor(at.y)

        -- One at a time: each call relights and re-sends the chunk it touched.
        local began = server.elapsedMs()
        for i = 0, count - 1 do
          world.setBlock(FOUNDATION, ax + (i % 20), y + math.floor(i / 400), az + math.floor(i / 20) % 20)
        end
        local immediate = server.elapsedMs() - began

        -- Staged: the same writes, paid for once per chunk at the commit.
        began = server.elapsedMs()
        for i = 0, count - 1 do
          world.queueBlock(WALL, ax + (i % 20), y + 10 + math.floor(i / 400), az + math.floor(i / 20) % 20)
        end
        local staged = server.elapsedMs() - began
        world.commit()
        local committed = server.elapsedMs() - began

        return report(
          ("%d blocks — one at a time %.0f ms (%.2f ms/block); staged %.0f ms, committed %.0f ms total (%.2f ms/block); %.1fx")
          :format(count, immediate, immediate / count, staged, committed,
                  committed / count, immediate / math.max(committed, 1)))
      end,
    },

    {
      name = "calls",
      description = "Measure what one call from a script into the mod costs",
      args = { { name = "count", type = "int", optional = true } },
      handler = function(e)
        -- Milliseconds are a coarse ruler, so a short run measures its own rounding
        -- more than the calls. A million is where the number settles.
        local count = e.args.count or 20000
        if count < 1 then return { error = "count must be at least 1" } end

        local at = players.position(e.player)
        local x, y, z = math.floor(at.x), math.floor(at.y), math.floor(at.z)

        -- A read that changes nothing, so what is measured is the crossing rather
        -- than the work on the other side.
        local began = server.elapsedMs()
        for _ = 1, count do world.blockAt(x, y - 1, z) end
        local reads = server.elapsedMs() - began

        -- The same loop doing nothing, so the loop itself can be subtracted.
        began = server.elapsedMs()
        for _ = 1, count do end
        local empty = server.elapsedMs() - began

        return report(("%d calls in %.0f ms (loop alone %.0f ms) — about %.0f ns per call")
          :format(count, reads, empty, (reads - empty) / count * 1000000))
      end,
    },
  },
}
