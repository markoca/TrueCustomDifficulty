# Factory-profile deep audit — 2026-10-15

**Historical baseline:** The 78-source role pass documented below has been followed by a consumable cadence / override rebalance. Current values and the new source-backed per-role contract live in `tools/factory_preset_consumable_balance.json` and `docs/FACTORY-PRESETS.md`; earlier hunger/drink values here are not the current factory snapshots.

## Scope and source verification

Twelve role-defined stock files each continue to ship **248 native CFG-backed parameters** (2,976 stored entries) across Combat, Survival, Health, Master, Stamina, Loot, Economy, Equipment, Environment and A-Life. A further **78 real source controls** are explicitly reauthored in `tools/factory_preset_role_rebalance.json`, yielding **702 changed source values**. Complete snapshots remain in `src/TrueCustomDifficulty/Presets/*.json`. This new matrix governs only changed values; unchanged source values retain their earlier provenance, bounds and CFG mappings. Original 37-source expansion and 46-source scarcity matrices have been synchronized.

This is an authored *gameplay balancing hypothesis*, not observed empirical in-game death frequency, loot rates or money earnings. Native world placement removal applies only to confirmed eligible objects; weapon density applies to proven regular world-weapon locations; changing a trader price coefficient alone does not prove net artifact/trophy resale profit. Values are recorded at their real native control scale, not merged into a fictional universal difficulty coefficient.

## Audited identity matrix

| Profile | Loose world removed | Eligible world weapon density kept | Medkit healing | Max stamina | Sprint cost | Artifact price coefficient | Mutant-part price coefficient | Actual source changes |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Immersive | 56% | 60% | 60 HP | 105 | 5.25 | 1.20x | 1.10x | 37 |
| Tactical | 58% | 55% | 45 HP | 115 | 3.5 | 1.10x | 1.05x | 65 |
| Field Medic | 50% | 60% | 105 HP | 105 | 5.25 | 1.00x | 1.05x | 65 |
| Mutant Hunter | 62% | 50% | 65 HP | 120 | 5 | 1.10x | 1.95x | 62 |
| Anomaly Runner | 61% | 60% | 60 HP | 150 | 2.75 | 1.85x | 1.15x | 66 |
| Wanderer | 58% | 60% | 60 HP | 155 | 2.5 | 1.65x | 1.20x | 70 |
| Mercenary | 61% | 60% | 65 HP | 120 | 3.25 | 1.15x | 1.10x | 62 |
| Smuggler | 70% | 45% | 55 HP | 120 | 5.5 | 1.95x | 1.50x | 61 |
| Scavenger | 70% | 40% | 50 HP | 90 | 6.25 | 1.15x | 1.40x | 59 |
| Outcast | 76% | 30% | 40 HP | 90 | 7 | 1.10x | 1.30x | 49 |
| Hardcore | 76% | 35% | 45 HP | 80 | 8 | 1.20x | 1.35x | 49 |
| Hardcore+ | 81% | 25% | 35 HP | 75 | 8.75 | 1.20x | 1.40x | 57 |

## Each profile's intended immersion loop

**Immersive.** Credible scarce life with consequential firearm fights and treatment.

**Tactical.** Quick lethal headshots, mobile combat, expensive ammunition.

**Field Medic.** Specialist medical access and bleeding care without free gun supplies.

**Mutant Hunter.** Dangerous specialist mutant hunts paid by valuable trophies.

**Anomaly Runner.** High-risk anomaly and radiation routes paid by artifact returns.

**Wanderer.** Long-distance autonomy with strong stamina, cargo capacity and expensive fast travel.

**Mercenary.** Well-armed contract work funded by higher side missions and ammunition stock.

**Smuggler.** Cargo and black-market artifact trade offsetting hostile acquisition.

**Scavenger.** Damaged salvage and lean repairs as the resource progression engine.

**Outcast.** Low-access subsistence with fatigue and exposure consequences.

