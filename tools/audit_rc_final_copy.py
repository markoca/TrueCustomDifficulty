#!/usr/bin/env python3
"""RC copy freeze: compare every visible catalog field with the 182-row human review ledger.

Static provenance and UI wiring only; this is not a WPF render or game test.
"""
from __future__ import annotations

import csv
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "src/TrueCustomDifficulty/Data/parameters.json"
LEDGER = ROOT / "docs/RC-FINAL-COPY-INDEX-2026-10-19.csv"
SRC = ROOT / "src/TrueCustomDifficulty"
TAB_COUNTS = {
    "combat": 17, "survival": 35, "health": 13, "master": 20,
    "stamina": 7, "loot": 27, "stashes": 34, "economy": 17,
    "equipment": 4, "environment": 6, "alife": 2,
}
FIELDS = ("id", "tab", "group", "label", "rationale", "logic", "interaction")
JARGON = re.compile(
    r"\b(?:CFG|sourceRaw|SmartLoot|ItemGenerator|source-native|prototype|"
    r"authored|authoring|backend|per-entry|refkey|integer-half-up|legacy)\b",
    re.I,
)
GESTURES = re.compile(
    r"\b(?:drag|hover|wheel|right.click|knob|slider|track|keyboard|"
    r"arrow keys|R resets)\b", re.I,
)
LABEL_JARGON = re.compile(r"\b(?:SmartLoot|ItemGen|CFG|SID|Scalar|Prototype)\b", re.I)


def demand(ok: bool, why: str) -> None:
    if not ok:
        raise AssertionError(why)


