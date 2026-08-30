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

-- The goal: sleeping in a bed becomes where you wake up. Vintage Story does not do
-- this on its own, so the last bed a player used becomes their spawn.
events.didUseBlock(function(e)
  if e.block and e.block:find("bed") then
    players.setSpawn(e.player, e.x, e.y, e.z)
    players.say(e.player, "Your spawn is now this bed.")
  end
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

