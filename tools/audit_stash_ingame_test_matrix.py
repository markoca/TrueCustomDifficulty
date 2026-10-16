#!/usr/bin/env python3
"""Validate source provenance for the Stashes MANUAL in-game case matrix.

This audits references/ranges/guard exclusions only. It deliberately cannot
evaluate Unreal generation, game save state, RNG or completed physical tests.
"""
import csv
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
MATRIX = ROOT / "docs/generators/stash-ingame-loot-test-matrix.csv"

def rows(path):
    with path.open(encoding="utf-8", newline="") as stream:
        return list(csv.DictReader(stream))

def tokens(raw):
    return [item.strip() for item in raw.split(",") if item.strip()]

def main():
    tests = rows(MATRIX)
    assert len(tests) == 30, ("Source-pinned case coverage drift", len(tests))
    ids = [r["case_id"] for r in tests]
    assert len(ids) == len(set(ids)), "Duplicate test case"
    assert all(r["expected_cfg_signal"].strip() for r in tests)
    assert set(r["item_type"] for r in tests) == {
        "Medical", "Food", "Drinks", "Ammo", "Grenades", "Coupons"
    }

    catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
    parameters = {p["id"]: p for p in catalog["parameters"]}
    protected = {r["ActorSID"] for r in rows(
        DATA / "base-stash-protected-actor-manifest.csv")}
    actors = {r["ActorSID"]: r for r in rows(
        DATA / "smartloot-regular-world-actor-manifest.csv")}
    direct = {(r["ActorSID"], r["GeneratorRoot"]) for name in (
        "cabin-direct-medkit-item-entries.csv",
        "regular-stash-direct-medical-entries.csv"
    ) for r in rows(DATA / name)}
    tier_manifests = [
        "cheap-stash-production-redirect-manifest.csv",
        "medium-stash-production-redirect-manifest.csv",
        "expensive-stash-production-redirect-manifest.csv",
    ]
    tier_links = {
        (r["physical_container_sid"], r["source_value"])
        for name in tier_manifests for r in rows(DATA / name)
        if r["file"] == "GameData/SpawnActorPrototypes.cfg"
    }

    levels = Counter()
    for r in tests:
        case = r["case_id"]
        actor_id = r["actor_sid"]
        assert len(actor_id) == 32 and actor_id in actors, (
            case, "Unknown physical base actor")
        actor = actors[actor_id]
        assert actor["ClueVariableSID"] == "EmptyInherited", (
            case, "Named clue cannot be used as a safe test source")
        assert actor_id not in protected, (case, "Protected actor")

        layer, source = r["source_layer"], r["source_sid"]
        if layer == "DIRECT":
            assert (actor_id, source) in direct, (
                case, "Not in an exact direct-item allowlist")
        elif layer == "ITEMGENERATOR":
            assert source in tokens(actor["OriginalItemGeneratorSIDs"]), (
                case, "Not the actor's original ItemGenerator")
            assert (actor_id, source) in tier_links, (
                case, "Tier redirect manifest does not confirm this consumer")
        elif layer == "SMARTLOOT":
            assert source in tokens(actor["OriginalStashPrototypeSIDs"]), (
                case, "Not the actor's original SmartLoot reference")
        else:
            raise AssertionError((case, "Unsupported generator layer"))

        control = r["control_id"]
        if r["stage"] == "baseline":
            assert not control and not r["value"], (
                case, "Baseline must not change any knob")
        else:
            assert control in parameters, (case, "Unknown parameter")
            knob = parameters[control]
            assert knob["tab"] == "stashes", (case, "Non-Stashes parameter")
            value = float(r["value"])
            assert knob["slider"]["min"] <= value <= knob["slider"]["max"], (
                case, "Value out of the actual UI range")
            assert r["stage"] in ("zero", "high", "negative")
            assert (r["stage"] != "high" or value == 3), (
                case, "High test must be isolated 3x")
            assert (r["stage"] != "negative" or case == "CAB-BIG-NEG-FOOD")
        status = r["status"]
        assert status in ("NOT_RUN", "PASS", "FAIL", "INCONCLUSIVE"), (
            case, "Unknown in-game status")
        if status == "NOT_RUN":
            assert not r["actual_in_game"].strip(), (
                case, "Unreviewed observation lacks test status")
        else:
            assert "evidence=" in r["actual_in_game"] and (
                "save=" in r["actual_in_game"]), (
                case, "In-game result needs an evidence ref and save ID")
        levels[status] += 1

    assert {r["source_layer"] for r in tests} == {
        "DIRECT", "ITEMGENERATOR", "SMARTLOOT"
    }
    assert {"CAB-BIG-ZERO", "CAB-BAG-ZERO", "CAB-SMALL-ZERO",
            "MED-BASIC-ZERO", "MED-STANDARD-ZERO", "MED-RARE-ZERO",
            "MED-SMART-CHEAP-ZERO", "MED-SMART-SMART-ZERO",
            "DIRECT-ROSTOK-MED-ZERO"} <= set(ids)
    print(f"PASS: {len(tests)} original-source validated, guarded manual game cases")
    print("PASS: all six loot categories and three independent source layers indexed")
    print("PASS: Cabin 2 direct Chance=1 medkits plus Bag negative source, "
          "Basic/Standard/Rare, SmartLoot and Rostok direct covered")
    print("GAMEPLAY STATUS: " + ", ".join(
        f"{state}={levels[state]}" for state in
        ("NOT_RUN", "PASS", "FAIL", "INCONCLUSIVE")))
    print("NOTE: source audit does NOT prove game drops or source isolation.")

if __name__ == "__main__":
    main()
