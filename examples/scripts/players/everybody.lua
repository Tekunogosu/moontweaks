-- Addressing everybody, rather than whoever happened to do something.
--
-- Every other function here takes a player identifier, and until now the only source
-- of one was an event. `players.all` is the other source, and `players.announce`
-- needs none at all.

local commands = moontweaks.commands
local events   = moontweaks.events
local groups   = moontweaks.groups
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

-- ## Reaching some players and not others
--
-- `announce` reaches everybody and `say` reaches one. A chat group is the middle:
-- the game's own channel, which players make and join, and which the server can
-- address as a whole.
--
-- A group is named by the number `of` and `find` hand back, not by its name, because
-- names are the players' own and two servers will not agree on them.
commands.add {
  name = "mygroups",
  description = "Say which chat groups you are in",
  requiresPlayer = true,
  handler = function(e)
    local mine = groups.of(e.player)
    if #mine == 0 then
      return "You are not in any chat group."
    end

    local said = {}
    for _, membership in ipairs(mine) do
      said[#said + 1] = ("%s (%s)"):format(membership.name, membership.standing)
    end

    return "You are in " .. table.concat(said, ", ")
  end,
}

-- Speaking into one. A group is named by its name, and one this server has not got is
-- refused by name — so this asks first rather than assuming the group exists.
local STAFF = "staff"

events.playerJoin(function(e)
  local staff = groups.find(STAFF)
  if not staff then return end

  groups.say(STAFF,
    ("%s has joined; %d of you are on."):format(players.name(e.player), #staff.online))
end)

-- ## Turning somebody away
--
-- `kick` ends the session and tells them why. It does not keep them out: they may
-- reconnect at once, so anything meant to be lasting has to turn them away again when
-- they do.
--
-- Nothing here asks first and nothing undoes it. A condition that is wrong empties the
-- server, so the condition is what wants the care, not the call.
commands.add {
  name = "sendhome",
  description = "Disconnect a player, telling them why",
  privilege = "controlserver",
  args = {
    { name = "player", type = "player" },
    { name = "reason", type = "text", optional = true },
  },
  handler = function(e)
    -- Read before the kick, not after: every other function here resolves a connected
    -- player, and a moment later there is no such player to resolve.
    local name = players.name(e.args.player)

    players.kick(e.args.player, e.args.reason or "An administrator ended your session.")

    return ("Disconnected %s."):format(name)
  end,
}
