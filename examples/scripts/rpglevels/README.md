# RPG levels

A levelling system for this server, written entirely in MoonTweaks Lua. Players earn
experience for killing things, gain levels, and are handed an item every fifth level
that suits how far into the game they are.

## What a player sees

```
DING!! You gained a lvl and are now 7
```

and, on every fifth level,

```
Level 10 reward (a full belly): 4 x spelt bread.
```

A reward that does not fit in their pack is dropped at their feet rather than lost.

## Commands

| Command | Who | What |
| --- | --- | --- |
| `/rpg` | anybody | Your level, the experience banked towards the next, and the level your next reward comes at. |
| `/rpgadmin show <player>` | `controlserver` | The same, for somebody else. |
| `/rpgadmin xp <player> <amount>` | `controlserver` | Grant experience, levelling and rewarding as it lands. |
| `/rpgadmin reset <player>` | `controlserver` | Put a player back to level 1. |

## Files

They load in name order and share one interpreter, so the numeric prefixes are the
load order. Each file declares the module table it needs, so none depends on another
having run first.

| File | What it owns |
| --- | --- |
| `01-config.lua` | Every tunable: the curve, what each creature is worth, what each reward tier holds. The only file an operator normally edits. |
| `02-progress.lua` | The arithmetic of a standing, and where one is stored. |
| `03-rewards.lua` | Choosing a reward and making sure it reaches the player. |
| `04-levelling.lua` | The single owner of "somebody earned experience": stores, announces, rewards. |
| `05-kills.lua` | Pricing a death and listening for one. |
| `06-commands.lua` | The two commands. |

## The curve

Going from one level to the next costs `xpBase * level ^ xpExponent`, rounded down.
At the shipped settings that is 15 experience for level 2, 237 for level 11, 888 for
level 31, and about 55,000 for the whole climb to the cap of 60.

## Pricing a creature

`config.killXp` is read top to bottom and the first match wins, so a rule for one
variant belongs above the rule for its family. A rule matches when the entity code
starts with `prefix` and, where one is given, holds `contains` somewhere in it — which
is how `game:wolf-eurasian-baby-male` is told apart from its parent. Codes are compared
as plain text, so a hyphen is a hyphen rather than a pattern quantifier.

Anything no rule names is worth nothing, which keeps straw dummies, boats and traders
off the ledger. The first time an unpriced creature is killed by a player its code is
written to the server log, so a creature added by another mod can be priced by adding
a rule for it.

Creative and spectator kills earn nothing.

## Who gets credited for a kill

MoonTweaks 0.28.0 takes the `byPlayer` on an `entityDeath` event from the damage's
`CauseEntity`. Vintage Story fills that field in for a projectile alone — the entity
that threw the arrow — and documents that it "will be null for non-projectile damage
e.g. melee attacks", naming the attacker in `SourceEntity` instead and offering
`GetCauseEntity()` as the accessor that answers for both. A kill made by hand therefore
reaches Lua with nobody named.

`kills.killerOf` is where that is dealt with, in one place:

1. The player the event names, where it names one. A bow or a thrown spear lands here
   and is attributed exactly.
2. Otherwise, where the damage was a blow (`bluntattack`, `slashingattack` or
   `piercingattack`), the nearest player within `config.meleeCreditRange` blocks of
   where it fell. A death by falling, drowning or cold names nobody and stays nobody's.

Setting `meleeCreditRange` to 0 turns the guess off and credits projectile kills alone.
When MoonTweaks reads `GetCauseEntity()`, step 1 will answer every time and step 2 will
simply stop being reached — nothing here needs changing for that, though it can then be
deleted.

## Where a standing lives

Against the player, in this world's save game (`players.setWorldData`). A second world
on this server keeps its own levels, and deleting a world takes them with it.

## Loading it

Scripts run once, as the server starts, so a restart is what puts a change in play.
`/moontweaks check` re-runs every script and reports what it would do without changing
anything, which is the cheap way to find a mistake before restarting.
