# Waiting on a running server

Work that is built and unverified. Everything here compiles, is covered by the
diagnostics suite where a suite can reach it, and has never been run against a live
server — so what is written below is what the code should do, not what it has been
seen doing.

An entry leaves this file when it has been watched working, not when it looks right.
`TODO.md` holds work that is decided and unbuilt; this holds work that is built and
unconfirmed, which is a different question and a different fix when the answer is no.

## Running the suite at all

```sh
./scripts/deploy.sh          # build, package, install into both data paths
```

Restart the server — the assets packet is built once at startup, so nothing here
takes effect without one. Confirm the version in the mod list is the one just built,
then:

| Command | Covers |
| --- | --- |
| *(startup)* | the load and world reports, written to the log |
| `/diag player` | everything needing a body: spawn, claims, privileges, inventory |
| `/diag container` | the container functions, against an empty chest you placed |
| `/diag events` | which events have fired, and what the first firing looked like |
| `/diag` | the whole tally |

`grep '\[diag\] FAIL'` over the server log is the whole answer to "did anything
break".

---

## Land claims — 0.37.0

`moontweaks.claims` reads claims, adds one and removes one. `52-claims.lua` is the
round trip: it claims a plot a thousand blocks from world spawn, reads it back at a
block inside the box, checks the number it was given names it, and removes it.

**Run:** `/diag player`.

**Good result:** four passes — `claims.add`, `claims.of`, `claims.at`,
`claims.remove` — and you hold exactly the claims you held before.

**What to watch for beyond the suite:**

- The number a claim reads back as should be the number `/land list` shows you for
  the same claim. That agreement is the whole identity scheme; if it disagrees, the
  filter in `ClaimAccess.Owned` is walking a different order from `CmdLand`.
- A claim a script adds should be visible to a *client* — the outline drawn in the
  world and the refusal when somebody else tries to build. Land claims broadcast on
  add and remove, so this should need nothing installed on the player's side.
- Removing one claim of several should move the later ones down a number, matching
  `/land free`. `clearplots` in `examples/scripts/world/claims.lua` works downwards
  from the highest to stay correct; confirm that it does.

**If it fails:** the likely place is `ClaimAccess.Described(LandClaim)`, which works
out a claim's number by searching its owner's list for the object. That is the one
path that resolves a number rather than being handed one.

## The access veto — 0.37.0

`events.testBlockAccess` is the only bound event whose return value decides
something. A handler is asked after the land claim check, is given that check's
answer on `e.allowed`, and whatever it returns stands — including `"granted"`, which
overrides a claim.

**Run:** mine a block, then `/diag events`.

**Good result:** `testBlockAccess` fills in, reading something like
`<you> wanted to buildorbreak at <x> <y> <z>, and the server said granted`.

**What to watch for beyond the suite:** the sanctuary handler at the bottom of
`examples/scripts/world/claims.lua` refuses building within 64 blocks of 500, 500 for
anybody without `controlserver`. Standing there and trying to place a block should
refuse. Standing outside it should not.

**The one thing the design cannot promise:** another mod may ask the same question
from a thread the server does not tick on, and a script cannot answer there. Those
asks are left to the server and logged once, as
`'testBlockAccess' was asked from another thread`. Seeing that line is not a failure —
it means some other mod reached the check off-thread, and it is worth knowing which.

## Reading a spawn — 0.36.0

`players.spawn` answers where somebody would respawn now; `world.spawn` answers where
the world puts anyone with none of their own.

**Run:** `/diag player`.

**Good result:** `players.setSpawn`, `players.spawn` and `players.clearSpawn` all
pass, and the spawn check reports reading back the spawn just written.

**What to watch for:** the position comes back as the centre of a block, so a spawn
set at 100 reads back as 100.5. If it reads back as 100 exactly, the game changed
`EntityPosFromSpawnPos` and the documentation on the binding is now wrong.

`players.spawn` can answer nil, for a spawn stored without a height in terrain never
generated. That path is unlikely to be reachable deliberately and is not covered by
the suite.

## Declaring a privilege — 0.36.0

`server.addPrivilege` makes a name exist for the life of the server run, so a
command may require it.

**Run:** startup, then look for `[diag] pass server.addPrivilege`.

