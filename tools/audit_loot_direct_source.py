#!/usr/bin/env python3
"""Strict original Loot direct structure/value/source-ancestry verifier.

This covers only the 1554 direct loot edit points (1476 of them NPC weapon
durability bounds, 80 in the DLC1 NORD generators); custom Loot production
generators, actor removals and shuffle writers are separate contracts.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import zipfile

from audit_cfg_source_values import check_point, read_tree, resolve_refkey_inheritance

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "src" / "TrueCustomDifficulty" / "Data" / "cfg-writer-baseline.json"
SOURCE = "GameData/ItemGeneratorPrototypes.cfg"
DLC_SOURCE = "DLCGameData/DLC1/ItemGeneratorPrototypes.cfg"
EXPECTED_TOTAL = 1554
EXPECTED_KINDS = {"mutant": 14, "npcCondition": 1476, "coupons": 64}
EXPECTED_STATUS = {"exact": 1490, "inherited_exact": 64}
EXPECTED_NPC_PAIRS = 738
EXPECTED_NPC_ROOTS = 92
EXPECTED_DLC_POINTS = 80
TEMPLATE = "GamePass_Stash_ItemGenerator_Rare"
WRAPPER_HEADER = (
    "refurl=ItemGeneratorPrototypes/Gamepass_ItemGenerators.cfg;"
    "refkey=GamePass_Stash_ItemGenerator_Rare"
)
RANKS = ("Newbie", "Experienced", "Veteran", "Master")
MONEY = ("MoneyCommon", "MoneyRare", "MoneyEpic", "MoneyLegendary")
VALID_WEAPON_CATEGORIES = {
    "EItemGenerationCategory::WeaponPrimary",
    "EItemGenerationCategory::WeaponSecondary",
    "EItemGenerationCategory::WeaponPistol",
}


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def descend(root, segments):
    node = root
    for segment in segments:
        children = [x for x in node.children if x.name == segment]
        if len(children) != 1:
            raise ValueError("Missing/ambiguous section: " + "/".join(segments))
        node = children[0]
    return node


def main() -> int:
    a = argparse.ArgumentParser(description=__doc__)
    a.add_argument("--base-zip", type=Path, required=True)
    a.add_argument("--dlc1-zip", type=Path, required=True)
    a.add_argument("--out", type=Path)
    opts = a.parse_args()
    manifest = json.loads(BASELINE.read_text(encoding="utf-8"))
    points = [p for p in manifest["editPoints"]
              if p["parameterId"].startswith("loot.")]
    findings = []
    rows = []
    def scope_ok(p):
        if p["scope"] == "baseGame":
            return p["file"] == SOURCE
        return (p["scope"] == "dlc1" and p["file"] == DLC_SOURCE
                and p["parameterId"] == "loot.droppedWeaponCondition")
    dlc_points = [p for p in points if p["scope"] == "dlc1"]
    if (len(points) != EXPECTED_TOTAL or not all(scope_ok(p) for p in points)
            or len(dlc_points) != EXPECTED_DLC_POINTS):
        findings.append({"reason": "Unexpected Loot manifest count, scope or path",
                         "actual": len(points)})
    with zipfile.ZipFile(opts.base_zip) as base, zipfile.ZipFile(opts.dlc1_zip) as dlc:
        if SOURCE not in base.namelist():
            raise ValueError("Original ZIP has no ItemGeneratorPrototypes.cfg")
        if SOURCE in dlc.namelist():
            findings.append({"reason": "DLC1 now contains item generator override; reassess scope"})
        data = base.read(SOURCE)
        raw_lines = data.decode("utf-8-sig").splitlines()
        roots = read_tree("\n".join(raw_lines))
        indexed = {r.name: r for r in roots}
        if DLC_SOURCE not in dlc.namelist():
            raise ValueError("DLC1 ZIP has no ItemGeneratorPrototypes.cfg")
        dlc_data = dlc.read(DLC_SOURCE)
        dlc_lines = dlc_data.decode("utf-8-sig").splitlines()
        dlc_roots = read_tree("\n".join(dlc_lines))
        dlc_indexed = {r.name: r for r in dlc_roots}
        def tree_for(point):
            if point["scope"] == "dlc1":
                return dlc_indexed, dlc_roots, dlc_lines
            return indexed, roots, raw_lines
        wrapper_roots = [
            indexed.get("Stash_Expensive_" + rank) for rank in RANKS
        ]
        for root in wrapper_roots:
            if root is None:
                findings.append({"reason": "Missing expensive-stash wrapper"})
                continue
            header = raw_lines[root.line - 1]
            if WRAPPER_HEADER not in header or root.children:
                findings.append({"reason": "Rare-stash wrapper direct/refkey structure drift",
                                 "prototype": root.name, "header": header})
        parent = indexed.get(TEMPLATE)
        if parent is None:
            findings.append({"reason": "Consolidated GamePass parent physically absent"})
        else:
            if parent.values.get("SID") != TEMPLATE:
                findings.append({"reason": "Consolidated parent SID mismatch"})

        kinds = Counter()
        seen = set()
        for point in points:
            pid = point["parameterId"]
            path = tuple(point["nodePath"])
            owner = (point["prototype"], *path, point["key"])
            if owner in seen:
                # Same vanilla path may appear for distinct difficulty SIDs
                # but with the same literal value, never with divergent source.
                if pid != "loot.expensiveStashMoneyChance":
                    findings.append({"reason": "Duplicate direct ownership", "path": owner})
            seen.add(owner)

            if pid.startswith("loot.mutant."):
                kind = "mutant"
                if point["operation"] != "set" or point["key"] != "Chance" or len(path) != 4:
                    findings.append({"reason": "Mutant reward source mapping drift", "id": pid})
                try:
                    root = indexed[point["prototype"]]
                    group = descend(root, path[:2])
                    item = descend(root, path)
                    if group.values.get("Category") != "EItemGenerationCategory::MutantLoot":
                        findings.append({"reason": "Mutant source category no longer MutantLoot", "id": pid})
                    if item.values.get("MinCount") != "1" or item.values.get("MaxCount") != "1":
                        findings.append({"reason": "Mutant chance control now changes multi-part reward", "id": pid})
                except (KeyError, ValueError) as e:
                    findings.append({"reason": "Mutant reward source structure missing", "id": pid, "detail": str(e)})
            elif pid == "loot.droppedWeaponCondition":
                kind = "npcCondition"
                if (point["operation"] != "setRangeBound"
                    or point.get("role") != "production-repeatable-human"
                    or point["key"] not in ("MinDurability", "MaxDurability")
                    or len(path) != 4):
                    findings.append({"reason": "Non-production NPC weapon target", "id": pid})
                try:
                    root = tree_for(point)[0][point["prototype"]]
                    group = descend(root, path[:2])
                    item = descend(root, path)
                    if group.values.get("Category") not in VALID_WEAPON_CATEGORIES:
                        findings.append({"reason": "NPC durability target not a weapon generator",
                                         "prototype": point["prototype"], "path": path})
                    if not item.values.get("ItemPrototypeSID"):
                        findings.append({"reason": "NPC weapon no physical item SID", "prototype": point["prototype"]})
                except (KeyError, ValueError) as e:
                    findings.append({"reason": "NPC weapon source structure missing", "detail": str(e)})
            elif pid == "loot.expensiveStashMoneyChance":
                kind = "coupons"
                if point["operation"] != "set" or point["key"] != "Chance":
                    findings.append({"reason": "Coupon direct route/operation drift"})
                try:
                    sid = point["difficultySid"]
                    expected_indices = range(11, 15) if sid in ("Easy", "Medium") else range(15, 19)
                    if sid not in ("Easy", "Medium", "Hard", "Stalker"):
                        raise ValueError("Unknown selected vanilla difficulty SID")
                    rank_index = RANKS.index(point["prototype"].replace("Stash_Expensive_", ""))
                    category = path[1]
                    if category not in {"[" + str(i) + "]" for i in expected_indices}:
                        raise ValueError("Money category index outside difficulty-specific quartet")
                    if path[2:] != ("PossibleItems", "[0]") or path[0] != "ItemGenerator":
                        raise ValueError("Money path no longer an eligible first coupon row")
                    expected_item = MONEY[(int(category.strip("[]")) - 11) % 4]
                    item = descend(parent, path)
                    group = descend(parent, path[:2])
                    if item.values.get("ItemPrototypeSID") != expected_item or (
                        group.values.get("Category") != "EItemGenerationCategory::Consumable"
                    ):
                        raise ValueError("Coupon tier membership altered")
                except (KeyError, ValueError, TypeError, AttributeError) as e:
                    findings.append({"reason": "Coupon original template/rank eligibility differs", "detail": str(e)})
            else:
                findings.append({"reason": "Unhandled direct Loot owner", "id": pid})
                kind = "unmapped"

            _, point_roots, point_lines = tree_for(point)
            direct = check_point(point, point_roots)
            resolved = resolve_refkey_inheritance(point, point_roots, point_lines, direct)
            expected_status = "inherited_exact" if kind == "coupons" else "exact"
            if resolved["status"] != expected_status:
                findings.append({"reason": "Original source value/path/refkey mismatch",
                                 "id": pid, "source": resolved})
            rows.append({
                "parameterId": pid, "kind": kind, "prototype": point["prototype"],
                "path": "/".join(path + (point["key"],)),
                "sourceExpected": point["sourceRaw"], "status": resolved["status"],
                "actual": resolved.get("actualRaw", resolved.get("inheritedRaw")),
                "inheritedFrom": resolved.get("inheritedFrom"),
                "refurlEdges": resolved.get("refurlEdges", []),
            })
            kinds[kind] += 1

    total = len(rows)
    status_counts = Counter(r["status"] for r in rows)
    if kinds != EXPECTED_KINDS:
        findings.append({"reason": "Domain inventory changed", "actual": dict(kinds)})
    if status_counts != EXPECTED_STATUS:
        findings.append({"reason": "Source raw-value or refkey identity drift", "status": dict(status_counts)})
    pairs = {
        (r["prototype"], r["path"].removesuffix("/MinDurability").removesuffix("/MaxDurability"))
        for r in rows if r["kind"] == "npcCondition"
    }
    roots = {r["prototype"] for r in rows if r["kind"] == "npcCondition"}
    if len(pairs) != EXPECTED_NPC_PAIRS or len(roots) != EXPECTED_NPC_ROOTS:
        findings.append({"reason": "NPC 738 weapons / 92 production generators count drift",
                         "weapons": len(pairs), "roots": len(roots)})
    if total != EXPECTED_TOTAL:
        findings.append({"reason": "Incomplete direct inventory", "total": total})
    report = {
        "phase": "14A Loot direct source",
        "status": "PASS" if not findings else "FAIL",
        "archiveSha256": {
            "base": sha(opts.base_zip.read_bytes()),
            "dlc1": sha(opts.dlc1_zip.read_bytes())},
        "sourceCfg": SOURCE, "sourceCfgSha256": sha(data),
        "points": total, "domainCounts": dict(kinds), "sourceStatus": dict(status_counts),
        "npcWeaponSourcePairs": len(pairs),
        "npcWeaponProductionRoots": len(roots),
        "findings": findings, "originalPoints": rows,
        "limitations": "Original source verification only. All custom Loot production/shuffle/removal writers and Unreal runtime are separate tests.",
    }
    if opts.out:
        opts.out.parent.mkdir(parents=True, exist_ok=True)
        opts.out.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n",
                            encoding="utf-8")
    print("Loot DIRECT original source:", report["status"],
          f"{total}/{EXPECTED_TOTAL} fields;", dict(status_counts),
          f"{len(roots)} NPC roots / {len(pairs)} weapon rows")
    for finding in findings[:25]:
        print("  INVESTIGATE", json.dumps(finding, ensure_ascii=False))
    return int(bool(findings))


if __name__ == "__main__":
    raise SystemExit(main())
