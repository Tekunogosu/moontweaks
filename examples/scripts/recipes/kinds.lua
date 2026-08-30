-- Recipes belonging to a mod this one has never heard of.
--
-- The modules beside this one — `recipes.grid`, `recipes.knapping` and the rest —
-- each know one kind properly: what it makes, how it is written, and what a mistake
-- in one looks like. Reach for those.
--
-- `moontweaks.recipes` itself is the fallback for kinds they do not cover: one a mod
-- registered for itself, whose shape nothing here knows. It can count them and take
-- them away, matching on what a recipe's output resolved to. It cannot add one,
-- because building a recipe means knowing its shape.

local commands = moontweaks.commands
local recipes  = moontweaks.recipes

-- Which kinds this server holds, by the code each is registered under. A mod chooses
-- its own, so this is how you find out what to name.
commands.add {
  name = "recipekinds",
  description = "List the recipe kinds this server holds",
  handler = function()
    return table.concat(recipes.kinds(), ", ")
  end,
}

-- The game's own kinds are in that list too, under the survival mod's codes. Counting
-- through here and counting through their own module agree, because they are two
-- views of one list rather than two lists.
moontweaks.log.info(("%d knapping recipes, counted both ways: %d")
  :format(recipes.count("knappingrecipes"), recipes.knapping.count()))

-- Taking one away from a kind nothing here knows the shape of. Guarded on the mod
-- being present: naming a kind the server does not have is refused with the line, and
-- a script meant for two servers should not fail on the one without the mod.
if moontweaks.mods.isEnabled("primitivesurvival") then
  recipes.remove {
    kind = "primitivesurvival:knifeblade",
    code = "primitivesurvival:*",
  }
end
