local barrel = moontweaks.recipes.barrel

moontweaks.log.info("starting with " .. barrel.count() .. " barrel recipes")

-- A barrel has no grid, so its ingredients are a list rather than a map keyed by a
-- pattern character. Each one is measured in items or in litres: `quantity` counts
-- things, `litres` measures a liquid, and a recipe may use both.

-- Mixed the moment the barrel holds all of it, because nothing seals it.
barrel.add {
  code = "moontweaks:weak-brine",
  ingredients = {
    { code = "game:waterportion", litres = 4 },
    { code = "game:salt", quantity = 1 },
  },
  output = { code = "game:brineportion", litres = 4 },
}

-- Left to seal instead: `sealHours` is in in-game hours, and its presence is how
-- the game tells a sealing recipe from a mixing one.
barrel.add {
  code = "moontweaks:quick-cured-{meat}",
  sealHours = 12,
  ingredients = {
    { code = "game:salt", quantity = 1 },
    { code = "game:*-raw", name = "meat", quantity = 1,
      allowedVariants = { "redmeat", "bushmeat" } },
  },
  output = { code = "game:{meat}-cured", quantity = 1 },
}

-- An ingredient may be required in a larger amount than the recipe actually takes:
-- the barrel must hold four litres, and only one is spent.
barrel.add {
  code = "moontweaks:thinned-brine",
  ingredients = {
    { code = "game:waterportion", litres = 4, consumeLitres = 1 },
    { code = "game:salt", quantity = 1 },
  },
  output = { code = "game:brineportion", litres = 1 },
}

moontweaks.log.info("done")
