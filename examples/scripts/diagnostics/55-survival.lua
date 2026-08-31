-- The three modules that reach inside another mod rather than the game's own API.
--
-- Weather, temporal stability and block reinforcement are the survival content's own
-- systems. A server without those mods has none of them, and every function here
-- fails on such a server naming the mod it wanted — so each group is registered only
-- where its system is loaded, and recorded as skipped where it is not. A skip here
-- means "this server does not have that mod", not "the binding is broken".
--
-- This is also the suite to run after a game update. Those systems are another mod's
-- internals rather than versioned API, so a rename shows up as a failure here before
-- it shows up on somebody's server. `MODSYSTEMS.md` lists what each of them calls.

local reinforce = moontweaks.reinforce
local stability = moontweaks.stability
local weather   = moontweaks.weather

--- Says a module is absent rather than broken, in the words the report should use.
local function without(mod)
  return ("this server has no '%s' mod, so the module answers nothing"):format(mod)
end

--- Registers checks against a system, or records them all as skipped where the
--- system is not loaded. Asked once and answered once, so a run cannot report a
--- module as both present and absent.
local function group(available, mod, register)
  local defer = available
    and function(name, fn) diag.later(name, fn) end
    or function(name) diag.skip(name, without(mod)) end

  local grounded = available
    and function(name, fn) diag.onPlayer(name, fn) end
    or function(name) diag.skip(name, without(mod)) end

  register(defer, grounded)
end

--- Where the checks read from, or a failure saying why there is nowhere.
---@return Spot
local function spot()
  local at = diag.loadedSpot()
  assert(at, "the server is holding no chunk near spawn, so there is nowhere to read")

  return at
end

-- ## Weather
--
-- Everything here needs the world up: a weather reading taken while the server is
-- still loading has no region to take it from.

diag.check("weather.available", function()
  return weather.available() and "the weather system is loaded" or "no weather system on this server"
end)

group(weather.available(), "game", function(later)
  later("weather.precipitation", function()
    local at = spot()
    local level = weather.precipitation(at.x, at.y, at.z)
    assert(level >= 0, "precipitation read below zero: " .. tostring(level))

    return ("%.2f coming down at %d %d %d"):format(level, at.x, at.y, at.z)
  end)

  later("weather.falling", function()
    local at = spot()
    local falling = weather.falling(at.x, at.y, at.z)
    assert(falling.kind, "the weather named nothing that could fall")

    return ("%s at %.2f, drops of %.2f"):format(falling.kind, falling.level, falling.size)
  end)

  later("weather.wetness", function()
    local at = spot()
    return ("%.3f wet over the last day"):format(weather.wetness(at.x, at.y, at.z, 1))
  end)

  -- Overriding the sky is checked as a round trip, so a run leaves the weather
  -- running itself however it found it. Nothing is left held: a server whose sky
  -- stayed stuck open after a diagnostic run would be the worse bug.
  later("weather.setPrecipitation", function()
    local before = weather.overridden()

    weather.setPrecipitation(0.5)
    local held = weather.overridden()

    weather.clearPrecipitation()
    local cleared = weather.overridden()

    if before then weather.setPrecipitation(before) end

    diag.used("weather.clearPrecipitation", "weather.overridden")
    assert(held == 0.5, "the override did not take: " .. tostring(held))
    assert(cleared == nil, "clearing left " .. tostring(cleared))

    return ("held at %s, cleared, back to %s"):format(tostring(held), tostring(weather.overridden()))
  end)

  -- Lightning is a flash and a noise rather than a strike, so calling it down over
  -- the spawn breaks nothing and sets nothing alight. Anyone standing there sees it.
  later("weather.lightning", function()
    local at = spot()
    weather.lightning(at.x, at.y + 30, at.z)

    return ("called a flash down over %d %d %d"):format(at.x, at.y + 30, at.z)
  end)
end)

-- ## Temporal stability

diag.check("stability.available", function()
  return stability.available() and "the stability system is loaded" or "no stability system here"
end)

group(stability.available(), "survival", function(later)
  later("stability.at", function()
    local at = spot()
    local sound = stability.at(at.x, at.y, at.z)
    assert(sound >= 0, "stability read below zero: " .. tostring(sound))

    -- A world with temporal stability turned off answers 2 everywhere, which is above
    -- anything a world with it on ever reads, so the two are told apart here.
    local off = sound >= 2 and " (temporal stability is off in this world)" or ""
    return ("%.2f at %d %d %d%s"):format(sound, at.x, at.y, at.z, off)
  end)

  later("stability.storm", function()
    local storm = stability.storm()
    assert(storm.strength, "the storm data named no strength")

    if storm.active then
      return ("a %s storm is running, glitch %.2f"):format(storm.strength, storm.glitch)
    end
    return ("no storm; the next is %s, due on day %.1f"):format(storm.strength, storm.nextDay)
  end)
end)

-- ## Block reinforcement
--
-- Reinforcing needs a block the game lets a player reinforce and a player to own it,
-- so the writing check waits for `/diag player`. Reading needs neither.

diag.check("reinforce.available", function()
  return reinforce.available() and "the reinforcement system is loaded" or "no reinforcement here"
end)

group(reinforce.available(), "survival", function(later, onPlayer)
  later("reinforce.at", function()
    local at = spot()
    local held = reinforce.at(at.x, at.y, at.z)

    diag.used("reinforce.isReinforced")
    assert(reinforce.isReinforced(at.x, at.y, at.z) == (held ~= nil),
      "isReinforced and at disagree about the same block")

    if not held then return "nothing protects the ground at spawn, which is an answer" end
    return ("%d strength, belonging to %s"):format(held.strength, tostring(held.playerName))
  end)

  -- The whole round trip on one block: put a wall up, protect it, read the protection
  -- back, wear it down, take it off and take the wall down again. Planks are used
  -- because the game lets them be reinforced, which the ground underfoot may not be.
  onPlayer("reinforce.strengthen", function(who)
    local ground = spot()
    local at = { x = ground.x, y = ground.y + 8, z = ground.z }
    local before = moontweaks.world.blockAt(at.x, at.y, at.z) or "game:air"

    moontweaks.world.setBlock("game:planks-oak-ud", at.x, at.y, at.z)

    local put = reinforce.strengthen(at.x, at.y, at.z, who, 50)
    local held = put and reinforce.at(at.x, at.y, at.z) or nil
    local locked = reinforce.isLockedFor(at.x, at.y, at.z, who)

    if put then reinforce.consume(at.x, at.y, at.z, 20) end
    local worn = put and reinforce.at(at.x, at.y, at.z) or nil

    reinforce.clear(at.x, at.y, at.z)
    local after = reinforce.at(at.x, at.y, at.z)
    moontweaks.world.setBlock(before, at.x, at.y, at.z)

    diag.used("reinforce.consume", "reinforce.clear", "reinforce.isLockedFor")
    assert(put, "planks could not be reinforced; the block may have no reinforcement behaviour")
    assert(held and held.strength == 50, "the reinforcement did not take")
    assert(worn and worn.strength == 30,
      ("wearing 20 off 50 left %s"):format(tostring(worn and worn.strength)))
    assert(not after, "clearing left the block protected")

    return ("50 strength on, worn to %d, cleared; locked for you: %s")
      :format(worn.strength, tostring(locked))
  end)
end)
