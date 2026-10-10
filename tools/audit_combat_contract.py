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

    assert len(combat) == 39, f"Combat catalog changed: {len(combat)}"
    assert len(grouped) == 27, f"Direct Combat parameter count changed: {len(grouped)}"
    assert len(points) == 68, f"Concrete direct Combat route count changed: {len(points)}"
    assert sum(e["file"] == "GameData/DifficultyPrototypes.cfg" for e in points) == 52
    assert sum(e["file"] == "GameData/ObjPrototypes.cfg" for e in points) == 16

    direct_ids = set(grouped)
    special_ids = set(combat) - direct_ids
    assert len(special_ids) == 12
    assert special_ids == (
        {f"combat.mutantCooldown.{name}" for name in
         ("bloodsucker", "chimera", "deer", "pseudogiant", "pseudodog",
          "burer", "bayun", "controller")}
        | {"combat.psySpawnCycleDelay", "combat.psyPhantomCount",
           "combat.psyBleeding", "combat.psyDurabilityDamage"}
    )
    # UI rate is 1/native cooldown wait. All eight cards must use 0.25x
    # RECHARGE increments (0.5x..3x), NOT raw-wait increments. Do not change
    # normal mutant attack frequency (an unrelated source).
    for pid in sorted(pid for pid in special_ids if pid.startswith("combat.mutantCooldown.")):
        slider = combat[pid]["slider"]
        assert abs(float(slider["min"]) - 1.0 / 3.0) < 1e-12, (
            f"{pid}: recharge cap must be exactly 3x (native wait floor 1/3)"
        )
        assert slider["max"] == 2 and slider["step"] == 0.25
        assert slider["stepMode"] == "absolute"
    assert combat["combat.mutantAttackCooldown"]["slider"]["max"] == 2

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
    families = special["mutantCooldownFamilies"]
    owners = special["mutantCooldownParameters"]
    assert len(families) == 14 and len(owners) == 8
    assert {agent for members in owners.values() for agent in members} == set(families)
    assert len({f["index"] for f in families.values()}) == 14
    for sid, family in families.items():
        assert family["agentPrototypeSid"] == sid
        assert len({p["index"] for p in family["cooldowns"]}) == len(family["cooldowns"])
        assert all(p["tag"] for p in family["cooldowns"])

    for parameter_id, agent_sids in owners.items():
        assert parameter_id in special_ids
        targets = combat[parameter_id]["targets"]
        assert len(targets) == len(agent_sids)
        assert all(t["operation"] == "setAgentAbilityCooldownFamily" for t in targets)
        for sid in agent_sids:
            assert any(f"[AgentPrototypeSID={sid}]" in t["path"] for t in targets)

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

    print("Combat static contract PASS: 39 controls (27 direct + 12 special),")
    print("  68 direct routes (52 difficulty fields + 16 nested mutant MaxHP).")
    print("  14 cooldown agent SIDs / 8 control families, 2 PSY effects / 4 controls.")
    print("  No direct ownership collision, target-path drift or vanilla marker drift.")
    print("NOT VERIFIED: original decoded CFG beyond prior HP evidence, BPatch runtime,")
    print("  non-Master override insertion, gameplay effects and hidden legacy presets.")


if __name__ == "__main__":
    try:
        verify()
    except (AssertionError, KeyError, ValueError) as exc:
        print(f"Combat static contract FAIL: {exc}", file=sys.stderr)
        sys.exit(1)
