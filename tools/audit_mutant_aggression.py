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
check(ability_families == ["bayun", "bloodsucker", "burer", "controller", "deer",
                           "poltergeist", "pseudodog", "pseudogiant"], f"8 ability families {ability_families}")
# Attack-only (2026-11-18): roars, growls, howls, dodges, teleports and shields have no control.
NON_ATTACK = {"Ability.Cooldown." + t for t in ("Roar", "Growl", "Howl", "Evasion", "BlinkTeleport", "Shield",
                                                 "ShortJumpAttack", "Grunt", "Squeek", "Alerted")}
for key, f in families.items():
    _bad = sorted(set(f["ability"]) & NON_ATTACK) + sorted(t for t in set(f["attack"]) & NON_ATTACK
                                                         if not (key == "bayun" and t.endswith("ShortJumpAttack")))
    check(not _bad, f"{key}: no roar / growl / howl / dodge / teleport / shield control {_bad}")
for key, f in families.items():
    check(f["attack"] and not set(f["attack"]) & set(f["ability"]), f"{key}: ATTACKS / ABILITIES disjoint")
    for sid, vtags in f["agents"].items():
        check(set(vtags) <= set(f["attack"]) | set(f["ability"]) | NON_ATTACK | {"Ability.Cooldown.ShockwaveAttack"},
              f"{key}/{sid}: vanilla Master tags are controls or written back as the game has them")
vanilla_agents = {sid: v for f in families.values() for sid, v in f["agents"].items() if v}
check(len(vanilla_agents) == 14, f"14 agents with vanilla Master entries ({len(vanilla_agents)})")


def master(key: str, group: str) -> bool:
    f = families[key]
    return any(t in f[group] for v in f["agents"].values() for t in v)


# ---- source-correct headlines and pseudodog abilities (2026-11-18) ------------
check(families["pseudodog"]["ability"] == ["Ability.Cooldown.CombatSummon", "Ability.Cooldown.RetreatSummon"],
      f"pseudodog ABILITIES: both phantom summons {families['pseudodog']['ability']}")
check('difficultySid == "Stalker" && agent.VanillaMasterTags.Contains(tag)' in writer
      and "MutantAggressionPolicy.VanillaMasterCooldownMultiplier" in writer, "uncontrolled Master tags written back at 0.5")
for _key in ("bayun", "deer"):
    check("Ability.Cooldown.CombatSummon" in families[_key]["ability"], f"{_key} owns its summon in ABILITIES")
check("SummonRate" not in policy and "combat.mutantSummonRate" not in by_id, "no global SUMMON RATE (owned per mutant)")
cooldowns = ROOT / "docs" / "combat" / "MUTANT-COOLDOWNS.md"
check(cooldowns.exists() and "| ABILITIES | summon | `CombatSummon` | 20 s |" in cooldowns.read_text(encoding="utf-8"),
      "generated MUTANT-COOLDOWNS.md is current")

# ---- catalog ------------------------------------------------------------------
ids = ["combat.mutantSpecialAbilityRate"]
check(all(i in by_id for i in ids), "SPECIAL ABILITY RATE is in parameters.json")

# ---- one knob per move ------------------------------------------------------------
moves_src = policy[policy.index("public static IReadOnlyList<Move> Moves("):policy.index("public static IReadOnlyList<(Family Family")]
move_rows = []
for _key, _group, _body in re.findall(r'\("(\w+)", Group\.(\w+)\) => new\[\] \{(.*?)\},\n', moves_src, re.S):
    for _slug, _label, _name, _tag, _secs in re.findall(
            r'M\("(\w+)", "([^"]+)", "([^"]+)", "([\w.]+)", ([\d.]+)\)', _body):
        move_rows.append((_key, _group, _slug, _label, _tag, float(_secs)))
check(len(move_rows) == 53, f"53 move knobs ({len(move_rows)})")
move_ids = [f"combat.mutantMoveRate.{k}.{slug}" for k, _, slug, *_ in move_rows]
check(len(set(move_ids)) == 53 and all(i in by_id for i in move_ids), "every move knob id is in parameters.json")
check(sorted(r["id"] for r in catalog if r["id"].startswith("combat.mutantMoveRate.")) == sorted(move_ids),
      "no catalog move knob without a policy move")
