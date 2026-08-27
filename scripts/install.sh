#!/bin/sh
# Build moontweaks and install it into a server's Mods folder, replacing whatever
# version is already there.
#
#   usage: install.sh [dataPath]      (default <repo>/.testbed)
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
DATA=${1:-$ROOT/.testbed}
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT/modinfo.json")

"$ROOT/scripts/package.sh" Release >/dev/null
mkdir -p "$DATA/Mods"
rm -f "$DATA/Mods"/moontweaks-*.zip
cp "$ROOT/bin/Release/moontweaks-$VERSION.zip" "$DATA/Mods/"
echo "installed moontweaks-$VERSION into $DATA/Mods"
