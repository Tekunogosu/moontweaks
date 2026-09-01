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

Creature tags are selected by and not yet written. `moontweaks.entities.around` and
its siblings take the same `tags` condition items and blocks take, read against
`ICoreAPI.EntityTagRegistry`. What is missing is giving a creature type a tag of its
own, which wants the mutation path `items.set` has rather than a binding, and an
editor that offers the creature names inside that key — a tag condition is one shape
wherever it is written and names its suggestion set once, so it offers the item and
block names in both places. Both want a script asking before they are designed.

The collectible half is done. `moontweaks.tags.add` declares
names an item or a block may carry, and `addTags` closed the asymmetry there. Whatever
declares creature tags inherits the same one-phase window: both registries lock
together, immediately after the phase this mod runs scripts in.

## The events still unbound

Thirty-one are bound. What is left falls into groups that want quite different
things, listed here nearest to done first.

**Notifications carrying something new** are down to nothing worth binding.
`PlayerDimensionChanged` is declared on `IEventAPI` and nothing in the server ever
raises it: the only call site for its trigger is the declaration.
`BeginChunkColumnLoadChunkThread` says a column is about to be read, on the thread
that reads it — marshalled onto the main thread it arrives after
`chunkColumnLoaded` already did, so it would be strictly the worse of the two.
`PhysicsThreadStart` fires once, at startup, and says nothing a script cannot see
from `worldgenStartup`. `AssetsFinalizers` is obsolete and wants binding never.

**Events whose handler must answer.** `OnTestBlockAccess` is bound, as
`events.testBlockAccess`, and settled the rule for the rest of the group by turning
out not to need one. The game hands each mod the answer the one before it gave and
takes the last as the decision, so handlers already compose without anything having to
be decided about vetoes: what a script returns is simply the next answer in that
chain. `RaiseAnswered` in `ScriptEvents` is that path, and is what any further
answering event should reach for.

`PlayerChat` is bound too, as `events.playerChat`, and needed a second raise path
rather than the same one: it carries two things a handler may change — the message and
whether anybody sees it — where the access event carries one. `RaiseChat` is that
path. Both follow the game's own rule that the last answer stands, which for chat
means a handler answering `true` puts back a message an earlier one swallowed. That is
documented on the binding rather than designed away, because designing it away would
have meant this mod's rule rather than the game's.

What is left needs the same treatment, one at a time. `CanUseBlock` and
`CanPlaceOrBreakBlock` return a bool, `BreakBlock`, `HandInteract` and
`OnPlayerInteractEntity` take a `ref EnumHandling`, `BeforeActiveSlotChanged` returns
`EnumHandling`, and `ServerSuspend` returns `EnumSuspendState`. These are not one
decision but several: an `EnumHandling` is not a chained answer the way an access
response is, and what two handlers each returning `PreventDefault` should mean is a
question per event rather than one for the group.

`OnTestBlockAccessClaim` is the sibling of the bound one and wants the same shape;
nothing has asked for it yet.

Marshalling does not help this group and never will. These events need an answer on
the thread that asked, and anything deferred to the next tick answers after the
decision was made. `RaiseAnswered` therefore compares the calling thread against the
one the server ticks on and leaves the decision alone anywhere else, saying so once
per event — the game's own callers ask on the main thread, and another mod calling
`TryAccess` off it is what that guard is for. `OnTrySpawnEntity` and
`OnTrySpawnGroupNearOffthread` are raised off the main thread always rather than
sometimes, so they stay out of reach whatever else is bound.

**Events on a hot path** should stay unbound whatever else is. These are raised per
frame, per match attempt or per block written, which is a different order of frequency
from `testBlockAccess` — bound above, and raised once per block a player actually
breaks or uses. `OnGetClimate`,
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
region, reads the weather, plays sounds, throws off particles, asks whether somebody
may act somewhere and remembers things against the save game. `moontweaks.claims`
reads, adds and removes the land claims that question is asked against. What is left is smaller than it was, and
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
