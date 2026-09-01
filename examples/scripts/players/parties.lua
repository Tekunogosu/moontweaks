-- Chat that goes somewhere other than to everybody: whispers between two players, and
-- parties built out of the game's own chat groups.
--
-- Three things are used together here. `players.say` reaches one person.
-- `groups.say` reaches everybody in a group. And `events.playerChat` is asked before
-- anybody sees what somebody typed, so a script can rewrite it, send it somewhere
-- else, or stop it being said at all.
--
-- A group is named by its name rather than by the number the server gives it: the
-- game assigns that number and then offers no way to look a group up by it, so the
-- name is the only handle there is. No two groups may share one.

local commands = moontweaks.commands
local events   = moontweaks.events
local groups   = moontweaks.groups
local players  = moontweaks.players

-- ## Whispers
--
-- Nothing to do with groups: two `players.say` calls and a note of who spoke last, so
-- that replying needs no name. Kept in memory rather than in world data, because a
-- conversation is over when somebody logs out.
local lastFrom = {}

commands.add {
  name = "w",
  description = "Whisper to one player",
  requiresPlayer = true,
  args = {
    { name = "player", type = "player" },
    { name = "message", type = "text" },
  },
  handler = function(e)
    players.say(e.args.player,
      ("%s whispers: %s"):format(players.name(e.player), e.args.message))

    lastFrom[e.args.player] = e.player

    return ("To %s: %s"):format(players.name(e.args.player), e.args.message)
  end,
}

commands.add {
  name = "r",
  description = "Reply to whoever whispered you last",
  requiresPlayer = true,
  args = { { name = "message", type = "text" } },
  handler = function(e)
    local to = lastFrom[e.player]
    if not to then
      return { error = "Nobody has whispered you." }
    end

    -- An identifier outlives the session it came from, so somebody who has left is a
    -- real answer rather than a failure.
    if not players.isOnline(to) then
      return { error = "They are no longer online." }
    end

    players.say(to, ("%s whispers: %s"):format(players.name(e.player), e.args.message))
    lastFrom[to] = e.player

    return ("To %s: %s"):format(players.name(to), e.args.message)
  end,
}

-- ## Parties
--
-- A party is a chat group, made by a script rather than by a player typing
-- `/group create`. The two are the same thing: a player can rename or disband one of
-- these with the game's own commands, and this can take away one they made.
--
-- `joinPolicy` decides one thing only — whether somebody may walk in with the game's
-- `/group join`. It does not gate `groups.join` below, which is the server putting
-- them there rather than them asking.
commands.add {
  name = "party",
  description = "A chat group for you and whoever you invite",
  requiresPlayer = true,
  subcommands = {
    {
      name = "new",
      description = "Start a party",
      args = {
        { name = "name", type = "word" },
        { name = "open", type = "bool", optional = true },
      },
      handler = function(e)
        if groups.find(e.args.name) then
          return { error = "A group of that name already exists." }
        end

        -- A name the game will not allow is refused here, naming the line, rather
        -- than later when somebody tries to type it.
        groups.add {
          name = e.args.name,
          owner = e.player,
          joinPolicy = e.args.open and "everyone" or "inviteonly",
        }

        groups.join(e.player, e.args.name, "owner")

        return e.args.open
          and ("Party '%s' is yours. Anybody may /group join it."):format(e.args.name)
          or ("Party '%s' is yours. Invite people to it."):format(e.args.name)
      end,
    },
    {
      name = "invite",
      description = "Add somebody to your party",
      args = {
        { name = "name", type = "word" },
        { name = "player", type = "player" },
      },
      handler = function(e)
        local party = groups.find(e.args.name)
        if not party then return { error = "No party of that name." } end
        if party.owner ~= e.player then
          return { error = "That party is not yours to invite to." }
        end

        groups.join(e.args.player, e.args.name)
        groups.say(e.args.name, ("%s has joined."):format(players.name(e.args.player)))

        players.say(e.args.player, ("You are in the party '%s'."):format(e.args.name))

        -- The one thing a script cannot do for them: the game tells a client about a
        -- group as it joins somebody to one, through a packet this mod cannot send.
        -- What is said in the party reaches them at once regardless.
        return "Invited. The party's own chat tab may not appear until they reconnect."
      end,
    },
    {
      name = "open",
      description = "Let anybody join your party, or stop them",
      args = {
        { name = "name", type = "word" },
        { name = "open", type = "bool" },
      },
      handler = function(e)
        local party = groups.find(e.args.name)
        if not party then return { error = "No party of that name." } end
        if party.owner ~= e.player then
          return { error = "That party is not yours to change." }
        end

        groups.setJoinPolicy(e.args.name, e.args.open and "everyone" or "inviteonly")

        return e.args.open
          and "Anybody may now /group join it."
          or "Only people you invite may join it now."
      end,
    },
    {
      name = "leave",
      description = "Leave a party",
      args = { { name = "name", type = "word" } },
      handler = function(e)
        groups.leave(e.player, e.args.name)
        groups.say(e.args.name, ("%s has left."):format(players.name(e.player)))

        return ("You have left '%s'."):format(e.args.name)
      end,
    },
    {
      name = "disband",
      description = "Take a party away",
      args = { { name = "name", type = "word" } },
      handler = function(e)
        local party = groups.find(e.args.name)
        if not party then return { error = "No party of that name." } end
        if party.owner ~= e.player then
          return { error = "That party is not yours to disband." }
        end

        groups.say(e.args.name, "This party is closing.")
        groups.remove(e.args.name)

        return ("Disbanded '%s'."):format(e.args.name)
      end,
    },
  },
}

-- ## Routing what somebody typed
--
-- The two halves together. A message beginning with `!` goes to the sender's first
-- party rather than to general chat, and the original is swallowed so that it is not
-- said twice.
--
-- Answering `false` is what swallows it. Answering nothing leaves it alone, which is
-- what this does for every message that is not addressed to a party — a handler that
-- does not mean to interfere should return nothing rather than `true`, because `true`
-- puts back a message some other script swallowed on purpose.
events.playerChat(function(e)
  local said = e.message:match("^!%s*(.+)$")
  if not said then return end

  local mine = groups.of(e.player)[1]
  if not mine then
    players.warn(e.player, "You are not in a party.")
    return false
  end

  groups.say(mine.name, ("%s: %s"):format(players.name(e.player), said))

  return false
end)

-- ## Rewriting rather than routing
--
-- Returning a string says what should be said instead. Handlers are asked in turn and
-- each is given what the one before it left, so this sees a message the handler above
-- may already have changed — and the last answer is the one that stands.
--
-- Scripts run in name order, so a handler that must have the last word belongs in a
-- file that sorts last.
events.playerChat(function(e)
  if e.group ~= 0 then return end

  if players.hasPrivilege(e.player, "controlserver") then
    return ("[staff] %s"):format(e.message)
  end
end)
