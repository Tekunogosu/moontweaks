#!/bin/sh
# Check the shipped examples against the generated type library.
#
# The examples are embedded in the mod and scaffolded from whatever the last run of
# scripts/docs.sh wrote, so this checks them against the types the current build
# produces rather than a checked-in copy. Run scripts/docs.sh first; this reads what
# that leaves behind and generates nothing itself.
#
# Exits non-zero on any diagnostic at Warning or above, and on the checker being
# absent, so a run that checked nothing is never mistaken for a run that passed.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)

if ! command -v lua-language-server >/dev/null 2>&1; then
    echo "lua-language-server is not installed, so the examples cannot be checked." >&2
    echo "Install it from https://github.com/LuaLS/lua-language-server/releases." >&2
    # An editor that installed it already has one, and it is the same binary. Mason is
    # where Neovim puts it, and it is not on PATH by default.
    if [ -x "$HOME/.local/share/nvim/mason/bin/lua-language-server" ]; then
        echo "One is already installed for Neovim. Run this with it on PATH:" >&2
        echo "  PATH=\"\$HOME/.local/share/nvim/mason/bin:\$PATH\" $0" >&2
    fi
    exit 1
fi

if [ ! -f "$ROOT/examples/library/moontweaks.lua" ]; then
    echo "examples/library/moontweaks.lua is missing: run scripts/docs.sh first." >&2
    exit 1
fi

lua-language-server --check "$ROOT/examples" --checklevel Warning
