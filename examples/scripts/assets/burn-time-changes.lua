-- Firepit fuel lasts as long on the game clock as it would in life.
--
-- Only the firepit reads burnDuration, in real seconds. At the default 48-minute
-- day one real second is 30 game seconds, so a piece that burns 30 minutes in life
-- burns 60 seconds here. Temperatures stay as the game set them, since they gate
-- what a fire can cook or smelt.

local items = moontweaks.items
local blocks = moontweaks.blocks

local function burns(code, seconds)
  items.set { code = code, combustible = { burnDuration = seconds } }
end

local function block_burns(code, seconds)
  blocks.set { code = code, combustible = { burnDuration = seconds } }
end

-- Wood
burns("game:firewood", 60)
burns("game:agedfirewood", 56)
burns("game:plank-*", 30)
burns("game:bamboostakes", 24)
burns("game:oakbark", 16)

-- Charcoal and coal
burns("game:charcoal", 80)
burns("game:coke", 100)
burns("game:ore-lignite", 90)
burns("game:ore-bituminouscoal", 120)
burns("game:ore-anthracite", 200)
burns("game:coal-contaminated", 60)

-- Peat
block_burns("game:peatbrick", 90)
block_burns("game:peat-*", 360)

-- Sap
burns("game:resin", 10)

-- Whole logs: the game's per-species ratios, three times over. The generic entry
-- goes first so the species entries override it.
block_burns("game:log-*", 216)
block_burns("game:log-*-kapok-*", 141)
block_burns("game:log-*-pine-*", 180)
block_burns("game:log-*-redwood-*", 216)
block_burns("game:log-*-birch-*", 234)
block_burns("game:log-*-maple-*", 237)
block_burns("game:log-*-acacia-*", 267)
block_burns("game:log-*-oak-*", 294)
block_burns("game:log-*-ebony-*", 330)
block_burns("game:log-*-aged-*", 120)

moontweaks.log.info("burn-time-changes applied")
