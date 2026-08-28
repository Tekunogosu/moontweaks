# TODO

Work that is decided but not yet done. Each entry says what it is and why it is
worth doing, so it can be picked up without reconstructing the conversation.

Ordered by how far each is from done: the converter at the top needs no
decisions, the interpreter at the bottom needs several.

## Recipe fields still unbound

`showInCreatedBy`, `mergeAttributesFrom`, `durabilityChange` and `matchingType`
are bound on none of the kinds, and vanilla's own recipe files use them zero
times. Leave them out until something asks: an offered field that does nothing is
worse than an absent one.

`averageDurability` is bound on grid recipes alone for the same reason. It is
read only as a product lands in a crafting output slot, and a knapping surface
clones its resolved output stack directly rather than passing through there.

## A bridge from Lua tables to JsonObject

`attributes` appears on the ingredient, on the output and on the recipe of every
kind, and vanilla's grid recipes use it 85 times. All three need one thing: a Lua
table turned into a `JsonObject`, which wraps a Newtonsoft `JToken`. The
`ScriptValue` tree already maps onto JSON exactly, so this is one converter in
`RecipeAssets` that unlocks every one of those uses and every kind still to come.

## Unique identifiers for the other recipe kinds

`RecipeRegistry.Register` renumbers a knapping recipe after the game numbers it by
list length, which collides with a surviving recipe whenever one was removed
first. `RegisterSmithingRecipe` numbers the same way, and the cooking and barrel
registries have their own schemes. Give every kind the same treatment as it is
bound, rather than rediscovering the bug once per kind.

## Check the examples in CI

`docs.sh --check` runs in CI and fails on an undocumented binding.
`lua-language-server --check examples` is the other half of that guarantee — it
fails on an example that disagrees with the generated types — and runs only by
hand. Adding it means fetching the language server in the workflow, which is why
it has not happened yet rather than because it is not worth it.

## Per-command permissions

`config.json` carries one `commandPrivilege`, which gates every `/moontweaks`
command together and defaults to the privilege administrators hold. Replace it
with a privilege per command, so a server can let a builder export asset codes
without also letting them change recipes. Keep the single setting working as the
default for any command the file does not name.

## Tag conditions beyond "must carry all of these"

`tags` builds one condition requiring every tag listed. The game's own shape is
richer: `ComplexTagCondition` holds several conditions, each with required *and*
forbidden tags, combined conjunctively or disjunctively. Nothing in vanilla's
recipes uses more than the simple form, so the rest is unbuilt rather than
unsupported — add it when a script wants it.

## Prune examples that a build no longer ships

`examples/` mirrors the build, but a renamed example leaves its old copy behind on
a server that already had it, where it can go on referencing an API that no longer
exists. Deliberately not done: it deletes files, and renames are rare enough that
the cost of getting it wrong outweighs the tidiness.

## An interpreter that outlives the run

Scripts run once and the interpreter is disposed with them, and `ScriptValue` has
no case for a function. So there is currently no way to hold a Lua callback at
all, which is what every event feature needs — `IServerEventAPI` offers 34 of
them, including `PlayerDeath`, `PlayerRespawn`, `PlayerJoin`, `BreakBlock` and
`DidPlaceBlock`.

Four things have to be true before any of that is reachable:

- `ScriptValue` gains a function case, a callable handle across the host boundary.
- The host is owned by the mod system rather than by a `using` inside one run.
- A callback that throws logs and unsubscribes rather than taking the server down.
- Those events do not all fire on the main thread, and MoonSharp is not thread
  safe, so calls into the interpreter need one place that serialises them.

This is an architectural step rather than a field, and it is worth taking on its
own terms before a feature is designed on top of it.
