#!/usr/bin/env python3
"""Canonical 12-role alcohol, bleeding, breakable-box and Trader Stock contract."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
policy = json.loads((ROOT / "tools/factory_preset_four_domain_balance_2026_11_19.json").read_text(encoding="utf-8"))
catalog = {p["id"]: p for p in json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]}
names = {f.stem for f in (APP / "Presets").glob("*.json")}
assert len(names) == 12 and set(policy["roles"]) == names
ids = set(policy["ids"])
assert len(ids) == 11
assert ids == {
    "survival.beerDrunknessStrength", "survival.vodkaDrunknessStrength",
    "survival.bleedingMediumThreshold", "survival.bleedingStrongThreshold",
    "loot.destructibleAmmoQuantity", "loot.ammoBoxWeight918",
    "loot.ammoBoxWeight045", "loot.ammoBoxWeight545", "loot.ammoBoxWeight012",
    "loot.medicalBoxArmyMedkitWeight", "loot.destructibleGrenadeQuantity",
}
trader_ids = {k for k in catalog if k.startswith("economy.traderSells.")}
assert len(trader_ids) == 31
assert len({k for k in trader_ids if ".technician." not in k}) == 25

stock_count = 0
alcohol = set()
thresholds = set()
mixes = set()
modified = 0

def grid(number, base, step):
    return abs((number - base) / step - round((number - base) / step)) < 1e-7

for name, role in sorted(policy["roles"].items()):
    preset = json.loads((APP / "Presets" / (name + ".json")).read_text(encoding="utf-8"))
    assert preset["name"] == name and len(preset["values"]) == len(catalog) == 452, name
    assert set(role["values"]) == ids and len(role["intent"]) >= 35, name
    for key, expected in role["values"].items():
        meta = catalog[key]["slider"]
        assert preset["values"][key] == {"kind": "slider", "number": expected}, (name, key)
        assert meta["min"] <= expected <= meta["max"], (name, key, expected)
        assert grid(expected, meta["min"] if meta.get("stepMode") == "absolute" else 0, meta["step"]), (
            name, key, expected
        )
    values = role["values"]
    a = (values["survival.beerDrunknessStrength"], values["survival.vodkaDrunknessStrength"])
    b = (values["survival.bleedingMediumThreshold"], values["survival.bleedingStrongThreshold"])
    m = tuple(values[k] for k in (
        "loot.ammoBoxWeight918", "loot.ammoBoxWeight045",
        "loot.ammoBoxWeight545", "loot.ammoBoxWeight012"
    ))
    assert a[0] < a[1] and b[0] + 15 <= b[1], name
    assert len(set(m)) >= 2 and max(m) > min(m), name
    alcohol.add(a)
    thresholds.add(b)
    mixes.add(m)
    assert preset["values"]["survival.drunknessThresholdScale"] == {
        "kind": "slider", "number": 1
    }, name
    # Category toggles are still independently ON; partial odds are handled
    # by the existing validated 5%-step TraderSellOdds preset sidecar.
    assert all(preset["values"][k] == {"kind": "toggle", "boolean": True}
               for k in trader_ids), name
    actual = preset.get("traderSellOdds")
    expected = role["traderSellOdds"]
    assert actual == expected and len(expected) >= 20, name
    assert set(expected) <= trader_ids and all(".technician." not in k for k in expected)
    for category, odds in expected.items():
        assert .05 <= odds <= .95 and grid(odds, 0, .05), (name, category, odds)
    stock_count += len(expected)
    modified += len(values)

assert len(alcohol) == len(thresholds) == len(mixes) == 12
assert stock_count == 289, stock_count
assert policy["roles"]["Hardcore+"]["values"]["survival.beerDrunknessStrength"] >= 30
assert policy["roles"]["Smuggler"]["values"]["survival.beerDrunknessStrength"] <= 10
assert "economy.traderSells.medic.medical" not in policy["roles"]["Field Medic"]["traderSellOdds"]
assert "economy.traderSells.bartender.drinks" not in policy["roles"]["Smuggler"]["traderSellOdds"]
assert policy["roles"]["Hardcore+"]["traderSellOdds"]["economy.traderSells.general.ammo"] < .5
assert policy["roles"]["Mercenary"]["traderSellOdds"]["economy.traderSells.general.ammo"] >= .9

print(f"PASS: {len(names)} profiles, {modified} source-authored values, 12 unique alcohol/bleeding/mix profiles")
print(f"PASS: {stock_count} source-native trader category odds, 5% steps, 31 original SELL toggles retained")
print("NOT TESTED: WPF/Wine live interaction or STALKER 2 spawned-container/trader inventories")
