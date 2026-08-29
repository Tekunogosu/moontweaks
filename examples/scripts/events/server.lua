-- Events about the server itself rather than about anything in it. Their handlers
-- are given a table with nothing in it, because the event happening is the whole of
-- what there is to say, so they are written taking no argument at all.

local events = moontweaks.events

-- Every one of these fires after the scripts have run. A script registers its
-- handlers while the server is still loading, and the server reaches these later.

-- `saveGameCreated` fires on the one start where the world is brand new, and never
-- again for that world. Anything a world should be set up with once belongs here.
events.saveGameCreated(function()
  moontweaks.log.info("a new world was created")
end)

-- `saveGameLoaded` fires on every start, immediately after the above, once the save
-- has been read.
events.saveGameLoaded(function()
  moontweaks.log.info("save game loaded")
end)

-- `worldgenStartup` is the last thing the server does before it begins ticking, so
-- reaching it means the load finished rather than failed partway.
events.worldgenStartup(function()
  moontweaks.log.info("world generators starting; the server is about to run")
end)

-- `gameWorldSave` fires each time the world is written to disk, which a server does
-- periodically and again as it shuts down.
events.gameWorldSave(function()
  moontweaks.log.info("world being saved")
end)

-- `serverResume` fires when a server that suspended itself for want of players
-- wakes up again. A server that never stands by never raises it.
events.serverResume(function()
  moontweaks.log.info("server resumed from standby")
end)
