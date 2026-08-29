-- Taking blocks away, and the three ways of doing it that are not the same thing.
--
--   setBlock to game:air   removes it. Nothing drops, nothing sounds, and whatever
--                          stood in it is gone. This is what clearing ground wants.
--   breakBlock             breaks it as a player would: its drops land, its sound
--                          plays, and its neighbours are told to check themselves.
--   exchangeBlock          swaps it for another and leaves what stands in it alone,
--                          so a chest keeps what was inside.

local events  = moontweaks.events
local players = moontweaks.players
local world   = moontweaks.world

-- Harvesting: break the block above properly, so it pays out as breaking it would.
events.didUseBlock(function(e)
  if e.block ~= "game:sign-ground-north" then return end

  world.breakBlock { x = e.x, y = e.y + 1, z = e.z, player = e.player }
end)

-- Crediting a player matters: the game takes their tool and their privileges into
-- account, so a drop that only comes out for a knife only comes out if they hold one.
-- `dropMultiplier` scales what lands — two is twice the usual, and zero breaks the
-- block properly without paying anything out at all.
events.didBreakBlock(function(e)
  if e.block ~= "game:rock-granite" then return end

  world.breakBlock { x = e.x, y = e.y - 1, z = e.z, player = e.player, dropMultiplier = 2 }
end)

-- Swapping without disturbing. The same chest written with `setBlock` would come
-- back empty, because that writes a new block where the old one was.
events.didUseBlock(function(e)
  if e.block ~= "game:chest-east" then return end

  world.exchangeBlock("game:chest-north", e.x, e.y, e.z)
  players.say(e.player, "The chest turns to face you, and keeps what is in it.")
end)

-- Acting away from a player means asking first. Writing into a chunk nobody has
-- loaded does nothing and says nothing about it, so anything at a distance checks.
--
-- `surfaceAt` reads the height of a column from the map the world already keeps,
-- which is one call where looking down block by block would be a hundred.
moontweaks.commands.add {
  name = "clearabove",
  description = "Take the column above you down to the sky",
  privilege = "controlserver",
  requiresPlayer = true,
  handler = function(e)
    local at = players.position(e.player)
    local x, z = math.floor(at.x), math.floor(at.z)

    if not world.isLoaded(x, math.floor(at.y), z) then
      return { error = "that ground is not loaded." }
    end

    local ground = world.surfaceAt(x, z)
    if not ground then return { error = "that column has no surface yet." } end

    local cleared = 0
    for y = ground, ground + 30 do
      if world.blockAt(x, y, z) ~= "game:air" then
        world.queueBlock("game:air", x, y, z)
        cleared = cleared + 1
      end
    end

    -- Nothing above has touched the world yet; this is where it all lands, at one
    -- relight and one send per chunk rather than one per block.
    world.commit()
    return ("Cleared %d block(s) above %d %d."):format(cleared, x, z)
  end,
}
