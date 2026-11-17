#!/usr/bin/env python3
"""Static Combat catalog -> direct writer -> special writer contract checks.

This deliberately does NOT claim original CFG provenance or in-game runtime proof.
Run the decoded source check separately with audit_cfg_source_values.py.
"""
from __future__ import annotations

from collections import Counter, defaultdict
from decimal import Decimal
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"


def verify() -> None:
    catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
    writer = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
    combat = {p["id"]: p for p in catalog["parameters"] if p["id"].startswith("combat.")}
    points = [e for e in writer["editPoints"] if e["parameterId"].startswith("combat.")]
    grouped = defaultdict(list)
    for point in points:
        grouped[point["parameterId"]].append(point)

    # +25 MUTANT AGGRESSION, +9 HUMAN TACTICS / HUMAN AWARENESS (2026-11-16)
    assert len(combat) == 62, f"Combat catalog changed: {len(combat)}"
    human_direct = {"combat.npcAccuracy", "combat.npcSightRange", "combat.npcHearingRange", "combat.npcAlertMemory"}
    human_tactics = {"combat.npcGrenadeRate", "combat.npcSuppressionRate", "combat.npcFlankRate",
                     "combat.npcPushRate", "combat.npcCamperPunishRate"}
    assert human_direct | human_tactics <= set(combat)
    assert len(grouped) == 28, f"Direct Combat parameter count changed: {len(grouped)}"
    assert len(points) == 164, f"Concrete direct Combat route count changed: {len(points)}"
    assert sum(e["file"] == "GameData/DifficultyPrototypes.cfg" for e in points) == 40
    assert sum(e["file"] == "GameData/ObjPrototypes.cfg" for e in points) == 27
    assert sum(e["parameterId"] == "combat.npcAccuracy" for e in points) == 77
    assert sum(e["parameterId"] in human_direct for e in points) == 97

    # Retired systems are not represented by oversimplified single-source controls.
    retired = {"combat.npcAttackCooldown", "combat.npcHipAccuracy"}
    assert retired.isdisjoint(combat) and retired.isdisjoint(grouped), (
        "Attack pacing or accuracy was reintroduced into shipped Combat routes"
    )
    for factory in (ROOT / "src/TrueCustomDifficulty/Presets").glob("*.json"):
        values = json.loads(factory.read_text(encoding="utf-8"))["values"]
        assert retired.isdisjoint(values), factory.name + ": retired preset controls"
    for source in ("Controls/AggregateVisualControl.cs",
                   "ViewModels/ParameterViewModel.cs",
                   "ViewModels/MainViewModel.cs"):
        body = (ROOT / "src/TrueCustomDifficulty" / source).read_text(encoding="utf-8")
        assert not any(identifier in body for identifier in retired), (
            source + ": retired control still wired into UI"
        )

    direct_ids = set(grouped) - human_direct
    special_ids = set(combat) - set(grouped) - human_tactics
    assert all(combat[pid]["support"]["status"] == "runtime_test_required"
               for pid in human_direct | human_tactics)
    aggression_ids = {pid for pid in special_ids
                      if pid.startswith(("combat.mutantAttackRate.", "combat.mutantAbilityRate."))
                      or pid == "combat.mutantSpecialAbilityRate"}
    assert len(aggression_ids) == 25, aggression_ids
    special_ids -= aggression_ids
    assert len(special_ids) == 4
    assert special_ids == {"combat.psySpawnCycleDelay", "combat.psyPhantomCount",
                           "combat.psyBleeding", "combat.psyDurabilityDamage"}
    assert all(combat[pid]["support"]["status"] == "runtime_test_required"
               for pid in aggression_ids)
    assert all(combat[pid]["support"]["status"] == "confirmed" for pid in direct_ids)
    assert all(combat[pid]["support"]["status"] == "runtime_test_required"
               for pid in special_ids)

    profiles = catalog["difficultyProfiles"]
    names_by_sid = {v["sid"]: name for name, v in profiles.items()}
    assert names_by_sid == {
        "Easy": "Novice", "Medium": "Stalker", "Hard": "Veteran", "Stalker": "Master"
    }
    ownership = {}
    for pid, route_points in grouped.items():
        if pid in human_direct:
            continue  # summary targets; exact routes in tools/audit_human_tactics.py
        definition = combat[pid]
        for point in route_points:
            path = ".".join([*point.get("nodePath", []), point["key"]])
            matched = any(
                target["file"] == point["file"]
                and target["operation"] == point["operation"]
                and target["path"] == path
                and (target["prototype"] == point["prototype"] or
                     target["prototype"] == "$selectedDifficultySID"
                     and point["prototype"] == point.get("difficultySid"))
                for target in definition["targets"]
            )
            assert matched, f"{pid}: catalog/writer path or operation mismatch ({path})"
            concrete_key = (point["scope"], point["file"], point["prototype"],
                            tuple(point.get("nodePath", [])), point["key"])
            assert concrete_key not in ownership, (
                f"Direct ownership collision: {pid} / {ownership.get(concrete_key)}"
            )
            ownership[concrete_key] = pid
            if point["file"] == "GameData/ObjPrototypes.cfg":
                assert pid.startswith("combat.mutantHealth.")
                assert point["nodePath"] == ["VitalParams"]
                assert point["key"] == "MaxHP" and point["operation"] == "scale"
                assert Decimal(point["sourceRaw"]) > 0
            else:
                assert point["file"] == "GameData/DifficultyPrototypes.cfg"
                sid = point["difficultySid"]
                assert point["prototype"] == sid and sid in names_by_sid
                expected = definition["vanilla"].get(
                    names_by_sid[sid], definition["vanilla"].get("All")
                )
                assert expected is not None, f"{pid}: absent vanilla reference"
                if definition["valueKind"] == "boolean":
                    assert point["sourceRaw"].lower() == str(expected).lower()
                else:
                    assert Decimal(point["sourceRaw"]) == Decimal(str(expected)), (
                        f"{pid}/{sid}: sourceRaw differs from catalog vanilla"
                    )
        if pid.startswith("combat.mutantHealth."):
            assert all(p["file"] == "GameData/ObjPrototypes.cfg" for p in route_points)
        else:
            assert len(route_points) == 4
            assert {p["difficultySid"] for p in route_points} == set(names_by_sid)

    special = writer["special"]
    psy = special["psyParameterKeys"]
    assert len(psy) == 4
    assert set(psy) == {pid for pid in special_ids if pid.startswith("combat.psy")}
    effects = set(special["psyOverrides"])
    assert effects == {"SpawnPSYPhantoms", "SpawnPSYNPC"}
    assert len({v["index"] for v in special["psyOverrides"].values()}) == 2
    for pid, key in psy.items():
        targets = combat[pid]["targets"]
        assert len(targets) == 2
        assert all(t["operation"] == "setOrCreateOverride" for t in targets)
        assert all(t["path"].endswith("." + key) for t in targets)
        assert all(any(f"[OverrideEffectPrototypeSID={effect}]" in t["path"]
                       for t in targets) for effect in effects)

    print("Combat static contract PASS: 28 sources (24 direct + 4 PSY special),")
    print("  67 difficulty/MaxHP routes + 97 human accuracy / awareness routes.")
    print("  2 PSY effects / 4 controls; no retired mutant pacing routes.")
    print("  No direct ownership collision, target-path drift or vanilla marker drift.")
    print("NOT VERIFIED: original decoded CFG, BPatch runtime and gameplay effects.")

if __name__ == "__main__":
    try:
        verify()
    except (AssertionError, KeyError, ValueError) as exc:
        print(f"Combat static contract FAIL: {exc}", file=sys.stderr)
        sys.exit(1)
