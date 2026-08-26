# MoonTweaks

Lua scripting for Vintage Story recipes. Drop a `.lua` file into a server's
config folder and it rewrites the crafting registries at load, without compiling
a mod or patching JSON.

## Writing scripts

Scripts live in `<dataPath>/ModConfig/moontweaks/` and run in filename order, so
a numeric prefix controls precedence.

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
`copyAttributesFrom` — so a vanilla recipe under `assets/survival/recipes/grid/`
reads as a working reference. A name changes only where the shape changed.

Two shorthands: a bare string stands in for a table with only `code` set, and
`type` is inferred by looking the code up in the item and block registries.

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
recipe tree half-edited — including the scripts that ran before it.

Failures name the file, the line, the call, and the argument:

```
[moontweaks] 99-broken.lua:1: moontweaks.recipes.grid.add argument 'recipe'
             has no field 'ingredents'; did you mean 'ingredients'?
[moontweaks] no changes were applied
```

### Layers

```
Host       ModSystem, script discovery, the log module
Scripting  ScriptValue / IScriptHost / ScriptOrigin
Api        annotations, spec records, SpecBinder, DomainBinder
Recipes    GridDomain, GridRecipeFactory, AssetKindResolver, MutationLog
```

MoonSharp appears in exactly one class, `Scripting/MoonSharpHost`. Lua values are
reduced to a neutral `ScriptValue` tree at that boundary, so swapping interpreters
means reimplementing `IScriptHost` and nothing else.

`AssetKindResolver` is the sole owner of the item-versus-block question, so no
recipe domain decides it independently.

## The API reference

The reference is generated from the bindings, never written alongside them:

```sh
./scripts/docs.sh            # docs/api.json, docs/library/moontweaks.lua, docs/index.html
./scripts/docs.sh --check    # fail on any undocumented binding, write nothing
```

`ApiReflector` enumerates the surface through `DomainBinder.FunctionsOf` and
`SpecBinder.FieldsOf` — the same helpers the interpreter uses to decide what
exists — and takes descriptions from the compiler's XML documentation output. It
cannot document a function that is not bound, or omit one that is.

`--check` fails when any module, function, parameter, table, field, or enumerated
value lacks a description. It runs in CI ahead of the Pages deploy, so an
undocumented binding cannot reach `main`.

`docs/library/moontweaks.lua` holds LuaCATS annotations. Point
`lua-language-server` at it and script authors get completion, parameter hints
and type checking against the real API.

`docs/` is generated rather than committed; a checked-in copy would be a second
source of truth free to go stale.

## Building

Requires the .NET 10 SDK and a Vintage Story install.

```sh
./scripts/sync-moonsharp.sh   # fetch MoonSharp at the pinned commit and patch it
./scripts/package.sh          # build, emit bin/Release/moontweaks-<version>.zip
```

`VINTAGE_STORY` overrides the client install path, which defaults to
`~/.local/share/vintagestory`.

### Testing against a real game

```sh
./scripts/run-server.sh       # package, install into .testbed/Mods, run a dedicated server
./scripts/run-client.sh       # connect a client carrying only this mod
```

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

## MoonSharp

MoonSharp is vendored as a submodule pinned to a reviewed commit and compiled
into `moontweaks.dll`, so the mod ships as a single assembly. The published
NuGet package is a prerelease that predates fixes this mod depends on, which is
why the build works from source.

`scripts/sync-moonsharp.sh` performs a forced checkout of the pinned commit
before patching, making it idempotent. It applies everything in `patches/` and
then disables the nullable context per vendored file, which keeps MoonTweaks' own
code under a strict nullable context without inheriting several hundred warnings.
The submodule working tree is expected to read as modified afterwards; that is
the patch and the pragmas, not drift.

`patches/0001-qualify-ReferenceEqualityComparer.patch` resolves an ambiguity
between MoonSharp's own `ReferenceEqualityComparer` and the one .NET 5 added to
`System.Collections.Generic`. The conflict only appears when MoonSharp's
`netstandard2.0` sources are compiled into a modern target, and belongs upstream.

## Layout

```
src/            the mod
tools/docgen/   reference generator
scripts/        build, docs, and testbed entry points
patches/        patches applied to the vendored MoonSharp checkout
examples/       scripts demonstrating the API
third_party/    MoonSharp submodule
```
