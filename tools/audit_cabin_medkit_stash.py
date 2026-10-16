#!/usr/bin/env python3
"""Audit the two authored non-unique post-Richter Cabin Medkits.

Optional --source-zip validates the exact actor and generator root definitions
in the uploaded 2026-10-16 GameData CFG dump. Always checks the live embedded
manifest, C# writer hook and generated-CFG contract test.
"""
import argparse
import csv
import re
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
SOURCE = ROOT / "src/TrueCustomDifficulty/Services"
MANIFEST = DATA / "cabin-direct-medkit-item-entries.csv"
ACTOR_NAMES = {
    "1C2FD1C141D6DA11E518758CC1CD36AE": "LesserZone_Cabin_StashGenerator1_",
    "59FDC1CD4D0D094270C695A18D84373A": "LesserZone_Cabin_StashGenerator3_",
}


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def source_actor(spawn, sid):
    match = re.search(rb"(?m)^" + sid.encode() + rb" : struct\.begin(?:\s*\r?\n)", spawn)
    check(match is not None, "missing exact Cabin actor " + sid)
    after = re.search(rb"(?m)^[0-9A-F]{32} : struct\.begin", spawn[match.end():])
    return spawn[match.start():match.end() + after.start() if after else len(spawn)]


def source_root(generators, root):
    key = root.encode()
    match = re.search(rb"(?m)^" + re.escape(key)
                      + rb" : struct\.begin[^\r\n]*\r?\n", generators)
    check(match is not None, "missing exact Cabin ItemGenerator " + root)
    after = re.search(rb"(?m)^\S[^\r\n]* : struct\.begin", generators[match.end():])
    return generators[match.start():match.end() + after.start() if after else len(generators)]


def check_cfg_archive(zpath, entries):
    with zipfile.ZipFile(zpath) as archive:
        actors = archive.read("GameData/SpawnActorPrototypes.cfg")
        generators = archive.read("GameData/ItemGeneratorPrototypes.cfg")
    for row in entries:
        aid, root = row["ActorSID"], row["GeneratorRoot"]
        actor = source_actor(actors, aid)
        check(b"   LevelName = WorldMap_WP" in actor
              and b"   SpawnType = ESpawnType::ItemContainer" in actor
              and b"   ClueVariablePrototypeSID = EmptyInherited" in actor,
              "Cabin physical actor is not an ordinary WorldMap ItemContainer: " + aid)
        check(("PrototypeSID = " + row["GeneratorSID"]).encode() in actor,
              "Cabin actor generator SID mismatched " + aid)
        check(b"StashMedicine_Smart" in actor, "Cabin SmartLoot family missing " + aid)
        root_cfg = source_root(generators, root)
        check(("   SID = " + row["GeneratorSID"]).encode() in root_cfg,
              "Cabin source key/SID spelling mismatch " + root)
        check(b"ItemGeneratorPrototypeSID = GamePass_Stash_ItemGenerator_Cheap" in root_cfg,
              "Cabin regular Cheap subgenerator missing " + root)
        check(b"ItemPrototypeSID = Medkit" in root_cfg
              and b"Chance = 1.0" in root_cfg
              and b"MinCount = 1" in root_cfg
              and b"MaxCount = 1" in root_cfg,
              "Original 100% chance 1x Medkit source changed " + root)
        section = root_cfg.split(b"      [1] : struct.begin", 1)
        check(len(section) == 2 and
              b"Category = EItemGenerationCategory::Consumable" in section[1]
              and b"ItemPrototypeSID = Medkit" in section[1],
              "Expected original direct Medkit under ItemGenerator.[1] " + root)
    print("PASS: supplied original CFG reproduces two fixed Cabin Medkits.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-zip", type=Path)
    args = parser.parse_args()
    with MANIFEST.open(encoding="utf-8", newline="") as f:
        items = list(csv.DictReader(f))
    check(len(items) == 2 and len({x["ActorSID"] for x in items}) == 2,
          "Cabin fixed Medkit list must contain two distinct physical actors")
    for e in items:
        sid = e["ActorSID"]
        check(sid in ACTOR_NAMES, "Unapproved Cabin actor " + sid)
        check(e["GeneratorRoot"] == ACTOR_NAMES[sid] + sid, "Cabin source root changed")
        check(e["GeneratorSID"] == e["GeneratorRoot"].replace("Cabin_", "Cabin _"),
              "Source original SID spacing changed")
        check((e["CategoryIndex"], e["ItemIndex"], e["ItemSID"],
               e["Chance"], e["MinCount"], e["MaxCount"])
              == ("1", "0", "Medkit", "1.0", "1", "1"),
              "Unexpected fixed Cabinet Medkit source row")

    project = (ROOT / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text()
    writer = (SOURCE / "CfgPatchWriterService.cs").read_text()
    extension = (SOURCE / "CfgPatchWriterService.StashCabin.cs").read_text()
    test = (ROOT / "tests/LootSpecialGeneratorsContract/Program.cs").read_text()
    check("Data\\cabin-direct-medkit-item-entries.csv" in project,
          "Cabin manifest not embedded in single-file executable")
    check(writer.count("WriteCabinDirectMedkits(") == 1,
          "Cabin writer must run once through the production CFG path")
    check("TuneStashSelectionField" in extension
          and "TuneStashMedicalCountField" in extension
          and "SmartLootActorResource" in extension,
          "Cabin medical tuning/positive actor allowlist missing")
    check("VerifyCabinDirectMedical();" in test
          and "missed guaranteed Cabin Medkit" in test
          and "Food slider rewrote an unrelated Cabin direct Medkit" in test,
          "Cabin generated-CFG contract regression missing")
    if args.source_zip:
        check_cfg_archive(args.source_zip, items)
    print("PASS: Cabin direct item source manifest, embedded resource, writer and CFG contract.")
    print("NOT VERIFIED: .NET build, Wine test or gameplay on previously generated containers.")


if __name__ == "__main__":
    main()
