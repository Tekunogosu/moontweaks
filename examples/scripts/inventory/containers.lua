-- Chests, barrels and anything else with slots standing in the world.
--
-- A block position names whatever container is there. A position holding a block that
-- is not a container is refused by name rather than answered with an empty set, so a
-- script pointing at the wrong place finds out rather than concluding the chest was
-- empty.

local commands  = moontweaks.commands
local events    = moontweaks.events
local inventory = moontweaks.inventory
local players   = moontweaks.players
local world     = moontweaks.world

-- Reading a chest without opening it.
commands.add {
  name = "peek",
  description = "Say what is in the container you are looking at",
  requiresPlayer = true,
  handler = function(e)
    local at = players.looking(e.player)
    if not at then return { error = "you are not looking at a block." } end

    local where = { x = at.x, y = at.y, z = at.z }

    -- Not every block has slots. Catching the refusal turns it into something the
    -- caller reads rather than something the log swallows.
    local ok, held = pcall(function() return inventory.list(where) end)
    if not ok then return { error = tostring(held) } end

    if #held == 0 then return ("Empty (%d slots)."):format(inventory.size(where)) end

    local lines = {}
    for _, slot in ipairs(held) do
      lines[#lines + 1] = ("  %2d  %s x%d"):format(slot.slot, slot.name, slot.quantity)
    end

    return table.concat(lines, "\n")
  end,
}

-- Filling one. `put` merges into part-full slots before it uses empty ones, the way
-- the game does when a player picks something up, and says how many fitted.
commands.add {
  name = "stock",
  description = "Put something into the container you are looking at",
  requiresPlayer = true,
  privilege = "controlserver",
  args = {
    { name = "code", type = "word" },
    { name = "quantity", type = "int" },
  },
  handler = function(e)
    local at = players.looking(e.player)
    if not at then return { error = "you are not looking at a block." } end

    local fitted = inventory.put(
      { x = at.x, y = at.y, z = at.z },
      { code = e.args.code, quantity = e.args.quantity })

    if fitted < e.args.quantity then
      return ("Only %d of %d fitted; it is full."):format(fitted, e.args.quantity)
    end

    return ("Put %d in."):format(fitted)
  end,
}

-- One slot at a time. `setSlot` replaces whatever was there rather than merging with
-- it, so read the slot first if what it held mattered. `clearSlot` says whether
-- anything was in it, which is how a script tells "emptied it" from "it was already
-- empty".
commands.add {
  name = "tidy",
  description = "Empty the first slot of the container you are looking at",
  requiresPlayer = true,
  privilege = "controlserver",
  handler = function(e)
    local at = players.looking(e.player)
    if not at then return { error = "you are not looking at a block." } end

    local where = { x = at.x, y = at.y, z = at.z }
    -- What is there is read before it is emptied, and read is what decides whether
    -- there is anything to do: a slot that answers with nothing has nothing to put
    -- back into the world afterwards.
    local first = inventory.slot(where, 1)
    if not first then return "That slot was already empty." end

    if not inventory.clearSlot(where, 1) then return "That slot could not be emptied." end

    -- Emptying a slot destroys what was in it, so put it back into the world rather
    -- than letting it vanish.
    world.dropItem {
      stack = { code = first.code, quantity = first.quantity },
      x = at.x + 0.5, y = at.y + 1, z = at.z + 0.5,
    }

    return ("Turned out %s x%d."):format(first.name, first.quantity)
  end,
}

-- A hopper of sorts: move everything from one chest into another, a slice at a time
-- so a big move never holds the server up. Reading the list fresh each tick is what
-- keeps it correct while somebody else is using either chest.
local function drain(from, to, onDone)
  moontweaks.server.every(500, function()
    local held = inventory.list(from)
    if #held == 0 then
      onDone()
      return false
    end

    local one = held[1]
    local moved = inventory.put(to, { code = one.code, quantity = one.quantity })
    if moved == 0 then
      onDone()
      return false
    end

    inventory.take(from, { code = one.code, quantity = moved })
    return true
  end)
end

-- Breaking a sign between two chests empties the one below into the one above.
events.didBreakBlock(function(e)
  if e.block ~= "game:sign-ground-north" then return end

  local below = { x = e.x, y = e.y - 1, z = e.z }
  local above = { x = e.x, y = e.y + 1, z = e.z }

  local ok = pcall(function()
    inventory.size(below)
    inventory.size(above)
  end)
  if not ok then return end

  local who = e.player
  drain(below, above, function()
    if players.isOnline(who) then players.say(who, "The chest below is empty.") end
  end)
end)