**Good result beyond the suite:** the `/warden` command in
`examples/scripts/players/privileges.lua` requires `moontweaks.warden`, which nothing
grants. An administrator should be able to run it — the game grants a newly declared
privilege to admins and the console as it is declared — and a player without the
privilege should be refused.

**If an administrator is also refused:** `RegisterPrivilege`'s `adminAutoGrant`
defaults to true and this binding does not pass it, so a refusal there means the
default changed.

## Tags of a server's own — 0.38.0

`moontweaks.tags.add` declares tag names; `addTags` and `setTags` on
`moontweaks.items.set` and `moontweaks.blocks.set` put them on assets.

**Run:** startup, then look for the four checks in `20-assets.lua`.

**Good result:** `tags.add`, `items.set (addTags)`, `items.set (selects by a declared
tag)` and `items.set (refuses an undeclared tag)` all pass. The third is the one that
matters — a tag that registered but never landed on an asset would pass the first two
and fail that one.

**The one thing that needs a second machine.** A tag declared by a script should
reach every client, because the server sends its whole tag table in the assets packet
and the client registers the names in handle order. Nothing in the suite can see
that, because the suite runs on the server.

To check it: declare a tag, put it on something with `addTags`, write a grid recipe
whose ingredient selects by that tag, and connect a client that has *nothing*
installed. The recipe should work, and the handbook should show it under "Created
by". If the recipe works when crafted but the handbook disagrees, the client resolved
the tag handle differently and the assumption behind this feature is wrong.

**If declaring fails with "the server closed its tag registry":** the window is one
startup phase wide. Scripts run at `AssetsLoaded`, and `ServerMain` locks both tag
registries immediately after that phase and before finalising assets. Moving this
mod's script run to `AssetsFinalize` would break tag declaration and nothing else.

## Chat groups — 0.39.0

`moontweaks.groups` reads which groups a player is in, finds one by name, and speaks
into one.

**Run:** startup for `groups.find` and `groups.say`, then `/diag player` for
`groups.of`.

**Good result:** all three pass, and the `[diag] the diagnostics suite is loaded` line
that `groups.say` sends reaches general chat — group 0 is the channel everyone is on,
which is why it is the one number a check can name without having made a group.

**What needs a real group:** make one in game (`/group create staff`), join it, and
check `/mygroups` from `examples/scripts/players/everybody.lua` names it with the
right standing. The join handler in that file speaks into it as somebody connects.

## Kicking — 0.39.0

`players.kick` ends a session with a reason.

**Not covered by the suite**, deliberately: disconnecting whoever is running the
checks is the opposite of a check that puts back what it moved, and the suite records
it as a skip so the coverage figure counts it as decided rather than missed.

**Run:** `/sendhome <player> <reason>` from `examples/scripts/players/everybody.lua`,
with a second account or a friend.

**Good result:** they are disconnected and the reason is what the client shows them.
The command reads their name *before* the kick, because a moment later there is no
connected player to resolve — if it reports the name correctly, that ordering is right.

## Creature tags — 0.39.0

`entities.around`, `nearest` and `count` take the same `tags` condition items and
blocks take, read against the creature registry.

**Run:** `/diag player` once the world is up.

**Good result:** `entities.around (by tag)` and `entities.around (refuses an unknown
tag)` both pass. The second is the one that matters — it proves the names are being
looked up rather than ignored.

**What to watch for:** the two registries are separate, and this is the check that
they are not being confused. Vanilla creature tags include `animal`, `predator`,
`huntable`, `humanoid` and `habitat-land`; an item tag such as `tool-axe` should be
refused here by name, and a creature tag should be refused inside `items.set`. If
either is accepted, the wrong registry is being asked.

`/whatsabout` in `examples/scripts/entities/finding.lua` counts predators against
other animals near you, which is the same thing read by eye.

## The wider server rules — 0.39.0

`server.rules` and `setRules` now carry `entitySpawning`, `blockTickInterval`,
`randomBlockTicksPerChunk` and `spawnCapPlayerScaling`.

**Run:** startup.

**Good result:** `server.rules`, `server.setRules`, `server.setRules (world rule)` and
`server.setRules (numbers)` all pass, and the server ends configured as it started.

