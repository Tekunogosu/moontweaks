#!/bin/sh
# Build moontweaks and copy the zip into each given directory, replacing whatever
# version of the mod is already there.
#
#   usage: install.sh [directory...]     (default <repo>/.testbed/Mods)
#
# The directory is taken as given rather than treated as a data path to append to,
# so a Vintage Story install names its own Mods folder and anywhere else works the
# same way.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT/modinfo.json")

[ "$#" -gt 0 ] || set -- "$ROOT/.testbed/Mods"

# Packaged once, however many places it is going.
"$ROOT/scripts/package.sh" Release >/dev/null

for dest in "$@"; do
    mkdir -p "$dest"
    rm -f "$dest"/moontweaks-[0-9]*.zip
    cp "$ROOT/bin/Release/moontweaks-$VERSION.zip" "$dest/"
    echo "installed moontweaks-$VERSION into $dest"
done