**Hardcore.** Scarce Master with severe but recoverable wounds and equipment wear.

**Hardcore+.** Last-resort Master with extreme finite supplies and severe injury.

## Coupled-source risk findings

**Abundance:** Vanilla-accessible supplies enter through unrelated world, NPC, container, trader and stash routes. Reducing only loose world pickups is not enough, and directly multiplying a configured crate source must not be mislabeled as an overall drop rate. We coordinated resource removal, world weapon density, corpse supply, trader stock, ammunition costs, medkit strength, item weights, repair costs, wear and economy rewards. Scarcity remains meaningful in Immersive; it is intentionally severe in Outcast and Master roles.

**Hard/last-survivor feedback loop:** Higher incoming damage plus simultaneously removing almost all health restoration, ammo and repairs risks softlock rather than immersion. Hardcore and Hardcore+ remain punishing but both retain positive medkit and sleep HP, nonzero trader ammo and specialist medicine channels. Hardcore+ has less loose loot, fewer ammo/food/medical crates and fewer eligible world weapons than Hardcore, without relying on health-sponge NPCs.

**Specialist trades:** Tactical uses lower humanoid HP and stronger accurate gunplay with a finite ammo economy. Field Medic trades gunpower for access to treatment and bleeding relief. Mutant Hunter receives more valuable trophy coefficients with deliberately risky mutant pressure and ammunition wear. Anomaly Runner and Wanderer favor stamina, drinking effects and radiation preparation, with correspondingly dangerous exposure and costly logistics. Mercenary receives contract income and ammunition access but gear costs money. Smuggler receives cargo capacity and artifact price leverage, not free world loot. Scavenger and Outcast must decide which marginal supplies deserve weight or repairs.

**Movement and native effect semantics:** The Water `WaterStamina2` +25% Stamina magnitude and `WaterStaminaPerAction1` negative SPDrain magnitude are now intentionally **independent by role** rather than forced equal, with separate 5s/30s duration targets. Parent durations remain whole seconds and child Water/Energy durations do not exceed their parent window. Native sources are not reinterpreted as verified per-second regeneration.

**Hazard fidelity:** Increased anomaly/radiation and psi pressure makes artifacts hard to recover rather than granting implicit resistance. Their offsets are actual anti-rad/healing sources and gear/carry options, not a fabricated protection modifier. Hidden `survival.drunknessThresholdScale`, `economy.repairDifficultyCost` and `alife.mutantSpawn.generic` remain neutral 1x because their precise gameplay effects lack sufficient validation.

**Source correctness and preservation:** Role deltas are checked against exact `parameters.json` IDs, source type, min/max and absolute step grid; all 248 factory entries remain present for every role. Medical/food invariants and no inverted trader buy/sell condition remain enforced. All control layouts, UI catalogs, native writer logic and factory reference difficulties remain unchanged.

**Upgrade of installed presets:** The previous 248-entry factory JSON Git blob SHA for each profile is allowlisted under `PreRoleRebalanceFactoryBlobs`; only byte-for-byte pristine installed factory files upgrade at startup. Factory-named edited files, user-named files and unknown future profiles are not replaced. This protects local balancing.

## Acceptance checklist

1. Run `python tools/audit_project_consistency.py` and the existing pre-release static audit/test pipeline in a developer workspace (not in a customer deployment context).
2. Publish with `./publish.sh`, launch via Wine and inspect all twelve profiles for legitimate ranges, native source texts and preserved Energy/Water controls.
3. Exercise preset saving/loading and verify a pristine previous embedded factory file upgrades, whereas a manually edited factory file stays unchanged.
4. In game, test each archetype's acquisition/expenditure loop at early and mid progression, particularly the hardest medical/recovery cases, Tactical lethality and specialist artifact/trophy economics.

Static coverage and role separation were validated before commit; **.NET build, Wine screenshots and live in-game balance have not yet been run**. Do not present configured ratios as measured world outcomes.
