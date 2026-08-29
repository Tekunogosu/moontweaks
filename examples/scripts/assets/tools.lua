-- What a tool is worth: how long it lasts, how hard it hits, and how fast it digs.
--
-- These reach players as well as the server. The item and block registries are sent
-- to every client, so a retuned durability shows in a tooltip without anybody
-- installing anything.
--
-- Only the keys a script writes change. Everything left out stays as the game loaded
-- it, so a script says what it means to alter and nothing else moves.

local items = moontweaks.items

moontweaks.log.info(("%d items and %d blocks on this server")
  :format(items.count(), moontweaks.blocks.count()))

-- One item, one number.
items.set {
  code = "game:axe-flint",
  durability = 120,
}

-- Everything a tool carries about swinging and digging, on one item. `miningSpeed`
-- replaces the whole table, so this pickaxe is fast at exactly these four materials
-- and ordinary at everything else.
items.set {
  code = "game:pickaxe-copper",
  durability = 350,
  attackPower = 3.5,
  attackRange = 4.5,
  toolTier = 3,
  materialDensity = 600,
  miningSpeed = { stone = 6.5, ore = 6.5, gravel = 4.0, soil = 3.0 },
}

-- `tool` and `toolTier` answer different questions and are set separately. The tier
-- is how hard a block it can break; the class is what kind of tool it counts as,
-- which is what a recipe asking for an axe and a block dropping only for an axe both
-- read. Naming a class an item never had is how something becomes a tool.
items.set {
  code = "game:club-*",
  tool = "hammer",
  toolTier = 2,
}

-- A wildcard changes the whole family at once, so this reaches every pickaxe the
-- server holds, including any a mod added under a code we could not have guessed.
items.set {
  code = "game:pickaxe-*",
  toolTier = 4,
}

moontweaks.log.info("tool properties done")
