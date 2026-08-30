# MoonTweaks

Lua scripting for Vintage Story recipes. Drop a `.lua` file into a server's
config folder and it rewrites the crafting registries at load, without compiling
a mod or patching JSON. Starting the server once scaffolds that folder into a
workspace your editor already understands.

## Writing scripts

Scripts live in `<dataPath>/ModConfig/moontweaks/scripts/` and run in path order,
at any depth. A subfolder groups related scripts into a package that one numeric
prefix orders as a whole, while a prefix inside it orders that package's own
members:

```
scripts/
  10-core/
    10-axes.lua
    20-tools.lua
  20-economy/
    10-prices.lua
  99-overrides.lua
```

A script is named by its path from `scripts/`, so that is what failures report.

```lua
local grid = moontweaks.recipes.grid

moontweaks.log.info("starting with " .. grid.count() .. " grid recipes")

-- Drop the vanilla flint axe.
grid.remove("game:axe-flint")

-- Put it back, but demanding a bone handle instead of a stick.
grid.add {
  name = "moontweaks:axe-flint-bone",
  pattern = { "T",
              "B" },
  ingredients = { T = "game:axehead-flint", B = "game:bone" },
  output = "game:axe-flint",
}

-- One declaration covering three stone axes, via wildcard expansion.
grid.add {
  name = "moontweaks:axe-stone-direct",
  pattern = { "T" },
  ingredients = {
    T = { code = "game:axehead-*", name = "material",
          allowedVariants = { "granite", "andesite", "chert" } },
  },
  output = { code = "game:axe-{material}", quantity = 1 },
}
```

`pattern` is one string per row and one character per column, with `_` for an
empty cell. Width and height come from the rows, so `{ "T", "B" }` is one column
by two rows and `{ "TB" }` is two by one. Vintage Story's own recipe files spell
this as a single string whose row separator is sometimes a comma and sometimes a
tab, alongside `width` and `height` fields free to contradict it; a list of rows
has one spelling and cannot disagree with itself.

Everywhere else the field names are Vintage Story's own — `ingredients`,
`output`, `code`, `quantity`, `shapeless`, `allowedVariants`,
`copyAttributesFrom`, `requiresTrait`, `enabled`, `averageDurability`,
`returnedStack` — so a vanilla recipe under `assets/survival/recipes/grid/` reads
as a working reference. A name changes only where the shape changed.

Two shorthands: a bare string stands in for a table with only `code` set, and
`type` is inferred by looking the code up in the item and block registries.

Every recipe kind carries `name`, `enabled` and `requiresTrait`, because the
game reads all three for every kind. `requiresTrait` gates a recipe behind a
character trait, and a trait this server's assets do not define is refused by
name. Servers that turn the `classExclusiveRecipes` world configuration off drop
the trait from a scripted recipe exactly as the game drops it from its own.

`enabled = false` keeps a recipe in the file without registering it. The recipe
is still built, expanded and resolved, so a mistake in a disabled one is reported
on the run that declares it rather than on the day it is switched back on — the
one place MoonTweaks does more than the game's loader, which reads `enabled`
before it parses anything.

`averageDurability` is bound on grid recipes alone. It is read as the product
lands in the crafting output slot, and no other kind passes through there: a
knapping surface clones its resolved output stack directly.

An ingredient may name what an asset *is* rather than what it is called:

```lua
ingredients = {
  A = { tags = { "tool-axe" }, isTool = true, toolDurabilityCost = 1 },
  F = "game:firewood",
}
```

`code = "game:axe-*"` would match only assets a mod happened to code as `axe-`;
`tags = { "tool-axe" }` accepts any axe, however it is named. Every tag listed
must be present, and a tag no item or block carries is refused by name rather
than quietly matching nothing. Either `code` or `tags` is required; both together
narrow a wildcard further.

Knapping reads the same way, with one material rather than a grid of them:

```lua
local knap = moontweaks.recipes.knapping

knap.remove("game:knifeblade-flint")

knap.add {
  ingredient = { code = "game:stone-*", name = "rock",
                 allowedVariants = { "chert", "granite", "andesite" } },
  pattern = { "___####___",
              "___####___",
              "____##____" },
  output = { code = "game:knifeblade-{rock}", quantity = 2 },
}
```

