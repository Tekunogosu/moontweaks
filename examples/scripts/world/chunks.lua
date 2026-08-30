-- Acting on a place nobody is standing.
--
-- Everything that writes to the world does nothing at all in a chunk that is not
-- loaded, and says nothing about it. That is the quiet failure this pair exists to
-- avoid: `world.loadChunk` asks for a chunk, and `events.chunkColumnLoaded` says when
-- one has arrived.
--
-- A column is every chunk at one place on the map, floor to ceiling. Loading is
-- raised once per column; unloading is raised once per chunk in it, which is why the
-- two events are named differently and carry different tables.

local events  = moontweaks.events
local players = moontweaks.players
local world   = moontweaks.world

-- A busy server loads columns constantly, so this handler runs often. Decide whether
-- the column is one worth caring about before doing anything that costs.
events.chunkColumnLoaded(function(e)
  local seen = world.getData("visited") or {}
  local key = ("%d,%d"):format(e.chunkX, e.chunkZ)
  if seen[key] then return end

  seen[key] = true
  world.setData("visited", seen)
end)

-- Unloading is per chunk rather than per column, so `chunkY` says which layer went.
-- This is where anything remembered about those blocks is forgotten; the blocks
-- themselves are on their way out and should not be reached.
events.chunkUnloaded(function(e)
  moontweaks.log.info(("chunk %d %d %d let go"):format(e.chunkX, e.chunkY, e.chunkZ))
end)

-- Asking for a chunk and then acting on it. `loadChunk` answers true when it was
-- already there, so the wait is only needed when it was not.
moontweaks.commands.add {
  name = "peek",
  description = "Say what the ground is like a long way east of you",
  requiresPlayer = true,
  handler = function(e)
    local at = players.position(e.player)
    local x, z = math.floor(at.x) + 600, math.floor(at.z)

    local function report()
      local ground = world.surfaceAt(x, z)
      if not ground then
        players.say(e.player, "That column is still not loaded.")
        return
      end

      players.say(e.player, ("Ground there is at y=%d, %s.")
        :format(ground, world.blockAt(x, ground - 1, z) or "nothing"))
    end

    if world.loadChunk(x, z) then
      report()
      return "Already loaded."
    end

    -- The column arrives over the following ticks, so the answer waits for it.
    moontweaks.server.after(3000, report)
    return "Asked for it. Reporting in a moment."
  end,
}
