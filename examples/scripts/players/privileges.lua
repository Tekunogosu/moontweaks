-- Asking what the server has already decided about somebody.
--
-- Reading a privilege grants nothing. It is how a script gates its own behaviour on
-- the roles a server administrator set up, rather than inventing a second set of
-- rules alongside them. A script cannot grant, revoke or change a role: one that
-- could would be able to give itself anything.

local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players
local server   = moontweaks.server

-- Declaring one is the exception, and it is not granting. A privilege the server has
-- never been told about is a privilege nobody holds, so a command requiring a name
-- invented here would be a command nobody could run. Declaring it makes the name real
-- for as long as the server runs: administrators and the console hold it at once, and
-- everybody else gets it when the operator names it in a role in `serverconfig.json`.
--
-- This belongs in a script's body rather than a handler, because the declaration is
-- lost on shutdown and a script's body is what runs at every startup.
server.addPrivilege("moontweaks.warden", "Use the warden tools this server adds")

commands.add {
  name = "warden",
  description = "Warden tools",
  privilege = "moontweaks.warden",
  requiresPlayer = true,
  handler = function(e)
    return ("%s, the wardens are yours to command."):format(players.name(e.player))
  end,
}

-- A command everyone may run that does more for those who may do more. The command's
-- own `privilege` decides who may type it at all; this decides what they get.
commands.add {
  name = "whoami",
  description = "Say what you are allowed to do here",
  requiresPlayer = true,
  handler = function(e)
    local held = players.privileges(e.player)
    table.sort(held)

    return ("%s, holding %d privilege(s): %s")
      :format(players.name(e.player), #held, table.concat(held, ", "))
  end,
}

-- Gating a script's own behaviour. Somebody who may not build is told so rather than
-- quietly having nothing happen, which is the difference between a rule and a bug.
events.didUseBlock(function(e)
  if e.block ~= "game:chest-east" then return end

  if not players.hasPrivilege(e.player, "build") then
    players.warn(e.player, "You may not open that here.")
    return
  end

  players.say(e.player, "The crate is yours to use.")
end)

-- `warn` renders in the style the game uses for things that went wrong, so a refusal
-- reads as a refusal rather than as news. `say` is the plain form of the same thing.
events.playerReady(function(e)
  if players.hasPrivilege(e.player, "controlserver") then
    players.say(e.player, "Administrator tools are available to you.")
  end
end)
