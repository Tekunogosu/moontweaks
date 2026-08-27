# TODO

Work that is decided but not yet done. Each entry says what it is and why it is
worth doing, so it can be picked up without reconstructing the conversation.

Ordered by how far each is from done: the fields at the top need no decisions,
the interpreter at the bottom needs several.

## Recipe fields the game's own recipes use

Four fields exist on every recipe kind and are bound on none, listed with how
often vanilla's own recipe files use them:

| field | uses | what it does |
| --- | --- | --- |
| `requiresTrait` | 124 | gates a recipe behind a character trait |
| `averageDurability` | 47 | averages the durability of combined tools |
| `returnedStack` | 32 | what an ingredient leaves behind, such as an empty bucket |
| `enabled` | 28 | the game's own switch for turning a recipe off |

`showInCreatedBy`, `mergeAttributesFrom`, `durabilityChange` and `matchingType`
are bound on none either, and vanilla uses them zero times. Leave them out until
something asks: an offered field that does nothing is worse than an absent one.

## Asset code suggestions on command parameters

`AssetCode` is offered on every `code` field of a table, because a field carries
its suggestions in `LuaFieldAttribute`. Function parameters take their type from
the CLR signature alone, so `grid.remove("game:axe-flint")` gets no suggestions
even though it names exactly the same thing. Give parameters the same annotation.

## Export the traits, and generalise the registry accessor

`requiresTrait` names one of 26 traits held in `CharacterSystem.TraitsByCode` —
a mod system field, the same shape `RecipeRegistry` already reaches
`RecipeRegistrySystem` through. Exporting them as an `AssetTrait` alias costs
what tags cost, and turns a bare string an author has to already know into one an
editor offers.

The rule this is the third instance of: anything a script writes as a bare string
that the game keeps a registry of should be exported and suggested. Codes and
tags are done, traits are next, entity codes will want the same.

It also means `RecipeRegistry` is really "reach a mod system's registry" and
wants renaming once it holds a second one.

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
