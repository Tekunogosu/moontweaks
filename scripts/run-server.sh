#!/bin/sh
# Build, install and run moontweaks on a dedicated server with an isolated data
# path, leaving the real install untouched. Runs in the foreground.
#
#   VS_SERVER            dedicated server install (default /mnt/media/vintagestory-server)
#   MOONTWEAKS_TESTBED   server data path (default <repo>/.testbed)
#   MOONTWEAKS_PORT      port to listen on (default 42460)
#
# The port is off the game's default of 42420 so this never contends with a real
# server on the same machine, and is passed rather than written to the testbed's
# config so a regenerated testbed still lands somewhere harmless.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
SERVER=${VS_SERVER:-/mnt/media/vintagestory-server}
DATA=${MOONTWEAKS_TESTBED:-$ROOT/.testbed}
PORT=${MOONTWEAKS_PORT:-42460}

"$ROOT/scripts/install.sh" "$DATA/Mods"

cd "$SERVER"
exec dotnet VintagestoryServer.dll --dataPath "$DATA" --withconfig "{ Port: $PORT }"
