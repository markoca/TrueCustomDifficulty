#!/usr/bin/env python3
"""Mutant knob CFG writer, checked against the game files.

Re-implements WriteMutantAggression's AgentCooldownMultipliers list in Python
(same rule as CfgPatchWriterService.MutantAggression.cs: a knob is a one-tag
group with its own Master vanilla; tags without a knob keep the game's Master
0.5) and renders it with the same PatchNode shape. Then it proves, on the real
DifficultyPrototypes / ObjPrototypes / AbilityPrototypes:

  1. Master with every knob at vanilla renders exactly the game's own list
     (same agents, same tags, same 0.5), i.e. the full-list replacement is lossless;
  2. one changed knob changes only that tag, on every agent of the family;
  3. Novice / Stalker / Veteran with one changed knob write only that tag;
  4. every SID is a real ObjPrototypes agent and every tag a real AbilityPrototypes
     cooldown tag; the render parses back with balanced structs and [0..n] indices;
  5. the writer source still carries the tokens this model is built on.

Usage: python3 tools/audit_mutant_cooldown_cfg.py [--game GameData] [--sample out.cfg]
Without --game (or TCD_BASE_DUMP) the game-file checks are NOT RUN.
"""
import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/TrueCustomDifficulty"
sys.path.insert(0, str(ROOT / "tools"))
from mutant_cooldown_extract import parse  # noqa: E402

failures = []


def check(ok, message):
    print(("[OK] " if ok else "[FAIL] ") + message)
    if not ok:
        failures.append(message)


def full(tag):
    return tag if tag.startswith("Abilities.") else "Ability.Cooldown." + tag


