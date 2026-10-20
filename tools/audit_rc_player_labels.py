#!/usr/bin/env python3
"""Release candidate: approved player-facing labels and WPF renderers."""
from pathlib import Path
import json
r=Path(__file__).resolve().parents[1]
p=json.loads((r/"src/TrueCustomDifficulty/Data/parameters.json").read_text())["parameters"]
by={v["id"]:v for v in p}
assert len(p)==len(by)==285
assert sum(v.get("exposeInUi",True) for v in p)==180
names={
  "combat.npcHealth": "Human Enemy HP",
  "combat.playerWeaponDamage": "Weapon Damage",
  "combat.playerHeadshotMultiplier": "Headshot Damage",
  "combat.weaponJamming": "Weapon Jam Rate",
  "combat.psySpawnCycleDelay": "Phantom Spawn Interval",
  "survival.hungerRate": "Hunger Rate",
  "survival.sleepinessRate": "Fatigue Rate",
  "survival.sleepEligibilityThreshold": "Sleep Threshold",
  "health.maxHP": "Max Health",
  "health.baseRegen": "Passive HP Regen",
  "health.regenDelay": "Regen Delay",
  "stamina.maxSP": "Max Stamina",
  "stamina.vaultMaxHeight": "Vault Height",
  "stamina.regeneration": "Stamina Regen",
  "loot.npcCorpseBonus": "Extra Corpse Loot",
  "loot.npcWeaponAmmoSupply": "Bonus Ammo Drops",
  "loot.destructibleAmmoQuantity": "Ammo per Box",
  "loot.destructibleGrenadeQuantity": "Grenades per Box",
  "loot.ammoBoxWeight918": "Ammo Type Mix",
  "loot.stashGearCoveragePercent": "Bonus Gear Coverage",
  "loot.expensiveStashMoneyChance": "Rare Stash Coupon Chance",
  "economy.buyCondition": "Prices Traders Pay",
  "economy.sellCondition": "Prices You Pay",
  "economy.traderMinBuyCondition": "Minimum Sellable Condition",
  "economy.itemValueFloor": "Broken Gear Value",
  "economy.traderAmmoStock": "Trader Ammo Supply",
  "economy.medicStock": "Medic Supplies",
  "economy.consumableCost": "Item Values",
  "economy.fastTravelCost": "Guide Travel Fees",
  "equipment.weaponWearPerShot": "Weapon Wear per Shot",
  "environment.electroAnomalyDamage": "Damage by Anomaly",
  "environment.psyAnomalyIntensity": "Psy Drain Strength",
  "alife.humanGroupSize.newbie": "Human Squad Size",
  "advanced.radiationMechanics": "Radiation Stamina Penalty",
  "survival.starvationStaminaDrain": "Starvation Action Cost"
}
assert len(names)==35
for pid,label in names.items():assert by[pid]["label"]==label,(pid,by[pid]["label"])
for old in ("Item Price Scalers","Equipment Wear","Individual Anomaly Damage","Human Group Size"):assert all(x["group"]!=old for x in p),old
check={
  "src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs": [
    "STASH LOOT MIX",
    "LOOT POOLS",
    "HUMAN ENEMY COMBAT",
    "GEAR WEAR",
    "BONUS GEAR COVERAGE",
    "BONUS AMMO DROPS",
    "AMMO PER BOX",
    "GRENADES PER BOX",
    "AMMO TYPE MIX",
    "PRICES TRADERS PAY",
    "PRICES YOU PAY",
    "STARVATION ACTION COST",
    "RADIATION STAMINA PENALTY"
  ],
  "src/TrueCustomDifficulty/Controls/AggregateVisualControl.StashBranch.cs": [
    "BASIC · LOOT PREVIEW"
  ],
  "src/TrueCustomDifficulty/Controls/ItemPriceScalersRadar.cs": [
    "ITEM VALUES"
  ],
  "src/TrueCustomDifficulty/Controls/AnomalyDamageRadar.cs": [
    "DAMAGE BY ANOMALY"
  ],
  "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs": [
    "Damage by Anomaly"
  ],
  "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs": [
    "Damage by Anomaly"
  ]
}
for file,markers in check.items():
 s=(r/file).read_text()
 for marker in markers:assert marker in s,(file,marker)
for file in ("docs/UI-DESIGN-LOCK.md","docs/VISUAL-CONTROL-SYSTEM.md"):assert "CURRENT RC player-facing labels (41 approved)" in (r/file).read_text()
print("PASS: 41 RC player-facing names, visible controls, and source identity")
