-- Asking before building on somebody else's land.
--
-- `world.setBlock`, `queueBlock`, `exchangeBlock` and `breakBlock` write as the
-- server rather than as a player. They do not check land claims, and that is
-- deliberate: a script laying out terrain or repairing the world should not be
-- stopped by a claim, and a script acting on what a player just asked for should.
--
-- Only the script knows which of the two it is doing, so the check is offered rather
-- than applied. `world.testAccess` answers and enforces nothing; reading it changes
-- no block.
--
-- It answers with a reason rather than a yes or no, so a refusal can say what stopped
-- it: `granted`, `landclaimed`, `noprivilege`, `playerdead`, `inguestmode`,
-- `inspectatormode` or `deniedbymod`.

local commands = moontweaks.commands
local players  = moontweaks.players
local world    = moontweaks.world

--- What to tell somebody who was refused, in words rather than a code.
local function refusal(response)
  if response == "landclaimed" then
    return "somebody else has claimed that land"
  elseif response == "noprivilege" then
    return "you do not have permission to build there"
  elseif response == "playerdead" then
    return "you are dead"
  elseif response == "inguestmode" or response == "inspectatormode" then
    return "you are only visiting"
  end

  return "something is stopping you building there"
end

-- Acting on what a player pointed at, so their claim standing is what decides it.
commands.add {
  name = "pillar",
  description = "Raise a column of stone where you are looking",
  requiresPlayer = true,
  handler = function(e)
    local looking = players.looking(e.player)
    if not looking then
      return { error = "You are not pointing at anything." }
    end

    -- Asked once for the base. A taller column would be worth asking about at the top
    -- as well, since a claim has edges and a column can cross one.
    local allowed = world.testAccess {
      player = e.player,
      x = looking.x, y = looking.y, z = looking.z,
      what = "buildorbreak",
    }

    if allowed ~= "granted" then
      return { error = ("You cannot build here: %s."):format(refusal(allowed)) }
    end

    for step = 1, 4 do
      world.queueBlock("game:rock-granite", looking.x, looking.y + step, looking.z)
    end

    return ("Raised %d blocks."):format(world.commit())
  end,
}

-- The other kind of write. This one is the server tidying up after itself rather than
-- a player acting, so it does not ask and should not: a claim is about what players
-- may do to each other's work, not about what the world may do to a leftover.
commands.add {
  name = "clearfire",
  description = "Put out any fire in the box around you",
  privilege = "controlserver",
  requiresPlayer = true,
  handler = function(e)
    local at = players.position(e.player)
    local radius = 10

    local burning = world.findBlocks {
      x = math.floor(at.x - radius), y = math.floor(at.y - radius), z = math.floor(at.z - radius),
      toX = math.floor(at.x + radius), toY = math.floor(at.y + radius), toZ = math.floor(at.z + radius),
      code = "game:fire",
      limit = 512,
    }

    for _, fire in ipairs(burning) do
      world.queueBlock("game:air", fire.x, fire.y, fire.z)
    end

    return ("Put out %d fire(s)."):format(world.commit())
  end,
}
