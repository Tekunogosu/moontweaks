-- Events run while the server is playing, rather than while it is loading. A script
-- registers a handler once at startup and the handler is called for as long as the
-- server runs, so the interpreter that read this file stays alive behind it.
--
-- A handler that throws is logged with the line that registered it and is not called
-- again: these run inside the game's own dispatch, where an error would otherwise
-- take down whatever raised it.
--
-- Every handler is given one table describing what happened, and every event names
-- the shape of its own, so an editor completes `e` and says what each key holds.

local events  = moontweaks.events
local players = moontweaks.players
local world   = moontweaks.world

--- Whether a code names a bed, of which this world holds one per wood and per half.
--- Matched on the code rather than on a tag, because the game gives beds none.
local function isBed(code)
  return code ~= nil and code:find(":bed%-") ~= nil
end

--- Whether a position stands in the block a vector points into. Spawns are answered
--- as block centres, so 100 reads back as 100.5 and has to be floored to compare.
local function isAt(where, x, y, z)
  return where ~= nil
    and math.floor(where.x) == x
    and math.floor(where.y) == y
    and math.floor(where.z) == z
end

-- The goal: sleeping in a bed becomes where you wake up. Vintage Story does not do
-- this on its own, so the last bed a player used becomes their spawn.
events.didUseBlock(function(e)
  if isBed(e.block) then
    players.setSpawn(e.player, e.x, e.y, e.z)
    players.say(e.player, "Your spawn is now this bed.")
  end
end)

-- And the other half of the rule: a spawn lasts only as long as the bed holding it.
-- Breaking a bed takes both of its blocks with it, so what settles this is whether a
-- bed still stands where the player would wake — not which half they swung at, and
-- not a position kept alongside the game's own.
--
-- `spawn` answers with the first of the four spawns the game holds, so somebody who
-- has never slept anywhere is answered with the world's. That is the one case to step
-- over: they have no bed to lose, and clearing would tell them otherwise.
events.didBreakBlock(function(e)
  if not isBed(e.block) then return end

  local waking = players.spawn(e.player)
  if not waking then return end

  local x, y, z = math.floor(waking.x), math.floor(waking.y), math.floor(waking.z)
  if isAt(world.spawn(), x, y, z) then return end   -- the world's spawn, not a bed
  if isBed(world.blockAt(x, y, z)) then return end  -- their own bed still stands

  players.clearSpawn(e.player)
  players.say(e.player, "Your bed is gone. You will wake at the world spawn.")
end)

-- A code inside a handler is only a string. Unlike `items.set`, which refuses a code
-- the server does not have and names the line, nothing here can be checked before it
-- runs — so a misspelled code never matches and the handler quietly does nothing.
-- Logging what actually arrived is the quickest way to learn what to write.
events.didBreakBlock(function(e)
  moontweaks.log.info(("%s broke %s at %d %d %d")
    :format(e.playerName, e.block or "nothing", e.x, e.y, e.z))
end)

-- Placing completes the three. `block` is what now stands there and `replaced` is
-- what it went over, which is `game:air` wherever nothing was — so the two together
-- say whether something was built on empty ground or over something else.
events.didPlaceBlock(function(e)
  if e.replaced == "game:air" or not e.replaced then return end

  players.say(e.player, ("You built over %s."):format(e.replaced))
end)

-- Changing which slot is held says who did it rather than which slot, because what is
-- in their hand is the useful part and the inventory module answers that.
events.playerChangeSlot(function(e)
  local held = moontweaks.inventory.held(e.player)
  if held and held.code:find("^game:torch") then
    players.say(e.player, "Mind the thatch.")
  end
end)

