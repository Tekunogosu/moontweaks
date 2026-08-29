-- What a block leaves behind when it is broken.
--
-- Writing `drops` replaces every drop the block had rather than adding to them. That
-- is the only honest spelling: a list has no key to merge on, so a script adding to
-- one would have to say which of the old entries it meant to keep. Write the whole
-- list, or leave the key out.
--
-- Each entry is rolled for on its own and each is resolved as it is set, so a code
-- naming nothing is refused here with the line that wrote it, rather than a player
-- discovering it by breaking the block.

local blocks = moontweaks.blocks

-- The simple case: one thing, every time. A bare string names one of something.
blocks.set {
  code = "game:glass-*",
  drops = { "game:glass-plain" },
}

-- A quantity that varies. `avg = 3` with `var = 1` gives somewhere between two and
-- four; an average below one is how a chance of nothing at all is spelled.
blocks.set {
  code = "game:rock-granite",
  drops = {
    { code = "game:stone-granite", quantity = { avg = 3, var = 1 } },
    { code = "game:gem-diamond-rough", quantity = { avg = 0.02 } },
  },
}

-- Drops that depend on the tool. Each entry names the class of tool it comes out
-- for, so grass gives dry grass to a knife or a scythe and nothing to a fist.
--
-- `lastDrop` stops the roll there when that entry drops, which is how a block offers
-- alternatives rather than a handful: whichever of these two lands, the other is not
-- rolled for.
blocks.set {
  code = "game:tallgrass-*",
  drops = {
    { code = "game:drygrass", quantity = { avg = 2 }, tool = "knife", lastDrop = true },
    { code = "game:drygrass", quantity = { avg = 1 }, tool = "scythe", lastDrop = true },
  },
}

-- An empty list makes a block drop nothing whatever breaks it.
blocks.set {
  code = "game:tallgrass-veryshort-free",
  drops = {},
}

moontweaks.log.info("block drops done")
