#!/bin/sh
# Build moontweaks and emit the distributable mod zip.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
CONFIG=${1:-Release}
OUT="$ROOT/bin/$CONFIG"
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT/modinfo.json")

dotnet build "$ROOT/moontweaks.csproj" -c "$CONFIG" --nologo -v q

# Lua-CSharp travels beside the mod as Lua.dll, so the zip is a binary
# redistribution of it. Its licence requires the notice to travel with the
# distribution, which is what THIRD-PARTY-NOTICES.md is doing here.
#
# The XML documentation travels too: a server renders the editor's type library
# from the loaded assembly at startup, and the summaries come from this file.
ZIP="$OUT/moontweaks-$VERSION.zip"
rm -f "$ZIP"
zip -qj "$ZIP" "$OUT/moontweaks.dll" "$OUT/moontweaks.xml" "$OUT/Lua.dll" "$ROOT/modinfo.json" \
    "$ROOT/LICENSE" "$ROOT/THIRD-PARTY-NOTICES.md"
echo "$ZIP ($(du -k "$ZIP" | cut -f1) KiB)"
