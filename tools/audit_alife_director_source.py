#!/usr/bin/env python3
"""Verify original ALife Director rank/species slots and authoring, not gameplay.

The generic direct-source gate checks MaxCount values. This additional source
gate also verifies that each index points to the INTENDED rank and AgentType;
same-value neighboring slots may otherwise appear correct while being miswired.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys
import zipfile

from audit_cfg_source_values import read_tree, check_point

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "src/TrueCustomDifficulty/Data/cfg-writer-baseline.json"
FILE = "GameData/ALifePrototypes/ALifeDirectorScenarioPrototypes.cfg"
RANKS = ("Newbie", "Experienced", "Veteran", "Master")
AGENTS = (
    "Human", "MutantGeneric", "Bloodsucker", "Blinddog", "Flesh",
    "Controller", "Poltergeist", "Snork", "Boar", "Chimera", "Cat",
    "Tushkan", "RatSwarm", "Burer", "PseudoDog", "Deer",
)
PARAMETER_SUFFIXES = (
    "humanGroupSize", "mutantSpawn.generic", "mutantSpawn.bloodsucker",
    "mutantSpawn.blindDog", "mutantSpawn.flesh", "mutantSpawn.controller",
    "mutantSpawn.poltergeist", "mutantSpawn.snork", "mutantSpawn.boar",
    "mutantSpawn.chimera", "mutantSpawn.bayun", "mutantSpawn.tushkan",
    "mutantSpawn.ratSwarm", "mutantSpawn.burer", "mutantSpawn.pseudodog",
    "mutantSpawn.deer",
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--base-zip", type=Path, required=True)
    p.add_argument("--dlc1-zip", type=Path, required=True)
    p.add_argument("--out", type=Path)
    args = p.parse_args()
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    points = [x for x in baseline["editPoints"] if x["parameterId"].startswith("alife.")]
    failures: list[dict] = []
    records: list[dict] = []
    if len(points) != 64:
        failures.append({"error": "ALife direct manifest field count drift",
                         "actual": len(points), "expected": 64})
    grouped = {}
    for point in points:
        key = (point["prototype"], *point["nodePath"], point["key"])
        if key in grouped:
            failures.append({"error": "ALife duplicate concrete leaf ownership", "path": key})
        grouped[key] = point

    with zipfile.ZipFile(args.base_zip) as base, zipfile.ZipFile(args.dlc1_zip) as dlc1:
        if FILE not in base.namelist():
            raise ValueError(f"Original archive missing {FILE}")
        text = base.read(FILE).decode("utf-8-sig")
        roots = read_tree(text)
        file_sha = hashlib.sha256(base.read(FILE)).hexdigest()
        if FILE in dlc1.namelist():
            failures.append({"error": "DLC1 now supplies an ALife Director file: reassess scope"})

        root = [r for r in roots if r.name == "ALifeDirectorPreset"]
        if len(root) != 1 or root[0].values.get("SID") != "Default":
            failures.append({"error": "Director prototype identity / SID changed"})
        else:
            group = [c for c in root[0].children if c.name == "ALifeScenarioNPCArchetypesLimitsPerPlayerRank"]
            if len(group) != 1:
                failures.append({"error": "Director ranked array missing or duplicated"})
            else:
                ranks = {r.name for r in group[0].children}
                if ranks != {f"[{i}]" for i in range(4)}:
                    failures.append({"error": "Director rank node inventory changed",
                                     "actual": sorted(ranks)})
                for rank, rank_name in enumerate(RANKS):
                    rank_path = ["ALifeScenarioNPCArchetypesLimitsPerPlayerRank", f"[{rank}]"]
                    target = dict(prototype="ALifeDirectorPreset", nodePath=rank_path,
                                  key="Rank", sourceRaw=f"ERank::{rank_name}")
                    status = check_point(target, roots)
                    if status["status"] != "exact":
                        failures.append({"error": "Player rank label mismatch", "rank": rank,
                                         "finding": status})
                    rank_nodes = [r for r in group[0].children if r.name == f"[{rank}]"]
                    if len(rank_nodes) != 1:
                        continue
                    restrictions = [r for r in rank_nodes[0].children if r.name == "Restrictions"]
                    if len(restrictions) != 1:
                        failures.append({"error": "Rank restrictions missing", "rank": rank})
                        continue
                    slots = {r.name for r in restrictions[0].children}
                    if slots != {f"[{i}]" for i in range(16)}:
                        failures.append({"error": "Rank slot inventory drift", "rank": rank,
                                         "actual": sorted(slots)})

                    for slot, agent in enumerate(AGENTS):
                        path = rank_path + ["Restrictions", f"[{slot}]"]
                        owner = ("alife.humanGroupSize." + rank_name.lower()) if slot == 0 else                             ("alife." + PARAMETER_SUFFIXES[slot])
                        field = ("ALifeDirectorPreset", *path, "MaxCount")
                        mapped = grouped.get(field)
                        if mapped is None:
                            failures.append({"error": "ALife MaxCount mapping missing",
                                             "rank": rank, "slot": slot})
                            continue
                        op = ("scaleIntegerHalfUp" if slot == 0
                              else "scaleIntegerHalfUpPreserveZero")
                        if mapped["parameterId"] != owner or mapped["operation"] != op or                            mapped["file"] != FILE or mapped["scope"] != "baseGame" or                            mapped.get("difficultySid"):
                            failures.append({"error": "ALife manifest owner/operation drift",
                                             "rank": rank, "slot": slot,
                                             "actual": mapped})
                        raw = check_point(mapped, roots)
                        tag = check_point(dict(prototype="ALifeDirectorPreset",
                            nodePath=path, key="AgentType", sourceRaw=f"EAgentType::{agent}"), roots)
                        record = {"rank": rank_name, "agentType": agent, "slot": slot,
                                  "parameterId": mapped["parameterId"],
                                  "maxCountStatus": raw["status"],
                                  "agentTypeStatus": tag["status"],
                                  "expectedRaw": mapped["sourceRaw"],
                                  "actualRaw": raw.get("actualRaw")}
                        records.append(record)
                        if raw["status"] != "exact" or tag["status"] != "exact":
                            failures.append({"error": "Original rank/slot value or agent identity differs",
                                             "finding": record})
    original_zeros = [r for r in records if r["actualRaw"] == "0"]
    if {(r["rank"], r["slot"]) for r in original_zeros} != {("Newbie", 2), ("Newbie", 5)}:
        failures.append({"error": "Two authored Newbie zero ceilings drifted", "zeros": original_zeros})
    if len(records) != 64:
        failures.append({"error": "Source rank/species row count drift", "total": len(records)})
    output = {
        "audit": "original ALife Director rank/AgentType/MaxCount structure",
        "gameVersion": baseline["gameVersion"],
        "archiveSha256": {"base": sha256(args.base_zip), "dlc1": sha256(args.dlc1_zip)},
        "sourceCfgSha256": file_sha, "source": FILE,
        "status": "PASS" if not failures else "FAIL",
        "total": len(records),
        "sourceValuesExact": sum(x["maxCountStatus"] == "exact" for x in records),
        "agentTypeExact": sum(x["agentTypeStatus"] == "exact" for x in records),
        "authoredZeros": original_zeros,
        "countsByParameter": dict(sorted(Counter(r["parameterId"] for r in records).items())),
        "failures": failures,
        "rows": records,
        "limitations": "No standalone engine behavior, DLC1 lair population, quest/lair timing, mod merge, or PAK/gameplay tests."
    }
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(json.dumps(output, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print("ALife original Director source:", output["status"], f"{len(records)}/64 rows;",
          f"{output['agentTypeExact']}/64 AgentType labels;",
          f"{output['sourceValuesExact']}/64 raw MaxCount values")
    for failure in failures[:30]:
        print("  INVESTIGATE", json.dumps(failure, ensure_ascii=False))
    return bool(failures)


if __name__ == "__main__":
    sys.exit(main())
