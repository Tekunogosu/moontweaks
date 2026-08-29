-- Carrying things: how many fit in a slot, which slots will take them, what wears
-- them out, and what arbitrary data they carry for other mods to read.

local items  = moontweaks.items
local blocks = moontweaks.blocks

-- Blocks take the same shared keys items do, from their own module. A block is an
-- item as far as a hand holding one is concerned.
blocks.set {
  code = "game:planks-*",
  maxStackSize = 128,
  materialDensity = 400,
}

-- Where it may be carried, and what wears it out. Both replace the whole set, so
-- each is exactly what is listed here and nothing else.
items.set {
  code = "game:knifeblade-copper",
  storageFlags = { "general", "backpack", "offhand" },
  damagedBy = { "attacking", "blockbreaking" },
}

-- Arbitrary data the game and other mods read off the item, written as a Lua table
-- and stored as JSON. This replaces whatever the item carried rather than merging
-- into it, so write the whole thing.
items.set {
  code = "game:stick",
  attributes = {
    moontweaksNote = "set by a script",
    handbook = { exclude = false },
    tiers = { 1, 2, 3 },
  },
}

moontweaks.log.info("storage properties done")
