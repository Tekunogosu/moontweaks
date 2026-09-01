-- Land claims: reading them, making one and taking it back.
--
-- A claim is the game's own protection rather than this mod's, and the checks here
-- are a round trip through it: claim a box nobody is standing in, read it back at a
-- block inside it, confirm the number it was given names it, and remove it again.
-- Somebody running this ends with exactly the claims they started with.
--
-- These wait for a world, because a claim is indexed by map region and the regions
-- are not there while the assets load.

local claims = moontweaks.claims

--- Somewhere in this world nobody is likely to have claimed. Worked out rather than
--- written down: a fixed coordinate is outside a small world and inside somebody's
--- base in a large one. Anchored on the world spawn, pushed a thousand blocks off it,
--- and clamped back inside the map so the offset cannot walk off the edge.
---
--- Worked out on first use rather than here, because the checks run against a world
--- and a script's body does not have one: `world.spawn` resolves against the terrain
--- map, which is not there while the assets are still loading.
local plot
local function testPlot()
  if plot then return plot end

  local map  = moontweaks.server.info()
  local at   = moontweaks.world.spawn()
  assert(at, "this world worked out no spawn to measure a test plot from")

  local function inside(value, limit)
    return math.max(0, math.min(math.floor(value) + 1000, limit - 64))
  end

  plot = { x = inside(at.x, map.mapSizeX), y = 0, z = inside(at.z, map.mapSizeZ), size = 8 }
  return plot
end

diag.onPlayer("claims.add", function(who)
  local at = testPlot()
  local number = claims.add {
    owner = who,
    x = at.x, y = at.y, z = at.z,
    toX = at.x + at.size, toY = at.y + 64, toZ = at.z + at.size,
    description = "MoonTweaks diagnostics",
  }

  assert(number >= 0, ("a new claim was given the number %d"):format(number))

  return ("claimed a test plot, given the number %d"):format(number)
end)

diag.onPlayer("claims.of", function(who)
  local held = claims.of(who)
  assert(#held > 0, "the claim just added was not listed among its owner's")

  local last = held[#held]
  assert(last.owner == who, "a claim came back owned by somebody else")
  assert(last.index == #held - 1,
    ("the last claim is numbered %d among %d claim(s)"):format(last.index, #held))
  assert(#last.areas > 0, "a claim came back covering nothing")

  return ("%d claim(s), the last of them numbered %d"):format(#held, last.index)
end)

diag.onPlayer("claims.at", function(who)
  local at = testPlot()
  -- A block inside the plot, rather than its corner: a corner is shared with the
  -- ground outside it and says less about whether the box is really held.
  local inside = claims.at(at.x + 1, at.y + 1, at.z + 1)

  local mine
  for _, claim in ipairs(inside) do
    if claim.owner == who and claim.description == "MoonTweaks diagnostics" then
      mine = claim
    end
  end

  assert(mine, "the claim just added does not cover a block inside its own box")

  return ("%d claim(s) cover that block, one of them the test plot at number %d")
    :format(#inside, mine.index)
end)

diag.onPlayer("claims.remove", function(who)
  local at = testPlot()
  local held = claims.of(who)

  local number
  for _, claim in ipairs(held) do
    if claim.description == "MoonTweaks diagnostics" then number = claim.index end
  end

  assert(number, "the test plot was gone before it could be removed")
  assert(claims.remove(who, number), "removing the test plot reported nothing removed")

  -- The point of the check: what it says it removed is actually gone.
  local left = claims.at(at.x + 1, at.y + 1, at.z + 1)
  for _, claim in ipairs(left) do
    assert(claim.description ~= "MoonTweaks diagnostics",
      "the test plot still covers its own box after being removed")
  end

  return ("removed claim %d, and it no longer covers its box"):format(number)
end)
