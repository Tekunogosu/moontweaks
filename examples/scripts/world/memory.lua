-- What the world remembers.
--
-- `players.setData` stores against one player and is saved with them.
-- `world.setData` stores against the save game itself, so it is the home for
-- anything counted across everybody rather than for each of them: a total, a
-- running tally, a fact about the world that no single player owns.
--
-- Both survive a restart. Both take any value a script can write, a table included.
-- Neither exists until there is a world, so both belong in a handler rather than in
-- a script's body — and this one says so plainly rather than failing oddly if you
-- try it from the body.

local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players
local world    = moontweaks.world

-- One tally for the whole server. Read, add, write: there is no increment, because a
-- stored value is whatever was last written.
events.didBreakBlock(function(e)
  if not e.block or not e.block:find("^game:ore%-") then return end

  local mined = world.getData("oreMined") or 0
  world.setData("oreMined", mined + 1)

  -- Every hundredth is worth telling everybody about.
  if (mined + 1) % 100 == 0 then
    players.announce(("%d pieces of ore have come out of this world.")
      :format(mined + 1))
  end
end)

-- A table rather than a number, which is stored just as readily. This keeps the
-- single best find rather than a running count.
events.didBreakBlock(function(e)
  if e.block ~= "game:rock-granite" then return end

  local deepest = world.getData("deepestDig")
  if deepest and deepest.y <= e.y then return end

  world.setData("deepestDig", {
    y = e.y,
    who = players.name(e.player),
    at = { x = e.x, z = e.z },
  })
end)

commands.add {
  name = "worldstats",
  description = "Say what this world has been remembering",
  handler = function()
    local mined = world.getData("oreMined") or 0
    local deepest = world.getData("deepestDig")

    if not deepest then
      return ("%d ore mined. Nobody has dug into the granite yet."):format(mined)
    end

    return ("%d ore mined. Deepest granite: %s at y=%d.")
      :format(mined, deepest.who, deepest.y)
  end,
}
