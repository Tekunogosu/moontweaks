# Diagnostics

A suite that calls every function MoonTweaks binds and says in the server log whether
each one answered. It is not a demonstration of the API — the rest of `examples/` is
that. This is what you run to find out whether a build works on a given server.

Everything it changes it changes back. It spawns one hen and clears it up, places one
block above spawn and puts back what was there, and restores every setting it moves.
The two deliberate exceptions are named where they happen: a counter written into the
save game, which is how persistence is proved, and one stick dropped on the ground,
which despawns on its own.

## What it reports

Three numbers, written into the log at each stage and shown by `/diag`:

- **passed / failed / skipped** — one line per check, `[diag] pass` or `[diag] FAIL`.
- **_n_ of 199 bound functions exercised** — measured against `01-surface.lua`, which
  `scripts/docs.sh` generates from the same reference the editor library comes from,
  so a function bound later shows up as untouched rather than going unnoticed. The
  total moves whenever a binding is added; the figure the suite prints is the one to
  believe, not this sentence.
- **_n_ of 31 watched events have fired** — with the quiet ones named.

The whole answer to "did anything break" is `grep '\[diag\] FAIL'` over the log.

## Installing it

Copy the folder into the server's script folder and restart:

```sh
cp -r examples/scripts/diagnostics <data path>/ModConfig/moontweaks/scripts/
```

The `/diag` command it adds asks for the `controlserver` privilege, which
administrators hold by default.

Take it out again by deleting that folder and restarting. Nothing it did survives:
recipe and asset changes live in memory and are rebuilt from the game's own files at
every start.

## Step by step

Each step says what it covers and what a good result looks like. Steps 1 to 3 need
nobody; the rest need you logged in.

### 1. Start the server

Watch for `[diag] ---- load ----`. This is the smallest report of the three: the
world is not up yet, so only the registries, the server itself, the tag registry and
the mod list have been reached. Expect no failures; the exercised count is roughly a
third of the total.

A few seconds later the world comes up and `[diag] ---- world ----` follows, adding
the calendar, the chunks, the weather and the entity checks. Expect no failures, and
the exercised count to rise to roughly two thirds of the total. Between the two reports the hen is spawned, put through every
function in its module, and cleared away again.

Above the first report, the mod's own change log is worth a glance. Lines reading
`add knapping recipe for game:rot (1 recipe(s))` followed by
`remove 'knappingrecipes' recipes producing 'game:rot' (1 recipe(s))` are the direct
evidence that both halves of each recipe check landed — a count taken inside a run
cannot show it, because a run's changes are applied only once every script has
finished.

**Good result:** two report blocks and no `FAIL` lines.

**If the world report says "the server is holding no chunk near spawn":** the server
had not finished loading spawn when the checks ran. Log in and run `/diag world`,
which runs the same checks in the chunks around you.

### 2. Start it a second time

The one check that cannot pass on a first run is persistence. Restart and look for:

```
[diag] pass world.getData -- run 2, so what run 1 wrote survived a restart
```

**Good result:** the run number goes up by one each start.

### 3. Read the timers

Two seconds after the load report, `server.after` reports. A second later
`server.every` reports having fired three times and stopped itself by answering
false. Neither is in the load report, which is what makes them the check on the tally
itself — a `/diag` later showing them passed is a report reading state a handler
wrote after the scripts had finished.

**Good result:** both `[diag] pass server.after` and `[diag] pass server.every`.

### 4. Log in and run the player checks

```
/diag player
```

This runs everything needing a body: position, facing, health, satiety, tiredness,
game mode, spawn point, stored data, privileges, stats, the whole of
`moontweaks.inventory`, and the two world functions that need somebody standing
somewhere. Each one that changes something puts it back, so you end where you
started — you will see your health and satiety flicker and return.

It needs one empty slot in your backpack. If every slot is full, the slot checks say
so rather than displacing anything.

**Good result:** the count reaches the full total, no failures. Every function left
untouched by the first two reports is in this one group. You will be handed a stick
and shown two chat lines, one plain and one as a warning.

### 5. Check the container functions

Place a chest, leave it empty, look at it, and run:

```
/diag container
```

Emptying a set of slots is only ever run against a container that is already empty:
the call is exercised, the answer is checked against what was there, and nothing
anybody owns is thrown away. Pointing it at a full chest reports a skip rather than
clearing it.

