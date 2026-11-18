#!/usr/bin/env python3
"""Release gate: every player-facing panel text follows docs/PANEL-TEXT-STYLE.md.

Scope: WHAT IT DOES (rationale), HIGHER / LOWER (logic) and WITH OTHER
SETTINGS (interaction) of every control the panel can show, the NPC LOOT
card texts and every CONTROLS help string.  Legacy compatibility-only rows
(no visible control) are listed in LEGACY and skipped.

Run with --report to list every hit without failing (style work), without
it the script exits 1 on the first-class rules.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
CATALOG = json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]
REPORT = "--report" in sys.argv

# Rows with no control of their own: kept only so older presets still load.
LEGACY = {
    "loot.stashMixAmmo", "loot.stashMixGrenades",
    "loot.standardStashChanceScale", "loot.rareStashChanceScale",
    "loot.mediumStashAmmoQuantity", "loot.expensiveStashAmmoQuantity",
    "survival.drunknessThresholdScale", "economy.repairDifficultyCost",
    "alife.mutantSpawn.generic",  # not used: per-species limits only
}
SKIP_GROUPS = {"Trader Buys"}  # legacy BUY restrictions: CLEAR notice only

# ---- 1. Tech speak: word -> what to say instead (docs/PANEL-TEXT-STYLE.md §3)
TECH = {
    r"\bCFG\b": "name the effect, not the file",
    r"\bSIDs?\b": "name the item",
    r"\b[Nn]ative\b": "vanilla / the game's",
    r"\bMin(?:Count)?/Max(?:Count)?\b|\bMinCount\b|\bMaxCount\b": "stack size",
    r"\b[Gg]enerators?\b": "loot list",
    r"\b[Pp]rototypes?\b": "item",
    r"\bauthored\b": "vanilla",
    r"\b[Ee]ntr(?:y|ies)\b": "item / offer / spot",
    r"\b(?:box|loot|item) pools?\b": "loot / list",
    r"\bweighted picks?\b|\b(?:Basic|shared) pick\b": "how often it turns up",
    r"\b[Rr]olls?\b|\brolled\b": "chance / turns up",
    r"\bcoefficient\b|\bcoef\b": "strength",
    r"\bparameters?\b": "setting / control",
    r"\b[Bb]undle\b": "these penalties",
    r"\blayer(?:s|ed)?\b": "on top",
    r"\bcomponent\b": "part",
    r"\bactor(?:-level)?\b": "you",
    r"\bconfigured\b": "set",
    r"\bbaseline\b": "vanilla",
    r"\bclamp(?:ed|s)?\b": "capped",
    r"\bruntime\b": "in game",
    r"\bbackings?\b|\bstored\b": "saved",
    r"\b(?:the game|you) (?:is|are|enters?) (?:in )?(?:its|the) [\w\- ]*state\b": "while you're …",
    r"\bstate\b": "while you're … / when …",
    r"\bSmartLoot\b|\bSmart_": "SMART LOOT",
    r"\b(?:verified|audited|acceptance test|TODO|not supported yet|not individually adjustable yet)\b": "drop the dev note",
    r"\b[Oo]riginal\b(?! spots)": "vanilla",
    r"\b[a-z]+[A-Z][A-Za-z]+\d*\b": "internal identifier",
}
# ---- 2. Format rules (§2)
FORMAT = [
    (r"(?<![\w.])\d+(?:\.\d+)?x\b", "multiplier sign is ×"),
    (r"\b\d+(?:\.\d+)?-\d+(?:\.\d+)?\b", "numeric range uses an en dash"),
    (r"->", "use →"),
    (r"\w - \w", "separator is · (or – for ranges), not a hyphen"),
    (r"\b\d+(?:\.\d+)? (?:seconds?|minutes?)\b", "time after a number is s / min (45 s, 5 min)"),
    (r"Vanilla:[^.]*\b(?:Novice|Stalker|Veteran) and (?:Stalker|Veteran|Master)\b", "neighbouring difficulties merge with an en dash (Novice–Stalker)"),
    (r"(?<![\w\-])\d+(?:\.\d+)?s\b", "space before the unit: 45 s"),
    (r"\n", "WHAT IT DOES is one paragraph"),
    (r"[Vv]anilla[^.]*\bon (?:every|all) difficult", "Vanilla: X. (no 'on every difficulty')"),
    (r"the game's odds|GAME ODDS", "say vanilla"),
]
# HIGHER / LOWER opens with the direction or the value that sets it (§1).
LOGIC_OPENERS = (r"^(Higher|Lower) = |^Higher MIN|^[0-9.]+(?:×|%| HP| s|–|\b)|^OFF|^ON|^Follows|^Each |^EFFECTIVE|^Keep |^Only |^Set |^From |^Every |^Shown |^While |^Once |^100% |^More negative|^Negative|^Range")
WORKS_WITH_BAD = r"^(This |These |How much|The game |Each radar|Drag |Affects eligible|Coverage decides|Changes only|Pair this|Use |Leave the|Sits beside|Set on|Moves in|Horizontal|Does not)"
LIMITS = {"rationale": 300, "logic": 200, "interaction": 220}


def lint(where, text, kind):
    hits = []
    for pat, fix in TECH.items():
        for m in re.finditer(pat, text):
            hits.append((where, kind, f"tech speak '{m.group(0)}' -> {fix}"))
    for pat, fix in FORMAT:
        if kind != "rationale" and fix.startswith("WHAT IT DOES"):
            continue
        if re.search(pat, text):
            hits.append((where, kind, fix))
    limit = LIMITS.get(kind)
    if limit and len(text) > limit:
        hits.append((where, kind, f"too long ({len(text)} > {limit})"))
    return hits


hits = []
by_id = {p["id"]: p for p in CATALOG}
for p in CATALOG:
    if p["id"] in LEGACY or p["group"] in SKIP_GROUPS:
        continue
    for kind in ("label", "rationale", "logic", "interaction"):
        text = p.get(kind) or ""
        if text:
            hits += lint(p["id"], text, kind)
    hint = p.get("lootHint") or {}
    hint_texts = [hint.get(k) or "" for k in ("label", "unit", "text")]
    hint_texts += [e.get(k) or "" for e in hint.get("entries") or [] if isinstance(e, dict)
                   for k in ("label", "qualifier", "selection")]
    for text in filter(None, hint_texts):
        hits += lint(p["id"], text, "lootHint")
        if re.search(r"\beligible\b|\bcrates?\b|\bWeight \d|\d/\d", text):
            hits.append((p["id"], "lootHint", f"example line '{text}': say Rare stashes / box / % share / ×"))
    value_label = (p.get("display") or {}).get("valueLabel") or ""
    if value_label:
        hits += lint(p["id"], value_label, "valueLabel")
        if value_label[0].islower():
            hits.append((p["id"], "valueLabel", "value label starts with a capital"))
    r = p.get("rationale") or ""
    if r and not re.search(r"\bVanilla\b", r):
        hits.append((p["id"], "rationale", "missing 'Vanilla:'"))
    elif r and len(re.findall(r"[.!?](?:\s|$)", r[:r.find("Vanilla")])) != 1:
        hits.append((p["id"], "rationale", "'Vanilla:' must follow the first sentence"))
    logic = p.get("logic") or ""
    if logic and not re.match(LOGIC_OPENERS, logic):
        hits.append((p["id"], "logic", "HIGHER / LOWER must open with a direction (Higher = / Lower = / a value / OFF / ON)"))
    inter = p.get("interaction") or ""
    if not inter.strip():
        hits.append((p["id"], "interaction", "WORKS WITH is empty; say what stacks or stays separate"))
    if inter and re.match(WORKS_WITH_BAD, inter):
        hits.append((p["id"], "interaction", "WORKS WITH must name what stacks / stays separate, not open with 'This …' or a UI how-to"))
    if re.search(r"\b(right-click|[Dd]rag this|radar point|eligible)\b", inter):
        hits.append((p["id"], "interaction", "UI how-to or jargon in WORKS WITH; mouse tips belong in CONTROLS"))
    if re.search(r"\b[Vv]anilla (?:base )?[+−-]?\d", logic) or re.search(r"\bvanilla (?:Master|Novice|Stalker|Veteran)\b", logic):
        hits.append((p["id"], "logic", "vanilla value belongs in WHAT IT DOES, not HIGHER / LOWER"))
    if re.match(r"(Higher|Lower) =", logic) and re.search(r"(?:^|[.!?] )(Higher|Lower) = ", r):
        hits.append((p["id"], "rationale", "'Higher = …' repeats HIGHER / LOWER; keep it there only"))
    master = p.get("requiresToggle")
    if master and master in by_id and "Only works while" not in r:
        hits.append((p["id"], "rationale", "missing 'Only works while … is ON.'"))

# NPC LOOT cards + CONTROLS help (C# string literals)
def literals(source, minimum):
    for line in source.splitlines():
        if line.lstrip().startswith("//"):
            continue
        for m in re.finditer(r'"((?:[^"\\]|\\.){%d,})"' % minimum, line):
            if " " in m.group(1):  # ids such as "survival.x" are code, not text
                yield m.group(1)


for f, kind in ((APP / "ViewModels/NpcLootCards.cs", "card"),):
    for text in literals(f.read_text(encoding="utf-8"), 40):
        hits += lint(f.name, text, kind)
vm = (APP / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
help_block = vm[vm.index("public string ControlHelp"):]
help_block = help_block[: help_block.index("\n    public ", 20)]
for text in literals(help_block, 30):
    hits += lint("ControlHelp", text, "help")

# Every other on-screen string in the UI layer (boards, panel blocks,
# dialogs, preset load errors): interpolations are blanked first.
UI_FILES = sorted((APP / "Controls").glob("*.cs")) + sorted((APP / "ViewModels").glob("*.cs")) \
    + [APP / "Services/PresetService.cs"]
UI_ALLOW = (
    "BPatch CFG file", "concrete CFG edit",  # build summary for modders
)
for f in UI_FILES:
    for text in literals(f.read_text(encoding="utf-8"), 12):
        if "{(" in text:  # conditional interpolation: code, not copy
            continue
        text = re.sub(r"\{[^{}]*\}", "N", text)
        if any(a in text for a in UI_ALLOW):
            continue
        for pat, fix in TECH.items():
            if pat.startswith(r"\b[a-z]+[A-Z]"):  # identifiers are expected in code
                continue
            m = re.search(pat, text)
            if m:
                hits.append((f.name, "ui", f"tech speak '{m.group(0)}' -> {fix}: {text[:80]}"))
                break
        for pat, fix in FORMAT[:3]:
            if re.search(pat, text):
                hits.append((f.name, "ui", f"{fix}: {text[:80]}"))

if REPORT:
    for h in hits:
        print(" | ".join(h))
    print(f"{len(hits)} style hits")
    sys.exit(0)
# Units next to a number are lower case (HP stays HP): 80 kg, 12 pts, 0.5 pts/s, 130 cm.
for f in UI_FILES:
    for n, line in enumerate(f.read_text().splitlines(), 1):
        if re.search(r'(\}|\d) (KG|PTS|PTS/S|RAD|SEC|UU|uu)"', line):
            hits.append((f"{f.name}:{n}", "ui", "unit after a number is lower case (kg, pts, pts/s, rad, s, cm)"))

# MIN / MAX are always abbreviated in names and captions.
for p in CATALOG:
    for field in (p.get("label") or "", (p.get("display") or {}).get("valueLabel") or ""):
        if re.search(r"\b(Minimum|Maximum)\b", field):
            hits.append((p["id"], "label", "write Min / Max in names"))
for f in UI_FILES:
    for n, line in enumerate(f.read_text().splitlines(), 1):
        if re.search(r'"[^"]*\b(MINIMUM|MAXIMUM)\b', line):
            hits.append((f"{f.name}:{n}", "ui", "caption says MIN / MAX"))

# CAPS names in panel texts must be something the player can find on screen:
# a control label or a caption the UI code draws.
_labels = {(p.get("label") or "").upper() for p in CATALOG}
_ui_code = " ".join(f.read_text() for f in (APP).rglob("*.cs")).upper()
for p in CATALOG:
    if p["id"] in LEGACY or p["group"] in SKIP_GROUPS:
        continue
    for kind in ("rationale", "logic", "interaction"):
        for name in re.findall(r"\b[A-Z][A-Z0-9\-'&/]+(?: [A-Z][A-Z0-9\-'&/]+)*\b", p.get(kind) or ""):
            name = re.sub(r" (ON|OFF)$", "", name)
            if len(name) < 4 or name in _labels or f'"{name}' in _ui_code \
                    or f'{name}"' in _ui_code or f" {name} " in _ui_code:
                continue
            hits.append((p["id"], kind, f"CAPS name '{name}' is not a control or caption on screen"))

if hits:
    for h in hits[:40]:
        print("FAIL", " | ".join(h))
    print(f"FAIL {len(hits)} panel-text style hits (run with --report for all)")
    sys.exit(1)
print("PASS panel text style: no tech speak, × / – / → typography, Vanilla and master hints present")
