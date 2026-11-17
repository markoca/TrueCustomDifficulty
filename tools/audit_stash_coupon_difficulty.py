#!/usr/bin/env python3
"""Regression: never expose coupon odds controls for an absent stash CFG branch.

Source is the pinned STALKER 2.1 writer baseline; gameplay effects still
require manual in-game validation.
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src" / "TrueCustomDifficulty"
baseline = json.loads((APP / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
special = baseline["special"]
tiers = {
    "Cheap": special["cheapStashProduction"]["templateCategories"],
    "Medium": special["mediumStashAmmoProduction"]["templateCategories"],
    "Expensive": special["expensiveStashProduction"]["templateCategories"],
}
native = {"Novice": "Easy", "Stalker": "Medium",
          "Veteran": "Hard", "Master": "Stalker"}
money = {"MoneyCommon", "MoneyRare", "MoneyEpic", "MoneyLegendary"}

def authored(tier, difficulty):
    entries = []
    for group in tiers[tier]:
        fields = group["fields"]
        if "EGameDifficulty::" + difficulty not in fields.get("Diff", "").split(", "):
            continue
        for item in group["items"]:
            values = item["fields"]
            if values.get("ItemPrototypeSID") not in money:
                continue
            field = "Chance" if "Chance" in values else "Weight"
            if float(values[field].rstrip("f")) > 0:
                entries.append((fields.get("PlayerRank"), field))
    return entries

for ui_difficulty, native_difficulty in native.items():
    basic = authored("Cheap", native_difficulty)
    standard = authored("Medium", native_difficulty)
    rare = authored("Expensive", native_difficulty)
    expected = ui_difficulty in ("Novice", "Stalker")
    assert bool(basic) == expected, (ui_difficulty, "Basic", basic)
    assert bool(standard) == expected, (ui_difficulty, "Standard", standard)
    assert bool(rare), (ui_difficulty, "Rare", rare)
    if basic:
        assert all(field == "Weight" for _, field in basic)
    if standard:
        assert all(field == "Chance" for _, field in standard)
    print(f"[OK] {ui_difficulty}: Basic {bool(basic)}, Standard {bool(standard)}, Rare {bool(rare)}")

odds = (APP / "Controls/AggregateVisualControl.StashOdds.cs").read_text(encoding="utf-8")
controls = (APP / "Controls/AggregateVisualControl.StashControls.cs").read_text(encoding="utf-8")
assert 'StashCouponTierAuthored(int tier)' in odds
assert 'StashItemAuthored(StashOddsTiers[tier], sid, StashDifficultySid())' in odds
assert '&& !StashCouponTierAuthored(t)' in odds
assert '&& !StashCouponTierAuthored(t)' in controls
assert 'DrawText(dc, "—"' in odds and 'DrawText(dc, "—"' in controls
print("[OK] Source-gated coupon controls and previews keep absent tiers blank")
