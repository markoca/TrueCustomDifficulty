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
STASH_TIER_SOURCE="$ROOT/src/TrueCustomDifficulty/Controls/AggregateVisualControl.StashBranch.cs"
[[ -f "$EXE" && -f "$RECEIPT" ]] || {
    echo "ERROR: Fresh executable or build receipt missing." >&2
    exit 1
}
HEAD="$(git rev-parse --short HEAD)"
SOURCE="$(sed -n 's/^source_commit=//p' "$RECEIPT")"
EXPECTED_SHA="$(sed -n 's/^exe_sha256=//p' "$RECEIPT")"
ACTUAL_SHA="$(sha256sum "$EXE" | awk '{print $1}')"
EXPECTED_STASH_SHA="$(sed -n 's/^stash_tier_source_sha256=//p' "$RECEIPT")"
ACTUAL_STASH_SHA="$(sha256sum "$STASH_TIER_SOURCE" | awk '{print $1}')"
NATIVE_ROW_STATUS="$(sed -n 's/^stash_chance_native_row=//p' "$RECEIPT")"

if [[ -z "$SOURCE" || -z "$EXPECTED_SHA" || -z "$EXPECTED_STASH_SHA"
      || "$SOURCE" != "$HEAD" || "$EXPECTED_SHA" != "$ACTUAL_SHA"
      || "$EXPECTED_STASH_SHA" != "$ACTUAL_STASH_SHA"
      || "$NATIVE_ROW_STATUS" != "absent" ]]; then
    echo "ERROR: Build source/EXE/Stashes renderer fingerprint mismatch. Refusing stale launch." >&2
    echo "Git HEAD=$HEAD; receipt=$SOURCE" >&2
    echo "EXE sha256: actual=$ACTUAL_SHA; receipt=$EXPECTED_SHA" >&2
    echo "Stash renderer sha256: actual=$ACTUAL_STASH_SHA; receipt=$EXPECTED_STASH_SHA" >&2
    echo "Stashes NATIVE row: $NATIVE_ROW_STATUS" >&2
    exit 1
fi
if grep -nF '"NATIVE' "$STASH_TIER_SOURCE" >&2; then
    echo "ERROR: Player-facing NATIVE caption still present in Stashes source." >&2
    exit 1
fi

echo "Launching validated True Custom Difficulty from commit $SOURCE"
echo "EXE SHA-256: $ACTUAL_SHA"
echo "Stashes Chance source SHA-256: $ACTUAL_STASH_SHA"
echo "NATIVE card caption: absent"
exec env WINEPREFIX="${WINEPREFIX:-$HOME/.wine-s2}" WINEDEBUG=-all wine "$EXE"