**Good result:** `inventory.clear` moves from skipped to passed. The coverage figure
does not change: a skip already counts as covered, since the suite reached the
function and made a decision about it. What changes is that the call was made.

### 6. Raise the events

`/diag events` lists them all and what the first firing of each looked like. Nine
fill themselves in during startup. The rest need somebody to do something:

| Do this | Fills in |
| --- | --- |
| Log in | `playerJoin`, `playerNowPlaying`, `playerReady` |
| Say anything in chat | `playerChat` |
| Log in for the first time ever on this save | `playerCreate` |
| Scroll the hotbar | `playerChangeSlot` |
| Right-click a door or a chest | `didUseBlock` |
| Mine a block | `didBreakBlock`, `testBlockAccess` |
| Place a block | `didPlaceBlock` |
| Sit in a boat or on a saddled animal, then get off | `entityMounted`, `entityUnmounted` |
| Die, by `/kill` or otherwise | `playerDeath` |
| Respawn | `playerRespawn` |
| `/gamemode creative` | `playerSwitchGameMode` |
| Log out | `playerLeave`, `playerDisconnect` |
| Walk a few hundred blocks and back | more `chunkColumnLoaded`, `chunkUnloaded` |

Two cannot be raised on an existing world at all. `saveGameCreated` fires only on the
run that makes a save game, so it needs a fresh world. `serverResume` fires when a
server that suspended itself for want of players starts ticking again, so it needs
the server left empty long enough to suspend and then somebody joining.

**Good result:** everything but those two filled in. A quiet event is not a failure —
it means nothing raised it.

### 7. Read the whole picture

```
/diag
```

Says the tally, every failure, every function still untouched, and how many events
have fired. The same lines go into the log, so a run can be read afterwards without
copying anything down.

**Good result:** `166 of 166 bound functions exercised`, no failures, and only
`saveGameCreated` and `serverResume` still listed as quiet.

### 8. Clean up

```
/diag cleanup
```

Despawns anything the suite left standing and clears the outline `/diag player` drew.
Nothing should need it after a clean run — it is there for a run that failed partway
through the entity checks.

## The subcommands

| Command | What it does |
| --- | --- |
| `/diag` | The whole picture: tally, failures, untouched, events |
| `/diag report` | The same thing under a name, for scripting round it |
| `/diag player` | Every check needing a body, run on you |
| `/diag world` | The world checks again, in the chunks around you |
| `/diag events` | Which events have fired, and what the first firing looked like |
| `/diag container` | The slot functions, against the container you are looking at |
| `/diag cleanup` | Take back anything left in the world |

## What the files are

| File | Covers |
| --- | --- |
| `00-harness.lua` | The checking, the tally, the report, and finding a loaded chunk |
| `01-surface.lua` | Generated checklist of every bound function |
| `10-server.lua` | `log`, `server`, `mods`, and the save game's memory |
| `20-assets.lua` | `items`, `blocks` |
| `30-recipes.lua` | All seven recipe kinds, and the kind-agnostic module |
| `40-calendar.lua` | The world's clock and its seasons |
| `50-world.lua` | Reading and writing blocks, weather, sound, particles |
| `60-entities.lua` | One hen, through every function in the module |
| `70-events.lua` | All twenty-six events, watched |
| `80-timers.lua` | `server.every`, `server.after` |
| `90-players.lua` | A player's body, standing and memory |
| `91-inventory.lua` | Slots, in a bag and in the world |
| `95-commands.lua` | `/diag` and everything under it |
| `98-report.lua` | The load report |

## Regenerating the checklist

`01-surface.lua` is generated from `docs/api.json`, which `scripts/docs.sh` writes.
After binding a new function, run that and then:

```sh
python3 - <<'PY'
import json
d = json.load(open('docs/api.json'))
names = sorted(f"{m['Path'].replace('moontweaks.', '')}.{f['Name']}"
               for m in d['Modules'] for f in m['Functions'])
head = open('examples/scripts/diagnostics/01-surface.lua').read().split('diag.surface = {')[0]
open('examples/scripts/diagnostics/01-surface.lua', 'w').write(
    head + 'diag.surface = {\n' + "".join(f'  "{n}",\n' for n in names) + '}\n')
print(len(names), "names")
PY
```

The new name then shows up as untouched in the next report, which is the point.