`#` is stone left in place and `_` is stone chipped away. A knapping surface is
16 by 16 and a smaller pattern leaves the rest of it untouched. Rows outside that
square, ragged rows, a pattern that leaves no stone, and any character other than
those two are all refused with the row named.

`examples/scripts/` holds worked scripts grouped by what they are about —
`recipes/`, `assets/`, `players/`, `entities/`, `inventory/`, `world/`, `calendar/`,
`server/`, `events/` and `commands/` — checked by `lua-language-server` on every
documentation build.

## How it works

**Scripts run at `ExecuteOrder` 1.1**, immediately after the vanilla recipe
loader at 1.0. By then the JSON recipes are loaded, wildcard-expanded and
resolved, so scripts see concrete recipes rather than templates. That is why
`remove("game:axe-flint")` matches one recipe and not the family declaration it
came from.

**Only the server runs scripts.** Recipes are server-authoritative and sync to
clients through the normal registry sync, so players joining a MoonTweaks server
need nothing installed.

**Changes are recorded, not applied.** `grid.add` builds, expands and resolves
the recipe on the line that declares it, then appends the resolved recipes to a
mutation log. Nothing touches a registry until every script has run. A script
that fails on line 40 therefore contributes nothing, rather than leaving the
recipe tree half-edited — including the scripts that ran before it. The log keeps
what each change turned out to affect, which is what `/moontweaks list` reports
and what the startup lines are written from, so the two cannot disagree.

**Added recipes are renumbered.** The game identifies a new recipe by how many
the list already holds, which collides with a surviving recipe whenever a script
removed one first. Knapping, clay forming and smithing all number that way, and
each of their surfaces resolves the recipe a player picked by taking the first
identifier that matches — so a duplicate hands them another recipe's output, an
axe where they chose a knife blade, and the surface saves that identifier, so it
does so again after a restart. `RecipeRegistry` therefore assigns an identifier
past every one in use rather than trusting the count, and reports at startup if
two recipes ever end up sharing one. Cooking and barrel recipes are identified by
their code instead and are not numbered at all, and an alloy carries no identifier
of any kind: a crucible resolves one by the metals in it.

Failures name the file, the line, the call, and the argument:

```
[moontweaks] 99-broken.lua:1: moontweaks.recipes.grid.add argument 'recipe'
             has no field 'ingredents'; did you mean 'ingredients'?
[moontweaks] no changes were applied
```

### Layers

```
Host       ModSystem, commands, script discovery, editor scaffolding, the log module
Scripting  ScriptValue / IScriptHost / ScriptOrigin, and the JSON they convert to
Api        annotations, the spec shapes, SpecBinder, PayloadWriter, DomainBinder
Assets     reaching items and blocks: codes, tags, stacks, properties
Recipes    one domain and factory per recipe kind, over the shared owners below
Players    reaching a player and the behaviours their state lives on
Events     what the game raises while it runs, and the handlers listening
```

Each layer names only the ones beneath it. Within a layer, a *system* reaches
something and owns a question about it, while a *domain* is the thin surface a
script calls — `PlayerAccess` finds a player and their behaviours, `PlayerDomain`
lists what a script may do with one. Utilities reach nothing at all and sit apart
from both: `ScriptJson` converts the value tree, `ValueSet` matches a declared set
to the game's own.

`ScriptRun` owns running the scripts. A server's startup and `/moontweaks check`
both go through it, so what a check reports is what a start would do.

The interpreter appears in exactly one class, `Scripting/LuaCSharpHost`. Lua values
are reduced to a neutral `ScriptValue` tree at that boundary, so swapping
interpreters means reimplementing `IScriptHost` and nothing else — which is how the
engine under it was replaced without a binding changing.

Tables cross that boundary in both directions and one annotation describes both.
`SpecBinder` reads a table a script wrote into a spec; `PayloadWriter` writes an
object out as the table a handler is given. Both work from the same `[LuaField]`
metadata the reference is generated from, so an event's keys, its documentation
and what a handler actually receives are one description rather than three.

Four questions every recipe kind asks have one owner apiece, so no domain
answers them for itself. `AssetKindResolver` decides whether a code names an item
or a block. `RecipeAssets` turns the shapes scripts write into the records the
game resolves. `RecipeRegistry` reaches the lists the game keeps outside the
world: grid recipes hang off the world itself, but every other kind lives on a
mod system.

