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
| Item and block properties | 16 fields | Creative tabs, spoilage list, tool class, light |
| Players | 20 functions | Rest of inventory, privileges, listing who is online |
| World | 5 functions | Calendar, entities, sound, area scans, block entities |
| Events | 16 of 50 | See `TODO.md` for the classification |
| Scheduling | nothing | A script cannot do anything on a timer |
| Commands | declaring one | Editing a command another mod declared |
| World-scoped storage | nothing | Only per-player data has a home |
| Server state and control | nothing | Uptime, player count, config, shutdown |
| Permissions | nothing | Privileges cannot be declared or checked |

## Recipes

Grid, knapping, clay forming, smithing, barrel, alloy and cooking are bound with
`add`, `remove` and `count` apiece. Every kind the survival mod declares is reached.

Cooking is the one that does not fit the shape of the others: a meal has no single
product, so a recipe is selected by the code it carries rather than by what it
makes, and `moontweaks.recipes.cooking.remove` takes that code where every other
kind takes an output.

Beyond the vanilla kinds, `RegisterRecipeRegistry` lets any mod declare a recipe
kind of its own, and `RecipeRegistry` reaches only the seven the survival mod
declares. A script therefore cannot touch a recipe kind another mod added. Doing
so means resolving a registry by its code rather than by its type, which is a
different lookup from the one that exists.

## Item and block properties

`moontweaks.items.set` and `moontweaks.blocks.set` reach sixteen fields, which is
most of what a server actually retunes: durability, stack size, tool tier, mining
speed, attack power and range, material density, storage flags, tags, arbitrary
attributes, and the combustible, grinding, crushing and nutrition property groups.

Unbound, in the order they are likely to be wanted:

- `CreativeInventoryTabs` and `CreativeInventoryStacks` decide where something
  appears in creative, and are a list of shapes rather than one value.
- `TransitionableProps` decides how something spoils, dries or ripens. The shape is
  bound already, since a cooking recipe writes one; an item carries a list of them
  where a meal carries one, and it is the list that is unreached.
- `Tool` names which tool class an item counts as, which is separate from the
  `toolTier` that is bound.
- `LightHsv` makes a block glow.
- `HeldSounds`, `LiquidSelectable`, `HeldPriorityInteract`, `ParticleProperties`.
- The model transforms, which decide how something is held and dropped. These are
  client-rendered and so are out of reach for the same reason names are.

Nothing creates an item or a block. `RegisterItem` and `RegisterBlock` exist, but
a scripted asset would need its shape, textures and name shipped to every client,
which is the different mod that `TODO.md` describes under names and descriptions.

## Players

Nineteen functions reach where a player is, their health, hunger and tiredness,
their mode, their spawn, whether they sleep, what they have eaten, their chat, and
whatever a script chose to remember about them.

**Nothing lists players.** Every function takes an identifier, and the only source
of one is an event handler. A script cannot ask who is online, so anything
addressed to everybody — an announcement, a sweep over players, a count for a
message — cannot be written. `IServerAPI.Players` and `world.AllOnlinePlayers`
both answer it, and this is the smallest change on this page with the largest
effect on what a script can be written to do.

Also unbound:

- **Inventory.** `players.give` hands a stack over through `TryGiveItemstack` and
  says whether it fitted, which is the one piece a command giving something out
  needs. The rest of `IPlayerInventoryManager` — the hotbar, the held slot, reading
  or taking from what a player carries — is unreached, and is its own domain.
- **Privileges.** `HasPrivilege` and `Privileges` are reads and safe. `SetRole` is
  deliberately excluded, and a read is not the same thing: it lets a script gate
  its own behaviour without granting anything.
- **Richer messages.** `SendIngameError` and `SendIngameDiscovery` render
  differently from ordinary chat, and `SendLocalisedMessage` renders in the
  player's own language. `say` is the plain form of all three.
- **Groups.** `Groups` and `GetGroup` name the chat groups a player belongs to,
  which is what messaging anything other than general chat needs.
- **What they are looking at.** `CurrentBlockSelection` and
  `CurrentEntitySelection`.
- **Connection facts.** `Ping`, `IpAddress`, `LanguageCode`, `ConnectionState`.

## World

