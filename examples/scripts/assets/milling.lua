-- The two machines that turn one thing into another without fire: the quern, which
-- grinds, and the pulveriser, which crushes.
--
-- Both stacks are resolved as they are set, so a code naming nothing is refused here
-- with the line that wrote it rather than handing a player nothing much later.

local items = moontweaks.items

-- The quern: what it grinds down into.
items.set {
  code = "game:charcoal",
  grinding = {
    groundStack = { code = "game:powder-charcoal", quantity = 2 },
  },
}

-- Tags reach what a code cannot: every knife the server holds grinds the same way,
-- whatever a mod called it.
items.set {
  tags = { "tool-knife" },
  grinding = { groundStack = { code = "game:powder-flint", quantity = 1 } },
}

-- The pulveriser: what it crushes into, how hard a cap it takes, and how much comes
-- out. The yield varies within a range rather than being fixed, so `avg = 2` with
-- `var = 1` gives somewhere between one and three.
items.set {
  code = "game:ore-quartz",
  crushing = {
    crushedStack = { code = "game:crushed-quartz", quantity = 1 },
    hardnessTier = 2,
    quantity = { avg = 2, var = 1 },
  },
}

moontweaks.log.info("milling properties done")
