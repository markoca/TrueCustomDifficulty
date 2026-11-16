#!/usr/bin/env python3
"""MUTANT AGGRESSION contract (docs/combat/MUTANT-AGGRESSION.md).

Checks that the 14 ATTACK RATE families + SPECIAL ABILITY RATE agree across
policy, catalog, writer, embedded CombatSynchronization tables, baseline and
UI. Optional: set TCD_BASE_DUMP=<dump>/base to also verify the policy against
the 2.1 DifficultyPrototypes.cfg agent list.
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
catalog = json.loads((SRC / "Data" / "parameters.json").read_text(encoding="utf-8"))
rows = catalog["parameters"] if isinstance(catalog, dict) else catalog
by_id = {r["id"]: r for r in rows}
baseline = json.loads((SRC / "Data" / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
sync = json.loads((SRC / "Data" / "combat-synchronization-2.1.json").read_text(encoding="utf-8"))

# ---- policy -----------------------------------------------------------------
families = re.findall(r'new\("([a-z]+)", "([^"]+)", new Agent\[\]', policy)
check(len(families) == 14, f"policy lists 14 families ({len(families)})")
keys = [k for k, _ in families]
check(keys == sorted(keys), "families are alphabetical")
agents = re.findall(r'new\("([A-Za-z0-9_]+)", (?:T\([^)]*\)|[A-Za-z]+Tags), (true|false)\)', policy)
vanilla_agents = {sid for sid, flag in agents if flag == "true"}
check(len(agents) == 25, f"policy lists 25 agent SIDs ({len(agents)})")
check(len(vanilla_agents) == 14, f"14 agents carry vanilla Master entries ({len(vanilla_agents)})")

# family → has vanilla Master entries
family_blocks = re.split(r'\n        new\("', policy.split("public static IReadOnlyList<Family> Families", 1)[1])[1:]
vanilla_families = {b.split('"', 1)[0] for b in family_blocks if ", true)" in b}
check(vanilla_families == {"bayun", "bloodsucker", "burer", "chimera", "controller", "deer",
                           "pseudodog", "pseudogiant"},
      "eight vanilla Master families: " + ", ".join(sorted(vanilla_families)))
check("VanillaMasterCooldownMultiplier = 0.5" in policy, "vanilla Master multiplier is 0.5")
check('("[3]", "TokenTag.Ability", 8.0, 12.0)' in policy
      and '("[4]", "TokenTag.Ability.Knockdown", 8.0, 12.0)' in policy,
      "special token groups [3]/[4] at 8–12 s")

# ---- catalog ------------------------------------------------------------------
ids = [f"combat.mutantAttackRate.{k}" for k in keys] + ["combat.mutantSpecialAbilityRate"]
check(all(i in by_id for i in ids), "all 15 aggression ids are in parameters.json")
for key, label in families:
    row = by_id.get(f"combat.mutantAttackRate.{key}")
    if not row:
        continue
    expected = {"Novice": 1, "Stalker": 1, "Veteran": 1, "Master": 2} if key in vanilla_families else {"All": 1}
    check(row.get("vanilla") == expected, f"{key}: vanilla {row.get('vanilla')}")
    check(row.get("label") == f"{label} Attack Rate", f"{key}: label '{row.get('label')}'")
    check(row.get("exposeInUi") is False and row.get("group") == "Mutant Health",
          f"{key}: hidden, owned by the Mutant Health radar")
    s = row.get("slider", {})
    check((s.get("min"), s.get("max"), s.get("step")) == (0.25, 3, 0.25), f"{key}: slider 0.25–3 / 0.25")
    check(row.get("support", {}).get("status") == "runtime_test_required", f"{key}: runtime_test_required")
special = by_id.get("combat.mutantSpecialAbilityRate", {})
check(special.get("vanilla") == {"Novice": 1, "Stalker": 1, "Veteran": 1, "Master": 2},
      "special ability rate vanilla N/S/V 1, Master 2")
s = special.get("slider", {})
check((s.get("min"), s.get("max"), s.get("step")) == (0.5, 3, 0.25), "special slider 0.5–3 / 0.25")

# ---- embedded synchronization tables -------------------------------------------
tables = sync["tables"]
check(set(tables) == {"Default", "CombatSynchronizerStalker"}, "sync tables Default + Stalker")
for sid, (lo, hi) in {"Default": ("8.f", "12.f"), "CombatSynchronizerStalker": ("4.f", "6.f")}.items():
    for rank in ("Newbie", "Experienced", "Veteran", "Master"):
        groups = tables[sid][rank]["children"]["FilterGroups"]["children"]
        for index, tag in (("[3]", "TokenTag.Ability"), ("[4]", "TokenTag.Ability.Knockdown")):
            g = groups[index]
            ok = (g["children"]["AllowedTags"]["fields"]["[0]"] == tag
                  and g["fields"].get("CooldownMin") == lo and g["fields"].get("CooldownMax") == hi)
            if not ok:
                check(False, f"{sid}.{rank}.{index} {tag} {lo}–{hi}")
check(not any("sync" in f for f in failures), "every rank keeps [3]/[4] tags and 8–12 / 4–6 s")
check("TrueCustomDifficulty.Data.combat-synchronization-2.1.json" in csproj
      or "combat-synchronization-2.1.json" in csproj, "csproj embeds combat-synchronization-2.1.json")

# ---- writer -------------------------------------------------------------------
check("WriteMutantAggression(" in main_writer and "MutantAggressionPolicy.ParameterIds" in main_writer,
      "main writer calls WriteMutantAggression and supports the 15 ids")
for token in ('GetOrAddChild("AgentCooldownMultipliers", useBpatch: false)', "1.0 / rate",
              "MutantAggressionPolicy.VanillaMasterCooldownMultiplier", "LoadCombatSynchronizationTables",
              '"CombatSynchronizerStalker" : "Default"', "IsValueModifiedFromReference"):
    check(token in writer, f"writer token {token}")
entry = baseline.get("special", {}).get("mutantAggression", {})
check(sorted(entry.get("parameterIds", [])) == sorted(ids[:-1])
      and entry.get("parameterId") == "combat.mutantSpecialAbilityRate",
      "baseline special.mutantAggression lists the writer route")

# ---- UI -----------------------------------------------------------------------
for token in ("MUTANT DAMAGE, HEALTH AND AGGRESSION", "SPECIAL ABILITY RATE", "ATTACK RATE",
              "DrawGlobalTrack", "DrawSpeciesTrack", "ReferenceMagnet"):
    check(token in radar, f"radar token {token}")

# ---- optional dump cross-check -----------------------------------------------
dump = os.environ.get("TCD_BASE_DUMP")
if dump:
    text = (Path(dump) / "GameData" / "DifficultyPrototypes.cfg").read_text(encoding="utf-8", errors="replace")
    listed = set(re.findall(r"AgentPrototypeSID\s*=\s*([A-Za-z0-9_]+)", text))
    check(listed == vanilla_agents, f"dump agent list matches policy vanilla flags ({len(listed)})")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
print("\nMUTANT AGGRESSION contract OK")
