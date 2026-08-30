-- Addressing everybody, rather than whoever happened to do something.
--
-- Every other function here takes a player identifier, and until now the only source
-- of one was an event. `players.all` is the other source, and `players.announce`
-- needs none at all.

local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players
local server   = moontweaks.server

-- One message to everyone on the server. Needs no list and no event to have
-- happened, so this is what an announcement is written with.
events.playerJoin(function(e)
  players.announce(e.playerName .. " has arrived.")
end)

-- A sweep over everybody. `players.all` gives identifiers, which is what every other
-- function in this module takes, so the two compose directly.
local function healEveryone()
  local healed = 0

  for _, player in ipairs(players.all()) do
    if players.health(player) < players.maxHealth(player) then
      players.setHealth(player, players.maxHealth(player))
      players.say(player, "Something kind passes over you.")
      healed = healed + 1
    end
  end

  return healed
end

-- Once an in-game morning, near enough. Timers count real time, so this is paced
-- against the clock rather than against the calendar.
server.every(1000 * 60 * 20, function()
  local healed = healEveryone()
  if healed > 0 then moontweaks.log.info(("healed %d player(s)"):format(healed)) end
end)

-- Names are how people refer to each other and identifiers are how the server does.
-- `uidOf` bridges the two, and answers for somebody who is not here — which is why
-- anything reaching their body has to check first.
commands.add {
  name = "lastseen",
  description = "Say where a player was when they last left",
  privilege = "chat",
  args = { { name = "name", type = "word" } },
  handler = function(e)
    local who = players.uidOf(e.args.name)
    if not who then return { error = "nobody here has ever gone by that name." } end

    if players.isOnline(who) then
      local at = players.position(who)
      return ("%s is here, at %d %d %d."):format(e.args.name, at.x, at.y, at.z)
    end

    -- What was stored against them is still readable while they are away; their
    -- position is not, because they have none.
    local left = players.getWorldData(who, "leftAt")
    if not left then return ("%s has been here, but not lately."):format(e.args.name) end

    return ("%s was last at %d %d %d."):format(e.args.name, left.x, left.y, left.z)
  end,
}
