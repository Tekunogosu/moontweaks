#!/bin/sh
# Build moontweaks and emit the distributable mod zip.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
CONFIG=${1:-Release}
OUT="$ROOT/bin/$CONFIG"
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT/modinfo.json")

dotnet build "$ROOT/moontweaks.csproj" -c "$CONFIG" --nologo -v q

ZIP="$OUT/moontweaks-$VERSION.zip"
rm -f "$ZIP"
zip -qj "$ZIP" "$OUT/moontweaks.dll" "$ROOT/modinfo.json"
echo "$ZIP ($(du -k "$ZIP" | cut -f1) KiB)"
