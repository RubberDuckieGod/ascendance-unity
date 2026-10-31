#!/usr/bin/env bash
set -euo pipefail

# EnvironmentFile is installed separately from releases by release.py.
: "${ASCENDANCE_ROOT:?}" "${ASCENDANCE_DATA_HOME:?}" "${ASCENDANCE_LOG_DIR:?}"
: "${ASCENDANCE_ROOM:?}" "${ASCENDANCE_PORT:?}"
release="$(readlink -f -- "$ASCENDANCE_ROOT/current")"
case "$release" in
  "$ASCENDANCE_ROOT"/releases/*/build) ;;
  *) echo 'Refusing a release outside the managed releases directory.' >&2; exit 1 ;;
esac
binary="$release/AscendanceServer.x86_64"
[[ -f "$binary" && ! -L "$binary" && -x "$binary" ]] || exit 1
export HOME="$ASCENDANCE_DATA_HOME"
export XDG_CONFIG_HOME="$ASCENDANCE_DATA_HOME/.config"
cd -- "$ASCENDANCE_DATA_HOME"
exec "$binary" -batchmode -nographics -ascendance-server \
  -room "$ASCENDANCE_ROOM" -port "$ASCENDANCE_PORT" \
  -logFile "$ASCENDANCE_LOG_DIR/server.log"
