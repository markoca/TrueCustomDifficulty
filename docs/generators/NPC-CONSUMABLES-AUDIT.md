# NPC Consumables Generator Audit

**Source:** STALKER 2 runtime/game + DLC CFG dump 2.0.5  
**Scope:** `GeneralNPC_Consumables*` pools and their standard-human NPC loadout consumers  
**Status:** deep-audited; no runtime writer changes made

## Verdict

The consumable data itself is concrete, but the six active `GeneralNPC_Consumables_*` pools are **shared cross-context dependencies**. Editing their item rows would affect far more than ordinary human NPCs: named/scripted NPCs, zombies, quest/scripted generators, and in some families corpse overrides also reuse the same pools.

For a future **standard-human-NPC-only** control, the shared pool definitions are therefore **not safe 1:1 targets**. Standard human loadouts do have their own direct `SubItemGenerator` links to these pools, but the active links normally use `Weight=1` rather than an item-level `Chance`; changing that link would tune the entire consumable bundle and its exact probability semantics still require validation. Separate Medical/Food/Utility controls cannot be obtained by changing only that link.

There are **8** `GeneralNPC_Consumables*` definitions, **6 active/shared** and **2 unreferenced** in the supplied graph. They contain **142 item rows**, **10 unique item SIDs**, and every item row uses `Chance` plus count ranges; none uses `Weight` at the item-row level.

Standard-human loadouts contain **39** direct links to these pools: **37 active** links and **2 Prolog links explicitly carrying `Chance=0`**. No shared pool is standard-NPC-only.

## Runtime branch model

Every one of the eight pool definitions follows the same four-way gate structure:

| Branch | Player-facing difficulty | Internal `Diff` | Player rank | Meaning |
| --- | --- | --- | --- | --- |
| A | Novice / Stalker / Veteran | `Easy, Medium, Hard` | Newbie / Experienced | lower-rank branch for the first three vanilla difficulties |
| B | Novice / Stalker / Veteran | `Easy, Medium, Hard` | Veteran / Master | higher-rank branch for the first three vanilla difficulties |
| C | Master | `Stalker` | Newbie / Experienced | lower-rank Master branch |
| D | Master | `Stalker` | Veteran / Master | higher-rank Master branch |

This is a useful warning for future controls: **Game Difficulty and Player Rank are independent axes**. A consumable control must preserve both unless intentionally redesigning the generator.

## Pool inventory and blast radius

| Pool | Incoming refs | Standard | Named/scripted | Zombie | Quest/scripted | Corpse override | Shared parents | Trader | Items | Verdict |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |
| `GeneralNPC_Consumables` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | AntiRad<br>ArmyMedkit<br>Bandage<br>CannedFood<br>Energetic<br>Medkit<br>Sausage<br>Vodka | EXCLUDE — unreferenced/orphan in supplied graph |
| `GeneralNPC_Consumables_CloseCombat` | 203 | 8 | 155 | 16 | 1 | 0 | 21 | 0 | Bandage<br>Beer<br>Medkit<br>Sausage | REVIEW — shared across gameplay contexts; not standard-NPC-only |
| `GeneralNPC_Consumables_Heavy` | 39 | 4 | 20 | 9 | 0 | 0 | 6 | 0 | AntiRad<br>ArmyMedkit<br>Bandage<br>CannedFood<br>Medkit<br>Sausage | REVIEW — shared across gameplay contexts; not standard-NPC-only |
| `GeneralNPC_Consumables_Noon` | 68 | 4 | 40 | 16 | 0 | 0 | 8 | 0 | AntiRad<br>ArmyMedkit<br>Bandage<br>CannedFood<br>Energetic<br>Medkit<br>Sausage | REVIEW — shared across gameplay contexts; not standard-NPC-only |
| `GeneralNPC_Consumables_Recon` | 313 | 8 | 260 | 22 | 1 | 0 | 19 | 1 | AntiRad<br>Bandage<br>Bread<br>CannedFood<br>Energetic<br>Medkit | REVIEW — shared across gameplay contexts; not standard-NPC-only |
| `GeneralNPC_Consumables_Recon_Armored` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | ArmyMedkit<br>Bandage<br>CannedFood<br>Medkit<br>Vodka | EXCLUDE — unreferenced/orphan in supplied graph |
| `GeneralNPC_Consumables_Sniper` | 69 | 8 | 35 | 16 | 1 | 0 | 9 | 0 | ArmyMedkit<br>Bandage<br>Bread<br>CannedFood<br>Energetic | REVIEW — shared across gameplay contexts; not standard-NPC-only |
| `GeneralNPC_Consumables_Stormtrooper` | 359 | 7 | 297 | 19 | 7 | 1 | 24 | 0 | AntiRad<br>Bandage<br>Bread<br>Medkit<br>Vodka | REVIEW — shared across gameplay contexts; not standard-NPC-only |