Five functions read a block, place one, queue a batch, commit it, and drop an item
stack. Everything else the world offers is unreached.

- **The calendar.** `IGameCalendar` gives hour of day, day of year, month, season,
  year, moon phase and total elapsed time. Every one is a read of a value the game
  already holds, so this is the cheapest useful domain left, and it is what any
  script about seasons or time of day needs first.
- **Entities.** `GetEntitiesAround`, `GetEntityById`, `SpawnEntity`,
  `SpawnItemEntity`, `DespawnEntity`, `GetNearestEntity`. Nothing reaches an
  entity that is not a player. Wants deciding first how a script names one.
- **Players near a place.** `GetPlayersAround` and `NearestPlayer` answer for
  players what the entity calls answer generally, and would work with the listing
  gap above.
- **Breaking a block properly.** `setBlock` replaces a block; `BlockAccessor.BreakBlock`
  breaks it as a player would, with its drops and its sound. The two are not the
  same operation and only one is offered.
- **Block entities.** `GetBlockEntity` reaches what a chest holds or what a firepit
  is burning. Nothing reaches it.
- **Area scans.** `WalkBlocks` and `SearchBlocks` walk a region inside the engine.
  A script doing the same through `blockAt` pays a call per block, which the README
  already warns is the expensive mistake — so the engine's own scan is the fix, not
  just a convenience.
- **Sound and particles.** `PlaySoundAt` and `SpawnParticles` are how a scripted
  effect is noticed at all.
- **Explosions.** `CreateExplosion`.
- **Land claims.** `ILandClaimAPI` decides who may build where, which anything
  editing blocks on a populated server has to respect.
- **World facts.** Seed, sea level, map size, default spawn, light levels, and the
  world configuration that `classExclusiveRecipes` is read from.

## Scheduling

Nothing. `RegisterGameTickListener`, `RegisterCallback` and `Event.Timer` are all
unbound, so a script can react to something the game raises and can do nothing on
its own schedule.

This is structural rather than a missing convenience: a whole shape of server
script — every so often, check something and act — cannot be written at all. The
interpreter already outlives the run that made it, which is the hard part, so what
is left is a domain that registers a callback and a decision about what happens to
a listener when a script fails.

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
altered, since the game allows one owner per name and this refuses a name that is
taken. Nesting stops nowhere in particular here but the game's own `BeginSubCommand`
nests indefinitely, which this follows.

## World-scoped storage

`players.setData` and `players.getData` store against a player and are saved with
them. There is no equivalent for the world: `ISaveGame.StoreData` holds data
against the save game itself, and `StoreModConfig` and `LoadModConfig` hold it in a
file beside the scripts.

So a script can remember something about each player and nothing about the world.
Anything counted, accumulated or tracked globally has nowhere to live.

## Server state and control

`IServerAPI` gives uptime, total play time, the current run phase, whether the
server is shutting down, the player list, and `ShutDown`. `IServerConfig` gives the
server name, the welcome message, the maximum client count, and the flags for PvP,
fire spread and falling blocks — several of them settable.

None of it is bound. A script cannot say how long the server has been up, cannot
read the rules it is running under, and cannot change them.

## Permissions

`IPermissionManager` declares privileges and grants, denies and revokes them, per
player or per group. Nothing is bound, and the per-command permissions entry in
`TODO.md` is held pending more of the API precisely so that what is worth gating is
known before the gates are built. `RegisterPrivilege` is what a script would need
to gate anything of its own.

## Deliberately out of scope

- **Anything client-side.** Rendering, GUI, sounds a client picks, model
  transforms, and item names and descriptions all live on the client and are
  unreachable without shipping this mod to every player. `TODO.md` records the
  reasoning under names and descriptions.
- **Networking.** `INetworkAPI` sends packets between a mod's own server and client
  halves. There is no client half.
- **Worldgen.** `MapChunkGeneration`, `ChunkColumnGeneration` and the tree
  generators all run on the generation thread, so they sit behind the same
  main-thread marshalling that `TODO.md` describes for off-thread events.
- **Registering classes.** `RegisterBlockClass`, `RegisterEntityBehaviorClass` and
  their siblings take a CLR type. A Lua table is not one, and making it one is a
  different mod from this.