**What the suite cannot see:** `entitySpawning` is kept with the world rather than the
server, so it is the one that should survive a restart of *this world* and should not
follow the server to another. `/quietworld on` in `examples/scripts/server/info.lua`
turns it off; confirm creatures stop appearing, then `/quietworld off`.

**Also worth confirming:** the six server-side rules are written back to
`serverconfig.json`. Change one, stop the server cleanly, and check the file.

## The world spawn, and wear — 0.39.0

`world.setSpawn` moves where anybody with no spawn of their own arrives.
`inventory.list` and `slot` now report `durability` and `maxDurability`.

**Run:** startup for `world.setSpawn`; `/diag player` exercises the inventory reads.

**Good result:** `world.setSpawn` passes, moving the spawn eight blocks and putting it
back. If it fails, the world spawn may have been left moved — `/worldspawn` says where
it is and `/setworldspawn` puts it where you stand.

**For the wear fields:** carry a used tool and a stack of something that does not wear
out, then run `/worn` from `examples/scripts/inventory/reading.lua`. A tool below a
quarter should be listed; the stack should not appear at all rather than appearing at
zero.

## Chat groups, made and joined — 0.40.0

`moontweaks.groups` now makes groups, puts players in and takes them out, changes who
may walk in, and takes a group away. `groups.say` moved from the group's number to its
name, along with everything else here.

**Run:** startup for the load-time lifecycle, then `/diag player`.

**Good result:** `groups.find`, `groups.add`, `groups.setJoinPolicy`, `groups.say`,
`groups.add (refuses a name the game will not take)` and `groups.remove` pass at
startup, and `groups.of` and `groups.join` pass on `/diag player`. The server ends
holding exactly the groups it started with — check with `/group list`.

**The thing that needs a real client, and the reason this section exists.** The game
tells a client about a group as it joins somebody to one, through `SendPlayerGroup` on
a server system that is not on the published API. So a player a *script* joins may
have **no chat tab for the group until they next connect**. What I could not test is
what the client does with a message sent to a group it has no tab for — it may render
in the current tab, or be dropped silently.

To check it: with a second account, run
`/party new testparty` then `/party invite testparty <them>` from
`examples/scripts/players/parties.lua`, and then say something with
`/party` chat or trigger the routing handler. Watch whether:

1. The invited player sees the message at all before reconnecting.
2. The tab appears after they reconnect.
3. Anything is lost rather than merely misplaced.

If messages are dropped rather than misplaced, `groups.join` should say so in its own
documentation rather than only here, and the example should tell the invited player to
reconnect rather than mentioning it in passing.

**Also worth confirming:** a group a script made should be indistinguishable from one a
player made. Have a player `/group rename` and `/group disband` one that
`groups.add` created — both should work, because it is the same object filled in the
same way.

**And the join policy.** `/party new open true` sets `joinPolicy = "everyone"`; a
second player should then be able to `/group join open`. `/party open <name> false`
should stop them, with the game answering "No such group found or the invite policy is
invite only". That string is the game's, not this mod's, which is how you know the
field is being read.

## Chat — 0.40.0

`events.playerChat` is asked before anybody sees a message: return a string to replace
it, `false` to swallow it, `true` to put it back, nothing to leave it alone.

**Run:** say anything in chat, then `/diag events`.

**Good result:** `playerChat` fills in, reading something like
`<you> said 12 character(s) in group 0, delivered: true`.

**What to check by hand**, all from `examples/scripts/players/parties.lua`:

- A message beginning with `!` should go to your party and **not** appear in general
  chat. If it appears in both, the swallow is not taking.
- An administrator's messages in general chat should read `[staff] ...`.
- `/w <player> <message>` and `/r <message>` should carry a conversation both ways,
  and `/r` should refuse once the other player has logged out.

**The sharp edge to see for yourself.** The last answer stands, which is the game's own
rule and what you asked for. Put a handler that returns `true` in a file sorting after
one that returns `false` and the swallow is undone — that is working as designed, and
it is why anything that must have the last word belongs in a file that sorts last. Worth
provoking once so the behaviour is familiar before somebody hits it by accident.

## Numbers to re-measure

`examples/scripts/diagnostics/README.md` used to name a per-phase exercised count —
"64 of 166" at load, "119 of 166" once the world was up. Those went stale as bindings
were added, and are now written as proportions rather than numbers. Read the real
figures off a run and put them back if they are worth naming.
