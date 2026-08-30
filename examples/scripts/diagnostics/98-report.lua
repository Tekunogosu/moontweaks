-- What the suite knows by the time the scripts have finished running.
--
-- This is the first of three reports, and the smallest. The world is not up yet, no
-- timer has fired and nobody is connected, so the functions needing any of those are
-- still untouched here — which is the point: a name missing from this report and
-- present in the next one is a function that needs the world, and one still missing
-- after `/diag player` is a function this server cannot reach at all.
--
-- The second report writes itself when the world comes up. The third is `/diag`.

diag.report("load")

moontweaks.log.info("[diag] the world checks run when the world comes up; "
  .. "log in and type /diag player for the rest, then /diag for the whole picture")
