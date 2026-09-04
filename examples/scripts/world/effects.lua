-- Making something noticed: sound and particles.
--
-- These are the two things besides `world.highlight` that a server-side script can
-- put on somebody's screen. Nothing needs installing on their machine — the sounds
-- and the particle drawing already ship with the game, and the server only says
-- which to play and where.
--
-- Both act on a loaded world, so they belong in a handler rather than in a script's
-- body.

local events  = moontweaks.events
local players = moontweaks.players
local world   = moontweaks.world

-- A sound is named by its path in the game's assets. One the game has no sound for
-- plays nothing and says nothing, so a path is worth checking against a working one.
events.didBreakBlock(function(e)
  if e.block ~= "game:ore-quartz" then return end

  world.playSound {
    sound = "game:sounds/effect/deepbell",
    x = e.x, y = e.y, z = e.z,
    range = 24,
  }
end)

-- Given one point, particles appear there. Given two, they fill the box between,
-- which is what makes a cloud rather than a spot.
--
-- `gravity` is what most changes how an effect reads: 1 falls like a dropped stack,
-- 0 hangs where it appeared, and a negative number rises the way smoke does.
events.didBreakBlock(function(e)
  if not e.block or not e.block:find("^game:ore%-") then return end

  world.spawnParticles {
    x = e.x, y = e.y, z = e.z,
    toX = e.x + 1, toY = e.y + 1, toZ = e.z + 1,
    quantity = 24,
    colour = { red = 255, green = 210, blue = 90 },
    velocity = { x = -0.05, y = 0.1, z = -0.05 },
    toVelocity = { x = 0.05, y = 0.3, z = 0.05 },
    life = 1.5,
    gravity = -0.2,
    size = 0.4,
  }
end)

-- Naming a pitch asks for exactly that pitch every time. Leaving it out lets the game
-- vary it a little on each play, which is what stops a repeated sound reading as a
-- loop — so a sound played often is usually better without one.
events.playerRespawn(function(e)
  local at = players.position(e.player)

  world.playSound {
    sound = "game:sounds/effect/deepbell",
    x = at.x, y = at.y, z = at.z,
    pitch = 1.4,
    volume = 0.6,
    range = 8,
  }

  -- A cube model reads as debris; the default quad reads as smoke or sparks.
  world.spawnParticles {
    x = at.x - 0.5, y = at.y, z = at.z - 0.5,
    toX = at.x + 0.5, toY = at.y + 1.8, toZ = at.z + 0.5,
    quantity = 40,
    colour = { red = 180, green = 230, blue = 255, alpha = 200 },
    toVelocity = { y = 0.4 },
    life = 2,
    gravity = -0.4,
    size = 0.3,
    model = "quad",
  }
end)
