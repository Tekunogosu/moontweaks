# TODO

Work that is decided but not yet done. Each entry says what it is and why it is
worth doing, so it can be picked up without reconstructing the conversation.

`COVERAGE.md` is the survey this is decided from: what the game offers a server-side
mod and how much of it a script reaches. A gap listed there is not yet work; a gap
listed here is.

Ordered by how far each is from done: the ones at the top need one decision, the
ones at the bottom need several.

## Quieten or drop ClientSyncProbe before release

`ClientSyncProbe` loads on every client and writes one line into that player's log at
`LevelFinalize`, naming how many grid and knapping recipes the client received. It is
instrumentation for developing this mod, and a player who installs the mod did not
ask for it.

There is a reason to keep the capability rather than delete it: recipe sync is the
one thing a server cannot check on its own. What the server registered and what a
client actually received are different questions, and this is what answers the
second.

So the decision is between deleting the system outright and keeping it silent by
default. The cheap version of silent is `api.Logger.VerboseDebug` in place of
`Notification`, which leaves the check available to anyone running a client with
verbose logging and writes nothing into an ordinary player's log. A setting in
`config.json` would work too, but the probe is client-side and the settings file is
the server's, so that is more machinery than the question is worth.

Decide before the first release. Whichever way it goes, the README paragraph
describing it changes with it.

## Compiled setters for reading a table into a shape

`SpecBinder.BindEntries` builds a spec with `Activator.CreateInstance` and writes each
key with `PropertyInfo.SetValue`, both of which go through reflection on every call.
`DomainBinder.InvokerFor` already solves the same problem the other way round, by
compiling an expression tree once per bound method, so the technique is in the
codebase and the pattern to copy is next door.

`bench.sh` now reports the row this would move: `bind: one Area table`, currently
about 500ns and 880 bytes per call, against 128ns for the four scalar arguments that
skip the shape entirely. Worth doing when that gap starts to matter — a script
scanning its surroundings every tick crosses it once per call — and worth leaving
alone until then, since it trades readable reflection for generated code.

## Nothing checks that the shipped examples parse

`examples/scripts/` is embedded in the build and written into every server's
MoonTweaks folder, and no part of the build reads any of it. A typo in one ships, and
the first person to find it is an author who copied it expecting it to work.

Compiling them needs no server and no bindings — `IScriptHost` can load a chunk
without running it, and a chunk that calls `moontweaks.players.setWorldData` compiles
whether or not that function exists. So the check is: walk `examples/scripts`, load
each file, report the ones that fail. All 40 pass today; this is about keeping it that
way.

The natural home is `scripts/docs.sh --check`, which already refuses to pass on an
undocumented member and is what CI runs, or a few lines in `luabench`, which already
references the engine.

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
growing. `moontweaks.server.setRules` writes to the server's own configuration and
`moontweaks.calendar.add` moves the world's clock, so the surface that wants
gating is now wider than it was.

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

## Asset properties still unbound

`moontweaks.items.set` and `moontweaks.blocks.set` carry what a server actually
retunes. What is left is shapes with no obvious Lua spelling yet:

`transitionableProps` decides how something spoils, dries or ripens. The shape
itself is built and bound — cooking recipes needed it, and `TransitionableProperties`
is what they write — so what is left here is the list: an item carries several of
them where a meal carries one. `creativeInventoryStacks` and
`creativeInventoryTabs` decide where it appears in creative. `crushing.quantity`
reaches its average and variance but not the distribution shape the game also
allows. A block's `sounds`, `collisionBoxes` and `cropProps` are each a shape of
their own rather than a value.

Tags are the one asymmetry worth naming: `tags` selects what to change and cannot
be changed. `CollectibleObject.Tags` is a `TagSet` the registry hands out rather
than a list an asset owns, so writing one means going through the tag registry,
and nothing has asked yet.

## Names and descriptions are out of reach

Not a task: a note, so it is not investigated twice. An item's displayed name
comes from `Lang.GetMatching("<domain>:item-<code>")` and its tooltip description
from `"<domain>:itemdesc-<code>"`. `Lang` is loaded on each side from that side's
own assets, and nothing textual is in `Packet_ServerAssets`, so the client renders
its own strings against a code that reaches it unchanged.

Changing them means shipping MoonTweaks to clients as well, which is a different
mod: every player would have to install it, where today they need nothing.

## Undoing what a script wrote to the world

`world.setBlock`, `queueBlock` and `breakBlock` all write through the plain
accessor, so nothing a script builds can be taken back except by building the
opposite. `IBlockAccessorRevertable` records what it wrote and can put it back.

What stops this being mechanical is ownership: an undo needs a stack, the stack
needs a lifetime, and neither belongs to a script that may fail halfway. Decide
whether an undo is per command, per script or per server before binding anything.

## Loading a chunk deliberately

`world.isLoaded` says whether a position can be written to. Nothing brings a chunk
in, so a script acting far from a player can only decline. `LoadChunkColumn` and
`TestChunkExists` are the calls, and both answer through a callback rather than
returning, so this wants the same shape a handler already has.

## The events still unbound

Sixteen are bound. `IServerEventAPI` declares 34 of its own and inherits 16 more
from `IEventAPI`, so 34 are unbound. They fall into groups that want quite
different things, listed here nearest to done first.

