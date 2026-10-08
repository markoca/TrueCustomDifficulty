#!/usr/bin/env bash
set -Eeuo pipefail

# Build AND launch the current main-branch application from one known location.
# Stop rather than accidentally reusing a pre-existing Wine window.
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

if pgrep -af '[T]rueCustomDifficulty\.exe' >/dev/null 2>&1; then
    echo "ERROR: TrueCustomDifficulty.exe is already running." >&2
    echo "Close the old window/process, then rerun ./run-latest.sh." >&2
    pgrep -af '[T]rueCustomDifficulty\.exe' >&2 || true
    exit 1
fi

git pull --ff-only origin main
bash "$ROOT/publish.sh"

RECEIPT="$ROOT/publish/win-x64/BUILD-INFO.txt"
EXE="$ROOT/publish/win-x64/TrueCustomDifficulty.exe"
[[ -f "$EXE" && -f "$RECEIPT" ]] || {
    echo "ERROR: Fresh executable or build receipt missing." >&2
    exit 1
}
HEAD="$(git rev-parse --short HEAD)"
SOURCE="$(sed -n 's/^source_commit=//p' "$RECEIPT")"
EXPECTED_SHA="$(sed -n 's/^exe_sha256=//p' "$RECEIPT")"
ACTUAL_SHA="$(sha256sum "$EXE" | awk '{print $1}')"

if [[ "$SOURCE" != "$HEAD" || "$EXPECTED_SHA" != "$ACTUAL_SHA" ]]; then
    echo "ERROR: Build source/EXE fingerprint mismatch. Refusing stale launch." >&2
    echo "Git HEAD=$HEAD; receipt=$SOURCE" >&2
    exit 1
fi

echo "Launching validated True Custom Difficulty from commit $SOURCE"
echo "Exe SHA-256: $ACTUAL_SHA"
exec env WINEPREFIX="${WINEPREFIX:-$HOME/.wine-s2}" WINEDEBUG=-all wine "$EXE"
