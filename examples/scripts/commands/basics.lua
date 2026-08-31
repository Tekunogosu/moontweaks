local commands = moontweaks.commands
local players  = moontweaks.players
local world    = moontweaks.world

-- A command a script declares needs nothing installed on a player's machine: the
-- client sends the line as it was typed and the server reads it, so everyone
-- already connected can use it. Declaring one still happens as the server loads,
-- so a new command wants a restart the way a new recipe does.

-- What `/myrequest give` will hand over. Keeping the list here rather than in the
-- handler means the same table decides what the command accepts and what it gives,
-- so the two cannot disagree.
-- A pie is drawn entirely from what is in it: the block's own shape is invisible,
-- so one with no `ucontents` is a real stack that renders as nothing at all. Giving
-- somebody a pie therefore means giving them its filling too, which is why this
-- names attributes where the bread does not.
local function pie(filling)
  local contents = { { type = "item", code = "dough-spelt", stackSize = 2 } }
  for _ = 1, 4 do
    contents[#contents + 1] = { type = "item", code = filling, stackSize = 2 }
  end
  contents[#contents + 1] = { type = "item", code = "dough-spelt", stackSize = 2 }

  return {
    code = "game:pie-perfect",
    attributes = { ucontents = contents, pieSize = 4, topCrustType = 1, bakeLevel = 2 },
  }
end

local ONOFFER = {
  pie   = pie("fruit-redapple"),
  bread = { code = "game:bread-spelt-perfect", quantity = 2 },
}

local function named()
  local names = {}
  for name in pairs(ONOFFER) do names[#names + 1] = name end
  table.sort(names)
  return names
end

commands.add {
  name = "myrequest",
  description = "Ask the server for something",

  -- Inherited by everything below it. `chat` is the privilege every player who can
  -- talk already holds; name `controlserver` for an administrators' command.
  privilege = "chat",

  subcommands = {
    {
      name = "give",
      description = "Hand over one of the things on offer",

      -- A handler is given no player when the server console runs a command, and
      -- this one has somebody to give something to or it has nothing to do.
      requiresPlayer = true,

      -- `values` closes the argument to exactly these words, and the game offers
      -- them as completions and rejects anything else before the handler runs.
      args = {
        { name = "what", type = "word", values = named() },
      },

      handler = function(e)
        local wanted = ONOFFER[e.args.what]

        -- Returning a table with `error` shows the caller a failure rather than a
        -- plain message. Returning a string is a success; returning nothing at all
        -- is a command that did its work quietly.
        if not wanted then
          return { error = "nothing here is called " .. e.args.what }
        end

        -- A code written into a handler is only checked when the handler runs, so a
        -- wrong one is a failure at that moment rather than at load. This is the one
        -- place a code cannot be checked for you.
        if not players.give(e.player, wanted) then
          -- Nothing fitted, and what does not fit is gone. Dropping it where they
          -- stand is what the game itself does when a player's hands are full.
          -- Naming them as the owner is what stops it going straight back to them
          -- before they have seen it land.
          -- Thrown gently the way they are facing, so it lands in front of them
          -- rather than inside them. The game's own throw is about this fast;
          -- velocity is per physics step, so larger numbers go a very long way.
          local at  = players.position(e.player)
          local dir = players.facing(e.player)
          world.dropItem {
            stack = wanted,
            x = at.x, y = at.y + 1.2, z = at.z,
            velocity = { x = dir.x * 0.1, y = dir.y * 0.1 + 0.05, z = dir.z * 0.1 },
            owner = e.player,
          }
          return "your inventory is full, so it is on the ground at your feet"
        end

        return ("here is your %s"):format(e.args.what)
      end,
    },

    {
      name = "list",
      description = "Say what is on offer",
      handler = function()
        return "on offer: " .. table.concat(named(), ", ")
      end,
    },
  },
}

-- A command may take values of several kinds. `player` is handed to the handler as
-- the identifier every `moontweaks.players` function takes, so one command can act
-- on somebody other than whoever typed it.
commands.add {
  name = "feed",
  description = "Fill somebody up",
  privilege = "controlserver",
  args = {
    { name = "who", type = "player" },
  },
  handler = function(e)
    players.setSatiety(e.args.who, players.maxSatiety(e.args.who))
    return "fed them"
  end,
}

-- ## Picking a name
--
-- A script's commands are declared while the server loads, before the mods that ship
-- with the game declare theirs. Taking a name one of them wants is not a clash this
-- mod can refuse — it registers first and wins — and the server stops when the other
-- one cannot have it.
--
-- The names already spoken for include `/weather`, `/time`, `/setblock`, `/group`,
-- `/waypoint`, `/tutorial` and `/npc`. Prefer a name nothing else would reach for,
-- or put everything under one command of your own and use subcommands beneath it,
-- which is what `/moontweaks` itself does.