policy = re.sub(r"//[^\n]*", "", (SRC / "Services/MutantAggressionPolicy.cs").read_text(encoding="utf-8"))
writer = (SRC / "Services/CfgPatchWriterService.MutantAggression.cs").read_text(encoding="utf-8")
catalog = {p["id"]: p for p in json.loads((SRC / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]}

body = policy.split("public static IReadOnlyList<Family> Families", 1)[1].split("\n    };", 1)[0]
families = {}
for chunk in re.split(r'\n        new\("', body)[1:]:
    key = re.match(r'([a-z]+)"', chunk).group(1)
    agents = [(sid, [full(t) for t in re.findall(r'"([\w.]+)"', rest)])
              for sid, rest in re.findall(r'A\("(\w+)"((?:, "[\w.]+")*)\)', chunk)]
    families[key] = agents
moves_src = policy[policy.index("public static IReadOnlyList<Move> Moves("):
                   policy.index("public static IReadOnlyList<(Family Family")]
knobs = {}
for key, _group, block in re.findall(r'\("(\w+)", Group\.(\w+)\) => new\[\] \{(.*?)\},\n', moves_src, re.S):
    for slug, _label, _name, tag, _secs in re.findall(
            r'M\("(\w+)", "([^"]+)", "([^"]+)", "([\w.]+)", ([\d.]+)\)', block):
        knobs.setdefault(key, []).append((f"combat.mutantMoveRate.{key}.{slug}", full(tag)))

check(len(families) == 14 and sum(len(v) for v in knobs.values()) == 53, "14 families, 53 knobs parsed")
for token in ("var tags = agent.VanillaMasterTags.Concat(knobs.Keys).Distinct(StringComparer.Ordinal);",
              "MutantAggressionPolicy.Multiplier(agent, tag, knob.Rate, knob.Reference)",
              'difficultySid == "Stalker" && agent.VanillaMasterTags.Contains(tag)',
              "if (Math.Abs(multiplier - 1.0) >= 0.0000001)",
              'mutant.RemoveAssignment("AgentCooldownMultipliers");',
              'GetOrAddChild("AgentCooldownMultipliers", useBpatch: false)',
              'entry.GetOrAddChild("CooldownMultipliers", useBpatch: false)',
              'document.GetOrAddNode(difficultySid, new[] { "MutantCombatDifficulty" })'):
    check(token in writer, "writer token: " + token)


def reference(pid, difficulty):
    v = catalog[pid]["vanilla"]
    rank = {"Easy": "Novice", "Medium": "Stalker", "Hard": "Veteran", "Stalker": "Master"}[difficulty]
    return v.get(rank, v.get("All"))


def multiplier(listed, rate, ref):
    # MutantAggressionPolicy.Multiplier
    if abs(rate - ref) < 1e-7:
        return 0.5 if ref > 1.0 and listed else 1.0
    return 1.0 / rate


GATED_SECS = {"combat.mutantMoveRate.bloodsucker.leap": 20, "combat.mutantMoveRate.boar.charge": 12,
              "combat.mutantMoveRate.deer.charge": 10, "combat.mutantMoveRate.pseudogiant.charge": 10,
              "combat.mutantMoveRate.pseudogiant.shockwave": 8}


def cap(pid, special, kref):
    # MutantAggressionPolicy.KnobCap: wait ≥ 8 s ÷ SPECIAL MOVES, vanilla always allowed.
    if pid not in GATED_SECS:
        return float("inf")
    import math
    c = math.floor(GATED_SECS[pid] * special / 8.0 * 4.0 + 1e-9) / 4.0
    return min(max(c, kref, 0.25), 3.0)


def simulate(difficulty, values, special=None):
    """Ordered [(agent, [(tag, multiplier)])] exactly as the C# writer emits it."""
    if special is None:
        special = 2.0 if difficulty == "Stalker" else 1.0
    out = []
    for key, agents in families.items():
        kn = {}
        for pid, tag in knobs.get(key, []):
            # KnobReference: 2× on Master where any agent of the family lists the tag.
            kref = 2.0 if difficulty == "Stalker" and any(tag in t for _, t in agents) else 1.0
            check_ref = reference(pid, difficulty)
            if abs(kref - check_ref) > 1e-9:
                failures.append(f"{pid}: catalog vanilla {check_ref} != writer reference {kref}")
            kn[tag] = (min(values.get(pid, check_ref), cap(pid, special, kref)), kref)
        for sid, master in agents:
            entries = []
            for tag in dict.fromkeys(master + list(kn)):
                if tag in kn:
                    m = multiplier(tag in master, *kn[tag])
                else:
                    m = 0.5 if difficulty == "Stalker" and tag in master else 1.0
                if abs(m - 1.0) >= 1e-7:
                    entries.append((tag, m))
            if entries:
                out.append((sid, entries))
    return out


def fmt(v):
    s = f"{v:.4f}".rstrip("0").rstrip(".")
    return s


def render(difficulty, lst):
    lines = [f"{difficulty} : struct.begin {{bpatch}}", "    MutantCombatDifficulty : struct.begin {bpatch}",
             "        AgentCooldownMultipliers : struct.begin"]
    for i, (sid, entries) in enumerate(lst):
        lines += [f"            [{i}] : struct.begin", f"                AgentPrototypeSID = {sid}",
                  "                CooldownMultipliers : struct.begin"]
        for t, (tag, m) in enumerate(entries):
            lines += [f"                    [{t}] : struct.begin", f"                        CooldownMultiplier = {fmt(m)}",
                      f"                        CooldownTag = {tag}", "                    struct.end"]
        lines += ["                struct.end", "            struct.end"]
    lines += ["        struct.end", "    struct.end", "struct.end"]
    return "\n".join(lines) + "\n"


# ---- model checks (no game files needed) --------------------------------------
vanilla_master = simulate("Stalker", {})
for d in ("Easy", "Medium", "Hard"):
    check(simulate(d, {}) == [], f"{d}: every knob at vanilla writes an empty list (game value)")
one = "combat.mutantMoveRate.controller.psy"
for d, ref in (("Medium", 1.0), ("Stalker", 2.0)):
    lst = dict(simulate(d, {one: 3.0}))
    psy = {sid: dict(e).get("Abilities.PSYAttack") for sid, e in lst.items()}
    want = {sid: round(1 / 3, 4) for sid, _ in families["controller"]}
    check({k: round(v, 4) for k, v in psy.items() if v} == want,
          f"{d}: CONTROLLER PSY 3× → 1/3 on all {len(want)} Controller agents")
    others = [(sid, tag) for sid, e in lst.items() for tag, _ in e
              if tag != "Abilities.PSYAttack" and not (d == "Stalker")]
    check(not others, f"{d}: nothing but PSY written {others[:3]}")
lst = dict(simulate("Stalker", {"combat.mutantMoveRate.bayun.claw": 1.0}))
check(dict(lst["Bayun"]).get("Ability.Cooldown.ClawAttack") is None
      and dict(lst["Bayun"]).get("Ability.Cooldown.Growl") == 0.5,
      "Master: Bayun CLAW turned down to 1× drops its 0.5, the uncontrolled growl keeps 0.5")
wave = "combat.mutantMoveRate.pseudogiant.shockwave"
check(simulate("Medium", {wave: 3.0}) == [],
      "Medium: WAVE 3× with SPECIAL MOVES 1× is capped at the 8 s pause (= vanilla, nothing written)")
_w = dict(simulate("Medium", {wave: 3.0}, special=2.0)).get("Pseudogiant", [])
check(dict(_w).get("Ability.Cooldown.ShockwaveAttack") == 0.5,
      "Medium: WAVE 3× with SPECIAL MOVES 2× caps at 2× (wait 4 s = the 4 s pause)")
check("MutantAggressionPolicy.KnobCap(family, move, specialRate, double.MaxValue, reference)" in writer,
      "writer token: gated knobs capped by SPECIAL MOVES")
sample = render("Stalker", simulate("Stalker", {one: 3.0, "combat.mutantMoveRate.pseudodog.phantoms": 2.0}))

# ---- game-file checks -----------------------------------------------------------
game = None
args = sys.argv[1:]
if "--game" in args:
    game = Path(args[args.index("--game") + 1])
elif os.environ.get("TCD_BASE_DUMP"):
    game = Path(os.environ["TCD_BASE_DUMP"]) / "GameData"
if "--sample" in args:
    Path(args[args.index("--sample") + 1]).write_text(sample, encoding="utf-8")

if game is None or not (game / "DifficultyPrototypes.cfg").is_file():
    print("[NOT RUN] game-file checks: pass --game <GameData> or set TCD_BASE_DUMP")
else:
    diff, _ = parse(str(game / "DifficultyPrototypes.cfg"))
    objs, _ = parse(str(game / "ObjPrototypes.cfg"))
    abil, _ = parse(str(game / "AbilityPrototypes.cfg"))

    def kid(n, name):
        return next((c for c in n["kids"] if c["name"] == name), None)

    def kv(n, key):
        return dict(n["kv"]).get(key)

    acm = kid(kid(diff["Stalker"], "MutantCombatDifficulty"), "AgentCooldownMultipliers")
    game_list = [(kv(e, "AgentPrototypeSID"),
                  [(kv(t, "CooldownTag"), float(kv(t, "CooldownMultiplier").rstrip("f")))
                   for t in kid(e, "CooldownMultipliers")["kids"]]) for e in acm["kids"]]
    norm = lambda l: sorted((s, sorted(set(e))) for s, e in l)  # the game lists Burer WeaponDrag twice
    check(norm(vanilla_master) == norm(game_list),
          f"Master, all knobs vanilla: our list == the game's own list ({len(game_list)} agents)")
    for d in ("Easy", "Medium", "Hard"):
        m = kid(diff[d], "MutantCombatDifficulty") or kid(diff["Empty"], "MutantCombatDifficulty")
        check(kid(m, "AgentCooldownMultipliers") is None, f"{d}: game has no own list (empty =), ours starts empty")
    tags = {kv(n, "CooldownTag") for n in abil.values() if kv(n, "CooldownTag")}
    sids = {sid for v in families.values() for sid, _ in v}
    check(not sorted(s for s in sids if s not in objs), f"all {len(sids)} AgentPrototypeSIDs exist in ObjPrototypes")
    used = {t for v in knobs.values() for _, t in v} | {t for v in families.values() for _, m in v for t in m}
    check(not sorted(t for t in used if t not in tags), f"all {len(used)} CooldownTags exist in AbilityPrototypes")
    # render parses back: balanced, contiguous indices
    import tempfile
    with tempfile.NamedTemporaryFile("w", suffix=".cfg", delete=False, encoding="utf-8") as handle:
        handle.write(sample)
        tmp = Path(handle.name)
    back, _ = parse(str(tmp))
    tmp.unlink(missing_ok=True)
    lst_node = kid(kid(back["Stalker"], "MutantCombatDifficulty"), "AgentCooldownMultipliers")
    idx_ok = [c["name"] for c in lst_node["kids"]] == [f"[{i}]" for i in range(len(lst_node["kids"]))]
    check(sample.count("struct.begin") == sample.count("struct.end") and idx_ok,
          f"rendered patch parses back: balanced structs, [0..{len(lst_node['kids']) - 1}] indices")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
print("\nMUTANT COOLDOWN CFG writer OK")
