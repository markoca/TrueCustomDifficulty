#!/usr/bin/env python3
"""Re-derive the 2.1 ordinary-human NPC loot scope from the ORIGINAL archives.

Checks, against the user's original GameData / DLC1 CFG ZIPs:

* every GeneralNPC_* / GuardNPC_* ItemGenerator link to a shared
  GeneralNPC_Consumables_* pool, minus the explicit exclusions
  (named GeneralNPC_MIK_*, disabled Prologue edges with Chance=0 and the
  spawner-less *_No_Armor roots), equals the embedded redirect manifest
  exactly (scope, file, prototype, node path, pool, evidence line);
* every such link is an always-selected SubItemGenerator edge (Weight>0 or
  Chance=1); the redirect only swaps the pool SID, preserving the gate;
* no refkey descendant (zombies, custom children) silently inherits a
  redirected link: each descendant re-declares that category locally;
* every included root is used by at least one ObjPrototype;
* the dropped-weapon-condition edit points are exactly the direct
  WeaponPrimary / WeaponSecondary / WeaponPistol Min/MaxDurability leaves
  of those same roots, with identical source values.

Usage:
  python3 tools/audit_npc_generalnpc_scope_21.py \
      --base-zip STALKER2-GameData-CFG.zip --dlc1-zip STALKER2-DLC1-CFG.zip
"""
from __future__ import annotations

import argparse
import csv
import io
import json
import re
import zipfile
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"
MANIFEST = DATA / "npc-medical-production-redirect-manifest.csv"
BASELINE = DATA / "cfg-writer-baseline.json"

BASE_IG = "GameData/ItemGeneratorPrototypes.cfg"
DLC_IG = "DLCGameData/DLC1/ItemGeneratorPrototypes.cfg"
BASE_OBJ = "GameData/ObjPrototypes.cfg"
DLC_OBJ = "DLCGameData/DLC1/ObjPrototypes.cfg"
SCOPE_FILE = {"baseGame": BASE_IG, "dlc1": DLC_IG}

ROOT_PATTERN = re.compile(r"^(GeneralNPC|GuardNPC)_.*ItemGenerator")
WEAPON_CATEGORIES = {
    "EItemGenerationCategory::WeaponPrimary",
    "EItemGenerationCategory::WeaponSecondary",
    "EItemGenerationCategory::WeaponPistol",
}
EXPECTED_LINKS = 92
EXPECTED_DLC_LINKS = 4
EXPECTED_WEAPON_POINTS = 1476

HEADER = re.compile(r"^(\s*)(\[[^\]]+\]|[A-Za-z0-9_]+)\s*:\s*struct\.begin\s*(\{[^}]*\})?")
ASSIGN = re.compile(r"^\s*([A-Za-z0-9_\[\]]+)\s*=\s*(.*?)\s*$")


def parse(text: str) -> dict:
    roots, stack = {}, []
    for number, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()
        if not stripped or stripped.startswith("//"):
            continue
        header = HEADER.match(line)
        if header:
            node = {"name": header.group(2), "meta": header.group(3) or "",
                    "line": number, "children": [], "fields": {}}
            if stack:
                stack[-1]["children"].append(node)
            else:
                roots[node["name"]] = node
            stack.append(node)
            continue
        if stripped == "struct.end":
            if stack:
                stack.pop()
            continue
        assign = ASSIGN.match(line)
        if assign and stack:
            stack[-1]["fields"][assign.group(1)] = (assign.group(2), number)
    return roots


def child(node, name):
    for item in node["children"] if node else []:
        if item["name"] == name:
            return item
    return None


def refkey(node):
    match = re.search(r"refkey=([^;}]+)", node["meta"])
    return match.group(1).strip() if match else None