for _key, _group, _slug, _label, _tag, _secs in move_rows:
    _full = _tag if _tag.startswith("Abilities.") else "Ability.Cooldown." + _tag
    _own = families[_key]["attack" if _group == "Attacks" else "ability"]
    check(_full in _own, f"{_key}.{_slug}: tag {_full} belongs to its {_group} slider")
    check(_secs > 0 and len(_label) <= 9 and _label == _label.upper(), f"{_key}.{_slug}: cooldown > 0, short CAPS caption")
    _row = by_id[f"combat.mutantMoveRate.{_key}.{_slug}"]
    _listed = any(_full in v for v in families[_key]["agents"].values())
    _want = {"Novice": 1, "Stalker": 1, "Veteran": 1, "Master": 2} if _listed else {"All": 1}
    check(_row.get("vanilla") == _want and _row.get("exposeInUi") is False
          and (_row["slider"]["min"], _row["slider"]["max"], _row["slider"]["step"]) == (0.25, 3, 0.25),
          f"{_key}.{_slug}: vanilla {_want}, board member, 0.25–3× step 0.25")
check(len({(k, t) for k, _, _, _, t, _ in move_rows}) == 53, "one knob per cooldown tag per family")
check(not [r["id"] for r in catalog if r["id"].startswith(("combat.mutantAbilityRate.", "combat.mutantAttackRate."))],
      "no ATTACKS / ABILITIES sliders: every move is its own knob")
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
for token in ('GetOrAddChild("AgentCooldownMultipliers", useBpatch: false)',
              "agent.VanillaMasterTags", "MutantAggressionPolicy.Multiplier(agent, tag, knob.Rate, knob.Reference)",
              "MutantAggressionPolicy.KnobReference(family, move, difficultySid)",
              "MutantAggressionPolicy.MoveId(family, move)",
              "LoadCombatSynchronizationTables", '"CombatSynchronizerStalker" : "Default"'):
    check(token in writer, f"writer token {token}")
entry = baseline["special"].get("mutantAggression", {})
check(sorted(entry.get("parameterIds", [])) == sorted(move_ids)
      and entry.get("parameterId") == "combat.mutantSpecialAbilityRate", "baseline special.mutantAggression")

# ---- UI -----------------------------------------------------------------------
for token in ("MUTANT DAMAGE, HEALTH AND AGGRESSION", "SPECIAL ABILITY RATE",
              "DrawKnobCells(", "DrawMoveKnob(", "MutantAggressionPolicy.MoveSeconds(", "OnMouseWheel(", '"ATTACKS"', '"ABILITIES"',
              "ReferenceMagnet"):
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
    # Every move second the panel shows equals a CFG cooldown of that family.
    import sys as _sys
    _sys.path.insert(0, str(ROOT / "tools"))
    from mutant_cooldown_extract import extract as _extract
    dlc_ability = Path(os.environ.get("TCD_DLC_DUMP", "")) / "AbilityPrototypes.cfg"
    cfg = _extract(str(gd / "AbilityPrototypes.cfg"),
                   str(dlc_ability if dlc_ability.is_file() else gd / "AbilityPrototypes.cfg"))
    # Controller_Deer (a Controller) uses CombatSummonAbility_Deer: blind dogs every 20 s.
    cfg_secs = {(r["fam"], r["tag"], r["dur"]) for r in cfg} | {("bloodsucker", "Roar", 6000.0)}
    cfg_secs |= {("controller", "CombatSummon", d) for f, t, d in cfg_secs if (f, t) == ("deer", "CombatSummon")}
    wrong = [f"{k} {slug} {t} {sec}" for k, _, slug, _, t, sec in move_rows
             if (k, t, sec) not in cfg_secs]
    check(not wrong, f"COOLDOWNS panel seconds match AbilityPrototypes {wrong}")
    # Every knob's move is in the ability list of at least one agent of its family
    # (Blind Dog shield: only the electrocollar dog; it is not an attack anyway).
    from mutant_cooldown_extract import agent_cooldown_tags as _agent_tags
    _have = {k: set().union(*(_agent_tags(str(gd / "ObjPrototypes.cfg"), str(gd / "AbilityPrototypes.cfg"), sid)
                              for sid in f["agents"])) for k, f in families.items()}
    _missing = [f"{k}.{slug}" for k, _, slug, _, t, _ in move_rows if t not in _have[k]]
    check(not _missing, f"every knob's move exists on an agent of its family {_missing}")
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
