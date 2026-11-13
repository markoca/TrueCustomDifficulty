#!/usr/bin/env python3
"""Read-only verification of the exact trader-role source ledger.

Input: committed extracted STALKER 2 CFG 2.0.5 trader catalogs.
Never claims fresh original CFG parity, compiled writer proof or gameplay.
"""
from __future__ import annotations

import argparse
from collections import Counter, defaultdict
import csv
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TOPOLOGY = ROOT / "docs/generators/TRADER-STOCK-AUDIT.md"
ENTRIES = ROOT / "docs/generators/TRADER-ENTRY-CATALOG.md"
LEDGER = ROOT / "docs/generators/trader-role-scope-catalog.csv"

ROLE = {
    "Medic": "MEDIC",
    "Technician": "TECHNICIAN",
    "Trader / general": "GENERAL_TRADER",
    "Bartender": "BARTENDER",
    "Generic NPC trader": "GENERIC_NPC_TRADER",
    "Shared stock pool": "SHARED_STOCK_POOL",
}
GRAPH = {
    "externally used concrete/root": "MERCHANT_ROOT",
    "external consumer + shared base": "MERCHANT_ROOT_AND_POOL",
    "internal/shared dependency": "INTERNAL_POOL",
    "unreferenced wrapper/specialization": "UNRESOLVED",
}
FIELDS = (
    "scope", "generator", "trader_type", "subtype", "topology",
    "trade_consumers", "direct_item_rows", "nested_generator_links",
    "native_categories", "nested_targets", "source_status",
)
EXPECTED = {
    "GENERAL_TRADER": (35, 32, 0, 3, 25, 10),
    "MEDIC": (13, 11, 1, 1, 11, 2),
    "TECHNICIAN": (16, 16, 0, 0, 15, 1),
    "BARTENDER": (11, 10, 1, 0, 8, 3),
    "GENERIC_NPC_TRADER": (9, 9, 0, 0, 9, 0),
    "SHARED_STOCK_POOL": (12, 0, 12, 0, 12, 0),
}
ORPHANS = {
    "HimzavodTrader_ItemGenerator",
    "Trader_Ammo_Experienced_ItemGenerator",
    "Trader_Ammo_ItemGenerator",
    "VezaMedic_TradeItemGenerator",
}
GENERIC = {
    "GeneralNPC_TradeItemGenerator",
    *(f"GeneralNPC_TradeItemGenerator_{group}" for group in (
        "Bandit", "Corpus", "Duty", "Freedom",
        "Mercenary", "Militaries", "Scientists", "Spark")),
}
CROSS_ROLE = {
    "Trader_T1_Ammo_ItemGenerator": {"GENERAL_TRADER", "BARTENDER", "TECHNICIAN"},
    "Trader_T1_Guns_ItemGenerator": {"GENERAL_TRADER", "BARTENDER"},
    "Trader_Attachments_T2_ItemGenerator": {"GENERAL_TRADER", "TECHNICIAN"},
    "Trader_T2_Ammo_ItemGenerator": {"GENERAL_TRADER", "TECHNICIAN"},
    "BartenderUniversal_TradeItemGenerator": {"GENERAL_TRADER", "BARTENDER"},
    "Trader_Cosnsumables_ItemGenerator": {"GENERAL_TRADER", "TECHNICIAN"},
}


def cells(line: str) -> list[str]:
    return [x.strip().replace(r"\|", "|") for x in line.strip().strip("|").split(" | ")]


def get_entries() -> list[list[str]]:
    rows = [cells(l) for l in ENTRIES.read_text(encoding="utf-8").splitlines()
            if l.startswith(("| base |", "| dlc1 |"))]
    if len(rows) != 834 or any(len(r) != 22 for r in rows):
        raise ValueError("Expected exactly 834 original 22-field trader rows")
    return rows


def get_topology() -> list[list[str]]:
    source = TOPOLOGY.read_text(encoding="utf-8")
    if source.count("## 8. Full trader generator inventory") != 1:
        raise ValueError("Missing trader consumer inventory")
    section = source.split("## 8. Full trader generator inventory", 1)[1]
    section = section.split("## 9. TradePrototype", 1)[0]
    rows = [cells(l) for l in section.splitlines()
            if l.startswith(("| base |", "| dlc1 |"))]
    if len(rows) != 96 or any(len(r) != 9 for r in rows):
        raise ValueError("Expected 96 distinct 9-field topology roots")
    return rows


def subtype(g: str, role: str) -> str:
    if role == "Generic NPC trader":
        return "generic_base" if g == "GeneralNPC_TradeItemGenerator" else "faction_profile"
    if role == "Trader / general":
        if g == "DynamicTraderItemGenerator":
            return "dynamic_npc_trader"
        if g == "RC_TraderNPC_ItemGenerator":
            return "rc_special_trader"
        if re.search(r"MQ\d+|SQ\d+|Stage\d+", g):
            return "dlc_quest_stage"
        if re.match(r"RostokTrader(Armor|Nato|Soviet)_", g):
            return "specialized_rostock"
        return "named_or_location"
    if role == "Shared stock pool":
        return "reusable_item_pool"
    if g == "MedicUniversal_TradeItemGenerator":
        return "medic_pool_and_trade_consumer"
    if g == "MedicNoon_TradeItemGenerator":
        return "medic_pool"
    if g == "BartenderCNPP_TradeItemGenerator":
        return "dlc1_bartender_pool"
    if g == "SkobaMedic_TradeItemGenerator":
        return "dlc1_stage_medic"
    return "standard_role"


