#!/bin/sh
# Build the XLib plugin and emit its mod zip, copying it into each given directory.
#
#   usage: build.sh [directory...]
#
#   XLIB_ZIP   the XLib mod zip to build against
#              (default: the newest XLib_Fork zip in the client's Mods folder)
#
# The zip holds the plugin's DLL, its XML documentation and its mod info, and
# nothing else: moontweaks.dll and xlib.dll are resolved by the game from the mods
# it has already loaded, and a second copy of either would break their loading.
set -eu

HERE=$(cd "$(dirname "$0")" && pwd)
VERSION=$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$HERE/modinfo.json")
MODS=${VS_CLIENT_DATA:-$HOME/.config/VintagestoryData}/Mods
XLIB_ZIP=${XLIB_ZIP:-$(ls "$MODS"/XLib_Fork_v*.zip 2>/dev/null | sort -V | tail -n 1)}

if [ -z "$XLIB_ZIP" ] || [ ! -f "$XLIB_ZIP" ]; then
    echo "no XLib zip found in $MODS; set XLIB_ZIP to one" >&2
    exit 1
fi

mkdir -p "$HERE/obj/xlib"
unzip -qo "$XLIB_ZIP" xlib.dll -d "$HERE/obj/xlib"

dotnet build "$HERE/moontweaks-xlib.csproj" -c Release --nologo -v q

OUT="$HERE/bin/Release"
ZIP="$OUT/moontweaks-xlib-$VERSION.zip"
rm -f "$ZIP"
zip -qj "$ZIP" "$OUT/moontweaks-xlib.dll" "$OUT/moontweaks-xlib.xml" "$HERE/modinfo.json"
echo "$ZIP ($(du -k "$ZIP" | cut -f1) KiB)"

for dest in "$@"; do
    mkdir -p "$dest"
    rm -f "$dest"/moontweaks-xlib-*.zip
    cp "$ZIP" "$dest/"
    echo "installed moontweaks-xlib-$VERSION into $dest"
done
