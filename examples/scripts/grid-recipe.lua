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
