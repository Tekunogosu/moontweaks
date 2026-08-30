-- Moving things: taking payment and handing goods over.
--
-- The one rule that matters here is that both `take` and `put` say how much they
-- actually moved, and that number is not always what was asked for. A bag may not
-- hold enough; a chest may not have room. Read what came back rather than assuming,
-- because taking half a payment and taking none are told apart by that number and by
-- nothing else.

local commands  = moontweaks.commands
local events    = moontweaks.events
local inventory = moontweaks.inventory
local players   = moontweaks.players
local world     = moontweaks.world

-- A shop counter. The price and the goods live in one table so that what is charged
-- and what is handed over cannot drift apart.
local STOCK = {
  rope    = { price = { code = "game:flaxfibers", quantity = 8 },  goods = { code = "game:rope",       quantity = 1 } },
  candle  = { price = { code = "game:beeswax",    quantity = 2 },  goods = { code = "game:candle",     quantity = 4 } },
  pickaxe = { price = { code = "game:ingot-iron", quantity = 3 },  goods = { code = "game:pickaxe-iron", quantity = 1 } },
}

local function onOffer()
  local names = {}
  for name in pairs(STOCK) do names[#names + 1] = name end
  table.sort(names)
  return names
end

commands.add {
  name = "buy",
  description = "Buy something from the server",
  requiresPlayer = true,
  args = { { name = "what", type = "word", values = onOffer() } },
  handler = function(e)
    local deal = STOCK[e.args.what]
    local bags = { player = e.player, which = "backpack" }

    -- Look before taking. Charging somebody who cannot pay and then failing to hand
    -- anything over is the shape of bug this ordering exists to prevent.
    local has = inventory.count(bags, deal.price.code)
    if has < deal.price.quantity then
      return { error = ("That costs %d %s and you have %d.")
        :format(deal.price.quantity, deal.price.code, has) }
    end

    -- Take first, and check what actually came out. Between the count above and this
    -- line nothing else has run, but reading the answer costs nothing and means the
    -- script is never wrong about what it charged.
    local paid = inventory.take(bags, deal.price)
    if paid < deal.price.quantity then
      -- Put back whatever was taken, so a partial charge is never kept.
      inventory.put(bags, { code = deal.price.code, quantity = paid })
      return { error = "Something went wrong taking payment; nothing was charged." }
    end

    -- Hand over, and deal honestly with a full bag: what did not fit goes on the
    -- floor at their feet rather than quietly evaporating.
    local given = inventory.put(bags, deal.goods)
    local short = deal.goods.quantity - given

    if short > 0 then
      local at = players.position(e.player)
      world.dropItem {
        stack = { code = deal.goods.code, quantity = short },
        x = at.x, y = at.y, z = at.z,
        owner = e.player,
      }
      return ("Bought. Your bags were full, so %d fell at your feet."):format(short)
    end

    return ("Bought %d %s for %d %s.")
      :format(given, e.args.what, paid, deal.price.code)
  end,
}

-- A toll gate. Taking a wildcard means any ingot pays, rather than one particular
-- metal, which is what makes a charge feel like a price rather than a puzzle.
local GATE = "game:chest-east"

events.didUseBlock(function(e)
  if e.block ~= GATE then return end

  local bags = { player = e.player, which = "backpack" }
  local paid = inventory.take(bags, { code = "game:ingot-*", quantity = 1 })

  if paid == 0 then
    players.warn(e.player, "The gate wants an ingot of some kind. Any will do.")
    return
  end

  players.say(e.player, "The gate opens.")
  world.setBlock("game:air", e.x, e.y + 1, e.z)
end)

-- Selling in the other direction: take what somebody is holding and pay for it. This
-- is where `held` earns its place — the thing in their hand is what they mean, and
-- nothing else has to be named.
commands.add {
  name = "sell",
  description = "Sell what you are holding",
  requiresPlayer = true,
  handler = function(e)
    local hand = inventory.held(e.player)
    if not hand then return { error = "you are holding nothing." } end

    local paid = math.max(1, math.floor(hand.quantity / 2))
    inventory.clearHeld(e.player)
    inventory.put({ player = e.player, which = "backpack" }, { code = "game:gear-rusty", quantity = paid })

    return ("Sold %s x%d for %d rusty gear."):format(hand.name, hand.quantity, paid)
  end,
}