`GeneralNPC_Consumables` and `GeneralNPC_Consumables_Recon_Armored` are retained in the catalog because they exist in the runtime CFG, but no incoming generator-composition edge reaches them in the supplied dump. They must not be treated as active generic pools without new evidence.

## Exact branch contents

The values below are raw source values. `Chance` is shown as a percentage for readability; no additional probability formula is assumed.

| Pool | Difficulty | Player rank | Possible item rows in this branch |
| --- | --- | --- | --- |
| `GeneralNPC_Consumables` | Novice / Stalker / Veteran | Newbie / Experienced | `Bandage` 15% ×1; `Sausage` 30% ×1; `Energetic` 10% ×1; `Medkit` 20% ×1; `Vodka` 10% ×1 |
| `GeneralNPC_Consumables` | Novice / Stalker / Veteran | Veteran / Master | `Bandage` 100% ×1–3; `CannedFood` 100% ×1; `Energetic` 100% ×0–1; `Medkit` 100% ×1; `ArmyMedkit` 100% ×0–1; `AntiRad` 100% ×0–2 |
| `GeneralNPC_Consumables` | Master | Newbie / Experienced | `Bandage` 6% ×1; `Sausage` 30% ×1; `Energetic` 10% ×1; `Medkit` 5% ×1; `Vodka` 10% ×1 |
| `GeneralNPC_Consumables` | Master | Veteran / Master | `Bandage` 50% ×1–2; `CannedFood` 100% ×1; `Energetic` 100% ×0–1; `Medkit` 50% ×1; `ArmyMedkit` 20% ×1; `AntiRad` 100% ×0–2 |
| `GeneralNPC_Consumables_CloseCombat` | Novice / Stalker / Veteran | Newbie / Experienced | `Bandage` 20% ×1; `Sausage` 60% ×1; `Medkit` 20% ×1; `Beer` 40% ×1 |
| `GeneralNPC_Consumables_CloseCombat` | Novice / Stalker / Veteran | Veteran / Master | `Bandage` 100% ×1–2; `Sausage` 60% ×1; `Medkit` 40% ×1; `Beer` 100% ×1–2 |
| `GeneralNPC_Consumables_CloseCombat` | Master | Newbie / Experienced | `Bandage` 5% ×1; `Sausage` 60% ×1; `Medkit` 6% ×1; `Beer` 40% ×1 |
| `GeneralNPC_Consumables_CloseCombat` | Master | Veteran / Master | `Bandage` 50% ×1–2; `Sausage` 60% ×1; `Medkit` 30% ×1; `Beer` 100% ×1–2 |
| `GeneralNPC_Consumables_Heavy` | Novice / Stalker / Veteran | Newbie / Experienced | `Bandage` 40% ×1; `Sausage` 60% ×1; `Medkit` 25% ×1 |
| `GeneralNPC_Consumables_Heavy` | Novice / Stalker / Veteran | Veteran / Master | `Bandage` 100% ×1–2; `CannedFood` 100% ×1; `Medkit` 100% ×1; `ArmyMedkit` 20% ×1; `AntiRad` 30% ×1 |
| `GeneralNPC_Consumables_Heavy` | Master | Newbie / Experienced | `Bandage` 30% ×1; `Sausage` 60% ×1; `Medkit` 17% ×1 |
| `GeneralNPC_Consumables_Heavy` | Master | Veteran / Master | `Bandage` 7% ×1–2; `CannedFood` 100% ×1; `Medkit` 50% ×1; `ArmyMedkit` 20% ×1; `AntiRad` 30% ×1 |
| `GeneralNPC_Consumables_Noon` | Novice / Stalker / Veteran | Newbie / Experienced | `Bandage` 20% ×1; `Sausage` 60% ×1; `Energetic` 15% ×1; `Medkit` 10% ×1 |
| `GeneralNPC_Consumables_Noon` | Novice / Stalker / Veteran | Veteran / Master | `Bandage` 100% ×1–3; `CannedFood` 100% ×1; `Energetic` 50% ×0–1; `Medkit` 40% ×1; `ArmyMedkit` 20% ×0–1; `AntiRad` 50% ×0–2 |
| `GeneralNPC_Consumables_Noon` | Master | Newbie / Experienced | `Bandage` 5% ×1; `Sausage` 60% ×1; `Energetic` 15% ×1; `Medkit` 7% ×1 |
| `GeneralNPC_Consumables_Noon` | Master | Veteran / Master | `Bandage` 50% ×1–2; `CannedFood` 100% ×1; `Energetic` 50% ×0–1; `Medkit` 30% ×1; `ArmyMedkit` 20% ×0–1; `AntiRad` 50% ×0–2 |
| `GeneralNPC_Consumables_Recon` | Novice / Stalker / Veteran | Newbie / Experienced | `Bandage` 30% ×1; `Bread` 70% ×1; `Energetic` 20% ×1; `Medkit` 10% ×1 |
| `GeneralNPC_Consumables_Recon` | Novice / Stalker / Veteran | Veteran / Master | `Bandage` 100% ×1–2; `CannedFood` 50% ×1; `Energetic` 100% ×1; `Medkit` 25% ×1; `AntiRad` 40% ×1 |
| `GeneralNPC_Consumables_Recon` | Master | Newbie / Experienced | `Bandage` 5% ×1; `Bread` 70% ×1; `Energetic` 20% ×1; `Medkit` 6% ×1 |
| `GeneralNPC_Consumables_Recon` | Master | Veteran / Master | `Bandage` 50% ×1–2; `CannedFood` 50% ×1; `Energetic` 100% ×1; `Medkit` 17% ×1; `AntiRad` 40% ×1 |
| `GeneralNPC_Consumables_Recon_Armored` | Novice / Stalker / Veteran | Newbie / Experienced | `Bandage` 15% ×1; `Medkit` 20% ×1; `Vodka` 10% ×1 |
| `GeneralNPC_Consumables_Recon_Armored` | Novice / Stalker / Veteran | Veteran / Master | `Bandage` 100% ×1–2; `Medkit` 50% ×1; `ArmyMedkit` 10% ×1; `Vodka` 50% ×1; `CannedFood` 30% ×1 |
| `GeneralNPC_Consumables_Recon_Armored` | Master | Newbie / Experienced | `Bandage` 6% ×1; `Medkit` 5% ×1; `Vodka` 10% ×1 |
| `GeneralNPC_Consumables_Recon_Armored` | Master | Veteran / Master | `Bandage` 7% ×1–2; `Medkit` 35% ×1; `ArmyMedkit` 10% ×1; `Vodka` 50% ×1; `CannedFood` 30% ×1 |
| `GeneralNPC_Consumables_Sniper` | Novice / Stalker / Veteran | Newbie / Experienced | `Bandage` 10% ×1; `CannedFood` 60% ×1; `Energetic` 20% ×1 |
| `GeneralNPC_Consumables_Sniper` | Novice / Stalker / Veteran | Veteran / Master | `Bandage` 100% ×1–2; `CannedFood` 50% ×1; `Energetic` 100% ×1; `ArmyMedkit` 30% ×1; `Bread` 100% ×1 |
| `GeneralNPC_Consumables_Sniper` | Master | Newbie / Experienced | `Bandage` 7% ×1; `CannedFood` 60% ×1; `Energetic` 20% ×1 |
| `GeneralNPC_Consumables_Sniper` | Master | Veteran / Master | `Bandage` 7% ×1–2; `CannedFood` 50% ×1; `Energetic` 100% ×1; `ArmyMedkit` 20% ×1; `Bread` 100% ×1 |
| `GeneralNPC_Consumables_Stormtrooper` | Novice / Stalker / Veteran | Newbie / Experienced | `Bandage` 30% ×1; `Bread` 70% ×1; `Medkit` 10% ×1; `Vodka` 10% ×1 |
| `GeneralNPC_Consumables_Stormtrooper` | Novice / Stalker / Veteran | Veteran / Master | `Bandage` 100% ×1–2; `Bread` 60% ×1; `Medkit` 30% ×1; `Vodka` 50% ×1; `AntiRad` 30% ×1 |
| `GeneralNPC_Consumables_Stormtrooper` | Master | Newbie / Experienced | `Bandage` 6% ×1; `Bread` 70% ×1; `Medkit` 5% ×1; `Vodka` 10% ×1 |
| `GeneralNPC_Consumables_Stormtrooper` | Master | Veteran / Master | `Bandage` 7% ×1–2; `Bread` 60% ×1; `Medkit` 20% ×1; `Vodka` 50% ×1; `AntiRad` 30% ×1 |

