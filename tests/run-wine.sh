#!/usr/bin/env bash
set -Eeuo pipefail

# Build a Windows/WPF contract on Linux, then execute its self-contained
# win-x64 EXE through Wine. "dotnet run" is for native Windows, not Linux.
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

TEST_NAME="${1:-LootSpecialGeneratorsContract}"
if (( $# > 1 )) || [[ ! "$TEST_NAME" =~ ^[A-Za-z][A-Za-z0-9]*$ ]]; then
    echo "Usage: ./tests/run-wine.sh [TestProjectName]" >&2
    exit 2
fi

PROJECT="$ROOT/tests/$TEST_NAME/$TEST_NAME.csproj"
if [[ ! -f "$PROJECT" ]]; then
    echo "ERROR: Unknown test project '$TEST_NAME'." >&2
    echo "Pick a directory under tests/ containing a matching .csproj." >&2
    exit 2
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: The .NET 10 SDK is required to cross-build the Windows test." >&2
    exit 1
fi
if ! command -v wine >/dev/null 2>&1; then
    echo "ERROR: Wine is required to execute WPF Windows tests on Linux." >&2
    exit 1
fi

# Publish into an isolated, temporary directory. Do not write into the app's
# portable publish/win-x64 directory or touch its tools/Presets/Output state.
OUTPUT="$(mktemp -d "${TMPDIR:-/tmp}/tcd-${TEST_NAME}-wine.XXXXXX")"
trap 'rm -rf -- "$OUTPUT"' EXIT

echo "Cross-publishing $TEST_NAME for Windows x64..."
dotnet publish "$PROJECT" \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=false \
    -o "$OUTPUT"

EXE="$OUTPUT/$TEST_NAME.exe"
if [[ ! -s "$EXE" ]]; then
    echo "ERROR: Expected executable missing after publish: $EXE" >&2
    exit 1
fi

echo "Running $TEST_NAME through Wine (not dotnet run)..."
env WINEPREFIX="${WINEPREFIX:-$HOME/.wine-s2}" \
    WINEDEBUG="${WINEDEBUG:--all}" \
    wine "$EXE"
