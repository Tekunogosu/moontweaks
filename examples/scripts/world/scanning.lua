-- Looking through a box of blocks without paying for every one.
--
-- The obvious way to find something in an area is a loop calling `world.blockAt` on
-- each position. That crosses from Lua into the mod once per block, and a 32-block
-- cube holds 32768 of them. `findBlocks` and `countBlocks` do the same walk inside
-- the game and cross once, however large the box.
--
-- Both take a box as two opposite corners. Either may be the lower one — the box is
-- worked out from both, so a script building one from two remembered positions does
-- not have to sort them.
--
-- Two things worth knowing. Chunks that are not loaded are stepped over, so a box
-- reaching past the loaded world answers for the part that is loaded. And the search
-- stops once it has `limit` matches, which defaults to 4096; a box holding more is
-- not read to the end.

local commands = moontweaks.commands
local players  = moontweaks.players
local world    = moontweaks.world

-- The box of a given radius around a player, built whole rather than filled in
-- afterwards, so every caller hands `findBlocks` a complete region.
local function around(player, radius, code, limit)
  local at = players.position(player)
  return {
    x = math.floor(at.x - radius),
    y = math.floor(at.y - radius),
    z = math.floor(at.z - radius),
    toX = math.floor(at.x + radius),
    toY = math.floor(at.y + radius),
    toZ = math.floor(at.z + radius),
    code = code,
    limit = limit,
  }
end

-- Counting asks a number of the game and gets a number back. Nothing is built for
-- each match, which is what makes this the one to use when the answer is "how many".
commands.add {
  name = "prospect",
  description = "Count the ore in a box around you",
  requiresPlayer = true,
  args = {
    { name = "radius", type = "int", optional = true },
  },
  handler = function(e)
    local radius = math.min(e.args.radius or 16, 64)

    local found = world.countBlocks(around(e.player, radius, "game:ore-*"))
    if found == 0 then
      return ("No ore within %d blocks."):format(radius)
    end

    return ("%d ore block(s) within %d."):format(found, radius)
  end,
}

-- Finding hands back where each one is, which is what feeds a highlight. The limit is
-- lowered on purpose: a highlight of ten thousand boxes helps nobody, and the search
-- stops as soon as it has enough rather than reading the rest of the box.
commands.add {
  name = "showore",
  description = "Outline the ore around you",
  requiresPlayer = true,
  handler = function(e)
    local found = world.findBlocks(around(e.player, 12, "game:ore-*", 64))

    local blocks = {}
    for i, ore in ipairs(found) do
      blocks[i] = { x = ore.x, y = ore.y, z = ore.z }
    end

    -- An empty list under the same slot is how the drawing is taken back, so running
    -- this where there is no ore clears the last one rather than leaving it behind.
    world.highlight {
      player = e.player,
      slot = 7,
      blocks = blocks,
      colour = { red = 255, green = 190, blue = 40, alpha = 160 },
    }

    if #found == 0 then
      return "No ore nearby. Cleared the last outline."
    end

    return ("Outlined %d ore block(s). Run it again anywhere empty to clear it."):format(#found)
  end,
}

-- A box with no `code` counts every block in it, air included, which is how a volume
-- is measured rather than searched.
commands.add {
  name = "roomsize",
  description = "Say how much air is in the box around you",
  requiresPlayer = true,
  handler = function(e)
    local counted = world.countBlocks(around(e.player, 8, "game:air", 100000))
    return ("%d block(s) of air within 8."):format(counted)
  end,
}
