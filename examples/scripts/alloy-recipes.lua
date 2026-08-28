local alloy = moontweaks.recipes.alloy

moontweaks.log.info("starting with " .. alloy.count() .. " alloys")

-- An alloy is not a recipe the game arranges: it is a list of metals and the share
-- of the mix each must make up. A crucible weighs what it holds against those
-- shares, counting every ore as the metal it smelts into, so ore and ingot of one
-- metal count towards the same share.

-- Shares are fractions of one, and the ranges must be able to add up to a whole
-- mix: the smallest shares may total no more than one, and the largest no less.
-- A cheaper route to a metal the game already pours, standing beside its own.
alloy.add {
  ingredients = {
    { code = "game:ingot-lead", minRatio = 0.45, maxRatio = 0.55 },
    { code = "game:ingot-zinc", minRatio = 0.45, maxRatio = 0.55 },
  },
  output = "game:ingot-leadsolder",
}

-- Loosening an alloy the game already has means removing it and adding it back.
-- Tin bronze normally wants tin between 8 and 12 percent of the mix; this pours
-- for anything from 5 to 20, so a rough guess at the crucible still works.
alloy.remove("game:ingot-tinbronze")

-- The output takes no quantity: a crucible pours as much metal as went into it, so
-- how much comes out is decided by the mix rather than by the recipe. A bare string
-- is shorthand for the whole output table, as above; write it out to say more.
alloy.add {
  ingredients = {
    { code = "game:ingot-tin", minRatio = 0.05, maxRatio = 0.2 },
    { code = "game:ingot-copper", minRatio = 0.8, maxRatio = 0.95 },
  },
  output = { code = "game:ingot-tinbronze" },
}

-- Every ingredient names one metal. Unlike every other kind, an alloy has no
-- variants to expand into, so a wildcard code is refused rather than registered as
-- an alloy no crucible could ever match.

moontweaks.log.info("done")
