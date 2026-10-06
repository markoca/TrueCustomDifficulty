#!/usr/bin/env python3
"""Close the current CFG writer provenance chain without pretending the raw 2.0.5 dump is bundled.

Two evidence classes are intentionally distinct:
1. The legacy 699 direct edit points and 18 legacy special routes were audited against the exact
   supplied runtime 2.0.5 dump in v0.1.63. The dump SHA-256 and exact-match result are retained
   in docs/AUDIT.md. This tool fingerprints the current retained legacy set so it cannot drift
   silently from v0.2.54 onward.
2. The 671 later direct points and 7 later clone/redirect routes are revalidated here against the
   embedded source inventories/manifests that were generated from the same runtime-source audit work.
"""
from __future__ import annotations

import argparse
import csv
import hashlib
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "Data" / "cfg-writer-baseline.json"
AUDIT = ROOT / "docs" / "AUDIT.md"
GEN = ROOT / "docs" / "generators"
RUNTIME_SHA256 = "739ecb4a60e2d98181cc6b326894d184b76a09c29b94a66c18d7b0da2c37a83a"

LATER_DIRECT = {
    "loot.droppedWeaponCondition": ("generator-local-source-item-entries.csv", "generator_local"),
    "loot.npcWeaponAmmoSupply": ("generator-local-source-item-entries.csv", "generator_local"),
    "economy.medicStock": ("trader-local-source-entries.csv", "trader"),
}
EXPECTED_LATER_DIRECT_COUNTS = {
    "loot.droppedWeaponCondition": 242,
    "loot.npcWeaponAmmoSupply": 242,
    "economy.medicStock": 96,
}

# One source-template configuration can back more than one parameter route (Cheap Stash).
LATER_SPECIAL_GROUPS = {
    "cheapStashProduction": {
        "source_root": "GamePass_Stash_ItemGenerator_Cheap",
        "categories_key": "templateCategories",
        "manifest": "cheap-stash-production-redirect-manifest.csv",
        "expected_manifest_rows": 1271,
        "expected_template_fields": 244,
        "routes": ["loot.cheapStashAmmoQuantity", "loot.cheapStashGrenadeChance", "loot.cheapStashGrenadeQuantity"],
    },
    "mediumStashAmmoProduction": {
        "source_root": "GamePass_Stash_ItemGenerator_Common_Var1",
        "categories_key": "templateCategories",
        "manifest": "medium-stash-production-redirect-manifest.csv",
        "expected_manifest_rows": 450,
        "expected_template_fields": 188,
        "routes": ["loot.mediumStashAmmoQuantity"],
    },
    "expensiveStashProduction": {
        "source_root": "GamePass_Stash_ItemGenerator_Rare",
        "categories_key": "templateCategories",
        "manifest": "expensive-stash-production-redirect-manifest.csv",
        "expected_manifest_rows": 305,
        "expected_template_fields": 229,
        "routes": ["loot.expensiveStashAmmoQuantity"],
    },
    "destructibleAmmoProduction": {
        "manifest": "destructible-ammo-production-redirect-manifest.csv",
        "expected_manifest_rows": 1235,
        "expected_template_fields": 36,
        "routes": ["loot.destructibleAmmoQuantity"],
    },
    "destructibleGrenadeProduction": {
        "manifest": "destructible-grenade-production-redirect-manifest.csv",
        "expected_manifest_rows": 182,
        "expected_template_fields": 12,
        "routes": ["loot.destructibleGrenadeQuantity"],
    },
    "npcMedicalLootProduction": {
        "manifest": "npc-medical-production-redirect-manifest.csv",
        "expected_manifest_rows": 33,
        "expected_template_fields": 488,
        "routes": ["loot.npcMedicalChance"],
    },
}


