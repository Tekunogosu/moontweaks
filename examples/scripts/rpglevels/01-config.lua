-- RPG levels: a levelling system built out of nothing but Lua.
--
-- This file is the whole of what a server operator is expected to edit. It holds data
-- and no behaviour: the curve, what each creature is worth, and what a milestone hands
-- over. Everything else in this folder reads from here.
--
-- The files load in name order and share one interpreter, so each declares the module
-- table it needs rather than trusting another file to have run first.

rpglevels = rpglevels or {}

rpglevels.config = {
  -- Where a standing is kept. World data rather than account data: levels earned in
  -- this world belong to this world, and a fresh world should start everybody at one.
  storageKey = "rpglevels",

  -- What it takes to go from one level to the next: xpBase * level ^ xpExponent,
  -- rounded down. Level 1 costs 15, level 10 costs 237, level 30 costs 888, and the
  -- whole climb to 60 comes to roughly 55,000.
  xpBase = 15,
  xpExponent = 1.2,
  maxLevel = 60,

  -- How often a milestone comes round. Every fifth level hands over an item.
  rewardEvery = 5,

  -- Who to credit for a kill the game named no killer for.
  --
  -- MoonTweaks 0.28.0 reads `byPlayer` from the damage's `CauseEntity`, which Vintage
  -- Story fills in for a projectile alone: the entity that threw the arrow. A melee
  -- blow leaves it null and names the attacker in `SourceEntity` instead, so a kill
  -- made by hand arrives with nobody named. Until the mod reads `GetCauseEntity()`,
  -- a death by a blow with no killer named is credited to the nearest player within
  -- this many blocks of where it fell.
  --
  -- Set it to 0 to turn the guess off and credit projectile kills alone.
  meleeCreditRange = 12,
}

-- The damage kinds that mean somebody swung something. A death by falling, drowning
-- or cold is nobody's kill, so only these are ever credited to a player standing near.
rpglevels.config.meleeCauses = {
  bluntattack = true,
  slashingattack = true,
  piercingattack = true,
}

