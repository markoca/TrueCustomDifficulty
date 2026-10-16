#!/usr/bin/env python3
"""Rebuild/check the Stashes native generator-branch SOURCE inventory.

Stage 1 is an auditable native CFG-source index, NOT a claim of runtime
probabilities or a preview/output parity test. Standard-library only.
Run: python tools/audit_stash_generator_branches.py
Regenerate after intentional source updates:
     python tools/audit_stash_generator_branches.py --write
"""

import argparse
import csv
import io
import json
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
OUT = ROOT / "docs/generators/stash-generator-branches.csv"

COLUMNS = [
    "layer", "tier", "source_file", "source_sid", "clone_sid",
    "rank_index", "rank_gate", "difficulty_gate", "category_index",
    "group_type", "group_index", "group_path", "selection_field",
    "item_count", "weight_total", "group_min_spawn_chance",
    "group_max_spawn_chance", "item_set_count", "item_sids",
]

def native_number(value):
    return float(value.rstrip("fF"))

def mode_of(items):
    fields = {name for item in items for name in ("Weight", "Chance")
              if name in item and item[name] != ""}
    assert len(fields) == 1, ("mixed or missing selection fields", fields, items)
    return fields.pop()

def row(**values):
    assert set(values) == set(COLUMNS), (set(values) ^ set(COLUMNS))
    return {field: str(values[field]) for field in COLUMNS}

def inventory():
    baseline = json.loads((DATA / "cfg-writer-baseline.json").read_text(
        encoding="utf-8"))["special"]
    results = []
    for tier, section in [
        ("Basic", "cheapStashProduction"),
        ("Standard", "mediumStashAmmoProduction"),
        ("Rare", "expensiveStashProduction"),
    ]:
        source = baseline[section]
        clone = source["customGeneratorSid"]
        assert clone.startswith("TCD_"), (tier, clone)
        sid = clone.removeprefix("TCD_")
        for group in source["templateCategories"]:
            category = group["node"]
            assert category.startswith("[") and category.endswith("]")
            index = int(category[1:-1])
            attrs = group["fields"]
            items = [x["fields"] for x in group["items"]]
            assert items and all(x.get("ItemPrototypeSID") for x in items)
            selection = mode_of(items)
            results.append(row(
                layer="ItemGenerator",
                tier=tier,
                source_file="GameData/ItemGeneratorPrototypes.cfg",
                source_sid=sid,
                clone_sid=clone,
                rank_index="",
                rank_gate=attrs.get("PlayerRank", ""),
                difficulty_gate=attrs.get("Diff", ""),
                category_index=index,
                group_type=attrs.get("Category", ""),
                group_index="",
                group_path=f"ItemGenerator.[{index}].PossibleItems",
                selection_field=selection,
                item_count=len(items),
                weight_total=(f"{sum(native_number(x['Weight']) for x in items):.6f}"
                              if selection == "Weight" else ""),
                group_min_spawn_chance="",
                group_max_spawn_chance="",
                item_set_count="",
                item_sids=";".join(x["ItemPrototypeSID"] for x in items),
            ))

    with (DATA / "smartloot-world-source-entries.csv").open(
        encoding="utf-8", newline=""
    ) as source:
        smart = list(csv.DictReader(source))
    groups = {}
    for item in smart:
        # Writer-native SmartStashGroupNode path includes rank + group + set.
        key = (item["Prototype"], item["RankIndex"],
               item["Group"], item["GroupIndex"])
        groups.setdefault(key, []).append(item)
    for (sid, rank, kind, index), items in groups.items():
        first = items[0]
        for candidate in items:
            assert all(candidate[f] == first[f] for f in (
                "Rank", "MinSpawnChance", "MaxSpawnChance", "ItemSetCount"
            )), (sid, rank, kind, index)
            assert candidate["ItemSID"]
        selection = mode_of(items)
        results.append(row(
            layer="SmartLoot",
            tier="World",
            source_file="GameData/StashPrototypes.cfg",
            source_sid=sid,
            clone_sid=f"TCD_{sid}",
            rank_index=rank,
            rank_gate=first["Rank"],
            difficulty_gate="",
            category_index="",
            group_type=kind,
            group_index=index,
            group_path=(
                f"ItemGenerators.[{rank}].SmartLootParams.{kind}.[{index}].Items"
            ),
            selection_field=selection,
            item_count=len(items),
            weight_total=(f"{sum(native_number(x['Weight']) for x in items):.6f}"
                          if selection == "Weight" else ""),
            group_min_spawn_chance=first["MinSpawnChance"],
            group_max_spawn_chance=first["MaxSpawnChance"],
            item_set_count=first["ItemSetCount"],
            item_sids=";".join(x["ItemSID"] for x in items),
        ))

    assert len(smart) == 991, len(smart)
    assert len(groups) == 422, len(groups)
    assert len(results) == 463, len(results)
    counts = Counter((x["layer"], x["selection_field"]) for x in results)
    assert counts == {
        ("ItemGenerator", "Weight"): 19,
        ("ItemGenerator", "Chance"): 22,
        ("SmartLoot", "Weight"): 410,
        ("SmartLoot", "Chance"): 12,
    }, counts
    assert sum(int(x["item_count"]) for x in results) == 1124
    keys = [(x["layer"], x["source_sid"], x["rank_index"],
             x["group_path"]) for x in results]
    assert len(keys) == len(set(keys)), "duplicate writer-native source path"
    return results

def render(rows):
    result = io.StringIO(newline="")
    writer = csv.DictWriter(
        result, fieldnames=COLUMNS, lineterminator="\n",
        quoting=csv.QUOTE_MINIMAL)
    writer.writeheader()
    writer.writerows(rows)
    return result.getvalue()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write", action="store_true",
                        help="Regenerate the checked-in source inventory.")
    args = parser.parse_args()
    data = render(inventory())
    if args.write:
        OUT.write_text(data, encoding="utf-8", newline="")
    else:
        assert OUT.exists(), f"Missing source inventory: {OUT}"
        actual = OUT.read_text(encoding="utf-8")
        assert actual == data, (
            f"Stash generator source inventory drift: {OUT}. "
            "Inspect the changed CFG inputs, then intentionally regenerate "
            "using --write and update the source provenance/documentation."
        )
    print("PASS: 41 ItemGenerator source branches (19 Weight, 22 Chance; 133 items)")
    print("PASS: 422 SmartLoot source branches (410 Weight, 12 Chance; 991 items)")
    print("PASS: 463 exact native source group paths, no mixed Weight/Chance")
    print("PASS: source inventory matches embedded CFG input" +
          (" (written)" if args.write else " (checked)"))
    print("PENDING: generator->consumer reachability and shared writer/preview "
          "calculation parity, .NET/Wine/in-game checks")

if __name__ == "__main__":
    main()
