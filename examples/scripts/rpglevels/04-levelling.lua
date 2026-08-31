-- The one place experience is granted.
--
-- Everything that awards experience — a kill, an administrator's command — comes
-- through here, so there is a single answer to what happens when somebody levels:
-- the standing is written, every level crossed is announced, and a milestone hands
-- over its reward. Crossing several levels at once announces each of them in turn.

rpglevels = rpglevels or {}
rpglevels.levelling = {}

local players   = moontweaks.players
local levelling = rpglevels.levelling

--- Grants experience to a player and reports how many levels it bought.
---@param player string
---@param gained integer
---@return integer
function levelling.award(player, gained)
  if gained <= 0 then return 0 end

  local progress = rpglevels.progress
  local before = progress.of(player)
  local after = progress.advance(before, gained)

  progress.remember(player, after)

  for level = before.level + 1, after.level do
    players.say(player, ("DING!! You gained a lvl and are now %d"):format(level))

    if progress.isMilestone(level) then
      rpglevels.rewards.grant(player, level)
    end
  end

  return after.level - before.level
end