def read(zip_path: Path, member: str) -> str:
    with zipfile.ZipFile(zip_path) as archive:
        return archive.read(member).decode("utf-8-sig", errors="replace")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--base-zip", type=Path, required=True)
    parser.add_argument("--dlc1-zip", type=Path, required=True)
    opts = parser.parse_args()

    trees = {"baseGame": parse(read(opts.base_zip, BASE_IG)),
             "dlc1": parse(read(opts.dlc1_zip, DLC_IG))}
    objects = {**parse(read(opts.base_zip, BASE_OBJ)),
               **parse(read(opts.dlc1_zip, DLC_OBJ))}
    consumers = defaultdict(set)
    for sid, node in objects.items():
        generator = node["fields"].get("ItemGeneratorPrototypeSID")
        if generator:
            consumers[generator[0]].add(sid)

    children = defaultdict(list)
    all_roots = {}
    for scope, tree in trees.items():
        for sid, node in tree.items():
            all_roots[sid] = node
            parent = refkey(node)
            if parent and parent != "[0]":
                children[parent].append(sid)

    def descendants(sid):
        for kid in children.get(sid, []):
            yield kid
            yield from descendants(kid)

    findings = []
    expected = {}
    excluded = Counter()
    gates = Counter()
    for scope, tree in trees.items():
        for sid, node in tree.items():
            if not ROOT_PATTERN.match(sid):
                continue
            generator = child(node, "ItemGenerator")
            for category in generator["children"] if generator else []:
                items = child(category, "PossibleItems")
                for entry in items["children"] if items else []:
                    pool = entry["fields"].get("ItemGeneratorPrototypeSID")
                    if not pool or not pool[0].startswith("GeneralNPC_Consumables_"):
                        continue
                    chance = entry["fields"].get("Chance")
                    if sid.startswith("GeneralNPC_MIK_"):
                        excluded["MIK"] += 1
                        continue
                    if chance is not None and float(chance[0]) == 0.0:
                        excluded["disabled Prologue"] += 1
                        continue
                    if sid.endswith("_No_Armor_ItemGenerator"):
                        excluded["No_Armor"] += 1
                        if consumers.get(sid):
                            findings.append(f"No_Armor root gained consumers: {sid}")
                        continue
                    # The redirect only swaps the pool SID, so the native
                    # gate is preserved. Vanilla uses Weight=1 (89), Chance=1
                    # (2) and one sole-entry Weight=0.5 (Duty Sniper).
                    weight = entry["fields"].get("Weight")
                    if chance is not None:
                        gate = "Chance=" + chance[0]
                        if float(chance[0]) != 1.0:
                            findings.append(f"Partial-Chance pool link needs review: {sid}")
                    elif weight is not None and float(weight[0]) > 0.0:
                        gate = "Weight=" + weight[0]
                    else:
                        gate = "none"
                        findings.append(f"Pool link has no positive gate: {sid}")
                    gates[gate] += 1
                    if not consumers.get(sid):
                        findings.append(f"Included root has no ObjPrototype consumer: {sid}")
                    node_path = f"ItemGenerator.{category['name']}.PossibleItems.{entry['name']}"
                    expected[(scope, sid)] = (node_path, pool[0], str(entry["line"]))
                    for kid in descendants(sid):
                        kid_generator = child(all_roots[kid], "ItemGenerator")
                        kid_category = child(kid_generator, category["name"])
                        if child(kid_category, "PossibleItems") is None:
                            findings.append(
                                f"Descendant {kid} silently inherits redirected link of {sid}")

    with MANIFEST.open(encoding="utf-8-sig", newline="") as handle:
        manifest = list(csv.DictReader(handle))
    actual = {}
    for row in manifest:
        key = (row["scope"], row["prototype"])
        if key in actual:
            findings.append(f"Duplicate manifest row: {key}")
        actual[key] = (row["node_path"], row["source_value"], row["evidence_line"])
        if row["file"] != SCOPE_FILE.get(row["scope"]):
            findings.append(f"Wrong file for {key}: {row['file']}")
        if row["key"] != "ItemGeneratorPrototypeSID" or row["target_value"] != "TCD_" + row["source_value"]:
            findings.append(f"Wrong redirect target for {key}")
    for key in sorted(set(expected) - set(actual)):
        findings.append(f"Source link missing from manifest: {key}")
    for key in sorted(set(actual) - set(expected)):
        findings.append(f"Manifest row not an eligible source link: {key}")
    for key in sorted(set(expected) & set(actual)):
        if expected[key] != actual[key]:
            findings.append(f"Manifest row differs from source: {key} {actual[key]} != {expected[key]}")
    dlc_links = sum(1 for scope, _ in actual if scope == "dlc1")
    if len(actual) != EXPECTED_LINKS or dlc_links != EXPECTED_DLC_LINKS:
        findings.append(f"Link count {len(actual)} ({dlc_links} DLC1); expected "
                        f"{EXPECTED_LINKS} ({EXPECTED_DLC_LINKS} DLC1)")

    # Dropped weapon condition: direct weapon leaves of the same roots.
    wanted = {}
    for scope, sid in expected:
        generator = child(trees[scope][sid], "ItemGenerator")
        for category in generator["children"]:
            if category["fields"].get("Category", ("",))[0] not in WEAPON_CATEGORIES:
                continue
            items = child(category, "PossibleItems")
            for entry in items["children"] if items else []:
                for key in ("MinDurability", "MaxDurability"):
                    if key in entry["fields"]:
                        path = ("ItemGenerator", category["name"], "PossibleItems", entry["name"])
                        wanted[(scope, sid, path, key)] = entry["fields"][key][0]
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    points = {}
    for point in baseline["editPoints"]:
        if point.get("parameterId") != "loot.droppedWeaponCondition":
            continue
        key = (point["scope"], point["prototype"], tuple(point["nodePath"]), point["key"])
        points[key] = point["sourceRaw"]
        if point["file"] != SCOPE_FILE.get(point["scope"]):
            findings.append(f"Weapon point in wrong file: {key}")
    for key in sorted(set(wanted) - set(points)):
        findings.append(f"Weapon durability leaf missing from baseline: {key}")
    for key in sorted(set(points) - set(wanted)):
        findings.append(f"Baseline weapon point outside the ordinary-human scope: {key}")
    for key in sorted(set(points) & set(wanted)):
        if points[key] != wanted[key]:
            findings.append(f"Weapon source value differs: {key} {points[key]} != {wanted[key]}")
    if len(points) != EXPECTED_WEAPON_POINTS:
        findings.append(f"Weapon point count {len(points)}; expected {EXPECTED_WEAPON_POINTS}")

    status = "PASS" if not findings else "FAIL"
    print(f"NPC ordinary-human 2.1 scope: {status} — {len(actual)} redirects "
          f"({dlc_links} DLC1), {len(points)} weapon durability leaves; "
          f"excluded {dict(excluded)}; gates {dict(gates)}")
    for finding in findings[:40]:
        print("  INVESTIGATE", finding)
    return int(bool(findings))


if __name__ == "__main__":
    raise SystemExit(main())
