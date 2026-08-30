# API coverage

What the game offers a server-side mod, and how much of it a script can reach.
`TODO.md` holds work that is decided; this holds the survey it is decided from, so
a gap can be looked up rather than rediscovered.

Each section says what is bound, what is not, and what the gap costs a script
author. A gap that is deliberate says so and why, rather than being listed as
though nobody had looked at it.

| Area | Reached | Missing in one line |
| --- | --- | --- |
| Recipes | every kind, vanilla and modded | Editing a kind this mod has never seen |
| Item properties | 15 fields | Creative tabs, spoilage list, particles |
| Block properties | 12 further fields | Sounds, collision boxes, crop behaviour, decor |
| Asset registry | counting them | Listing or testing a code from a script |
| Players | 36 functions | Groups, richer messages, connection facts, reading a spawn |
| World | 21 functions | The fluid layer, explosions, decor, ray casting |
| Entities | 23 functions | Selecting by tag, mounting, pathing, behaviours |
| Inventory | 12 functions | What one stack carries, moving between two places at once |
| Calendar | 8 functions | Nothing worth naming |
| Events | 26 of 50 | See `TODO.md` for the classification |
| Scheduling | 2 functions | Nothing worth naming |
| Commands | declaring one | Six argument kinds of two dozen, aliases, others' commands |
| Storage | per player and per world | Nothing worth naming |
| Server state | facts and three rules | Mob spawning, tick rates, most of the config |
| Permissions | reading them | Declaring, granting and revoking |
| Other mods | naming and versions | Reaching into what one declared |

## Recipes

Grid, knapping, clay forming, smithing, barrel, alloy and cooking are bound with
`add`, `remove` and `count` apiece. Every kind the survival mod declares is reached.

Cooking is the one that does not fit the shape of the others: a meal has no single
product, so a recipe is selected by the code it carries rather than by what it
makes, and `moontweaks.recipes.cooking.remove` takes that code where every other
kind takes an output.

Beyond the vanilla kinds, `RegisterRecipeRegistry` lets any mod declare a recipe
kind of its own, and `moontweaks.recipes.kinds`, `count` and `remove` reach any of
them by registry code through `IWorldAccessor.GetRecipeRegistry`. A kind this mod
has never seen is matched on what its recipes resolve to as an output, since that
is all such a kind reliably offers — so a modded kind can be counted and thinned
out, and a recipe cannot be added to one or have its ingredients read.

## Item and block properties

`moontweaks.items.set` reaches the fifteen fields anything carried has: durability,
stack size, tool class and tool tier, mining speed, attack power and range, material
density, storage flags, what damages it, arbitrary attributes, and the combustible,
grinding, crushing and nutrition property groups.

`moontweaks.blocks.set` takes all of those and twelve more that only something
standing in the world has: breaking resistance, required mining tier, block
material, drops, light colour, light absorption, replaceability, fertility, walk
speed, drag, climbability and rain permeability.

Selecting is by `code`, which accepts a `*` wildcard, or by `tags`, or by both.
Tags select on what an asset is rather than what it is called, so one entry reaches
a modded axe as readily as a vanilla one. **Tags are read and never written**: a
script cannot give an asset a tag it does not carry.

Unbound, in the order they are likely to be wanted:

- `CreativeInventoryTabs` and `CreativeInventoryStacks` decide where something
  appears in creative, and are a list of shapes rather than one value.
- `TransitionableProps` decides how something spoils, dries or ripens. The shape is
  bound already, since a cooking recipe writes one; an item carries a list of them
  where a meal carries one, and it is the list that is unreached.
- `BlockSounds`, which decides what a block sounds like to walk on, break and place.
- `CollisionBoxes` and `SelectionBoxes`, which are its shape as far as anything
  bumping into it is concerned.
- `CropProps`, which is a domain of its own rather than a field.
- `AllowSpawnCreatureGroups`, `Dimensions`, `ParticleProperties`,
  `LiquidSelectable`, `HeldPriorityInteract`, `HeldSounds`.
- The model transforms, which decide how something is held and dropped. These are
  client-rendered and so are out of reach for the same reason names are.

Nothing lists them either. `moontweaks.items.count` and `moontweaks.blocks.count`
say how many there are, and `IWorldAccessor.SearchItems` and `SearchBlocks` answer
which codes match a wildcard — the lookup this mod uses internally and does not
offer. A script wanting the codes of every ingot, or wanting to ask whether one
exists before naming it, has `moontweaks.mods` to guard on and nothing finer:
every other binding refuses an unknown code rather than answering for it.

Nothing creates an item or a block. `RegisterItem` and `RegisterBlock` exist, but
a scripted asset would need its shape, textures and name shipped to every client,
which is the different mod that `TODO.md` describes under names and descriptions.

## Players

