-- Remembering the person rather than the playthrough.
--
-- A script has two places to put something about a player, and they are different
-- stores rather than two ways of reaching one:
--
--   players.setWorldData    saved with this save game. A second world on the same
--                           server keeps its own, and deleting a world takes it with
--                           it. The player has to be on the server.
--
--   players.setAccountData  saved beside the ban and whitelist rolls, so it is one
--                           value for every world this server runs and it outlives
--                           any of them. Answers for a player who is offline.
--
-- Pick by what the thing is. How much ore somebody has mined *in this world* is world
-- data — a fresh world should start it at zero. Whether they have been shown the
-- welcome once, ever, is account data — showing it again on a new world would be a
-- bug, not a feature.
--
-- The reach is the point and the trap both. If a host runs two servers off one data
-- path, account data is shared between them. That is what makes it the right home
-- for a preference and the wrong home for anything a world reset should clear.

local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players

-- Shown once ever, not once per world. Written against the account for that reason:
-- a player who has seen it and then joins a freshly generated world has still seen it.
events.playerNowPlaying(function(e)
  if players.getAccountData(e.player, "welcomed") then return end

  players.say(e.player, "Welcome. Type /prefs to set how much this server talks to you.")
  players.setAccountData(e.player, "welcomed", true)
end)

-- A preference belongs to the person, so it follows them between worlds. Stored as a
-- table to show that account data takes whatever world data does.
commands.add {
  name = "prefs",
  description = "Read or change how much this server tells you",
  requiresPlayer = true,
  args = {
    { name = "setting", type = "word", optional = true, values = { "quiet", "normal" } },
  },
  handler = function(e)
    local prefs = players.getAccountData(e.player, "prefs") or { chatter = "normal" }

    if not e.args.setting then
      return ("You are set to %s."):format(prefs.chatter)
    end

    prefs.chatter = e.args.setting
    players.setAccountData(e.player, "prefs", prefs)
    return ("Set to %s. This follows you to every world on this server.")
      :format(prefs.chatter)
  end,
}

-- The same fact counted both ways, so the difference is visible rather than described.
-- Deaths in this world reset when the world does; deaths across every world do not.
events.playerDeath(function(e)
  players.setWorldData(e.player, "deaths", (players.getWorldData(e.player, "deaths") or 0) + 1)
  players.setAccountData(e.player, "deaths", (players.getAccountData(e.player, "deaths") or 0) + 1)
end)

commands.add {
  name = "mydeaths",
  description = "Say how many times you have died here and everywhere",
  requiresPlayer = true,
  handler = function(e)
    return ("%d death(s) in this world, %d across every world on this server.")
      :format(
        players.getWorldData(e.player, "deaths") or 0,
        players.getAccountData(e.player, "deaths") or 0)
  end,
}

-- Account data answers for somebody who is not here, which world data cannot. This is
-- what lets a note be left for a player who has already logged off.
commands.add {
  name = "leavenote",
  description = "Leave a note for a player, here or not",
  privilege = "controlserver",
  args = {
    { name = "who", type = "word" },
    { name = "note", type = "text" },
  },
  handler = function(e)
    -- A name typed by hand becomes an identifier the rest of the module accepts.
    local uid = players.uidOf(e.args.who)
    if not uid then
      return { error = ("This server has never seen a player called %s."):format(e.args.who) }
    end

    players.setAccountData(uid, "note", e.args.note)
    return ("Noted for %s. They will see it when they next join."):format(e.args.who)
  end,
}

events.playerNowPlaying(function(e)
  local note = players.getAccountData(e.player, "note")
  if not note then return end

  players.say(e.player, ("A note was left for you: %s"):format(note))
  players.setAccountData(e.player, "note", nil)
end)