## Item-role matrix

The `Analysis group` column is **our review grouping**, not a separate CFG category; the runtime category for these rows is `EItemGenerationCategory::Consumable`.

| Item SID | Analysis group | Present in pools |
| --- | --- | --- |
| `AntiRad` | Medical | `Base`<br>`Heavy`<br>`Noon`<br>`Recon`<br>`Stormtrooper` |
| `ArmyMedkit` | Medical | `Base`<br>`Heavy`<br>`Noon`<br>`Recon_Armored`<br>`Sniper` |
| `Bandage` | Medical | `Base`<br>`CloseCombat`<br>`Heavy`<br>`Noon`<br>`Recon`<br>`Recon_Armored`<br>`Sniper`<br>`Stormtrooper` |
| `Beer` | Utility / alcohol | `CloseCombat` |
| `Bread` | Food | `Recon`<br>`Sniper`<br>`Stormtrooper` |
| `CannedFood` | Food | `Base`<br>`Heavy`<br>`Noon`<br>`Recon`<br>`Recon_Armored`<br>`Sniper` |
| `Energetic` | Utility / stimulant | `Base`<br>`Noon`<br>`Recon`<br>`Sniper` |
| `Medkit` | Medical | `Base`<br>`CloseCombat`<br>`Heavy`<br>`Noon`<br>`Recon`<br>`Recon_Armored`<br>`Stormtrooper` |
| `Sausage` | Food | `Base`<br>`CloseCombat`<br>`Heavy`<br>`Noon` |
| `Vodka` | Utility / alcohol | `Base`<br>`Recon_Armored`<br>`Stormtrooper` |

