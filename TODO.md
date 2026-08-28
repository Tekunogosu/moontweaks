# TODO

Work that is decided but not yet done. Each entry says what it is and why it is
worth doing, so it can be picked up without reconstructing the conversation.

Ordered by how far each is from done: the ones at the top need one decision, the
ones at the bottom need several.

## Recipe fields still unbound

`showInCreatedBy`, `mergeAttributesFrom`, `durabilityChange` and `matchingType`
are bound on none of the kinds, and vanilla's own recipe files use them zero
times. Leave them out until something asks: an offered field that does nothing is
worse than an absent one.

`averageDurability` is bound on grid recipes alone for the same reason. It is
read only as a product lands in a crafting output slot, and a knapping surface
clones its resolved output stack directly rather than passing through there.

## The recipe kind still unbound

Every kind is bound but one. **Cooking** is not a `RecipeBase` at all:
`CookingRecipeIngredient` holds a minimum and maximum quantity, a portion size in
litres and a list of valid stacks, and the recipe itself carries `CooksInto`,
`IsFood`, `PerishableProps` and a `Shape`. Nothing above it can be reused, so it
wants its own spec tree.

## Codes a handler compares are unverifiable

Not a task: a note, so it is not investigated twice. `items.set` and every recipe
kind refuse a code the server does not have, naming it and the line. A handler
comparing `e.block` to a string gets no such help, and nothing at load time can
see it, since the comparison has not run.

The reachable half is done: each event names the shape it hands over, so the keys
complete and a code reads as an `AssetCode` rather than as a bare string. The sets
that type names widen `string` rather than closing it, deliberately — a server's
codes are its own, and a script naming one the editor has not seen is not wrong.
So the value is suggested and never checked, and that is as far as this goes.

## Per-command permissions

`config.json` carries one `commandPrivilege`, which gates every `/moontweaks`
command together and defaults to the privilege administrators hold. Replace it
with a privilege per command, so a server can let a builder export asset codes
without also letting them change recipes. Keep the single setting working as the
default for any command the file does not name.

Deliberately held until more of the API is bound: what the commands are worth
gating separately depends on what a script can do through them, and that is still
growing.

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

## Item and block properties still unbound

`moontweaks.items.set` and `moontweaks.blocks.set` carry what a script is likely
to want. Three things the client is told are still unbound, each of them a shape
with no obvious Lua spelling yet:

`transitionableProps` decides how something spoils, dries or ripens, and is a
list rather than one shape. `creativeInventoryStacks` and `creativeInventoryTabs`
decide where it appears in creative. `combustible.smeltingType` is bound but the
`crushing.quantity` spread only reaches its average and variance, not the
distribution shape the game also allows.

## Names and descriptions are out of reach

Not a task: a note, so it is not investigated twice. An item's displayed name
comes from `Lang.GetMatching("<domain>:item-<code>")` and its tooltip description
from `"<domain>:itemdesc-<code>"`. `Lang` is loaded on each side from that side's
own assets, and nothing textual is in `Packet_ServerAssets`, so the client renders
its own strings against a code that reaches it unchanged.

Changing them means shipping MoonTweaks to clients as well, which is a different
mod: every player would have to install it, where today they need nothing.

## Prune examples that a build no longer ships

`examples/` mirrors the build, but a renamed example leaves its old copy behind on
a server that already had it, where it can go on referencing an API that no longer
exists. Deliberately not done: it deletes files, and renames are rare enough that
the cost of getting it wrong outweighs the tidiness.

## Events the interpreter cannot yet be trusted with

Five of the game's events are bound, all of them raised on the main thread:
`didUseBlock`, `didBreakBlock`, `playerJoin`, `playerDeath` and `playerRespawn`.
`IServerEventAPI` offers 34.

The ones deliberately left out are those the game raises on another thread —
`BeginChunkColumnLoadChunkThread`, `OnTrySpawnGroupNearOffthread` and
`PhysicsThreadStart` among them. MoonSharp is not thread safe and nothing here
serialises calls into it, so binding one would be a race rather than a feature.
Offering them means one place that marshals a call onto the main thread, and that
is the piece to build before the count goes past the main-thread events.

The rest of `IServerEventAPI` is main-thread and wants nothing new: a subscribe
method on `ScriptEvents`, a function on `EventDomain` naming the shape it hands
over, and a payload class for that shape when no bound event already carries what
this one does.

## What a handler can do to the world

`moontweaks.players` reaches where a player is, their health, their hunger, their
mode, their spawn, their chat, and whatever a script chose to remember about them.
`moontweaks.world` reads and places blocks and drops item stacks. Nothing yet
reaches a player's inventory, or touches an entity that is not a player. Each is a
small domain of its own, and each wants the same treatment the recipe kinds had —
one owner for reaching the thing, and a spec for what a script writes.

Deliberately unbound: `Role`, `SetRole` and `Disconnect`. A script that can set
roles can grant itself anything, and the privilege on the `/moontweaks` command
gates who may run the command rather than what a script file may do. That wants
deciding alongside per-command permissions rather than before it.
