# Performance

A suite that measures what MoonTweaks costs on the server it is running on, and
writes the figures into the log. The numbers in the project's README came from this,
on one machine; the numbers it prints for you came from yours, which makes them the
ones worth planning against.

It changes nothing permanently. The block readings happen in a box of air above
spawn, and every block in that box is read before it is touched and written back
afterwards.

## Installing it

Copy the folder into the server's script folder and restart:

```sh
cp -r examples/scripts/performance <data path>/ModConfig/moontweaks/scripts/
```

Take it out again by deleting that folder and restarting.

## Reading it

Fifteen seconds after the world comes up, the suite takes every reading twice and
logs the second set under `[perf] ---- what this server costs ----`. Both the wait
and the second pass are deliberate: spawn chunks are still being generated when the
world first comes up, and .NET compiles a method properly only after it has watched
it run, so a figure taken immediately is a figure taken against a busy machine
running half-compiled code.

```sh
grep '\[perf\]' <data path>/Logs/server-main.log
```

A reading is nanoseconds per operation, with the loop that drove it already taken
off. `/perf` says the same thing on screen.

## Taking them again

| Command | What it does |
| --- | --- |
| `/perf` | The standing figures |
| `/perf calls` | Take the crossing and interpreter readings again |
| `/perf world` | Take the block readings again, in the chunks around you |

The startup run happens on an empty server, which is the one condition that cannot
show what a write costs when people are watching it: an immediate write re-sends the
chunk it touched to every player in range. `/perf world` on a populated server is the
figure that answers for one.

## What the files are

| File | Measures |
| --- | --- |
| `00-harness.lua` | The timing itself: scaling a reading, taking the loop off it, reporting |
| `10-calls.lua` | One call from a script into the mod, by how much crosses with it |
| `20-lua.lua` | The interpreter on its own, so the crossings have something to sit beside |
| `30-world.lua` | Reading and writing blocks, one at a time against staged and committed |
| `95-commands.lua` | `/perf` |

## Reading a figure honestly

Each reading repeats a pass until the total passes 100ms, then divides. The only
clock a script has counts whole milliseconds, so a single pass of anything fast
measures the clock rather than the work; repeating until the total is long enough
puts that rounding well under a percent.

What is left after the loop is subtracted is the operation, not the operation plus
the `for` around it. The world readings subtract their own driver rather than a bare
loop, since walking a box costs more per position than counting to one.
