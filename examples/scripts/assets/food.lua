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

moontweaks.log.info("food properties done")