def build() -> list[dict[str, str]]:
    data: dict[str, dict] = {}
    incoming: dict[str, set[str]] = defaultdict(set)
    for r in get_entries():
        scope, g, role, category, kind, sid = r[0], r[1], r[2], r[3], r[9], r[10]
        if role not in ROLE:
            raise ValueError(f"Unknown role {role} at {g}")
        v = data.setdefault(g, {"scope": scope, "role": role, "item": 0,
                                "nested": 0, "cats": set(), "targets": set()})
        if (v["scope"], v["role"]) != (scope, role):
            raise ValueError(f"Scope/role collision: {g}")
        if kind == "item":
            v["item"] += 1
            v["cats"].add(category)
        elif kind == "nested":
            v["nested"] += 1
            v["targets"].add(sid)
            incoming[sid].add(ROLE[role])
        else:
            raise ValueError(f"Unexpected kind {kind} at {g}")
    records = []
    for row in get_topology():
        scope, g, role, graph, consumer, _cat, count, _reachable, _policy = row
        v = data.get(g)
        if not v or role not in ROLE or graph not in GRAPH:
            raise ValueError(f"Unexpected/missing role or generator {g}")
        if (v["scope"], v["role"]) != (scope, role):
            raise ValueError(f"Topological mismatch at {g}")
        if v["item"] + v["nested"] != int(count):
            raise ValueError(f"Local row count drift at {g}")
        t = GRAPH[graph]
        records.append({
            "scope": scope,
            "generator": g,
            "trader_type": ROLE[role],
            "subtype": subtype(g, role),
            "topology": t,
            "trade_consumers": "" if consumer == "—" else consumer,
            "direct_item_rows": str(v["item"]),
            "nested_generator_links": str(v["nested"]),
            "native_categories": ";".join(sorted(v["cats"])),
            "nested_targets": ";".join(sorted(v["targets"])),
            "source_status": {
                "UNRESOLVED": "EXCLUDE_UNRESOLVED",
                "INTERNAL_POOL": "NOT_A_MERCHANT",
                "MERCHANT_ROOT_AND_POOL": "MERCHANT_AND_POOL_REQUIRES_CARE",
                "MERCHANT_ROOT": "TRADER_ROOT_BOUNDARY_REVIEW",
            }[t],
        })
    if len(records) != len(data) or len({r["generator"] for r in records}) != 96:
        raise ValueError("Source vs topology generator inventory mismatch")
    stats: dict[str, Counter] = defaultdict(Counter)
    for r in records:
        s = stats[r["trader_type"]]
        s["total"] += 1
        s[r["scope"]] += 1
        if r["topology"] == "UNRESOLVED":
            s["unresolved"] += 1
        elif r["topology"] == "INTERNAL_POOL":
            s["pool"] += 1
        else:
            s["merchant"] += 1
    for k, expected in EXPECTED.items():
        s = stats[k]
        actual = tuple(s[n] for n in ("total", "merchant", "pool", "unresolved", "base", "dlc1"))
        if actual != expected:
            raise ValueError(f"{k} drift: {actual} != {expected}")
    if set(stats) != set(EXPECTED):
        raise ValueError("Role family drift")
    if {r["generator"] for r in records if r["topology"] == "UNRESOLVED"} != ORPHANS:
        raise ValueError("Unresolved consumers changed")
    if {r["generator"] for r in records if r["trader_type"] == "GENERIC_NPC_TRADER"} != GENERIC:
        raise ValueError("Generic faction trader names changed")
    for target, roles in CROSS_ROLE.items():
        if incoming[target] != roles:
            raise ValueError(f"Cross-role consumer links changed: {target}")
    return records


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--out", type=Path, help="Regenerate 96-row comparison CSV to chosen path")
    args = p.parse_args()
    try:
        result = build()
        with LEDGER.open(newline="", encoding="utf-8") as f:
            reader = csv.DictReader(f)
            if reader.fieldnames != list(FIELDS) or list(reader) != result:
                raise ValueError("Committed role CSV differs from audited source topology")
        if args.out:
            args.out.parent.mkdir(parents=True, exist_ok=True)
            with args.out.open("w", newline="", encoding="utf-8") as f:
                w = csv.DictWriter(f, fieldnames=FIELDS, lineterminator="\n")
                w.writeheader()
                w.writerows(result)
    except (OSError, ValueError, KeyError) as e:
        print(f"[FAIL] trader role audit: {e}", file=sys.stderr)
        return 1
    print("[PASS] trader roles: 96 roots, 78 merchant roots, 14 internal pools, 4 unresolved")
    print("  Merchant role roots: 32 general, 11 medic, 16 technician, 10 bartender, 9 generic NPC")
    print("  Nine generic trader profiles distinct from dynamic and RC specialist NPC models")
    print("  Existing shared pool fanout preserved; no purchase or sale CFG edits")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