Thirty-six functions reach where a player is and which way they face, their
health, hunger and tiredness, their mode, their spawn, whether they sleep, what
they have eaten, what they are looking at, what they may do, what their abilities
come to, their chat, and whatever a script chose to remember about them.

`players.all` lists who is online, which is the only source of a player identifier
that is not an event, and `players.uidOf` turns a name somebody typed into one —
answering for a player who is not here, which nothing else in the module does.
`players.announce` reaches everybody without needing either.

Stats — `stat`, `setStat`, `clearStat` — are how walk speed, healing and hunger
rate are retuned per player. The game holds each ability as a set of named
contributions added to a base of 1, so a script names its own and can take exactly
that one back; contributions this mod writes carry its own prefix, so a script
cannot clear one the game or another mod is holding.

Also unbound:

- **Inventory** is reached, through `moontweaks.inventory` rather than here.
  `players.give` remains the one-line way to hand something over and hear whether it
  fitted. What `IPlayerInventoryManager` still keeps to itself is moving a stack from
  one place to another in a single call: a script does it as a `take` and a `put`, and
  has to put back what the second half could not place.
- **Granting privileges.** Reading them is bound; `IPermissionManager` also declares,
  grants, denies and revokes them, and none of that is. `SetRole` is deliberately
  excluded: a script that can set roles can grant itself anything.
- **Richer messages.** `SendIngameError` and `SendIngameDiscovery` render against
  the client's own language files, and `SendLocalisedMessage` renders in the
  player's own language. `players.warn` gets the error styling without the lookup,
  which is as far as a server with no client half can go.
- **Groups.** `Groups` and `GetGroup` name the chat groups a player belongs to,
  which is what messaging anything other than general chat needs. `IGroupManager`
  creates and removes them.
- **What they are looking at** is reached: `players.looking` for the block and
  `players.lookingAtEntity` for the creature, which hands back the identifier
  `moontweaks.entities` takes.
- **Reading a spawn.** `players.setSpawn` and `clearSpawn` write one; nothing reads
  one back. `IServerPlayer.GetSpawnPosition` answers, and a script that wants to
  send somebody home has to have remembered where home was itself. The world's own
  default spawn, which `ISaveGame.DefaultSpawn` holds, is unreached in both
  directions.
- **Which slot is held.** `moontweaks.inventory.setHeld` writes what is in the hand;
  `ActiveHotbarSlotNumber` decides which hand slot that is, and is unbound. The
  offhand slot is reached only as a numbered slot of the hotbar inventory, where
  `OffhandHotbarSlot` names it directly.
- **Connection facts.** `Ping`, `IpAddress`, `LanguageCode`, `ConnectionState`.
- **Offline players past a name.** `IPlayerDataManager` answers for somebody who is
  not here; `uidOf` uses it, and the rest of what it holds is unreached.

## World

Twenty-one functions read a block, place one, queue a batch, commit it, break a
block properly, exchange one, drop a stack, ask whether a chunk is loaded, bring one
in, find the surface of a column, read light, climate and wind, search a region for
blocks and count what is there, ask whether a player may build somewhere, play a
sound, throw off particles, outline blocks on a player's screen, and remember
something against the save game.

Still unreached:

- **The fluid layer.** A block position holds two blocks: a solid one and a fluid
  one. `world.blockAt` reads the solid layer and falls back to the fluid layer only
  where the solid one is empty, and `world.setBlock` routes a block to the fluid
  layer only when that block belongs there. So placing water in an empty place works
  and reading water in an empty place works; asking whether a block is underwater
  does not, and draining a place without disturbing what stands in it does not
  either. `BlockLayersAccess` is the argument that names a layer, and every call
  here takes its default.
- **Entities** are reached, through `moontweaks.entities`. What is left there is
  mounting, pathing and behaviours, none of which has been asked for.
- **Players near a place.** `moontweaks.entities.around` answers this already, since
  a player's body is an entity and `skipPlayers` turns them back on. `GetPlayersAround`
  and `NearestPlayer` would answer it directly and are unbound.
- **Block entities past their inventory.** `moontweaks.inventory` reaches what a chest
  holds. What a firepit is burning, what a quern is grinding and everything else a
  `BlockEntity` keeps is unreached, as are `SpawnBlockEntity` and `RemoveBlockEntity`.
- **Scans past a box of blocks.** `world.findBlocks` and `countBlocks` walk a region
  inside the engine, which is what keeps a scan off the per-block call that the
  README warns is the expensive mistake. `WalkStructures` does the same for generated
  structures and is unbound, as is `SearchFluidBlocks` for the layer above.
- **An effect one player alone notices.** `world.playSound` and `spawnParticles` are
  positional: everybody near enough sees and hears them. `PlaySoundFor` plays to one
  player wherever they are, which is what a private cue wants.
