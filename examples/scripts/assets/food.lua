-- Eating: what it settles, what it counts towards, and what is left in hand.
--
-- The game calls this quantity satiety on a food and saturation on a player. It is
-- one quantity, and it is satiety throughout here.

local items = moontweaks.items

-- What eating it does, and the bowl it leaves behind.
items.set {
  code = "game:redmeat-cooked",
  nutrition = {
    foodCategory = "protein",
    satiety = 240,
    health = 1.5,
    satietyLossDelay = 30,
    eatenStack = { code = "game:bowl-blue-fired", quantity = 1 },
  },
}

-- Flint is not food. Naming nutrition it never had gives it some, and a negative
-- `health` is how something is made to cost rather than heal.
items.set {
  code = "game:flint",
  nutrition = {
    foodCategory = "vegetable",
    satiety = 5,
    health = -2,
    intoxication = 0.2,
  },
}

-- How it changes once it stops being fresh. A list rather than one, because a thing
-- may change in more than one way and which happens depends on where it is kept:
-- meat left out rots, and the same meat in a barrel of brine cures. Writing the key
-- replaces every change the item had, so the list says all of them.
items.set {
  code = "game:redmeat-raw",
  transitionableProps = {
    {
      type = "perish",
      freshHours = { avg = 60 },
      transitionHours = { avg = 24 },
      transitionedStack = { code = "game:rot" },
      transitionRatio = 1,
    },
    {
      type = "cure",
      freshHours = { avg = 0 },
      transitionHours = { avg = 100 },
      transitionedStack = { code = "game:redmeat-cured" },
      transitionRatio = 1,
    },
  },
}

-- Where something shows up in the creative inventory. The tabs are the plain form;
-- `creativeInventoryStacks` is for something that should appear there more than once,
-- each entry carrying different data.
items.set {
  code = "game:flint",
  creativeInventoryTabs = { "general", "items" },
}

moontweaks.log.info("food properties done")
