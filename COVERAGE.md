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
| Item properties | 20 fields | Particles, held sounds |
| Block properties | 16 further fields | Decor, particles |
| Asset registry | counting them, declaring tags | Listing or testing a code from a script |
| Players | 38 functions | Richer messages, connection facts |
| World | 25 functions | The fluid layer, explosions, decor, ray casting |
| Land claims | reading, adding and removing them | Editing one that already stands |
| Chat groups | reading, making, joining and speaking into them | Telling a client its tab exists |
| Entities | 23 functions | Giving one a tag, mounting, pathing, behaviours |
| Inventory | 13 functions | What one stack carries besides its wear |
| Calendar | 8 functions | Nothing worth naming |
| Events | 31 of 50 | See `TODO.md` for the classification |
| Scheduling | 2 functions | Nothing worth naming |
| Commands | declaring one | Six argument kinds of two dozen, aliases, others' commands |
| Storage | per player and per world | Nothing worth naming |
| Server state | facts, seven rules, declaring a privilege | Most of the config |
| Permissions | reading and declaring them | Granting and revoking |
| Other mods | naming, versions, three of the game's own systems | Any other mod's system |

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
a modded axe as readily as a vanilla one. The whole of the game's condition grammar
is reached — `allOf`, `anyOf` and `noneOf`, as names or as groups of them, spelled
as a recipe file spells them — with one deliberate difference: a condition naming
only `noneOf` selects what does not carry those tags, where the game's own loader
reads it as matching nothing.

Tags are written as well as read. `moontweaks.tags.add` declares names this server
has not got, and `addTags` and `setTags` on either setter put them on whatever the
selector reached — so a rule can be written once against a name and what carries that
name decided somewhere else. Declaring works only from a script's body: the server
locks its tag registry immediately after the phase scripts run in, and a handler
asking is told exactly that. Nothing reaches a player's machine for it, because the
server sends its whole tag table in the assets packet and each client registers the
names in handle order, so a declared tag is one every client knows by the same
handle.

Beyond those: `transitionableProps` is the list of ways a thing changes once it stops
being fresh, `creativeInventoryTabs` and `creativeInventoryStacks` are where it
appears in creative, and a block carries `sounds`, `collisionBoxes`,
`selectionBoxes` and `cropProps`. Every quantity written as an average and a variance
also takes a `dist`, so a drop or a crushing yield can be made to cluster near its
average rather than falling anywhere in its range.

Unbound, in the order they are likely to be wanted:

- `ParticleProperties` and `ParticleCollisionBoxes`, which decide what a block throws
  off and what those particles bounce against.
- `AllowSpawnCreatureGroups`, `Dimensions`, `LiquidSelectable`,
  `HeldPriorityInteract`, `HeldSounds`.
- Decor — a block laid onto the face of another, as a rug or a moulding — which is a
  placement path rather than a property.
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

Thirty-eight functions reach where a player is and which way they face, their
health, hunger and tiredness, their mode, their spawn — read and written both —
whether they sleep, what they have eaten, what they are looking at, what they may
do, what their abilities come to, their chat, and whatever a script chose to
remember about them. `kick` ends a session, telling them why; it does not keep them
out, so anything lasting has to turn them away again when they come back.

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
- **Granting privileges.** Reading them is bound, and declaring one is
  `moontweaks.server.addPrivilege`. What `IPermissionManager` grants, denies and
  revokes is not, and `SetRole` is deliberately excluded: a script that can set roles
  can grant itself anything.
- **Whitelisting and banning.** Neither is in the server API at all.
  `ICoreServerAPI.PlayerData` is typed `IPlayerDataManager`, which only looks players
  up; the object behind it is `PlayerDataManager` in VintagestoryLib, where
  `WhitelistPlayer` and `UnWhitelistPlayer` are public and `BanPlayer` is `internal`.
  Reaching either means casting past the interface the game published, which is a
  coupling this mod has taken on only for the three mod systems `MODSYSTEMS.md`
  tracks. Nothing has asked.
- **Richer messages.** `SendIngameError` and `SendIngameDiscovery` render against
  the client's own language files, and `SendLocalisedMessage` renders in the
  player's own language. `players.warn` gets the error styling without the lookup,
  which is as far as a server with no client half can go.
- **Groups.** Reached, through `moontweaks.groups` rather than here.
- **What they are looking at** is reached: `players.looking` for the block and
  `players.lookingAtEntity` for the creature, which hands back the identifier
  `moontweaks.entities` takes.