-- What a kill is worth, as rules read top to bottom with the first match winning. A
-- rule matches when the code starts with `prefix` and, where one is named, holds
-- `contains` somewhere in it — which is how an age caught in the middle of a variant
-- code (`game:wolf-eurasian-baby-male`) is told apart from its parent.
--
-- Codes are matched as plain text rather than as patterns, so a hyphen is a hyphen.
-- Anything no rule names is worth nothing, which is what keeps a straw dummy, a boat
-- and a trader off the ledger without a rule apiece.
rpglevels.config.killXp = {
  -- The two things in this world that are meant to end a run.
  { prefix = "game:erel-",                     xp = 500 },
  { prefix = "game:eidolon-",                  xp = 400 },

  -- Bowtorn, from the surface down. Deeper is worse, and the gearfoot is worst.
  { prefix = "game:bowtorn-gearfoot",          xp = 120 },
  { prefix = "game:bowtorn-nightmare",         xp = 100 },
  { prefix = "game:bowtorn-corrupt",           xp = 75 },
  { prefix = "game:bowtorn-tainted",           xp = 55 },
  { prefix = "game:bowtorn-deep",              xp = 40 },
  { prefix = "game:bowtorn-",                  xp = 25 },

  -- Shivers. The three named forms are rarer than the graded ones and pay like it.
  { prefix = "game:shiver-bellhead",           xp = 110 },
  { prefix = "game:shiver-stilt",              xp = 105 },
  { prefix = "game:shiver-deepsplit",          xp = 100 },
  { prefix = "game:shiver-nightmare",          xp = 95 },
  { prefix = "game:shiver-corrupt",            xp = 70 },
  { prefix = "game:shiver-tainted",            xp = 50 },
  { prefix = "game:shiver-deep",               xp = 35 },
  { prefix = "game:shiver-",                   xp = 22 },

  -- Drifters, which is what most of this will be paid in.
  { prefix = "game:drifter-double-headed",     xp = 80 },
  { prefix = "game:drifter-nightmare",         xp = 65 },
  { prefix = "game:drifter-corrupt",           xp = 45 },
  { prefix = "game:drifter-tainted",           xp = 30 },
  { prefix = "game:drifter-deep",              xp = 20 },
  { prefix = "game:drifter-",                  xp = 10 },

  -- Locusts. A sawblade is the one worth going out of your way for.
  { prefix = "game:locust-corrupt-sawblade",   xp = 70 },
  { prefix = "game:locust-corrupt",            xp = 55 },
  { prefix = "game:locust-bronze",             xp = 30 },
  { prefix = "game:locust-",                   xp = 30 },

  -- Predators. A polar bear is the one that hunts you back.
  { prefix = "game:bear-polar",                xp = 55 },
  { prefix = "game:bear-",                     xp = 40 },
  { prefix = "game:wolf-",   contains = "-baby-", xp = 4 },
  { prefix = "game:wolf-",                     xp = 18 },
  { prefix = "game:hyena-",  contains = "-baby-", xp = 4 },
  { prefix = "game:hyena-",                    xp = 16 },

  -- Game worth hunting. A moose is large enough to be a fight rather than a meal.
  { prefix = "game:deer-moose", contains = "-baby-", xp = 4 },
  { prefix = "game:deer-moose",                xp = 20 },
  { prefix = "game:deer-elk",   contains = "-baby-", xp = 3 },
  { prefix = "game:deer-elk",                  xp = 14 },
  { prefix = "game:deer-",   contains = "-baby-", xp = 2 },
  { prefix = "game:deer-",                     xp = 6 },
  { prefix = "game:pig-",    contains = "-baby-", xp = 2 },
  { prefix = "game:pig-",                      xp = 7 },
  { prefix = "game:goat-",   contains = "-baby-", xp = 2 },
  { prefix = "game:goat-",                     xp = 5 },
  { prefix = "game:sheep-",  contains = "-baby-", xp = 2 },
  { prefix = "game:sheep-",                    xp = 4 },
  { prefix = "game:gazelle-",contains = "-baby-", xp = 2 },
  { prefix = "game:gazelle-",                  xp = 4 },
  { prefix = "game:fox-",    contains = "-baby-", xp = 1 },
  { prefix = "game:fox-",                      xp = 4 },
  { prefix = "game:raccoon-",contains = "-baby-", xp = 1 },
  { prefix = "game:raccoon-",                  xp = 3 },
  { prefix = "game:hare-",   contains = "-baby-", xp = 1 },
  { prefix = "game:hare-",                     xp = 2 },

  -- Small things, worth acknowledging rather than worth hunting.
  { prefix = "game:beemob",                    xp = 5 },
  { prefix = "game:chicken-",                  xp = 1 },
  { prefix = "game:fish-",                     xp = 1 },
}

