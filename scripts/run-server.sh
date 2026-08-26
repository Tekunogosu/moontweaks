#!/bin/sh
# Build, install and run moontweaks on a dedicated server with an isolated data
# path, leaving the real install untouched. Runs in the foreground.
#
#   VS_SERVER            dedicated server install (default /mnt/media/vintagestory-server)
#   MOONTWEAKS_TESTBED   server data path (default <repo>/.testbed)
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
SERVER=${VS_SERVER:-/mnt/media/vintagestory-server}
DATA=${MOONTWEAKS_TESTBED:-$ROOT/.testbed}
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT/modinfo.json")

"$ROOT/scripts/package.sh" Release >/dev/null
mkdir -p "$DATA/Mods"
rm -f "$DATA/Mods"/moontweaks-*.zip
cp "$ROOT/bin/Release/moontweaks-$VERSION.zip" "$DATA/Mods/"
echo "installed moontweaks-$VERSION into $DATA/Mods"

cd "$SERVER"
exec dotnet VintagestoryServer.dll --dataPath "$DATA"
