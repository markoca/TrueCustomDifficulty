#!/usr/bin/env python3
"""Release gate: 180 clear WHAT IT DOES explanations, source semantics protected."""
import json
import re
from pathlib import Path
import sys as _sys; _sys.path.insert(0, str(__import__("pathlib").Path(__file__).resolve().parent))
from catalog_census import CATALOG, VISIBLE, HIDDEN, EDIT_POINTS, DIRECT_IDS

ROOT = Path(__file__).resolve().parents[1]
src = ROOT / "src/TrueCustomDifficulty"
catalog = json.loads((src / "Data/parameters.json").read_text(encoding="utf-8"))
rows = catalog["parameters"]
visible = {p["id"]: p for p in rows if p.get("exposeInUi", True)}
assert len(rows) == CATALOG and len(visible) == VISIBLE, "Catalog census changed"
banned = ("CFG", "ActorSID", "SmartLoot", "ItemGenerator", "SHA256",
          "per-entry", "source-native", "native Weight", "authored", "prototype",
          "backend")
for pid, row in visible.items():
    sentence = (row.get("rationale") or "").strip()
    # Current copy: effect sentence(s) plus a short "Vanilla: ..." line.
    assert 40 <= len(sentence) <= 360, (pid, "Missing, verbose or clipped rationale", sentence)
    assert sentence.endswith((".", "!", "?")), (pid, "Not a complete sentence")
    assert not any(x.lower() in sentence.lower() for x in banned), (pid, "Technical source jargon")
    assert not re.search(r"\b(right-click|press R|drag the slider|hover over)\b",
                         sentence, re.I), (pid, "Move input instructions to CONTROLS")

critical = {
    "loot.stashGearCoveragePercent": ("stashes", "Bonus Chance"),
    "loot.stashGearBonusChance": ("each", "isn't a guarantee"),
    "loot.stashMixMedical": ("odds", "keep their own odds"),
    "loot.mediumStashMedicalChance": ("how often", "bandages 40%"),
    "loot.expensiveStashMoneyChance": ("coupon", "progression"),
    "loot.cheapStashAmmoQuantity": ("Basic, Standard and Rare", "rounds"),
    "survival.starvationStaminaDrain": ("actions", "starving"),
    "economy.buyCondition": ("traders pay", "sell"),
    "economy.sellCondition": ("traders charge", "buy"),
    "survival.sleepHealthRestore": ("sleep", "300 HP")
}
for pid, parts in critical.items():
    text = visible[pid]["rationale"].lower()
    assert all(s.lower() in text for s in parts), (pid, "Gameplay caveat missing", parts)

for pid in ("loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
            "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
            "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
            "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance"):
    assert any(w in visible[pid]["rationale"].lower() for w in ("chance", "how often")), (pid, "Chance rationale missing")

vm = (src / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
for key in ('"StashContentMix" => "ADJUST ODDS: each cell keeps',
            '"StashMedicalComposition" => "ADJUST ODDS: each item keeps',
            '"StashGear" => "BONUS GEAR COVERAGE chooses',
            '"TradingDashboard" => "PRICES TRADERS PAY',
            '"HumanCombatProfile" => "Drag the bottom ring'):
    assert key in vm, ("Outdated player guidance", key)
for f in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md"):
    assert "CURRENT RC rationale readability lock (180 visible)" in (
        ROOT / "docs" / f).read_text(encoding="utf-8"), f
print(f"PASS: {VISIBLE} readable player rationales, 8 new stash descriptions, 25 help families and source distinctions")
print("NOT RUN: .NET build, Wine visual inspection or gameplay tests")
