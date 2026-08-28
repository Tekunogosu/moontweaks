-- Item and block properties: the numbers and sets an item carries, rather than the
-- recipes that make it. These reach players as well as the server, because the item
-- and block registries are sent to every client, so a tooltip shows the new
-- durability without the client installing anything.
--
-- Only the keys a script writes change. Everything left out stays as the game
-- loaded it, so a script says what it means to alter and nothing else moves.

local items  = moontweaks.items
local blocks = moontweaks.blocks

moontweaks.log.info(("%d items and %d blocks"):format(items.count(), blocks.count()))

-- One item, one number.
items.set {
  code = "game:axe-flint",
  durability = 120,
}

-- Everything a tool carries about swinging and digging, on one item.
items.set {
  code = "game:pickaxe-copper",
  durability = 350,
  attackPower = 3.5,
  attackRange = 4.5,
  toolTier = 3,
  materialDensity = 600,
  miningSpeed = { stone = 6.5, ore = 6.5, gravel = 4.0, soil = 3.0 },
}

-- A wildcard changes the whole family at once, so this reaches every pickaxe the
-- server holds, including any a mod added.
items.set {
  code = "game:pickaxe-*",
  toolTier = 4,
}

-- Blocks take the same shape from the same module surface.
blocks.set {
  code = "game:planks-*",
  maxStackSize = 128,
  materialDensity = 400,
}

-- Tags select what to change, the same way they select a recipe ingredient: on what
-- an asset is rather than what it is called. This reaches every axe the server
-- holds, including ones a mod added under codes we could not have guessed.
items.set {
  tags = { "tool-axe" },
  durability = 500,
}

-- Several tags narrow rather than widen: every one listed must be present, so this
-- reaches only what is both a tool and a melee weapon.
items.set {
  tags = { "tool", "weapon-melee" },
  attackPower = 4.0,
}

-- A code and tags together: the wildcard proposes and the tags narrow. Naming
-- neither is refused, because it would change everything.
items.set {
  code = "game:*-copper",
  tags = { "tool-pickaxe" },
  toolTier = 4,
}

-- Where it may be carried, and what wears it out. Both replace the whole set, so
-- each is exactly what is listed here.
items.set {
  code = "game:knifeblade-copper",
  storageFlags = { "general", "backpack", "offhand" },
  damagedBy = { "attacking", "blockbreaking" },
}

-- Arbitrary data the game and other mods read off the item, written as a Lua table
-- and stored as JSON. This one replaces whatever the item carried.
items.set {
  code = "game:stick",
  attributes = {
    moontweaksNote = "set by a script",
    handbook = { exclude = false },
    tiers = { 1, 2, 3 },
  },
}

moontweaks.log.info("asset properties done")
