#!/usr/bin/env python3
"""Every shipped factory preset stores every catalog control.

Controls added after the 2026-10-19 tuning (2026-11-16: MUTANT AGGRESSION,
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
assert len(NEW_IDS) == 15, len(NEW_IDS)  # 40 before RESPAWN RATE retired; ATTACKS / ABILITIES sliders all folded into knobs (2026-11-18)
# 2026-11-17: weather AI coefficients and per-item medical HEAL TIME seconds.
NEW_IDS |= {"combat.npcFogVisibility", "combat.npcRainNoise", "combat.npcStormNoise",
            "health.bandageHealTime", "health.medkitHealTime", "health.armyMedkitHealTime",
            "health.ecoMedkitHealTime", "loot.looseArtifactsRemoved",
            "loot.medicalBoxArmyMedkitWeight", "loot.stashSmartStockedChance",
            "loot.stashSmartShortChance", "loot.stashSmartLootEnabled",
            "loot.npcSmartLootEnabled", "loot.stashSmartMeds", "loot.stashSmartAmmo",
            "loot.stashSmartFoodDrinks", "loot.stashSmartGrenades"}

# 2026-11-17 thematic pass: role values for those controls (absent = vanilla).
THEMATIC = json.loads((ROOT / "tools/factory_preset_thematic_2026_11_17.json").read_text(encoding="utf-8"))
# 2026-11-18: LAIR POPULATION role values (absent = vanilla 1×).
LAIR = json.loads((ROOT / "tools/factory_preset_lair_population_2026_11_18.json").read_text(encoding="utf-8"))
NEW_IDS |= {"alife.lairPopulation.human", "alife.lairPopulation.mutant", "alife.lairRespawnTime"}
# 2026-11-18: ZONE WEATHER + TRADER RESTOCK role values (absent = reference vanilla).
ZONE = json.loads((ROOT / "tools/factory_preset_zone_weather_2026_11_18.json").read_text(encoding="utf-8"))
NEW_IDS |= {"environment.emissionFrequency", "environment.stormFrequency",
            "environment.emissionWarning", "economy.traderRestockHours"}

# 2026-11-18: SUMMON RATE role values (absent = vanilla 1×).
SUMMONS = json.loads((ROOT / "tools/factory_preset_summons_2026_11_18.json").read_text(encoding="utf-8"))
# 2026-11-18: SUMMON RATE retired the same day into per-move knobs (blobs kept for upgrades).
MOVES = json.loads((ROOT / "tools/factory_preset_move_knobs_2026_11_18.json").read_text(encoding="utf-8"))
NEW_IDS |= {pid for pid in by_id if pid.startswith("combat.mutantMoveRate.")}

ATTACK_KNOBS = json.loads((ROOT / "tools/factory_preset_attack_knobs_2026_11_18.json").read_text(encoding="utf-8"))
ABILITY_KNOBS = json.loads((ROOT / "tools/factory_preset_ability_knobs_2026_11_18.json").read_text(encoding="utf-8"))
presets = sorted((APP / "Presets").glob("*.json"))
assert len(presets) == 12, len(presets)
for path in presets:
    raw = path.read_text(encoding="utf-8")
    data = json.loads(raw)
    assert raw == json.dumps(data, indent=2, ensure_ascii=False) + "\n", path.name
    values = data["values"]
    assert set(values) == set(by_id), (path.name, sorted(set(by_id) ^ set(values)))
    ref = data["referenceDifficulty"]
    role = dict(THEMATIC["values"][data["name"]] if data["name"] in THEMATIC["values"] else THEMATIC["values"][path.stem])
    role.update(LAIR["values"][path.stem])
    role.update(ZONE["values"][path.stem])
    role.update(MOVES["values"][path.stem])
    # 2026-11-18: ABILITIES sliders folded into their knobs (knob × slider, Master 2× where halved).
    role.update({pid: new for stem, pid, _old, new in ABILITY_KNOBS["migrated"] if stem == path.stem})
    role.update({pid: new for stem, pid, _old, new in ATTACK_KNOBS["migrated"] if stem == path.stem})
    # 2026-11-18 attack-only pass: role values of removed controls (roars, growls, ...) are gone.
    role = {k: v for k, v in role.items() if k in by_id}
    for pid in NEW_IDS | set(role):
        p = by_id[pid]
        expected = role.get(pid, p["vanilla"].get(ref, p["vanilla"].get("All")))
        want = ({"kind": "toggle", "boolean": expected} if p.get("control") == "toggle"
                else {"kind": "slider", "number": expected})
        assert values[pid] == want, (path.name, pid, values[pid])
    side = THEMATIC["sidecars"].get(path.stem, {})
    for key in ("traderSellOdds", "genericNpcRareGearEnabled", "genericNpcRareGearChance"):
        assert data.get(key) == side.get(key), (path.name, key, data.get(key))
    for pid in side.get("traderSellOdds", {}):
        assert values[pid] == {"kind": "toggle", "boolean": True}, (path.name, pid)
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
assert pds.count("PreThematicFactoryBlobs") == 3
assert pds.count("PreLairPopulationFactoryBlobs") == 3
assert pds.count("PreLairRefillFactoryBlobs") == 3
for _name, _hash in {**LAIR["previousStockBlobs"]}.items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in LAIR["previousStockBlobsBeforeRefill"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
assert pds.count("PreLairRespawnTimeFactoryBlobs") == 3
assert pds.count("PreZoneWeatherFactoryBlobs") == 3
assert pds.count("PrePseudodogAbilityFactoryBlobs") == 3 and '["Hardcore"] = "3bc16c0d89a73d5b5c8a4bc6a01c8d4221c0a7c4"' in pds
assert pds.count("PreSummonRateFactoryBlobs") == 3
assert pds.count("PreMoveKnobFactoryBlobs") == 3
ATTACK_ONLY = json.loads((ROOT / "tools/factory_preset_attack_only_2026_11_18.json").read_text(encoding="utf-8"))
assert pds.count("PreAttackOnlyFactoryBlobs") == 3
assert pds.count("PreAbilityKnobFactoryBlobs") == 3
assert pds.count("PreAttackKnobFactoryBlobs") == 3
CONTROLLER_DOGS = json.loads((ROOT / "tools/factory_preset_controller_dogs_2026_11_18.json").read_text(encoding="utf-8"))
assert pds.count("PreControllerDogsFactoryBlobs") == 3
for _name, _hash in CONTROLLER_DOGS["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in ATTACK_KNOBS["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in ABILITY_KNOBS["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in ATTACK_ONLY["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in MOVES["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in SUMMONS["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in ZONE["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in LAIR["previousStockBlobsBeforeRespawnTime"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in THEMATIC["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
assert len(pds.split("PreNewControlsFactoryBlobs =", 1)[1].split("};", 1)[0].split('"] = "')) == 13

print(f"PASS: 12 factory presets store all {len(by_id)} controls; the newest controls follow the 2026-11-17 thematic matrix; stock upgrade hashes kept")
