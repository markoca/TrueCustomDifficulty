#!/usr/bin/env python3
"""Conservative stash source inventory audit, not a gameplay writer."""
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "docs" / "generators"
FILE = ROOT / "tools" / "regular-stash-source-inventory.json"
EXCLUDED = {"NPC_Ammo_Smart", "NPC_Medicine_Smart", "NPC_Water_Smart", "StashMedicine_Corpse", "StashVodka_Corpse", "empty"}
CONTROL = {"AMMO": "loot.stashMixAmmo", "MEDICAL": "loot.stashMixMedical", "FOOD": "loot.stashMixFood", "DRINKS": "loot.stashMixDrinks", "GRENADES": "loot.stashMixGrenades", "COUPONS": "loot.stashMixCoupons"}

def category(sid):
    if re.match(r"^A\d", sid): return "AMMO"
    for cat, sids in (
        ("MEDICAL", {"Bandage", "Medkit", "ArmyMedkit", "EcoMedkit", "ScientificMedkit", "AntiRad", "Antirad"}),
        ("FOOD", {"Bread", "Sausage", "CannedFood"}),
        ("DRINKS", {"Vodka", "Milk", "Water", "Energetic", "Beer"}),
    ):
        if sid in sids: return cat
    if sid.startswith("Grenade"): return "GRENADES"
    if sid.startswith("Money"): return "COUPONS"
    if sid.startswith(("GamePass", "Gamepass", "CNPPGamePass")): return "NESTED"
    raise AssertionError("Unreviewed, possibly quest/unique SID: " + sid)

def rows(path):
    lines = [line for line in path.read_text(encoding="utf-8").splitlines() if line.startswith("|")]
    assert len(lines) > 2 and re.match(r"^\|\s*-", lines[1]) is not None, path
    return [[c.strip().strip(chr(96)) for c in line.strip("|").split("|")] for line in lines[2:]]

doc = json.loads(FILE.read_text(encoding="utf-8"))
assert doc["tuningParameterIds"] == CONTROL
assert set(doc["excludedSmartLootPrototypes"]) == EXCLUDED - {"empty"}
summary = Counter()
for key, filename, column, exclude in (
    ("itemGeneratorCore", "WORLD-STASH-CORE-POOL-CATALOG.md", 4, set()),
    ("smartLootWorld", "SMARTLOOT-BRANCH-CATALOG.md", 8, EXCLUDED)):
    grouped = defaultdict(list)
    for row in rows(EVIDENCE / filename):
        if row[0] not in exclude:
            grouped[row[0]].append(row)
            category(row[column])
    expected = {e["sid"]: e for e in doc["items"][key]}
    assert set(grouped) == set(expected), (key, set(grouped) ^ set(expected))
    for sid, source_rows in grouped.items():
        entry = expected[sid]
        assert len(source_rows) == entry["rawRowCount"], sid
        assert dict(Counter(category(r[column]) for r in source_rows)) == entry["categoryRows"], sid
        assert sorted({r[column] for r in source_rows}) == entry["itemSids"], sid
        if key == "smartLootWorld":
            assert entry["consumerEdges"] >= entry["productionEdges"] >= 0, sid
            assert entry["excludedOrUnresolvedEdges"] == entry["consumerEdges"] - entry["productionEdges"], sid
            assert entry["rankCount"] == len({r[1] for r in source_rows}), sid
            assert entry["sourceGroupKinds"] == sorted({r[2] for r in source_rows}), sid
            assert entry["status"] == "tcd_production_actor_smartloot_clone", sid
            summary["smartConsumers"] += entry["consumerEdges"]
            summary["smartProduction"] += entry["productionEdges"]
        summary[key + "Rows"] += len(source_rows)
    summary[key + "Roots"] = len(grouped)

totals = doc["totals"]
assert summary["itemGeneratorCoreRoots"] == totals["coreGenerators"] == 17
assert summary["itemGeneratorCoreRows"] == totals["coreRows"] == 245
assert summary["smartLootWorldRoots"] == totals["smartGenerators"] == 13
assert summary["smartLootWorldRows"] == totals["smartRows"] == 991
assert summary["smartConsumers"] == totals["smartConsumerEdges"] == 2170
assert summary["smartProduction"] == totals["smartProductionEdges"] == 2143
assert len(totals["productionOnly"]) == 5
for sid in totals["productionOnly"]:
    e = next(e for e in doc["items"]["smartLootWorld"] if e["sid"] == sid)
    assert e["productionEdges"] == e["consumerEdges"] and e["excludedOrUnresolvedEdges"] == 0
params = {p["id"]: p for p in json.loads((ROOT / "src" / "TrueCustomDifficulty" / "Data" / "parameters.json").read_text(encoding="utf-8"))["parameters"]}
assert all(id in params and params[id]["tab"] == "stashes" for id in CONTROL.values())
writer = (ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.StashTuning.cs").read_text(encoding="utf-8")
assert "HasStashTuningChange" in writer and "StashMixParameterIds" in writer
smart = (ROOT / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.StashSmartLoot.cs").read_text(encoding="utf-8")
assert "WriteWorldStashSmartLoot(" in smart and "GetOrAddReferencedRoot(" in smart
assert 'StashPrototypeSID", string.Join(",", mapped)' in smart
assert doc["currentProductionActorAllowlist"]["allowed"] == 831
assert doc["currentProductionActorAllowlist"]["excludedSpecial"] == 71
for source, expected in [("smartloot-regular-world-actor-manifest.csv", 832), ("smartloot-world-source-entries.csv", 992)]:
    assert len((ROOT / "src/TrueCustomDifficulty/Data" / source).read_text(encoding="utf-8").splitlines()) == expected

print("PASS: 17 historic ItemGenerator cores / 245 source rows; 13 SmartLoot roots / 991 source rows.")
print("PASS: current user CFG dump: 831 strict actor allowlist, 71 blueprint/unique/quest generator-linked actors excluded.")
print("HISTORIC AUDIT ONLY: 2143 production edges from earlier snapshot; not an actor count for this supplied ZIP.")
print("NOT VERIFIED: actual C# build, resulting UE5 CFG clone behavior, saved-stash generation or in-game effects.")
