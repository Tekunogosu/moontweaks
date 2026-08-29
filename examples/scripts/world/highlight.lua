-- Drawing on somebody's screen from the server.
--
-- This is the one thing a server-side script can put in front of a player's eyes.
-- Nothing needs installing on their machine: highlighting is a facility the game
-- already ships for its own area-selection tools, and a server may point it at
-- whatever it likes.
--
-- A slot holds one set of blocks until it is given another, so a script drawing under
-- its own slot number replaces or clears its own drawing without disturbing anybody
-- else's. Passing an empty list of blocks is how a drawing is taken back.

local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players
local world    = moontweaks.world

-- A number nothing else here uses. The game's own tools take the low slots, so a
-- script picks one of its own and stays with it.
local SLOT = 71

-- Show somebody the ground their next build would sit on.
commands.add {
  name = "showplot",
  description = "Outline the ground around you",
  requiresPlayer = true,
  args = { { name = "radius", type = "int", optional = true } },
  handler = function(e)
    local at = players.position(e.player)
    local radius = math.min(e.args.radius or 4, 12)
    local x, z = math.floor(at.x), math.floor(at.z)

    local corners = {}
    for dx = -radius, radius do
      for dz = -radius, radius do
        -- The edge alone: a filled square would be a wall of boxes to see through.
        if math.abs(dx) == radius or math.abs(dz) == radius then
          local ground = world.surfaceAt(x + dx, z + dz)
          if ground then
            corners[#corners + 1] = { x = x + dx, y = ground - 1, z = z + dz }
          end
        end
      end
    end

    world.highlight {
      player = e.player,
      slot = SLOT,
      blocks = corners,
      colour = { red = 90, green = 200, blue = 120, alpha = 100 },
    }

    return ("Outlined %d block(s). Run /showplotclear to take it back.")
      :format(#corners)
  end,
}

commands.add {
  name = "showplotclear",
  description = "Take back the outline drawn by /showplot",
  requiresPlayer = true,
  handler = function(e)
    -- An empty list under the same slot is what clears it.
    world.highlight { player = e.player, slot = SLOT, blocks = {} }
    return "Outline cleared."
  end,
}

-- Highlights are drawn for one player and live on their client, so a player who
-- leaves takes theirs with them and there is nothing to clean up.
events.playerDisconnect(function(e)
  moontweaks.log.info(e.playerName .. " left; their highlights went with them")
end)
