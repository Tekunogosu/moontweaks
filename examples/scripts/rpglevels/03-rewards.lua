-- What a milestone hands over, and making sure it actually arrives.
--
-- `players.give` is all or nothing: a full inventory takes none of it and answers
-- false, and whatever did not fit is gone. A reward earned over several hours is not
-- something to lose to a full backpack, so what the inventory refuses is dropped at
-- the player's feet instead, owned by them so nobody standing nearby collects it.

rpglevels = rpglevels or {}
rpglevels.rewards = {}

local log     = moontweaks.log
local players = moontweaks.players
local world   = moontweaks.world
local rewards = rpglevels.rewards

--- The tier a player of this level draws from: the first that reaches them.
---@param level integer
---@return table|nil
function rewards.tierFor(level)
  for _, tier in ipairs(rpglevels.config.rewardTiers) do
    if level <= tier.upTo then return tier end
  end

  return nil
end

--- Puts a stack into a player's hands, or at their feet where their inventory has no
--- room for it. Answers where it ended up.
---@param player string
---@param item table
---@return "inventory"|"ground"
local function handOver(player, item)
  local stack = { code = item.code, quantity = item.quantity or 1 }
  if players.give(player, stack) then return "inventory" end

  local at = players.position(player)
  world.dropItem { stack = stack, x = at.x, y = at.y, z = at.z, owner = player }

  return "ground"
end

--- Draws one reward for reaching `level` and hands it over, telling the player what
--- they got and where it went.
---@param player string
---@param level integer
function rewards.grant(player, level)
  local tier = rewards.tierFor(level)
  if not tier then
    log.warn(("no reward tier covers level %d, so it went unrewarded"):format(level))
    return
  end

  local item = tier.items[math.random(#tier.items)]
  local amount = item.quantity or 1
  local where = handOver(player, item)

  players.say(player, ("Level %d reward (%s): %d x %s.")
    :format(level, tier.name, amount, item.label))

  if where == "ground" then
    players.say(player, "Your pack was full, so it is on the ground at your feet.")
  end
end
