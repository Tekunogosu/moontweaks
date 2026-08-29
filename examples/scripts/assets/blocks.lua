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

moontweaks.log.info("block properties done")
