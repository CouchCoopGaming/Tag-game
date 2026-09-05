#!/usr/bin/env bash
# Build Windows standalone via Unity batchmode when UNITY_PATH is set.
# Editor menu: Tag → Build Windows Standalone (preferred on Landon's machine).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/Builds/Windows"
UNITY="${UNITY_PATH:-}"
if [[ -z "$UNITY" ]]; then
  echo "Set UNITY_PATH to Unity 6000.4.2f1 Editor binary, or use Tag → Build Windows Standalone in Editor."
  exit 1
fi
mkdir -p "$OUT"
"$UNITY" -quit -batchmode -projectPath "$ROOT" -executeMethod Tag.EditorTools.BuildWindows.Build -logFile -
echo "Built → $OUT/Tag.exe"
