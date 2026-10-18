#!/usr/bin/env python3
"""Current Stashes tab catalog -> WPF -> writer -> factory preset contract.

Static source audit, not a .NET compile or in-game probability test.
"""
import csv
import json
import re
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / "src/TrueCustomDifficulty"
data = src / "Data"
def read(path):
    return path.read_text(encoding="utf-8")
parameters = json.loads(read(data / "parameters.json"))["parameters"]
catalog = {p["id"]: p for p in parameters}
baseline = json.loads(read(data / "cfg-writer-baseline.json"))
stash = {p["id"]: p for p in parameters if p["tab"] == "stashes"}
assert len(parameters) == baseline["supportedParameterCount"] == 281
assert len(stash) == 39
assert len(catalog) == len(parameters), "Duplicate parameter ID"
assert Counter(p["group"] for p in stash.values()) == {
    "Stash Content": 17,
    "Stash Medical Composition": 12,
    "Stash Gear": 7,
    "Stashes": 3,
}
assert sum(p.get("exposeInUi", True) for p in stash.values()) == 35

ui = read(src / "Controls/AggregateVisualControl.cs")
vm = read(src / "ViewModels/MainViewModel.cs")
pvm = read(src / "ViewModels/ParameterViewModel.cs")
tuning = read(src / "Services/CfgPatchWriterService.StashTuning.cs")
gear = read(src / "Services/CfgPatchWriterService.StashGearCoverage.cs")
writer = read(src / "Services/CfgPatchWriterService.cs")
rewards = ["loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity", "loot.expensiveStashAmmoQuantity"]
mix = [
    "loot.stashMixAmmo", "loot.stashMixMedical", "loot.stashMixFood",
    "loot.stashMixDrinks", "loot.stashMixGrenades", "loot.stashMixCoupons",
    "loot.cheapStashGrenadeChance", "loot.cheapStashGrenadeQuantity",
    "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
    "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
    "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
    "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance",
    "loot.expensiveStashMoneyChance",
]
medical = [
    "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
    "loot.stashMedicalArmyMedkitBias", "loot.stashMedicalAntiRadBias",
    "loot.stashMedicalBandageChanceScale", "loot.stashMedicalMedkitChanceScale",
    "loot.stashMedicalArmyMedkitChanceScale", "loot.stashMedicalAntiRadChanceScale",
    "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
    "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount",
]
bonus = [
    "loot.stashGearEnabled", "loot.stashGearCoveragePercent",
    "loot.stashGearWeaponWeight", "loot.stashGearArmorWeight",
    "loot.stashGearArtifactWeight", "loot.stashGearAttachmentWeight",
    "loot.stashGearBonusChance",
]
families = {
    "StashRewardProfile": rewards, "StashContentMix": mix,
    "StashMedicalComposition": medical, "StashGear": bonus
}
for name, expected in families.items():
    match = re.search(r'"' + re.escape(name) + r'"\s*=>\s*new\[\]\s*\{(.*?)\}', ui, re.S)
    assert match, "Missing UI family: " + name
    actual = re.findall(r'"(loot\.[A-Za-z0-9.]+)"', match.group(1))
    assert actual == expected, f"Missing/misordered UI family controls: {name}: {actual}"
assert set(stash) == set(rewards + mix + medical + bonus)
for pid in bonus:
    assert f'"{pid}"' in vm, "Missing MainViewModel member: " + pid
    assert f'["{pid}"] = "StashGear"' in pvm, "Missing PVM family: " + pid
assert 'P("loot.stashGearAttachmentWeight")' in ui or (
    '("loot.stashGearAttachmentWeight", "ATTACHMENTS")' in ui)
assert 'DrawSourceKnob(dc, P(weights[i].Item1)' in ui
assert 'DrawTogglePill(dc, P("loot.stashGearEnabled")' in ui
assert 'P("loot.stashGearBonusChance")' in ui
assert 'P("loot.stashGearCoveragePercent")' in ui

# Renderer computes paint width as host minus 30 DIP. At host width 550
# it paints two knob rows, so WPF must allocate tall height.
assert 'Math.Min(630.0, Math.Max(1.0, ActualWidth - 30.0))' in ui
assert 'var narrowMix = width < 540.0;' in ui
assert 'DrawText(dc, "WEIGHT SHARE"' in ui
assert 'var chanceY = narrowMix ? 593.0 : 429.0;' in ui
assert 'narrowMix ? 390.0 : 224.0' in ui
assert '=> width < 570.0 ? 670.0 : 502.0;' in ui
for host in (520, 540, 550, 569, 570, 600, 760):
    narrow = min(630.0, max(1.0, host - 30.0)) < 540.0
    assigned_height = 670.0 if host < 570.0 else 502.0
    assert assigned_height == (670.0 if narrow else 502.0)

for pid in bonus[1:]:
    assert stash[pid]["requiresToggle"] == bonus[0]
