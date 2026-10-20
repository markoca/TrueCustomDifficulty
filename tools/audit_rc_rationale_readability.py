#!/usr/bin/env python3
"""Release gate: 180 clear WHAT IT DOES explanations, source semantics protected."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
src = ROOT / "src/TrueCustomDifficulty"
catalog = json.loads((src / "Data/parameters.json").read_text(encoding="utf-8"))
rows = catalog["parameters"]
visible = {p["id"]: p for p in rows if p.get("exposeInUi", True)}
assert len(rows) == 285 and len(visible) == 180, "Catalog census changed"
banned = ("CFG", "ActorSID", "SmartLoot", "ItemGenerator", "SHA256",
          "per-entry", "source-native", "native Weight", "authored", "prototype",
          "backend")
for pid, row in visible.items():
    sentence = (row.get("rationale") or "").strip()
    assert 40 <= len(sentence) <= 170, (pid, "Missing, verbose or clipped rationale", sentence)
    assert sentence.endswith((".", "!", "?")), (pid, "Not a complete sentence")
    assert not any(x.lower() in sentence.lower() for x in banned), (pid, "Technical source jargon")
    assert not re.search(r"\b(right-click|press R|drag the slider|hover over)\b",
                         sentence, re.I), (pid, "Move input instructions to CONTROLS")

critical = {
    "loot.stashGearCoveragePercent": ("share", "not the chance"),
    "loot.stashGearBonusChance": ("each", "not guarantee"),
    "loot.stashMixMedical": ("priority", "not a drop percentage"),
    "loot.mediumStashMedicalChance": ("original chance", "does not guarantee"),
    "loot.expensiveStashMoneyChance": ("coupon", "progression"),
    "loot.cheapStashAmmoQuantity": ("Basic, Standard and Rare", "stacks"),
    "survival.starvationStaminaDrain": ("actions", "not passive"),
    "economy.buyCondition": ("traders offer", "sell"),
    "economy.sellCondition": ("traders charge", "buy"),
    "survival.sleepHealthRestore": ("normal sleep", "300 HP")
}
for pid, parts in critical.items():
    text = visible[pid]["rationale"].lower()
    assert all(s.lower() in text for s in parts), (pid, "Gameplay caveat missing", parts)

for pid in ("loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
            "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
            "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
            "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance"):
    assert "chance" in visible[pid]["rationale"].lower(), (pid, "Chance rationale missing")

vm = (src / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
for key in ('"StashContentMix" => "Ammo Quantity changes',
            '"StashMedicalComposition" => "DROP ROLLS changes',
            '"StashGear" => "BONUS GEAR COVERAGE chooses',
            '"TradingDashboard" => "PRICES TRADERS PAY',
            '"HumanCombatProfile" => "Drag the bottom ring'):
    assert key in vm, ("Outdated player guidance", key)
for f in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md"):
    assert "CURRENT RC rationale readability lock (180 visible)" in (
        ROOT / "docs" / f).read_text(encoding="utf-8"), f
print("PASS: 180 readable player rationales, 8 new stash descriptions, 25 help families and source distinctions")
print("NOT RUN: .NET build, Wine visual inspection or gameplay tests")