`TraitRegistry` answers which character traits exist, reading the `config/traits`
assets rather than the character system that also reads them: that system fills
its own registry at run phase `ModsAndConfigReady`, after scripts have run.

Specs share the same way. `Material` is a code that a wildcard may name a family
of; an `Ingredient` is a `Material` the recipe also consumes a quantity of, or
uses as a tool, and may hand a `ReturnedStack` back for. `Output` and
`ReturnedStack` are one shape under the two names the game's own recipe files
give it, so a recipe ported from JSON reads the same here as it did there.
Knapping takes a `Material` rather than an `Ingredient` because the stone decides
which recipes a surface offers rather than how much is spent — the game consumes
exactly one when the surface is placed, before a recipe is even chosen, so a
quantity there would be a field that could never mean anything.

## The API reference

The reference is generated from the bindings, never written alongside them:

```sh
./scripts/docs.sh            # the reference, the library, and a scaffolded examples/
./scripts/docs.sh --check    # fail on any undocumented binding, write nothing
```

It writes `docs/api.json`, `docs/library/`, `docs/index.html`, and scaffolds
`examples/` with the same files the mod installs into a server.

`ApiReflector` enumerates the surface through `DomainBinder.FunctionsOf` and
`SpecBinder.FieldsOf` — the same helpers the interpreter uses to decide what
exists — and takes descriptions from the compiler's XML documentation output. It
cannot document a function that is not bound, or omit one that is.

`--check` fails when any module, function, parameter, table, field, or enumerated
value lacks a description. It runs in CI ahead of the Pages deploy, so an
undocumented binding cannot reach `main`.

`docs/` is generated rather than committed; a checked-in copy would be a second
source of truth free to go stale.

## Editor setup

Starting a server once is the whole setup. The mod scaffolds its own folder:

```
<dataPath>/ModConfig/moontweaks/
  .luarc.json                 language server configuration
  .vscode/extensions.json     recommends the Lua extension to VS Code
  config.json                 settings, written with their defaults
  library/moontweaks.lua      LuaCATS annotations for this build's bindings
  library/codes.lua           every asset code this server's registries hold
  examples/<topic>/           worked scripts grouped by topic, to copy and edit
  scripts/                    your scripts
```

Three rules keep that folder current without a server writing to disk on every
start. `library/moontweaks.lua` carries a fingerprint of the bindings it was
generated from, in a comment on its fourth line; a server compares that one line
against the build it is running and rewrites the file only when the two disagree.
`LibraryHeader` owns that format for both the generator that writes it and the
server that reads it. The examples carry no such marker, so they are compared
outright and rewritten only when their contents differ, which is what keeps a
folder's examples in step as recipe kinds are added. `.luarc.json` and
`.vscode/extensions.json` are written only when absent, since from then on they
are the author's files.

### Asset codes

`library/codes.lua` declares one alias per registry the game keeps and a script
writes as a bare string — every asset code, every tag those assets carry, and
every character trait — so an editor offers them inside a string rather than
leaving an author to guess:

```lua
output = "game:axe-|"          -- suggestions appear here
ingredient = { code = "game:|" }
tags = { "tool-|" }
requiresTrait = "cloth|"
```

It is generated from the running game rather than shipped, so it covers whatever
mods a server loads, and it is rewritten only when what it lists changes.
`/moontweaks export` regenerates it without a restart, for after a mod is added.

`AssetCodeLibrary.SetsOf` is the sole owner of what the file contains: a registry
worth suggesting becomes one entry there and one `SuggestionSets` constant for a
`[LuaSuggests]` annotation to name, and needs nothing else.

The aliases widen `string` rather than closing over the values they list. An
editor therefore suggests them without rejecting anything absent from the list,
which matters because a code may reach the game after the file was written.

Expect the suggestion list to cost roughly 50 microseconds per code, all of which
is the editor's, not the server's: around 300ms for a vanilla install's 7,000-odd
codes, and proportionally more on a heavily modded one.

An editor reports a required field left out before a server ever reads the
script. It cannot report a *misspelled* one:
`lua-language-server` does not check for unknown keys in a table literal, at any
severity. The binder catches those when a server loads, naming the file, the line
and the nearest field it knows:

