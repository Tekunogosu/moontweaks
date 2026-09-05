-- Sleep somewhere and wake up there: a player's spawn follows the bed they last
-- slept in, and goes back to the world's when that bed is gone.
--
-- The game never does this itself; only a temporal gear sets a spawn. A bed's use
-- event only fires once the player is actually lying in it, so a daytime click or a
-- bed somebody else is in binds nothing. Breaking either half removes the other
-- without a second break event, so once a break is reported the whole bed is gone.
--
-- The bed a spawn belongs to is kept with the player, because the game's own spawn
-- cannot say whether it came from the player, their role or the world. A bed can go
-- while its owner is away or looking elsewhere, so the check is made wherever it can
-- turn out to matter: at the break, at death, and when the owner comes back.

local events  = moontweaks.events
local players = moontweaks.players
local world   = moontweaks.world

local BED_KEY = "bedAt"

local function isBed(code)
  return code ~= nil and code:find(":bed%-") ~= nil
end

local function place(at)
  return ("%d, %d, %d"):format(at.x, at.y, at.z)
end

-- Where their bed is, or nil for a player who has never slept in one.
local function bedOf(player)
  return players.getWorldData(player, BED_KEY)
end

local function bedStands(at)
  return isBed(world.blockAt(at.x, at.y, at.z))
end

local function bind(player, at)
  players.setWorldData(player, BED_KEY, at)
  players.setSpawn(player, at.x, at.y, at.z)
  players.say(player, "You will wake here, at " .. place(at) .. ".")
end

local function unbind(player)
  players.setWorldData(player, BED_KEY, nil)
  players.clearSpawn(player)
  players.say(player, "Your bed is gone. You will wake at the world spawn.")
end

-- Unbinds a player whose bed no longer stands. Does nothing for a player with no bed
-- or whose bed is still there.
local function unbindIfBedGone(player)
  local at = bedOf(player)
  if at and not bedStands(at) then unbind(player) end
end

events.didUseBlock(function(e)
  if not isBed(e.block) then return end

  local at = { x = e.x, y = e.y, z = e.z }
  local bed = bedOf(e.player)
  if bed and bed.x == at.x and bed.y == at.y and bed.z == at.z then return end

  bind(e.player, at)
end)

events.didBreakBlock(function(e)
  if isBed(e.block) then unbindIfBedGone(e.player) end
end)

events.playerDeath(function(e)
  unbindIfBedGone(e.player)
end)

events.playerJoin(function(e)
  unbindIfBedGone(e.player)
end)
