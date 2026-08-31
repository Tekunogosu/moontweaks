-- Turning a death into experience.
--
-- `entityDeath` is raised wherever the game happens to be and handed to Lua on the
-- next tick of the main thread, so what arrives is what was true when it happened. The
-- killer may have logged off in between, which is why their identifier is checked
-- before anything reaches for them.
--
-- Who killed it is not simply read off the event. MoonTweaks 0.28.0 takes `byPlayer`
-- from the damage's `CauseEntity`, and Vintage Story fills that in for a projectile
-- alone — a melee blow names the attacker in `SourceEntity` and leaves `CauseEntity`
-- null. `killerOf` owns the whole question: it takes the named killer where there is
-- one, and otherwise credits a blow to the nearest player standing by.
--
-- A creature no rule names is worth nothing. That is the safe default — it keeps straw
-- dummies, boats and traders off the ledger — but a server running a mod this file has
-- never heard of will want its creatures priced, so each unpriced code is written to
-- the log once for whoever is editing `01-config.lua`.

rpglevels = rpglevels or {}
rpglevels.kills = {}

local events  = moontweaks.events
local log     = moontweaks.log
local players = moontweaks.players
local kills   = rpglevels.kills

local unpriced = {}

--- Whether a rule names this code. Codes are compared as plain text, so a hyphen in a
--- creature code is a hyphen rather than a pattern quantifier.
---@param code string
---@param rule table
---@return boolean
local function matches(code, rule)
  if code:find(rule.prefix, 1, true) ~= 1 then return false end

  return not rule.contains or code:find(rule.contains, 1, true) ~= nil
end

--- What killing this creature is worth. Rules are read top to bottom and the first
--- match wins, so a rule for one variant belongs above the rule for its family.
---@param code string
---@return integer
function kills.xpFor(code)
  for _, rule in ipairs(rpglevels.config.killXp) do
    if matches(code, rule) then return rule.xp end
  end

  return 0
end

--- The player standing closest to a point, within `range` blocks of it.
---@param x number
---@param y number
---@param z number
---@param range number
---@return string|nil
local function nearestPlayer(x, y, z, range)
  local nearest, closest = nil, range * range

  for _, player in ipairs(players.all()) do
    local at = players.position(player)
    local dx, dy, dz = at.x - x, at.y - y, at.z - z
    local distance = dx * dx + dy * dy + dz * dz

    if distance <= closest then nearest, closest = player, distance end
  end

  return nearest
end

--- Who to credit for a death: the killer the game named, or — where a blow killed it
--- and named nobody — whoever was standing closest to it.
---@param e table
---@return string|nil
function kills.killerOf(e)
  if e.byPlayer then return e.byPlayer end

  local config = rpglevels.config
  if config.meleeCreditRange <= 0 then return nil end
  if not e.cause or not config.meleeCauses[e.cause] then return nil end

  return nearestPlayer(e.x, e.y, e.z, config.meleeCreditRange)
end

--- Whether this player is in a position to earn anything. Creative and spectator kills
--- cost nothing, so they are worth nothing.
---@param player string
---@return boolean
local function isEarning(player)
  return players.isOnline(player) and players.gameMode(player) == "survival"
end

events.entityDeath(function(e)
  if e.player then return end -- a player's own death is not a kill worth paying

  local killer = kills.killerOf(e)
  if not killer then return end

  local xp = kills.xpFor(e.code)

  if xp == 0 then
    if not unpriced[e.code] then
      unpriced[e.code] = true
      log.info(("no experience rule names %s, so killing one is worth nothing"):format(e.code))
    end
    return
  end

  if not isEarning(killer) then return end

  rpglevels.levelling.award(killer, xp)
end)
