#!/bin/zsh
set -euo pipefail
PROJECT_DIR="${0:A:h}"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$PROJECT_DIR/Evidence"
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PROJECT_DIR/TavernChaos" -buildTarget osx -executeMethod TavernChaos.Editor.DemoBuild.BuildMac -logFile "$PROJECT_DIR/Evidence/unity-build.log"
