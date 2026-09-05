# MoonTweaks

**[API reference](https://tekunogosu.github.io/moontweaks/)** — every function, table
and value a script can reach, generated from the bindings themselves.

MoonTweaks brings Lua scripting to Vintage Story! *Most* of the C# api has been implemented which gives you a lot of 
flexibility. Add new recipes, modify existing ones, create new commands, modify player data, set new spawn points.. you 
really can do pretty much anything you can think of. A good use is adding recipe compatibilities between mods, that's
what I use it for, and some QoL recipes like Firewood -> sticks and respawn at beds (both are in the examples).

A proper C# mod is still required if you want to add new assets as
this runs entirely on the server (can be used in singleplayer without issue). This also means that creating new UIs on
the client is not available yet. `CLIENTSIDE.md` holds a design for reaching them from a
client-side companion mod; it is a proposal rather than committed work.

Another mod can add bindings of its own, which scripts reach under `plugin.<name>` and
an editor completes like the rest. `PLUGINS.md` is the contract, and `plugins/xlib/` a
complete plugin exposing XLib's skills.

Now, Lua *IS* slower than writing a full C#. It's really designed for smaller customizations. While you could write a full
mod in Lua with MoonTweaks, it's not recommended. That being said, a lot of work went into making it run as performant
as possible. If you do experience significant performance issue while utilizing MoonTweaks, please create an issue and 
post your code, I'll do what I can to fix the issues.

I try to include as many examples into the project as possible so you can see the full suite of what is available. If 
you have a script you would like to contribute, please open a PR or an issue and I can get it in for you. 


Note: AI was used in the creation of this project. As such, there may be bugs that have been missed. I try to create 
extensive testing suites and manually check every functionality for correctness, but things will always slip through the 
cracks.

## Installing

Put `moontweaks-<version>.zip` in the `Mods` folder of the install that will run it: a
client's for singleplayer, a dedicated server's for multiplayer. Releases are on
[GitHub](https://github.com/Tekunogosu/moontweaks/releases), and `./scripts/package.sh`
builds the same zip from source.

Players joining a MoonTweaks server need nothing installed, because recipes reach them
through the game's own registry sync.

Start the game once after installing. That first start writes the folder the next
section sets an editor up against.

## Editor setup

Scripts are written against a type library the mod generates from its own bindings, so
an editor completes every function, checks its arguments and shows its documentation as
you type. Any editor driving `lua-language-server` reads the same configuration; the
steps below are Neovim and VS Code.

### 1. Run the game once with the mod installed

The mod builds its own folder the first time it loads, in singleplayer or on a server:

```
<dataPath>/ModConfig/moontweaks/
  scripts/                    your scripts
  library/moontweaks.lua      types for every binding this build carries
  library/codes.lua           every asset code this install holds
  examples/<topic>/           worked scripts, grouped by what they are about
  .luarc.json                 language server configuration
  .vscode/extensions.json     recommends the Lua extension to VS Code
  config.json                 settings, written with their defaults
```

`<dataPath>` is `%APPDATA%\VintagestoryData` on Windows, `~/.config/VintagestoryData`
on Linux and `~/Library/Application Support/VintagestoryData` on macOS. A dedicated
server uses whatever path it was started with.

Later starts keep the folder current on their own: the library is rewritten when the
bindings it describes have changed, and the examples when they differ from the build,
so updating the mod repeats none of this. `.luarc.json`, `.vscode/extensions.json` and
`config.json` are written once and are yours from then on.

### 2. Install the language server

**Neovim** needs `lua-language-server` on the system and the stock `lua_ls` setup.
Nothing MoonTweaks-specific goes in your configuration:

```lua
-- Neovim 0.11 or newer, with nvim-lspconfig installed
vim.lsp.enable("lua_ls")

-- Earlier versions
require("lspconfig").lua_ls.setup {}
```

`:MasonInstall lua-language-server` installs the server itself where a system package
manager does not carry it.

**VS Code** and VSCodium need the `sumneko.lua` extension, which is that same
identifier on both the Marketplace and Open VSX. `.vscode/extensions.json` offers it
the first time the folder is opened, so accepting that prompt is the whole step.

### 3. Open the MoonTweaks folder, not `scripts/`

Open `<dataPath>/ModConfig/moontweaks/` as the project folder. `.luarc.json` points the
language server at `library/` by a relative path, so the workspace root has to be the
folder holding both. VS Code roots at whatever folder was opened and so needs this one
exactly; Neovim resolves either, because `lua_ls` walks up for `.luarc.json` ahead of
`.git`.

That file also names the Lua version the interpreter reports and disables the standard
libraries it never opens, so the editor's idea of the language is the server's rather
than stock Lua's. [What a script may and may not do](#what-a-script-may-and-may-not-do)
lists them.

### 4. Write in `scripts/`, copy out of `examples/`

Copy an example into `scripts/` before changing it. Everything under `examples/` is
rewritten to match the build on the next start, so an edit made there does not survive
one, and nothing in that folder runs.

Open a script and the editor should complete `moontweaks.` into the modules the
reference lists. If it completes nothing, the workspace root is the usual cause: check
that `.luarc.json` sits beside the folder you opened rather than above it.

### Asset codes

`library/codes.lua` declares one alias per registry the game keeps and a script writes
as a bare string — every asset code, every tag those assets carry, and every character
trait — so an editor offers them inside the string rather than leaving you to guess:

```lua
output = "game:axe-|"          -- suggestions appear here
ingredient = { code = "game:|" }
tags = { "tool-|" }
requiresTrait = "cloth|"
```

It is generated from the running game rather than shipped, so it covers whatever mods
the install loads, and `/moontweaks export` rewrites it without a restart after a mod
is added. The aliases widen `string` rather than closing over the values they list, so
an editor suggests a code without rejecting one the file does not list — which matters,
because a code may reach the game after the file was written.

Expect the list to cost roughly 50 microseconds per code, all of it the editor's rather
than the server's: around 300ms for a vanilla install's 7,000-odd codes, and
proportionally more on a heavily modded one.

### What the editor cannot catch

Two mistakes reach the server. A misspelled field in a table literal is the first:
`lua-language-server` does not report unknown keys at any severity. The binder catches
it as the server loads, naming the file, the line and the nearest field it knows:

```
[moontweaks] 99-broken.lua:1: moontweaks.recipes.grid.add argument 'recipe'
             has no field 'ingredents'; did you mean 'ingredients'?
```

A code compared inside a handler is the second. A code written into a spec is refused
by name if the server does not have it, at the moment the script is read. A code
compared against later is only a Lua string, so a wrong one never matches and the
handler quietly does nothing:

```lua
if e.block == "game:crock-burned" then   -- no such block; this is never true
```

Nothing can catch that at load, because the comparison has not happened yet. What the
editor reaches instead is the table itself: every event names the shape it hands over,
so `e` completes its own keys and `e.block` is typed as an asset code like every other
code in the API, offering what the server actually holds. Log `e.block` and go and
break the thing if you want to know for certain what it is called.

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

A bare list is shorthand for `allOf`, and the longer spelling is the game's own —
`allOf`, `anyOf` and `noneOf`, written as a recipe file writes them:

```lua
tags = { anyOf = { "tool-axe", "tool-pickaxe" } }        -- either kind
tags = { allOf = { "tool" }, noneOf = { "weapon-melee" } } -- a tool, not a weapon
tags = { noneOf = { "weapon" } }                          -- anything but a weapon
```

A junction may hold groups rather than names, and then each group is a condition
of its own. The verbs alternate by layer, because a group is what the junction
combines:

```lua
-- Any one group is enough: a tool that is also a weapon, or a hammer that is not.
tags = { anyOf = { { allOf = { "tool", "weapon-melee" } },
                   { allOf = { "tool-hammer" }, noneOf = { "weapon" } } } }

-- Every group must hold, and each asks for any one of its own tags.
tags = { allOf = { { anyOf = { "tool-knife", "tool-cleaver" } },
                   { anyOf = { "weapon-melee", "weapon-ranged" } } } }
```

Writing the junction's own verb inside it is refused by name, as is naming both
verbs on one layer — the same two rules the game's loader enforces. The one place
this goes further: a condition naming only `noneOf` selects what does not carry
those tags, where the game's loader reads it as matching nothing at all.

The same `tags` key selects for `items.set`, `blocks.set` and every recipe
selector, so a condition worked out once is written the same way wherever it is
used.

A server may declare tags of its own and put them on whatever should carry them,
which is how a rule gets a name that means something here rather than being spelled
out at every call site:

```lua
moontweaks.tags.add "myserver:contraband"          -- one name
moontweaks.tags.add { "myserver:scrap", "myserver:relic" }  -- or several

moontweaks.items.set { code = "game:metalbit-*", addTags = "myserver:scrap" }
moontweaks.items.set { tags = { "myserver:scrap" }, maxStackSize = 128 }
```

`addTags` puts them on top of what an asset already carries, which is nearly always
what is wanted — the tags the game gave something are what its own recipes select it
by. `setTags` replaces them instead.

Declaring belongs in a script's body: the server closes its tag registry as soon as
the scripts have run, so a handler is too late and is told so. Players install
nothing for it, because the server sends its whole tag table to each client as they
connect.

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
`server/`, `events/`, `commands/` and `survival/` — checked by `lua-language-server`
on every documentation build. Three sit apart from that grouping, being about something other
than one corner of the API: `diagnostics/` calls every bound function and says which
ones answer on your server, `performance/` measures what they cost there, and
`rpglevels/` is one finished feature rather than a tour — a levelling system, where
players earn experience for what they kill and are handed something every fifth
level.

## How it works

**Scripts run at `ExecuteOrder` 1.1**, immediately after the vanilla recipe
loader at 1.0. By then the JSON recipes are loaded, wildcard-expanded and
resolved, so scripts see concrete recipes rather than templates. That is why
`remove("game:axe-flint")` matches one recipe and not the family declaration it
came from.

**Only the server runs scripts.** Recipes are server-authoritative, so what a script
changes reaches a client through the same registry sync the game already performs for
its own.

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
Host       ModSystem, commands, script discovery, plugins, editor scaffolding, the log module
Reference  the scriptable surface read back out of an assembly, and the library written from it
Scripting  ScriptValue / IScriptHost / ScriptOrigin, and the JSON they convert to
Api        annotations, the plugin contract, the spec shapes, SpecBinder, PayloadWriter, DomainBinder
Assets     reaching items and blocks: codes, tags, stacks, properties
Recipes    one domain and factory per recipe kind, over the shared owners below
Players    reaching a player and the behaviours their state lives on
Events     what the game raises while it runs, and the handlers listening
GameSystems  what another mod declared: weather, stability, reinforcement
```

Each layer names only the ones beneath it. Within a layer, a *system* reaches
something and owns a question about it, while a *domain* is the thin surface a
script calls — `PlayerAccess` finds a player and their behaviours, `PlayerDomain`
lists what a script may do with one. Utilities reach nothing at all and sit apart
from both: `ScriptJson` converts the value tree, `ValueSet` matches a declared set
to the game's own.

`GameSystems` is the one layer that reaches outside the game's own API, into the
systems the mods shipping with the game declare. Everything it touches is listed in
`MODSYSTEMS.md`, which is the list to walk after a game update.

`ScriptRun` owns running the scripts. A server's startup and `/moontweaks check`
both go through it, so what a check reports is what a start would do. It binds
MoonTweaks's own domains first and every plugin's after them, against the paths
already taken, which is what keeps `moontweaks.` the mod's own.

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

The reference is published at
**[tekunogosu.github.io/moontweaks](https://tekunogosu.github.io/moontweaks/)** and
generated from the bindings, never written alongside them:

```sh
./scripts/docs.sh              # the reference, the library, and a scaffolded examples/
./scripts/docs.sh --check      # fail on any undocumented binding, write nothing
./scripts/check-examples.sh    # fail on an example disagreeing with the types just written
```

It writes `docs/api.json`, `docs/library/`, `docs/index.html`, the highlighter that
page loads, and scaffolds `examples/`
with the same files the mod installs into a server: the editor configuration held in
`src/Host/Resources/`, embedded in the mod and written out verbatim, and the library
this build produced. The repository's own scripts are therefore checked exactly where
an author's are, by `check-examples.sh` running `lua-language-server` over that
scaffolded folder. Generating and checking are separate programs because they answer
separate questions: a diagnostic reported by a build step reads as the packaging
being broken rather than as a finding about the examples.

`ApiReflector` enumerates the surface through `DomainBinder.FunctionsOf` and
`SpecBinder.FieldsOf` — the same helpers the interpreter uses to decide what
exists — and takes descriptions from the compiler's XML documentation output. It
cannot document a function that is not bound, or omit one that is. It lives in the
mod rather than in the generator, because a server runs the same reflection at
startup to write `library/moontweaks.lua` and one library per plugin from the
assemblies it actually loaded; the generator reuses it for the site. The zip
therefore carries the XML documentation beside the DLL.

Every module carries a worked example as well as a description, written beside the
binding as an XML `example` element. One source reaches three places: the reference
page renders it under the module heading, the editor library carries it into the
hover text, and `docs.sh` gathers all of them into `examples/snippets/modules.lua`,
where `lua-language-server` checks them against the same types an author writes
against. A snippet that stops compiling fails the build rather than sitting wrong on
the site.

`--check` fails when any module, function, parameter, table, field, or enumerated
value lacks a description, when a module lacks an example, or when two bindings
declare the same type name. That last one matters because a Lua type is addressed by
name alone: two bindings sharing one are read as a single type holding the union of
both sets of fields, and a table satisfying either is then reported against the
other's requirements.

The checks pair: `--check` fails on a binding the reference cannot describe,
`check-examples.sh` on an example that disagrees with the types the binding produced.
Both run in CI ahead of the Pages deploy, the example check against the library
`docs.sh` has just written rather than a checked-in copy, so neither an undocumented
binding nor a drifted example reaches `main`.

The examples are coloured by highlight.js, vendored under `third_party/highlight.js`
at a pinned version and copied into `docs/` beside the page. The site loads nothing
from anybody else, so it works from a checkout with no network and cannot change
underneath a reader. `third_party/highlight.js/README.md` records the version, the
commit it came from, a digest per file, and how to move to a later one.

`docs/` is generated rather than committed; a checked-in copy would be a second source
of truth free to go stale. The `docs` workflow rebuilds it on every push to `main` and
publishes what it built, so the site describes the current source rather than whichever
build last remembered to write it.

## Performance

Every call a script makes crosses from the interpreter into C#, and that crossing is
the unit of cost — not the work on the far side, which for most bindings is a field
read. Scripts and handlers run on the server's main thread, so the limit that bites
first is not how much a script can do but how long it holds the tick while doing it.

### What one operation costs

Taken by `examples/scripts/performance/` on one machine, on an empty server. **These
are rough figures.** They move with the hardware, with what the server is doing at the
time, and with how many players are in range of the work. Take your own with `/perf`
rather than planning against these.

| Operation | Roughly |
| --- | --- |
| A line of Lua — arithmetic, a table write | 20–30 ns |
| A call to a Lua function | ~100 ns |
| A call into MoonTweaks | 110–210 ns |
| A call handing back a table, such as `server.info` | 1.3 µs |
| Reading one block with `blockAt` | 0.4–0.5 µs |
| Staging one block with `queueBlock` | 0.4 µs |
| Writing one block, staged and committed | 2 µs |
| Writing one block with `setBlock` | 2–2.5 µs |
| Scanning a region with `countBlocks`, per block | 20–30 ns |

A handler is handed its event as a table before its first line runs, which is the
same work the `server.info` row measures. Events that fire per block or per entity
are the ones where that matters.

### The budget is the tick

The server ticks about every 33ms and logs `Server overloaded` for any tick over
500ms. Half a second of main thread is roughly **two million calls** or **two hundred
thousand block writes** — and every one of them is time the server is answering
nobody. A handler that stays under a few milliseconds is invisible; one that runs for
a second is a freeze players feel, even though nothing has crashed.

Past that, split the work across ticks with `moontweaks.server.every` and budget each
slice by **time rather than by count**: a count has to be guessed against hardware the
script knows nothing about, where a deadline measures it as it goes.
`examples/scripts/world/house-builder.lua` does exactly that: `/build spread` fills
25ms of each tick and hands the rest back, however large the job it was given.

### Three things that matter more than the figures

**Fewer calls beat faster calls.** `countBlocks` scans a region at some 25ns a block;
the same region walked with `blockAt` costs around 450ns a block, near twenty times
more. Where a
binding takes a region, a stack or a list, one call for the lot is the whole
optimisation.

**Stage bulk writes.** `queueBlock` costs a fifth of `setBlock` and defers the chunk
work to one `commit`. On an empty server the finished cost per block is close either
way, because the lighting is the same work whichever route asked for it. The
difference is what a populated server pays: an immediate write re-sends the chunk it
touched to every player in range, once per call, where a commit does it once per
chunk. `/perf world` measures both where the players are.

**Measure before optimising.** .NET compiles a method properly only after watching it
run, so the first pass of anything reads several times slow. The suite takes every
figure twice and keeps the second, which is why its numbers settle and a hand-rolled
timing loop's often do not.

## Commands

```
/moontweaks list       the changes this server's scripts applied at startup
/moontweaks check      re-run every script and report what it would change
/moontweaks plugins    the plugins bound on this server and the paths scripts reach them at
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

`VINTAGE_STORY` names the folder holding `VintagestoryAPI.dll` — the game's install
folder rather than its data folder. Left unset, the first of the platform's usual
places that exists is taken: `%APPDATA%\Vintagestory` on Windows, the application
bundle or `~/Library/Application Support/vintagestory` on macOS, and
`~/.local/share/vintagestory` on Linux. A build that finds none of them says so in
one line and names the variable, rather than failing as a wall of unresolved
references.

### Platforms

The mod itself is portable and has nothing platform-specific in it: every path it
touches is built from the folder the game hands it, and the two places that need a
platform-neutral form convert deliberately.

The scripts in `scripts/` are POSIX shell, so they run on Linux and macOS as they
are. On Windows they need WSL or Git Bash. Three want a program beyond the shell:
`package.sh` wants `zip`, which Windows does not ship, `run-client.sh` wants `jq`,
and `check-examples.sh` wants `lua-language-server`. Building and testing without
any of them is `dotnet build` and copying the output; the scripts are convenience
rather than requirement.

Develop on Linux where there is a choice. Its filesystem is case-sensitive, so a
script folder or an asset code with the wrong casing fails there and passes quietly
on Windows and macOS — which means a mistake made on either of those first surfaces
on somebody's Linux server.

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
`/tmp/moontweaks-client`), `MOONTWEAKS_PORT` (default `42460`) and
`MOONTWEAKS_ADDRESS` (default `127.0.0.1:$MOONTWEAKS_PORT`).

The port is off the game's default of `42420` so the testbed never contends with a
real server on the same machine, and it is passed on the command line rather than
written into the testbed's config, so a regenerated testbed still lands somewhere
harmless.

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

The reported version is Lua 5.2, so `//` is not integer division, `goto` is not a
keyword, and there is no integer subtype: every number is a double.

The interpreter opens the basic library, `string`, `table`, `math` and `bit32`, and
nothing else. `coroutine`, `debug`, `io`, `os`, `package` and `utf8` are absent
rather than present and refusing, so a script has no clock of its own — which is
what `moontweaks.server.elapsedMs` exists to answer — and no way to reach a file.
`require` belongs to `package` and is absent with it, so a script cannot pull in
another; scripts share one environment instead, described below.

`dofile`, `loadfile`, `load` and `loadstring` come with the basic library and are
taken back out, each being a way to compile or load code the bindings never offered.
An editor still offers those four, since they are the standard library's rather than
this mod's to withdraw; a script that calls one fails on the line that does.

This is the whole of what a script may call. Anything not named here is absent.

| Library | Available |
| --- | --- |
| basic | `assert` `collectgarbage` `error` `getmetatable` `ipairs` `next` `pairs` `pcall` `print` `rawequal` `rawget` `rawlen` `rawset` `select` `setmetatable` `tonumber` `tostring` `type` `xpcall` `_G` `_VERSION` |
| `string` | `byte` `char` `dump` `find` `format` `gmatch` `gsub` `len` `lower` `match` `rep` `reverse` `sub` `upper` |
| `table` | `concat` `insert` `pack` `remove` `sort` `unpack` |
| `math` | `abs` `acos` `asin` `atan` `atan2` `ceil` `cos` `cosh` `deg` `exp` `floor` `fmod` `frexp` `huge` `ldexp` `log` `max` `min` `modf` `pi` `pow` `rad` `random` `randomseed` `sin` `sinh` `sqrt` `tan` `tanh` |
| `bit32` | `arshift` `band` `bnot` `bor` `btest` `bxor` `extract` `lrotate` `lshift` `replace` `rrotate` `rshift` |

Five details that the list alone does not give away:

**Strings carry their methods.** The string metatable is set, so `("x"):upper()`
works as well as `string.upper("x")`.

**`string.dump` is present and always fails.** The engine ships it as a stub that
raises rather than returning a binary chunk. Nothing could be done with one anyway,
since `load` is withdrawn.

**`print` does not reach the game log.** It writes a bare line to the server's
standard output, with no timestamp, no severity and nothing naming the script it
came from. `moontweaks.log.info` and `moontweaks.log.warn` are what write to the
log the server keeps, and they name the file and line that called them.

**Every script shares one global environment.** They run in sequence on one
interpreter, so a global one script assigns is readable by every script after it,
and a global one script overwrites is overwritten for all of them. `local` is what
keeps a name to the script that declared it.

**`math.randomseed` reseeds the whole server.** The generator is one shared
instance rather than one per script. It lives in a global the engine reads on every
`math.random` call, named `__lua_mathematics_library_random_instance`, which is
therefore visible to scripts and load-bearing: a script that assigns to that name
breaks `math.random` for every script and every handler until the server restarts.
Nothing outside the engine has any reason to touch it.

## License

MoonTweaks is MIT licensed; see `LICENSE`.

The mod ships `Lua.dll` beside its own assembly, which makes every release a
binary redistribution of Lua-CSharp's MIT licensed code. Its notice therefore
travels with the build: `scripts/package.sh` puts `LICENSE` and
`THIRD-PARTY-NOTICES.md` in the zip beside `moontweaks.dll`, and the notice
reproduces Lua-CSharp's licence in full. A release built any other way has to do
the same.

Vintage Story's assemblies are referenced at build time and never redistributed.
