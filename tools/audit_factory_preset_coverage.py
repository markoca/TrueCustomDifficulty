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
VIBE = json.loads((ROOT / "tools/factory_preset_vibe_2026_11_18.json").read_text(encoding="utf-8"))
# Current later pass: scarce-but-survivable source budgets, not a simulated RNG rate.
SCARCITY = json.loads((ROOT / "tools/factory_preset_scarcity_refine_2026_11_18.json").read_text(encoding="utf-8"))
HEAL_TIMES = json.loads((ROOT / "tools/factory_preset_heal_time_2026_11_18.json").read_text(encoding="utf-8"))
HEAL_IDS = ("health.bandageHealTime", "health.medkitHealTime", "health.armyMedkitHealTime", "health.ecoMedkitHealTime")
BANDAGE_HP = json.loads((ROOT / "tools/factory_preset_bandage_hp_policy_2026_11_18.json").read_text(encoding="utf-8"))
ROLE_DIVERSITY = json.loads((ROOT / "tools/factory_preset_role_diversification_2026_11_19.json").read_text(encoding="utf-8"))
LOOSE_WORLD = json.loads((ROOT / "tools/factory_preset_world_removal_policy_2026_11_19.json").read_text(encoding="utf-8"))
HUMAN_ROLES = json.loads((ROOT / "tools/factory_preset_human_roles_2026_11_19.json").read_text(encoding="utf-8"))
FOUR_DOMAINS = json.loads((ROOT / "tools/factory_preset_four_domain_balance_2026_11_19.json").read_text(encoding="utf-8"))
assert set(FOUR_DOMAINS["roles"]) == set(VIBE["values"]) and len(FOUR_DOMAINS["ids"]) == 11
RADIATION = json.loads((ROOT / "tools/factory_preset_radiation_lethality_2026_11_19.json").read_text(encoding="utf-8"))
OVERWEIGHT = json.loads((ROOT / "tools/factory_preset_overweight_2026_11_23.json").read_text(encoding="utf-8"))
RAD_ID = RADIATION["sourceId"]
assert RAD_ID == "survival.radiationDamage" and RADIATION["range"]["max"] == 25
assert set(RADIATION["roles"]) == set(VIBE["values"])
assert len({role["damageMultiplier"] for role in RADIATION["roles"].values()}) == 12
assert RADIATION["roles"]["Hardcore+"]["damageMultiplier"] == 25
assert RADIATION["roles"]["Hardcore"]["damageMultiplier"] < 25
HUMAN_IDS = tuple(HUMAN_ROLES["ids"])
assert len(HUMAN_IDS) == len(set(HUMAN_IDS)) == 12
assert set(HUMAN_ROLES["roles"]) == set(VIBE["values"]) == set(HUMAN_ROLES["previousStockBlobs"])
assert set(HUMAN_ROLES["tactics"]) | set(HUMAN_ROLES["awareness"]) == set(HUMAN_IDS)
assert len(HUMAN_ROLES["tactics"]) == len(HUMAN_ROLES["awareness"]) == 6
assert all(set(role["values"]) == set(HUMAN_IDS) and role["intent"]
           for role in HUMAN_ROLES["roles"].values())
