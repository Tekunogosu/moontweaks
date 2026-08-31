-- How a crop grows on farmland.
--
-- `cropProps` only means anything on a block the game already farms: it changes how
-- a crop behaves and does not turn an ordinary block into one. Only the keys written
-- change, so a crop can be made slower without restating what it feeds on.

local blocks = moontweaks.blocks

-- A faster harvest that takes more out of the soil for it. Growth is counted in
-- stages, and the days are how long the whole run of them takes.
blocks.set {
  code = "game:crop-carrot-*",
  cropProps = {
    totalGrowthDays = 4.0,
    nutrientConsumption = 60,
  },
}

-- What a crop feeds on decides which fertiliser helps it. The three are nitrogen,
-- phosphorus and potassium, written `n`, `p` and `k` as the game names them.
blocks.set {
  code = "game:crop-flax-*",
  cropProps = {
    requiredNutrient = "p",
    totalGrowthDays = 8.0,
  },
}

-- Weather damage: below the cold threshold a crop starts suffering, and the ripe
-- multiplier is how much worse that is once it is ready to pick. Raising the
-- threshold makes a crop temperate rather than hardy.
blocks.set {
  code = "game:crop-rye-*",
  cropProps = {
    coldDamageBelow = -2,
    coldDamageRipeMul = 2.0,
    damageGrowthStuntMul = 0.5,
  },
}

-- A crop that can be picked more than once falls back by stages rather than being
-- pulled up, which is what makes a berry bush a bush.
blocks.set {
  code = "game:crop-pineapple-*",
  cropProps = {
    multipleHarvests = true,
    harvestGrowthStageLoss = 2,
  },
}

moontweaks.log.info("crop properties done")
