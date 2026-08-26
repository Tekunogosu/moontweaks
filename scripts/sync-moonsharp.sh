#!/bin/sh
# Fetch MoonSharp at the pinned commit and prepare its sources for compilation
# into moontweaks.dll. Idempotent: the forced checkout discards any previous run.
#
# MoonSharp is vendored as source rather than consumed from NuGet because the
# published package (3.0.0-beta.1) predates upstream fixes this mod depends on.
set -eu

PIN=cb4a978093bae3fd7b0b331643a8cd9b6fb8ed16
ROOT=$(cd "$(dirname "$0")/.." && pwd)
SUB="$ROOT/third_party/moonsharp"
SRC="$SUB/src/MoonSharp.Interpreter"

git -C "$ROOT" submodule update --init --quiet third_party/moonsharp
git -C "$SUB" fetch --quiet origin
git -C "$SUB" checkout --quiet --force "$PIN"
echo "moonsharp pinned to $(git -C "$SUB" log -1 --format='%h %cd %s' --date=short)"

for patch in "$ROOT"/patches/*.patch; do
    git -C "$SUB" apply "$patch"
    echo "applied $(basename "$patch")"
done

# Vendored sources predate nullable reference types; opting each file out keeps
# moontweaks' own code under a strict nullable context without 775 warnings.
python3 - "$SRC" <<'PY'
import pathlib, sys
n = 0
for f in pathlib.Path(sys.argv[1]).rglob("*.cs"):
    text = f.read_text(encoding="utf-8-sig")
    f.write_text("#nullable disable\n" + text, encoding="utf-8")
    n += 1
print(f"nullable context disabled for {n} vendored sources")
PY
