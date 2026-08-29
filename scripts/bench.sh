#!/bin/sh
# Measure the interpreter MoonTweaks runs scripts on, and record what it makes of
# the Lua the checks put through it.
#
#   usage: bench.sh [--engine NAME]... [--quick] [--json]
#
# Needs no running server: the workload reaches an engine only through IScriptHost,
# and the game is not part of that.
#
# One engine is registered, so the checks are recorded rather than compared. Register
# a candidate beside it and this becomes the comparison that decides whether to swap:
# it exits non-zero when two engines read the same Lua differently, whatever the
# timings say.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)

# Build chatter goes to stderr so that --json leaves stdout parseable.
dotnet build "$ROOT/tools/luabench/luabench.csproj" -c Release --nologo -v q >&2
exec dotnet "$ROOT/tools/luabench/bin/Release/moontweaks-luabench.dll" "$@"