-- What a milestone hands over, in tiers matched to where a player of that level is in
-- the game. The first tier whose `upTo` reaches the level is the one used, so the last
-- must be open-ended for a server that raises `maxLevel`.
--
-- One entry is drawn at random from the tier. A `label` travels with each stack
-- because a script has the code to hand and not the name the game prints; write it as
-- a bare noun, since it is read out after a count.
rpglevels.config.rewardTiers = {
  {
    upTo = 10,
    name = "a full belly",
    items = {
      { code = "game:bread-spelt-perfect", quantity = 4, label = "spelt bread" },
      { code = "game:bushmeat-cooked",     quantity = 6, label = "cooked bushmeat" },
      { code = "game:cheese-cheddar-4slice",             label = "wheel of cheddar" },
      { code = "game:fruit-blueberry",     quantity = 8, label = "blueberries" },
      { code = "game:vegetable-carrot",    quantity = 6, label = "carrots" },
      { code = "game:bandage-clean",       quantity = 3, label = "clean bandage" },
      { code = "game:candle",              quantity = 4, label = "candle" },
      { code = "game:firestarter",                       label = "fire starter" },
      { code = "game:flaxtwine",           quantity = 8, label = "flax twine" },
    },
  },
  {
    upTo = 20,
    name = "the copper age",
    items = {
      { code = "game:pickaxe-copper",            label = "copper pickaxe" },
      { code = "game:axe-felling-copper",        label = "copper felling axe" },
      { code = "game:knife-generic-copper",      label = "copper knife" },
      { code = "game:shovel-copper",             label = "copper shovel" },
      { code = "game:prospectingpick-copper",    label = "copper prospecting pick" },
      { code = "game:armor-body-lamellar-copper",label = "copper lamellar cuirass" },
      { code = "game:ingot-copper", quantity = 4,label = "copper ingot" },
      { code = "game:clothes-foot-commoner-boots",label = "pair of commoner's boots" },
      { code = "game:rope",         quantity = 4, label = "rope" },
    },
  },
  {
    upTo = 30,
    name = "the bronze age",
    items = {
      { code = "game:pickaxe-tinbronze",          label = "bronze pickaxe" },
      { code = "game:axe-felling-tinbronze",      label = "bronze felling axe" },
      { code = "game:saw-tinbronze",              label = "bronze saw" },
      { code = "game:scythe-tinbronze",           label = "bronze scythe" },
      { code = "game:spear-generic-tinbronze",    label = "bronze spear" },
      { code = "game:armor-body-scale-tinbronze", label = "bronze scale cuirass" },
      { code = "game:ingot-tinbronze", quantity = 4, label = "bronze ingot" },
      { code = "game:backpack-normal",            label = "leather backpack" },
      { code = "game:clothes-hand-hunter-gloves", label = "pair of hunter's gloves" },
    },
  },
  {
    upTo = 40,
    name = "the iron age",
    items = {
      { code = "game:pickaxe-iron",                 label = "iron pickaxe" },
      { code = "game:axe-felling-iron",             label = "iron felling axe" },
      { code = "game:hammer-iron",                  label = "iron hammer" },
      { code = "game:spear-generic-iron",           label = "iron spear" },
      { code = "game:armor-body-chain-iron",        label = "iron chain hauberk" },
      { code = "game:armor-head-brigandine-iron",   label = "iron brigandine helm" },
      { code = "game:ingot-iron", quantity = 4,     label = "iron ingot" },
      { code = "game:backpack-sturdy",              label = "sturdy backpack" },
      { code = "game:clothes-foot-knee-high-fur-boots", label = "pair of knee-high fur boots" },
    },
  },
  {
    upTo = 50,
    name = "the age of steel",
    items = {
      { code = "game:pickaxe-steel",            label = "steel pickaxe" },
      { code = "game:axe-felling-steel",        label = "steel felling axe" },
      { code = "game:saw-steel",                label = "steel saw" },
      { code = "game:shears-steel",             label = "steel shears" },
      { code = "game:spear-generic-steel",      label = "steel spear" },
      { code = "game:armor-body-plate-steel",   label = "steel breastplate" },
      { code = "game:armor-legs-plate-steel",   label = "steel plate greaves" },
      { code = "game:armor-head-plate-steel",   label = "steel plate helm" },
      { code = "game:ingot-steel", quantity = 4,label = "steel ingot" },
    },
  },
  {
    -- Open-ended on purpose: raising maxLevel must not leave a milestone with no tier.
    upTo = math.huge,
    name = "what falls from the sky",
    items = {
      { code = "game:pickaxe-meteoriciron",           label = "meteoric iron pickaxe" },
      { code = "game:spear-generic-meteoriciron",     label = "meteoric iron spear" },
      { code = "game:armor-body-plate-meteoriciron",  label = "meteoric iron breastplate" },
      { code = "game:armor-legs-plate-meteoriciron",  label = "meteoric iron greaves" },
      { code = "game:armor-head-plate-meteoriciron",  label = "meteoric iron helm" },
      { code = "game:ingot-meteoriciron", quantity = 4, label = "meteoric iron ingot" },
      { code = "game:gear-temporal", quantity = 2,    label = "temporal gear" },
      { code = "game:clothes-upperbody-aristocrat-shirt", label = "aristocrat's shirt" },
      { code = "game:clothes-head-gem-encrusted-fur-hat", label = "gem-encrusted fur hat" },
    },
  },
}
