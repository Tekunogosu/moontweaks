-- Gating a recipe on something the recipe itself cannot say: who is crafting, where
-- they are standing, or what the server has decided about them.
--
-- This is not the same as removing a recipe. `moontweaks.recipes.grid.remove` takes a
-- recipe out of the game for everybody and out of the handbook with it. These events
-- leave the recipe where it is and answer, per attempt, whether this arrangement by
-- this player makes it — so the recipe stays discoverable and stays craftable by
-- anybody the handler does not refuse.
--
-- Both events require an output code as their first argument. That is not a
-- convenience: the game asks these once per candidate recipe every time somebody
-- moves an item in a crafting grid, and the code named here is matched in the mod
-- itself before any of this file runs. A handler is entered only for the recipes it
-- asked about, so watching one recipe costs the server a code comparison per
-- candidate rather than a call into Lua.
--
-- A handler refuses and cannot permit. Returning `false` stops a recipe that would
-- otherwise have been made; returning anything else, or nothing, leaves the game to
-- decide for itself. The game asks this before it has checked the ingredients against
-- the recipe at all, so there is no way from here to make an arrangement produce
-- something it does not match.

local events  = moontweaks.events
local players = moontweaks.players
local log     = moontweaks.log

-- A handler is asked while ingredients are being arranged, not when the result is
-- taken. It is asked again for every rearrangement, so it must not be treated as
-- somebody having crafted something: nothing is consumed by answering, and counting
-- here would count the same attempt many times over.

-- Rule one: metal tools are for those the server has trusted. The recipes stay in
-- everybody's handbook, so a new player can see what they are working towards.
events.matchesGridRecipe("game:axe-*", function(e)
  if e.player ~= nil and not players.hasPrivilege(e.player, "commandplayer") then
    return false
  end
end)

-- The filter takes a `*` wildcard, so one handler covers a family. Without it this
-- would need one registration per metal.
events.matchesGridRecipe("game:pickaxe-*", function(e)
  if e.player ~= nil and not players.hasPrivilege(e.player, "commandplayer") then
    return false
  end
end)

-- Rule two: bread is baked where there is a baker. `ingredients` and `gridWidth`
-- describe the arrangement, so a handler can read what is actually laid out rather
-- than only what it makes.
events.matchesGridRecipe("game:bread-*", function(e)
  -- The game names nobody when something other than a player asks, and every
  -- players.* call refuses an identifier naming nobody, so guard before asking.
  if e.player == nil then return end

  -- Below the surface is no place for a bakery.
  if players.position(e.player).y < 60 then
    players.warn(e.player, "It is too cold down here for bread to rise.")
    return false
  end
end)

-- The non-grid kinds — barrels, anvils, clay forms, knapping surfaces — are the same
-- shape under a different name, so a rule that spans both is written twice and reads
-- the same either way.
events.matchesRecipe("game:ingot-*", function(e)
  if e.player ~= nil and not players.hasPrivilege(e.player, "commandplayer") then
    return false
  end
end)

-- An empty slot in the arrangement reads as an empty string rather than as nil, so
-- the list keeps its length and its gaps. A nil would have cut it short at the first
-- hole, which in a crafting grid is nearly always.
events.matchesGridRecipe("game:firestarter", function(e)
  local held = 0
  for _, code in ipairs(e.ingredients) do
    if code ~= "" then held = held + 1 end
  end

  log.info(("%s is arranging %d ingredient(s) across %d column(s) for %s")
    :format(e.playerName or "somebody", held, e.gridWidth, e.output))
end)