- **Undoing block edits.** `IBlockAccessorRevertable` records what it wrote so it
  can be put back. Everything here writes through the plain accessor, so nothing a
  script builds can be undone except by building the opposite.
- **Damaging a block short of breaking it.** `DamageBlock`.
- **Decor.** `SetDecor`, `GetDecors` and `BreakDecor` reach the layer a block
  carries on its faces.
- **Explosions.** `CreateExplosion`.
- **Changing a land claim.** `world.testAccess` asks `ILandClaimAPI` who may build
  where, which is what anything editing blocks on a populated server has to respect.
  Listing the claims at a place, and `Add` and `Remove` which change them, are
  unbound.
- **Ray casting.** `RayTraceForSelection` answers what is along a line, which is
  what a reach test or a line-of-sight check needs.
- **World facts a script cannot change.** Light level tables, sun brightness, sea
  level as a setting rather than a reading, and the world configuration that
  `classExclusiveRecipes` is read from.

## Entities

`moontweaks.entities` reaches everything alive that is not a player, and the stacks
lying on the floor besides. It finds them — `around`, `nearest`, `count`, `get` — and
changes them: spawning and despawning, killing and hurting, health, position, fire,
names, abilities, and whatever a script chose to remember against one.

An entity is named by the identifier a search hands back, in the same way a player is
named by theirs. The two differ in one way a script has to know about: a player's
identifier outlives everything, while an entity's is good only while the entity is
loaded. A chunk unloading takes one out of reach without saying so, which is what
`isLoaded` exists to answer.

Selecting is by `code`, which accepts a `*` wildcard. Items and blocks are also
selected by `tags`, and entities are not: the game keeps entity tags in a registry of
their own, `ICoreAPI.EntityTagRegistry`, and `Entity.Tags` carries them. Until that
is bound, "every hostile creature" is spelled as a list of codes where the same
question about an item is spelled as one tag.

Left unbound besides: mounting and dismounting, the pathfinding an entity does for
itself, and the behaviours it is built from. Each is a domain rather than a field,
and none has been asked for.

## Inventory

`moontweaks.inventory` reaches any set of slots, named by where it is — a player and
which of their inventories, a block position, or an entity that carries one. One shape
rather than three families of function, because everything a script does to a chest it
also does to a backpack.

Reading is `size`, `list`, `count` and `slot`; writing is `put`, `take`, `setSlot`,
`clearSlot` and `clear`; and `held`, `setHeld` and `clearHeld` reach the one slot a
player has in their hand. Slots are numbered from 1, as everything in Lua is.

Both `put` and `take` say how much they actually moved, which is rarely something a
caller may assume: a bag may not hold enough and a chest may not have room. A script
charging for something reads that number rather than trusting it.

What is left is two things.

Moving a stack from one place to another in a single operation, with the game
deciding where it best fits, is what `IPlayerInventoryManager.TryTransferAway` does
and `TryTransferTo` does to a named slot. A script spells either as a `take` and a
`put`, and has to put back what the second half could not place.

What one stack carries is unreached in the reading direction. A slot answers with a
code, a count, what would fit and what the game calls it — not with the attributes
the stack itself holds, of which a tool's remaining durability is the one every
server asks about. Writing them is bound: every shape naming a stack takes
`attributes`, so a script can hand over a half-worn axe it cannot afterwards read.

Nothing says a set of slots changed. `IInventory.SlotModified` is raised per
inventory rather than through `IEventAPI`, so hearing it means subscribing to one
container at a time, which is a shape rather than an event binding.

## Calendar

`moontweaks.calendar` reads the clock — hour, day, month, year, moon phase, elapsed
hours and days, and the game's own pretty date — as one table, and reads season and
hemisphere at a position, since the two halves of the world are half a year apart.

It also writes: `add` moves time itself, so everything ageing by the clock ages
with it; `setSpeed` and `clearSpeed` change how fast it passes, under a name so two
scripts do not undo each other; `setSeason` and `clearSeason` hold the world at a
point in the year.

## Scheduling

`moontweaks.server.every` and `after` register a repeating and a one-shot callback,
and a handler answering `false` stops a repeating one. A timer asked for while
scripts load starts only once the run has succeeded, so `/moontweaks check` starts
nothing; one asked for inside a handler starts at once.

`Event.Timer` and the position-keyed listener overloads are unbound and want a
reason before they are added.

## Commands

`moontweaks.commands.add` declares a command with arguments, subcommands and a
required privilege, and calls a script's handler when somebody runs it. Six kinds of
argument are read — a word, a whole number, a number, on or off, the rest of the
line, and an online player — each of which the game parses and offers completions
for before a handler sees it.

A command a script declares needs nothing on a player's machine: the client sends
the line as typed and the server reads it. Declaring one still happens as the server
loads, so a new command wants a restart the way a new recipe does.

