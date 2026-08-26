#!/bin/sh
# Generate the scripting API reference from the mod's bindings.
#   --check   report undocumented members and fail, writing nothing
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
OUT="$ROOT/docs"
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT/modinfo.json")

dotnet build "$ROOT/tools/docgen/docgen.csproj" -c Release --nologo -v q
exec dotnet "$ROOT/tools/docgen/bin/Release/moontweaks-docgen.dll" \
    "$ROOT/bin/Release/moontweaks.dll" \
    "$ROOT/bin/Release/moontweaks.xml" \
    "$OUT" "$VERSION" "$@"
