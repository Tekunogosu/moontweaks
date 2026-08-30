-- The item and block registries, and retuning what is in them.
--
-- Both setters are checked against one asset apiece, writing the value the game
-- already loaded — a stick stacks to 64 and a pineapple to 8 in the game's own
-- files, so the write lands and changes nothing. That is on purpose: neither module
-- offers a reader, so there is no value to capture and put back the way every other
-- setter in this suite is checked, and writing what is already there is the nearest
-- honest thing.
--
-- Worth knowing either way: a change made through these lives in memory and never
-- reaches the game's asset files. A server started without this suite has none of
-- them, whatever any run did.

local blocks = moontweaks.blocks
local items  = moontweaks.items

diag.check("items.count", function()
  local held = items.count()
  assert(held > 0, "the registry holds no items")

  return ("%d item(s)"):format(held)
end)

diag.check("blocks.count", function()
  local held = blocks.count()
  assert(held > 0, "the registry holds no blocks")

  return ("%d block(s)"):format(held)
end)

diag.check("items.set", function()
  items.set { code = "game:stick", maxStackSize = 64 }
  return "wrote game:stick maxStackSize = 64, the value the game loads it with"
end)

diag.check("blocks.set", function()
  blocks.set { code = "game:pineapple", maxStackSize = 8 }
  return "wrote game:pineapple maxStackSize = 8, the value the game loads it with"
end)

-- The refusal path matters as much as the writing one. A code the server does not
-- hold is refused by name rather than quietly matching nothing, and a suite that
-- never checks that cannot tell a working guard from an absent one.
diag.check("items.set (refuses an unknown code)", function()
  local ok, why = pcall(items.set, { code = "game:no-such-item-anywhere", maxStackSize = 1 })

  assert(not ok, "a code nothing holds was accepted")
  assert(tostring(why):find("no%-such%-item%-anywhere"), "refused without naming the code: " .. tostring(why))

  return "refused, naming the code"
end)
