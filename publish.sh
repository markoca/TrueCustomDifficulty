#!/usr/bin/env bash
set -Eeuo pipefail

# Publish the WPF application from Linux/CachyOS as a self-contained Windows x64 EXE.
# This does NOT run the application and does NOT require Wine during publish.
# The resulting portable runtime is emitted under publish/win-x64/, matching publish.ps1.
# Run the published EXE on Linux with the existing wine-s2 prefix, for example:
#   WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all wine "$ROOT/publish/win-x64/TrueCustomDifficulty.exe"

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

# An older Wine instance can keep showing the former 4x/2x UI even after
# publishing the correct 2x/1x source. Never present that stale window as QA.
# Stop before touching the publish output so the operator can close it safely.
if command -v pgrep >/dev/null 2>&1 && pgrep -af '[T]rueCustomDifficulty\.exe' >/dev/null 2>&1; then
    echo "ERROR: A TrueCustomDifficulty.exe process is already running." >&2
    echo "Close the old Wine application before publishing and testing UI changes." >&2
    pgrep -af '[T]rueCustomDifficulty\.exe' >&2 || true
    exit 1
fi

PROJECT="$ROOT/src/TrueCustomDifficulty/TrueCustomDifficulty.csproj"
PROJECT_DIR="$(dirname "$PROJECT")"
STASH_TIER_SOURCE="$PROJECT_DIR/Controls/AggregateVisualControl.StashBranch.cs"
EXE_NAME="TrueCustomDifficulty.exe"
PUBLISH_DIR="$ROOT/publish/win-x64"
FINAL_EXE="$PUBLISH_DIR/$EXE_NAME"
TEMP_OUTPUT="$(mktemp -d "${TMPDIR:-/tmp}/TrueCustomDifficulty-publish.XXXXXX")"

# Never remove/recreate publish/win-x64: KDE Dolphin can retain the old
# inode, and users keep repak/retoc, customized presets and output there.
PUBLISH_STAGING=""
PUBLISH_BACKUP=""
TRANSACTION_STARTED=0
declare -a INSTALLED_NAMES=()
declare -a BACKED_UP_NAMES=()

cleanup() {
    local status=$?
    trap - EXIT

    if (( status != 0 && TRANSACTION_STARTED )); then
        # Roll back partially installed runtime artifacts on publish failure.
        local restore_failed=0
        for name in "${INSTALLED_NAMES[@]}"; do
            rm -rf -- "$PUBLISH_DIR/$name" || restore_failed=1
        done
        for name in "${BACKED_UP_NAMES[@]}"; do
            if ! mv -f -- "$PUBLISH_BACKUP/$name" "$PUBLISH_DIR/$name"; then
                echo "ERROR: Could not restore $name; recover it from $PUBLISH_BACKUP" >&2
                restore_failed=1
            fi
        done
        if (( restore_failed )); then
            echo "ERROR: Publish rollback needs manual recovery from $PUBLISH_BACKUP" >&2
            PUBLISH_BACKUP="" # Keep the backup intact.
        fi
    fi

    [[ -z "$PUBLISH_STAGING" ]] || rm -rf -- "$PUBLISH_STAGING"
    [[ -z "$PUBLISH_BACKUP" ]] || rm -rf -- "$PUBLISH_BACKUP"
    rm -rf -- "$TEMP_OUTPUT"
    exit "$status"
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

# STANDARD/RARE stash chance cards contain a user percentage and a slider,
# not a duplicate NATIVE source percentage. Refuse to package regressed UI
# so a successful publish cannot accidentally restore the removed third row.
if [[ ! -f "$STASH_TIER_SOURCE" ]]; then
    echo "ERROR: Stashes Chance renderer was not found: $STASH_TIER_SOURCE" >&2
    exit 1
fi
if grep -nF '"NATIVE' "$STASH_TIER_SOURCE" >&2; then
    echo "ERROR: NATIVE text reappeared in the Standard/Rare Stashes Chance renderer." >&2
    exit 1
fi

TARGET_FRAMEWORK="$(sed -n 's:.*<TargetFramework>\(.*\)</TargetFramework>.*:\1:p' "$PROJECT" | head -n1)"
if [[ "$TARGET_FRAMEWORK" != "net10.0-windows" ]]; then
    echo "ERROR: Project target framework is '$TARGET_FRAMEWORK', expected 'net10.0-windows'." >&2
    echo "Refusing to publish an old/stale project checkout." >&2
    exit 1
fi

# Publish the current source without running test or verification scripts.
# WPF creates generated temporary projects under obj/.  Removing both build trees
# guarantees that an older net8.0-windows temp project cannot leak into a .NET 10 publish.
rm -rf -- "$PROJECT_DIR/bin" "$PROJECT_DIR/obj"

GIT_HEAD="not-a-git-checkout"
if command -v git >/dev/null 2>&1 && git -C "$ROOT" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    GIT_HEAD="$(git -C "$ROOT" rev-parse --short HEAD)"
fi

echo "Publishing True Custom Difficulty..."
echo "Host: Linux"
echo "Target: Windows x64 (win-x64)"
echo "Framework: $TARGET_FRAMEWORK"
echo "SDK: $SDK_VERSION"
echo "Source commit: $GIT_HEAD"
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
# Never destroy a prior working build if dotnet publish failed to emit EXE.
if [[ ! -s "$BUILT_EXE" ]]; then
    echo "ERROR: Publish completed, but the expected EXE was not created:" >&2
    echo "  $BUILT_EXE" >&2
    exit 1
fi

# Replace published *runtime artifacts*, not the published directory itself.
# Removing and recreating the directory breaks open Dolphin tabs and destroys
# user-supplied tools, customized Presets and previously built Output packages.
if [[ -L "$PUBLISH_DIR" ]]; then
    echo "ERROR: Refusing to publish into a symlink: $PUBLISH_DIR" >&2
    exit 1
fi
mkdir -p -- "$PUBLISH_DIR"
if [[ ! -d "$PUBLISH_DIR" ]]; then
    echo "ERROR: Publish path is not a directory: $PUBLISH_DIR" >&2
    exit 1
fi

# Same-filesystem staging + rename keeps the directory inode stable and
# switches each runtime file into place without exposing partially copied EXEs.
PUBLISH_STAGING="$(mktemp -d "$PUBLISH_DIR/.tcd-publish-new.XXXXXX")"
PUBLISH_BACKUP="$(mktemp -d "$PUBLISH_DIR/.tcd-publish-old.XXXXXX")"
declare -a RUNTIME_NAMES=()

while IFS= read -r -d '' file; do
    name="$(basename -- "$file")"

    # These paths contain user-owned runtime tools, presets and built mods;
    # .NET must never replace them, even if its output layout changes.
    case "$name" in
        tools|Presets|Output|BUILD-INFO.txt)
            echo "ERROR: Unexpected .NET publish artifact collides with portable state: $name" >&2
            exit 1
            ;;
    esac

    if [[ -f "$file" ]]; then
        cp -p -- "$file" "$PUBLISH_STAGING/$name"
    elif [[ -d "$file" ]]; then
        cp -a -- "$file" "$PUBLISH_STAGING/$name"
    else
        echo "ERROR: Unsupported .NET publish artifact: $file" >&2
        exit 1
    fi
    RUNTIME_NAMES+=("$name")
