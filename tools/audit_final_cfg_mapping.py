#!/usr/bin/env python3
"""Final, reproducible *static* 2.0.5 catalog -> production BPatch writer audit.

Verifies source inventory and both direct/special writer dispatch, authored CFG
edit-point identities, all twelve shipped profiles and integer source effects.
This cannot execute the WPF writer or STALKER 2, nor validate game loading.
"""
from __future__ import annotations

import argparse
import csv
import json
import math
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"
WRITER_PATH = ROOT / "src" / "TrueCustomDifficulty" / "Services" / "CfgPatchWriterService.cs"
PRESETS = ROOT / "src" / "TrueCustomDifficulty" / "Presets"
NAMES = ("Immersive", "Tactical", "Field Medic", "Mutant Hunter",
         "Anomaly Runner", "Wanderer", "Mercenary", "Smuggler",
         "Scavenger", "Outcast", "Hardcore", "Hardcore+")
SIDS = {"Novice": "Easy", "Stalker": "Medium", "Veteran": "Hard", "Master": "Stalker"}
OPERATIONS = {
    "set", "toggleSourceOrZero", "negateSet", "inverseScale", "scale",
    "scaleNonZero", "scaleClamp01", "setRangeBound", "scaleIntegerHalfUp",
    "scaleIntegerHalfUpPreserveZero", "setDeltaFromRetainedFraction",
}
NON_UI = {
    "economy.repairDifficultyCost", "alife.mutantSpawn.generic",
    "survival.drunknessThresholdScale",
    "stamina.sprintDisableThreshold", "stamina.sprintRecoveryDelay",
    "stamina.lowRecoveryThreshold", "stamina.lowRecoveryStrength",
    "stamina.lowRecoveryDuration", "stamina.sprintUnderRunSpeedRegenCoef",
    "stamina.joggingRegenCoef", "stamina.slowRunRegenCoef",
    "stamina.sprintRegenCoef", "stamina.runRegenCoef",
}

def require(value, message):
    if not value:
        raise AssertionError(message)

def same(a, b):
    if isinstance(a, bool) or isinstance(b, bool):
        return a is b
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        return math.isclose(a, b, rel_tol=0, abs_tol=0.0000001)
    return a == b

def number(raw):
    s = str(raw).strip()
    if s.endswith("%"):
        return float(s[:-1].rstrip("fF")) / 100
    return float(s.rstrip("fF"))

def parse_source(raw):
    s = str(raw).strip()
    if s.lower() in ("true", "false"):
        return s.lower() == "true"
    try:
        return number(s)
    except ValueError:
        return s

def emitted_value(point, stored):
    op = point["operation"]
    source = parse_source(point["sourceRaw"])
    value = stored.get("number")
    if op == "set":
        return stored.get("boolean") if stored["kind"] == "toggle" else (
            stored.get("text") if stored["kind"] == "choiceSlider" else value)
    if op == "toggleSourceOrZero":
        return source if stored["boolean"] else 0.0
    if op == "negateSet":
        return -value
    if op == "inverseScale":
        require(value > 0, "inverseScale requires positive value")
        return source / value
    if op == "scale":
        return source * value
    if op == "scaleNonZero":
        return source if abs(source) < 1e-7 else source * value
    if op == "scaleClamp01":
        return min(1, max(0, source * value))
    if op == "setRangeBound":
        return stored["minNumber"] if point["key"] == "MinDurability" else stored["maxNumber"]
    if op in ("scaleIntegerHalfUp", "scaleIntegerHalfUpPreserveZero"):
        if op == "scaleIntegerHalfUpPreserveZero" and abs(source) < 1e-7:
            return 0
        return max(0 if op.endswith("PreserveZero") else 1, math.floor(source * value + .5))
    if op == "setDeltaFromRetainedFraction":
        return value - 1
    raise AssertionError("unrecognized writer operation: " + op)

def collect_resource_paths(obj, result):
    if isinstance(obj, dict):
        for key, child in obj.items():
            if "Resource" in key and isinstance(child, str) and child:
                result.append(child)
            collect_resource_paths(child, result)
    elif isinstance(obj, list):
        for child in obj:
            collect_resource_paths(child, result)

