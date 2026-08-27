# TODO

Work that is decided but not yet done. Each entry says what it is and why it is
worth doing, so it can be picked up without reconstructing the conversation.

## Per-command permissions

`config.json` carries one `commandPrivilege`, which gates every `/moontweaks`
command together and defaults to the privilege administrators hold. Replace it
with a privilege per command, so a server can let a builder export asset codes
without also letting them change recipes. Keep the single setting working as the
default for any command the file does not name.

## Asset code suggestions on command parameters

`AssetCode` is offered on every `code` field of a table, because a field carries
its suggestions in `LuaFieldAttribute`. Function parameters take their type from
the CLR signature alone, so `grid.remove("game:axe-flint")` gets no suggestions
even though it names exactly the same thing. Give parameters the same annotation.

## Prune examples that a build no longer ships

`examples/` mirrors the build, but a renamed example leaves its old copy behind on
a server that already had it, where it can go on referencing an API that no longer
exists. Deliberately not done: it deletes files, and renames are rare enough that
the cost of getting it wrong outweighs the tidiness.

## Tag conditions beyond "must carry all of these"

`tags` builds one condition requiring every tag listed. The game's own shape is
richer: `ComplexTagCondition` holds several conditions, each with required *and*
forbidden tags, combined conjunctively or disjunctively. Nothing in vanilla's
recipes uses more than the simple form, so the rest is unbuilt rather than
unsupported — add it when a script wants it.

## Unique identifiers for the other recipe kinds

`RecipeRegistry.Register` renumbers a knapping recipe after the game numbers it by
list length, which collides with a surviving recipe whenever one was removed
first. `RegisterSmithingRecipe` numbers the same way, and the cooking and barrel
registries have their own schemes. Give every kind the same treatment as it is
bound, rather than rediscovering the bug once per kind.
