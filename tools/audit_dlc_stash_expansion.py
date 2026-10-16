#!/usr/bin/env python3
"""Source-only DLC1 Stashes production gate; no gameplay or Wine assertions."""
import csv
import hashlib
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
DOCS = ROOT / "docs/generators"
SERVICES = ROOT / "src/TrueCustomDifficulty/Services"


def rows(p):
    with p.open(encoding="utf-8", newline="") as stream:
        return list(csv.DictReader(stream))


def check(predicate, message):
    if not predicate:
        raise AssertionError(message)


all_dlc = rows(DOCS / "dlc1-stash-generic-actors.csv")
eligible = rows(DATA / "dlc1-stash-eligible-actor-manifest.csv")
source = rows(DATA / "cheap-no-fresh-stash-item-entries.csv")
safe = [a for a in all_dlc
        if a["CandidateClass"] == "ORDINARY_SOURCE_CANDIDATE_NOT_APPROVED"]
allowed_smart = {
    "StashFood_Cheap", "StashFood_Smart", "StashGrenade_Cheap",
    "StashGrenade_Smart", "StashMedicine_Cheap", "StashMedicine_Smart",
    "Stash_AmmoAll_Cheap", "Stash_AmmoNATO_Smart",
    "Stash_AmmoNATO_Smart_MainLoot", "Stash_AmmoSNG_Smart",
    "Stash_AmmoSNG_Smart_MainLoot", "Stash_Ammo_Smart_Cheap",
    "Stash_Ammo_Smart_CommonRare"
}
allowed_generators = {
    "", "GamePass_Stash_ItemGenerator_Cheap",
    "GamePass_Stash_ItemGenerator_Common_Var1",
    "GamePass_Stash_ItemGenerator_Rare",
    "GamePass_Stash_ItemGenerator_CheapNoFresh"
}
check(len(all_dlc) == 196 and len(safe) == 112 and len(eligible) == 112,
      "DLC1 actor count drift")
check(len({x["ActorSID"] for x in eligible}) == 112, "duplicate actor")
by_id = {x["ActorSID"]: x for x in safe}
check({x["ActorSID"] for x in eligible} == set(by_id),
      "DLC1 allowlist differs from explicitly ordinary actor candidates")
for actor in eligible:
    sid = actor["ActorSID"]
    raw = by_id[sid]
    check(re.fullmatch(r"[0-9A-F]{32}", sid) is not None, "invalid ActorSID")
    check(raw["LevelName"] == "WorldMap_WP" and
          raw["ClueVariableSID"] == "EmptyInherited" and
          not raw["OtherAuthoredItemTypeFlag"] and
          not raw["UnresolvedReferences"], "unsafe ordinary actor " + sid)
    check(actor["ClueVariableSID"] == raw["ClueVariableSID"] and
          actor["OriginalStashPrototypeSIDs"] == raw["StashPrototypeSIDs"] and
          actor["OriginalItemGeneratorSIDs"] == raw["ItemGeneratorSIDs"],
          "source actor list changed " + sid)
    check(set(actor["OriginalStashPrototypeSIDs"].split(",")) <= allowed_smart,
          "unexpected SmartLoot source in DLC actor " + sid)
    check(actor["OriginalItemGeneratorSIDs"] in allowed_generators,
          "unexpected ItemGenerator source " + sid)

types = Counter(x["OriginalItemGeneratorSIDs"] for x in eligible)
check(types == {
    "GamePass_Stash_ItemGenerator_Cheap": 24,
    "GamePass_Stash_ItemGenerator_Common_Var1": 59,
    "GamePass_Stash_ItemGenerator_Rare": 19,
    "GamePass_Stash_ItemGenerator_CheapNoFresh": 6,
    "": 4
}, "source generator class count drift")
check(hashlib.sha256((DATA / "cheap-no-fresh-stash-item-entries.csv").read_bytes())
      .hexdigest() == "ec883cba352f2bf21da023ecd6ec9da3c93cef2a2785252db8fc3f6c8a4e2317",
      "CheapNoFresh source rows differ from exact 2026-10-16 base CFG dump")
