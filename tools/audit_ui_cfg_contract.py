#!/usr/bin/env python3
from __future__ import annotations
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"

AGGREGATES = {
    "Breakable Box Ammo Distribution": [
        "loot.ammoBoxWeight918","loot.ammoBoxWeight045","loot.ammoBoxWeight545","loot.ammoBoxWeight012",
    ],
    "Stash Ammo Amounts": [
        "loot.cheapStashAmmoQuantity","loot.mediumStashAmmoQuantity","loot.expensiveStashAmmoQuantity",
    ],
    "Mutant Part Drop Chances": [
        "loot.mutant.blindDogChance","loot.mutant.bloodsuckerChance","loot.mutant.boarChance",
        "loot.mutant.burerChance","loot.mutant.catChance","loot.mutant.chimeraChance",
        "loot.mutant.controllerChance","loot.mutant.deerChance","loot.mutant.fleshChance",
        "loot.mutant.poltergeistChance","loot.mutant.pseudodogChance","loot.mutant.pseudogiantChance",
        "loot.mutant.snorkChance","loot.mutant.tushkanChance",
    ],
    "Individual Anomaly Damage": [
        "environment.electroAnomalyDamage","environment.chemicalAnomalyDamage","environment.fireAnomalyDamage",
        "environment.carouselAnomalyDamage","environment.toxicCloudDamage","environment.steamAnomalyDamage",
        "environment.clickerHealthDamage","environment.razorHealthDamage","environment.soapBubbleHealthDamage",
        "environment.fireballHealthDamage","environment.diamondHealthDamage",
    ],
    "World Loot Removal": [
        "loot.worldAmmoCratesRemoved","loot.worldF1GrenadeBoxes","loot.worldFoodDrinkCratesRemoved",
        "loot.worldMedicalCratesRemoved","loot.looseWorldLootRemoved",
    ],
    "Healing Amount": [
        "health.bandageHealing","health.medkitHealing","health.armyMedkitHealing","health.ecoMedkitHealing",
    ],
    "Action Costs": [
        "stamina.sprintCost","stamina.jumpCost","stamina.vaultCost","stamina.meleeNormalCost",
        "stamina.meleeStrongCost","stamina.butstockCost","stamina.aimRegenCoef",
    ],
    "Item Price Scalers": [
        "economy.consumableCost","economy.ammoCost","economy.armorCost",
        "economy.weaponCost","economy.artifactCost","economy.mutantPartValue",
    ],
    "Short Consumable Durations": [
        "survival.waterInstantStaminaRestore",  # Visible ConsumableMixer anchor.
        "survival.energyDrinkInstantStaminaRestore",  # Hidden native aggregate member.
        "survival.waterRegenDuration","survival.energyFullCostDuration",
        "survival.energyPartialCostDuration",
    ],
    "Mutant Health": [
        "combat.mutantHealth.bloodsucker","combat.mutantHealth.chimera","combat.mutantHealth.deer",
        "combat.mutantHealth.pseudogiant","combat.mutantHealth.pseudodog","combat.mutantHealth.burer",
        "combat.mutantHealth.bayun","combat.mutantHealth.controller",
        "combat.mutantHealth.blinddog","combat.mutantHealth.boar","combat.mutantHealth.flesh",
        "combat.mutantHealth.poltergeist","combat.mutantHealth.snork","combat.mutantHealth.tushkan",
    ],
}

# Unlike aggregate backing members, these source IDs are intentionally
# audit-only, not exposed by a native or aggregate editor.
EXPECTED_HIDDEN_NON_UI = {
    "economy.repairDifficultyCost",
    "alife.mutantSpawn.generic",
    "survival.drunknessThresholdScale",
    "stamina.sprintDisableThreshold",
    "stamina.sprintRecoveryDelay",
    "stamina.lowRecoveryThreshold",
    "stamina.lowRecoveryStrength",
    "stamina.lowRecoveryDuration",
    "stamina.sprintUnderRunSpeedRegenCoef",
    "stamina.joggingRegenCoef",
    "stamina.slowRunRegenCoef",
    "stamina.sprintRegenCoef",
    "stamina.runRegenCoef",
}

