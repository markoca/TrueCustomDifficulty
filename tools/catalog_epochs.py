"""Catalog epochs shared by frozen historical audits.

The 361-source catalog (frozen 248-source ledger + 42 Stashes/Food/Armor/Weight
sources + 71 trader matrix sources) is the slice the historical CFG-mapping
audits describe. Controls added on 2026-11-16 (MUTANT AGGRESSION, ARTIFACT
HUNTING, HUMAN TACTICS / HUMAN AWARENESS) have their own dedicated audits and
are excluded from those historical slices here.
"""

POST_361_PREFIXES = ("combat.mutantAttackRate.", "combat.mutantAbilityRate.", "environment.artifact")
POST_361_IDS = frozenset({
    "combat.mutantSpecialAbilityRate",
    "combat.npcGrenadeRate", "combat.npcSuppressionRate", "combat.npcFlankRate",
    "combat.npcPushRate", "combat.npcCamperPunishRate", "combat.npcAccuracy",
    "combat.npcSightRange", "combat.npcHearingRange", "combat.npcAlertMemory",
    # 2026-11-17: weather AI coefficients and per-item medical HEAL TIME.
    "combat.npcFogVisibility", "combat.npcRainNoise", "combat.npcStormNoise",
    "health.bandageHealTime", "health.medkitHealTime", "health.armyMedkitHealTime",
    "health.ecoMedkitHealTime",
    # 2026-11-17: REMOVE LOOSE ARTIFACTS (own audit).
    "loot.looseArtifactsRemoved",
    # 2026-11-17: ARMY MEDKIT MIX in breakable medical boxes (own audit).
    "loot.medicalBoxArmyMedkitWeight",
    # 2026-11-17: SMART LOOT NEED (own audit).
    "loot.stashSmartStockedChance", "loot.stashSmartShortChance", "loot.stashSmartLootEnabled",
    # 2026-11-17: NPC SMART LOOT master (WriteCorpseSmartLoot).
    "loot.npcSmartLootEnabled",
    # 2026-11-17: SMART ODDS (stash SmartLoot categories).
    "loot.stashSmartMeds", "loot.stashSmartAmmo",
    "loot.stashSmartFoodDrinks", "loot.stashSmartGrenades",
})


def is_post_361(parameter_id: str) -> bool:
    return parameter_id in POST_361_IDS or parameter_id.startswith(POST_361_PREFIXES)


def slice_361(parameters):
    """Catalog entries of the 361-source historical slice (order preserved)."""
    result = [p for p in parameters if not is_post_361(p["id"])]
    assert len(result) == 361, len(result)
    return result


def edit_points_361(edit_points):
    return [e for e in edit_points if not is_post_361(e["parameterId"])]
