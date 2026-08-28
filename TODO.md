# TODO

Work that is decided but not yet done. Each entry says what it is and why it is
worth doing, so it can be picked up without reconstructing the conversation.

Ordered by how far each is from done: the permissions at the top need one
decision, the interpreter at the bottom needs several.

## Recipe fields still unbound

`showInCreatedBy`, `mergeAttributesFrom`, `durabilityChange` and `matchingType`
are bound on none of the kinds, and vanilla's own recipe files use them zero
times. Leave them out until something asks: an offered field that does nothing is
worse than an absent one.

`averageDurability` is bound on grid recipes alone for the same reason. It is
read only as a product lands in a crafting output slot, and a knapping surface
clones its resolved output stack directly rather than passing through there.

## The recipe kinds still unbound

Grid, knapping, clay forming, smithing and barrel are bound. Two kinds are left,
and neither resembles what exists:

**Cooking** is not a `RecipeBase` at all. `CookingRecipeIngredient` holds a
minimum and maximum quantity, a portion size in litres and a list of valid
stacks, and the recipe itself carries `CooksInto`, `IsFood`, `PerishableProps`
and a `Shape`. Nothing above it can be reused, so it wants its own spec tree.

**Alloy** is a plain `IByteSerializable` holding `MetalAlloyIngredient[]` and an
output — metal ratios rather than a shape. Small, and unlike everything else.

## Per-command permissions

`config.json` carries one `commandPrivilege`, which gates every `/moontweaks`
command together and defaults to the privilege administrators hold. Replace it
with a privilege per command, so a server can let a builder export asset codes
without also letting them change recipes. Keep the single setting working as the
default for any command the file does not name.

## Tag conditions beyond "must carry all of these"

`tags` builds the one shape the game's own converter builds for a bare tag array:
a single condition holding every tag, disjunctive, which asks that an asset carry
all of them. The game's full shape is richer — several conditions, each with
required *and* forbidden tags, combined conjunctively or disjunctively — and its
JSON spells that as `allOf`, `anyOf` and `noneOf`.

The catch worth knowing before building it: `RequiredTags` means two different
things depending on the flag beside it. Disjunctive asks that the asset contain
all of them, conjunctive only that the two sets overlap. A `noneOf` alongside
either is the straightforward part; the junction is where a mistake is silent.

Nothing in vanilla's recipes uses more than the simple form, so the rest is
unbuilt rather than unsupported — add it when a script wants it.

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
