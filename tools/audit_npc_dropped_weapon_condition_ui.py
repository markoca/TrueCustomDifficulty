#!/usr/bin/env python3
"""NPC dropped-weapon range card: compact alignment and naming contract."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
app = root / "src" / "TrueCustomDifficulty"
ui = (app / "Controls" / "AggregateVisualControl.cs").read_text(encoding="utf-8")
cards = (app / "ViewModels" / "NpcLootCards.cs").read_text(encoding="utf-8")
catalog = json.loads((app / "Data" / "parameters.json").read_text(encoding="utf-8"))
condition = [p for p in catalog["parameters"] if p["id"] == "loot.droppedWeaponCondition"]
assert len(condition) == 1
assert condition[0]["label"] == "Dropped Weapon Condition"
assert condition[0]["control"] == "range" and condition[0]["slider"]["min"] == 0
assert condition[0]["slider"]["max"] == 1
assert 'DrawText(dc, "DROPPED WEAPON CONDITION"' in ui
assert 'new(WeaponCondition, "loot.droppedWeaponCondition", "DROPPED WEAPON CONDITION"' in cards
assert 'condition = new Rect(left + half + gap, top, half, scalarHeight);' in ui
assert 'condition = new Rect(left, supply.Bottom + gap, contentWidth, scalarHeight);' in ui
assert 'new Point(rangeCard.Right - 11.0, rangeCard.Top + 8.0)' in ui
assert 'new Point(minReadoutX, rangeCard.Top + 23.0)' in ui
assert 'new Point(maxReadoutX, rangeCard.Top + 23.0)' in ui
assert 'new Point(minReadoutX, rangeCard.Top + 37.0)' in ui
assert 'new Point(maxReadoutX, rangeCard.Top + 37.0)' in ui
assert 'rangeCard.Top + 56.0' in ui
assert 'AddRangeTarget(condition, fullHit, rail, HitKind.RangeMin);' in ui
assert 'AddRangeTarget(condition, fullHit, rail, HitKind.RangeMax);' in ui
print("[OK] Dropped weapon condition name, equal-height NPC row and two-handle rail")
