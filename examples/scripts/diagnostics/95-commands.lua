-- The one command the suite adds, and the checks that only a person can start.
--
-- Registering it is itself the check on `commands.add`: a command the server refuses
-- to take fails on the line that declares it. Everything under it exists because
-- something in the API needs a player standing in the world, which no event at
-- startup can arrange.
--
-- Every subcommand answers on the screen of whoever typed it and writes the same
-- lines into the log, so a run can be read afterwards without anybody having to
-- copy anything down.

local commands  = moontweaks.commands
local inventory = moontweaks.inventory
local log       = moontweaks.log
local players   = moontweaks.players
local world     = moontweaks.world

--- Says a block of lines to the log and hands the same block back as one message,
--- which is what the client shows in the chat.
local function tell(lines)
  for _, line in ipairs(lines) do log.info("[diag] " .. line) end
  return table.concat(lines, "\n")
end

diag.check("commands.add", function()
  commands.add {
    name = "diag",
    description = "Check which parts of the MoonTweaks API answer on this server",
    privilege = "controlserver",

    -- Bare `/diag` reports; each subcommand runs a group of checks that needs
    -- somebody standing in the world and then reports again.
    handler = function()
      return tell(diag.lines())
    end,

    subcommands = {
      {
        name = "report",
        description = "Say what has passed, failed and gone untouched so far",
        privilege = "controlserver",
        handler = function()
          return tell(diag.lines())
        end,
      },

      {
        name = "player",
        description = "Run every check that needs a player, on you",
        privilege = "controlserver",
        requiresPlayer = true,
        handler = function(e)
          log.info(("[diag] running the player checks on %s"):format(e.playerName))
          diag.run(diag.grounded, e.player)
          return tell(diag.lines())
        end,
      },

      {
        name = "world",
        description = "Run the world checks again, in the chunks around you",
        privilege = "controlserver",
        requiresPlayer = true,
        handler = function()
          log.info("[diag] running the world checks again")
          diag.run(diag.deferred)
          diag.cleanup()
          return tell(diag.lines())
        end,
      },

      {
        name = "events",
        description = "Say which watched events have fired and which are still quiet",
        privilege = "controlserver",
        handler = function()
          local lines = {}

          for _, name in ipairs(diag.expected) do
            lines[#lines + 1] = ("%-22s %s"):format(name, diag.seen[name] or "-- not yet")
          end

          return tell(lines)
        end,
      },

      {
        name = "container",
        description = "Check the slot functions against the container you are looking at",
        privilege = "controlserver",
        requiresPlayer = true,
        handler = function(e)
          local at = players.looking(e.player)
          if not at then return { error = "look at a container within reach and try again." } end

          local where = { x = at.x, y = at.y, z = at.z }
          local ok, slots = pcall(inventory.size, where)
          if not ok then return { error = ("%s holds nothing with slots in it."):format(at.block) } end

          local lines = { ("%s at %d %d %d has %d slot(s)"):format(at.block, at.x, at.y, at.z, slots) }
          local held = inventory.list(where)
          local filled = 0
          for _, slot in ipairs(held) do
            if slot.code and slot.quantity > 0 then filled = filled + 1 end
          end

          -- Emptying is only ever run against a container that is already empty:
          -- the call is exercised, the answer is checked against what was there,
          -- and nothing anybody owns is thrown away.
          if filled > 0 then
            diag.skip("inventory.clear",
              ("%s holds %d filled slot(s); empty it first to check clearing"):format(at.block, filled))
            lines[#lines + 1] = "skipped clearing: empty the container first"
            return tell(lines)
          end

          diag.check("inventory.clear", function()
            local cleared = inventory.clear(where)
            assert(cleared == 0, ("cleared %d slot(s) from an empty container"):format(cleared))
            return "emptied an already empty container, which took nothing"
          end)

          lines[#lines + 1] = "checked clearing against an empty container"
          return tell(lines)
        end,
      },

      {
        name = "cleanup",
        description = "Take back anything the suite left in the world",
        privilege = "controlserver",
        requiresPlayer = true,
        handler = function(e)
          local cleared = diag.cleanup()

          -- An empty list under the same slot is how a drawing is taken back.
          world.highlight { player = e.player, slot = 61, blocks = {} }

          return tell({ ("despawned %d leftover entity(s) and cleared the outline"):format(cleared) })
        end,
      },
    },
  }

  return "registered /diag with report, player, world, events, container and cleanup"
end)
