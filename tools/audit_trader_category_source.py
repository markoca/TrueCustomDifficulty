#!/usr/bin/env python3
"""Fail-closed classification audit of the committed STALKER 2 trader source table.

This audits a previously extracted CFG 2.0.5 source *catalog*, not original ZIP
parity, a working BPatch writer, or Unreal runtime effects.
"""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict
import csv
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "docs/generators/TRADER-ENTRY-CATALOG.md"

# Not connected to a concrete TradePrototype in the existing consumer graph.
UNRESOLVED = frozenset({
    "HimzavodTrader_ItemGenerator",
    "Trader_Ammo_ItemGenerator",
    "Trader_Ammo_Experienced_ItemGenerator",
    "VezaMedic_TradeItemGenerator",
})

MEDICAL = frozenset("AntiRad ArmyMedkit Bandage EcoMedkit Hercules Medkit".split())
FOOD = frozenset("Bread CannedFood Milk MonolithDryRation Sausage".split())
DRINKS = frozenset("Beer Energetic Vodka Water".split())
DETECTORS = frozenset("Bear Echo Gilka Veles".split())
GRENADES = frozenset("GrenadeF1 GrenadeRGD5".split())
AMMO = frozenset("""
    A012A A012D A012E A045A A045D A045E A545A A545D A545E
    A556A A556D A556E A556S A762NATOA A762NATOD A762NATOS
    A762SniperA A762SniperD A762SniperS A918A A918D A919A
    A919D A939D A939E A939S AGA AHEDP APG7V AVOG
""".split())

# (direct entries, distinct SIDs, generator roots, DLC1 direct entries).
EXPECTED = {
    "Weapons": (73, 34, 10, 0),
    "Armor": (120, 41, 20, 4),
    "Attachments": (31, 18, 10, 0),
    "Ammo": (53, 30, 13, 0),
    "Medical": (67, 6, 14, 2),
    "Food": (32, 5, 10, 13),
    "Drinks": (48, 4, 14, 17),
    "Artifacts": (24, 13, 8, 0),
    "Detectors": (14, 4, 12, 0),
    "Grenades": (16, 2, 11, 0),
}
PROTECTED = {
    "Night vision": (8, 3, 6, 0),
    "Binoculars": (21, 3, 10, 0),
    "Sentinel": (3, 1, 1, 0),
}
# The target definition is shared beyond shops. A local link is NOT a
# substitute for cloning a mixed-category target.
SHARED_TARGET_EDGES = {
    "Trader_Cosnsumables_ItemGenerator": 29,
    "BartenderUniversal_TradeItemGenerator": 9,
    "Trader_NatoAmmo_ItemGenerator": 1,
}
FIELDS = (
    "scope", "generator", "role", "native_category", "category_node",
    "entry", "difficulty", "player_rank", "reputation", "kind", "item_sid",
    "chance", "weight", "min_count", "max_count", "min_durability",
    "max_durability", "ammo_min", "ammo_max", "ammo_maxcount",
    "require_weapon", "cfg_line",
)


def classify(sid: str) -> str:
    # Explicit exceptions MUST precede any generic Gun*, A*, or category rule.
    if sid == "empty":
        return "Sentinel"
    if sid.startswith("Binoculars_"):
        return "Binoculars"
    if sid.startswith("NVG_"):
        return "Night vision"
    if sid == "GunUDP_MagIncreased":
        return "Attachments"
    if sid in MEDICAL:
        return "Medical"
    if sid in FOOD:
        return "Food"
    if sid in DRINKS:
        return "Drinks"
    if sid in DETECTORS:
        return "Detectors"
    if sid in GRENADES:
        return "Grenades"
    if sid in AMMO:
        return "Ammo"
    if sid.startswith(("CArtifact", "EArtifact", "GArtifact")):
        return "Artifacts"
    if sid.startswith(("EN_", "RU_")):
        return "Attachments"
    if sid.startswith("Gun"):
        return "Weapons"
    if sid.endswith(("_Armor", "_Helmet")) or sid in ("DutyArmor_3_U1", "DutyMask_1"):
        return "Armor"
    raise ValueError(f"Unknown item SID {sid!r}; review item taxonomy before editing traders")


def parse_rows(path: Path) -> list[dict[str, str]]:
    rows = []
    for source_line, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not (line.startswith("| base |") or line.startswith("| dlc1 |")):
            continue
        fields = [part.strip() for part in line.split("|")[1:-1]]
        if len(fields) != len(FIELDS):
            raise ValueError(f"{path}:{source_line}: expected {len(FIELDS)} columns, got {len(fields)}")
        row = dict(zip(FIELDS, fields))
        row["catalog_line"] = str(source_line)
        if row["kind"] not in ("item", "nested"):
            raise ValueError(f"{path}:{source_line}: unexpected kind {row['kind']!r}")
        if row["scope"] not in ("base", "dlc1"):
            raise ValueError(f"{path}:{source_line}: unexpected scope")
        if row["kind"] == "item" and not row["cfg_line"].isdigit():
            raise ValueError(f"{path}:{source_line}: missing native CFG source line")
        rows.append(row)
    return rows