```
[moontweaks] 99-broken.lua:1: moontweaks.recipes.grid.add argument 'recipe'
             has no field 'ingredents'; did you mean 'ingredients'?
```

**Copy an example into `scripts/` before changing it.** Nothing under `examples/`
runs, and a server restores anything edited there, because that folder mirrors the
build rather than belonging to the author — a build that no longer ships an example
deletes the copy a server had, so a renamed one leaves nothing behind. Every example
ships from `examples/scripts/` in this repository, so each one is a script the
documentation build type-checks rather than prose that can rot.

`scripts/` is walked to any depth and run in path order, so the same grouping works
there: a folder gathers related scripts and one numeric prefix orders the folder as
a whole.

Open `<dataPath>/ModConfig/moontweaks/` as the project folder, not `scripts/`.
Neovim resolves either, because `lua_ls` roots a workspace by walking up for
`.luarc.json` ahead of `.git`; VS Code roots at the folder it was opened on and
so needs the one holding the configuration.

**Neovim** needs `lua-language-server` installed and the stock `lua_ls` setup;
nothing MoonTweaks-specific goes in your configuration. **VS Code** and VSCodium
need the `sumneko.lua` extension, which `.vscode/extensions.json` offers on first
open and which is the same identifier on both the Marketplace and Open VSX. Any
other editor driving `lua-language-server` reads the same `.luarc.json`, so
nothing here is specific to those two.

That configuration names the Lua version the interpreter reports, 5.2, and disables
the standard libraries it never opens: `coroutine`, `debug`, `io`, `os`, `package`
and `utf8`. `dofile`, `loadfile`, `load` and `loadstring` come with the basic
library and are taken back out, each being a way to reach code or files the bindings
never offered; an editor still offers those four although a script cannot call them.
`pcall`, `error` and `setmetatable` are available.

`src/Host/Resources/` holds those files, embedded in the mod and written out
verbatim. `./scripts/docs.sh` scaffolds `examples/` from the same resources and
the same library, so the repository's own scripts are checked exactly as a
server's are:

```sh
lua-language-server --check examples
```

It pairs with `./scripts/docs.sh --check`: the first fails on an example that
disagrees with the generated types, the second on a binding without a
description. Both run in CI, the example check against the library `docs.sh` has
just written rather than a checked-in copy, so an example cannot drift from the
bindings it demonstrates.

### Codes inside a handler are not checked

A code written into a spec is refused by name if the server does not have it, at
the moment the script is read. A code compared inside a handler is only a Lua
string, so a wrong one never matches and the handler quietly does nothing:

```lua
if e.block == "game:crock-burned" then   -- no such block; this is never true
```

Nothing can catch that at load, because the comparison has not happened yet. What
the editor reaches instead is the table itself: every event names the shape it
hands over, so `e` completes its own keys and `e.block` is typed as an asset code
like every other code in the API, offering what the server actually holds. The
string is still only suggested — the aliases widen `string` rather than closing
it — so a wrong one is accepted here as it is anywhere else. Log `e.block` and go
and break the thing if you want to know for certain what it is called.

### What a script costs

A call from a script into the mod costs roughly **480ns**, against about 3ns for
the same method called from C#. That figure settles at scale and only there: the
same measurement over twenty thousand calls reads 850ns, which is the millisecond
clock rounding rather than the calls.

The interpreter costs about as much again. Placing a block through `queueBlock`
takes around **2.6µs**, of which the crossing is a fifth; roughly **1µs** is the
Lua loop that decided which block to place, and the rest is marshalling and the
staging itself. That ratio is what makes a bulk call worth more than a faster
binder: the way to place blocks faster is to place more of them per call.

That fixes the scale of what a handler can sensibly do. A thousand calls cost
under a millisecond, which is nothing; a hundred thousand cost a tenth of a
second on the main thread, which players feel. Anything shaped like a loop over a
region should ask how many calls it makes before it asks anything else.

Block writes cost more than a bare call, and the difference is not where it
looks. Staging a block costs around **2.6µs** and committing it under **0.5µs**,
so a shape goes up at roughly **330,000 blocks a second**: nearly two million
blocks in about six seconds. Most of that is the calls rather than the engine —
staging is five times the commit. Staging costs the same per block whether the
batch is eighty thousand or two million, so there is nothing to gain by keeping
one small; the commit, by contrast, gets cheaper per block the more is queued
into it.