done < <(find "$TEMP_OUTPUT" -mindepth 1 -maxdepth 1 ! -name '*.pdb' -print0)

SIZE_BYTES="$(stat -c '%s' "$BUILT_EXE")"
SIZE_MB="$(awk -v bytes="$SIZE_BYTES" 'BEGIN { printf "%.1f", bytes / 1048576 }')"
EXE_SHA256="$(sha256sum "$BUILT_EXE" | cut -d' ' -f1)"
STASH_TIER_SHA256="$(sha256sum "$STASH_TIER_SOURCE" | cut -d' ' -f1)"
# Publish the matching source/binary receipt as part of the same transaction.
# The renderer checksum differentiates this source-backed UI from an older
# executable or another checkout that still paints the removed NATIVE row.
printf 'source_commit=%s\nexe_sha256=%s\nstash_tier_source_sha256=%s\nstash_chance_native_row=absent\n' "$GIT_HEAD" "$EXE_SHA256" "$STASH_TIER_SHA256" > "$PUBLISH_STAGING/BUILD-INFO.txt"
RUNTIME_NAMES+=("BUILD-INFO.txt")

TRANSACTION_STARTED=1
for name in "${RUNTIME_NAMES[@]}"; do
    if [[ -e "$PUBLISH_DIR/$name" || -L "$PUBLISH_DIR/$name" ]]; then
        mv -- "$PUBLISH_DIR/$name" "$PUBLISH_BACKUP/$name"
        BACKED_UP_NAMES+=("$name")
    fi
    mv -- "$PUBLISH_STAGING/$name" "$PUBLISH_DIR/$name"
    INSTALLED_NAMES+=("$name")
done

mkdir -p -- "$PUBLISH_DIR/tools" "$PUBLISH_DIR/Presets" "$PUBLISH_DIR/Output"
# Seed user-facing documentation like Localization Workbench. Preserve an
# existing user-edited README and never overwrite supplied repak/retoc binaries.
if [[ ! -e "$PUBLISH_DIR/tools/README.txt" ]]; then
    cp -- "$ROOT/tools/README.txt" "$PUBLISH_DIR/tools/README.txt"
fi
# Only initialize preset README if absent. Never reset user-edited portable state.
if [[ ! -e "$PUBLISH_DIR/Presets/README.md" ]]; then
    cp -- "$ROOT/src/TrueCustomDifficulty/Presets/README.md" "$PUBLISH_DIR/Presets/README.md"
fi
TRANSACTION_STARTED=0

echo
echo "Published successfully."
echo "EXE: $EXE_NAME ($SIZE_MB MB)"
echo "Location: $PUBLISH_DIR"
# BUILD-INFO.txt was installed alongside the executable transactionally.
echo "Build receipt: $PUBLISH_DIR/BUILD-INFO.txt"
echo "Executable SHA-256: $EXE_SHA256"
echo "Stashes Chance renderer SHA-256: $STASH_TIER_SHA256"
echo
echo "For a verified, fresh main-branch launch, use:"
echo "  bash \"$ROOT/run-latest.sh\""
echo "It closes no other Wine processes; an already-running app must be closed manually."

mapfile -d '' EXTRA_FILES < <(find "$TEMP_OUTPUT" -maxdepth 1 -type f ! -name '*.pdb' ! -name "$EXE_NAME" -print0)
if (( ${#EXTRA_FILES[@]} > 0 )); then
    echo
    echo "Additional runtime files produced by .NET:"
    for file in "${EXTRA_FILES[@]}"; do
        echo "  $(basename -- "$file")"
    done
fi
