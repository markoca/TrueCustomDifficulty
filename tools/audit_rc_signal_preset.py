#!/usr/bin/env python3
"""Verify that the RC diagnostic remains a test-only CFG writer fixture,
not a shipped/seeding preset. This does not run the game."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
QA = ROOT / "tests/RCSignalPatchContract/Fixtures/RC-CFG-Signal.json"
CATALOG = ROOT / "src/TrueCustomDifficulty/Data/parameters.json"
BASELINE = ROOT / "src/TrueCustomDifficulty/Data/cfg-writer-baseline.json"

# Intentional high-contrast PROBES, not a balanced gameplay profile.
EXPECTED = {
    "economy.startingMoney": 100000,
    "stamina.maxCarryWeight": 160,
    "stamina.maxSP": 200,
    "health.maxHP": 200,
    "health.medkitHealing": 140,
    "survival.waterInstantStaminaRestore": 1,
    "combat.npcWeaponDamage": 0,
    "combat.playerWeaponDamage": 2,
    "environment.anomalyDamage": 0,
    "health.vodkaRadiationRemoval": 100,
    "loot.mutant.blindDogChance": 1,
    "alife.mutantSpawn.blindDog": 3,
    "equipment.weaponWearPerShot": 2,
}
PROFILE_NAME = "RC - CFG Signal"
data = json.loads(QA.read_text(encoding="utf-8"))
catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
by_id = {p["id"]: p for p in catalog["parameters"]}
ids = set(EXPECTED)
assert (data["profileVersion"] == 1 and data["referenceDifficulty"] == "Stalker"
        and data["name"] == PROFILE_NAME and data["gameVersion"] == catalog["source"]["gameVersion"]
        and not data.get("drinkDurationOverride") and not data.get("medicalOverride"))
assert set(data["values"]) == ids and len(ids) == 13
assert len(by_id) == 285
for id, target in EXPECTED.items():
    definition = by_id[id]
    entry = data["values"][id]
    assert entry == {"kind": "slider", "number": target}, (id, entry)
    assert definition["control"] == "slider", id
    # Medkit, Vodka and A-Life live in approved visual aggregate editors;
    # their real native source-backed slider IDs are deliberately not shown
    # as duplicate standalone rows.
    aggregate_members = {
        "health.medkitHealing", "health.vodkaRadiationRemoval",
        "alife.mutantSpawn.blindDog",
    }
    assert definition.get("exposeInUi", True) or id in aggregate_members, id
    assert not definition.get("requiresToggle"), id
    bound = definition["slider"]
    assert bound["min"] <= target <= bound["max"], id
    if bound.get("stepMode") == "absolute":
        steps = (target - bound["min"]) / bound["step"]
        assert abs(steps - round(steps)) < 0.000001, (id, steps)
    vanilla = definition["vanilla"].get("Stalker", definition["vanilla"].get("All"))
    assert isinstance(vanilla, (int, float)) and vanilla != target, id
    points = [p for p in baseline["editPoints"] if p["parameterId"] == id
              and p["scope"] == "baseGame"]
    assert points, "No production CFG edit point: " + id
    assert all(p["file"] and p["key"] and p["sourceRaw"] is not None for p in points), id

# The A-Life test signal is a real changed integer outcome at EVERY player
# rank, unlike 0.95x for 1-cap mutants. It is only a ceiling, not guaranteed
# actual encounter group composition.
dog = sorted((p for p in baseline["editPoints"]
              if p["parameterId"] == "alife.mutantSpawn.blindDog"),
             key=lambda p: int(p["nodePath"][1][1:-1]))
assert [int(p["sourceRaw"]) for p in dog] == [4, 6, 8, 12]
assert [int(p["sourceRaw"]) * 3 for p in dog] == [12, 18, 24, 36]

# The executable must ship only its twelve role presets. The historical RC
# diagnostic remains executable as a test-owned resource and is not seeded.
factory_names = {p.stem for p in (ROOT / "src/TrueCustomDifficulty/Presets").glob("*.json")}
assert len(factory_names) == 12 and PROFILE_NAME not in factory_names
service = (ROOT / "src/TrueCustomDifficulty/Services/PresetDirectoryService.cs").read_text(encoding="utf-8")
project = (ROOT / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
contract = (ROOT / "tests/RCSignalPatchContract/Program.cs").read_text(encoding="utf-8")
contract_project = (ROOT / "tests/RCSignalPatchContract/RCSignalPatchContract.csproj").read_text(encoding="utf-8")
assert not (ROOT / "src/TrueCustomDifficulty/QA/RC - CFG Signal.json").exists()
assert 'QA\\RC - CFG Signal.json' not in project
assert 'TrueCustomDifficulty.QA.RC-CFG-Signal.json' not in project
assert "RcSignalResourceName" not in service
assert 'RetireRcSignalPreset(directory);' in service
assert 'File.Delete(previous);' in service
assert 'File.Move(previous, destination);' in service
assert 'Path.Combine(directory, "Archived")' in service
assert 'RetiredRcSignalStockBlob = "12a9b6b5a1857815fa0627e5fdd67aa5527c574d"' in service
assert 'Fixtures\\RC-CFG-Signal.json' in contract_project
assert 'RCSignalPatchContract.Fixtures.RC-CFG-Signal.json' in contract_project
assert 'typeof(Program).Assembly.GetManifestResourceStream(ResourceName)' in contract
assert (ROOT / "docs/RC-CFG-SIGNAL-TEST.md").is_file()
print("PASS: 13 source-backed extreme test signals; no production RC profile; "
      "stock retirement with archival preservation of modified user copies.")
print("NOT TESTED: Windows/Wine writer fixture run, IoStore or gameplay.")