def method_routes(special):
    """Explicit production dispatch, not a generic string-search of IDs."""
    routes = defaultdict(set)
    def add(method, *ids):
        for pid in ids:
            require(isinstance(pid, str) and pid, method + " has missing parameter ID")
            routes[pid].add(method)
    add("WritePsyOverrides", *special["psyParameterKeys"].keys())
    add("WriteAdditionalMechanics", *(x["parameterId"] for x in special["listMembership"]))
    add("WriteCorpseSmartLoot", *(x["parameterId"] for x in special["setList"]))
    add("WriteNpcCorpseAmmoProduction", special["npcCorpseAmmoProduction"]["parameterId"])
    add("WriteNpcMedicalLootProduction", special["npcMedicalLootProduction"]["parameterId"])
    add("WriteCheapStashProduction", *(
        special["cheapStashProduction"][key] for key in
        ("ammoQuantityParameterId", "grenadeChanceParameterId", "grenadeQuantityParameterId")))
    # Twelve additional sources share the three audited production-only
    # stash clone writers. A source is routed to the class it changes; content
    # bias uses all three but represents one user control each.
    stash_ids = special["stashContentAndGearProduction"]["parameterIds"]
    require(len(stash_ids) == 12 and len(set(stash_ids)) == 12,
            "stash content/gear source roster drift")
    for pid in stash_ids[:6]:
        add("WriteCheapStashProduction", pid)
    for pid in stash_ids[6:]:
        tier = "Cheap" if "Cheap" in pid else ("Medium" if "Medium" in pid else "Expensive")
        add({"Cheap":"WriteCheapStashProduction",
             "Medium":"WriteMediumStashAmmoProduction",
             "Expensive":"WriteExpensiveStashProduction"}[tier], pid)
    medical_ids = special["stashMedicalProduction"]["parameterIds"]
    require(len(medical_ids) == 8 and len(set(medical_ids)) == 8,
            "stash medical item/stack roster drift")
    for pid in medical_ids:
        add("WriteCheapStashProduction", pid)
    add("WriteMediumStashAmmoProduction", special["mediumStashAmmoProduction"]["parameterId"])
    add("WriteExpensiveStashProduction", special["expensiveStashProduction"]["ammoQuantityParameterId"])
    add("WriteDestructibleAmmoProduction", special["destructibleAmmoProduction"]["parameterId"],
        *set(special["destructibleAmmoProduction"]["weightParameterIds"].values()))
    add("WriteDestructibleGrenadeProduction", special["destructibleGrenadeProduction"]["parameterId"])
    add("WriteTraderMinBuyCondition", special["traderMinBuyCondition"]["parameterId"])
    add("WriteWorldWeaponDistribution", *(x["parameterId"] for x in special["worldWeaponShuffle"]["parameters"]))
    add("WriteWorldAttachmentShuffle", special["worldAttachmentShuffle"]["parameterId"])
    add("WriteWorldDetectorSpawns", special["worldDetectors"]["parameterId"])
    add("WriteBlueprintShuffle", special["blueprintShuffle"]["parameterId"])
    add("WriteWorldF1GrenadeBoxes", special["worldF1GrenadeBoxes"]["parameterId"])
    add("WriteCrateSpawnRemovals", *(x["parameterId"] for x in special["crateSpawnProduction"]["categories"]))
    add("WriteLooseWorldLootRemovals", special["looseWorldLootProduction"]["parameterId"])
    add("WriteLooseUniqueWeaponShuffle", special["looseUniqueWeaponShuffle"]["parameterId"])
    return routes

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, help="Write a machine-readable inventory and CSV")
    args = parser.parse_args()
    catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
    baseline = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
    code = WRITER_PATH.read_text(encoding="utf-8")
    definitions = {p["id"]: p for p in catalog["parameters"]}
    require(len(definitions) == len(catalog["parameters"]) == 268, "268 unique CFG source IDs expected")
    require(baseline["supportedParameterCount"] == 268, "writer expected coverage drift")
    require(not baseline["deferredParameterIds"], "deferred production parameters")
    require(baseline["gameVersion"] == catalog["source"]["gameVersion"] == "2.0.5", "source/game baseline mismatch")
    require(set(catalog["difficultyProfiles"]) == set(SIDS), "difficulty baseline drift")
    for difficulty, sid in SIDS.items():
        require(catalog["difficultyProfiles"][difficulty]["sid"] == sid, "CFG difficulty SID drift: " + difficulty)
    direct = defaultdict(list)
    owned = {}
    for point in baseline["editPoints"]:
        pid = point["parameterId"]
        require(pid in definitions, "orphan writer point: " + pid)
        require(point["operation"] in OPERATIONS, "unknown transform: " + str(point))
        require(all(point.get(k) not in (None, "") for k in ("scope", "file", "prototype", "key", "sourceRaw")),
                "incomplete point: " + str(point))
        require(point["scope"] in ("baseGame", "dlc1"), "unsupported scope: " + str(point))
        require(point["file"].endswith(".cfg"), "non CFG direct target: " + str(point))
        require(point.get("difficultySid", "") in ("", *SIDS.values()), "bad difficulty SID: " + str(point))
        target = (point["scope"], point["file"], point["prototype"],
                  tuple(point.get("nodePath", [])), point["key"])
        prior = owned.get(target)
        require(prior in (None, pid), "different IDs own the same concrete CFG leaf: " + repr(target))
        owned[target] = pid
        require(any(t["scope"] == point["scope"] and t["file"] == point["file"]
                    and t["operation"] == point["operation"] for t in definitions[pid]["targets"]),
                "catalog/baseline target file/scope/operation drift: " + pid)
        if point["operation"] not in ("set", "toggleSourceOrZero"):
            parse_source(point["sourceRaw"])
        direct[pid].append(point)
    require(len(baseline["editPoints"]) == 1230 and len(direct) == 213,
            "direct writer point count drift")
    special = method_routes(baseline["special"])
    require(len(special) == 55, "special writer route count drift")
    require(not set(direct).intersection(special), "direct/special parameter double ownership")
    require(set(direct) | set(special) == set(definitions), "unrouted CFG IDs")
    require(len(definitions) - len(NON_UI) == 255, "UI dimension count drift")
    require(NON_UI.issubset(definitions), "missing intentional hidden source")
    for pid in NON_UI:
        require(definitions[pid].get("exposeInUi") is False, "non-UI setting exposed " + pid)
    for pid, p in definitions.items():
        require(p.get("targets"), "source target missing " + pid)
        require(p.get("support", {}).get("status") in ("confirmed", "runtime_test_required"),
                "missing evidence status " + pid)
    main_write = code.split("public CfgWriteResult Write(", 1)[1].split("private static void ValidateBaseline", 1)[0]
    require("baseline.EditPoints" in main_write and "TransformValue(parameter, point)" in main_write,
            "production direct CFG dispatch missing")
    require("p.DifficultySid.Equals(difficulty.Sid" in main_write and "IsScopeApplicable(p.Scope, hasDlc1)" in main_write,
            "difficulty/DLC guards missing from direct dispatch")
    require("writtenParameters.Add(parameter.Id)" in main_write,
            "direct writer does not record actual edits")
    require("modifiedSupported.Where(id => !writtenParameters.Contains(id))" in main_write,
            "production missing-output guard was removed")
    for operation in OPERATIONS:
        require('"' + operation + '" =>' in code, "direct transform not implemented: " + operation)
    for pid, methods in special.items():
        for method in methods:
            require(re.search(r"\b" + method + r"\s*\(", main_write),
                    "special method not invoked by production Write: " + pid + " -> " + method)
            require(code.count(method + "(") >= 2 and ("private static " in code.split(method + "(", 2)[1][-105:]),
                    "special method implementation absent: " + method)
    require("document.AddAssignment(point.Prototype, point.NodePath, point.Key, value)" in main_write,
            "direct nested BPatch source node write missing")
    require("fileName.EndsWith(\"Prototypes.cfg\"" in code and "_patch_{PatchSuffix}.cfg" in code
            and "{stem" in code and "{bpatch}" in code,
            "BPatch output path/renderer structural contract drift")
    require("File.WriteAllText(outputPath" in main_write, "staged CFG patch file not emitted")
    resources = []
    collect_resource_paths(baseline["special"], resources)
    for resource in resources:
        filename = resource.split(".Data.", 1)[-1]
        require((DATA / filename).is_file(), "missing special writer manifest " + resource)

    by_id = {}
    for pid, p in definitions.items():
        by_id[pid] = {
            "tab": p["tab"], "control": p["control"], "exposeInUi": p.get("exposeInUi", True),
            "evidence": p["support"]["status"], "route": "direct" if pid in direct else "special",
            "directCount": len(direct[pid]),
            "writerMethods": ["TransformValue"] if pid in direct else sorted(special[pid]),
            "directOperations": sorted(set(e["operation"] for e in direct[pid])),
            "directTargets": [{
                "scope": e["scope"], "file": e["file"], "prototype": e["prototype"],
                "nodePath": e.get("nodePath", []), "key": e["key"],
                "operation": e["operation"], "sourceRaw": e["sourceRaw"],
                "difficultySid": e.get("difficultySid", "")
            } for e in direct[pid]],
            "declaredTargets": p["targets"],
            "runtimeVerified": False,
        }
    inert = []
    for name in NAMES:
        doc = json.loads((PRESETS / (name + ".json")).read_text(encoding="utf-8"))
        require(len(doc["values"]) == 268, "factory catalog source count drift " + name)
        sid = SIDS[doc["referenceDifficulty"]]
        for pid, p in definitions.items():
            if pid not in direct:
                continue
            value = doc["values"][pid]
            ref = p["vanilla"].get(doc["referenceDifficulty"], p["vanilla"].get("All"))
            if value["kind"] == "slider":
                changed = not same(value["number"], ref)
            elif value["kind"] == "toggle":
                changed = not same(value["boolean"], ref)
            elif value["kind"] == "range":
                changed = not same(value["minNumber"], .25) or not same(value["maxNumber"], .50)
            else:
                changed = False
            if not changed:
                continue
            parent = p.get("requiresToggle")
            if parent and not doc["values"][parent]["boolean"]:
                continue
            points = [e for e in direct[pid] if e["scope"] == "baseGame"
                      and e.get("difficultySid", "") in ("", sid)]
            require(points, name + ": no base-game direct points for changed " + pid)
            if all(same(emitted_value(e, value), parse_source(e["sourceRaw"])) for e in points):
                inert.append({"profile": name, "parameterId": pid, "value": value,
                              "source": [(e["sourceRaw"], e["operation"]) for e in points]})
    require(not inert, "factory values cause zero concrete edits: " +
            ", ".join(f"{x['profile']}/{x['parameterId']}" for x in inert[:25]) +
            f" (total {len(inert)})")
    statuses = Counter(p["support"]["status"] for p in definitions.values())
    route_counts = Counter("direct" if pid in direct else "special" for pid in definitions)
    summary = {
        "gameVersion": baseline["gameVersion"],
        "catalogParameters": len(definitions),
        "editableDimensions": len(definitions) - len(NON_UI),
        "auditOnlyNonUi": len(NON_UI),
        "directParameterIds": route_counts["direct"],
        "specialParameterIds": route_counts["special"],
        "concreteEditPoints": len(baseline["editPoints"]),
        "directPhysicalTargetCollisions": 0,
        "unroutedParameterIds": 0,
        "factoryProfileCount": len(NAMES),
        "factoryInertModifiedDirectParameters": len(inert),
        "sourceEvidenceStatus": dict(statuses),
        "gameRuntimeEffectVerifiedByThisAudit": False,
        "productionWriterExecutedByThisAudit": False,
    }
    if args.out:
        args.out.mkdir(parents=True, exist_ok=True)
        (args.out / "mapping.json").write_text(json.dumps({
            "summary": summary, "sources": by_id,
        }, indent=2) + "\n", encoding="utf-8")
        with (args.out / "mapping.csv").open("w", newline="", encoding="utf-8") as fp:
            writer = csv.DictWriter(fp, fieldnames=[
                "parameterId", "tab", "exposeInUi", "evidence", "route",
                "directCount", "writerMethods", "directOperations"])
            writer.writeheader()
            for pid in sorted(by_id):
                e = by_id[pid]
                writer.writerow({"parameterId": pid, "tab": e["tab"],
                    "exposeInUi": e["exposeInUi"], "evidence": e["evidence"],
                    "route": e["route"], "directCount": e["directCount"],
                    "writerMethods": ";".join(e["writerMethods"]),
                    "directOperations": ";".join(e["directOperations"])})
    print(json.dumps(summary, indent=2))
    print("PASS: static catalog -> actual production BPatch route and quantized factory-output audit")
    print("NOT VERIFIED: .NET/WPF execution, real game BPatch merge, live gameplay outcome")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
