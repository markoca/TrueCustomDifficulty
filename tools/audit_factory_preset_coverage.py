#!/usr/bin/env python3
"""Every shipped factory preset stores every catalog control.

Controls added after the last thematic tuning (2026-11-16: MUTANT AGGRESSION,
ARTIFACT HUNTING, HUMAN TACTICS / HUMAN AWARENESS, supply reductions) are
stored at the preset's reference-difficulty vanilla value, i.e. exactly what
loading produced while they were omitted. Previous stock files upgrade only
when byte-identical (PreNewControlsFactoryBlobs).
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
catalog = json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))
catalog = catalog["parameters"] if isinstance(catalog, dict) else catalog
by_id = {p["id"]: p for p in catalog}

NEW_IDS = {pid for pid in by_id if pid.startswith((
    "combat.mutantAttackRate.", "combat.mutantAbilityRate.", "environment.artifact"))}
NEW_IDS |= {"combat.mutantSpecialAbilityRate", "combat.npcGrenadeRate", "combat.npcSuppressionRate",
            "combat.npcFlankRate", "combat.npcPushRate", "combat.npcCamperPunishRate",
            "combat.npcAccuracy", "combat.npcSightRange", "combat.npcHearingRange",
            "combat.npcAlertMemory", "loot.genericCorpseReduction", "loot.genericStashReduction"}
assert len(NEW_IDS) == 39, len(NEW_IDS)  # 40 before RESPAWN RATE retired
# 2026-11-17: weather AI coefficients and per-item medical HEAL TIME seconds.
NEW_IDS |= {"combat.npcFogVisibility", "combat.npcRainNoise", "combat.npcStormNoise",
            "health.bandageHealTime", "health.medkitHealTime", "health.armyMedkitHealTime",
            "health.ecoMedkitHealTime", "loot.looseArtifactsRemoved",
            "loot.medicalBoxArmyMedkitWeight", "loot.stashSmartStockedChance",
            "loot.stashSmartShortChance", "loot.stashSmartLootEnabled",
            "loot.npcSmartLootEnabled", "loot.stashSmartMedsOdds", "loot.stashSmartAmmoOdds",
            "loot.stashSmartFoodDrinksOdds", "loot.stashSmartGrenadesOdds"}

presets = sorted((APP / "Presets").glob("*.json"))
assert len(presets) == 12, len(presets)
for path in presets:
    raw = path.read_text(encoding="utf-8")
    data = json.loads(raw)
    assert raw == json.dumps(data, indent=2, ensure_ascii=False) + "\n", path.name
    values = data["values"]
    assert set(values) == set(by_id), (path.name, sorted(set(by_id) ^ set(values)))
    ref = data["referenceDifficulty"]
    for pid in NEW_IDS:
        p = by_id[pid]
        expected = p["vanilla"].get(ref, p["vanilla"].get("All"))
        want = ({"kind": "toggle", "boolean": expected} if p.get("control") == "toggle"
                else {"kind": "slider", "number": expected})
        assert values[pid] == want, (path.name, pid, values[pid])
    for pid, stored in values.items():
        p = by_id[pid]
        if stored.get("kind") == "slider" and p.get("slider") and not data.get("drinkDurationOverride"):
            assert p["slider"]["min"] <= stored["number"] <= p["slider"]["max"], (path.name, pid, stored)

pds = (APP / "Services/PresetDirectoryService.cs").read_text(encoding="utf-8")
assert pds.count("PreNewControlsFactoryBlobs") == 3 and pds.count("PreWeatherHealTimeFactoryBlobs") == 3
assert pds.count("PreRespawnRetirementFactoryBlobs") == 3 and pds.count("PreLooseArtifactFactoryBlobs") == 3
assert pds.count("PreMedicalBoxFactoryBlobs") == 3 and pds.count("PreSmartLootNeedFactoryBlobs") == 3
assert pds.count("PreSmartLootSwitchFactoryBlobs") == 3
assert pds.count("PreNpcSmartLootFactoryBlobs") == 3
assert pds.count("PreSmartOddsFactoryBlobs") == 3
assert len(pds.split("PreNewControlsFactoryBlobs =", 1)[1].split("};", 1)[0].split('"] = "')) == 13

print(f"PASS: 12 factory presets store all {len(by_id)} controls; the 56 newest at reference vanilla; stock upgrade hashes kept")
