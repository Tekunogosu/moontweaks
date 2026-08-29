-- Asking what else is installed here.
--
-- Every other binding is strict on purpose: a recipe or an `items.set` naming a code
-- the server does not have refuses the whole run and names the line, which is what
-- stops a typo becoming a silently missing recipe. That strictness is also why this
-- module exists. A script written for two servers — one with a mod, one without —
-- cannot simply name that mod's items and hope. It asks first, and the codes inside
-- the guarded block are only ever read on a server that has them.

local mods = moontweaks.mods

moontweaks.log.info(("%d mod(s) loaded here"):format(#mods.all()))

-- Named by identifier rather than by title: `primitivesurvival`, not
-- "Primitive Survival". A mod's own modinfo.json is where the spelling comes from,
-- and this list is what a server actually has.
for _, mod in ipairs(mods.all()) do
  moontweaks.log.info(("  %s %s (%s)"):format(mod.id, mod.version or "?", mod.name))
end

-- The guard. Nothing inside is read on a server without the mod, so the codes are
-- safe to name however strict the rest of the binding is.
if mods.isEnabled("primitivesurvival") then
  moontweaks.log.info("primitive survival is here; adding its recipes")

  -- moontweaks.recipes.grid.add { ... naming primitivesurvival: codes ... }
end

-- `get` answers the same question and the version besides, so a script that wants
-- both asks once. It gives nil where the mod is not loaded, which is the check.
local survival = mods.get("game")
if survival then
  moontweaks.log.info(("running against %s %s"):format(survival.name, survival.version or "?"))
end

-- Versions are strings as the mod wrote them, so compare them as strings or parse
-- them yourself. There is no ordering here to lean on.
local self = mods.get("moontweaks")
if self then
  moontweaks.log.info("moontweaks " .. (self.version or "?") .. " is running this script")
end
