#!/usr/bin/env bash
# Launch a licensed local Unity Editor directly. The asynchronous bridge owns -quit.
set -euo pipefail

project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
editor_path="${UNITY_EDITOR_PATH:-}"
artifacts_dir=""
validation_timeout=1800
build_linux=false
build_windows=false

usage() {
    cat <<'USAGE'
Usage: tools/run_unity_validation.sh --editor /absolute/path/to/Unity [options]
  --artifacts PATH       New output directory (must not already exist)
  --timeout SECONDS      Runtime deadline, 60-7200 (default: 1800)
  --build-linux-server   Build Linux Dedicated Server after all runtime gates pass
  --build-windows        Build Windows player after all runtime gates pass
UNITY_EDITOR_PATH can replace --editor. Requires a licensed Unity 6000.6.5f1,
Python 3, and GNU timeout. This script never installs or activates Unity.
USAGE
}

while (($#)); do
    case "$1" in
        --editor|--artifacts|--timeout)
            if (($# < 2)); then usage >&2; exit 2; fi
            case "$1" in
                --editor) editor_path="$2" ;;
                --artifacts) artifacts_dir="$2" ;;
                --timeout) validation_timeout="$2" ;;
            esac
            shift 2 ;;
        --build-linux-server) build_linux=true; shift ;;
        --build-windows) build_windows=true; shift ;;
        --help|-h) usage; exit 0 ;;
        *) printf 'Unknown option: %s\n' "$1" >&2; usage >&2; exit 2 ;;
    esac
done

if [[ -z "$editor_path" || "$editor_path" != /* || ! -x "$editor_path" ]]; then
    printf 'Set --editor or UNITY_EDITOR_PATH to the absolute executable path of licensed Unity 6000.6.5f1.\n' >&2
    exit 2
fi
if [[ ! "$validation_timeout" =~ ^[0-9]+$ ]] || ((validation_timeout < 60 || validation_timeout > 7200)); then
    printf 'Runtime timeout must be between 60 and 7200 seconds.\n' >&2
    exit 2
fi
command -v python3 >/dev/null || { printf 'Python 3 is required to verify runtime results.\n' >&2; exit 2; }
command -v timeout >/dev/null || { printf 'GNU timeout is required (on macOS install coreutils and expose gtimeout as timeout).\n' >&2; exit 2; }
required_version="$(sed -n 's/^m_EditorVersion: //p' "$project_dir/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
if [[ "$required_version" != '6000.6.5f1' ]]; then
    printf 'Expected ProjectVersion.txt to pin 6000.6.5f1, found %s.\n' "$required_version" >&2
    exit 2
fi
if [[ -z "$artifacts_dir" ]]; then
    mkdir -p "$project_dir/artifacts/unity-validation"
    artifacts_dir="$(mktemp -d "$project_dir/artifacts/unity-validation/run-$(date -u +%Y%m%dT%H%M%SZ)-XXXXXX")"
else
    mkdir -p -- "$(dirname -- "$artifacts_dir")"
    mkdir -- "$artifacts_dir"  # Existing results must never satisfy a new invocation.
fi
artifacts_dir="$(cd -- "$artifacts_dir" && pwd)"
printf 'Unity validation artifacts: %s\n' "$artifacts_dir"
if command -v git >/dev/null && git -C "$project_dir" rev-parse --verify HEAD >"$artifacts_dir/source-revision.txt" 2>/dev/null; then
    git -C "$project_dir" status --porcelain >"$artifacts_dir/source-worktree-status.txt"
else
    printf 'Git revision unavailable: record the source archive and its checksum.\n' >"$artifacts_dir/source-revision.txt"
fi
timeout --signal=TERM --kill-after=15s 60s "$editor_path" -version >"$artifacts_dir/editor-version.txt" 2>&1
if ! python3 - "$artifacts_dir/editor-version.txt" <<'PY'
import re
import sys
from pathlib import Path
text = Path(sys.argv[1]).read_text(encoding="utf-8", errors="replace")
raise SystemExit(0 if re.search(r"(?m)^6000\.6\.5f1(?:\s|$)", text) else 1)
PY
then
    printf 'Editor version check failed; see %s/editor-version.txt.\n' "$artifacts_dir" >&2
    exit 1
fi

mkdir "$artifacts_dir/game-data"
printf '%s\n' 'ascendance-unity-validation-0.10.2h' >"$artifacts_dir/game-data/.ascendance-validation-data"
export ASCENDANCE_TEST_MODE=1
export ASCENDANCE_TEST_DATA_DIR="$artifacts_dir/game-data"
# Keep HOME, XDG_CONFIG_HOME, and existing Unity licensing/authentication untouched.
cd -- "$project_dir"
if timeout --signal=TERM --kill-after=15s "$((validation_timeout + 90))s" "$editor_path" \
    -batchmode -nographics -projectPath "$project_dir" \
    -executeMethod Ascendance.EditorTools.AscendanceBatchValidation0102.Run \
    -ascendance-results "$artifacts_dir/validation-results.json" \
    -ascendance-timeout "$validation_timeout" -logFile "$artifacts_dir/validation.log"; then
    python3 "$project_dir/tools/verify_unity_validation.py" "$artifacts_dir/validation-results.json"
else
    status=$?
    printf 'Unity runtime validation failed (exit %s); inspect %s.\n' "$status" "$artifacts_dir/validation.log" >&2
    exit "$status"
fi

build() {
    local target="$1" method="$2" output="$3" label="$4"
    printf 'Building %s after successful runtime validation.\n' "$label"
    timeout --signal=TERM --kill-after=15s 3600s "$editor_path" \
        -batchmode -nographics -quit -projectPath "$project_dir" -buildTarget "$target" \
        -executeMethod "$method" -buildOutput "$output" -logFile "$artifacts_dir/build-$label.log"
    if [[ ! -s "$output" || ! -d "${output%.*}_Data" ]]; then
        printf 'Expected %s build executable and data directory are missing.\n' "$label" >&2
        exit 1
    fi
}
if $build_linux; then
    build Linux64 Ascendance.EditorTools.AscendanceDedicatedServerBuilder0100.BuildLinuxDedicatedServer0102 \
        "$artifacts_dir/LinuxServer/AscendanceServer.x86_64" linux-server
fi
if $build_windows; then
    build Win64 Ascendance.EditorTools.AscendanceStandaloneBuilder.BuildWindowsStandalone \
        "$artifacts_dir/Windows/Ascendance.exe" windows-player
fi