assert stash[bonus[0]]["control"] == "toggle" and stash[bonus[0]]["vanilla"]["All"] is False
for pid in bonus[2:6]:
    assert stash[pid]["slider"] == {"min": 0, "max": 3, "step": 0.1, "stepMode": "absolute"}
assert stash[bonus[1]]["slider"]["step"] == stash[bonus[6]]["slider"]["step"] == 1
assert 'ChooseStashGearClasses(' in gear
assert 'all, actor.ActorSid, code, eligibleAttachments.Length > 0' in gear
assert 'item.SetAssignment("Chance", itemChance)' in gear
assert 'for (var categoryIndex = 0; categoryIndex < selectedClasses.Count;' in gear
assert 'selectedClasses.Count > 2' in gear
assert 'remaining.RemoveAt(i);' in gear
assert 'item.SetAssignment("MinCount", "1")' in gear
assert 'item.SetAssignment("MaxCount", "1")' in gear
assert 'WriteStashAuthoredWeaponRemovals(allById, documents)' in writer
assert 'WriteStashGearCoverage(allById, hasDlc1, documents)' in writer
assert 'loot.stashGearAttachmentWeight' in tuning
assert 'loot.stashGearAttachmentWeight' in gear
routes = set(x["parameterId"] for x in baseline["editPoints"])
def walk(o):
    if isinstance(o, dict):
        for k, v in o.items():
            if k in catalog:
                routes.add(k)
            walk(v)
    elif isinstance(o, list):
        for x in o:
            walk(x)
    elif isinstance(o, str) and o in catalog:
        routes.add(o)
walk(baseline["special"])
assert routes == set(catalog), "Missing writer route IDs: " + str(sorted(set(catalog) - routes))
# Match all 21 production-routed Stashes IDs exactly and in their
# deliberate order. The 7 Extra Gear members include the ONE master ID.
# The two standalone Basic grenade controls and Rare coupons route via
# their other concrete writers; do not fake a duplicate here.
stash_routes = baseline["special"]["stashContentAndGearProduction"]["parameterIds"]
expected_routes = mix[:6] + [bonus[1], bonus[0], *bonus[2:]] + mix[8:16]
assert stash_routes == expected_routes, (
    "Stashes writer routing diverged from its 6 mix / 7 gear / 8 chance sources")
assert len(stash_routes) == len(set(stash_routes)) == 21
assert stash_routes.count("loot.stashGearEnabled") == 1
# Four Weight + four Quantity + four Chance-scale native medical sources.
medical_routes = baseline["special"]["stashMedicalProduction"]["parameterIds"]
assert medical_routes == medical[:4] + medical[8:12] + medical[4:8]
assert len(medical_routes) == len(set(medical_routes)) == 12

# All twelve shipped presets must have complete, typed values. Unchanged
# new stash parameters retain vanilla semantics instead of being dropped.
names = ("Anomaly Runner", "Field Medic", "Hardcore", "Hardcore+",
         "Immersive", "Mercenary", "Mutant Hunter", "Outcast",
         "Scavenger", "Smuggler", "Tactical", "Wanderer")
for name in names:
    preset = json.loads(read(src / "Presets" / (name + ".json")))
    values = preset["values"]
    assert set(values) == set(catalog), f"{name} does not match catalog"
    for pid in bonus:
        assert values[pid]["kind"] == ("toggle" if pid == bonus[0] else "slider")
    assert values["loot.stashGearEnabled"]["boolean"] is False

def csv_rows(filename):
    with (data / filename).open(encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f))
ranked = csv_rows("stash-gear-actor-ranks.csv")
removals = csv_rows("stash-authored-weapon-removal-manifest.csv")
protection = csv_rows("base-stash-protected-actor-manifest.csv")
attachments = csv_rows("stash-bonus-attachment-progressions.csv")
assert len(ranked) == 915 and Counter(r["Scope"] for r in ranked) == {"base": 803, "dlc1": 112}
assert len(removals) == 13 and len(protection) == 106
assert len(attachments) == 19
t1_weapon_ids = {r["ItemSID"] for r in csv_rows("gear-progression-ranks.csv")
                 if r["ItemClass"] == "Weapon" and r["ProgressionTier"] == "T1"}
assert len(t1_weapon_ids) == 8
t1_attachments = {r["ItemSID"] for r in attachments
                  if t1_weapon_ids.intersection(r["FittingWeaponsSIDs"].split(";"))}
assert t1_attachments == {
    "GunAK74_MagIncreased", "GunUDP_MagIncreased", "RU_GLaunch_1"}
assert not set(r["ActorSID"] for r in removals) & set(r["ActorSID"] for r in protection)
assert all(r["ProgressionTier"] in ("T2", "T3", "T4") for r in attachments)

print("PASS: Stashes 39 catalog parameters / 4 exact UI families / 7 bonus controls")
print("PASS: responsive 4-to-2 knob layout, all writer routes, 12 complete presets")
print("PASS: 13 original regular removals exclude protected actors; 19 audit-backed attachments")
print("LIMIT: static contract; no .NET/Wine/native RNG or unopened-stash validation")