- **Writing the world's own spawn.** `players.spawn` reads where somebody would
  wake up and `moontweaks.world.spawn` reads where the world puts anyone with no
  spawn of their own, so nothing has to remember where home was any more. Moving the
  world's own, which `ISaveGame.DefaultSpawn` sets, stays unbound: it decides where
  every new arrival starts, and nothing has asked.
- **Which slot is held.** `moontweaks.inventory.setHeld` writes what is in the hand;
  `ActiveHotbarSlotNumber` decides which hand slot that is, and is unbound. The
  offhand slot is reached only as a numbered slot of the hotbar inventory, where
  `OffhandHotbarSlot` names it directly.
- **Connection facts.** `Ping`, `IpAddress`, `LanguageCode`, `ConnectionState`.
- **Offline players past a name.** `IPlayerDataManager` answers for somebody who is
  not here; `uidOf` uses it, and the rest of what it holds is unreached.

## World

Twenty-five functions read a block, place one, queue a batch, commit it, take that
batch back and put it again, break a block properly, exchange one, drop a stack, ask
whether a chunk is loaded, bring one in, find the surface of a column, read light,
climate and wind, read and move where the world puts an arrival, search a region for blocks
and count what is there, ask whether a player may build somewhere, play a sound,
throw off particles, outline blocks on a player's screen, and remember something
against the save game.

`undo` and `redo` walk the history each commit closes, and that history belongs to
the script that wrote it: a script takes back what it wrote and cannot reach what
another wrote underneath it. One step is one `setBlock`, or one `commit` however many
blocks were queued into it — which is the second reason to queue rather than write
block by block. How far back it goes is `undoHistory` in `config.json`, since every
step held is that step's blocks kept in memory. `breakBlock` and `exchangeBlock` are
outside it: a break has already scattered its drops and played its sound, and neither
comes back by putting the block where it stood.

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
- **Editing a land claim that already stands.** `moontweaks.claims` reads them,
  adds one and removes one. Changing one in place — moving its boxes, letting another
  player in, retitling it — is not bound: the game has no operation for it either,
  and does it by removing the claim and adding the replacement, which a script can
  already do.
- **Ray casting.** `RayTraceForSelection` answers what is along a line, which is
  what a reach test or a line-of-sight check needs.
- **World facts a script cannot change.** Light level tables, sun brightness, sea
  level as a setting rather than a reading, and the world configuration that
  `classExclusiveRecipes` is read from.

## Chat groups

`moontweaks.groups` reads which groups a player belongs to, finds one by name, makes
one, puts players in and takes them out, changes who may walk in, takes a group away,
and says something in one. A group is the game's own channel, so this is what reaches
some players and not others where `players.announce` reaches everybody and
`players.say` reaches one.

A group is named by its name rather than by its number. The game assigns the number as
a group is added and then offers no way to look one up by it, so the number is
something a script is told rather than something it uses; no two groups may share a
name, which is what makes the name a handle. Names are letters, numbers and
underscores, and both that and uniqueness are refused here by name rather than later.

A group a script makes is the same object the game's own `/group` command makes, so
the two are interchangeable. `joinPolicy` decides one thing and one thing only:
whether `/group join` lets a player in. It does not gate `groups.join`, which is the
server putting somebody there rather than them asking.

One thing does not reach the player. The game tells a client about a group as it joins
somebody to one, through `SendPlayerGroup` on a server system that is not on the
published API, so somebody a script joins may have no tab for the group until they
next connect. Messages reach them either way: `SendMessageToGroup` delivers by
membership, which is written at once.

## Land claims

`moontweaks.claims` reads the claims covering a block, lists one player's in the order
the game numbers them, adds one and takes one back. A claim is the game's own
protection, saved with the world and broadcast to every client by the server itself,
so land a script claims is protected and drawn for players who have installed nothing.

A claim carries no identifier, and `ILandClaimAPI.Remove` takes the object. So a
script names one the way the game's own `/land` commands do: by its owner and its
number among that owner's claims, counting from zero, which is the number `/land list`
shows that player. The number is a position, so removing one moves every later claim
of that owner down — the same behaviour `/land free` has, and what
`examples/scripts/world/claims.lua` works downwards to avoid.

Nothing here applies the checks the game applies when a *player* claims land: their
allowance, how many claims they may hold, or whether the box overlaps somebody else's.
A script claiming land is the server acting rather than a player asking, the same way
`world.setBlock` builds without consulting a claim. `claims.at` is what a script asks
first if it means to behave like a player.

