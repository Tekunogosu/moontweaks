local grid = moontweaks.recipes.grid

moontweaks.log.info("starting with " .. grid.count() .. " grid recipes")

-- Drop the vanilla flint axe.
grid.remove("game:axe-flint")

-- Put it back, but demanding a bone handle instead of a stick.
grid.add {
  name = "moontweaks:axe-flint-bone",
  pattern = { "T",
              "B" },
  ingredients = { T = "game:axehead-flint", B = "game:bone" },
  output = "game:axe-flint",
}

-- One declaration covering three stone axes, via wildcard expansion.
grid.add {
  name = "moontweaks:axe-stone-direct",
  pattern = { "T" },
  ingredients = {
    T = { code = "game:axehead-*", name = "material",
          allowedVariants = { "granite", "andesite", "chert" } },
  },
  output = { code = "game:axe-{material}", quantity = 1 },
}

moontweaks.log.info("done")

-- Tags match on what an asset is rather than what it is called, so this accepts
-- any axe, including one a mod adds under a code we could not have guessed.
grid.add {
  name = "moontweaks:sticks-from-firewood",
  pattern = { "AF" },
  ingredients = {
    A = { tags = { "tool-axe" }, isTool = true, toolDurabilityCost = 1 },
    F = "game:firewood",
  },
  output = { code = "game:stick", quantity = 4 },
}

-- Shapeless: the player may place these anywhere in the grid rather than in the
-- arrangement shown. The pattern still names the ingredients and how many, and its
-- width and height still bound the recipe, so write one compactly: a four-ingredient
-- shapeless recipe belongs in two rows of two, never one row of four, which no
-- three-wide grid could satisfy.
--
-- The loop is the other half. `game:*firewood` would match both of these woods and
-- is anchored, so it would not reach `game:firewoodpile` either — but it would take
-- in whatever a mod adds later under a code ending the same way. Listing the codes
-- accepts exactly what is written and nothing that arrives afterwards. This is what
-- a script buys over a recipe file, where the whole block would be duplicated once
-- per wood; here a third wood is one more entry.
for _, wood in ipairs({ "firewood", "agedfirewood" }) do
  grid.add {
    name = "moontweaks:sticks-from-" .. wood,
    shapeless = true,
    pattern = { "AL" },
    ingredients = {
      A = { tags = { "tool-axe" }, isTool = true, toolDurabilityCost = 1 },
      L = { code = "game:" .. wood },
    },
    output = { code = "game:stick", quantity = 4 },
  }
end

-- Several tags narrow rather than widen: every one listed must be present, so
-- this accepts something that is both a tool and a melee weapon. A knife and a
-- cleaver carry both tags and qualify; a club is a melee weapon but not a tool,
-- an axe is a tool but not a melee weapon, and neither is accepted.
grid.add {
  name = "moontweaks:bone-shard-from-bone",
  pattern = { "BK" },
  ingredients = {
    B = "game:bone",
    K = { tags = { "tool", "weapon-melee" }, isTool = true, toolDurabilityCost = 1 },
  },
  output = { code = "game:bone", quantity = 2 },
}

-- Everything a recipe carries beyond its shape. `requiresTrait` gates it behind a
-- character trait, exactly as the game gates its own clothier recipes; a trait
-- this server does not define is refused by name rather than becoming a recipe
-- nobody can reach. The sewing kit is consumed like any other ingredient and
-- hands back the twine it was wound on. Turning `averageDurability` off stops a
-- worn ingredient from dragging the product's durability down with it.
grid.add {
  name = "moontweaks:axe-flint-bound",
  requiresTrait = "clothier",
  averageDurability = false,
  pattern = { "KT" },
  ingredients = {
    T = "game:axehead-flint",
    K = { code = "game:sewingkit", returnedStack = "game:flaxtwine" },
  },
  output = "game:axe-flint",
}

-- Kept in the file but not registered. A disabled recipe is still built and
-- checked, so a mistake in one is reported on the run that declares it rather
-- than on the day it is switched back on.
grid.add {
  name = "moontweaks:axe-flint-experimental",
  enabled = false,
  pattern = { "TT" },
  ingredients = { T = "game:axehead-flint" },
  output = { code = "game:axe-flint", quantity = 2 },
}

-- `attributes` is arbitrary data the game carries on the recipe, on an ingredient
-- or on the output, written as a Lua table and stored as JSON. What a key means is
-- the game's business: liquid ingredients need one, and mods read their own.
grid.add {
  name = "moontweaks:axe-flint-attributed",
  pattern = { "TB" },
  ingredients = {
    T = "game:axehead-flint",
    B = "game:bone",
  },
  output = {
    code = "game:axe-flint",
    attributes = { moontweaksOrigin = "example", durability = 100 },
  },
}

-- `consume` and `durabilityChange` are the game's other spelling of `isTool` and
-- `toolDurabilityCost`; a recipe writes one pair or the other, and writing both is
-- refused. The pair says two things the tool spelling cannot. `consume = false`
-- with no durability cost keeps an ingredient untouched, which is how the game
-- crafts from a schematic: present to build from, and not spent doing so. And
-- `breakOnZeroDurability` lets a worn-out tool survive the craft that emptied it,
-- staying in the grid rather than shattering. The game's own recipe files spell
-- that field `break`, which Lua keeps as a keyword.
grid.add {
  name = "moontweaks:bone-shard-chiselled",
  pattern = { "CB" },
  ingredients = {
    B = "game:bone",
    C = { tags = { "tool-chisel" }, consume = false, durabilityChange = -20, breakOnZeroDurability = false },
  },
  output = { code = "game:bone", quantity = 2 },
}

-- `mergeAttributesFrom` carries attributes onto the output from several
-- ingredients where `copyAttributesFrom` carries them from one, naming the pattern
-- characters they sit under. What the output already declares survives a
-- collision, and an earlier character survives a later one. `showInCreatedBy`
-- keeps the recipe off the "Created by" list on its product's handbook page, which
-- is how a second way to craft something stays available without crowding the page
-- a player reads.
grid.add {
  name = "moontweaks:axe-flint-merged",
  showInCreatedBy = false,
  mergeAttributesFrom = { "T", "B" },
  pattern = { "TB" },
  ingredients = {
    T = "game:axehead-flint",
    B = "game:bone",
  },
  output = "game:axe-flint",
}
