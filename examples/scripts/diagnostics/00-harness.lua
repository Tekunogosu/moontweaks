-- Whether this mod's API answers on the server it is running on, checked by using it.
--
-- The suite is not a demonstration: every check calls a bound function for real and
-- says in the log whether the call answered. A server that loads it and reads
-- `[diag]` in the log knows which parts of the API work here, rather than assuming
-- they all do because the mod started without complaining.
--
-- Three things decide when a check can run, and the whole suite is shaped around
-- them. Scripts run while the game is still loading its assets, so the registries
-- are populated and the world is not: the calendar, the weather and anything that
-- spawns raise a failure at that point rather than answering. Chunks come up shortly
-- after, which `worldgenStartup` is the signal for. And a player's body, their
-- inventory and anything drawn for them need somebody actually standing in the
-- world, which no event at startup can conjure.
--
-- So a check declares which of those it needs and the harness runs it when that is
-- true: `diag.check` now, `diag.later` once the world is up, `diag.onPlayer` when
-- somebody types `/diag player`.
--
-- Read the results with `grep '\[diag\]'` over the server log. A failing check logs
-- as a warning, so `grep '\[diag\] FAIL'` is the whole answer to "did anything
-- break".

local log = moontweaks.log

diag = {}

--- Every API name a check has actually called, against which coverage is measured.
diag.touched = {}

--- The standing verdict on each check, by name, and the order the names were first
--- seen in. A name recorded again replaces the verdict before it, so a check that
--- failed against one inventory and passed against the next counts once and reads as
--- it stands now rather than as every attempt went.
diag.results = {}
diag.order = {}

--- Checks waiting for the world, and checks waiting for a player.
diag.deferred = {}
diag.grounded = {}

--- Events registered for, and what the first firing of each looked like.
diag.expected = {}
diag.seen = {}

--- Entities a check made, so a check that fails halfway still cleans up after itself.
diag.spawned = {}

--- Marks API names as exercised. A check covers the name it is called by; anything
--- else it genuinely calls is named here, so coverage counts calls rather than
--- intentions.
function diag.used(...)
  for i = 1, select("#", ...) do diag.touched[select(i, ...)] = true end
end

