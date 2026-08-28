#!/bin/sh
# Fetch MoonSharp at the pinned commit, ready for compilation into moontweaks.dll.
# Idempotent: the forced checkout discards whatever a previous run or a local
# experiment left behind.
#
# MoonSharp is vendored as source rather than consumed from NuGet because the
# published package (3.0.0-beta.1) predates upstream fixes this mod depends on.
# The pin is a commit on the moontweaks branch of the fork, which carries those
# fixes and the nullable context the sources are compiled under.
set -eu

PIN=6716ae4af1a80a9c33c3ac39774d7164741434da
ROOT=$(cd "$(dirname "$0")/.." && pwd)
SUB="$ROOT/third_party/moonsharp"

git -C "$ROOT" submodule update --init --quiet third_party/moonsharp
git -C "$SUB" fetch --quiet origin
git -C "$SUB" checkout --quiet --force "$PIN"
echo "moonsharp pinned to $(git -C "$SUB" log -1 --format='%h %cd %s' --date=short)"
