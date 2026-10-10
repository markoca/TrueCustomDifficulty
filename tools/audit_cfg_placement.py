#!/usr/bin/env python3
"""Audit that every TCD parameter is routed to the intended CFG location.

This is intentionally a static file/field placement audit.  It does not launch
or validate the application executable.  The contract is:

parameters.json target declaration -> cfg-writer-baseline concrete route ->
production writer route family.

Outputs a canonical parameter summary plus every direct concrete writer target
and every special-route placement used by the project.
"""
from __future__ import annotations

import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "Data" / "parameters.json"
BASELINE = ROOT / "Data" / "cfg-writer-baseline.json"
WRITER = ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs"

OP_ALIAS = {
    "scaleChanceClamp01": "scaleClamp01",
}


def load(path: Path):
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def norm_op(op: str) -> str:
    return OP_ALIAS.get(op, op)


def special_route_map(baseline: dict) -> dict[str, dict]:
    special = baseline.get("special") or {}
    out: dict[str, dict] = {}

    for row in special.get("listMembership") or []:
        out[row["parameterId"]] = {
            "route": "listMembership",
            "primaryFile": "GameData/DifficultyPrototypes.cfg",
            "detail": f"EnvironmentDifficulty.AdditionalMechanicsEffects member={row['member']}",
            "resource": "",
        }

    for row in special.get("setList") or []:
        out[row["parameterId"]] = {
            "route": "setList",
            "primaryFile": row["file"],
            "detail": row["path"],
            "resource": "",
        }

    for pid, key in (special.get("psyParameterKeys") or {}).items():
        out[pid] = {
            "route": "psyArray",
            "primaryFile": "GameData/DifficultyPrototypes.cfg",
            "detail": f"NPCCombatDifficulty.PsyPhantomNPCOverrides[*].{key}",
            "resource": "",
        }

    def add_single(key: str, route: str):
        cfg = special.get(key) or {}
        pid = cfg.get("parameterId")
        if pid:
            out[pid] = {
                "route": route,
                "primaryFile": cfg.get("file", ""),
                "detail": cfg.get("customGeneratorSid", "") or ", ".join(p.get("targetSid", "") for p in cfg.get("pools", [])),
                "resource": cfg.get("redirectManifestResource", ""),
            }

    add_single("npcMedicalLootProduction", "npcMedicalCloneRedirect")
    add_single("mediumStashAmmoProduction", "mediumStashCloneRedirect")
    add_single("destructibleAmmoProduction", "destructibleAmmoCloneRedirect")
    add_single("destructibleGrenadeProduction", "destructibleGrenadeCloneRedirect")

    cheap = special.get("cheapStashProduction") or {}
    for field, route in (("ammoQuantityParameterId", "cheapStashAmmoCloneRedirect"),
                         ("grenadeChanceParameterId", "cheapStashGrenadeCloneRedirect"),
                         ("grenadeQuantityParameterId", "cheapStashGrenadeQuantityCloneRedirect")):
        pid = cheap.get(field)
        if pid:
            out[pid] = {
                "route": route,
                "primaryFile": cheap.get("file", ""),
                "detail": cheap.get("customGeneratorSid", ""),
                "resource": cheap.get("redirectManifestResource", ""),
            }

    expensive = special.get("expensiveStashProduction") or {}
    pid = expensive.get("ammoQuantityParameterId")
    if pid:
        out[pid] = {
            "route": "expensiveStashAmmoCloneRedirect",
            "primaryFile": expensive.get("file", ""),
            "detail": expensive.get("customGeneratorSid", ""),
            "resource": expensive.get("redirectManifestResource", ""),
        }
    # couponChanceParameterId is intentionally not special-routed: it has its
    # own direct editPoints and therefore belongs to the ordinary route.

    crate_spawn = special.get("crateSpawnProduction") or {}
    for row in crate_spawn.get("categories") or []:
        pid = row.get("parameterId")
        if pid:
            out[pid] = {
                "route": "spawnActorRandomRemoveNode",
                "primaryFiles": [
                    "GameData/SpawnActorPrototypes/WorldMap_WP/<ActorSID>.cfg",
                    "DLCGameData/DLC1/SpawnActorPrototypes/WorldMap_WP/<ActorSID>.cfg",
                ],
                "detail": f"category={row.get('category', '')}; {row.get('expectedActorCount', 0)} base+DLC1 actor roots",
                "resource": crate_spawn.get("manifestResource", ""),
            }
    pid = crate_spawn.get("removalPercentParameterId")
    if pid:
        out[pid] = {
            "route": "spawnActorRandomRemoveNode",
            "primaryFiles": [
                "GameData/SpawnActorPrototypes/WorldMap_WP/<ActorSID>.cfg",
                "DLCGameData/DLC1/SpawnActorPrototypes/WorldMap_WP/<ActorSID>.cfg",
            ],
            "detail": "shared 0-100% build-time random removal fraction across eligible base+DLC1 rows",
            "resource": crate_spawn.get("manifestResource", ""),
        }

    return out


