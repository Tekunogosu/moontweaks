#!/bin/sh
# Build moontweaks and emit the distributable mod zip.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
CONFIG=${1:-Release}
OUT="$ROOT/bin/$CONFIG"
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT/modinfo.json")

# Two passes: the first produces the assembly the reference generator reflects
# over, the second embeds the library it wrote. The generated library depends on
# the bindings and not on itself, so the second pass is a fixed point.
dotnet build "$ROOT/moontweaks.csproj" -c "$CONFIG" --nologo -v q
"$ROOT/scripts/docs.sh" >/dev/null
dotnet build "$ROOT/moontweaks.csproj" -c "$CONFIG" --nologo -v q

ZIP="$OUT/moontweaks-$VERSION.zip"
rm -f "$ZIP"
zip -qj "$ZIP" "$OUT/moontweaks.dll" "$ROOT/modinfo.json"
echo "$ZIP ($(du -k "$ZIP" | cut -f1) KiB)"
