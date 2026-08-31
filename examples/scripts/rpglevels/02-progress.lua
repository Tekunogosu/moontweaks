-- A player's standing, and the arithmetic that moves it.
--
-- Nothing here talks to anybody or hands anything over. `advance` is a pure function
-- from one standing to the next, which is what makes the curve something that can be
-- read off and checked rather than traced through the code that announces it.

rpglevels = rpglevels or {}
rpglevels.progress = {}

local players = moontweaks.players
local progress = rpglevels.progress

--- What it takes to go from `level` to the one above it, or nil at the cap, where
--- there is no next level to buy.
---@param level integer
---@return integer|nil
function progress.xpToNext(level)
  local config = rpglevels.config
  if level >= config.maxLevel then return nil end

  return math.floor(config.xpBase * level ^ config.xpExponent)
end

--- A standing with `gained` experience applied, leaving the one it was given alone.
--- Experience exactly meeting the cost buys the level, and whatever is left over is
--- banked against the next one. At the cap there is nothing to bank, so it is dropped.
---@param standing table
---@param gained integer
---@return table
function progress.advance(standing, gained)
  local level, xp = standing.level, standing.xp + gained
  local needed = progress.xpToNext(level)

  while needed and xp >= needed do
    xp = xp - needed
    level = level + 1
    needed = progress.xpToNext(level)
  end

  if not needed then xp = 0 end

  return { level = level, xp = xp }
end

--- Where a player stands. Somebody who has never killed anything reads level 1.
---@param player string
---@return table
function progress.of(player)
  local stored = players.getWorldData(player, rpglevels.config.storageKey)
  if type(stored) ~= "table" then return { level = 1, xp = 0 } end

  return { level = stored.level or 1, xp = stored.xp or 0 }
end

--- Writes a standing back against the player, where it is saved with the world.
---@param player string
---@param standing table
function progress.remember(player, standing)
  players.setWorldData(player, rpglevels.config.storageKey, standing)
end

--- Whether reaching `level` is a milestone, which is what a reward hangs on.
---@param level integer
---@return boolean
function progress.isMilestone(level)
  return level % rpglevels.config.rewardEvery == 0
end
