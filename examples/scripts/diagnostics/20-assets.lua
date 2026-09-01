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
local tags   = moontweaks.tags

--- A tag nothing else could be carrying, so what carries it afterwards is this suite's
--- doing rather than something the server already believed.
local MARK = "moontweaks:diagnostics-mark"

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

-- ## Tags of this server's own
--
-- Declaring one, putting it on something, and then selecting by it. The last step is
-- what makes this a measurement: a tag that registered but never landed on an asset
-- would pass the first two checks and fail this one.
--
-- These run in a script's body on purpose. The registry closes as soon as the scripts
-- have run, so this is the only place the first of them can work at all.
diag.check("tags.add", function()
  tags.add(MARK)

  -- Declaring the same name twice is two scripts meaning the same thing, not a clash.
  tags.add { MARK, MARK }

  return ("declared %s for this run"):format(MARK)
end)

diag.check("items.set (addTags)", function()
  -- Added rather than set: a stick carries tags the game's own recipes select it by,
  -- and this check must not be what takes them away.
  items.set { code = "game:stick", addTags = MARK }

  return ("put %s on game:stick, on top of what it already carried"):format(MARK)
end)

diag.check("items.set (selects by a declared tag)", function()
  -- Writing the value the game already loads, so what is measured is the selection
  -- rather than the change: reaching nothing would raise, and reaching the stick
  -- leaves it exactly as it was.
  items.set { tags = { MARK }, maxStackSize = 64 }

  return ("selected by %s and wrote the value already there"):format(MARK)
end)

diag.check("items.set (refuses an undeclared tag)", function()
  local ok, why = pcall(items.set,
    { code = "game:stick", addTags = "moontweaks:never-declared-anywhere" })

  assert(not ok, "a tag nothing declared was accepted")
  assert(tostring(why):find("never%-declared%-anywhere"),
    ("the refusal did not name the tag: %s"):format(tostring(why)))

  return "refused an undeclared tag and named it"
end)