LOOSE_WORLD_IDS = ("loot.worldDetectors", "loot.worldArmorRemoved", "loot.looseArtifactsRemoved")
assert set(LOOSE_WORLD["roles"]) == set(VIBE["values"])
assert set(LOOSE_WORLD["sourceIDs"]) == set(LOOSE_WORLD_IDS)
assert set(LOOSE_WORLD["previousStockBlobs"]) == {
    name for name, role in LOOSE_WORLD["roles"].items() if role["changes"]
}
assert sum(len(role["changes"]) for role in LOOSE_WORLD["roles"].values()) == 19
assert set(ROLE_DIVERSITY["roles"]) == set(VIBE["values"])
assert set(ROLE_DIVERSITY["previousStockBlobs"]) == set(VIBE["values"])
assert sum(len(x["changes"]) for x in ROLE_DIVERSITY["roles"].values()) == 363
assert len({pid for role in ROLE_DIVERSITY["roles"].values() for pid in role["changes"]}) == 84
assert all(len(role["changes"]) >= 25 and role["intent"] for role in ROLE_DIVERSITY["roles"].values())
assert set(BANDAGE_HP["values"]) == set(VIBE["values"])
assert BANDAGE_HP["values"]["Field Medic"] == 10
assert all(hp == 0 for name, hp in BANDAGE_HP["values"].items() if name != "Field Medic")
assert set(HEAL_TIMES["values"]) == set(VIBE["values"])
assert set(HEAL_TIMES["previousStockBlobs"]) == set(VIBE["values"])
assert set(SCARCITY["roles"]) == set(VIBE["values"])
assert set(SCARCITY["priorCurrentStockBlobs"]) == set(VIBE["values"])
assert set(VIBE["values"]) == {p.stem for p in (APP / "Presets").glob("*.json")}
assert len(VIBE["previousStockBlobs"]) == 12

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
    # Current role-vibe overlay supersedes the historical upgrade snapshots.
    # Check all 53 native mutant move rates per profile, not merely non-vanilla keys.
    vibe = VIBE["values"][path.stem]
    assert sum(pid.startswith("combat.mutantMoveRate.") for pid in vibe) == 53, path.stem
    assert VIBE["referenceDifficulty"][path.stem] == ref, path.stem
    role.update(vibe)
    # Source-native 2026-11-18 scarcity/refinement overlay wins over old balance
    # epoch matrices. These previous matrices stay immutable historical evidence.
    role.update(SCARCITY["roles"][path.stem]["changes"])
    role.update(HEAL_TIMES["values"][path.stem])
    role["health.bandageHealing"] = BANDAGE_HP["values"][path.stem]
    # The 2026-11-19 source-specific overlay supersedes historical snapshots.
    role.update(ROLE_DIVERSITY["roles"][path.stem]["changes"])
    role.update(LOOSE_WORLD["roles"][path.stem]["changes"])
    role.update(HUMAN_ROLES["roles"][path.stem]["values"])
    role[RAD_ID] = RADIATION["roles"][path.stem]["damageMultiplier"]
    role.update(FOUR_DOMAINS["roles"][path.stem]["values"])
    role.update(dict(zip(OVERWEIGHT["ids"], OVERWEIGHT["roles"][path.stem]["ratios"])))
    # 2026-11-18 attack-only pass: role values of removed controls (roars, growls, ...) are gone.
    role = {k: v for k, v in role.items() if k in by_id}
    for pid in NEW_IDS | set(role):
        p = by_id[pid]
        expected = role.get(pid, p["vanilla"].get(ref, p["vanilla"].get("All")))
        want = ({"kind": "toggle", "boolean": expected} if p.get("control") == "toggle"
                else {"kind": "slider", "number": expected})
        assert values[pid] == want, (path.name, pid, values[pid])
    # Supply masters now affect genuine generic human/stash chance sources.
    # Their source percentages stack with per-item and tier chance cuts, so
    # keep them modest: no wholesale 100% deletion of a second loot domain.
    def amount(pid):
        return values[pid]["number"]
    def enabled(pid):
        return values[pid]["boolean"]
    assert .05 <= amount("loot.genericCorpseReduction") <= .20, path.name
    assert .15 <= amount("loot.genericStashReduction") <= .25, path.name
    assert amount("loot.looseWorldLootRemoved") >= .5, path.name
    assert amount("loot.worldWeaponDensity") <= .7, path.name
    assert amount("environment.artifactCount") == 1, path.name
    assert all(amount(pid) == HUMAN_ROLES["roles"][path.stem]["values"][pid] for pid in HUMAN_IDS), path.name
    assert amount("combat.npcStormNoise") < amount("combat.npcRainNoise"), path.name
    assert amount("combat.npcSightRange") <= 2.5, path.name
    assert amount(RAD_ID) == RADIATION["roles"][path.stem]["damageMultiplier"], path.name
    assert amount("stamina.maxCarryWeight") == OVERWEIGHT["roles"][path.stem]["maxCarryKg"], path.name
    assert [amount(id) for id in OVERWEIGHT["ids"]] == OVERWEIGHT["roles"][path.stem]["ratios"], path.name
    assert all(amount(pid) == target for pid, target in FOUR_DOMAINS["roles"][path.stem]["values"].items()), path.name
    assert 4.5 <= amount(RAD_ID) <= 25, path.name
    assert all(enabled(id) is LOOSE_WORLD["roles"][path.stem]["values"][id] for id in LOOSE_WORLD_IDS), path.name
    assert amount("health.bandageHealing") == BANDAGE_HP["values"][path.stem], path.name
    assert amount("health.bandageBleeding") > 0, path.name
    assert not enabled("health.foodHealing"), path.name
    assert set(HEAL_TIMES["values"][path.stem]) == set(HEAL_IDS), path.name
    assert all(5 <= amount(pid) <= 10 for pid in HEAL_IDS), path.name
    assert amount(HEAL_IDS[0]) <= min(amount(pid) for pid in HEAL_IDS[1:]), path.name
    assert amount("economy.traderAmmoStock") >= .25, path.name
    assert amount("loot.npcMedicalChance") > 0, path.name
    if enabled("loot.stashSmartLootEnabled"):
        assert 0 <= amount("loot.stashSmartStockedChance") <= .30, path.name
        assert 0 < amount("loot.stashSmartShortChance") <= .65, path.name
        assert amount("loot.stashSmartStockedChance") <= amount("loot.stashSmartShortChance"), path.name
        assert any(enabled("loot.stashSmart" + category) for category in
                   ("Meds", "Ammo", "FoodDrinks", "Grenades")), path.name
    else:
        assert not any(pid.startswith("loot.stashSmart") for pid in
                       SCARCITY["roles"][path.stem]["changes"]), path.name
    if not enabled("loot.stashGearEnabled"):
        assert not any(pid.startswith("loot.stashGear") for pid in
                       SCARCITY["roles"][path.stem]["changes"]), path.name
    side = THEMATIC["sidecars"].get(path.stem, {})
    assert data.get("traderSellOdds") == FOUR_DOMAINS["roles"][path.stem]["traderSellOdds"], (path.name, "traderSellOdds")
    for key in ("genericNpcRareGearEnabled", "genericNpcRareGearChance"):
        assert data.get(key) == side.get(key), (path.name, key, data.get(key))
    for pid in FOUR_DOMAINS["roles"][path.stem]["traderSellOdds"]:
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
FALL = json.loads((ROOT / "tools/factory_preset_fall_damage_2026_11_18.json").read_text(encoding="utf-8"))
assert pds.count("PreFallDamageFactoryBlobs") == 3
assert pds.count("PreVibeFactoryBlobs") == 3
assert pds.count("PreScarcityRefineFactoryBlobs") == 3
assert pds.count("PreHealTimeFactoryBlobs") == 3
assert pds.count("PreBandageHpPolicyFactoryBlobs") == 3
assert pds.count("PreRoleDiversityFactoryBlobs") == 3
assert pds.count("PreWorldRemovalPolicyFactoryBlobs") == 3
assert pds.count("PreHumanRolesFactoryBlobs") == 3
for _name, _hash in HUMAN_ROLES["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in LOOSE_WORLD["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in ROLE_DIVERSITY["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
assert BANDAGE_HP["previousStockBlobs"] == {"Scavenger": "162438f40e6615d7e0488cc4535b53f3c7a536fa"}
assert '["Scavenger"] = "162438f40e6615d7e0488cc4535b53f3c7a536fa"' in pds
for _name, _hash in HEAL_TIMES["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in SCARCITY["priorCurrentStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in VIBE["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
for _name, _hash in FALL["previousStockBlobs"].items():
    assert f'["{_name}"] = "{_hash}"' in pds, _name
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

assert sum(len(role["changes"]) for role in SCARCITY["roles"].values()) == 140
assert all(role.get("intent") for role in SCARCITY["roles"].values())
print(f"PASS: 12 factory roles × {len(by_id)} settings; human tactics/awareness 12×12, 110 real retunes; corpse/stash, medical and world-removal policies preserved")