def main() -> int:
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    all_params = catalog["parameters"]
    visible = [p for p in all_params if p.get("exposeInUi", True)]
    demand(len(all_params) == 287 and len(visible) == 182,
           "current 287/182 catalog boundary changed; re-review all visible copy")
    demand(len({p["id"] for p in all_params}) == len(all_params), "duplicate catalog IDs")
    demand(dict(Counter(p["tab"] for p in visible)) == TAB_COUNTS,
           "visible per-tab inventory drifted; redo RC review")

    with LEDGER.open(encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        demand(reader.fieldnames == [*FIELDS, "decision"], "review ledger header changed")
        ledger = list(reader)
    demand(len(ledger) == 182, "not all 182 visible settings have been reviewed")
    by_id = {entry["id"]: entry for entry in ledger}
    demand(len(by_id) == 182 and len({p["label"] for p in visible}) == 182,
           "duplicate IDs or ambiguous visible labels")
    demand({p["id"] for p in visible} == set(by_id), "unreviewed/new/retired visible ID")

    for p in visible:
        pid = p["id"]
        entry = by_id[pid]
        for field in FIELDS:
            demand((p.get(field) or "") == (entry[field] or ""),
                   pid + ": " + field + " is not the reviewed RC copy")
        demand(entry["decision"] in ("retained", "revised"), pid + ": no review disposition")
        name = p["label"]
        rationale = p.get("rationale") or ""
        demand(3 <= len(name) <= 39 and not LABEL_JARGON.search(name),
               pid + ": long or internal player-facing label")
        demand(40 <= len(rationale) <= 170 and rationale.endswith((".", "?", "!")),
               pid + ": incomplete WHAT IT DOES")
        demand(name.casefold().strip() != rationale.casefold().strip(),
               pid + ": label echoed as entire WHAT IT DOES")
        for field in ("rationale", "logic", "interaction"):
            value = p.get(field) or ""
            demand(not JARGON.search(value), pid + ": " + field + " contains developer jargon")
            demand(not GESTURES.search(value),
                   pid + ": " + field + " contains CONTROLS instructions")
        demand((p.get("logic") or "").strip().casefold()
               != (p.get("interaction") or "").strip().casefold()
               or not (p.get("logic") or "").strip(),
               pid + ": duplicated HIGHER / LOWER and WORKS WITH")
        demand(p["label"].strip() == p["label"], pid + ": label padding")
        demand("  " not in name, pid + ": doubled label spaces")

    by_source_id = {p["id"]: p for p in visible}
    for pid in ("loot.stashMixMedical", "loot.stashMixFood", "loot.stashMixDrinks"):
        p = by_source_id[pid]
        demand("Priority" in p["label"] and "chance" in p["interaction"].lower(),
               pid + ": priority was incorrectly presented as an absolute chance")
    for pid in ("loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
                "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
                "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
                "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance"):
        p = by_source_id[pid]
        demand("Chance" in p["label"] and "100%" in p["logic"]
               and "original" in p["logic"] and "guarantee" in p["logic"],
               pid + ": relative original chance semantics missing")
    demand(by_source_id["loot.stashMedicalAntiRadBias"]["label"] == "Anti-Rad Availability",
           "Anti-Rad ON/OFF gate mislabeled as a loot priority knob")
    demand(by_source_id["survival.starvationStaminaDrain"]["label"]
           == "Starvation Action Cost", "Master active stamina-cost naming drift")
    demand("actions" in by_source_id["survival.starvationStaminaDrain"]["rationale"].lower()
           and "not passive" in by_source_id["survival.starvationStaminaDrain"]["rationale"].lower(),
           "Starvation Action Cost incorrectly advertised as passive drain")
    demand("100" in by_source_id["survival.milkSatiety"]["rationale"],
           "Condensed Milk must not imply more than 100 hunger points")
    demand(by_source_id["economy.buyCondition"]["label"] == "Prices Traders Pay"
           and by_source_id["economy.sellCondition"]["label"] == "Prices You Pay",
           "trader buy/sell direction reversed")
    demand("not the chance" in by_source_id["loot.stashGearCoveragePercent"]["rationale"].lower(),
           "stash coverage incorrectly called bonus chance")
    demand("not guarantee" in by_source_id["loot.stashGearBonusChance"]["rationale"].lower(),
           "bonus chance wording promises guaranteed items")
    demand(by_source_id["combat.accumulatedDamageWeightMin"]["label"]
           == "Minimum Hit Protection"
           and by_source_id["combat.accumulatedDamageWeightMax"]["label"]
           == "Peak Hit Protection", "repeated-hit caps mislabeled as percentages")
    demand(by_source_id["loot.mutant.catChance"]["label"] == "Bayun Part Chance",
           "internal cat SID must remain displayed as Bayun")
    for p in visible:
        if p["id"].startswith("loot.mutant.") and p["id"] != "loot.mutant.blindDogChance":
            demand(p["label"].endswith(" Part Chance") and not p.get("logic")
                   and not p.get("interaction"),
                   p["id"] + ": probability meaning was duplicated on every mutant row")

    vm = (SRC / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
    xaml = (SRC / "MainWindow.xaml").read_text(encoding="utf-8")
    demand("Each eligible mutant kill rolls for one part." in vm
           and 'return $"{CurrentDisplay} is the actual drop chance.' not in vm,
           "mutant probability side-panel still echoes current numeric chance")
    demand("if (IsChoiceSlider || IsToggle)" in vm,
           "generic choice/toggle repeat in value meaning was reintroduced")
    for heading in ("WHAT IT DOES", "WHAT THIS VALUE MEANS", "HIGHER / LOWER",
                    "WORKS WITH", "CONTROLS"):
        demand(xaml.count('Text="' + heading + '"') == 1,
               "global player-help heading duplicated or removed: " + heading)
    for doc in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md"):
        text = (ROOT / "docs" / doc).read_text(encoding="utf-8")
        demand("CURRENT FINAL RC COPY FREEZE — 182 visible controls" in text,
               doc + ": missing current RC copy authority")
    print("PASS: reviewed every one of 182 player-visible controls across 11 tabs")
    print("PASS: names, WHAT IT DOES, HIGHER / LOWER and WORKS WITH match copy ledger")
    print("PASS: no developer jargon, duplicate-value explanations or stray gestures")
    print("PASS: independent Priority/Chance/Quantity, Master, mutant, and trader meaning")
    print("NOT RUN: .NET build, Wine visual acceptance or STALKER 2 runtime")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
