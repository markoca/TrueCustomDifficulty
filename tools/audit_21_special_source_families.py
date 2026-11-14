#!/usr/bin/env python3
"""Original-CFG compatibility gate for TCD's 2.1 special source families.

The inventory-only mode intentionally cannot claim 2.1 source certification.
Use both original outer 2.0 and 2.1 CFG ZIPs for byte-exact checks.
This verifies source *identity* and family routing, not Unreal BPatch loading.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "src/TrueCustomDifficulty/Data/cfg-writer-baseline.json"

# These are unchanged in the user's original 2.0 -> 2.1 CFG archives.
# Each exact 2.1 SHA prevents comparing two wrong or stale archives.
SOURCES = {
    "GameData/ItemGeneratorPrototypes.cfg": "4a06bc946eb9eaa7ff736421df7fd1bb6fe38fb91038bf5fffa005f680d64ac3",
    "DLCGameData/DLC1/ItemGeneratorPrototypes.cfg": "3a6ad1ef628ab6182591a9c1f9273d053ee1e338433a81402346f6354c700155",
    "GameData/TradePrototypes.cfg": "c5115350d67d91a329b5922f0a51e9abe02f91075e5fbe24c1b3b4fa48f540eb",
    "DLCGameData/DLC1/TradePrototypes.cfg": "7a72b5dfc112226c92c081e49ca6c3877b7cf00cd541964d07113253d37e31a5",
    "GameData/StashPrototypes.cfg": "d1d9c57037c5904c273b578fffea9e69d371362c983cb38dc9f4f04630efa50a",
    "GameData/DifficultyPrototypes.cfg": "43d041e6895fbf8443d2c3abcfa502dc360dd53cd3ea9acbaca38e5e6e5d0aac",
    "GameData/FastTravelPrototypes.cfg": "95db455818f1172be0f59aa5c6982cd394e311bd9b1f03a1dd586d621eb60e4b",
    "GameData/CorpseClueStashPrototypes.cfg": "d178f73c0ebc058407683efb3d20734bc2b0b0cb54bfb6e1bf6224823f5e5a16",
    "GameData/ALifePrototypes/ALifeDirectorScenarioPrototypes.cfg": "f44252ad581b0c5018c64c5148d20aa2674340a9dabc8568c9d97ffdce307b21",
    "GameData/ObjPrototypes.cfg": "ffdaa4e881db1f4f76e5cd7d4072711066748df6375143558253a9103d60cf9f",
    "GameData/ItemPrototypes.cfg": "f6b98e5931a3c936cd9f279aac23b39b45527b1995fc7008b0a7940eed790d42",
}
# The DLC1 item prototype source IS changed (handled by the 35-leaf direct
# comparison), so it must never be on the unchanged special-source allowlist.
CHANGED_DLC = "DLCGameData/DLC1/ItemPrototypes.cfg"
EXPECTED_DLC_SHA = "43d7d35a3eae5d6c33d53bca1f2199b8ea15a72755c0ee78b18b2a5582a80776"

# Some special writers also address SpawnActor, which changed in 2.1.
# Byte equivalence is insufficient for that family: use the actor-root audits.
ACTOR_DEPENDENT = {
    "cheapStashProduction", "mediumStashAmmoProduction",
    "expensiveStashProduction", "destructibleAmmoProduction",
    "destructibleGrenadeProduction", "npcMedicalLootProduction",
    "crateSpawnProduction", "worldWeaponShuffle", "looseWorldLootProduction",
    "blueprintShuffle", "worldAttachmentShuffle", "worldDetectors",
    "worldF1GrenadeBoxes", "stashContentAndGearProduction",
    "stashMedicalProduction", "worldArmorRemoved",
    "looseUniqueWeaponShuffle",
}


def cfg_inventory(path: Path) -> dict[str, bytes]:
    result = {}
    with zipfile.ZipFile(path) as outer:
        for entry in outer.namelist():
            if not entry.endswith(".zip"):
                continue
            with zipfile.ZipFile(io.BytesIO(outer.read(entry))) as child:
                for name in child.namelist():
                    if name.endswith(".cfg"):
                        if name in result:
                            raise ValueError("Duplicate original CFG: " + name)
                        result[name] = child.read(name)
    if not result:
        raise ValueError("No decoded CFGs in outer archive: " + str(path))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--old", type=Path)
    parser.add_argument("--new", type=Path)
    parser.add_argument("--inventory-only", action="store_true")
    parser.add_argument("--json-out", type=Path)
    args = parser.parse_args()
    if not args.old and not args.new:
        args.inventory_only = True
    if args.inventory_only == bool(args.old):
        parser.error("Use either --inventory-only OR both --old and --new")
    if not args.inventory_only and not args.new:
        parser.error("--new is required with --old")
    if args.inventory_only and args.new:
        parser.error("--new cannot be supplied in inventory-only mode")

    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    special = baseline["special"]
    assert len(special) == 25, "Special CFG writer group census drift"
    assert ACTOR_DEPENDENT <= set(special), "Actor-dependent writer group removed"
    assert baseline["gameVersion"] == "2.1", "2.1 target/source metadata mismatch"
    assert "GameData/SpawnActorPrototypes.cfg" not in SOURCES
    assert "DLCGameData/DLC1/SpawnActorPrototypes.cfg" not in SOURCES
    assert CHANGED_DLC not in SOURCES
    report = {
        "mode": "INVENTORY_ONLY" if args.inventory_only else "ORIGINAL_2_1_SOURCE",
        "unchangedSpecialSourceFiles": len(SOURCES),
        "actorDependentSpecialGroups": sorted(ACTOR_DEPENDENT),
        "globalBaseline": baseline["gameVersion"],
        "sourceStatus": "NOT_RUN",
        "gameplayStatus": "NOT_RUN",
    }
    if not args.inventory_only:
        old = cfg_inventory(args.old)
        new = cfg_inventory(args.new)
        assert (len(old), len(new)) == (603, 605), "Unexpected archive census"
        for path, expected in SOURCES.items():
            if path not in old or path not in new:
                raise ValueError("Missing original source " + path)
            before = hashlib.sha256(old[path]).hexdigest()
            after = hashlib.sha256(new[path]).hexdigest()
            if before != after or after != expected:
                raise ValueError("2.1 special source changed: " + path)
        if hashlib.sha256(new[CHANGED_DLC]).hexdigest() != EXPECTED_DLC_SHA:
            raise ValueError("DLC1 2.1 ItemPrototypes source drift")
        if old[CHANGED_DLC] == new[CHANGED_DLC]:
            raise ValueError("DLC1 2.1 change unexpectedly absent")
        report["sourceStatus"] = "PASS_BYTE_IDENTICAL"
        report["sourceFiles"] = SOURCES
        report["changedDlcPrototypeSource"] = CHANGED_DLC
    if args.json_out:
        args.json_out.parent.mkdir(parents=True, exist_ok=True)
        args.json_out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))
    print("NOT VERIFIED: SpawnActor merges, newly added content or game runtime")
    print("The source identity check does not establish in-game 2.1 BPatch acceptance.")


if __name__ == "__main__":
    main()
