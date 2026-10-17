#!/usr/bin/env python3
"""Join approved stash weapon item SIDs to exact weapon setup caliber evidence.

Never assume item SID equals setup SID. Only explicit mappings are accepted.
"""
import argparse
import csv
import sys
from pathlib import Path

DATA = Path(__file__).resolve().parents[1] / "src/TrueCustomDifficulty/Data"

def read(path):
    with path.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--calibers", type=Path, required=True, help="CSV produced by audit_progression_weapon_calibers.py")
    ap.add_argument("--item-setup-map", type=Path, required=True, help="Explicit audited item_sid,weapon_setup_sid,evidence_source mapping")
    ap.add_argument("--strict", action="store_true", help="Fail unless every allowlisted stash weapon resolves uniquely")
    ap.add_argument("--output", type=Path)
    args = ap.parse_args()
    slots = read(DATA / "world-weapon-shuffle-manifest.csv")
    if len(slots) != 14:
        raise ValueError(f"Expected 14 stash weapon slots, got {len(slots)}")
    caliber_rows = read(args.calibers)
    by_setup = {}
    for row in caliber_rows:
        sid = row["weapon_setup_sid"]
        if sid in by_setup:
            raise ValueError(f"Duplicate weapon setup {sid}")
        by_setup[sid] = row
    mappings = {}
    for row in read(args.item_setup_map):
        item = row["item_sid"]
        setup = row["weapon_setup_sid"]
        evidence = row["evidence_source"]
        if not item or not setup or not evidence:
            raise ValueError("Mapping must provide item_sid,weapon_setup_sid,evidence_source")
        if item in mappings:
            raise ValueError(f"Duplicate item mapping {item}")
        mappings[item] = row
    output = []
    for slot in slots:
        item = slot["item_sid"]
        mapping = mappings.get(item)
        setup = by_setup.get(mapping["weapon_setup_sid"]) if mapping else None
        status = ("NO_ITEM_SETUP_EVIDENCE" if not mapping else
                  "NO_SETUP_CALIBER" if not setup else setup["status"])
        output.append({
            "generator_sid":slot["generator_sid"],
            "node_path":slot["node_path"],
            "item_sid":item,
            "weapon_setup_sid":mapping["weapon_setup_sid"] if mapping else "",
            "ammo_caliber":setup["ammo_caliber"] if setup else "",
            "default_ammo_sid":setup["default_ammo_sid"] if setup and status=="EXACT_SINGLE_DEFAULT" else "",
            "status":status,
            "evidence_source":mapping["evidence_source"] if mapping else "",
        })
    if args.strict and any(row["status"] != "EXACT_SINGLE_DEFAULT" for row in output):
        unresolved = [row["item_sid"] for row in output if row["status"] != "EXACT_SINGLE_DEFAULT"]
        raise ValueError(f"Unresolved stash item mappings: {unresolved}")
    fields = list(output[0])
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("w", newline="", encoding="utf-8") as f:
            writer = csv.DictWriter(f, fieldnames=fields)
            writer.writeheader()
            writer.writerows(output)
    else:
        writer = csv.DictWriter(sys.stdout, fieldnames=fields)
        writer.writeheader()
        writer.writerows(output)
    print(f"[AUDIT] {len(output)} slots, {sum(row['status']=='EXACT_SINGLE_DEFAULT' for row in output)} proven default ammo links; no CFG edits", file=sys.stderr)

if __name__ == "__main__":
    main()
