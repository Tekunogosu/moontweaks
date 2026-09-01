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

local claims   = moontweaks.claims
local commands = moontweaks.commands
local events   = moontweaks.events
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

-- Reading the claims themselves, rather than only asking about access. A claim is
-- named by its owner and its number among that owner's claims, which is the same
-- number the game shows them in `/land list`.
commands.add {
  name = "whoseland",
  description = "Say who has claimed the ground you are looking at",
  requiresPlayer = true,
  handler = function(e)
    local looking = players.looking(e.player)
    if not looking then
      return { error = "You are not pointing at anything." }
    end

    local here = claims.at(looking.x, looking.y, looking.z)
    if #here == 0 then
      return "Nobody has claimed that."
    end

    local said = {}
    for _, claim in ipairs(here) do
      said[#said + 1] = ("%s (claim %d%s)"):format(
        claim.ownerName, claim.index,
        claim.description ~= "" and ", " .. claim.description or "")
    end

    return "Claimed by " .. table.concat(said, "; ")
  end,
}

-- Claiming land for somebody. This is the server acting rather than a player asking,
-- so none of what the game checks when a player claims land themselves applies: how
-- much they are allowed, how many claims they may hold, or whether it overlaps.
-- Asking `claims.at` first is what a script does instead.
commands.add {
  name = "grantplot",
  description = "Claim a 32-block plot around you for a player",
  privilege = "controlserver",
  requiresPlayer = true,
  args = { { name = "player", type = "player" } },
  handler = function(e)
    local at = players.position(e.player)
    local half = 16

    local x, z = math.floor(at.x), math.floor(at.z)
    if #claims.at(x, math.floor(at.y), z) > 0 then
      return { error = "That ground is already claimed." }
    end

    local number = claims.add {
      owner = e.args.player,
      x = x - half, y = 0,   z = z - half,
      toX = x + half, toY = 256, toZ = z + half,
      description = "Granted plot",
    }

    return ("Granted claim %d, and the player sees it without installing anything.")
      :format(number)
  end,
}

-- Taking one back. The number is a position rather than a name, so removing one moves
-- every later claim of that owner down: this reads the numbers once and works down
-- from the highest, which is what makes removing several safe.
commands.add {
  name = "clearplots",
  description = "Take back every claim a player holds",
  privilege = "controlserver",
  args = { { name = "player", type = "player" } },
  handler = function(e)
    local held = claims.of(e.args.player)

    for index = #held, 1, -1 do
      claims.remove(e.args.player, held[index].index)
    end

    return ("Took back %d claim(s)."):format(#held)
  end,
}

-- A protection of a script's own. This is the one event whose answer decides
-- something: it is asked after the claim check, with that check's answer on
-- `e.allowed`, and whatever the handler returns is the decision.
--
-- Returning nothing leaves the decision alone, which is what a handler should do
-- everywhere it does not mean to interfere. Returning "granted" overrides a land
-- claim and opens somebody's land, so it is only ever right when the script really is
-- the authority on who may build there.
--
-- The server asks this for every block anybody breaks or uses, so the handler stays
-- arithmetic and a lookup and reads nothing that searches the world.
local SANCTUARY = { x = 500, z = 500, radius = 64 }

events.testBlockAccess(function(e)
  if e.what ~= "buildorbreak" then return end
  if e.allowed ~= "granted" then return end

  local dx, dz = e.x - SANCTUARY.x, e.z - SANCTUARY.z
  if dx * dx + dz * dz > SANCTUARY.radius * SANCTUARY.radius then return end

  if players.hasPrivilege(e.player, "controlserver") then return end

  return "deniedbymod"
end)
