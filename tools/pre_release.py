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
    ("world-weapon-nested-source", "tools/audit_world_weapon_nested_source.py"),
)
SOURCE = (
    ("original-files-and-hp", "tools/audit_cfg_dump_provenance.py"),
    ("all-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("combat-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("combat-original-special-arrays-and-inheritance", "tools/audit_combat_source_structures.py"),
    ("health-stamina-original-source", "tools/audit_health_stamina_source.py"),
    ("consumable-original-effect-identities", "tools/audit_consumable_effect_source.py"),
    ("survival-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("economy-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("equipment-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("environment-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("alife-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("alife-director-source-structure", "tools/audit_alife_director_source.py"),
    ("loot-direct-source-values", "tools/audit_cfg_source_values.py"),
    ("loot-direct-source-structures", "tools/audit_loot_direct_source.py"),
    ("loot-world-original-source-structures", "tools/audit_loot_world_source.py"),
)
WRITERS = (
    ("combat-bpatch-writer", "CombatPatchContract", "combat-patch-fixtures", 84),
    ("health-stamina-bpatch-writer", "HealthStaminaPatchContract", "health-stamina-patch-fixtures", 46),
    ("survival-bpatch-writer", "SurvivalPatchContract", "survival-patch-fixtures", 87),
    ("economy-bpatch-writer", "EconomyPatchContract", "economy-patch-fixtures", 34),
    ("equipment-bpatch-writer", "EquipmentPatchContract", "equipment-patch-fixtures", 32),
    ("environment-bpatch-writer", "EnvironmentPatchContract", "environment-patch-fixtures", 90),
    ("alife-bpatch-writer", "ALifePatchContract", "alife-patch-fixtures", 94),
    ("loot-direct-bpatch-writer", "LootDirectPatchContract", "loot-direct-patch-fixtures", 117),
    ("loot-special-generators-writer", "LootSpecialGeneratorsContract", "loot-special-generators-fixtures", 63),
    ("loot-world-removals-writer", "LootWorldRemovalsContract", "loot-world-removals-fixtures", 27),
    ("loot-world-placement-writer", "LootWorldPlacementContract", "loot-world-placement-fixtures", 24),
)
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
        for label, project, _, cases in WRITERS:
            print(f"WINDOWS + .NET 10: {project} ({cases} writer scenarios) -> {label}")
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
            if label == "consumable-original-effect-identities":
                command.extend(["--out", str(out / "consumable-original-effects.json")])
            if label == "health-stamina-original-source":
                command.extend(["--out", str(out / "health-stamina-original-source.json")])
            if label == "economy-direct-source-values":
                command.extend(["--out", str(out / "economy-source-values.json"),
                                "--parameter-prefix", "economy.", "--strict"])
            if label == "equipment-direct-source-values":
                command.extend(["--out", str(out / "equipment-source-values.json"),
                                "--parameter-prefix", "equipment.", "--strict"])
            if label == "environment-direct-source-values":
                command.extend(["--out", str(out / "environment-source-values.json"),
                                "--parameter-prefix", "environment.", "--strict"])
            if label == "alife-direct-source-values":
                command.extend(["--out", str(out / "alife-source-values.json"),
                                "--parameter-prefix", "alife.", "--strict"])
            if label == "alife-director-source-structure":
                command.extend(["--out", str(out / "alife-director-structure.json")])
            if label == "loot-direct-source-values":
                command.extend(["--out", str(out / "loot-direct-source-values.json"),
                                "--parameter-prefix", "loot.", "--strict"])
            if label == "loot-direct-source-structures":
                command.extend(["--out", str(out / "loot-direct-source-structures.json")])
            if label == "loot-world-original-source-structures":
                command.extend(["--out", str(out / "loot-world-original-source.json")])
            if label == "survival-direct-source-values":
                command.extend(["--out", str(out / "survival-source-values.json"),
                                "--parameter-prefix", "survival.", "--strict"])
            steps.append(execute(label, command, out))
    else:
        steps.extend(not_run(label, "matching source ZIPs not selected")
                     for label, _ in SOURCE)

    if args.mode in ("writer", "all"):
        if os.name != "nt":
            for label, _, _, _ in WRITERS:
                steps.append(not_run(label, "Windows WPF required; verify the "
                                     "corresponding Windows GitHub Actions job at this SHA"))
        elif not shutil.which("dotnet"):
            for label, _, _, _ in WRITERS:
                steps.append({"name": label, "status": "FAIL",
                              "reason": "Requires .NET 10 SDK"})
                print(f"[FAIL] {label}: dotnet not found")
        else:
            for label, project, folder, _ in WRITERS:
                command = [
                    "dotnet", "run", "--project",
                    str(ROOT / "tests" / project / (project + ".csproj")),
                    "-c", "Release", "--", "--out", str(out / folder)
                ]
                steps.append(execute(label, command, out))
    else:
        steps.extend(not_run(label, "not selected; check Windows CI separately")
                     for label, _, _, _ in WRITERS)

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