def manifest_path(resource: str) -> Path | None:
    if not resource:
        return None
    marker = ".Data."
    name = resource.split(marker, 1)[1] if marker in resource else resource.rsplit(".", 1)[-1]
    return ROOT / "Data" / name


def run(output: Path) -> int:
    catalog = load(CATALOG)
    baseline = load(BASELINE)
    parameters = catalog.get("parameters") or []
    by_id = {p["id"]: p for p in parameters}
    direct = defaultdict(list)
    for point in baseline.get("editPoints") or []:
        direct[point.get("parameterId")].append(point)
    special = special_route_map(baseline)

    errors: list[str] = []
    summary_rows: list[dict] = []
    direct_rows: list[dict] = []
    special_rows: list[dict] = []

    unknown_direct = sorted(set(direct) - set(by_id))
    unknown_special = sorted(set(special) - set(by_id))
    if unknown_direct:
        errors.append(f"direct editPoints reference unknown parameters: {unknown_direct}")
    if unknown_special:
        errors.append(f"special routes reference unknown parameters: {unknown_special}")

    for p in parameters:
        pid = p["id"]
        declared = p.get("targets") or []
        declared_files = sorted({t.get("file", "") for t in declared if t.get("file")})
        declared_ops = sorted({norm_op(t.get("operation", "")) for t in declared})
        pts = direct.get(pid, [])

        if pts:
            route = "directEditPoints"
            concrete_files = sorted({e.get("file", "") for e in pts})
            concrete_ops = sorted({e.get("operation", "") for e in pts})
            missing_files = sorted(set(concrete_files) - set(declared_files))
            missing_ops = sorted(set(concrete_ops) - set(declared_ops))
            if missing_files:
                errors.append(f"{pid}: direct writer files not declared by parameter targets: {missing_files}")
            if missing_ops:
                errors.append(f"{pid}: direct writer operations not declared by parameter targets: {missing_ops}")
            for e in pts:
                compatible_targets = [
                    t for t in declared
                    if t.get("scope") == e.get("scope")
                    and t.get("file") == e.get("file")
                    and norm_op(t.get("operation", "")) == e.get("operation")
                ]
                if not compatible_targets:
                    errors.append(
                        f"{pid}: concrete edit point has no matching declared scope/file/operation: "
                        f"{e.get('scope')} {e.get('file')} {e.get('operation')}"
                    )
                elif not any(str(e.get("key", "")).lower() in str(t.get("path", "")).lower() for t in compatible_targets):
                    errors.append(
                        f"{pid}: concrete key {e.get('key')!r} is not named by any compatible declared target path"
                    )
                direct_rows.append({
                    "parameterId": pid,
                    "visible": str(p.get("exposeInUi", True)).lower(),
                    "support": (p.get("support") or {}).get("status", ""),
                    "scope": e.get("scope", ""),
                    "file": e.get("file", ""),
                    "prototype": e.get("prototype", ""),
                    "nodePath": ".".join(e.get("nodePath") or []),
                    "key": e.get("key", ""),
                    "operation": e.get("operation", ""),
                    "sourceRaw": e.get("sourceRaw", ""),
                    "difficultySid": e.get("difficultySid", ""),
                })
            concrete_count = len(pts)
            resource = ""
            detail = ""
        else:
            cfg = special.get(pid)
            if not cfg:
                errors.append(f"{pid}: no direct editPoints and no known special writer route")
                route = "UNSUPPORTED"
                concrete_files = []
                concrete_ops = []
                concrete_count = 0
                resource = ""
                detail = ""
            else:
                route = cfg["route"]
                primary = cfg.get("primaryFile", "")
                primary_files = cfg.get("primaryFiles") or ([primary] if primary else [])
                concrete_files = list(primary_files)
                concrete_ops = declared_ops
                concrete_count = 1
                resource = cfg.get("resource", "")
                detail = cfg.get("detail", "")
                missing_primary = [f for f in primary_files if f not in declared_files]
                if missing_primary:
                    errors.append(f"{pid}: special writer primary files {missing_primary!r} not declared by parameter targets {declared_files}")
                res_path = manifest_path(resource)
                if resource and (res_path is None or not res_path.is_file()):
                    errors.append(f"{pid}: special redirect resource missing: {resource} -> {res_path}")
                special_rows.append({
                    "parameterId": pid,
                    "visible": str(p.get("exposeInUi", True)).lower(),
                    "support": (p.get("support") or {}).get("status", ""),
                    "route": route,
                    "primaryFile": " | ".join(primary_files),
                    "declaredPaths": " | ".join(t.get("path", "") for t in declared),
                    "declaredOperations": " | ".join(t.get("operation", "") for t in declared),
                    "routeDetail": detail,
                    "redirectManifestResource": resource,
                    "redirectManifestFile": str(res_path.relative_to(ROOT)) if res_path and res_path.is_file() else "",
                })

        summary_rows.append({
            "parameterId": pid,
            "tab": p.get("tab", ""),
            "group": p.get("group", ""),
            "label": p.get("label", ""),
            "visible": str(p.get("exposeInUi", True)).lower(),
            "support": (p.get("support") or {}).get("status", ""),
            "route": route,
            "declaredTargetCount": len(declared),
            "declaredFiles": " | ".join(declared_files),
            "declaredOperations": " | ".join(t.get("operation", "") for t in declared),
            "concreteWriterFiles": " | ".join(concrete_files),
            "concreteEditPointCount": concrete_count,
            "status": "PASS" if route != "UNSUPPORTED" else "FAIL",
        })

    # Direct concrete fields must have one logical owner. Multiple rows for the
    # same parameter are allowed, but no field may belong to two parameters.
    owners = defaultdict(set)
    for e in baseline.get("editPoints") or []:
        key = (e.get("scope"), e.get("file"), e.get("prototype"), tuple(e.get("nodePath") or []), e.get("key"))
        owners[key].add(e.get("parameterId"))
    collisions = {k: sorted(v) for k, v in owners.items() if len({x for x in v if x}) > 1}
    if collisions:
        errors.append(f"cross-parameter direct CFG field collisions: {len(collisions)}")

    # Production writer must actually contain the special route entry points.
    writer = WRITER.read_text(encoding="utf-8") if WRITER.is_file() else ""
    required_writer_tokens = {
        "listMembership": "WriteAdditionalMechanics(",
        "setList": "WriteCorpseSmartLoot(",
        "psyArray": "WritePsy",
        "npcMedicalCloneRedirect": "WriteNpcMedicalLootProduction(",
        "cheapStashAmmoCloneRedirect": "WriteCheapStashProduction(",
        "cheapStashGrenadeCloneRedirect": "WriteCheapStashProduction(",
        "cheapStashGrenadeQuantityCloneRedirect": "WriteCheapStashProduction(",
        "mediumStashCloneRedirect": "WriteMediumStashAmmoProduction(",
        "destructibleAmmoCloneRedirect": "WriteDestructibleAmmoProduction(",
        "destructibleGrenadeCloneRedirect": "WriteDestructibleGrenadeProduction(",
        "expensiveStashAmmoCloneRedirect": "WriteExpensiveStashProduction(",
        "spawnActorRandomRemoveNode": "WriteCrateSpawnRemovals(",
    }
    for route in sorted({r["route"] for r in special_rows}):
        token = required_writer_tokens.get(route)
        if token and token not in writer:
            errors.append(f"special route {route}: canonical writer token missing: {token}")

    output.mkdir(parents=True, exist_ok=True)

    def write_csv(name: str, rows: list[dict]):
        path = output / name
        if not rows:
            path.write_text("", encoding="utf-8")
            return
        with path.open("w", encoding="utf-8", newline="") as f:
            w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
            w.writeheader(); w.writerows(rows)

    write_csv("parameter-summary.csv", summary_rows)
    write_csv("direct-targets.csv", direct_rows)
    write_csv("special-routes.csv", special_rows)

    file_counts = Counter(r["file"] for r in direct_rows)
    file_rows = [{"file": f, "directEditPoints": n} for f, n in sorted(file_counts.items())]
    write_csv("cfg-file-summary.csv", file_rows)

    route_counts = Counter(r["route"] for r in summary_rows)
    visible = sum(r["visible"] == "true" for r in summary_rows)
    hidden = len(summary_rows) - visible
    direct_param_count = route_counts.get("directEditPoints", 0)
    special_param_count = len(summary_rows) - direct_param_count

    md = [
        "# CFG placement audit — v0.2.53",
        "",
        "This audit is deliberately **CFG-only**. It does not launch, build, or verify the EXE.",
        "It proves that every catalog parameter is routed to an explicit CFG writer location and that",
        "the catalog target declaration agrees with the canonical writer baseline/special route.",
        "",
        f"- Catalog parameters: **{len(parameters)}**",
        f"- Visible / hidden: **{visible} / {hidden}**",
        f"- Direct-editPoint parameters: **{direct_param_count}**",
        f"- Special-route parameters: **{special_param_count}**",
        f"- Concrete direct CFG edit points: **{len(direct_rows)}**",
        f"- Direct cross-parameter field collisions: **{len(collisions)}**",
        f"- Result: **{'PASS' if not errors else 'FAIL'}**",
        "",
        "## Route counts",
        "",
        "| Route | Parameters |",
        "|---|---:|",
    ]
    for route, count in sorted(route_counts.items()):
        md.append(f"| `{route}` | {count} |")
    md += [
        "",
        "## Evidence",
        "",
        "- `parameter-summary.csv` — one row per parameter and its writer route.",
        "- `direct-targets.csv` — every concrete ordinary CFG field: file, prototype, node, key and operation.",
        "- `special-routes.csv` — all special array/list/clone/redirect writer routes and their primary CFG.",
        "- `cfg-file-summary.csv` — direct edit-point counts per CFG file.",
        "",
        "Special clone/redirect writers may additionally patch consumer CFGs named by their locked redirect manifests;",
        "those are intentional route scaffolding, not a second gameplay control surface.",
        "",
    ]
    if errors:
        md += ["## Failures", ""] + [f"- {e}" for e in errors] + [""]
    (output / "README.md").write_text("\n".join(md), encoding="utf-8")

    result = {
        "status": "PASS" if not errors else "FAIL",
        "catalogParameters": len(parameters),
        "visible": visible,
        "hidden": hidden,
        "directParameters": direct_param_count,
        "specialParameters": special_param_count,
        "directEditPoints": len(direct_rows),
        "directCfgFiles": len(file_counts),
        "directTargetCollisions": len(collisions),
        "routeCounts": dict(sorted(route_counts.items())),
        "errors": errors,
    }
    (output / "summary.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")

    print(f"[{result['status']}] CFG placement: {len(parameters)}/{len(parameters)} parameters; {len(direct_rows)} direct edit points; {special_param_count} special routes")
    print(f"[INFO] direct CFG files={len(file_counts)} collisions={len(collisions)}")
    print(f"[OK] evidence: {output}")
    if errors:
        for error in errors:
            print(f"[FAIL] {error}")
        return 1
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "docs" / "cfg-placement" / "v0.2.53")
    args = parser.parse_args()
    return run(args.output.resolve())


if __name__ == "__main__":
    raise SystemExit(main())
