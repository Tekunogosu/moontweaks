-- The shaped properties: what happens when an item is burned, eaten, ground or
-- crushed. Each is a table of its own, and each merges into whatever the item
-- already said — naming a melting point moves that and leaves the rest of what the
-- item said about fire alone.
--
-- Naming a property the item never had gives it one, so a script can make something
-- edible that never was.

local items = moontweaks.items

-- Fire: how it burns, and what it becomes when it melts.
items.set {
  code = "game:ingot-copper",
  combustible = {
    meltingPoint = 1000,
    meltingDuration = 4,
    smeltedRatio = 1,
    smeltingType = "smelt",
    requiresContainer = true,
    smeltedStack = { code = "game:ingot-copper", quantity = 1 },
  },
}

-- Only the two keys named move; everything else firewood said about burning stays.
items.set {
  code = "game:firewood",
  combustible = {
    burnTemperature = 900,
    burnDuration = 24,
  },
}

-- The rest of what fire cares about: how much smoke it gives off, how hot it may
-- get before it is ruined, and how well it keeps heat from reaching what it holds.
items.set {
  code = "game:ore-anthracite",
  combustible = {
    burnTemperature = 1300,
    burnDuration = 40,
    smokeLevel = 0.5,
    maxTemperature = 1400,
    heatResistance = 3,
  },
}

-- Food: what eating it does, and what is left in hand afterwards.
items.set {
  code = "game:redmeat-cooked",
  nutrition = {
    foodCategory = "protein",
    satiety = 240,
    health = 1.5,
    satietyLossDelay = 30,
    eatenStack = { code = "game:bowl-blue-fired", quantity = 1 },
  },
}

-- Flint is not food. Naming nutrition it never had gives it some, intoxication
-- included.
items.set {
  code = "game:flint",
  nutrition = {
    foodCategory = "vegetable",
    satiety = 5,
    health = -2,
    intoxication = 0.2,
  },
}

-- Tags reach what a code cannot: every knife the server holds grinds the same way,
-- whatever a mod called it.
items.set {
  tags = { "tool-knife" },
  grinding = { groundStack = { code = "game:powder-flint", quantity = 1 } },
}

-- The quern: what it grinds down into. The stack is resolved as it is set, so a
-- code naming nothing is refused here rather than handing a player nothing later.
items.set {
  code = "game:charcoal",
  grinding = {
    groundStack = { code = "game:powder-charcoal", quantity = 2 },
  },
}

-- The pulveriser: what it crushes into, how hard a cap it takes, and how much it
-- yields. The yield varies within a range rather than being fixed.
items.set {
  code = "game:ore-quartz",
  crushing = {
    crushedStack = { code = "game:crushed-quartz", quantity = 1 },
    hardnessTier = 2,
    quantity = { avg = 2, var = 1 },
  },
}

moontweaks.log.info("asset processing done")
