#!/usr/bin/env python3
"""Audit the exported 2.1 trader offer graph; fail closed on unknown item categories."""
import csv
import sys
from collections import Counter

MEDICAL = set("AntiRad ArmyMedkit Bandage EcoMedkit Hercules Medkit".split())
FOOD = set("Bread CannedFood MonolithDryRation Sausage".split())
DRINKS = set("Beer Energetic Milk Vodka Water".split())
GRENADES = {"GrenadeF1", "GrenadeRGD5"}
MAP = {"WeaponPrimary": "Weapons", "WeaponPistol": "Weapons",
       "WeaponSecondary": "Weapons", "Ammo": "Ammo", "BodyArmor": "Armor",
       "Head": "Armor", "Attach": "Attachments", "Artifact": "Artifacts",
       "Detector": "Detectors"}

def category(row):
    item = row["item_sid"]
    if item.startswith("Binoculars_"): return "Binoculars"
    for label, values in (("Medical", MEDICAL), ("Food", FOOD),
                          ("Drinks", DRINKS), ("Grenades", GRENADES)):
        if item in values: return label
    raw = row["source_category"].removeprefix("EItemGenerationCategory::")
    if raw == "Junk":
        if item.startswith("Gun"): return "Weapons"
        if "Armor" in item or "Helmet" in item: return "Armor"
    if raw in MAP: return MAP[raw]
    raise ValueError("Unknown category/item: " + raw + "/" + item)

def audit(path):
    with open(path, encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    required = {"trade_scope", "trade_sid", "root_sid", "generator_scope",
                "generator_sid", "entry_path", "source_category", "item_sid",
                "chance", "weight", "nested_chain", "child_ref"}
    if not rows or not required.issubset(rows[0]):
        raise ValueError("Invalid graph header")
    counts = Counter()
    for row in rows:
        counts[category(row)] += 1
        if len(row["entry_path"].split("/")) != 4:
            raise ValueError("Unexpected leaf path")
        if row["nested_chain"].split(">")[-1] != row["generator_sid"]:
            raise ValueError("Broken generator chain")
    profiles = {(r["trade_scope"], r["trade_sid"]) for r in rows}
    if len(rows) != 3150 or len(profiles) != 90:
        raise ValueError("2.1 census drift")
    print("Leaf occurrences:", len(rows), "Profiles with leaves:", len(profiles))
    print("Unique item SIDs:", len({r["item_sid"] for r in rows}))
    for name, count in sorted(counts.items()):
        print(name + ":", count)

if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("Usage: python3 tools/audit_trader_21_categories.py graph.csv")
    audit(sys.argv[1])
