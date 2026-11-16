#!/usr/bin/env python3
"""MUTANT AGGRESSION contract (docs/combat/MUTANT-AGGRESSION.md).

14 families x ATTACKS (+10 ABILITIES) + SPECIAL ABILITY RATE agree across
policy, catalog, writer, embedded CombatSynchronization tables, baseline and
UI. Optional: TCD_BASE_DUMP=<dump>/base also checks agents, vanilla Master
lists and tags against the 2.1 dump.
"""
from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TrueCustomDifficulty"
failures: list[str] = []


def check(cond: bool, message: str) -> None:
    print(("[OK] " if cond else "[FAIL] ") + message)
    if not cond:
        failures.append(message)


policy = (SRC / "Services" / "MutantAggressionPolicy.cs").read_text(encoding="utf-8")
writer = (SRC / "Services" / "CfgPatchWriterService.MutantAggression.cs").read_text(encoding="utf-8")
main_writer = (SRC / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
radar = (SRC / "Controls" / "MutantHealthRadar.cs").read_text(encoding="utf-8")
csproj = (SRC / "TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
catalog = json.loads((SRC / "Data" / "parameters.json").read_text(encoding="utf-8"))["parameters"]
by_id = {r["id"]: r for r in catalog}
baseline = json.loads((SRC / "Data" / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
sync = json.loads((SRC / "Data" / "combat-synchronization-2.1.json").read_text(encoding="utf-8"))


def tags(text: str) -> list[str]:
    return [t if t.startswith("Abilities.") else "Ability.Cooldown." + t for t in re.findall(r'"([\w.]+)"', text)]


# ---- parse the policy --------------------------------------------------------
body = policy.split("public static IReadOnlyList<Family> Families", 1)[1].split(
    "public static IReadOnlyList<string> RateParameterIds", 1)[0]
families = {}
for chunk in re.split(r'\n        new\("', body)[1:]:
    key, label = re.match(r'([a-z]+)", "([^"]+)"', chunk).groups()
    agents = {sid: tags(rest) for sid, rest in re.findall(r'A\("(\w+)"((?:, "[\w.]+")*)\)', chunk)}
    tail = chunk[chunk.rindex("A(\""):]
    tail = tail[tail.index(")") + 1:]
    lists = re.findall(r'T\([^)]*\)|Array\.Empty<string>\(\)', tail)
    attack = tags(lists[0]) if lists[0].startswith("T(") else []
    ability = tags(lists[1]) if lists[1].startswith("T(") else []
    families[key] = dict(label=label, agents=agents, attack=attack, ability=ability)

check(len(families) == 14 and list(families) == sorted(families), f"14 alphabetical families ({len(families)})")
agent_count = sum(len(f["agents"]) for f in families.values())
check(agent_count == 38, f"38 agent SIDs ({agent_count})")
ability_families = sorted(k for k, f in families.items() if f["ability"])
check(ability_families == ["bayun", "blinddog", "bloodsucker", "burer", "chimera", "controller",
                           "poltergeist", "pseudodog", "pseudogiant", "snork"], f"10 ability families {ability_families}")
for key, f in families.items():
    check(f["attack"] and not set(f["attack"]) & set(f["ability"]), f"{key}: ATTACKS / ABILITIES disjoint")
    for sid, vtags in f["agents"].items():
        check(set(vtags) <= set(f["attack"]) | set(f["ability"]), f"{key}/{sid}: vanilla Master tags covered")
vanilla_agents = {sid: v for f in families.values() for sid, v in f["agents"].items() if v}
check(len(vanilla_agents) == 14, f"14 agents with vanilla Master entries ({len(vanilla_agents)})")


def master(key: str, group: str) -> bool:
    f = families[key]
    return any(t in f[group] for v in f["agents"].values() for t in v)


# ---- catalog ------------------------------------------------------------------
ids = [f"combat.mutantAttackRate.{k}" for k in families] + \
      [f"combat.mutantAbilityRate.{k}" for k in ability_families] + ["combat.mutantSpecialAbilityRate"]
check(len(ids) == 25 and all(i in by_id for i in ids), "all 25 aggression ids are in parameters.json")
check(not [r["id"] for r in catalog if r["id"].startswith("combat.mutantAbilityRate.")
           and r["id"].split(".")[-1] not in ability_families], "no ability rate for families without abilities")
for key, f in families.items():
    for group, prefix, word in (("attack", "combat.mutantAttackRate.", "Attack"),
                                ("ability", "combat.mutantAbilityRate.", "Ability")):
        row = by_id.get(prefix + key)
        if not row:
            continue
        expected = {"Novice": 1, "Stalker": 1, "Veteran": 1, "Master": 2} if master(key, group) else {"All": 1}
        check(row.get("vanilla") == expected, f"{prefix}{key}: vanilla {row.get('vanilla')}")
        check(row.get("label") == f"{f['label']} {word} Rate", f"{prefix}{key}: label")
        check(row.get("exposeInUi") is False and row.get("group") == "Mutant Health", f"{prefix}{key}: board member")
        s = row.get("slider", {})
        check((s.get("min"), s.get("max"), s.get("step")) == (0.25, 3, 0.25), f"{prefix}{key}: slider")
        check(row["support"]["status"] == "runtime_test_required", f"{prefix}{key}: runtime_test_required")
check(by_id["combat.mutantSpecialAbilityRate"].get("vanilla") == {"Novice": 1, "Stalker": 1, "Veteran": 1, "Master": 2},
      "special vanilla N/S/V 1, Master 2")

# ---- embedded synchronization tables -------------------------------------------
bad = []
for sid, (lo, hi) in {"Default": ("8.f", "12.f"), "CombatSynchronizerStalker": ("4.f", "6.f")}.items():
    for rank in ("Newbie", "Experienced", "Veteran", "Master"):
        groups = sync["tables"][sid][rank]["children"]["FilterGroups"]["children"]
        for index, tag in (("[3]", "TokenTag.Ability"), ("[4]", "TokenTag.Ability.Knockdown")):
            g = groups[index]
            if not (g["children"]["AllowedTags"]["fields"]["[0]"] == tag
                    and g["fields"].get("CooldownMin") == lo and g["fields"].get("CooldownMax") == hi):
                bad.append(f"{sid}.{rank}.{index}")
check(not bad, f"sync tables keep [3]/[4] tags and 8–12 / 4–6 s {bad}")
check("combat-synchronization-2.1.json" in csproj, "csproj embeds combat-synchronization-2.1.json")

# ---- writer -------------------------------------------------------------------
check("WriteMutantAggression(" in main_writer and "MutantAggressionPolicy.ParameterIds" in main_writer,
      "main writer calls WriteMutantAggression and supports the ids")
for token in ('GetOrAddChild("AgentCooldownMultipliers", useBpatch: false)', "1.0 / r.Rate",
              "agent.VanillaMasterTags", "MutantAggressionPolicy.Reference(family, group, difficultySid)",
              "LoadCombatSynchronizationTables", '"CombatSynchronizerStalker" : "Default"'):
    check(token in writer, f"writer token {token}")
entry = baseline["special"].get("mutantAggression", {})
check(sorted(entry.get("parameterIds", [])) == sorted(ids[:-1])
      and entry.get("parameterId") == "combat.mutantSpecialAbilityRate", "baseline special.mutantAggression")

# ---- UI -----------------------------------------------------------------------
for token in ("MUTANT DAMAGE, HEALTH AND AGGRESSION", "SPECIAL ABILITY RATE", '"ATTACKS"', '"ABILITIES"',
              "DrawRateHalf(", "MutantAggressionPolicy.RateDisplay(", "AbilityRowBase", "ReferenceMagnet"):
    check(token in radar, f"radar token {token}")

# ---- optional dump cross-check -----------------------------------------------
dump = os.environ.get("TCD_BASE_DUMP")
if dump:
    gd = Path(dump) / "GameData"
    diff = (gd / "DifficultyPrototypes.cfg").read_text(encoding="utf-8", errors="replace")
    stalker = diff[diff.index("\nStalker : struct.begin"):diff.index("\nCustom : struct.begin")]
    stalker = stalker[stalker.index("AgentCooldownMultipliers"):]
    dump_lists = {sid: re.findall(r"CooldownTag = (\S+)", b)
                  for sid, b in re.findall(r"AgentPrototypeSID = (\w+)\n(.*?)(?=AgentPrototypeSID|\Z)", stalker, re.S)}
    check(dump_lists == vanilla_agents, "vanilla Master lists equal the dump (order included)")
    obj = (gd / "ObjPrototypes.cfg").read_text(encoding="utf-8", errors="replace")
    missing = [sid for f in families.values() for sid in f["agents"]
               if not re.search(r"^" + sid + r" : struct\.begin", obj, re.M)]
    check(not missing, f"every agent SID exists in ObjPrototypes {missing}")
    ability = (gd / "AbilityPrototypes.cfg").read_text(encoding="utf-8", errors="replace")
    used = set(re.findall(r"(?:CooldownTag|\[\d+\]) = (\S+)", ability))
    unknown = sorted({t for f in families.values() for t in f["attack"] + f["ability"]} - used)
    check(not unknown, f"every tag occurs in AbilityPrototypes {unknown}")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
print("\nMUTANT AGGRESSION contract OK")
