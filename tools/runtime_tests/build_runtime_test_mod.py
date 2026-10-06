#!/usr/bin/env python3
"""Generate and pack one isolated TCD runtime-proof test mod.

Uses the exact same container contract as the production ModBuildService:
repak V11, mount point ../../../, then retoc UE5_5. Third-party tools are
never bundled; supply repak/retoc in --tools-dir (default: project tools/).
"""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import tempfile
from pathlib import Path

from generate_runtime_test_patches import TESTS, generate

ROOT = Path(__file__).resolve().parents[2]
REPAK_VERSION = "V11"
RETOC_VERSION = "UE5_5"
MOUNT_POINT = "../../../"
DISPLAY = {
    "weapon-condition": "WeaponCondition",
    "weapon-ammo": "WeaponAmmo",
    "medic-stock": "MedicStock",
}


def require_tool(tools_dir: Path, name: str) -> Path:
    for candidate in (tools_dir / name, tools_dir / f"{name}.exe"):
        if candidate.is_file():
            return candidate.resolve()
    raise SystemExit(f"required tool {name!r} not found in {tools_dir}")


def run(cmd: list[str], cwd: Path) -> None:
    shown = " ".join(cmd)
    print(f"[RUN] {shown}")
    try:
        proc = subprocess.run(cmd, cwd=cwd, text=True, capture_output=True)
    except OSError as exc:
        raise SystemExit(f"could not execute {cmd[0]}: {exc}") from exc
    if proc.returncode:
        if proc.stderr.strip():
            print(proc.stderr.strip())
        if proc.stdout.strip():
            print(proc.stdout.strip())
        raise SystemExit(f"tool failed with exit code {proc.returncode}: {cmd[0]}")


def validate_triplet(paths: list[Path]) -> None:
    bad = [p.name for p in paths if not p.is_file() or p.stat().st_size == 0]
    if bad:
        raise SystemExit("packing did not produce a complete non-empty triplet: " + ", ".join(bad))


def build(test: str, variant: str, tools_dir: Path, output_dir: Path, keep_staging: bool) -> list[Path]:
    repak = require_tool(tools_dir, "repak")
    retoc = require_tool(tools_dir, "retoc")
    mod_name = f"TCD_RuntimeTest_{DISPLAY[test]}_{variant.upper()}_P"
    output_dir.mkdir(parents=True, exist_ok=True)

    work = Path(tempfile.mkdtemp(prefix="tcd-runtime-test-"))
    staging = work / "staging"
    packed = work / "packed"
    packed.mkdir(parents=True)
    try:
        rows, patch_files = generate(test, variant, staging)
        print(f"[OK] generated {rows} locked rows -> {patch_files} BPatch file(s)")

        pak = packed / f"{mod_name}.pak"
        utoc = packed / f"{mod_name}.utoc"
        ucas = packed / f"{mod_name}.ucas"
        run([str(repak), "pack", str(staging), str(pak), "--mount-point", MOUNT_POINT, "--version", REPAK_VERSION], packed)
        run([str(retoc), "to-zen", str(staging), str(utoc), "--version", RETOC_VERSION], packed)
        validate_triplet([pak, utoc, ucas])

        # Replace only this exact test/variant triplet. Other runtime tests are left alone.
        final = []
        for src in (pak, utoc, ucas):
            dst = output_dir / src.name
            tmp = output_dir / (src.name + ".tmp")
            shutil.copy2(src, tmp)
            os.replace(tmp, dst)
            final.append(dst)

        if keep_staging:
            saved = output_dir / f"{mod_name}_staging"
            if saved.exists():
                shutil.rmtree(saved)
            shutil.copytree(staging, saved)
            print(f"[OK] staging copy: {saved}")
        return final
    finally:
        shutil.rmtree(work, ignore_errors=True)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--test", choices=sorted(TESTS), required=True)
    ap.add_argument("--variant", choices=["low", "high"], required=True)
    ap.add_argument("--tools-dir", type=Path, default=ROOT / "tools")
    ap.add_argument("--output", type=Path, default=ROOT / "RuntimeTestOutput")
    ap.add_argument("--keep-staging", action="store_true")
    args = ap.parse_args()
    files = build(args.test, args.variant, args.tools_dir.resolve(), args.output.resolve(), args.keep_staging)
    for path in files:
        print(f"[OK] {path}")


if __name__ == "__main__":
    main()