def audit(rows: list[dict[str, str]]) -> list[dict[str, str]]:
    if len(rows) != 834 or len({r["generator"] for r in rows}) != 96:
        raise ValueError("Trader source table changed: expected 834 rows and 96 roots")
    if sum(r["kind"] == "nested" for r in rows) != 231:
        raise ValueError("Trader nested-source cardinality changed")
    if len({r["item_sid"] for r in rows if r["kind"] == "nested"}) != 22:
        raise ValueError("Trader nested-target inventory changed")

    active = [r for r in rows if r["generator"] not in UNRESOLVED]
    orphan = [r for r in rows if r["generator"] in UNRESOLVED]
    direct = [r for r in active if r["kind"] == "item"]
    if (len(active), len(orphan), len(direct),
            len(active) - len(direct), len({r["generator"] for r in active})) != (737, 97, 510, 227, 92):
        raise ValueError("Source consumer-scope or row counts changed; repeat consumer graph audit")

    edges = Counter(r["item_sid"] for r in rows if r["kind"] == "nested")
    for sid, count in SHARED_TARGET_EDGES.items():
        if edges[sid] != count:
            raise ValueError(f"Cross-context link drift for {sid}: {edges[sid]} != {count}")

    scopes = defaultdict(lambda: {"rows": 0, "items": set(), "roots": set(), "dlc": 0})
    for row in rows:
        if row["generator"] in UNRESOLVED:
            row["trader_category"] = "UNRESOLVED"
            row["disposition"] = "EXCLUDE_UNRESOLVED_CONSUMER"
        elif row["kind"] == "nested":
            row["trader_category"] = "GENERATOR_LINK"
            row["disposition"] = (
                "SHARED_TARGET_EDGE_REVIEW"
                if row["item_sid"] in SHARED_TARGET_EDGES
                else "TRADER_EDGE_REVIEW"
            )
            if row["item_sid"] == "empty":
                row["disposition"] = "PROTECT_EMPTY_SENTINEL"
        else:
            category = classify(row["item_sid"])
            row["trader_category"] = category
            row["disposition"] = (
                "PROTECT_OUT_OF_SCOPE" if category in PROTECTED
                else "CANDIDATE_DIRECT_ONLY_NOT_WRITER_APPROVED"
            )
            bucket = scopes[category]
            bucket["rows"] += 1
            bucket["items"].add(row["item_sid"])
            bucket["roots"].add(row["generator"])
            bucket["dlc"] += row["scope"] == "dlc1"

    for category, expected in {**EXPECTED, **PROTECTED}.items():
        bucket = scopes[category]
        actual = (
            bucket["rows"], len(bucket["items"]),
            len(bucket["roots"]), bucket["dlc"],
        )
        if actual != expected:
            raise ValueError(f"{category} source drift: {actual} != {expected}")
    if set(scopes) != set(EXPECTED) | set(PROTECTED):
        raise ValueError(f"Unexpected classified group(s): {set(scopes) - set(EXPECTED) - set(PROTECTED)}")
    if sum(scopes[c]["rows"] for c in EXPECTED) != 478:
        raise ValueError("Expected exactly 478 item candidates")
    if sum(scopes[c]["rows"] for c in PROTECTED) != 32:
        raise ValueError("Expected exactly 32 protected source rows")
    return rows


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--csv", type=Path,
        help="Optional full 834-row ledger output with classifications (not production allowlist)",
    )
    args = parser.parse_args()
    try:
        rows = audit(parse_rows(CATALOG))
        if args.csv:
            args.csv.parent.mkdir(parents=True, exist_ok=True)
            with args.csv.open("w", encoding="utf-8", newline="") as stream:
                fields = list(FIELDS) + ["catalog_line", "trader_category", "disposition"]
                writer = csv.DictWriter(stream, fieldnames=fields, lineterminator="\n")
                writer.writeheader()
                writer.writerows(rows)
    except (OSError, ValueError) as exc:
        print(f"[FAIL] trader category source audit: {exc}", file=sys.stderr)
        return 1

    print("[PASS] committed CFG 2.0.5 trader-catalog classification only")
    print("  834 raw rows / 96 roots; 97 orphan rows excluded")
    print("  737 mapped-source rows: 510 direct items + 227 generator edges")
    print("  478 proposed category rows + 32 protected rows")
    print("  Shared trader-local edges remain REVIEW ONLY; no production patch approved")
    for category, (count, sid_count, roots, dlc) in EXPECTED.items():
        print(f"  {category:<13} {count:>3} rows / {sid_count:>2} SIDs / {roots:>2} roots / {dlc:>2} DLC1")
    if args.csv:
        print(f"  Ledger: {args.csv}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
