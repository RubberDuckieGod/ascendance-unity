#!/usr/bin/env bash
set -euo pipefail
UNITY_PATH="${UNITY_PATH:-/opt/unity/Editor/Unity}"
ROOT="$(cd "$(dirname "$0")" && pwd)"
mkdir -p "$ROOT/Builds/LinuxServer"
"$UNITY_PATH" -batchmode -quit -projectPath "$ROOT" -executeMethod Ascendance.EditorTools.AscendanceDedicatedServerBuilder0100.BuildLinuxDedicatedServer0102 -logFile "$ROOT/Builds/LinuxServer/build.log"