`setBlock` relights and re-sends the chunk it touched before the next call runs,
where `queueBlock` stages writes for a `commit` that pays that once per chunk.
The gap widens with the size of the job, because only the staged half improves
with scale: around three times slower over a thousand blocks, **four and a half
times** over fifty thousand. Those were measured with one player standing beside
the work, and the resend is per player in range, so a populated server multiplies
the slower half and not the other.

**The ceiling is not throughput, it is the pause.** Scripts run on the main
thread, so a handler holds the whole server for as long as it runs, and the game
logs `Server overloaded` for any tick over 500ms. Two million blocks in one
handler is a six-second freeze — nothing crashes, memory holds, players simply
stop being served. That budget is the real limit: about **170,000 blocks**, or
**800,000 calls**, before one command costs a visible stall.

Past that, split the work across ticks with `moontweaks.server.every` rather than
trying to make it faster. The same two million blocks spread over a tick budget
finishes in about seven seconds with the server answering throughout, against six
seconds of answering nothing. Budget the slice by **time rather than by count**:
a count has to be guessed against hardware the script knows nothing about, where
a deadline measures it as it goes. Twenty-five milliseconds of a thirty-three
millisecond tick fills each one without ever running long.

`examples/scripts/world/house-builder.lua` is where these numbers come from, and
`/build bench` and `/build calls` measure them again on whatever the server is
actually running on. Measure before optimising: the first run of anything in a
session reads about three times slow while the code is still being compiled, so
only a large run says anything true.

## Commands

```
/moontweaks list       the changes this server's scripts applied at startup
/moontweaks check      re-run every script and report what it would change
/moontweaks export     rewrite library/codes.lua from the live registries
```

`check` is a compile test, not a reload. It runs every script against a fresh
interpreter and reports the failure that stopped it, or the changes it would have
made, and then discards them. Nothing is applied, so a script can be checked
against a running server without that server and its players disagreeing about
what the recipes are.

It cannot be a reload. The server builds one assets packet at startup, caches it,
and hands that same packet to every client that connects, freeing the underlying
per-asset memory as it serialises. Changing a registry afterwards therefore
reaches nobody, and rebuilding the packet would serialise assets already freed.
Seeing a change still means restarting the server; `check` is what makes the
attempts before that cheap.

`config.json` decides who may run them:

```json
{
  "commandPrivilege": "controlserver"
}
```

`controlserver` is the privilege administrators hold, so a fresh server keeps
these commands with its administrators until it says otherwise. Any privilege
name the game knows may be used instead. One setting currently gates every
command together; see `TODO.md` for the per-command version.

Keys are matched without regard to case, so the casing above and the casing the
file is written back in both bind. A settings file that cannot be parsed is
reported and the defaults are used, so bad JSON costs a server its settings
rather than its startup.

## Building

Requires the .NET 10 SDK and a Vintage Story install.

```sh
./scripts/package.sh          # build, emit bin/Release/moontweaks-<version>.zip
```

`package.sh` builds twice. The first pass produces the assembly the reference
generator reflects over; the second embeds the library it wrote. The library is
derived from the bindings and not from itself, so the second pass is a fixed
point. A plain `dotnet build` embeds whichever library the last `docs.sh` wrote,
or none at all, in which case the mod still runs scripts and says at startup that
it cannot describe itself to an editor.

`VINTAGE_STORY` overrides the client install path, which defaults to
`~/.local/share/vintagestory`.

### Testing against a real game

```sh
./scripts/install.sh [directory...]   # package and install into the given folders
./scripts/run-server.sh               # install into .testbed and run a dedicated server
./scripts/run-client.sh               # connect a client carrying only this mod
```

`install.sh` replaces whatever version of the mod each directory already holds,
so an old zip cannot linger beside a new one. It packages once however many
destinations it is given, and copies into each one as given rather than treating
it as a data path to append `Mods` to — so an install names its own `Mods` folder
and any other directory works the same way. `run-server.sh` calls it rather than
installing for itself.

The version in `modinfo.json` names the zip and appears in the server's mod list,
which is how a tester tells one build from another. Bump it with every change: the
patch number for a fix, the minor number when something new is added.

