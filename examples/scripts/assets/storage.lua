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
-- and stored as JSON. Merged into what the item already carries, the way every
-- other key on `set` is: a key named here replaces the value under it, a key left
-- out keeps the game's, and that holds at every depth. So this moves one key inside
-- `handbook` and leaves the rest of that table alone. A list is the one thing that
-- does not merge: `tiers` replaces whatever list sat there whole.
items.set {
  code = "game:stick",
  attributes = {
    moontweaksNote = "set by a script",
    handbook = { exclude = false },
    tiers = { 1, 2, 3 },
  },
}

-- This is how a mod's crafting station is taught a new material. Immersive
-- Fibercraft reads `spinningProps` off whatever is put on its wheel; hemp fibers
-- keep their shelf and ground-storage data and gain one key beside it.
if moontweaks.mods.isEnabled("grassroots") and moontweaks.mods.isEnabled("spinningwheel") then
  items.set {
    code = "grassroots:gr-hemp-fibers",
    attributes = {
      spinningProps = {
        outputType = "grassroots:gr-hemp-twine",
        outputQuantity = 1,
        inputQuantity = 4,
        spinTime = 4,
      },
    },
  }
end

-- Replacing instead. A Lua table cannot hold a nil, so a merge can never take a key
-- away; `setAttributes` is how one is removed, at the cost of every other key the
-- game gave the item. Write the whole tree, and read the item's own JSON first.
items.set {
  code = "game:stick",
  setAttributes = {
    moontweaksNote = "the only key left",
  },
}

moontweaks.log.info("storage properties done")
