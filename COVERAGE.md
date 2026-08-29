# API coverage

What the game offers a server-side mod, and how much of it a script can reach.
`TODO.md` holds work that is decided; this holds the survey it is decided from, so
a gap can be looked up rather than rediscovered.

Each section says what is bound, what is not, and what the gap costs a script
author. A gap that is deliberate says so and why, rather than being listed as
though nobody had looked at it.

| Area | Reached | Missing in one line |
| --- | --- | --- |
| Recipes | every kind | Kinds other mods declare |
| Item properties | 15 fields | Creative tabs, spoilage list, particles |
| Block properties | 12 further fields | Sounds, collision boxes, crop behaviour, decor |
| Players | 33 functions | Inventory past `give`, groups, connection facts |
| World | 15 functions | Entities, block entities, area scans, sound, explosions |
| Calendar | 8 functions | Nothing worth naming |
| Events | 16 of 50 | See `TODO.md` for the classification |
| Scheduling | 2 functions | Nothing worth naming |
| Commands | declaring one | Editing a command another mod declared |
| Storage | per player and per world | Nothing worth naming |
| Server state | facts and three rules | Shutdown, run phase, most of the config |
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
kind of its own, and `RecipeRegistry` reaches only the seven the survival mod
declares. A script therefore cannot touch a recipe kind another mod added.
`IWorldAccessor.GetRecipeRegistry(code)` is the lookup that would answer for one,
and it is a different lookup from the by-type one that exists.

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

Nothing creates an item or a block. `RegisterItem` and `RegisterBlock` exist, but
a scripted asset would need its shape, textures and name shipped to every client,
which is the different mod that `TODO.md` describes under names and descriptions.

## Players

Thirty-three functions reach where a player is and which way they face, their
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

- **Inventory.** `players.give` hands a stack over through `TryGiveItemstack` and
  says whether it fitted, which is the one piece a command giving something out
  needs. The rest of `IPlayerInventoryManager` — the hotbar, the held slot, reading
  or taking from what a player carries — is unreached, and is its own domain.
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
- **What entity they are looking at.** `CurrentBlockSelection` is bound as
  `players.looking`; `CurrentEntitySelection` waits on the entity domain.
- **Connection facts.** `Ping`, `IpAddress`, `LanguageCode`, `ConnectionState`.
- **Offline players past a name.** `IPlayerDataManager` answers for somebody who is
  not here; `uidOf` uses it, and the rest of what it holds is unreached.

## World

Fifteen functions read a block, place one, queue a batch, commit it, break a block
properly, exchange one, drop a stack, ask whether a chunk is loaded, find the
surface of a column, read light, climate and wind, outline blocks on a player's
screen, and remember something against the save game.

Still unreached:

- **Entities.** `GetEntitiesAround`, `GetEntityById`, `SpawnEntity`,
  `SpawnItemEntity`, `DespawnEntity`, `GetNearestEntity`,
  `GetEntitiesInsideCuboid`. Nothing reaches an entity that is not a player. Wants
  deciding first how a script names one.
- **Players near a place.** `GetPlayersAround` and `NearestPlayer` answer for
  players what the entity calls answer generally.
- **Block entities.** `GetBlockEntity` reaches what a chest holds or what a firepit
  is burning. `SpawnBlockEntity` and `RemoveBlockEntity` place and clear them.
  Nothing reaches any of it.
- **Area scans.** `WalkBlocks` and `SearchBlocks` walk a region inside the engine.
  A script doing the same through `blockAt` pays a call per block, which the README
  already warns is the expensive mistake — so the engine's own scan is the fix, not
  just a convenience. `WalkStructures` does the same for generated structures.
- **Sound and particles.** `PlaySoundAt` and `SpawnParticles` are how a scripted
  effect is noticed at all. Both are server-callable and reach a vanilla client, in
  the same way `world.highlight` does.
- **Undoing block edits.** `IBlockAccessorRevertable` records what it wrote so it
  can be put back. Everything here writes through the plain accessor, so nothing a
  script builds can be undone except by building the opposite.
- **Loading chunks deliberately.** `world.isLoaded` says whether a chunk is there;
  `IWorldManagerAPI.LoadChunkColumn` and `TestChunkExists` are how one is brought
  in, and are unbound.
- **Damaging a block short of breaking it.** `DamageBlock`.
- **Decor.** `SetDecor`, `GetDecors` and `BreakDecor` reach the layer a block
  carries on its faces.
- **Explosions.** `CreateExplosion`.
- **Land claims.** `ILandClaimAPI` decides who may build where, which anything
  editing blocks on a populated server has to respect. `TestAccess` asks, and `Add`
  and `Remove` change them.
- **Ray casting.** `RayTraceForSelection` answers what is along a line, which is
  what a reach test or a line-of-sight check needs.
- **World facts a script cannot change.** Light level tables, sun brightness, sea
  level as a setting rather than a reading, and the world configuration that
  `classExclusiveRecipes` is read from.

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

Unbound: `ShutDown`, `CurrentRunPhase`, `IsShuttingDown`, and the rest of
`IServerConfig` — the password, the whitelist mode, the tick rate, the chunk radius
and the roles. Several are settable and none are things a script should change
casually, which is why they wait on the per-command permissions entry in `TODO.md`.

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

What it does not do is reach into what another mod declared — its recipe registries,
its mod systems, its own settings. `IModLoader.GetModSystem` is how that would be
done, and it couples this mod to another's internals in a way the vanilla API does
not, which is a decision rather than an oversight.

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