--- Sole owner of what a run knows: every verdict reaches the report and the log
--- through here, whether it came from a check, a skip or a handler firing later.
---@param name string the API name the verdict is about
---@param verdict "pass"|"fail"|"skip"
---@param detail string what to show beside it
function diag.record(name, verdict, detail)
  diag.used(name)

  if not diag.results[name] then diag.order[#diag.order + 1] = name end
  diag.results[name] = { verdict = verdict, detail = detail }

  local line = ("[diag] %s %s -- %s"):format(verdict == "fail" and "FAIL" or verdict, name, detail)
  if verdict == "fail" then log.warn(line) else log.info(line) end
end

--- Calls one bound function and says whether it answered. The check passes unless it
--- raises, so `assert` is how a check states what the answer had to look like, and
--- what it returns is what the log shows beside the result.
function diag.check(name, fn)
  local ok, detail = pcall(fn)
  diag.record(name, ok and "pass" or "fail", tostring(detail))
end

--- Names a function this server cannot exercise, and why. A skip counts as covered:
--- the suite reached it and made a decision about it, which is the opposite of the
--- silence an unmentioned function leaves.
function diag.skip(name, why)
  diag.record(name, "skip", why)
end

--- How many checks stand passed, failed and stepped over, counting each name once.
function diag.tally()
  local counts = { pass = 0, fail = 0, skip = 0 }

  for _, result in pairs(diag.results) do
    counts[result.verdict] = counts[result.verdict] + 1
  end

  return counts
end

--- The checks standing failed, in the order they were first run.
function diag.failures()
  local out = {}

  for _, name in ipairs(diag.order) do
    local result = diag.results[name]
    if result.verdict == "fail" then out[#out + 1] = ("%s -- %s"):format(name, result.detail) end
  end

  return out
end

--- Queues a check for the moment the world is up, which is where the calendar, the
--- weather, the chunks and anything that spawns start answering.
function diag.later(name, fn)
  diag.deferred[#diag.deferred + 1] = { name = name, run = fn }
end

--- Queues a check needing somebody standing in the world. It is handed the
--- identifier of whoever asked for it.
function diag.onPlayer(name, fn)
  diag.grounded[#diag.grounded + 1] = { name = name, run = fn }
end

--- Runs a queue, keeping one check's failure to itself.
function diag.run(queue, argument)
  for _, entry in ipairs(queue) do
    local ok, detail = pcall(entry.run, argument)
    diag.record(entry.name, ok and "pass" or "fail", tostring(detail))
  end
end

--- Reads a value, writes a different one, reads it back and puts the first one
--- returned. This is the shape of every setter check in the suite: proving a write
--- lands means reading after it, and doing that to a live server means putting back
--- what was there whether or not the check succeeded.
---
--- The three values are reported rather than compared. A server clamps some of what
--- it is given — a health above the maximum, a satiety a body cannot hold — and a
--- clamped write is working correctly, so the log shows what happened and leaves the
--- judgement to whoever reads it.
function diag.roundTrip(read, write, probe)
  local before = read()
  local written, why = pcall(write, probe)

  -- Written out rather than folded into an `and`/`or`, which cannot tell a value of
  -- false from nothing at all — and half the values checked here are booleans.
  local seen
  if written then seen = read() end

  local restored = pcall(write, before)

  assert(written, tostring(why))
  assert(restored, ("wrote %s but could not put %s back"):format(tostring(probe), tostring(before)))

  return ("%s -> wrote %s, read %s -> back to %s")
    :format(tostring(before), tostring(probe), tostring(seen), tostring(read()))
end

--- Where the ground is in a column the server currently holds in memory.
---@class Spot
---@field x integer
---@field y integer ground level, so a check works above it rather than inside it
---@field z integer

--- A column the server currently holds in memory, or nothing when it holds none.
--- Every world check needs one: an unloaded chunk is stepped over rather than
--- answered for, so a reading taken from one says nothing about whether the binding
--- works.
---
--- The search starts at the middle of the map rather than at the origin. A Vintage
--- Story world is a million blocks across with its spawn at the centre, so nothing
--- is ever loaded anywhere near 0 0, and the chunks a server holds with nobody
--- connected are the ones around spawn.
---
--- Handed back as one value rather than three, so a caller checking it has checked
--- all of it.
---@return Spot?
local held
function diag.loadedSpot()
  if held then return held end

  local world = moontweaks.world
  local info = moontweaks.server.info()
  local middleX, middleZ = math.floor(info.mapSizeX / 2), math.floor(info.mapSizeZ / 2)

  for _, step in ipairs({ 0, 16, -16, 32, -32, 64, -64, 128, -128 }) do
    local x, z = middleX + step, middleZ + step
    local surface = world.surfaceAt(x, z)

    if surface and surface > 0 and world.isLoaded(x, surface, z) then
      held = { x = x, y = surface, z = z }
      return held
    end
  end
end

--- Forgets the column found last time, so the next search runs against the chunks
--- the server is holding now rather than the ones it held at startup.
function diag.forgetSpot()
  held = nil
end

--- Registers for an event and remembers the first time it fires. Watching costs
--- nothing until something happens, so every event the mod binds is watched and the
--- report says which of them this server has actually raised.
function diag.watch(name, register, describe)
  diag.used("events." .. name)
  diag.expected[#diag.expected + 1] = name

  register(function(e)
    if diag.seen[name] then return end

    local ok, detail = pcall(describe, e)
    diag.seen[name] = ok and tostring(detail) or "fired"
    log.info(("[diag] event %s -- %s"):format(name, diag.seen[name]))
  end)
end

--- Removes whatever the suite left in the world. Called at the end of the world
--- checks and again by `/diag cleanup`, so a run that failed partway does not leave
--- livestock standing at the origin.
function diag.cleanup()
  local cleared = 0

  for _, id in ipairs(diag.spawned) do
    if pcall(moontweaks.entities.despawn, id) then cleared = cleared + 1 end
  end

  diag.spawned = {}
  return cleared
end

--- What the checklist still has nothing against, in order.
function diag.untouched()
  local missing = {}

  for _, name in ipairs(diag.surface) do
    if not diag.touched[name] then missing[#missing + 1] = name end
  end

  return missing
end

--- The whole picture, as lines. Handed back rather than logged, so the same figures
--- reach the log at startup and a player's screen through `/diag`.
function diag.lines()
  local out = {}
  local function say(line) out[#out + 1] = line end

  local missing = diag.untouched()
  local covered = #diag.surface - #missing
  local counts = diag.tally()
  local failures = diag.failures()

  say(("%d passed, %d failed, %d skipped"):format(counts.pass, counts.fail, counts.skip))
  say(("%d of %d bound functions exercised"):format(covered, #diag.surface))

  if #failures > 0 then
    say(("%d failure(s):"):format(#failures))
    for _, failure in ipairs(failures) do say("  " .. failure) end
  end

  if #missing > 0 then
    say(("%d not yet exercised: %s"):format(#missing, table.concat(missing, ", ")))
  end

  local quiet = {}
  for _, name in ipairs(diag.expected) do
    if not diag.seen[name] then quiet[#quiet + 1] = name end
  end

  say(("%d of %d watched events have fired"):format(#diag.expected - #quiet, #diag.expected))
  if #quiet > 0 then
    say("  not yet seen: " .. table.concat(quiet, ", "))
  end

  return out
end

--- Puts the picture in the log under a heading, which is what each phase ends with.
function diag.report(phase)
  log.info(("[diag] ---- %s ----"):format(phase))
  for _, line in ipairs(diag.lines()) do log.info("[diag] " .. line) end
end

-- The world comes up shortly after the scripts have run, and this is the signal.
-- Everything `diag.later` collected runs here, in the order the files declared it.
moontweaks.events.worldgenStartup(function()
  log.info("[diag] world is up; running the checks that were waiting for it")
  diag.run(diag.deferred)
  diag.cleanup()
  diag.report("world")
end)
