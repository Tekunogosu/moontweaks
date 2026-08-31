-- What a player and an administrator can ask of the system.
--
-- Two commands rather than one with a privileged branch: a subcommand inherits the
-- privilege of the command it sits under, so an administrators' branch under a
-- command every player may type would be open to every player.

rpglevels = rpglevels or {}

local commands = moontweaks.commands
local players  = moontweaks.players

--- The next level at which a reward is handed over, or nil for somebody at the cap.
---@param level integer
---@return integer|nil
local function nextMilestone(level)
  local config = rpglevels.config
  local milestone = level + config.rewardEvery - level % config.rewardEvery

  if milestone > config.maxLevel then return nil end

  return milestone
end

--- How a player's standing reads back to them.
---@param player string
---@return string
local function standingOf(player)
  local progress = rpglevels.progress
  local standing = progress.of(player)
  local needed = progress.xpToNext(standing.level)

  if not needed then
    return ("Level %d. You are as high as this world goes."):format(standing.level)
  end

  local milestone = nextMilestone(standing.level)
  local line = ("Level %d — %d of %d experience towards level %d.")
    :format(standing.level, standing.xp, needed, standing.level + 1)

  if milestone then
    line = line .. (" Your next reward comes at level %d."):format(milestone)
  end

  return line
end

commands.add {
  name = "rpg",
  description = "Say what level you are and how far the next one is",
  requiresPlayer = true,
  handler = function(e)
    return standingOf(e.player)
  end,
}

commands.add {
  name = "rpgadmin",
  description = "Adjust what a player has earned",
  privilege = "controlserver",
  subcommands = {
    {
      name = "xp",
      description = "Grant a player experience, levelling them as it lands",
      args = {
        { name = "who", type = "player" },
        { name = "amount", type = "int" },
      },
      handler = function(e)
        if e.args.amount <= 0 then
          return { error = "Grant a positive amount; nothing here takes experience away." }
        end

        local gained = rpglevels.levelling.award(e.args.who, e.args.amount)

        return ("Granted %d experience to %s, worth %d level(s). %s")
          :format(e.args.amount, players.name(e.args.who), gained, standingOf(e.args.who))
      end,
    },
    {
      name = "show",
      description = "Say what level a player is",
      args = {
        { name = "who", type = "player" },
      },
      handler = function(e)
        return ("%s: %s"):format(players.name(e.args.who), standingOf(e.args.who))
      end,
    },
    {
      name = "reset",
      description = "Put a player back to level one with nothing banked",
      args = {
        { name = "who", type = "player" },
      },
      handler = function(e)
        rpglevels.progress.remember(e.args.who, { level = 1, xp = 0 })

        return ("%s is back to level 1."):format(players.name(e.args.who))
      end,
    },
  },
}
