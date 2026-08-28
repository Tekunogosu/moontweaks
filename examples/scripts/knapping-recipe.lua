local knap = moontweaks.recipes.knapping

moontweaks.log.info("starting with " .. knap.count() .. " knapping recipes")

-- Drop the vanilla flint knife blade.
knap.remove("game:knifeblade-flint")

-- Put it back as a stubbier blade, chipped from the same flint. `#` is stone
-- left in place and `_` is stone chipped away; the surface is 16 by 16, so a
-- smaller pattern simply leaves the rest of it untouched.
knap.add {
  name = "moontweaks:knifeblade-flint-stubby",
  ingredient = "game:flint",
  pattern = { "____##____",
              "____##____",
              "___###____",
              "___###____",
              "___###____" },
  output = "game:knifeblade-flint",
}

-- One declaration covering three stones, via wildcard expansion. The variant
-- that matched `name` is substituted into the output's `{rock}`. The fields every
-- recipe kind shares work here too, so this one is gated behind a character trait.
knap.add {
  name = "moontweaks:knifeblade-stone-wide",
  requiresTrait = "technical",
  ingredient = { code = "game:stone-*", name = "rock",
                 allowedVariants = { "chert", "granite", "andesite" } },
  pattern = { "___####___",
              "___####___",
              "____##____" },
  output = { code = "game:knifeblade-{rock}", quantity = 2 },
}

moontweaks.log.info("done")
