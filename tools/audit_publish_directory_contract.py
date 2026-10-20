#!/usr/bin/env python3
"""Exercise publish.sh using a fake dotnet publish (no SDK/Wine required).

Regression: never unlink publish/win-x64 or erase tools, user presets, output
artifacts, custom files or a previously working executable after failed build.
"""
import hashlib
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
PUBLISH_SCRIPT = ROOT / "publish.sh"
script = PUBLISH_SCRIPT.read_text(encoding="utf-8")
assert subprocess.run(
    ["bash", "-n", str(PUBLISH_SCRIPT)], check=False, capture_output=True
).returncode == 0, "publish.sh failed bash syntax validation"
assert 'rm -rf -- "$PUBLISH_DIR"' not in script
assert 'PUBLISH_STAGING="$(mktemp -d "$PUBLISH_DIR/' in script
assert 'PUBLISH_BACKUP="$(mktemp -d "$PUBLISH_DIR/' in script
assert 'TRANSACTION_STARTED=1' in script
assert 'tools|Presets|Output|BUILD-INFO.txt' in script
assert 'mv -- "$PUBLISH_STAGING/$name" "$PUBLISH_DIR/$name"' in script
assert 'cp -- "$ROOT/tools/README.txt" "$PUBLISH_DIR/tools/README.txt"' in script
assert 'if [[ ! -e "$PUBLISH_DIR/tools/README.txt" ]]; then' in script