Two things sit beside this and answer different questions. `world.testAccess` asks
whether one player may act somewhere, and is the whole answer rather than the claim
half of it: it runs the claim check and then every mod's `OnTestBlockAccess` handler,
so it already accounts for protections this mod knows nothing about.
`moontweaks.reinforce` reaches the survival mod's separate protection on a single
block.

`events.testBlockAccess` is the other side of that: a script's handler is asked the
same question and its answer is the decision. It is the one bound event whose return
value is read, and the only one that can override a land claim in either direction.

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

Selecting is by `code`, which accepts a `*` wildcard, or by `tags`, or by both — the
same condition grammar items and blocks take, read against `ICoreAPI.EntityTagRegistry`
rather than the collectible one. The two registries are separate and a name in one
says nothing about the other, so `library/codes.lua` lists the creature names under
`EntityTag`. Vanilla carries `animal`, `predator`, `huntable`, `humanoid`,
`habitat-land` and a dozen more.

What is not bound is giving a creature a tag. `EntityProperties.Tags` is what an
entity copies its own from as it is created, so writing one means reaching the entity
types the way `items.set` reaches the item registry, which is a mutation path rather
than a binding.

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

`move` takes a stack out of one place and puts it in another as one operation, and
is what a move should be written as: it carries the stacks themselves rather than
describing them, so a worn axe arrives worn, and whatever the destination could not
hold stays exactly where it was rather than needing putting back by hand. `put` and
`take` remain bound for the halves that are genuinely halves — charging for something
that then ceases to exist, or handing out something that did not exist a moment ago.

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
survive a restart. `addPrivilege` declares a privilege for as long as the server
runs, which is what lets a script's own command require a name of its own; the
Permissions section below says why declaring is bound where granting is not.

Seven rules are bound. Six are the server's own settings and are written back to its
configuration; `entitySpawning` is `ISaveGame.EntitySpawning` and belongs to the world
instead, so a server running two worlds may have it on in one and off in the other.
Reading answers every key the setter takes, which is what lets a script put back what
it moved.

Unbound besides: `ShutDown`, `CurrentRunPhase`, `IsShuttingDown`, and the rest of
`IServerConfig` — the password, the whitelist mode, the tick rate, the chunk radius,
the default role and the roles themselves. Several are settable and none are bound;
nothing has asked for them.

## Permissions

`players.hasPrivilege` and `players.privileges` read what a server has already
decided, and `moontweaks.server.addPrivilege` declares one, so that a command may
require a name this server would not otherwise know. Declaring is not granting:
administrators and the console hold a new privilege as it is declared and everybody
else gets it from a role in `serverconfig.json`, and the declaration lasts only as
long as the server runs.

What `IPermissionManager` grants, denies and revokes, per player or per group, stays
unbound, as does `SetRole`. The line is drawn there rather than at the module: a
script that can grant a privilege can grant itself any of them, where one that can
only declare a name has added nothing it did not already have.

Gating is where the game already puts it. A command a script declares carries its
own `privilege`, and the whole of `/moontweaks` is behind the one `commandPrivilege`
in `config.json`. Beyond that, a script file is writable only by whoever owns the
server, and what it does is theirs to get right.

## Other mods

`moontweaks.mods` says whether a mod is loaded, what version it declares and what
it calls itself, and lists them all. This is what lets one script serve two servers:
every other binding refuses a code the server does not have, so naming another mod's
items is only safe inside a guard that has asked first.

Another mod's recipe registries are reached, through `moontweaks.recipes.kinds` and
its siblings.

Three mod systems are reached outright, through `IModLoader.GetModSystem`.
`moontweaks.weather` reads what is falling where and holds the whole world's
precipitation at a level; `moontweaks.stability` answers how sound a place is and
when the next temporal storm is due; `moontweaks.reinforce` reads, adds, wears down
and clears the protection on a block. Each answers a shape of this mod's own and each
reports plainly on a server without the mod declaring it.

That couples this mod to another's internals, which the vanilla API's own versioning
does not cover. The types are referenced rather than reflected over, so a rename in a
game update fails the build here rather than a script on somebody's server, and
`MODSYSTEMS.md` lists every member each one calls — the list to walk after an update.
The coupling is not new: the recipe kinds are the survival mod's own types, so this
mod has never run without it.

What is not reached is any *other* mod's system. Such a type cannot be referenced
from here, so it would have to be reflected over by name, and what that binding
should look like depends on what is being reached for.

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
