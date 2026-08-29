-- Doing something on a schedule, rather than in answer to something happening.
--
-- Everything a script does runs on the main thread, so a handler that works for a
-- second is a second in which the server serves nobody. The same work cut into
-- slices, one slice per timer, costs the same in total and nobody notices.
--
-- A timer asked for in a script's body starts once the whole run is known to have
-- succeeded, so `/moontweaks check` starts nothing. One asked for inside a handler is
-- already past that point and starts at once, which is what lets a command hand a
-- long job to the ticks that follow it.

local players = moontweaks.players
local server  = moontweaks.server
local world   = moontweaks.world

-- Over and over, with a wait between each. These are real milliseconds, not in-game
-- ones — `moontweaks.calendar` is what measures the world's time.
server.every(1000 * 60 * 5, function(e)
  -- `dt` is what the server actually measured rather than what was asked for, so a
  -- tick that ran late says so and work paced against real time stays paced.
  moontweaks.log.info(("%d player(s) online, %.1fs since the last check")
    :format(#players.all(), e.dt))
end)

-- Answering false stops it. This is what a job spread over several ticks does once it
-- has finished, so a timer is also a way of writing a loop that yields.
local countdown = 5
server.every(1000, function()
  countdown = countdown - 1
  moontweaks.log.info(("starting in %d"):format(countdown))
  return countdown > 0
end)

-- Once, later. A handler that wants to act after something has settled asks for this
-- rather than doing it in the middle of the event.
moontweaks.events.playerJoin(function(e)
  local who = e.player

  server.after(3000, function()
    -- Three seconds is long enough for somebody to have left again, so check before
    -- reaching for their body.
    if not players.isOnline(who) then return end
    players.say(who, "Settled in? Type /serverinfo to see where you are.")
  end)
end)

-- A long job, cut up. Each slice does a fixed amount and hands the rest to the next
-- tick, so the server keeps serving while it runs.
moontweaks.commands.add {
  name = "surveycolumn",
  description = "Walk a tall column of blocks a slice at a time",
  privilege = "controlserver",
  requiresPlayer = true,
  handler = function(e)
    local at = players.position(e.player)
    local x, z = math.floor(at.x), math.floor(at.z)
    local y, top, found = 1, math.floor(at.y), 0

    server.every(0, function()
      -- Sixty-four blocks a tick: enough to finish quickly, small enough that no
      -- single tick notices.
      for _ = 1, 64 do
        if y > top then
          players.say(e.player, ("%d solid block(s) beneath you."):format(found))
          return false
        end

        local block = world.blockAt(x, y, z)
        if block and block ~= "game:air" then found = found + 1 end
        y = y + 1
      end

      return true
    end)

    return "Counting..."
  end,
}
