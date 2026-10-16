#!/usr/bin/env python3
"""Source-only validation of 32 ordinary human corpse-loot GAME test cases.

No game state, probability, drop frequency or runtime patch acceptance is
certified. Distinguish ordinary consumable generators from difficulty corpse
SmartLoot packages and separately cloned bonus ammo. CorpseStashSID overrides
are deliberately OUT of the generic NPC experiment.
"""
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
TESTS = ROOT / "docs/generators/npc-corpse-ingame-loot-test-matrix.csv"


def read_csv(path):
    with path.open(encoding="utf-8", newline="") as stream:
        return list(csv.DictReader(stream))


def main():
    tests = read_csv(TESTS)
    assert len(tests) == 32, ("NPC in-game source scope drift", len(tests))
    assert len({row["case_id"] for row in tests}) == 32
    assert all(row["expected_cfg_signal"] and row["status"] in
               ("NOT_RUN", "PASS", "FAIL", "INCONCLUSIVE") for row in tests)
    assert [row["test_scope"] for row in tests] == (
        ["GENERIC_NPC"] * 30 + ["CONTROL_INTERACTION"] * 2), (
        "Generic human tests must precede cross-control interactions")

    catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
    controls = {p["id"]: p for p in catalog["parameters"]}
    for control in ("loot.npcMedicalChance", "loot.npcCorpseBonus",
                    "loot.npcWeaponAmmoSupply"):
        assert control in controls and controls[control]["tab"] == "loot"
    medical = controls["loot.npcMedicalChance"]["slider"]
    ammo = controls["loot.npcWeaponAmmoSupply"]["slider"]
    assert (medical["min"], medical["max"]) == (0, 2)
    assert (ammo["min"], ammo["max"]) == (0, 3)

    expected_packages = {"MEDICINE_AMMO": ["NPC_Medicine_Smart", "NPC_Ammo_Smart"],
                         "WATER": ["NPC_Water_Smart"], "NONE": []}
    vanilla = controls["loot.npcCorpseBonus"]["vanilla"]
    assert vanilla == {"Novice": expected_packages["MEDICINE_AMMO"],
                       "Stalker": expected_packages["WATER"],
                       "Veteran": [], "Master": []}, (
        "Difficulty-to-human-corpse bonus packages changed")
    producers = read_csv(DATA / "npc-medical-production-redirect-manifest.csv")
    assert len(producers) == 33
    allowed = {"CloseCombat", "Heavy", "Noon", "Recon", "Sniper", "Stormtrooper"}
    source_for_role = {}
    role_cases = defaultdict(list)
    layers = Counter()

    for row in tests:
        case = row["case_id"]
        role = row["role"]
        archetype = row["archetype_sid"]
        loadout = row["loadout_sid"]
        package = row["corpse_package"]
        difficulty = row["reference_difficulty"]
        assert role in allowed and archetype and loadout
        assert difficulty in vanilla and package in expected_packages, case
        assert not any(token in archetype.lower() or token in loadout.lower()
                       for token in ("_prolog", "mykluha", "generalnpc_mik", "quest",
                                     "scripted", "generalzombie")), (
            case, "Excluded scripted/named/zombie consumer")
        matches = [p for p in producers
                   if p["prototype"] == loadout
                   and archetype in p["external_consumers"].split(" | ")]
        assert len(matches) == 1, (case, "Missing exact 33-link standard-human path")
        match = matches[0]
        assert match["target_value"] == "TCD_" + match["source_value"]
        assert match["key"] == "ItemGeneratorPrototypeSID"
        assert match["file"] == "GameData/ItemGeneratorPrototypes.cfg"
        layer = row["source_layer"]
        assert layer in ("STANDARD_CONSUMABLE", "CORPSE_SMART_LIST",
                         "CORPSE_AMMO_SMART"), (case, layer)
        layers[layer] += 1

        if layer == "STANDARD_CONSUMABLE":
            assert row["source_sid"] == match["source_value"], case
            if row["test_scope"] == "GENERIC_NPC":
                assert row["item_type"] == "Medical"
                assert package == "NONE" and difficulty in ("Veteran", "Master")
                role_cases[(role, difficulty)].append(row)
            else:
                assert case.startswith("NPC-MED-PLUS-CORPSE-")
                assert difficulty == "Novice" and row["value"] == "0"
        elif layer == "CORPSE_SMART_LIST":
            assert row["source_sid"] == "EconomyDifficulty.CorpseSmartLoot.GeneralNPC"
            assert row["item_type"] == "CorpseBonus"
            if row["stage"] == "baseline":
                assert expected_packages[package] == vanilla[difficulty], case
        else:
            assert row["source_sid"] == "NPC_Ammo_Smart" and row["item_type"] == "Ammo"
            assert package == ("WATER" if row["stage"] == "negative"
                               else "MEDICINE_AMMO"), case
            assert difficulty == ("Stalker" if row["stage"] == "negative"
                                   else "Novice"), case

        control = row["control_id"]
        if row["stage"] == "baseline":
            assert not control and not row["value"], case
        elif row["stage"] == "choice":
            assert control == "loot.npcCorpseBonus", case
            assert row["value"] == package, case
            assert expected_packages[package] != vanilla[difficulty], case
        elif layer == "CORPSE_AMMO_SMART":
            assert control == "loot.npcWeaponAmmoSupply", case
            value = float(row["value"])
            assert ammo["min"] <= value <= ammo["max"]
            assert value == (0 if row["stage"] == "zero" else 3), case
        else:
            assert control == "loot.npcMedicalChance", case
            value = float(row["value"])
            assert medical["min"] <= value <= medical["max"]
            assert value == (0 if row["stage"] in ("zero", "interaction")
                             else 2), case

        if row["status"] == "NOT_RUN":
            assert not row["actual_in_game"].strip(), case
        else:
            detail = row["actual_in_game"]
            assert "evidence=" in detail and "save=" in detail and "NPC=" in detail, (
                case, "Evidence and NPC identity required before passing")

    assert len(role_cases) == 7, role_cases.keys()
    assert set(role for role, diff in role_cases if diff == "Veteran") == allowed
    assert ("Recon", "Master") in role_cases
    for group, records in role_cases.items():
        assert [r["stage"] for r in records] == ["baseline", "zero", "high"], group
        assert [r["value"] for r in records] == ["", "0", "2"], group
    assert layers == {"STANDARD_CONSUMABLE": 23, "CORPSE_SMART_LIST": 5,
                      "CORPSE_AMMO_SMART": 4}, layers
    assert {r["case_id"] for r in tests if r["stage"] == "negative"} == {
        "NPC-AMMO-BONUS-STALKER-WATER-NEG"}
    assert {r["corpse_package"] for r in tests
            if r["test_scope"] == "CONTROL_INTERACTION"} == {
        "NONE", "MEDICINE_AMMO"}
    assert {r["stage"] for r in tests if r["source_layer"] == "CORPSE_SMART_LIST"
            } == {"baseline", "choice"}

    statuses = Counter(r["status"] for r in tests)
    print("PASS: 32 unmodified-on-game NPC test records, 30 standard and 2 interactions")
    print("PASS: 7 role/difficulty three-case baseline/0x/2x experiments; 33-link source allowlist")
    print("PASS: difficulty corpse SmartLoot set and 43-item bonus ammo clone kept distinct")
    print("PASS: no named/Prologue/zombie/corpse override or shared global pool edits")
    print("GAME STATUS: " + ", ".join(
        f"{state}={statuses[state]}" for state in
        ("NOT_RUN", "PASS", "FAIL", "INCONCLUSIVE")))
    print("PENDING: actual CFG output, clean corpse RNG trials, Wine and in-game behavior")


if __name__ == "__main__":
    main()
