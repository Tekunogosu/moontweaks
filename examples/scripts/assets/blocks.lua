-- What a block is once it is standing in the world, rather than while it is being
-- carried. `moontweaks.blocks.set` takes everything `items.set` takes — a block in a
-- hand is an item — and these keys besides, which only something placed can have.

local blocks = moontweaks.blocks

-- How long it takes to break, before any tool is taken into account. Vanilla rock
-- sits at 8 and needs a tier 2 tool to drop anything at all; this halves the work
-- and lets a tier 1 pick claim it.
blocks.set {
  code = "game:rock-*",
  resistance = 4,
  requiredMiningTier = 1,
}

-- What it is made of decides which of a tool's `miningSpeed` entries applies to it,
-- so this and a tool's mining speeds are two halves of the same tuning.
blocks.set {
  code = "game:glass-*",
  blockMaterial = "glass",
  resistance = 0.1,
}

-- Ground: how well things grow on it, and how readily something else may be built
-- over it. `replaceable` runs from 0 for anything solid up past 6000 for grass that
-- a placed block simply swallows.
blocks.set {
  code = "game:soil-*",
  fertility = 100,
  resistance = 1.0,
  replaceable = 400,
}

-- Moving over and through: a multiplier on walking speed, and how much it drags on
-- something passing through it. One is ordinary; below one slows.
blocks.set {
  code = "game:sand-*",
  walkSpeedMultiplier = 0.7,
  dragMultiplier = 0.9,
}

-- Two flags worth knowing. `climbable` is what makes a ladder a ladder, and
-- `rainPermeable` is whether weather falls through rather than being stopped.
blocks.set {
  code = "game:log-*",
  climbable = true,
  rainPermeable = false,
}

-- How much light a block stops. Glass at 0 lets a room stay lit through a window;
-- solid rock stops it entirely.
blocks.set {
  code = "game:glass-*",
  lightAbsorption = 0,
}

-- What it sounds like. Only the sounds named change, so a block can be given a new
-- breaking sound without restating what it sounds like underfoot. The game calls the
-- breaking one `break`, which Lua keeps as a keyword, so it is `breaking` here.
--
-- A bare string names the sound and leaves how it is played as the game had it,
-- which is what a script usually wants: the range the game fills in per kind of
-- sound is what decides whether it is heard at all.
blocks.set {
  code = "game:glass-*",
  sounds = {
    breaking = "survival:block/glass",
    place = "survival:block/glass",
    walk = { path = "survival:walk/stone", range = 10 },
  },
}

-- The shape of a block as far as anything walking into it is concerned. Boxes are
-- measured within the block, 0 to 1 in each direction, so this is a slab standing on
-- the floor of its own space. Writing either list replaces every box the block had.
--
-- `collisionBoxes` is what stops something moving; `selectionBoxes` is what a
-- player's cursor picks out. They are usually the same, and a block that should be
-- walked through but still clicked is where they differ.
blocks.set {
  code = "game:snowblock",
  collisionBoxes = { { y2 = 0.5 } },
  selectionBoxes = { { y2 = 0.5 } },
}

-- A drop that varies within a range, and how that range picks. Left alone a range is
-- uniform, which is the game's own default; `dist` is how a yield is made to cluster
-- near its average instead. Every quantity written as an average and a variance takes
-- it, so crushing yields and spoilage times read the same way.
blocks.set {
  code = "game:looseflints-*",
  drops = {
    { code = "game:flint", quantity = { avg = 2, var = 1, dist = "gaussian" } },
  },
}

moontweaks.log.info("block properties done")