The game parses two dozen kinds of argument and six are bound. What a script cannot
ask for: a position, whether typed or taken from where the caller stands
(`WorldPosition`, `Vec3i`); an item or block code, which the game completes from the
registry as it is typed (`Item`, `Block`); an entity or entity type; a privilege or
a role; a colour; a number held to a range. The asset ones are the sharpest absence,
since naming an asset is what this mod is for.

A command also carries aliases — `WithAlias` and `WithRootAlias` — and neither is
bound, so a script's command answers to exactly one name.

What is not reached: a command another mod already declared cannot be added to or
altered. That is this mod's own rule rather than the game's —
`IChatCommandApi.GetOrCreate` hands back a command that already exists and
`BeginSubCommand` nests onto it — and it is held because two mods extending one
command have no way to agree on what its arguments mean.

`IChatCommandApi.Execute` runs a command a script did not declare, which nothing
here offers.

## Storage

`players.setData` and `players.getData` store against a player and are saved with
them. `world.setData` and `world.getData` store against the save game, which is the
home for anything counted across everybody rather than for each of them. Both take
any value a script can write, a table included, and both scope their keys under this
mod so a script cannot read or overwrite what another mod stored.

`StoreModConfig` and `LoadModConfig` hold data in a file beside the scripts rather
than in the save, and are unbound: a script wanting a file wants it for a reason
that has not come up.

## Server state and control

`moontweaks.server.info` reads the server's name, its welcome message, how many
players it allows and how many are here, its uptime, total play time, world name,
seed, sea level and map size. `rules` and `setRules` read and change PvP, fire
spread and falling blocks, written back to the server's configuration so they
survive a restart.

Three rules are bound and the rules beside them are not. Whether creatures spawn at
all is `ISaveGame.EntitySpawning`; how fast crops grow and fires spread is
`BlockTickInterval` and `RandomBlockTicksPerChunk`; how the spawn cap scales with the
number of players is `SpawnCapPlayerScaling`. Each is one value a server operator
actually retunes, and each sits beside `AllowPvP` rather than anywhere harder to
reach.

Unbound besides: `ShutDown`, `CurrentRunPhase`, `IsShuttingDown`, and the rest of
`IServerConfig` — the password, the whitelist mode, the tick rate, the chunk radius,
the default role and the roles themselves. Several are settable and none are things a
script should change casually, which is why they wait on the per-command permissions
entry in `TODO.md`.

## Permissions

`players.hasPrivilege` and `players.privileges` read what a server has already
decided. `IPermissionManager` also declares privileges and grants, denies and
revokes them, per player or per group; none of that is bound, and the per-command
permissions entry in `TODO.md` is held pending it, precisely so that what is worth
gating is known before the gates are built.

## Other mods

`moontweaks.mods` says whether a mod is loaded, what version it declares and what
it calls itself, and lists them all. This is what lets one script serve two servers:
every other binding refuses a code the server does not have, so naming another mod's
items is only safe inside a guard that has asked first.

Another mod's recipe registries are reached, through `moontweaks.recipes.kinds` and
its siblings. What is not reached is a mod system itself, and the settings it keeps.
`IModLoader.GetModSystem` is how that would be done, and it couples this mod to
another's internals in a way the vanilla API does not, which is a decision rather
than an oversight.

The survival and essentials mods are where a good deal of a server's behaviour
actually lives: `WeatherSystemServer` overrides precipitation and spawns lightning,
`SystemTemporalStability` answers how stable somewhere is, and
`ModSystemBlockReinforcement` decides what may be broken. All three are reachable
only through `GetModSystem`, and all three would tie this to their versions.

## Deliberately out of scope

- **Anything client-side.** Rendering, GUI, sounds a client picks, model
  transforms, and item names and descriptions all live on the client and are
  unreachable without shipping this mod to every player. `TODO.md` records the
  reasoning under names and descriptions. `world.highlight` is the exception that
  proves it: the game already ships that drawing, so a server may point it at
  whatever it likes without the client knowing what a mod is.
- **Networking.** `INetworkAPI` sends packets between a mod's own server and client
  halves. There is no client half.
- **The mod event bus.** `Event.PushEvent` and `RegisterEventBusListener` carry
  signals between mods. Same coupling question as `GetModSystem`, and no script has
  wanted one yet.
- **Worldgen.** `MapChunkGeneration`, `ChunkColumnGeneration` and the tree
  generators all run on the generation thread, so they sit behind the same
  main-thread marshalling that `TODO.md` describes for off-thread events.
- **Registering classes.** `RegisterBlockClass`, `RegisterEntityBehaviorClass` and
  their siblings take a CLR type. A Lua table is not one, and making it one is a
  different mod from this.
