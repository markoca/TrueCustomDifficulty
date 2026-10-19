#!/usr/bin/env python3
"""Incremental production route/CFG destination check for Stashes added since
the frozen 248-source final mapping inventory. Prior domains are not re-audited.
This verifies code structure and declared destinations, NOT a .NET/game run.
"""
from __future__ import annotations

import argparse
import csv
import json
import re
from pathlib import Path

from audit_final_cfg_mapping import method_routes

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TrueCustomDifficulty"
DATA = SRC / "Data"
OLD = ROOT / "tools" / "final_cfg_mapping_inventory.json"


def require(test: bool, message: str) -> None:
    if not test:
        raise AssertionError(message)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, help="Optional CSV with 35 new source routes")
    args = parser.parse_args()
    old = json.loads(read(OLD))
    current = json.loads(read(DATA / "parameters.json"))
    baseline = json.loads(read(DATA / "cfg-writer-baseline.json"))
    definitions = {p["id"]: p for p in current["parameters"]}
    direct = {p["parameterId"] for p in baseline["editPoints"]}
    routed = method_routes(baseline["special"])
    previous = set(old["parameters"])
    added = set(definitions) - previous

    require(old["counts"]["catalog"] == 248 and len(previous) == 248,
            "Frozen 248-source audited inventory changed")
    require(len(definitions) == baseline["supportedParameterCount"] == 283,
            "Current catalog must have 283 registered sources")
    require(len(added) == 35 and all(definitions[x]["tab"] == "stashes" for x in added),
            "Current mapping delta is no longer exactly 35 Stashes sources")
    require(not (added & direct), "A newly introduced source acquired direct edit points")
    require(len(direct) == 213 and len(baseline["editPoints"]) == 1230,
            "Prior source-audited direct mapping changed; re-audit old scope")
    # Reuse earlier source audit, but prove that the previously verified leaf
    # identities have not drifted. Counts alone can hide silent reassignment.
    direct_by_id = {}
    for point in baseline["editPoints"]:
        direct_by_id.setdefault(point["parameterId"], []).append(point)
    previous_direct = {pid: row for pid, row in old["parameters"].items()
                       if row["writerType"] == "direct"}
    require(len(previous_direct) == 213 and set(previous_direct) == direct,
            "Previously source-audited direct parameter ownership changed")
    keys = ("scope", "file", "prototype", "key", "operation", "sourceRaw")
    for pid, record in previous_direct.items():
        points = direct_by_id[pid]
        leaf = record["representativeDirectLeaf"]
        require(len(points) == record["directEditPointCount"],
                "Previously audited direct leaf count changed: " + pid)
        require(any(all(point[k] == leaf[k] for k in keys) and
                    point.get("nodePath", []) == leaf.get("nodePath", [])
                    for point in points),
                "Previously audited direct CFG destination/value drift: " + pid)
    require(len(routed) == 70 and set(routed) | direct == set(definitions)
            and not set(routed) & direct, "Writer route coverage/ownership drift")

    mix = {"loot.stashMix" + x for x in
           ("Ammo", "Medical", "Food", "Drinks", "Grenades", "Coupons")}
    medical = set(baseline["special"]["stashMedicalProduction"]["parameterIds"])
    gear = {"loot.stashGearEnabled", "loot.stashGearCoveragePercent",
            "loot.stashGearWeaponWeight", "loot.stashGearArmorWeight",
            "loot.stashGearArtifactWeight", "loot.stashGearAttachmentWeight",
            "loot.stashGearBonusChance"}
    tier = {"loot.standardStashChanceScale", "loot.rareStashChanceScale",
            "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
            "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
            "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
            "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance"}
    require([len(mix), len(medical), len(gear), len(tier)] == [6, 12, 7, 10],
            "Changed Stashes source cohorts")
    require(added == mix | medical | gear | tier
            and sum(map(len, (mix, medical, gear, tier))) == 35,
            "Unidentified or doubly owned incremental source ID")
    registered_mix = baseline["special"]["stashContentAndGearProduction"]["parameterIds"]
    require(set(registered_mix) == mix | gear | tier and len(registered_mix) == 23,
            "Extra Gear/content special registration drift")

    main_writer = read(SRC / "Services/CfgPatchWriterService.cs")
    smart_writer = read(SRC / "Services/CfgPatchWriterService.StashSmartLoot.cs")
    gear_writer = read(SRC / "Services/CfgPatchWriterService.StashGearCoverage.cs")
    removal_writer = read(SRC / "Services/CfgPatchWriterService.StashAuthoredWeapons.cs")
    tuning = read(SRC / "Services/CfgPatchWriterService.StashTuning.cs")
    entrypoint = main_writer.split("public CfgWriteResult Write(", 1)[1].split(
        "private static void ValidateBaseline", 1)[0]

    production = (
        "WriteCheapStashProduction", "WriteMediumStashAmmoProduction",
        "WriteExpensiveStashProduction", "WriteWorldStashSmartLoot",
        "WriteStashGearCoverage", "WriteStashAuthoredWeaponRemovals")
    for method in production:
        require(re.search(r"\b" + method + r"\s*\(", entrypoint) is not None,
                "Writer method is not actually called: " + method)
    require("StashMixParameterIds.Any(modified.ContainsKey)" in tuning
            and "StashMedicalParameterIds.Any(id =>" in tuning
            and "StashTierChanceParameterIds.Any(id =>" in tuning,
            "Stashes tier clone dispatch guard lost one source cohort")
    require("StashMixParameterIds.Concat(StashMedicalParameterIds)" in smart_writer,
            "SmartLoot writer does not consume mix and medical source IDs")

    prototype = "GameData/ItemGeneratorPrototypes.cfg"
    smart = "GameData/StashPrototypes.cfg"
    actors = "GameData/SpawnActorPrototypes.cfg"
    dlc_actors = "DLCGameData/DLC1/SpawnActorPrototypes.cfg"
    for route in ("cheapStashProduction", "mediumStashAmmoProduction",
                  "expensiveStashProduction"):
        require(baseline["special"][route]["file"] == prototype,
                "Tier generator CFG destination drift: " + route)
    for method in production[:3]:
        fragment = main_writer.split("private static int " + method + "(", 1)
        require(len(fragment) == 2
                and "GetDocument(documents, config.File)" in fragment[1].split(
                    "\n    private ", 1)[0],
                "Concrete tier generator destination lost: " + method)
    require('GetDocument(documents, "GameData/StashPrototypes.cfg")' in smart_writer
            and '("GameData/SpawnActorPrototypes.cfg", actors)' in smart_writer
            and "DlcSmartLootActorFile" in smart_writer,
            "Stash SmartLoot source/physical-actor output destinations drift")
    require('GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg")' in gear_writer
            and "BaseProtectedActorFile : DlcSmartLootActorFile" in gear_writer.replace(
                "\n", " "),
            "Private Extra Gear generator/actor output destination drift")
    require('GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg")' in removal_writer
            and 'document.AddRemovalNode(row.GeneratorSid' in removal_writer,
            "Authored regular weapon removals lost their concrete CFG target")
    require('fileName.EndsWith("Prototypes.cfg"' in main_writer
            and '_patch_{PatchSuffix}.cfg' in main_writer
            and 'File.WriteAllText(outputPath' in entrypoint,
            "Native BPatch path materialization lost")

    rows = []
    for pid in sorted(added):
        p = definitions[pid]
        if pid in mix or pid in medical:
            group = "Content Priority" if pid in mix else "Medical Priority/Chance/Quantity"
            methods = production[:4]
            files = (prototype, smart, actors, dlc_actors)
            require(prototype in {t["file"] for t in p["targets"]},
                    "Stash source catalog has no native generator target: " + pid)
        elif pid in gear:
            group = "Extra Stash Gear"
            methods = ("WriteStashGearCoverage",)
            if pid == "loot.stashGearEnabled":
                methods += ("WriteStashAuthoredWeaponRemovals",)
            files = (prototype, actors, dlc_actors)
            require({t["file"] for t in p["targets"]} & {prototype, actors, dlc_actors},
                    "Extra Gear catalog has no output target: " + pid)
        else:
            group = "Standard/Rare native Chance"
            methods = (production[1],) if pid == "loot.standardStashChanceScale" or pid.startswith(
                "loot.mediumStash") else (production[2],)
            files = (prototype, actors, dlc_actors)
            require(prototype in {t["file"] for t in p["targets"]},
                    "Tier Chance catalog has no native generator target: " + pid)

        # The registered method is a PRIMARY dispatch anchor. Composite tier
        # clone / SmartLoot fan-out is separately proven above and is not
        # misrepresented as an independent new UI control.
        require(set(routed[pid]).issubset(methods),
                "Source is registered under an unrelated production method: " + pid)
        require(p["support"]["status"] in ("confirmed", "runtime_test_required"),
                "Missing source evidence class: " + pid)
        rows.append({"parameterId": pid, "group": group, "primaryWriter": ";".join(
            sorted(routed[pid])), "productionWriters": ";".join(methods),
            "possibleCfgDestinations": ";".join(files),
            "sourceStatus": p["support"]["status"],
            "verified": "static route+destination, NOT runtime"})

    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        with args.out.open("w", newline="", encoding="utf-8") as out:
            writer = csv.DictWriter(out, fieldnames=list(rows[0]))
            writer.writeheader()
            writer.writerows(rows)
    print("PASS: 248 prior audited IDs reused + 35 added Stashes IDs")
    print("PASS: 213 old direct sources, 70 special routes, zero unmapped sources")
    print("PASS: 35/35 new controls -> called production writers -> concrete CFG destinations")
    print("NOT VERIFIED: C# build / real emitted patches / Unreal game runtime")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