def fail(msg: str) -> None:
    raise SystemExit("[FAIL] " + msg)

def collect_parameter_ids(obj, all_ids):
    out = set()
    if isinstance(obj, dict):
        for k, v in obj.items():
            if k in all_ids:
                out.add(k)
            out |= collect_parameter_ids(v, all_ids)
    elif isinstance(obj, list):
        for x in obj:
            out |= collect_parameter_ids(x, all_ids)
    elif isinstance(obj, str) and obj in all_ids:
        out.add(obj)
    return out

def main():
    catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
    baseline = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
    parameters = catalog["parameters"]
    by_id = {p["id"]: p for p in parameters}
    all_ids = set(by_id)

    # Override is editor-only state, not a ninth Duration CFG source.
    if "survival.drinkDurationOverride" in all_ids:
        fail("UI-only Drink Duration Override must not masquerade as a gameplay catalog parameter")
    if sum(p["id"].endswith("EffectDuration") for p in parameters
           if p["id"].startswith("survival.")) != 5:
        fail("unexpected main drink-duration source inventory")

    # These are the CURRENT catalog invariants, not v0.9.26.51's 199/188
    # obsolete snapshot. The core must route every parameter, even hidden
    # aggregate backings; 13 independent source IDs are audit-only.
    if len(parameters) != 248:
        fail(f"catalog count drifted: {len(parameters)} != 248")
    if len(all_ids) != len(parameters):
        fail("duplicate catalog parameter IDs")

    visible_ids = {p["id"] for p in parameters if p.get("exposeInUi", True)}
    hidden_ids = all_ids - visible_ids
    aggregate_ids = {pid for ids in AGGREGATES.values() for pid in ids}
    effective_ui_ids = all_ids - EXPECTED_HIDDEN_NON_UI
    hidden_non_ui = all_ids - effective_ui_ids
    backing_ids = hidden_ids - EXPECTED_HIDDEN_NON_UI

    if len(visible_ids) != 148 or len(hidden_ids) != 100:
        fail(f"live visible/hidden catalog drift: {len(visible_ids)}/{len(hidden_ids)} != 148/100")
    if not EXPECTED_HIDDEN_NON_UI.issubset(hidden_ids):
        fail(f"declared non-UI controls became visible or vanished: {sorted(EXPECTED_HIDDEN_NON_UI-hidden_ids)}")
    if len(effective_ui_ids) != 235 or len(backing_ids) != 87:
        fail(f"active source dimension count drifted: {len(effective_ui_ids)} / {len(backing_ids)} backing")

    # Named legacy aggregate cohorts are evidence, not the complete editor
    # roster. Some families (e.g. individual mutant drops) have more than one
    # visible member; enforcing 'exactly one visible anchor' was already stale.
    for name, ids in AGGREGATES.items():
        if len(set(ids)) != len(ids):
            fail(f"{name}: duplicate backing source in aggregate cohort")
        missing = [pid for pid in ids if pid not in by_id]
        if missing:
            fail(f"{name}: missing source IDs {missing}")
        if not any(pid in visible_ids for pid in ids):
            fail(f"{name}: no player-visible entry for aggregate cohort")

    # Every hidden-but-editable parameter must be referenced by live source
    # code, rather than present only in catalog/baseline/documentation.
    live_sources = [
        ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "MainViewModel.cs",
        ROOT / "src" / "TrueCustomDifficulty" / "ViewModels" / "ParameterViewModel.cs",
        *sorted((ROOT / "src" / "TrueCustomDifficulty" / "Controls").glob("*.cs")),
    ]
    rendered_text = "\n".join(path.read_text(encoding="utf-8") for path in live_sources)
    unreferenced = sorted(pid for pid in backing_ids if f'"{pid}"' not in rendered_text)
    if unreferenced:
        fail(f"hidden editable source IDs without live UI/VM reference: {unreferenced}")

    edit_points = baseline.get("editPoints", [])
    direct_ids = {e["parameterId"] for e in edit_points}
    points_by_id = defaultdict(list)
    for e in edit_points:
        points_by_id[e["parameterId"]].append(e)
        for required in ("scope", "file", "prototype", "key", "sourceRaw", "operation"):
            if required not in e:
                fail(f"{e.get('parameterId')}: direct edit point missing {required}")

    special_groups = {}
    special_ids = set()
    for group, obj in baseline.get("special", {}).items():
        ids = collect_parameter_ids(obj, all_ids)
        special_groups[group] = ids
        special_ids |= ids

    routed_ids = direct_ids | special_ids
    if routed_ids != all_ids:
        fail(f"writer route coverage drifted: missing={sorted(all_ids-routed_ids)} extra={sorted(routed_ids-all_ids)}")

    ui_unrouted = effective_ui_ids - routed_ids
    if ui_unrouted:
        fail(f"effective UI dimensions without writer route: {sorted(ui_unrouted)}")

    # Direct writer targets may fan out, but the same concrete target must never belong
    # to two different parameter IDs.
    target_owners = defaultdict(set)
    for e in edit_points:
        target = (
            e.get("scope"), e.get("file"), e.get("prototype"),
            tuple(e.get("nodePath") or []), e.get("key")
        )
        target_owners[target].add(e.get("parameterId"))
    collisions = {k: v for k, v in target_owners.items() if len(v) > 1}
    if collisions:
        fail(f"concrete direct target collision count: {len(collisions)}")

    # Every special writer resource declared by the baseline must exist in the embedded
    # Data directory. This catches UI routes pointing to stale/missing manifests.
    resources = []
    def collect_resources(obj):
        if isinstance(obj, dict):
            for k, v in obj.items():
                if isinstance(v, str) and "Resource" in k:
                    resources.append(v)
                collect_resources(v)
        elif isinstance(obj, list):
            for x in obj:
                collect_resources(x)
    collect_resources(baseline.get("special", {}))
    missing_resources = []
    for resource in resources:
        filename = resource.split(".Data.", 1)[-1] if ".Data." in resource else resource
        if not (DATA / filename).is_file():
            missing_resources.append(filename)
    if missing_resources:
        fail(f"special writer resources missing: {sorted(set(missing_resources))}")

    # Support state must be explicit for every player-editable dimension.
    bad_support = [
        pid for pid in sorted(effective_ui_ids)
        if by_id[pid].get("support", {}).get("status") not in {"confirmed", "runtime_test_required"}
    ]
    if bad_support:
        fail(f"editable dimensions without explicit support status: {bad_support}")

    print("True Custom Difficulty — UI → real CFG contract audit")
    print(f"[OK] catalog: {len(parameters)} parameters")
    print(f"[OK] visible anchor parameters: {len(visible_ids)}")
    print(f"[OK] named audited aggregate cohorts: {len(AGGREGATES)} / {len(aggregate_ids)} member references")
    print(f"[OK] hidden active aggregate backings referenced by UI code: {len(backing_ids)}")
    print(f"[OK] effective player-editable dimensions: {len(effective_ui_ids)}")
    print(f"[OK] intentionally hidden non-UI settings: {len(hidden_non_ui)}")
    print(f"[OK] direct writer parameters: {len(direct_ids)} / {len(edit_points)} concrete edit points")
    print(f"[OK] special writer parameter references: {len(special_ids)}")
    print(f"[OK] writer route coverage: {len(routed_ids)}/{len(all_ids)} catalog parameters")
    print(f"[OK] player-editable route coverage: {len(effective_ui_ids)}/{len(effective_ui_ids)}")
    print(f"[OK] direct concrete target collisions: 0")
    print(f"[OK] special writer resources: {len(set(resources))} referenced / all present")
    print("[OK] every editable dimension has confirmed or runtime-test-required support status")
    print("PASS")

if __name__ == "__main__":
    main()