`run-client.sh` runs the client on a throwaway data path with `modPaths` reduced
to the base game, so the tester's own mods stay out of the result. It copies the
account session from the real `clientsettings.json` into a directory it creates
mode 700, since that file carries the session key and signature.

Overrides: `VS_SERVER` (dedicated server install, default
`/mnt/media/vintagestory-server`), `MOONTWEAKS_TESTBED` (server data path,
default `.testbed`), `MOONTWEAKS_CLIENT` (client data path, default
`/tmp/moontweaks-client`), `MOONTWEAKS_ADDRESS` (default `127.0.0.1:42420`).

`ClientSyncProbe` logs the grid recipe count the client received, so a test can
compare it against what the server reported.

## The script engine

Scripts run on [Lua-CSharp](https://github.com/nuskey8/Lua-CSharp), shipped beside
the mod as `Lua.dll`. It reaches the rest of the mod only through `IScriptHost`,
which is the whole of what an engine implements, so replacing it means writing that
one interface again and touching no binding.

That is not hypothetical: the mod ran on another interpreter until Lua-CSharp was
measured against it and won on every shape that mattered — Lua itself several times
faster, a crossing into a binding about twice as fast, a fifth of the allocation,
and failures that name the operation as well as the line. Placing blocks, which is
the heaviest thing a script does here, more than doubled.

`./scripts/bench.sh` measures the interpreter and records what it makes of the Lua
its checks put through it. It needs no running server: the workload reaches an
engine only through `IScriptHost`, and the game is not part of that.

It reports two tables. `Cost` is the interpreter, one column per engine, and its
bindings are built by hand so that no engine is charged for the layer above it.
`Binder` is that layer — reading the table a script wrote into the shape a binding
takes, and writing the table a handler is given — measured once, because every
engine reaches it through the same neutral values. A server crosses it on every call
a script makes and again for every event it raises, so a regression there costs more
than one in the engine.

```sh
./scripts/bench.sh                    # check, then measure
./scripts/bench.sh --quick            # the same, at a twentieth of the counts
./scripts/bench.sh --json             # for something other than a person to read
./scripts/bench.sh --engine luacsharp # name one engine
```

With one engine those recordings describe it. Register a candidate beside it and
they become the test: the run exits non-zero when the two read the same Lua
differently, which is a reason not to swap whatever the timings say.

Adding a candidate is the same three steps every time: implement `IScriptHost`,
register it in `Scripting/ScriptEngine`, and run `bench.sh`. Nothing else in the mod
learns that a second engine exists.

### What a script may and may not do

The interpreter opens the basic library, `string`, `table`, `math` and `bit32`, and
nothing else. `coroutine`, `debug`, `io`, `os`, `package` and `utf8` are absent
rather than present and refusing, so a script has no clock of its own — which is
what `moontweaks.server.elapsedMs` exists to answer — and no way to reach a file.
`dofile`, `loadfile`, `load` and `loadstring` come with the basic library and are
taken back out, each being a way to compile or load code the bindings never offered.

`pcall`, `error` and `setmetatable` are available. The reported version is Lua 5.2,
so `//` is not integer division and `goto` is not a keyword.

## Layout

```
src/            the mod
tools/docgen/   reference generator
tools/luabench/ script engine measurement, run by scripts/bench.sh
scripts/        build, docs, install and testbed entry points
examples/       worked scripts grouped by topic, shipped with the mod
TODO.md         work that is decided but not yet done
LICENSE         MIT, covering this project
THIRD-PARTY-NOTICES.md   what the shipped assembly carries besides this project
```

`examples/` doubles as a MoonTweaks folder: `docs.sh` scaffolds it with the same
library and editor files a server gets, so the examples are checked exactly where
an author's scripts would be, and ship from there into every install.

## Licence

MoonTweaks is MIT licensed; see `LICENSE`.

The mod ships `Lua.dll` beside its own assembly, which makes every release a
binary redistribution of Lua-CSharp's MIT licensed code. Its notice therefore
travels with the build: `scripts/package.sh` puts `LICENSE` and
`THIRD-PARTY-NOTICES.md` in the zip beside `moontweaks.dll`, and the notice
reproduces Lua-CSharp's licence in full. A release built any other way has to do
the same.

Vintage Story's assemblies are referenced at build time and never redistributed.
