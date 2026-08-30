-- What the world remembers.
--
-- There are three places a script may put something, and which to use is decided by
-- what the thing is about rather than by which is convenient.
--
--   world.setData             about the world, not about anybody in particular.
--                             Saved with the save game. The home for a total, a
--                             running tally, a fact no single player owns.
--
--   players.setWorldData      about one player in this world. Saved with the save
--                             game too, so a second world keeps its own and deleting
--                             a world takes it with it. Needs the player to be here.
--
--   players.setAccountData    about the person rather than their game. Saved beside
--                             the ban and whitelist rolls, so every world this server
--                             runs reads the same value and it survives the world
--                             being deleted. Answers for a player who is offline.
--
-- All three survive a restart and take any value a script can write, a table
-- included. `players/account-data.lua` is about the third and the difference between
-- it and the second; this file is about the first.
--
-- None of them exists until there is a world, so all belong in a handler rather than
-- in a script's body — and this one says so plainly rather than failing oddly if you
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
