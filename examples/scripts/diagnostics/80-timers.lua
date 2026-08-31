-- Work put off until later, and work done over and over.
--
-- A timer asked for in a script's body starts once the whole run is known to have
-- succeeded, so neither of these has fired by the time the load report is written.
-- Both record their result into the tally as they fire, which is why the figures in
-- `/diag report` are higher than the ones in the log at startup.
--
-- That is also what makes these two the check on the tally itself: a `/diag report`
-- showing them passed is a report reading state that a handler wrote after the
-- scripts had finished.

local server = moontweaks.server

-- Once, later. Two seconds is long enough to be plainly after the run and short
-- enough to have happened before anybody thinks to ask for a report.
diag.used("server.after")
server.after(2000, function(e)
  diag.check("server.after", function()
    assert(type(e.dt) == "number", "the handler was given no elapsed time")
    return ("fired once, %0.2fs after it was asked for"):format(e.dt)
  end)
end)

-- Over and over, until it answers false. Three firings prove both halves: that it
-- repeats, and that answering false is what stops it.
diag.used("server.every")
local firings = 0

server.every(1000, function(e)
  firings = firings + 1

  if firings < 3 then return true end

  diag.check("server.every", function()
    assert(type(e.dt) == "number", "the handler was given no elapsed time")
    return ("fired %d times a second apart, then stopped by answering false"):format(firings)
  end)

  return false
end)
