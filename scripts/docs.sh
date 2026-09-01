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
    "$OUT" "$VERSION" "$ROOT/third_party/highlight.js" "$@"

# Check mode writes nothing, so there is nothing to scaffold from.
case " $* " in *" --check "*) exit 0 ;; esac

# examples/ is a MoonTweaks folder like any other, so give it exactly what the mod
# writes into a server's own: the same editor files, and the same library.
mkdir -p "$ROOT/examples/library" "$ROOT/examples/.vscode"
cp "$ROOT/src/Host/Resources/luarc.json" "$ROOT/examples/.luarc.json"
cp "$ROOT/src/Host/Resources/vscode-extensions.json" "$ROOT/examples/.vscode/extensions.json"
cp "$OUT/library/moontweaks.lua" "$ROOT/examples/library/moontweaks.lua"
cp "$OUT/library/codes.lua" "$ROOT/examples/library/codes.lua"
# The module examples go where the check below reaches them, which is what keeps a
# snippet on the reference page from drifting out of step with the bindings it shows.
mkdir -p "$ROOT/examples/snippets"
cp "$OUT/snippets.lua" "$ROOT/examples/snippets/modules.lua"
# The diagnostics suite measures its coverage against a checklist of every bound
# function, and the checklist is generated for the same reason the snippets are: one
# left to drift reports full coverage of the functions it happens to list and says
# nothing about the ones it does not.
cp "$OUT/surface.lua" "$ROOT/examples/scripts/diagnostics/01-surface.lua"
echo "  examples/.luarc.json"
echo "  examples/.vscode/extensions.json"
echo "  examples/library/moontweaks.lua"
echo "  examples/library/codes.lua"
echo "  examples/snippets/modules.lua"
echo "  examples/scripts/diagnostics/01-surface.lua"

# Checking what was just scaffolded belongs to scripts/check-examples.sh, which is a
# program of its own so that generating the reference and judging it are separately
# invokable: scripts/package.sh generates as a build step and must not fail on a
# diagnostic in an example, and CI runs the check as a step that reports its own
# findings rather than one buried inside a build.
echo
echo "Run scripts/check-examples.sh to check them against the types just written."
