-- The one command the suite adds, for taking the readings again under conditions a
-- startup cannot arrange: a server with players on it, chunks loaded around
-- somebody, and whatever else is running at the time.
--
-- Every reading it takes replaces the standing one of the same name, so the report
-- always says what this server does now rather than what it did while it was empty.

local commands = moontweaks.commands
local players  = moontweaks.players
local log      = moontweaks.log

--- Says a block of lines to the log and hands the same block back as one message,
--- which is what the client shows in the chat.
local function tell(lines)
  for _, line in ipairs(lines) do log.info("[perf] " .. line) end
  return table.concat(lines, "\n")
end

commands.add {
  name = "perf",
  description = "Measure what MoonTweaks costs on this server",
  privilege = "controlserver",

  handler = function()
    return tell(perf.lines())
  end,

  subcommands = {
    {
      name = "calls",
      description = "Take the crossing and interpreter readings again",
      privilege = "controlserver",
      handler = function()
        perf.calls()
        perf.interpreter()
        return tell(perf.lines())
      end,
    },

    {
      name = "world",
      description = "Take the block readings again, in the chunks around you",
      privilege = "controlserver",
      requiresPlayer = true,
      handler = function(e)
        local at = players.position(e.player)

        -- Whoever asked is standing in loaded chunks by definition, which is the one
        -- thing these readings need. The box goes up well above their head.
        perf.world { x = math.floor(at.x), y = math.floor(at.y), z = math.floor(at.z) }
        return tell(perf.lines())
      end,
    },
  },
}
