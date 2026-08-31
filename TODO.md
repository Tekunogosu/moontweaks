# TODO

Work that is decided but not yet done. Each entry says what it is and why it is
worth doing, so it can be picked up without reconstructing the conversation.

`COVERAGE.md` is the survey this is decided from: what the game offers a server-side
mod and how much of it a script reaches. A gap listed there is not yet work; a gap
listed here is.

Ordered by how far each is from done: the ones at the top need one decision, the
ones at the bottom need several.

## An ingredient's matching type is the game's to decide

Not a task: a note, so it is not investigated twice. `matchingType` is the one
field on an ingredient no script can set. `CraftingRecipeIngredient.Resolve`
opens by assigning it from `IRecipeIngredient.GetMatchType`, which reads the code
for a wildcard, an advanced wildcard or a regex, and reads whether a name sits
beside it. Every recipe this mod builds is resolved, so a value written there is
gone before the first match is attempted. The code string already says what a
script would be reaching for.

`averageDurability` and `showInCreatedBy` are bound on grid recipes alone, each
because only that kind reads them. The first is read as a product lands in a
crafting output slot, through `OnCreatedByCrafting`, and a knapping surface
clones its resolved output stack directly rather than passing through there. The
second is read by the handbook, which consults it building the "Created by" list
for grid recipes and skips it for every other kind.

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

## Names and descriptions are out of reach

Not a task: a note, so it is not investigated twice. An item's displayed name
comes from `Lang.GetMatching("<domain>:item-<code>")` and its tooltip description
from `"<domain>:itemdesc-<code>"`. `Lang` is loaded on each side from that side's
own assets, and nothing textual is in `Packet_ServerAssets`, so the client renders
its own strings against a code that reaches it unchanged.

Changing them means shipping MoonTweaks to clients as well, which is a different
mod: every player would have to install it, where today they need nothing.

## Asset properties still unbound

`moontweaks.items.set` and `moontweaks.blocks.set` carry what a server retunes: the
fields anything carried has, the fields only something standing in the world has,
how it spoils, where it appears in creative, what it sounds like, what shape it is to
walk into and how it grows as a crop.

What is left is the shapes nothing has asked for. `particleProperties` and
`particleCollisionBoxes` decide what a block throws off and what those particles
bounce against. `allowSpawnCreatureGroups` decides what may spawn on it,
`liquidSelectable` whether a cursor picks it out through water, and
`heldPriorityInteract` and `heldSounds` how it behaves in a hand. Decor — a block laid
onto the face of another, as a rug or a moulding — is a placement path rather than a
property.

Tags are the one asymmetry worth naming: `tags` selects what to change and cannot be
changed. `CollectibleObject.Tags` is a `TagSet` the registry hands out rather than a
list an asset owns, so writing one means registering the names and rebuilding the
set, and nothing has asked yet.

## The events still unbound

Twenty-nine are bound. What is left falls into groups that want quite different
things, listed here nearest to done first.

**Notifications carrying something new** are down to nothing worth binding.
`PlayerDimensionChanged` is declared on `IEventAPI` and nothing in the server ever
raises it: the only call site for its trigger is the declaration.
`BeginChunkColumnLoadChunkThread` says a column is about to be read, on the thread
that reads it — marshalled onto the main thread it arrives after
`chunkColumnLoaded` already did, so it would be strictly the worse of the two.
`PhysicsThreadStart` fires once, at startup, and says nothing a script cannot see
from `worldgenStartup`. `AssetsFinalizers` is obsolete and wants binding never.

**Events whose handler must answer** are the ones needing a decision rather than
work. `CanUseBlock` and `CanPlaceOrBreakBlock` return a bool, `BreakBlock`,
`HandInteract` and `OnPlayerInteractEntity` take a `ref EnumHandling`, `PlayerChat`
takes `ref string message` and a `BoolRef consumed`, `BeforeActiveSlotChanged`
returns `EnumHandling`, `ServerSuspend` returns `EnumSuspendState`, and
`OnTestBlockAccess` and `OnTestBlockAccessClaim` decide whether somebody may touch a
place at all. `ScriptValue.Func.Call` already hands back what a handler returned and
`Raise` throws it away, so the machinery is half there. What is missing is a rule:
several handlers may answer one event, and what a veto beside an approval means has
to be decided before any of these is offered. The two access ones are the sharpest
case, since answering wrongly hands somebody else's build to a stranger.

Marshalling does not help this group and never will. These events need an answer on
the thread that asked, and anything deferred to the next tick answers after the
decision was made — so the four raised off the main thread
(`OnTrySpawnEntity`, `OnTrySpawnGroupNearOffthread`, and the two access ones where
they are reached off-thread) stay out of reach whatever is decided about vetoes.

**Events on a hot path** should stay unbound whatever else is. `OnGetClimate`,
`OnGetWindSpeed`, `MatchesGridRecipe`, `MatchesRecipe` and `ChunkDirty` are raised
per frame, per match attempt or per block written, and a script call costs roughly
130ns against 3ns for the same method in C#. Binding one puts the interpreter inside
the game's inner loop. The pull-based readings are bound instead: `world.climateAt`
and `world.windAt` answer the same questions when a script asks rather than when the
game does, and `chunkColumnLoaded` says a chunk arrived without saying every time
one is touched.

`MountGaitReceived` was in this group and left it: the game raises it per packet
from every rider, and `mountGaitChanged` reports only a change, so the stream is
thinned where it lands rather than in every handler.

## What a handler can do to the world

`moontweaks.players` reaches a player's body, their standing and their memory.
`moontweaks.entities` reaches everything else alive. `moontweaks.inventory` reaches
any set of slots. `moontweaks.world` reads and writes blocks, searches a
region, reads the weather, plays sounds, throws off particles, asks about land claims
and remembers things against the save game. What is left is smaller than it was, and
each piece wants the same treatment the recipe kinds had — one owner for reaching the
thing, and a spec for what a script writes.

**Block entities past their inventory.** `moontweaks.inventory` reaches what a chest
holds. What a firepit is burning and what a quern is grinding sit on the block entity
rather than in its slots, and reaching them means naming a shape per kind of block —
which is a domain rather than a binding.

## Reaching another mod

`moontweaks.mods` says what is loaded and what version it is, which is what a script
needs to guard a block of codes that only exist on some servers. Another mod's recipe
registries are reached through `moontweaks.recipes.kinds` and its siblings.

Three of the systems the game's own content mods declare are bound —
`moontweaks.weather`, `moontweaks.stability` and `moontweaks.reinforce` — through
`IModLoader.GetModSystem`, with the types referenced rather than reflected over, so a
rename fails the build here instead of failing a script on somebody's server.
`MODSYSTEMS.md` lists every member each of them calls and is the list to walk after a
game update; `examples/scripts/diagnostics/55-survival.lua` asks the same question of
a running server.

What is left is any *other* mod's system, which is a different problem. Such a type
cannot be referenced from here, so it would have to be reflected over — a type name,
a member name and a shape, all written by the script and none of them checkable
before it runs. That wants a script actually asking for a named mod before it is
designed, since what the binding should look like depends entirely on what is being
reached for.
