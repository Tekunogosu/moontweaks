#!/bin/sh
# Generate the scripting API reference from the mod's bindings.
#   --check   report undocumented members and fail, writing nothing
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
OUT="$ROOT/docs"
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$ROOT/modinfo.json")

dotnet build "$ROOT/tools/docgen/docgen.csproj" -c Release --nologo -v q
dotnet "$ROOT/tools/docgen/bin/Release/moontweaks-docgen.dll" \
    "$ROOT/bin/Release/moontweaks.dll" \
    "$ROOT/bin/Release/moontweaks.xml" \
    "$OUT" "$VERSION" "$@"

# Check mode writes nothing, so there is nothing to scaffold from.
case " $* " in *" --check "*) exit 0 ;; esac

# examples/ is a MoonTweaks folder like any other, so give it exactly what the mod
# writes into a server's own: the same editor files, and the same library.
mkdir -p "$ROOT/examples/library" "$ROOT/examples/.vscode"
cp "$ROOT/src/Host/Resources/luarc.json" "$ROOT/examples/.luarc.json"
cp "$ROOT/src/Host/Resources/vscode-extensions.json" "$ROOT/examples/.vscode/extensions.json"
cp "$OUT/library/moontweaks.lua" "$ROOT/examples/library/moontweaks.lua"
cp "$OUT/library/codes.lua" "$ROOT/examples/library/codes.lua"
echo "  examples/.luarc.json"
echo "  examples/.vscode/extensions.json"
echo "  examples/library/moontweaks.lua"
echo "  examples/library/codes.lua"

# The examples are shipped in the mod and scaffolded against the library just
# written, so they are checked against the types this build actually produces.
# CI runs this and fails on it; here it runs when the tool is installed and says
# so when it is not, rather than passing quietly either way.
if command -v lua-language-server >/dev/null 2>&1; then
    echo
    lua-language-server --check "$ROOT/examples" --checklevel Warning
else
    echo
    echo "lua-language-server is not installed, so the examples were not checked here."
    echo "CI checks them on every push; install it to get the same answer locally."
fi
