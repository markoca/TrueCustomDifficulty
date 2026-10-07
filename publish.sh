#!/usr/bin/env bash
set -Eeuo pipefail

# Publish the WPF application from Linux/CachyOS as a self-contained Windows x64 EXE.
# This does NOT run the application and does NOT require Wine during publish.
# The resulting portable runtime is emitted under publish/win-x64/, matching publish.ps1.
# Run the published EXE on Linux with the existing wine-s2 prefix, for example:
#   WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all wine "$ROOT/publish/win-x64/TrueCustomDifficulty.exe"

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

PROJECT="$ROOT/src/TrueCustomDifficulty/TrueCustomDifficulty.csproj"
EXE_NAME="TrueCustomDifficulty.exe"
PUBLISH_DIR="$ROOT/publish/win-x64"
FINAL_EXE="$PUBLISH_DIR/$EXE_NAME"
TEMP_OUTPUT="$(mktemp -d "${TMPDIR:-/tmp}/TrueCustomDifficulty-publish.XXXXXX")"

cleanup() {
    rm -rf -- "$TEMP_OUTPUT"
}
trap cleanup EXIT

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: .NET SDK was not found." >&2
    echo "Install the .NET 10 SDK and make sure 'dotnet' is available in PATH." >&2
    echo "CachyOS/Arch: sudo pacman -Syu dotnet-sdk-10.0" >&2
    exit 1
fi

if ! SDK_VERSION="$(dotnet --version 2>/dev/null)"; then
    echo "ERROR: No compatible .NET 10 SDK could be selected for this repository." >&2
    echo "global.json pins the project to the .NET 10 SDK line." >&2
    echo "CachyOS/Arch: sudo pacman -Syu dotnet-sdk-10.0" >&2
    exit 1
fi

if [[ "$SDK_VERSION" != 10.* ]]; then
    echo "ERROR: True Custom Difficulty requires a .NET 10 SDK; selected SDK is $SDK_VERSION." >&2
    exit 1
fi

if [[ ! -f "$PROJECT" ]]; then
    echo "ERROR: Project file was not found:" >&2
    echo "  $PROJECT" >&2
    exit 1
fi

echo "Publishing True Custom Difficulty..."
echo "Host: Linux"
echo "Target: Windows x64 (win-x64)"
echo "Framework: net10.0-windows"
echo "SDK: $SDK_VERSION"
echo "Final runtime output will be placed in: $PUBLISH_DIR"
echo

dotnet publish "$PROJECT" \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:EnableWindowsTargeting=true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:PublishTrimmed=false \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o "$TEMP_OUTPUT"

BUILT_EXE="$TEMP_OUTPUT/$EXE_NAME"
rm -rf -- "$PUBLISH_DIR"
mkdir -p -- "$PUBLISH_DIR"
if [[ ! -f "$BUILT_EXE" ]]; then
    echo "ERROR: Publish completed, but the expected EXE was not created:" >&2
    echo "  $BUILT_EXE" >&2
    exit 1
fi

find "$TEMP_OUTPUT" -maxdepth 1 -type f ! -name '*.pdb' -print0 |
while IFS= read -r -d '' file; do
    cp -f -- "$file" "$PUBLISH_DIR/$(basename -- "$file")"
done

find "$TEMP_OUTPUT" -mindepth 1 -maxdepth 1 -type d -print0 |
while IFS= read -r -d '' dir; do
    name="$(basename -- "$dir")"
    rm -rf -- "$PUBLISH_DIR/$name"
    cp -a -- "$dir" "$PUBLISH_DIR/$name"
done

mkdir -p "$PUBLISH_DIR/tools"
mkdir -p "$PUBLISH_DIR/Presets" "$PUBLISH_DIR/Output"
cp -f -- "$ROOT/src/TrueCustomDifficulty/Presets/README.md" "$PUBLISH_DIR/Presets/README.md"

SIZE_BYTES="$(stat -c '%s' "$FINAL_EXE")"
SIZE_MB="$(awk -v bytes="$SIZE_BYTES" 'BEGIN { printf "%.1f", bytes / 1048576 }')"

echo
echo "Published successfully."
echo "EXE: $EXE_NAME ($SIZE_MB MB)"
echo "Location: $PUBLISH_DIR"
echo
echo "Run on Linux with wine-s2:"
echo '  WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all wine "'"$FINAL_EXE"'"'

mapfile -d '' EXTRA_FILES < <(find "$TEMP_OUTPUT" -maxdepth 1 -type f ! -name '*.pdb' ! -name "$EXE_NAME" -print0)
if (( ${#EXTRA_FILES[@]} > 0 )); then
    echo
    echo "Additional runtime files produced by .NET:"
    for file in "${EXTRA_FILES[@]}"; do
        echo "  $(basename -- "$file")"
    done
fi
