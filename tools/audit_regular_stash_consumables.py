#!/usr/bin/env python3
"""Guard the gear randomization backlog and source-backed direct consumables.

This is a source/writer static audit. Optional --source-zip also verifies the
separately archived 80-row source inventory and case-sensitive item identity.
No gameplay or probability guarantees are inferred.
"""
import argparse
import collections
import csv
import re
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / "docs/generators"
DATA = ROOT / "src/TrueCustomDifficulty/Data"
CODE = ROOT / "src/TrueCustomDifficulty/Services"
EXPECT = {"OTHER_AUTHORED": 45, "MEDICAL": 12, "BOOSTER": 9,
          "AMMO": 11, "GRENADE": 2, "MEDICAL_SID_CASE_REVIEW": 1}


def table(path):
    with path.open(encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f))


def check(ok, text):
    if not ok:
        raise AssertionError(text)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--source-zip", type=Path)
    args = p.parse_args()
    all_rows = table(DOC / "regular-stash-direct-additions-2026-10-16.csv")
    check(len(all_rows) == 80 and dict(collections.Counter(
        r["ItemClass"] for r in all_rows)) == EXPECT,
        "Original 80-row direct source corpus has changed")

    gear = [r for r in all_rows if r["ItemClass"] == "OTHER_AUTHORED"]
    saved = table(DOC / "stash-gear-unique-randomization-backlog.csv")
    check(len(saved) == 45 and len({r["ActorSID"] for r in saved}) == 41
          and len({r["ItemPrototypeSID"] for r in saved}) == 31,
          "45-row reserved gear backlog changed")
    original_cols = list(gear[0].keys())
    check([{k: r[k] for k in original_cols} for r in gear] ==
          [{k: r[k] for k in original_cols} for r in saved],
          "Reserved gear rows are not source-identical to original audit")
    check(all(r["WorkStatus"] == "RESERVED_NOT_RANDOMIZED" for r in saved),
          "Authored gear was marked processed without a separate randomization design")
    classes = collections.Counter(r["FuturePool"] for r in saved)
    check(classes == {"WEAPON": 14, "MAGAZINE": 9, "OPTIC": 11,
                      "SUPPRESSOR": 3, "HELMET": 7, "ARMOR": 1},
          "Future gear categories drifted")

    medis = [r for r in all_rows if r["ItemClass"] == "MEDICAL"]
    already = [r for r in medis if r["CoverageStatus"] == "ALREADY_TUNED_CABIN"]
    new = [r for r in medis if r["CoverageStatus"] ==
           "ADDITIONAL_GENERIC_SOURCE_UNTUNED"]
    check(len(already) == 2 and len(new) == 10 and
          len({r["ActorSID"] for r in new}) == 6,
          "Expected two existing Cabin + ten additional canonical medical rows")
    embedded = table(DATA / "regular-stash-direct-medical-entries.csv")
    expected = [[r[k] for k in ("ActorSID", "GeneratorRoot", "GeneratorSID",
                "CategoryIndex", "ItemIndex", "ItemPrototypeSID", "SourceChance",
                "SourceMinCount", "SourceMaxCount")] for r in new]
    actual = [[r[k] for k in ("ActorSID", "GeneratorRoot", "GeneratorSID",
               "CategoryIndex", "ItemIndex", "ItemSID", "Chance",
               "MinCount", "MaxCount")] for r in embedded]
    check(expected == actual and all(r["SourceChance"] == "1" for r in new),
          "Embedded 10-row medical source inventory drift")
    check(all(r["ItemPrototypeSID"] in
              {"Medkit", "ArmyMedkit", "EcoMedkit", "Bandage"} for r in new),
          "Unreviewed medical SID in direct suppression manifest")

    booster = [r for r in all_rows if r["ItemClass"] == "BOOSTER"]
    check(len(booster) == 9 and all(r["SourceChance"] == "1" and
          r["ItemPrototypeSID"] in {"Hercules", "PSYBlocker"} for r in booster),
          "Guaranteed booster inventory changed")
    lower = [r for r in all_rows if r["ItemClass"] == "MEDICAL_SID_CASE_REVIEW"]
    check(len(lower) == 1 and lower[0]["ItemPrototypeSID"] == "medkit" and
          lower[0]["SourceChance"] == "1",
          "Case-sensitive medkit review source changed")

    patch = (CODE / "CfgPatchWriterService.StashDirectMedical.cs").read_text()
    writer = (CODE / "CfgPatchWriterService.cs").read_text()
    project = (ROOT / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text()
    tests = (ROOT / "tests/LootSpecialGeneratorsContract/Program.cs").read_text()
    check(writer.count("WriteRegularStashDirectMedical(") == 1 and
          "SmartLootActorResource" in patch and
          "TuneStashSelectionField" in patch and
          "TuneStashMedicalCountField" in patch,
          "Direct medical source writer/allowlist check missing")
    check("Data\\regular-stash-direct-medical-entries.csv" in project,
          "Direct medical manifest not embedded in published app")
    check("VerifyRegularStashDirectMedical();" in tests and
          "gear.Length == 45" in tests and
          "reserved or unresolved item was rewritten" in tests,
          "The writer must have an actual generated-CFG negative gear/booster test")

    if args.source_zip:
        subprocess.run([sys.executable,
                        str(ROOT / "tools/audit_regular_stash_direct_additions.py"),
                        "--source-zip", str(args.source_zip)], check=True)
        with zipfile.ZipFile(args.source_zip) as z:
            txt = z.read("GameData/ItemPrototypes.cfg")
        check(re.search(rb"(?m)^Medkit\s+: struct\.begin", txt) is not None,
              "Canonical Medkit source missing")
        check(re.search(rb"(?m)^medkit\s+: struct\.begin", txt) is None,
              "Lowercase medkit now exists: review before medical scope changes")
        print("PASS source: uppercase Medkit prototype exists; lowercase medkit does not")

    print("PASS future shuffle: 45 authored gear/weapon rows / 41 actors, 31 unique SIDs.")
    print("PASS consumables: 12 canonical medical (2 Cabin + 10 added),")
    print("     9 non-tuned boosters, 1 unresolved lowercase medkit, exact 100% item chances.")
    print("NOT VERIFIED: real generator/category activation, packaged patch and gameplay.")


if __name__ == "__main__":
    main()
