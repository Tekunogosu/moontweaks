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

-- ## Conditions a bare list cannot say
--
-- A bare list is shorthand for `allOf`, so each call above could have been written
-- `tags = { allOf = { ... } }`. Spelling it out is what makes the other conditions
-- available, and the spelling is the game's own: `allOf`, `anyOf` and `noneOf`,
-- exactly as a recipe file writes them.

-- At least one of them rather than all of them: an axe or a pickaxe, rather than
-- something that would have to be both.
items.set {
  tags = { anyOf = { "tool-axe", "tool-pickaxe" } },
  toolTier = 3,
}

-- `noneOf` excludes. Every tool that is not also swung at people, which reaches the
-- hammers and the shovels and leaves the knives alone.
items.set {
  tags = { allOf = { "tool" }, noneOf = { "weapon-melee" } },
  durability = 90,
}

-- `noneOf` on its own selects on what something is not. This is the one place the
-- mod goes further than the game's own loader, which reads a condition with nothing
-- required as matching nothing at all.
items.set {
  code = "game:hammer-*",
  tags = { noneOf = { "weapon-melee" } },
  attackPower = 1.5,
}

-- ## Groups
--
-- A junction may hold groups rather than names, and then each group is a condition
-- of its own. The verbs alternate by layer, because a group is what the junction
-- combines: groups under `anyOf` ask with `allOf`, and groups under `allOf` ask with
-- `anyOf`. Writing the junction's own verb inside it is refused by name.

-- Any one group matching is enough: something that is both a tool and a melee
-- weapon, or a hammer that is no weapon at all.
items.set {
  tags = {
    anyOf = {
      { allOf = { "tool", "weapon-melee" } },
      { allOf = { "tool-hammer" }, noneOf = { "weapon" } },
    },
  },
  toolTier = 5,
}

-- Every group must match, and each asks for any one of its own tags: a knife or a
-- cleaver, which is also carried as a weapon.
items.set {
  tags = {
    allOf = {
      { anyOf = { "tool-knife", "tool-cleaver" } },
      { anyOf = { "weapon-melee", "weapon-ranged" } },
    },
  },
  miningSpeed = { plant = 3.0 },
}

-- Naming neither a code nor any tags is refused rather than obeyed, because what it
-- asks for is every item on the server. Uncomment to see the failure it reports:
--
-- items.set { durability = 1 }

moontweaks.log.info("tag-matched properties done")
