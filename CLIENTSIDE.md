# Client-side scripting

A proposal, not decided work. `TODO.md` holds what is decided and `COVERAGE.md`
holds the survey it is decided from; this holds one design that has been thought
through but not agreed. When it is agreed, what survives moves into `TODO.md` as
tasks and this file goes away.

## What the server cannot reach

This mod runs on the server, so a script reaches only what a server-side mod
reaches. Whole categories are absent for that reason rather than because nobody
has bound them: dialogs, HUD elements, hotkeys, client particles and sounds,
anything drawn or pressed. A server owner who wants a custom window has no route
to one, however much Lua they are willing to write.

The route proposed here is to let the server carry the client's code. A server
owner writes client scripts into their own MoonTweaks folder; a joining player
running the client half of this mod is handed those scripts over the mod's
network channel and runs them locally. The player installs one mod, once, and
every server they visit supplies its own behaviour.

## Delivery needs no restart

The payload is Lua source and the interpreter is already resident in the client
process. Nothing is added to the game's `ModLoader`, so there is no assembly to
load and no restart to earn. The client builds a fresh `IScriptHost` against the
client bindings and runs what it received, the same way `ScriptRun.Execute` runs
a server's scripts today.

A restart would not help the one case where the timing genuinely hurts, which is
the next section.

## Behaviour, not content

The client's lifecycle divides what a delivered script can do, and the division
is firm.

Runtime behaviour is reachable. Dialogs, HUD elements, chat handling, client
particles and sounds, and input are all registered after the world exists, which
is later than the connection that delivered the code.

Content is not. Blocks, items, entities, shapes, textures and language entries
are fixed during asset load, long before a server has been chosen. No delivery
mechanism moves that boundary: code arriving at connection time arrives after
it, on the first join and on every join after. A prompt telling the player to
restart so their content loads would be a promise the design cannot keep.

Registries the game already synchronises, recipes among them, need none of this.
`ClientSyncProbe` measures that path.

## The sandbox is what makes this defensible

Joining a server would mean running that server's code on the player's machine.
Two properties keep that reasonable, and both are held deliberately rather than
by accident.

The interpreter is narrow. `LuaCSharpHost` opens the basic, string, table, math
and bitwise libraries and nothing else: no `io`, no `os`, no foreign function
interface, no path to native code. A delivered script reaches exactly the
bindings the client half exposes and nothing beyond them. This is a far stronger
position than shipping an assembly, and it is the reason the proposal is worth
considering at all.

The client's bindings are therefore a separate allow-list, not the server's list
behind a side flag. Nothing that touches the filesystem outside a per-server
scoped folder, launches a process, opens a socket or reads the player's session.
Sharing one binder between the two sides means a single careless `[LuaField]` on
a server type becomes remote code on every player who joins.

Two hazards remain past the sandbox. A loop that does not end reads as a frozen
game rather than a slow server, so client callbacks run under a cancellation
token and a per-callback budget. And trust is the player's to give: a first
connection to a server offering scripts asks, naming the server and what it
sent, against a client setting of never, ask or always.

## Delivery is content-addressed

The server sends a manifest first, naming each script with its hash and size.
The client answers with the hashes it does not hold, and only those are sent,
compressed. What arrives is cached by hash under the client's own config folder,
per server. A reconnection to a server whose scripts have not changed transfers
nothing and writes nothing.

The channel is registered identically on both sides, and scripts are sent once
the player is in the world rather than at an earlier connection phase.

## The message channel is half the feature

A dialog that cannot ask the server anything is decoration. Delivery is worth
building only alongside a script-visible channel carrying messages both ways,
with the server treating everything a client sends as hostile input. The two
should be designed together and released together.

## The client half stays optional

`requiredonclient` is false and stays false, so a server's scripts must degrade
for players who have nothing installed. That needs a capability handshake: the
client announces itself with an integer API level, the server exposes to scripts
whether a given player has it, and the server declines to send a script that
declares a level the joining client cannot serve, saying so plainly rather than
failing inside the script.

## Open questions, nearest first

**Folder shape.** `ScriptLibrary.SCRIPTS_FOLDER` is flat. Splitting it into
`scripts/server` and `scripts/client` reads better than adding a sibling folder
beside it, and the cost of moving it is only ever going to rise.

**Handshake contents.** Beyond the API level, what a server script should be
able to ask about a joining client, and what it must not be told.

**Hotkey registration timing.** Registering a hotkey outside the client's own
startup is assumed to work here and has not been checked. Whether a late
registration appears correctly in the game's own controls list decides whether
hotkeys are part of the first client surface or held back.

**Which bindings the client gets first.** Dialogs, HUD, input and drawing are
four surfaces of quite different sizes, and the first release does not need all
four. This is the real design work; everything above it is mechanism.