## Standard-human NPC links

These are the direct loadout-generator links that are actually isolated to the `human_npc_standard` context. They are useful as a boundary map, but **not yet approved writer targets**.

| Loadout generator | Direct runtime consumer(s) | Consumable pool | Edge field(s) | State |
| --- | --- | --- | --- | --- |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU` | GeneralNPC_Bandit_Recon_Prolog_AKU | `GeneralNPC_Consumables_CloseCombat` | Chance=0 | DISABLED EDGE (Chance=0) |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez` | GeneralNPC_Bandit_Recon_Prolog_Obrez | `GeneralNPC_Consumables_Recon` | Chance=0 | DISABLED EDGE (Chance=0) |
| `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom` | GeneralNPC_Neutral_SniperCustom | `GeneralNPC_Consumables_Sniper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GeneralNPC_SIRCAA_Scientist_Recon_ItemGenerator` | GeneralNPC_SIRCAA_Scientist_Recon | `GeneralNPC_Consumables_Recon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Bandit_CloseCombat_ItemGenerator` | GuardNPC_SultanBandit_CloseCombat | `GeneralNPC_Consumables_CloseCombat` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Bandit_Heavy_ItemGenerator` | GuardNPC_SultanBandit_Heavy | `GeneralNPC_Consumables_Heavy` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Bandit_Recon_ItemGenerator` | GuardNPC_SultanBandit_Recon | `GeneralNPC_Consumables_Recon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Bandit_Stormtrooper_ItemGenerator` | GuardNPC_SultanBandit_Stormtrooper | `GeneralNPC_Consumables_Stormtrooper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Corpus_CloseCombat_ItemGenerator` | GuardNPC_Corpus_CloseCombat | `GeneralNPC_Consumables_CloseCombat` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Corpus_Heavy_ItemGenerator` | GuardNPC_Corpus_Heavy | `GeneralNPC_Consumables_Heavy` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Corpus_Recon_ItemGenerator` | GuardNPC_Corpus_Recon | `GeneralNPC_Consumables_Recon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | GuardNPC_Corpus_Sniper | `GeneralNPC_Consumables_Sniper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | GuardNPC_Corpus_Stormtrooper | `GeneralNPC_Consumables_Stormtrooper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Duty_CloseCombat_ItemGenerator` | GuardNPC_Duty_CloseCombat | `GeneralNPC_Consumables_CloseCombat` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Duty_Heavy_ItemGenerator` | GuardNPC_Duty_Heavy | `GeneralNPC_Consumables_Heavy` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Duty_Recon_ItemGenerator` | GuardNPC_Duty_Recon | `GeneralNPC_Consumables_Recon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Duty_Sniper_ItemGenerator` | GuardNPC_Duty_Sniper | `GeneralNPC_Consumables_Sniper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | GuardNPC_Duty_Stormtrooper | `GeneralNPC_Consumables_Stormtrooper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | GuardNPC_Freedom_Sniper | `GeneralNPC_Consumables_Sniper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Monolith_CloseCombat_ItemGenerator` | GuardNPC_Monolith_CloseCombat | `GeneralNPC_Consumables_CloseCombat` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Monolith_Recon_ItemGenerator` | GuardNPC_Monolith_Recon | `GeneralNPC_Consumables_Recon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | GuardNPC_Monolith_Sniper | `GeneralNPC_Consumables_Sniper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | GuardNPC_Monolith_Stormtrooper | `GeneralNPC_Consumables_Stormtrooper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Neutral_CloseCombat_ItemGenerator` | GuardNPC_Neutral_CloseCombat<br>GuardNPC_Digger_CloseCombat | `GeneralNPC_Consumables_CloseCombat` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Neutral_Sniper_ItemGenerator` | GuardNPC_Neutral_Sniper<br>GuardNPC_Digger_Sniper | `GeneralNPC_Consumables_Sniper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Neutral_Stormtrooper_ItemGenerator` | GuardNPC_Neutral_Stormtrooper<br>GuardNPC_Digger_Stormtrooper | `GeneralNPC_Consumables_Stormtrooper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Noon_CloseCombat_ItemGenerator` | GuardNPC_Noon_CloseCombat | `GeneralNPC_Consumables_Noon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Noon_Recon_ItemGenerator` | GuardNPC_Noon_Recon | `GeneralNPC_Consumables_Noon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Noon_Sniper_ItemGenerator` | GuardNPC_Noon_Sniper | `GeneralNPC_Consumables_Noon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Noon_Stormtrooper_ItemGenerator` | GuardNPC_Noon_Stormtrooper | `GeneralNPC_Consumables_Noon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Varta_CloseCombat_ItemGenerator` | GuardNPC_Varta_CloseCombat | `GeneralNPC_Consumables_CloseCombat` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Varta_Heavy_ItemGenerator` | GuardNPC_Varta_Heavy | `GeneralNPC_Consumables_Heavy` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Varta_Recon_ItemGenerator` | GuardNPC_Varta_Recon | `GeneralNPC_Consumables_Recon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Varta_Sniper_ItemGenerator` | GuardNPC_Varta_Sniper | `GeneralNPC_Consumables_Sniper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GuardNPC_Varta_Stormtrooper_ItemGenerator` | GuardNPC_Varta_Stormtrooper | `GeneralNPC_Consumables_Stormtrooper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GeneralNPC_MIK_CloseCombat_ItemGenerator` | GeneralNPC_MykluhaFreedom_CloseCombat | `GeneralNPC_Consumables_CloseCombat` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GeneralNPC_MIK_Recon_ItemGenerator` | GeneralNPC_MykluhaFreedom_Recon | `GeneralNPC_Consumables_Recon` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GeneralNPC_MIK_Sniper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Sniper | `GeneralNPC_Consumables_Sniper` | Weight=1 | ACTIVE STANDARD-NPC LINK |
| `GeneralNPC_MIK_Stormtrooper_ItemGenerator` | GeneralNPC_MykluhaFreedom_Stormtrooper | `GeneralNPC_Consumables_Stormtrooper` | Weight=1 | ACTIVE STANDARD-NPC LINK |

Two Prolog links are explicitly disabled with `Chance=0`; they should remain excluded from any active-target allowlist unless runtime behavior proves they are intentionally activated elsewhere.

## What a future control would actually affect

| Candidate idea | Direct CFG target | Standard-NPC-only? | 1:1? | Decision |
| --- | --- | --- | --- | --- |
| NPC Medical Drop Chance | Medical item `Chance` rows inside shared pools | No | No | **Do not implement from shared pools.** Would also modify named NPCs/zombies/quests that reuse them. |
| NPC Food Drop Chance | Food item `Chance` rows inside shared pools | No | No | **Do not implement from shared pools.** Same blast-radius problem. |
| NPC Utility Drop Chance | Energetic/Vodka/Beer `Chance` rows inside shared pools | No | No | **Do not implement from shared pools.** Same blast-radius problem. |
| NPC Medical Stack Size | `MinCount/MaxCount` on medical rows inside shared pools | No | No | **Do not implement from shared pools.** Counts are real, scope is not. |
| Standard NPC Consumable Bundle | 37 active standard-NPC `SubItemGenerator` links | Yes, structurally | Not yet | Potential future aggregate control only after `Weight`/`Chance` edge semantics are runtime-validated. It changes the whole pool, not one item family. |
| Clone pools and rewire only standard NPC loadouts | New custom generator definitions + 37 rewires | Yes | No | Technically possible but this is authored generator redesign, not a source-aligned 1:1 control. Defer until deliberate design phase. |

## Audit decision

- **Shared pool item rows:** source-confirmed, but **excluded** for standard-NPC-only controls.
- **37 active standard-human source links:** scope-confirmed, **review only** until nested-generator selection semantics are proven.
- **2 `Chance=0` Prolog links:** **excluded** from active targeting.
- **2 unreferenced pool definitions:** **excluded** until a real consumer is found.
- **No Medical/Food/Utility NPC control is approved yet.** The mapping is now complete enough to explain exactly why.

Machine-readable companions: `npc-consumable-pools.csv`, `npc-consumable-branches.csv`, `npc-consumable-standard-consumers.csv`, and `npc-consumable-item-matrix.csv`.
