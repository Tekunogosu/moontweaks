-- Fire: what burns, how hot, for how long, and what it becomes when it melts.
--
-- `combustible` is a table of its own and merges into whatever the item already
-- said, so naming a melting point moves that and leaves the rest of what the item
-- said about fire alone. Naming a property the item never had gives it one, which is
-- how something that never burned starts burning.

local items = moontweaks.items

-- Melting: how hot it must get, how long it takes once it is, and what comes out.
-- `requiresContainer` is what puts it in a crucible rather than straight in the fire.
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

-- The rest of what fire cares about: how much smoke it gives off, how hot it may get
-- before it is ruined, and how well it keeps heat from reaching what it holds.
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

moontweaks.log.info("fuel properties done")
