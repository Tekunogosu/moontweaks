#!/bin/sh
# Launch the game client against the testbed server with moontweaks as its only
# mod. The client runs on a throwaway data path so that neither the real Mods
# folder nor the extra mod paths configured in clientsettings.json are loaded.
#
#   VINTAGE_STORY        client install (default ~/.local/share/vintagestory)
#   VINTAGE_STORY_DATA   real client data path, read once for the login session
#   MOONTWEAKS_TESTBED   server data path, whose Mods folder is shared (default <repo>/.testbed)
#   MOONTWEAKS_CLIENT    throwaway client data path (default /tmp/moontweaks-client)
#   MOONTWEAKS_ADDRESS   server to connect to (default 127.0.0.1:42420)
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
CLIENT=${VINTAGE_STORY:-$HOME/.local/share/vintagestory}
REAL=${VINTAGE_STORY_DATA:-$HOME/.config/VintagestoryData}
TESTBED=${MOONTWEAKS_TESTBED:-$ROOT/.testbed}
DATA=${MOONTWEAKS_CLIENT:-/tmp/moontweaks-client}
ADDRESS=${MOONTWEAKS_ADDRESS:-127.0.0.1:42420}

# Mode 700: the copied settings carry the account's session key and signature.
mkdir -p -m 700 "$DATA"
chmod 700 "$DATA"

# Multiplayer needs a logged-in session, which lives in clientsettings.json.
# modPaths keeps only "Mods" (the install folder holding the base game mods);
# the real data path's Mods folder is dropped so no user mods load.
jq '.stringListSettings.modPaths = ["Mods"] | .stringListSettings.disabledMods = []' \
    "$REAL/clientsettings.json" > "$DATA/clientsettings.json"
chmod 600 "$DATA/clientsettings.json"
echo "session copied from $REAL/clientsettings.json, user mod paths dropped"

echo "client data path $DATA, mods from $TESTBED/Mods, connecting to $ADDRESS"
cd "$CLIENT"
exec ./Vintagestory --dataPath "$DATA" --connect "$ADDRESS" --addModPath "$TESTBED/Mods"