**Notifications carrying something new** want a payload shape apiece:
`DidPlaceBlock` (the block replaced and the stack placed, over `BlockEvent`),
`AfterActiveSlotChanged`, `MountGaitReceived`, `ChunkColumnLoaded`,
`ChunkColumnUnloaded`, `PlayerDimensionChanged`, `ChunkDirty`, `MapRegionLoaded`,
`MapRegionUnloaded`, and the entity events `IEventAPI` adds — `OnEntitySpawn`,
`OnEntityLoaded`, `OnEntityDeath`, `OnEntityDespawn`, `EntityMounted`,
`EntityUnmounted`. The entity ones want the entity domain deciding first how a
script names an entity that is not a player.

**Events whose handler must answer** are the ones needing a decision rather than
work. `CanUseBlock` and `CanPlaceOrBreakBlock` return a bool, `BreakBlock`,
`HandInteract` and `OnPlayerInteractEntity` take a `ref EnumHandling`, `PlayerChat`
takes `ref string message` and a `BoolRef consumed`, `BeforeActiveSlotChanged`
returns `EnumHandling`, `ServerSuspend` returns `EnumSuspendState`, and
`OnTestBlockAccess` and `OnTestBlockAccessClaim` decide whether somebody may touch
a place at all. `ScriptValue.Func.Call` already hands back what a handler returned
and `Raise` throws it away, so the machinery is half there. What is missing is a
rule: several handlers may answer one event, and what a veto beside an approval
means has to be decided before any of these is offered. The two access ones are
the sharpest case, since answering wrongly hands somebody else's build to a
stranger.

**Events on a hot path** should stay unbound whatever else is. `OnGetClimate`,
`OnGetWindSpeed`, `MatchesGridRecipe` and `MatchesRecipe` are raised per frame or
per match attempt, and a script call costs roughly 600ns against 3ns for the same
method in C#. Binding one puts the interpreter inside the game's inner loop. The
pull-based readings are bound instead: `world.climateAt` and `world.windAt` answer
the same questions when a script asks rather than when the game does.

**Events raised off the main thread** cannot be bound as things stand:
`BeginChunkColumnLoadChunkThread`, `OnTrySpawnGroupNearOffthread`,
`PhysicsThreadStart`, and `OnTrySpawnEntity`, which `GenCreatures` raises from
chunk column generation. The interpreter is not thread safe and nothing here
serialises calls into it, so binding one would be a race rather than a feature.
Offering them means one place that marshals a call onto the main thread, and
`IEventAPI.EnqueueMainThreadTask` is what such a place would be built on.
`ChunkColumnLoaded` and `ChunkColumnUnloaded` were checked and are main-thread;
the entity events have not been, and want checking before they are bound rather
than after.

`AssetsFinalizers` is obsolete and wants binding never.

## What a handler can do to the world

`moontweaks.players` reaches a player's body, their standing and their memory.
`moontweaks.entities` reaches everything else alive. `moontweaks.inventory` reaches
any set of slots. `moontweaks.world` reads and writes blocks, reads the weather and
remembers things against the save game. What is left is smaller than it was, and each
piece wants the same treatment the recipe kinds had — one owner for reaching the
thing, and a spec for what a script writes.

**Scanning an area.** `WalkBlocks` and `SearchBlocks` walk a region inside the
engine. A script doing the same through `blockAt` pays a call per block, which is
the expensive mistake the README already warns about, so this is a fix rather than
a convenience.

**Sound and particles.** `PlaySoundAt` and `SpawnParticles` reach a vanilla client
the same way `world.highlight` does, and are how a scripted effect is noticed at
all.

**Land claims.** `ILandClaimAPI.TestAccess` asks whether somebody may build
somewhere. Anything editing blocks on a populated server should be asking it, and
`world.setBlock` currently does not.

**Block entities past their inventory.** `moontweaks.inventory` reaches what a chest
holds. What a firepit is burning and what a quern is grinding sit on the block entity
rather than in its slots, and reaching them means naming a shape per kind of block —
which is a domain rather than a binding.

**Moving a stack in one operation.** `put` and `take` each say how much they moved, so
a script moving something between two places does both and has to put back what the
second half could not place. `IPlayerInventoryManager.TryTransferAway` does it as one
operation with the game choosing where it best fits. Worth binding once something
writes that put-back by hand twice.

Deliberately unbound: `Role`, `SetRole` and `Disconnect`, along with the granting
half of `IPermissionManager`. A script that can set roles or grant privileges can
grant itself anything, and the privilege on the `/moontweaks` command gates who may
run the command rather than what a script file may do. That wants deciding
alongside per-command permissions rather than before it.

## Reaching another mod

`moontweaks.mods` says what is loaded and what version it is, which is what a script
needs to guard a block of codes that only exist on some servers. It does not reach
into what another mod declared.

Two things would: `IWorldAccessor.GetRecipeRegistry(code)` resolves a recipe kind by
its code rather than by its type, which is what a scripted edit to another mod's
recipes needs. `IModLoader.GetModSystem` reaches a mod system outright — which is
where the survival mod keeps weather (`WeatherSystemServer`), temporal stability
(`SystemTemporalStability`) and block reinforcement (`ModSystemBlockReinforcement`).

The recipe registry one is worth doing and is only a lookup. The mod system one
couples this to another mod's internals across versions, and wants a decision about
whether that coupling is acceptable before any of it is written.