check(len(source) == 35 and
      len({(x["CategoryIndex"], x["ItemIndex"]) for x in source}) == 35,
      "35-row no-fresh source inventory changed")
check(len({x["CategoryIndex"] for x in source}) == 12,
      "CheapNoFresh category count drift")
check(all(x["ItemSID"] and (x["Chance"] or x["Weight"]) and
          x["MinCount"] and x["MaxCount"] for x in source),
      "CheapNoFresh empty source field")
check(any(x["ItemSID"] == "Medkit" for x in source) and
      any(x["ItemSID"] == "Beer" for x in source) and
      any(x["ItemSID"] == "GrenadeRGD5" for x in source),
      "CheapNoFresh classes changed")

target = rows(DATA / "medium-stash-production-redirect-manifest.csv")
check(len(target) == 450, "Original medium manifest assignment count drift")
dlc_actors = {x["ActorSID"] for x in all_dlc}
hits = [r for r in target
        if r["physical_container_sid"] in dlc_actors and
        r["redirect_kind"] == "DIRECT_CONTAINER_SETTING"]
check(len(hits) == 69 and all(
    r["file"] == "DLCGameData/DLC1/SpawnActorPrototypes.cfg" and
    r["node_path"] == "ItemGeneratorSettings.[0]" and
    r["key"] == "PrototypeSID"
    for r in hits), "DLC1 medium redirection path/regression")
check(len([r for r in hits if r["physical_container_sid"] in by_id]) == 59,
      "safe medium redirect subset changed")

smart_code = (SERVICES / "CfgPatchWriterService.StashSmartLoot.cs").read_text(encoding="utf-8")
dlc_code = (SERVICES / "CfgPatchWriterService.StashDlc1.cs").read_text(encoding="utf-8")
writer = (SERVICES / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
project = (ROOT / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
tuning = (SERVICES / "CfgPatchWriterService.StashTuning.cs").read_text(encoding="utf-8")
test = (ROOT / "tests/LootSpecialGeneratorsContract/Program.cs").read_text(encoding="utf-8")
check('DlcSmartLootActorFile = "DLCGameData/DLC1/SpawnActorPrototypes.cfg"' in smart_code,
      "SmartLoot wrong DLC file scope")
check("if (hasDlc1)" in smart_code and "DlcActors.Value" in smart_code,
      "DLC SmartLoot guard removed")
check("IsApprovedDlcStashTierRedirect" in smart_code and
      writer.count("!IsApprovedDlcStashTierRedirect") == 3,
      "tier redirects not safety-filtered")
check("WriteDlcCheapNoFreshStashes(" in writer and
      "GetOrAddReferencedRoot(TcdCheapNoFreshSid" in dlc_code and
      "WriteExtraStashGear(items, all, \"Cheap\", firstFreeCategory: 12)" in dlc_code,
      "CheapNoFresh source clone contract missing")
check("if (!hasDlc1)" in dlc_code and
      'new[] { "ItemGeneratorSettings", "[0]" }' in dlc_code,
      "CheapNoFresh direct DLC-only patch path missing")
for resource in ["dlc1-stash-eligible-actor-manifest.csv",
                 "cheap-no-fresh-stash-item-entries.csv"]:
    check("Data\\" + resource in project, "resource missing from csproj: " + resource)
check('or "Beer" => "loot.stashMixDrinks"' in tuning,
      "Beer must use existing Drinks slider")
check("VerifyDlcStashExpansion" in test and
      "DlcEligibleActorIds" in test,
      "missing generated-CFG DLC contract tests")
print("PASS: DLC1 ordinary source roster 112, base/dlc protection,")
print("      35 exact CheapNoFresh source rows, 69 medium file-path repairs,")
print("      required DLC guards, embedded resources, tests and UI-preserving writer.")
print("NOT VERIFIED: C# compilation, packaged Unreal patch loading or gameplay.")
