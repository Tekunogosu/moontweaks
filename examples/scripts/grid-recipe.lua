local grid = moontweaks.recipes.grid

moontweaks.log.info("starting with " .. grid.count() .. " grid recipes")

-- Drop the vanilla flint axe.
grid.remove("game:axe-flint")

-- Put it back, but demanding a bone handle instead of a stick.
grid.add {
  name = "moontweaks:axe-flint-bone",
  pattern = { "T",
              "B" },
  ingredients = { T = "game:axehead-flint", B = "game:bone" },
  output = "game:axe-flint",
}

-- One declaration covering three stone axes, via wildcard expansion.
grid.add {
  name = "moontweaks:axe-stone-direct",
  pattern = { "T" },
  ingredients = {
    T = { code = "game:axehead-*", name = "material",
          allowedVariants = { "granite", "andesite", "chert" } },
  },
  output = { code = "game:axe-{material}", quantity = 1 },
}

moontweaks.log.info("done")

-- Tags match on what an asset is rather than what it is called, so this accepts
-- any axe, including one a mod adds under a code we could not have guessed.
grid.add {
  name = "moontweaks:sticks-from-firewood",
  pattern = { "AF" },
  ingredients = {
    A = { tags = { "tool-axe" }, isTool = true, toolDurabilityCost = 1 },
    F = "game:firewood",
  },
  output = { code = "game:stick", quantity = 4 },
}

-- Everything a recipe carries beyond its shape. `requiresTrait` gates it behind a
-- character trait, exactly as the game gates its own clothier recipes; a trait
-- this server does not define is refused by name rather than becoming a recipe
-- nobody can reach. The sewing kit is consumed like any other ingredient and
-- hands back the twine it was wound on. Turning `averageDurability` off stops a
-- worn ingredient from dragging the product's durability down with it.
grid.add {
  name = "moontweaks:axe-flint-bound",
  requiresTrait = "clothier",
  averageDurability = false,
  pattern = { "KT" },
  ingredients = {
    T = "game:axehead-flint",
    K = { code = "game:sewingkit", returnedStack = "game:flaxtwine" },
  },
  output = "game:axe-flint",
}

-- Kept in the file but not registered. A disabled recipe is still built and
-- checked, so a mistake in one is reported on the run that declares it rather
-- than on the day it is switched back on.
grid.add {
  name = "moontweaks:axe-flint-experimental",
  enabled = false,
  pattern = { "TT" },
  ingredients = { T = "game:axehead-flint" },
  output = { code = "game:axe-flint", quantity = 2 },
}
