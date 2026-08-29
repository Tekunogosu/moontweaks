-- Choosing what to change by what a thing is, rather than by what it is called.
--
-- A code is a name, and names are a server's own: a mod's axe is not called
-- `game:axe-anything`, so a wildcard over vanilla codes never reaches it. A tag is a
-- claim about what something is, and the game's own assets carry them, so one entry
-- written against a tag reaches a modded axe as readily as a vanilla one.
--
-- `library/codes.lua` lists every tag this server's assets actually carry, and a tag
-- nothing carries is refused by name rather than quietly matching nothing.

local items = moontweaks.items

-- Every axe the server holds, whatever anybody called it.
items.set {
  tags = { "tool-axe" },
  durability = 500,
}

-- Several tags narrow rather than widen: every one listed must be present, so this
-- reaches only what is both a tool and a melee weapon.
items.set {
  tags = { "tool", "weapon-melee" },
  attackPower = 4.0,
}

-- A code and tags together: the wildcard proposes and the tags narrow. This is every
-- copper thing that is also a pickaxe, which neither half would have picked out
-- alone.
items.set {
  code = "game:*-copper",
  tags = { "tool-pickaxe" },
  toolTier = 4,
}

-- Naming neither a code nor any tags is refused rather than obeyed, because what it
-- asks for is every item on the server. Uncomment to see the failure it reports:
--
-- items.set { durability = 1 }

moontweaks.log.info("tag-matched properties done")
