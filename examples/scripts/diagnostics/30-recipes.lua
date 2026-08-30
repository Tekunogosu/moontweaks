-- Every recipe kind, added to and taken from again.
--
-- One thing about the module shapes the whole file. A run's changes are held until
-- every script has finished and then applied together, so `count` during a script
-- reads the registry as it stood before the run — and a recipe added two lines above
-- is not in it yet. There is no second application either: a call made later, from a
-- handler or a command, is recorded against a log nothing applies again.
--
-- So each kind is checked in two halves. The adding and the removing happen here,
-- while the run can still apply them, and the counting happens once the world is up
-- and the run has been applied. What the count is read for is a loss. Every other
-- script on the server was applied in the same run, so the total afterwards includes
-- whatever they added and is not this file's to predict — but nothing in a run
-- removes what it did not add, so a total that has fallen means the removal below
-- reached the game's own recipes.
--
-- That last risk is why every kind but cooking produces rot. A selector names the
-- code a recipe produces rather than the recipe itself, so removing by anything the
-- game also crafts would take the game's with it. Cooking is the exception, as it is
-- everywhere: a meal has no single product, so its recipes carry their own code and
-- are selected by it.

local recipes = moontweaks.recipes

-- A code the game never crafts, so removing by it reaches nothing but what this file
-- added.
local UNCRAFTED = "game:rot"

--- Records what a kind held before the run, adds a recipe, removes it again, and
--- queues the count that judges the pair.
local function balanced(kind, module, add, remove)
  local before = module.count()

  diag.used(("recipes.%s.remove"):format(kind))
  diag.check(("recipes.%s.add"):format(kind), function()
    add()
    remove()
    return ("queued an addition and a removal against %d existing recipe(s)"):format(before)
  end)

  diag.later(("recipes.%s.count"):format(kind), function()
    local after = module.count()

    -- The count is only ever checked for having gone down. Every other script on
    -- this server was applied in the same run, so anything they added is in this
    -- figure too and the total is not the suite's to predict. A total that has
    -- fallen is another matter: nothing in a run removes what it did not add, so a
    -- loss means the removal above reached the game's own recipes.
    assert(after >= before,
      ("%d before the run and %d after: the removal took %d of the game's own recipes with it")
        :format(before, after, before - after))

    return ("%d before the run, %d after -- nothing was lost (a rise is other scripts adding)")
      :format(before, after)
  end)
end

balanced("grid", recipes.grid,
  function()
    recipes.grid.add {
      name = "moontweaks:diagnostic-grid",
      pattern = { "SS" },
      ingredients = { S = "game:stick" },
      output = UNCRAFTED,
    }
  end,
  function() recipes.grid.remove(UNCRAFTED) end)

balanced("knapping", recipes.knapping,
  function()
    recipes.knapping.add {
      name = "moontweaks:diagnostic-knapping",
      ingredient = "game:flint",
      pattern = { "##", "##" },
      output = UNCRAFTED,
    }
  end,
  function() recipes.knapping.remove(UNCRAFTED) end)

balanced("clayforming", recipes.clayforming,
  function()
    recipes.clayforming.add {
      name = "moontweaks:diagnostic-clayforming",
      ingredient = "game:clay-blue",
      pattern = { { "##", "##" } },
      output = UNCRAFTED,
    }
  end,
  function() recipes.clayforming.remove(UNCRAFTED) end)

balanced("smithing", recipes.smithing,
  function()
    recipes.smithing.add {
      name = "moontweaks:diagnostic-smithing",
      ingredient = "game:ingot-copper",
      pattern = { { "##", "##" } },
      output = UNCRAFTED,
    }
  end,
  function() recipes.smithing.remove(UNCRAFTED) end)

balanced("barrel", recipes.barrel,
  function()
    recipes.barrel.add {
      code = "moontweaks:diagnostic-barrel",
      ingredients = {
        { code = "game:waterportion", litres = 1 },
        { code = "game:salt", quantity = 1 },
      },
      output = { code = UNCRAFTED, quantity = 1 },
    }
  end,
  function() recipes.barrel.remove(UNCRAFTED) end)

balanced("alloy", recipes.alloy,
  function()
    recipes.alloy.add {
      ingredients = {
        { code = "game:ingot-copper", minRatio = 0.4, maxRatio = 0.6 },
        { code = "game:ingot-tin", minRatio = 0.4, maxRatio = 0.6 },
      },
      output = UNCRAFTED,
    }
  end,
  function() recipes.alloy.remove(UNCRAFTED) end)

-- A meal is drawn as well as eaten, so this one carries a shape to draw it from and
-- a list of what may stand in each of its ingredients.
balanced("cooking", recipes.cooking,
  function()
    recipes.cooking.add {
      code = "moontweaks:diagnostic-cooking",
      shape = "block/food/meal/soup",
      perishableProps = {
        freshHours = { avg = 24 },
        transitionHours = { avg = 6 },
        transitionedStack = UNCRAFTED,
      },
      ingredients = {
        {
          code = "root",
          typeName = "vegetable",
          minQuantity = 1,
          maxQuantity = 2,
          validStacks = {
            { code = "game:vegetable-carrot", shapeElement = "bowl/vegetable base 1/*" },
          },
        },
      },
    }
  end,
  function() recipes.cooking.remove("moontweaks:diagnostic-cooking") end)

-- The kind-agnostic half of the module, which reaches a registry by its own code
-- rather than through a module of its own. That is how a kind another mod declared
-- is counted and thinned, and the game's own kinds answer through it too.
diag.check("recipes.kinds", function()
  local kinds = recipes.kinds()
  assert(#kinds > 0, "no recipe registries at all")

  return table.concat(kinds, ", ")
end)

diag.check("recipes.count", function()
  local both = { recipes.count("knappingrecipes"), recipes.knapping.count() }
  assert(both[1] == both[2],
    ("counted %d through the registry and %d through the kind"):format(both[1], both[2]))

  return ("%d knapping recipes, counted both ways"):format(both[1])
end)

local knappingBefore = recipes.count("knappingrecipes")

diag.check("recipes.remove", function()
  recipes.knapping.add {
    name = "moontweaks:diagnostic-by-registry",
    ingredient = "game:flint",
    pattern = { "##", "##" },
    output = UNCRAFTED,
  }

  recipes.remove { kind = "knappingrecipes", code = UNCRAFTED }
  return "queued an addition and took it back through the registry rather than the kind"
end)

diag.later("recipes.count (after the run)", function()
  local after = recipes.count("knappingrecipes")
  assert(after >= knappingBefore, ("%d before, %d after"):format(knappingBefore, after))

  return ("%d before the run, %d after -- nothing was lost"):format(knappingBefore, after)
end)
