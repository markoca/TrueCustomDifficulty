#!/usr/bin/env python3
"""Factory role-coverage gate. Static source checks, not runtime/gameplay proof."""
import json
import re
from pathlib import Path
import sys as _sys; _sys.path.insert(0, str(__import__("pathlib").Path(__file__).resolve().parent))
from catalog_census import CATALOG, VISIBLE, HIDDEN, EDIT_POINTS, DIRECT_IDS

ROOT = Path(__file__).resolve().parents[1]
matrix = json.loads((ROOT/"tools/factory_profile_role_coverage_2026_10_19.json").read_text(encoding="utf-8"))
# Historical source-role matrix remains frozen; only active 2026-11-18
# scarcity overrides take precedence over its original stash categories.
scarcity = json.loads((ROOT/"tools/factory_preset_scarcity_refine_2026_11_18.json").read_text(encoding="utf-8"))
assert len(scarcity["roles"]) == 12
role_diversity = json.loads((ROOT/"tools/factory_preset_role_diversification_2026_11_19.json").read_text(encoding="utf-8"))
assert len(role_diversity["roles"]) == 12
catalog = json.loads((ROOT/"src/TrueCustomDifficulty/Data/parameters.json").read_text(encoding="utf-8"))
sources = {p["id"]:p for p in catalog["parameters"]}
names = matrix["profiles"]
assert len(names)==len(set(names))==12
assert len(sources)==CATALOG and matrix["sourceCount"]==288
# Historical 2026-10-19 role-target ledger covers original 288 IDs; the
# new 40 Trader Buys cells intentionally start neutral in every preset.
trader_buys = {p["id"]: p for p in sources.values() if p["id"].startswith("economy.traderBuys.")}
assert len(trader_buys)==40
trader_sells = {p["id"]: p for p in sources.values() if p["id"].startswith("economy.traderSells.")}
assert len(trader_sells)==31
assert len(matrix["targets"])==49
assert set(matrix["previousFactoryGitBlobs"])==set(names)
service=(ROOT/"src/TrueCustomDifficulty/Services/PresetDirectoryService.cs").read_text(encoding="utf-8")
assert "PreRoleCoverageFactoryBlobs.TryGetValue(name, out knownHash)" in service
assert "PreRoleCoverageFactoryBlobs.TryGetValue(name, out var preRoleCoverageHash)" in service
base=ROOT/"src/TrueCustomDifficulty/Presets"
assert {p.stem for p in base.glob("*.json")}==set(names)
for index,name in enumerate(names):
    d=json.loads((base/(name+".json")).read_text(encoding="utf-8"))
    hash=matrix["previousFactoryGitBlobs"][name]
    assert re.fullmatch(r"[0-9a-f]{40}",hash) and f'["{name}"] = "{hash}"' in service
    # Presets moved to game 2.1; the matrix records its 2.0.5 authoring baseline.
    assert d["profileVersion"]==1 and d["name"]==name and d["gameVersion"]==catalog["source"]["gameVersion"]
    assert d["referenceDifficulty"]==matrix["roleResults"][name]["ref"]
    assert d["drinkDurationOverride"] is True and d["medicalOverride"]==(name=="Field Medic")
    assert set(d["values"])==set(sources)
    for tid,p in trader_buys.items():
        assert d["values"][tid]=={"kind":"toggle","boolean":p["vanilla"]["All"]},(name,tid)
    for tid,p in trader_sells.items():
        assert d["values"][tid]=={"kind":"toggle","boolean":True},(name,tid)
    def get(id):
        e=d["values"][id]
        return e["number"] if e["kind"]=="slider" else e["boolean"]
    for id,values in matrix["targets"].items():
        assert len(values)==12,(id,len(values))
        p=sources[id]
        entry=d["values"][id]
        value=values[index]
        if id.startswith("loot.stashMedical") and id.endswith("Bias"):
            # 2026-11-16 MEDICAL COMPOSITION: Weight mirrors the Chance cell.
            value=d["values"][id[:-4]+"ChanceScale"]["number"]
        # The role matrix is an immutable 2026-10-19 authoring snapshot;
        # newer source-valid stash/gear scarcity decisions explicitly supersede it.
        value = scarcity["roles"][name]["changes"].get(id, value)
        value = role_diversity["roles"][name]["changes"].get(id, value)
        if p["control"]=="toggle":
            assert entry=={"kind":"toggle","boolean":value},(name,id)
        else:
            assert entry=={"kind":"slider","number":value},(name,id,entry,value)
            slider=p["slider"]
            assert slider["min"]-1e-6<=value<=slider["max"]+1e-6,(name,id,value)
            if slider.get("stepMode")=="absolute":
                steps=(value-slider["min"])/slider["step"]
                assert abs(steps-round(steps))<1e-6,(name,id,value)
    assert get("loot.stashGearEnabled")!=get("loot.worldWeaponShuffle"),name
    assert get("loot.stashGearEnabled")==matrix["roleResults"][name]["gear"],name
    if get("loot.stashGearEnabled"):
        assert get("loot.stashGearCoveragePercent")>0 and get("loot.stashGearBonusChance")>0
    for ordered in (("survival.hungerLowThreshold","survival.hungerMediumThreshold","survival.hungerStrongThreshold"),
                    ("survival.sleepLowThreshold","survival.sleepMediumThreshold","survival.sleepStrongThreshold"),
                    ("survival.bleedingMediumThreshold","survival.bleedingStrongThreshold")):
        assert all(get(a)<get(b) for a,b in zip(ordered,ordered[1:])),(name,ordered)
    for prefix in ("loot.stashMedkit","loot.stashArmyMedkit"):
        assert get(prefix+"MinCount")<=get(prefix+"MaxCount"),(name,prefix)
    for id in ("loot.mediumStashMedicalChance","loot.mediumStashDrinksChance","loot.mediumStashCouponsChance",
               "loot.expensiveStashAmmoChance","loot.expensiveStashMedicalChance","loot.expensiveStashFoodChance",
               "loot.expensiveStashDrinksChance","loot.expensiveStashGrenadesChance"):
        assert 0<=get(id)<=1,(name,id)
    assert matrix["roleResults"][name]["newChanges"]>=30,name
print(f"PASS: 12 factory roles / {CATALOG} catalog sources each (40 neutral Trader Buys + 31 Trader Sells) / 49 curated control dimensions")
print("PASS: native bounds, absolute snaps, ordered stages, stash chance, exclusive gear/shuffle, pristine-only migration map")
print("PASS: current 2026-11-19 role-specific overlays checked; historical native scope and role invariants preserved")
print("NOT TESTED: WPF publish, actual .NET preset import, BPatch, Wine or UE gameplay.")
