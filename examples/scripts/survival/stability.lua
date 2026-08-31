-- Temporal stability: how sound a place is, and when the next storm is due.
--
-- Stability belongs to the place rather than to the player. Deep and far from the
-- surface reads low; a settled surface reads around 1. A world with temporal
-- stability turned off answers 2 everywhere, which is higher than any world with it
-- on ever reads — so a script can tell "very stable" from "not a thing here".

local stability = moontweaks.stability

if not stability.available() then
  moontweaks.log.warn("no temporal stability on this server; the stability script does nothing")
  return
end

-- Warning somebody as they arrive somewhere thin.
moontweaks.events.playerReady(function(e)
  local at = moontweaks.players.position(e.player)
  local sound = stability.at(at.x, at.y, at.z)

  if sound < 0.6 then
    moontweaks.players.warn(e.player, "The air here does not feel right.")
  end
end)

moontweaks.commands.add {
  name = "stability",
  description = "Say how stable the ground under you is",
  requiresPlayer = true,
  handler = function(e)
    local at = moontweaks.players.position(e.player)
    local sound = stability.at(at.x, at.y, at.z)

    if sound >= 2 then return "Temporal stability is turned off in this world." end
    return ("%.0f%% stable here."):format(sound * 100)
  end,
}

-- Announcing a storm as it arrives and as it passes, without saying it twice. The
-- system reports what is true now rather than raising an event, so this watches for
-- the change itself and remembers what it last said.
moontweaks.server.every(10000, function()
  local storm = stability.storm()
  local said = moontweaks.world.getData("stormAnnounced") or false

  if storm.active and not said then
    moontweaks.world.setData("stormAnnounced", true)
    for _, who in ipairs(moontweaks.players.all()) do
      moontweaks.players.warn(who, ("A %s temporal storm is here."):format(storm.strength))
    end
  elseif not storm.active and said then
    moontweaks.world.setData("stormAnnounced", false)
    for _, who in ipairs(moontweaks.players.all()) do
      moontweaks.players.say(who, "The storm has passed.")
    end
  end
end)

-- How long there is until the next one, in whole days.
moontweaks.commands.add {
  name = "storm",
  description = "Say when the next temporal storm is due",
  handler = function()
    local storm = stability.storm()
    if storm.active then
      return ("A %s storm is running, at %.0f%% strength."):format(storm.strength, storm.glitch * 100)
    end

    local days = storm.nextDay - moontweaks.calendar.now().totalDays
    return ("The next storm is %s, in %.1f day(s)."):format(storm.strength, days)
  end,
}
