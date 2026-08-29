# TODO

Work that is decided but not yet done. Each entry says what it is and why it is
worth doing, so it can be picked up without reconstructing the conversation.

`COVERAGE.md` is the survey this is decided from: what the game offers a server-side
mod and how much of it a script reaches. A gap listed there is not yet work; a gap
listed here is.

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

`transitionableProps` decides how something spoils, dries or ripens. The shape
itself is built and bound — cooking recipes needed it, and `TransitionableProperties`
is what they write — so what is left here is the list: an item carries several of
them where a meal carries one. `creativeInventoryStacks` and
`creativeInventoryTabs` decide where it appears in creative.
`combustible.smeltingType` is bound but the `crushing.quantity` spread only reaches
its average and variance, not the distribution shape the game also allows.

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

## The events still unbound

Sixteen are bound. `IServerEventAPI` declares 34 of its own and inherits 16 more
from `IEventAPI`, so 34 are unbound. They fall into groups that want quite
different things, listed here nearest to done first.

**Notifications carrying something new** want a payload shape apiece:
`DidPlaceBlock` (the block replaced and the stack placed, over `BlockEvent`),
`AfterActiveSlotChanged`, `MountGaitReceived`, `ChunkColumnLoaded`,
`ChunkColumnUnloaded`, and the entity events `IEventAPI` adds — `OnEntitySpawn`,
`OnEntityLoaded`, `OnEntityDeath`, `OnEntityDespawn`, `EntityMounted`,
`EntityUnmounted`. The entity ones want the entity domain deciding first how a
script names an entity that is not a player.

**Events whose handler must answer** are the ones needing a decision rather than
work. `CanUseBlock` and `CanPlaceOrBreakBlock` return a bool, `BreakBlock`,
`HandInteract` and `OnPlayerInteractEntity` take a `ref EnumHandling`, `PlayerChat`
takes `ref string message` and a `BoolRef consumed`, `BeforeActiveSlotChanged`
returns `EnumHandling`, and `ServerSuspend` returns `EnumSuspendState`.
`ScriptValue.Func.Call` already hands back what a handler returned and `Raise`
throws it away, so the machinery is half there. What is missing is a rule: several
handlers may answer one event, and what a veto beside an approval means has to be
decided before any of these is offered.

**Events on a hot path** should stay unbound whatever else is. `OnGetClimate`,
`OnGetWindSpeed`, `MatchesGridRecipe` and `MatchesRecipe` are raised per frame or
per match attempt, and a script call costs roughly 600ns against 3ns for the same
method in C#. Binding one puts the interpreter inside the game's inner loop.

**Events raised off the main thread** cannot be bound at all as things stand:
`BeginChunkColumnLoadChunkThread`, `OnTrySpawnGroupNearOffthread`,
`PhysicsThreadStart`, and `OnTrySpawnEntity`, which `GenCreatures` raises from
chunk column generation. MoonSharp is not thread safe and nothing here serialises
calls into it, so binding one would be a race rather than a feature. Offering them
means one place that marshals a call onto the main thread. `ChunkColumnLoaded` and
`ChunkColumnUnloaded` were checked and are main-thread; the entity events have not
been, and want checking before they are bound rather than after.

`AssetsFinalizers` is obsolete and wants binding never.

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
