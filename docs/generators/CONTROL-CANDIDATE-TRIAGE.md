# Production control candidate triage — current through v0.2.26

This is the post-audit UI decision layer. Structural isolation is necessary but not sufficient: a normal control must also have honest gameplay meaning and complete coverage for the label shown to the player.

## Hard exclusions

Test/debug-only, Prologue-specific and one-time/story-scripted targets remain hard-excluded. See `CONTROL-CANDIDATE-EXCLUSIONS.md`.

## Existing generator-backed controls that stay

| Control | Decision |
|---|---|
| Extra NPC Corpse Loot | **KEEP EXISTING** — exact difficulty -> SmartLoot set/list mapping. |
| Expensive Stash Money Chance | **KEEP EXISTING** — narrow direct stash-facing Chance control. |
| 14 Mutant Part Drop Chances | **KEEP EXISTING** — strict one-species / one-part / one-Chance mappings. |
| Ammo Crate Quantity | **KEEP EXISTING / RUNTIME TEST REQUIRED** — 1,235 production-only redirects to TCD-owned SNG/NATO copies; only `MinCount`/`MaxCount` are scaled. |
| Grenade Crate Quantity | **KEEP EXISTING / RUNTIME TEST REQUIRED** — 182 production-only redirects (101 F1 / 81 RGD-5); only `MinCount`/`MaxCount` are scaled. |

## Current new-control decisions

| Candidate | Status | Exact boundary |
|---|---|---|
| **Zombie Grenade Frequency** | **IMPLEMENTED / RUNTIME TEST REQUIRED** | 91 child-local grenade rows across 91/93 generic roots. v0.2.17 scales source `Chance` only; two generic Scientist-Recon roots have no grenade branch and remain untouched. |
| **Blueprint Shuffle** | **IMPLEMENTED / RUNTIME TEST REQUIRED** | 77 one-to-one `Stash_Blueprint_*` world slots; build-time deranged permutation without replacement. Faust quest blueprint + 2 unresolved stash generators excluded. |
| Zombie Weapon Condition | **NO UI / AUDIT ONLY** | Fully mapped, but weapon condition is loot/loadout tuning rather than a core difficulty control. |
| Zombie Weapon Ammo | **NO UI / AUDIT ONLY** | Fully mapped, but attached ammo is loot/loadout tuning rather than a core difficulty control. |
| Human NPC Detector Chance | **NO UI — NOT CORE DIFFICULTY** | Structurally clean, but this is a loot/economy detail rather than a useful core difficulty control. |
| Zombie Detector Chance | **NO UI — NO OBSERVABLE LOOT VALUE** | Source rows exist, but detector generation is not a useful player-facing zombie loot mechanic. |
| Human NPC Junk / Misc Loot | **NO UI — SYSTEM MARKER** | All 35 standard-human-relevant `Junk` rows generate only `GuardQuestItem`, an invisible `Info` marker used by contextual-action inventory preconditions. Preserve vanilla `Chance=1`, `Count=1–1`; never tune `Category::Junk` globally. |

The exact generic-zombie baseline is frozen in `GENERIC-ZOMBIE-GENERATOR-BASELINE.md` and its companion CSV files. This corrects the earlier assumption that the 791 local weapon rows formed a complete 93-root surface: they cover **81 roots**, not all 93.

## Rule for the deferred zombie weapon controls

The corrected v0.2.16 model does not materialize parent `WeaponPrimary` rows. The safe design is to patch the 791 existing child-local primary rows and clone the 12 shared `*_WeaponPistol` pools, redirecting only the 85 generic-zombie pistol links. The two weaponless Scientist roots remain untouched.

Structural coverage is now complete. Both controls still need an in-game smoke test of the final dropped-weapon condition/ammo result before normal UI exposure.

## Other concepts not promoted

Human-NPC weapon condition/ammo is now structurally complete over the repeatable production surface after Prologue/named-MIK exclusions, but still requires runtime proof. Human-NPC grenades are now **closed as NO UI**: the 44-row surface contains 36 active `Chance` rows, 6 hard-excluded Prologue `Chance=0` rows, and 2 normal repeatable Weight-only rows (`GuardNPC_Varta_Stormtrooper_ItemGenerator` and `GuardNPC_Noon_Recon_ItemGenerator`). Excluding those two just to manufacture a percentage slider would make the label incomplete. Consumables, trader stock, world stash abundance, medical/food-drink destructibles and SmartLoot likewise do not collapse into one honest scalar. Ammo-crate and grenade-crate quantity are the two audited destructible exceptions and remain narrow count-only controls. Those decisions remain documented in their domain audits and in `control-candidate-triage.csv`.

## Machine-readable files

- `control-candidate-triage.csv` — current disposition per control concept.
- `control-candidate-triage-summary.json` — compact status summary.
- `generic-zombie-generator-baseline.csv` — all 93 generic zombie roots and coverage state.
- `generic-zombie-weapon-source-boundary.csv` — corrected 93-root primary/pistol/weaponless source classification.
- `generic-zombie-pistol-links.csv` — exact 85 zombie links to shared pistol pools.
- `generic-zombie-pistol-pool-entries.csv` — all 71 shared pistol source rows.
- `generic-zombie-weapon-coverage-gaps.csv` — retained only as a deprecated record of the old parent-backed assumption.
- `generic-zombie-grenade-targets.csv` — exact 91 grenade targets.
