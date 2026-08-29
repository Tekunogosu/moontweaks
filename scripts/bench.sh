#!/bin/sh
# Measure the script engines against each other, outside the game.
#
# Builds the mod first, because the benchmark loads the same assembly a server
# would rather than a copy compiled for the occasion.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)

dotnet build "$ROOT/moontweaks.csproj" -c Release --nologo -v q
exec dotnet run -c Release --project "$ROOT/tools/luabench/luabench.csproj" -- "$@"
