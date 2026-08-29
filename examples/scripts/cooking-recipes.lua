local cooking = moontweaks.recipes.cooking

moontweaks.log.info("starting with " .. cooking.count() .. " cooking recipes")

-- Cooking is the odd kind. Every other recipe names one product; a meal is named
-- after whatever went into it, so a recipe carries a `code` of its own and that is
-- what identifies it, both here and when the game names the bowl.
--
-- A pot has four slots. Each ingredient says how few of them it may fill and how
-- many, and `validStacks` says what may go in one. Listing nine ingredients is
-- normal: they are alternatives, and no pot holds all of them at once. What must
-- fit together is the smallest each one asks for, and that is what is checked.

cooking.add {
  code = "moontweaks:roots",

  -- Where the meal is drawn from, as a path into the game's own assets. Required:
  -- the server writes it out for every client, and a bowl with no shape cannot be
  -- drawn. Borrowing a vanilla shape is the usual thing to do.
  shape = "block/food/meal/soup",

  -- How the meal spoils. Also required, for the same reason.
  perishableProps = {
    freshHours = { avg = 120 },
    transitionHours = { avg = 24 },
    transitionedStack = "game:rot",
  },

  isFood = true,

  ingredients = {
    -- Something poured rather than counted: `portionSizeLitres` is how much one
    -- slot of it holds.
    {
      code = "stock",
      typeName = "stock",
      minQuantity = 1,
      maxQuantity = 1,
      portionSizeLitres = 1,
      validStacks = {
        -- `shapeElement` is the part of the shape this fills in. Left out, the
        -- ingredient is in the pot but not drawn.
        { code = "game:waterportion", shapeElement = "bowl/water" },
      },
    },

    -- A wildcard here stays a wildcard: the pot matches against it as it cooks,
    -- rather than the recipe being expanded into one per variant the way a grid
    -- recipe would be.
    {
      code = "root",
      typeName = "vegetable",
      minQuantity = 1,
      maxQuantity = 3,
      validStacks = {
        { code = "game:vegetable-carrot", shapeElement = "bowl/vegetable base 1/*" },
        { code = "game:vegetable-parsnip", shapeElement = "bowl/vegetable base 1/*" },
        { code = "game:vegetable-turnip", shapeElement = "bowl/vegetable base 1/*" },
      },
    },

    -- `minQuantity = 0` makes an ingredient optional, so the meal cooks with or
    -- without it. `cookedStack` accepts the cooked form of something without
    -- listing it a second time.
    {
      code = "meat",
      typeName = "meat",
      minQuantity = 0,
      maxQuantity = 2,
      validStacks = {
        { code = "game:bushmeat-raw",
          shapeElement = "bowl/meat/*",
          cookedStack = "game:bushmeat-cooked" },
      },
    },
  },
}

-- `cooksInto` makes the pot yield an item rather than a bowl of servings, which is
-- how the game renders glue and oil.
cooking.add {
  code = "moontweaks:tallow",
  shape = "block/food/meal/liquid",
  perishableProps = {
    type = "harden",
    freshHours = { avg = 4 },
    transitionHours = { avg = 0.01 },
    transitionedStack = "game:fat",
  },
  ingredients = {
    {
      code = "fat",
      minQuantity = 2,
      maxQuantity = 4,
      validStacks = { "game:fat" },
    },
  },
  cooksInto = { code = "game:fat", quantity = 4 },
}

-- Removal names the recipe's own code rather than an asset code, and takes a
-- wildcard like every other kind.
cooking.remove("moontweaks:*")

moontweaks.log.info("done")
