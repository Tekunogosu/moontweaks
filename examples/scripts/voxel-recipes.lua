local knap  = moontweaks.recipes.knapping
local clay  = moontweaks.recipes.clayforming
local smith = moontweaks.recipes.smithing

moontweaks.log.info(("start: %d knapping, %d clay forming, %d smithing")
  :format(knap.count(), clay.count(), smith.count()))

-- Knapping, clay forming and smithing are the same shape: a material, a pattern of
-- voxels, an output. They differ only in how deep they work — one layer for a
-- knapping surface, sixteen for clay, six on an anvil — and every layer of a pattern
-- has to be the same size as the first.

-- One layer, so the rows are written directly.
knap.add {
  name = "moontweaks:knifeblade-stub",
  ingredient = "game:flint",
  pattern = { "_##_", "_##_" },
  output = "game:knifeblade-flint",
}

-- Several layers, bottom first.
clay.add {
  name = "moontweaks:bowl-squat",
  ingredient = "game:clay-blue",
  pattern = {
    { "####", "####" },
    { "#__#", "#__#" },
  },
  output = "game:bowl-blue-raw",
}

-- An anvil works up to six layers, and `code` groups it in the selection dialog.
smith.add {
  name = "moontweaks:nugget-bar",
  code = "moontweaks-bar-{metal}",
  ingredient = { code = "game:ingot-*", name = "metal",
                 allowedVariants = { "copper", "tinbronze" } },
  pattern = { { "##_", "##_" } },
  output = { code = "game:metalbit-{metal}", quantity = 3 },
}
