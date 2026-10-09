#!/usr/bin/env python3
"""Pre-release test runner; preserves PASS/FAIL/NOT RUN and logs.

Modes: list, static, source, writer, all.
See docs/RELEASE-TESTING.md. A PASS never verifies in-game behavior.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
STATIC = (
    ("combat-catalog", "tools/audit_combat_contract.py"),
    ("ui-cfg", "tools/audit_ui_cfg_contract.py"),
    ("project-consistency", "tools/audit_project_consistency.py"),
)
SOURCE = (
    ("original-files-and-hp", "tools/audit_cfg_dump_provenance.py"),
    ("all-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("combat-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("combat-original-special-arrays-and-inheritance", "tools/audit_combat_source_structures.py"),
)
WRITER = "combat-bpatch-writer"
MANUAL = (
    "S.T.A.L.K.E.R. 2 Combat BPatch loading and gameplay effects",
    "Wine UI acceptance against both design lock documents",
    "Build Mod PAK/UTOC/UCAS packaging and actual game load order",
)


def git_head():
    if not shutil.which("git"):
        return "unknown"
    r = subprocess.run(["git", "rev-parse", "HEAD"], cwd=ROOT,
                       capture_output=True, text=True, check=False)
    return r.stdout.strip() if r.returncode == 0 else "unknown"


def execute(label, command, out):
    log = out / "logs" / (label + ".log")
    log.parent.mkdir(parents=True, exist_ok=True)
    print(f"\n[RUN] {label}: {' '.join(command)}", flush=True)
    try:
        r = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE,
                           stderr=subprocess.STDOUT, text=True,
                           encoding="utf-8", errors="replace", timeout=900,
                           check=False)
        output, code = r.stdout, r.returncode
    except (OSError, subprocess.TimeoutExpired) as exc:
        output, code = str(exc), -1
    log.write_text(output, encoding="utf-8")
    if output:
        print(output, end="" if output.endswith("\n") else "\n")
    status = "PASS" if code == 0 else "FAIL"
    print(f"[{status}] {label} (log {log})")
    return {"name": label, "status": status, "returnCode": code,
            "command": command, "log": str(log)}


def not_run(label, why):
    print(f"[NOT RUN] {label}: {why}")
    return {"name": label, "status": "NOT RUN", "reason": why}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("list", "static", "source", "writer", "all"),
                        default="static", nargs="?")
    parser.add_argument("--base-zip", type=Path)
    parser.add_argument("--dlc1-zip", type=Path)
    parser.add_argument("--output", type=Path,
                        help="Evidence folder; defaults to audit/pre-release/<UTC>-<SHA>")
    args = parser.parse_args()

    if args.mode == "list":
        print("STATIC (Linux/Windows):")
        for label, path in STATIC:
            print(f"  {label}: python {path}")
        print("SOURCE (requires matching original base and DLC1 CFG ZIPs):")
        for label, path in SOURCE:
            print(f"  {label}: python {path}")
        print("WINDOWS + .NET 10: tests/CombatPatchContract (82 real writer cases)")
        print("MANUAL (never automatically PASS):")
        for name in MANUAL:
            print(f"  [NOT RUN] {name}")
        print("Full instructions: docs/RELEASE-TESTING.md")
        return 0

    if args.mode in ("source", "all"):
        for opt, value in (("--base-zip", args.base_zip),
                           ("--dlc1-zip", args.dlc1_zip)):
            if value is None or not value.is_file():
                parser.error(f"{args.mode} requires existing {opt} (got {value})")

    sha = git_head()
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    out = (args.output or ROOT / "audit" / "pre-release" /
           f"{stamp}-{sha[:10]}").resolve()
    out.mkdir(parents=True, exist_ok=True)
    start = datetime.now(timezone.utc).isoformat()
    steps = []

    if args.mode in ("static", "all"):
        for label, script in STATIC:
            steps.append(execute(label, [sys.executable, str(ROOT / script)], out))
    else:
        steps.extend(not_run(label, "not selected") for label, _ in STATIC)

    zip_sources = {}
    if args.mode in ("source", "all"):
        base, dlc1 = str(args.base_zip.resolve()), str(args.dlc1_zip.resolve())
        zip_sources = {"base": base, "dlc1": dlc1}
        for label, script in SOURCE:
            command = [sys.executable, str(ROOT / script),
                       "--base-zip", base, "--dlc1-zip", dlc1]
            if label == "all-direct-source-values":
                command.extend(["--out", str(out / "all-source-values.json"), "--strict"])
            if label == "combat-direct-source-values":
                command.extend(["--out", str(out / "combat-source-values.json"),
                                "--parameter-prefix", "combat.", "--strict"])
            if label == "combat-original-special-arrays-and-inheritance":
                command.extend(["--out", str(out / "combat-original-special-arrays.json")])
            steps.append(execute(label, command, out))
    else:
        steps.extend(not_run(label, "matching source ZIPs not selected")
                     for label, _ in SOURCE)

    if args.mode in ("writer", "all"):
        if os.name != "nt":
            steps.append(not_run(WRITER, "Windows WPF required; verify the "
                                 "combat-writer-windows CI job for this SHA"))
        elif not shutil.which("dotnet"):
            steps.append({"name": WRITER, "status": "FAIL",
                          "reason": "Requires .NET 10 SDK"})
            print("[FAIL] writer: dotnet not found")
        else:
            command = ["dotnet", "run", "--project",
                       str(ROOT / "tests/CombatPatchContract/CombatPatchContract.csproj"),
                       "-c", "Release", "--", "--out",
                       str(out / "combat-patch-fixtures")]
            steps.append(execute(WRITER, command, out))
    else:
        steps.append(not_run(WRITER, "not selected; check Windows CI separately"))

    failed = sum(s["status"] == "FAIL" for s in steps)
    skipped = sum(s["status"] == "NOT RUN" for s in steps)
    passed = sum(s["status"] == "PASS" for s in steps)
    if failed:
        overall, code = "FAIL", 1
    elif skipped and args.mode in ("writer", "all"):
        overall, code = "INCOMPLETE", 2
    else:
        overall, code = "PASS", 0

    result = {
        "schemaVersion": 1,
        "mode": args.mode,
        "result": overall,
        "gitCommit": sha,
        "platform": platform.platform(),
        "startedUtc": start,
        "finishedUtc": datetime.now(timezone.utc).isoformat(),
        "sourceArchives": zip_sources,
        "steps": steps,
        "manualAcceptance": [{"name": name, "status": "NOT RUN"} for name in MANUAL],
        "interpretation": (
            "PASS covers selected automated checks only. "
            "Game loading, WPF visuals and gameplay acceptance are NOT RUN."
        ),
    }
    summary_path = out / "summary.json"
    summary_path.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n",
                            encoding="utf-8")
    print(f"\n[{overall}] {passed} PASS / {failed} FAIL / {skipped} NOT RUN")
    print(f"Evidence: {summary_path}")
    print("[NOT RUN] Manual game/Wine/packaging acceptance")
    return code


if __name__ == "__main__":
    sys.exit(main())
