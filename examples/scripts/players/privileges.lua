-- Asking what the server has already decided about somebody.
--
-- Reading a privilege grants nothing. It is how a script gates its own behaviour on
-- the roles a server administrator set up, rather than inventing a second set of
-- rules alongside them. A script cannot grant, revoke or change a role: one that
-- could would be able to give itself anything.

local commands = moontweaks.commands
local events   = moontweaks.events
local players  = moontweaks.players

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
