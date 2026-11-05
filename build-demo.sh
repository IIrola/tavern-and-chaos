#!/bin/zsh
set -euo pipefail
PROJECT_DIR="${0:A:h}"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity}"
if [[ -f "$PROJECT_DIR/TavernChaos/Temp/UnityLockfile" ]] && /usr/sbin/lsof -t "$PROJECT_DIR/TavernChaos/Temp/UnityLockfile" >/dev/null 2>&1; then
    print -u2 'Cierra el editor de este proyecto antes de compilar desde este script.'
    exit 1
fi
# Only generated script metadata is regenerated. Keep an inspectable backup outside Assets.
# Numbered duplicate JSON files in this cache were being merged as duplicate type declarations.
if [[ -d "$PROJECT_DIR/TavernChaos/Library/BuildPlayerData" ]]; then
    mkdir -p "$PROJECT_DIR/.validation"
    TAVERN_BUILD_CACHE_BACKUP="$(mktemp -d "$PROJECT_DIR/.validation/script-data.XXXXXX")"
    mv "$PROJECT_DIR/TavernChaos/Library/BuildPlayerData" "$TAVERN_BUILD_CACHE_BACKUP/BuildPlayerData"
fi
mkdir -p "$PROJECT_DIR/Evidence"
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PROJECT_DIR/TavernChaos" -buildTarget osx -executeMethod TavernChaos.Editor.DemoBuild.BuildMac -logFile "$PROJECT_DIR/Evidence/unity-build.log"