with tempfile.TemporaryDirectory(prefix="tcd-publish-inode-") as tmp:
    work = Path(tmp)
    fixture = work / "TrueCustomDifficulty"
    fixture.mkdir()
    shutil.copy2(PUBLISH_SCRIPT, fixture / "publish.sh")
    project = fixture / "src/TrueCustomDifficulty"
    (project / "Presets").mkdir(parents=True)
    (project / "TrueCustomDifficulty.csproj").write_text(
        "<Project><PropertyGroup><TargetFramework>net10.0-windows"
        "</TargetFramework></PropertyGroup></Project>\n",
        encoding="utf-8",
    )
    (project / "Presets/README.md").write_text("SOURCE README\n", encoding="utf-8")
    source_tools = fixture / "tools"
    source_tools.mkdir()
    (source_tools / "README.txt").write_text(
        "SOURCE TOOL INSTRUCTIONS\n", encoding="utf-8"
    )
    fakebin = work / "bin"
    fakebin.mkdir()
    dotnet = fakebin / "dotnet"
    dotnet.write_text(
        r"""#!/usr/bin/env bash
set -euo pipefail
if [[ "$1" == "--version" ]]; then
    printf '10.0.100\n'
    exit 0
fi
[[ "$1" == "publish" ]]
out=""
prev=""
for arg in "$@"; do
    if [[ "$prev" == "-o" ]]; then out="$arg"; fi
    prev="$arg"
done
[[ -n "$out" ]]
mkdir -p -- "$out"
if [[ "${FAKE_DOTNET_NO_EXE:-0}" != "1" ]]; then
    printf 'EXE BUILD %s\n' "${FAKE_DOTNET_BUILD_ID:-1}" > "$out/TrueCustomDifficulty.exe"
fi
printf 'SIDECAR %s\n' "${FAKE_DOTNET_BUILD_ID:-1}" > "$out/RuntimeSidecar.dll"
mkdir -p "$out/runtimes"
printf 'NATIVE %s\n' "${FAKE_DOTNET_BUILD_ID:-1}" > "$out/runtimes/NativeResource.dat"
if [[ "${FAKE_DOTNET_COLLIDE:-0}" == "1" ]]; then
    mkdir -p "$out/tools"
    printf 'BAD\n' > "$out/tools/repak.exe"
fi
""",
        encoding="utf-8",
    )
    dotnet.chmod(0o755)
    fake_pgrep = fakebin / "pgrep"
    fake_pgrep.write_text("#!/usr/bin/env bash\nexit 1\n", encoding="utf-8")
    fake_pgrep.chmod(0o755)

    dest = fixture / "publish/win-x64"
    (dest / "tools").mkdir(parents=True)
    (dest / "Presets").mkdir()
    (dest / "Output").mkdir()
    sentinels = {
        "tools/repak.exe": b"USER REPAK",
        "tools/retoc.exe": b"USER RETOC",
        "Presets/Custom.json": b'{"test":"user"}\n',
        "Presets/README.md": b"USER EDITED README",
        "Output/previous.pak": b"PREVIOUS GOOD MOD",
        "user-notes.txt": b"USER NOTES",
    }
    for name, value in sentinels.items():
        (dest / name).write_bytes(value)

    (dest / "TrueCustomDifficulty.exe").write_bytes(b"OLD GOOD EXE")
    (dest / "BUILD-INFO.txt").write_bytes(b"OLD GOOD RECEIPT")
    inode = dest.stat().st_ino
    tools_inode = (dest / "tools").stat().st_ino
    presets_inode = (dest / "Presets").stat().st_ino
    output_inode = (dest / "Output").stat().st_ino

    def run(**opts):
        env = os.environ.copy()
        env.update({
            "PATH": str(fakebin) + os.pathsep + env.get("PATH", ""),
            "FAKE_DOTNET_BUILD_ID": "1",
            "FAKE_DOTNET_NO_EXE": "0",
            "FAKE_DOTNET_COLLIDE": "0",
        })
        env.update(opts)
        return subprocess.run(
            ["bash", "./publish.sh"],
            cwd=fixture, env=env, capture_output=True,
            text=True, timeout=35, check=False,
        )

    def check_state():
        assert dest.stat().st_ino == inode, "Dolphin inode would go stale"
        assert (dest / "tools").stat().st_ino == tools_inode
        assert (dest / "Presets").stat().st_ino == presets_inode
        assert (dest / "Output").stat().st_ino == output_inode
        for name, contents in sentinels.items():
            assert (dest / name).read_bytes() == contents, name
        assert not list(dest.glob(".tcd-publish-*.??????")), "Stale publish staging"

    one = run()
    assert one.returncode == 0, one.stdout + one.stderr
    check_state()
    # The first publish supplies only the README; user-owned binaries survive.
    runtime_tools_readme = dest / "tools/README.txt"
    assert runtime_tools_readme.read_text(encoding="utf-8") == "SOURCE TOOL INSTRUCTIONS\n"
    runtime_tools_readme.write_text("USER EDITED TOOLS README\n", encoding="utf-8")
    assert (dest / "TrueCustomDifficulty.exe").read_bytes() == b"EXE BUILD 1\n"
    assert (dest / "RuntimeSidecar.dll").read_bytes() == b"SIDECAR 1\n"
    assert (dest / "runtimes/NativeResource.dat").read_bytes() == b"NATIVE 1\n"
    receipt = (dest / "BUILD-INFO.txt").read_text()
    assert "exe_sha256=" + hashlib.sha256(b"EXE BUILD 1\n").hexdigest() in receipt

    two = run(FAKE_DOTNET_BUILD_ID="2")
    assert two.returncode == 0, two.stdout + two.stderr
    check_state()
    assert runtime_tools_readme.read_text(encoding="utf-8") == "USER EDITED TOOLS README\n"
    assert (dest / "TrueCustomDifficulty.exe").read_bytes() == b"EXE BUILD 2\n"
    assert (dest / "RuntimeSidecar.dll").read_bytes() == b"SIDECAR 2\n"
    assert (dest / "runtimes/NativeResource.dat").read_bytes() == b"NATIVE 2\n"
    previous_exe = (dest / "TrueCustomDifficulty.exe").read_bytes()
    previous_receipt = (dest / "BUILD-INFO.txt").read_bytes()

    missing = run(FAKE_DOTNET_NO_EXE="1", FAKE_DOTNET_BUILD_ID="3")
    assert missing.returncode != 0
    check_state()
    assert runtime_tools_readme.read_text(encoding="utf-8") == "USER EDITED TOOLS README\n"
    assert (dest / "TrueCustomDifficulty.exe").read_bytes() == previous_exe
    assert (dest / "BUILD-INFO.txt").read_bytes() == previous_receipt

    collision = run(FAKE_DOTNET_COLLIDE="1", FAKE_DOTNET_BUILD_ID="4")
    assert collision.returncode != 0
    check_state()
    assert runtime_tools_readme.read_text(encoding="utf-8") == "USER EDITED TOOLS README\n"
    assert (dest / "TrueCustomDifficulty.exe").read_bytes() == previous_exe
    assert (dest / "BUILD-INFO.txt").read_bytes() == previous_receipt

print("PASS: publish/win-x64 retains its inode, portable tools/presets/output,"
      " and unrelated user files after repeat publish.")
print("PASS: user-facing tools/README.txt is seeded once and never overwrites edits.")
print("PASS: runtime artifacts and BUILD-INFO update together;"
      " bad/unsafe dotnet output leaves the last good build intact.")
print("NOT TESTED: real .NET 10 WPF publish output or Dolphin rendering.")
