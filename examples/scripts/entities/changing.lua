-- Changing a living thing: its health, its name, what it can do, and what a script
-- remembers about it.
--
-- Everything here mirrors `moontweaks.players`, because a player's body is an entity
-- like any other. What differs is the identifier: a player's outlives them, an
-- entity's lasts only as long as the entity is loaded.

local commands = moontweaks.commands
local entities = moontweaks.entities
local events   = moontweaks.events
local players  = moontweaks.players

-- Health reads and writes the same way it does on a player. Something with no health
-- to have — a falling rock, a stack on the floor — says so rather than answering
-- zero, because asking is then plainly the mistake rather than the answer.
commands.add {
  name = "mend",
  description = "Heal the creature you are looking at",
  requiresPlayer = true,
  privilege = "controlserver",
  handler = function(e)
    local quarry = players.lookingAtEntity(e.player)
    if not quarry then return { error = "you are not looking at anything." } end

    local before = entities.health(quarry)
    entities.setHealth(quarry, entities.maxHealth(quarry))

    return ("%s: %.1f -> %.1f."):format(entities.name(quarry), before, entities.health(quarry))
  end,
}

-- Hurting something answers whether it took the damage. False is ordinary rather than
-- a failure: it may be invulnerable, already dead, or still inside the moment that
-- stops one blow landing twice.
commands.add {
  name = "smite",
  description = "Hurt the creature you are looking at",
  requiresPlayer = true,
  privilege = "controlserver",
  args = { { name = "amount", type = "number" } },
  handler = function(e)
    local quarry = players.lookingAtEntity(e.player)
    if not quarry then return { error = "you are not looking at anything." } end

    if not entities.damage(quarry, e.args.amount) then
      return "It shrugged that off."
    end

    entities.ignite(quarry)
    return ("%s took %.1f and is alight."):format(entities.name(quarry), e.args.amount)
  end,
}

-- Naming. Only creatures meant to carry a name can, which in practice means the ones
-- that talk: traders, villagers. Naming a chicken is refused by name rather than
-- silently ignored, so a script finds out rather than wondering.
commands.add {
  name = "christen",
  description = "Name the creature you are looking at",
  requiresPlayer = true,
  privilege = "controlserver",
  args = { { name = "name", type = "text" } },
  handler = function(e)
    local quarry = players.lookingAtEntity(e.player)
    if not quarry then return { error = "you are not looking at anything." } end

    -- The refusal is a script error, so catching it is how a command turns it into
    -- something the caller can read rather than something the log swallows.
    local ok, why = pcall(function() entities.setName(quarry, e.args.name) end)
    if not ok then return { error = tostring(why) } end

    return "It answers to " .. e.args.name .. " now."
  end,
}

-- Abilities work exactly as they do on a player: named contributions added to a base
-- of 1, so 0.5 makes something half again as fast and -0.5 halves it. Naming each one
-- is what lets it be taken back without disturbing anybody else's.
local function makeSwift(entity)
  entities.setStat { entity = entity, stat = "walkspeed", name = "hunted", value = 0.4 }
end

-- What a script remembers about an entity is stored on the entity itself, so it
-- survives its chunk unloading and coming back — unlike the identifier, which does
-- not. This is how a script recognises something it has met before.
events.didBreakBlock(function(e)
  if e.block ~= "game:drygrass" then return end

  for _, it in ipairs(entities.around { x = e.x, y = e.y, z = e.z, range = 8 }) do
    local met = entities.getData(it.id, "met") or 0
    entities.setData(it.id, "met", met + 1)

    -- Three disturbances and it learns to run.
    if met + 1 == 3 then
      makeSwift(it.id)
      players.say(e.player, ("The %s has had enough of you."):format(it.name))
    end
  end
end)

-- Taking it back. The contribution is named, so this removes exactly the one this
-- script added and leaves anything else affecting the same ability alone.
commands.add {
  name = "calm",
  description = "Settle the creature you are looking at",
  requiresPlayer = true,
  privilege = "controlserver",
  handler = function(e)
    local quarry = players.lookingAtEntity(e.player)
    if not quarry then return { error = "you are not looking at anything." } end

    entities.clearStat(quarry, "walkspeed", "hunted")
    entities.setData(quarry, "met", 0)
    entities.extinguish(quarry)

    return ("%s walks at %.2f times the usual pace again.")
      :format(entities.name(quarry), entities.stat(quarry, "walkspeed"))
  end,
}