def read_csv(path: Path):
    with path.open(newline="", encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def norm(value):
    s = str(value).strip()
    if s.lower() in {"true", "false"}:
        return ("bool", s.lower())
    if s.endswith(("f", "F")):
        s = s[:-1]
    try:
        return ("num", round(float(s), 12))
    except ValueError:
        return ("str", s)


def same(a, b):
    return norm(a) == norm(b)


def tuple_line(e):
    return "|".join([
        e["parameterId"], e["scope"], e["file"], e["prototype"],
        ".".join(e.get("nodePath", [])), e["key"], str(e["sourceRaw"]), e["operation"],
    ])


def sha_lines(lines):
    payload = "\n".join(sorted(lines)) + "\n"
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def source_index(rows):
    return {(r["scope"], r["root"], r["category_node"], r["entry_node"]): r for r in rows}


def validate_later_direct(edit_points):
    gen_rows = read_csv(GEN / "generator-local-source-item-entries.csv")
    trader_rows = read_csv(GEN / "trader-local-source-entries.csv")
    zombie_rows = read_csv(GEN / "generic-zombie-grenade-targets.csv")
    gen_idx = source_index(gen_rows)
    trader_idx = source_index(trader_rows)
    zombie_idx = {(r["scope"], r["generator_sid"], r["category_node"], r["entry_node"]): r for r in zombie_rows}

    out = []
    failures = []
    for e in edit_points:
        pid = e["parameterId"]
        if pid not in LATER_DIRECT:
            continue
        np = e["nodePath"]
        cat, ent = np[1], np[3]
        scope = "base" if e["scope"] == "baseGame" else e["scope"]
        evidence_file, kind = LATER_DIRECT[pid]
        if kind == "generator_local":
            row = gen_idx.get((scope, e["prototype"], cat, ent))
        elif kind == "trader":
            row = trader_idx.get((scope, e["prototype"], cat, ent))
        else:
            row = zombie_idx.get((scope, e["prototype"], cat, ent))
        status = "PASS"
        evidence_line = ""
        if row is None:
            status = "FAIL_NO_SOURCE_ROW"
        elif e["key"] not in row:
            status = "FAIL_NO_SOURCE_FIELD"
        elif not same(e["sourceRaw"], row[e["key"]]):
            status = "FAIL_SOURCE_VALUE_MISMATCH"
        else:
            evidence_line = row.get("line", "")
        if status != "PASS":
            failures.append((e, status, row))
        out.append({
            "parameterId": pid,
            "provenanceClass": "FRESH_EMBEDDED_SOURCE_EXACT",
            "scope": e["scope"], "file": e["file"], "prototype": e["prototype"],
            "nodePath": ".".join(e["nodePath"]), "key": e["key"], "sourceRaw": e["sourceRaw"],
            "operation": e["operation"], "evidenceFile": f"docs/generators/{evidence_file}",
            "evidenceLine": evidence_line, "status": status,
        })
    return out, failures


def validate_category(root, cat, rows, idx):
    cat_map = {
        "Category": "category", "Diff": "Diff", "PlayerRank": "PlayerRank",
        "ReputationThreshold": "ReputationThreshold",
        "bAllowSameCategoryGeneration": "bAllowSameCategoryGeneration",
    }
    errs = []
    matches = 0
    category_rows = [r for r in rows if r["root"] == root and r["category_node"] == cat["node"]]
    if not category_rows:
        return 0, [(root, cat["node"], "NO_CATEGORY")]
    for key, value in cat.get("fields", {}).items():
        col = cat_map.get(key, key)
        values = [r.get(col, "") for r in category_rows]
        if not any(same(value, x) for x in values):
            errs.append((root, cat["node"], key, value, values[:4]))
        else:
            matches += 1
    for item in cat.get("items", []):
        row = idx.get((root, cat["node"], item["node"]))
        if row is None:
            errs.append((root, cat["node"], item["node"], "NO_ITEM"))
            continue
        for key, value in item.get("fields", {}).items():
            if key not in row or not same(value, row[key]):
                errs.append((root, cat["node"], item["node"], key, value, row.get(key, "<missing>")))
            else:
                matches += 1
    return matches, errs


def validate_later_special(special):
    rows = read_csv(GEN / "generator-local-source-item-entries.csv")
    idx = {(r["root"], r["category_node"], r["entry_node"]): r for r in rows}
    output = []
    failures = []
    total_fields = 0
    total_manifest_rows = 0

    for group, spec in LATER_SPECIAL_GROUPS.items():
        cfg = special[group]
        matches = 0
        errs = []
        if group in {"cheapStashProduction", "mediumStashAmmoProduction", "expensiveStashProduction"}:
            root = spec["source_root"]
            for cat in cfg[spec["categories_key"]]:
                m, e = validate_category(root, cat, rows, idx)
                matches += m; errs += e
        elif group in {"destructibleAmmoProduction", "destructibleGrenadeProduction"}:
            for pool in cfg["pools"]:
                cat = {"node": pool["categoryNode"], "fields": pool.get("categoryFields", {}), "items": pool.get("items", [])}
                m, e = validate_category(pool["sourceSid"], cat, rows, idx)
                matches += m; errs += e
        elif group == "npcMedicalLootProduction":
            for pool in cfg["pools"]:
                root = pool["vanillaSid"]
                for cat in pool["templateCategories"]:
                    m, e = validate_category(root, cat, rows, idx)
                    matches += m; errs += e
                for me in pool["medicalEntries"]:
                    row = idx.get((root, me["categoryNode"], me["entryNode"]))
                    if row is None or not same(me["sourceChanceRaw"], row.get("Chance", "")):
                        errs.append((root, me, "MEDICAL_SOURCE_CHANCE_MISMATCH"))
                    # sourceChanceRaw is a selector transform source, not an additional emitted template field;
                    # don't add it to template-field accounting.

        manifest_data = ROOT / "Data" / spec["manifest"]
        manifest_doc = GEN / spec["manifest"]
        manifest_rows = read_csv(manifest_data)
        manifest_ok = (manifest_doc.exists() and manifest_data.read_bytes() == manifest_doc.read_bytes()) or (not manifest_doc.exists() and all(r.get("evidence_line") and r.get("file") and r.get("prototype") and r.get("key") and r.get("source_value") for r in manifest_rows))
        if len(manifest_rows) != spec["expected_manifest_rows"]:
            errs.append((group, "MANIFEST_COUNT", len(manifest_rows), spec["expected_manifest_rows"]))
        if not manifest_ok:
            errs.append((group, "MANIFEST_MIRROR_MISMATCH"))
        if matches != spec["expected_template_fields"]:
            errs.append((group, "TEMPLATE_FIELD_COUNT", matches, spec["expected_template_fields"]))

        total_fields += matches
        total_manifest_rows += len(manifest_rows)
        status = "PASS" if not errs else "FAIL"
        if errs:
            failures.extend(errs)
        for route in spec["routes"]:
            output.append({
                "parameterId": route,
                "provenanceClass": "FRESH_EMBEDDED_SOURCE_CLONE_REDIRECT",
                "specialGroup": group,
                "templateFieldMatches": matches,
                "redirectManifest": f"Data/{spec['manifest']}",
                "redirectRows": len(manifest_rows),
                "sourceEvidence": "docs/generators/generator-local-source-item-entries.csv + mirrored audited redirect manifest",
                "status": status,
            })
    return output, failures, total_fields, total_manifest_rows


def write_csv(path, rows, fields):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=fields)
        w.writeheader(); w.writerows(rows)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=str(ROOT / "docs" / "cfg-provenance" / "v0.2.54"))
    args = ap.parse_args()
    outdir = Path(args.out)

    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    edit_points = baseline["editPoints"]
    special = baseline["special"]
    audit_text = AUDIT.read_text(encoding="utf-8")

    errors = []
    if RUNTIME_SHA256 not in audit_text or "699/699 exact" not in audit_text:
        errors.append("retained v0.1.63 runtime provenance statement missing")

    later_rows, later_fail = validate_later_direct(edit_points)
    errors.extend([f"later direct mismatch: {x}" for x in later_fail])
    late_counts = Counter(r["parameterId"] for r in later_rows)
    if dict(late_counts) != EXPECTED_LATER_DIRECT_COUNTS:
        errors.append(f"later direct split drifted: {dict(late_counts)}")

    later_ids = set(LATER_DIRECT)
    legacy_edit_points = [e for e in edit_points if e["parameterId"] not in later_ids]
    if len(legacy_edit_points) != 699:
        errors.append(f"legacy direct set is {len(legacy_edit_points)}, expected 699")

    legacy_rows = [{
        "parameterId": e["parameterId"],
        "provenanceClass": "HISTORICAL_RUNTIME_2_0_5_EXACT",
        "scope": e["scope"], "file": e["file"], "prototype": e["prototype"],
        "nodePath": ".".join(e["nodePath"]), "key": e["key"], "sourceRaw": e["sourceRaw"],
        "operation": e["operation"], "evidenceFile": "docs/AUDIT.md",
        "evidenceLine": "v0.1.63 Writer baseline verification; runtime dump SHA-256 retained",
        "status": "HISTORICAL_PASS",
    } for e in legacy_edit_points]

    special_rows = []
    # 18 special routes explicitly called out as source-shape checked by the v0.1.63 raw runtime audit.
    for entry in special["listMembership"]:
        special_rows.append({"parameterId": entry["parameterId"], "provenanceClass": "HISTORICAL_RUNTIME_2_0_5_STRUCTURE_EXACT", "specialGroup": "listMembership", "templateFieldMatches": "", "redirectManifest": "", "redirectRows": "", "sourceEvidence": "docs/AUDIT.md: Additional Mechanics membership", "status": "HISTORICAL_PASS"})
    for entry in special["setList"]:
        special_rows.append({"parameterId": entry["parameterId"], "provenanceClass": "HISTORICAL_RUNTIME_2_0_5_STRUCTURE_EXACT", "specialGroup": "setList", "templateFieldMatches": "", "redirectManifest": "", "redirectRows": "", "sourceEvidence": "docs/AUDIT.md: CorpseSmartLoot.GeneralNPC", "status": "HISTORICAL_PASS"})
    for pid in special["psyParameterKeys"]:
        special_rows.append({"parameterId": pid, "provenanceClass": "HISTORICAL_RUNTIME_2_0_5_STRUCTURE_EXACT", "specialGroup": "psyArray", "templateFieldMatches": "", "redirectManifest": "", "redirectRows": "", "sourceEvidence": "docs/AUDIT.md: Master PSY override entries", "status": "HISTORICAL_PASS"})
    for pid in special["mutantCooldownParameters"]:
        special_rows.append({"parameterId": pid, "provenanceClass": "HISTORICAL_RUNTIME_2_0_5_STRUCTURE_EXACT", "specialGroup": "mutantCooldownArray", "templateFieldMatches": "", "redirectManifest": "", "redirectRows": "", "sourceEvidence": "docs/AUDIT.md: mutant cooldown-family entries/tags", "status": "HISTORICAL_PASS"})

    if len(special_rows) != 18:
        errors.append(f"historical special route count {len(special_rows)}, expected 18")

    later_special_rows, later_special_fail, template_fields, manifest_rows = validate_later_special(special)
    errors.extend([f"later special mismatch: {x}" for x in later_special_fail])
    special_rows.extend(later_special_rows)
    if len(later_special_rows) != 8:
        errors.append(f"later special route count {len(later_special_rows)}, expected 8")
    if len(special_rows) != 26:
        errors.append(f"total special route count {len(special_rows)}, expected 26")

    direct_rows = legacy_rows + later_rows
    if len(direct_rows) != 1370:
        errors.append(f"direct provenance row count {len(direct_rows)}, expected 1370")

    legacy_fp = sha_lines(tuple_line(e) for e in legacy_edit_points)
    current_direct_fp = sha_lines(tuple_line(e) for e in edit_points)

    summary = {
        "status": "PASS" if not errors else "FAIL",
        "rawRuntimeDumpBundled": False,
        "runtimeDumpVersion": "2.0.5",
        "runtimeDumpSha256": RUNTIME_SHA256,
        "direct": {
            "total": 1370,
            "historicalRuntimeExact": len(legacy_rows),
            "freshEmbeddedSourceExact": len(later_rows),
            "freshEmbeddedSourceMismatches": len(later_fail),
            "legacyCurrentTupleFingerprintSha256": legacy_fp,
            "currentDirectTupleFingerprintSha256": current_direct_fp,
        },
        "special": {
            "total": len(special_rows),
            "historicalRuntimeStructureExact": 18,
            "freshEmbeddedCloneRedirect": len(later_special_rows),
            "freshTemplateFieldMatches": template_fields,
            "freshRedirectManifestRows": manifest_rows,
            "freshSpecialMismatches": len(later_special_fail),
        },
        "laterDirectCounts": dict(sorted(late_counts.items())),
        "errors": errors,
    }

    outdir.mkdir(parents=True, exist_ok=True)
    (outdir / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    write_csv(outdir / "direct-provenance.csv", direct_rows, ["parameterId","provenanceClass","scope","file","prototype","nodePath","key","sourceRaw","operation","evidenceFile","evidenceLine","status"])
    write_csv(outdir / "special-provenance.csv", special_rows, ["parameterId","provenanceClass","specialGroup","templateFieldMatches","redirectManifest","redirectRows","sourceEvidence","status"])

    readme = f"""# CFG provenance evidence — v0.2.54\n\nStatus: **{summary['status']}**\n\nThis evidence deliberately distinguishes historical raw-runtime validation from fresh validation against embedded source inventories. The original runtime 2.0.5 dump is **not bundled** in this source package.\n\n- Historical direct runtime proof: **{len(legacy_rows)}/699** retained from the v0.1.63 exact raw-dump audit.\n- Fresh later direct proof: **{len(later_rows)}/671** exact source-inventory matches.\n- Direct total provenance coverage: **{len(direct_rows)}/1370**.\n- Historical special runtime-structure proof: **18/18**.\n- Fresh clone/redirect special proof: **{len(later_special_rows)}/7** routes, **{template_fields}** source-template field matches, **{manifest_rows}** audited redirect-manifest rows.\n- Runtime dump fingerprint retained by the historical audit: `{RUNTIME_SHA256}`.\n- Current retained 699-entry legacy tuple fingerprint: `{legacy_fp}`.\n\n`HISTORICAL_PASS` means the source shape/value was proven against the exact runtime dump in the retained v0.1.63 audit; it is not a fresh re-read of that raw dump in v0.2.54. `PASS` on later entries is freshly recomputed from the embedded source inventories/manifests in this release.\n"""
    (outdir / "README.md").write_text(readme, encoding="utf-8")

    print(f"CFG provenance v0.2.54: {summary['status']}")
    print(f"direct: historical={len(legacy_rows)} fresh={len(later_rows)} total={len(direct_rows)}")
    print(f"special: historical=18 fresh={len(later_special_rows)} total={len(special_rows)}")
    print(f"fresh clone template fields={template_fields}; redirect manifest rows={manifest_rows}")
    print(f"legacy tuple fingerprint={legacy_fp}")
    if errors:
        for e in errors: print("FAIL:", e)
        raise SystemExit(1)

if __name__ == "__main__":
    main()
