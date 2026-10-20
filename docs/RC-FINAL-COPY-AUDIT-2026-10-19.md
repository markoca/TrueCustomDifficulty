# Final RC label & redundancy audit — updated 20 October 2026

## Verdict and exact scope

Two Human Enemies controls were retired on 2026-10-20: Attack Frequency and Shot Accuracy. The active ledger excludes them, preserving all other approved wording; the original 19 October review covered 182 controls before retirement. The live status counts and table below describe the current 180-row ledger.

**The full copy/label pass is complete for all 180 player-visible controls across 11 tabs**. Of those, **102 control records** received player-facing copy corrections; **60 labels** were made shorter, consistent or more explicit. Other changes affect **60 HIGHER / LOWER** descriptions and **78 WORKS WITH** explanations. All **180 WHAT IT DOES** rationales were reviewed; **10** newer Psy/Stashes explanations from the concurrent RC pass are preserved alongside the targeted label and redundancy cleanup. This is a *copy/UI-meaning release gate*, not a claim of passing .NET, Wine, GitHub Actions or real-game tests.

The 180-row signed-off copy inventory is [RC-FINAL-COPY-INDEX-2026-10-19.csv](RC-FINAL-COPY-INDEX-2026-10-19.csv): one entry for each exact `id`, tab, group, approved label, WHAT IT DOES, HIGHER / LOWER, WORKS WITH and retained/revised decision. `tools/audit_rc_final_copy.py` compares every entry back to the current parameter catalog, validates all 11 tab totals, bans technical jargon and gestures outside CONTROLS, and protects gameplay-critical semantics. This complements the original 35-label approval and 180-rationale audit checks, rather than silently replacing either.

| Tab | Reviewed | Copy revised | Labels renamed |
|---|---:|---:|---:|
| combat | 15 | 6 | 3 |
| survival | 35 | 16 | 11 |
| health | 13 | 6 | 6 |
| master | 20 | 8 | 6 |
| stamina | 7 | 1 | 0 |
| loot | 27 | 23 | 14 |
| stashes | 34 | 31 | 17 |
| economy | 17 | 10 | 3 |
| equipment | 4 | 0 | 0 |
| environment | 6 | 1 | 0 |
| alife | 2 | 0 | 0 |
| **Total** | **180** | **102** | **60** |

## Approved gamer-facing label changes

These labels are the full audited rename set. No source identifiers, CFG paths, scalar ranges, default values or saved preset keys have been renamed.

### COMBAT

| Before | RC label |
|---|---|
| Human Enemy Gun Damage | **Enemy Gun Damage** |
| Repeated-Hit Protection - Maximum | **Peak Hit Protection** |
| Repeated-Hit Protection - Minimum | **Minimum Hit Protection** |
| Psy Equipment Durability Damage | **Psy Gear Damage** |

### SURVIVAL

| Before | RC label |
|---|---|
| Hunger Stamina Recovery Penalty | **Hunger Stamina Regen Loss** |
| Severe Hunger Stamina Recovery Penalty | **Severe Hunger Regen Loss** |
| Water Effect Duration | **Water Buff Duration** |
| Energy Drink Effect Duration | **Energy Drink Regen Duration** |
| Low Fatigue Aim-Time Penalty | **Light Fatigue Aim Delay** |
| Medium Fatigue Aim-Time Penalty | **Medium Fatigue Aim Delay** |
| Severe Fatigue Aim-Time Penalty | **Severe Fatigue Aim Delay** |
| Medium Fatigue Reload-Time Penalty | **Medium Fatigue Reload Delay** |
| Severe Fatigue Reload-Time Penalty | **Severe Fatigue Reload Delay** |
| Post-Sleep Health Restore | **Sleep HP Restore** |
| Post-Sleep Stamina Restore | **Sleep Stamina Restore** |

### HEALTH

| Before | RC label |
|---|---|
| Health Recovery Multiplier | **Health Regen Strength** |
| Low-Health Recovery Rate | **Low-HP Recovery Rate** |
| Bandage Bleeding Removal | **Bandage Bleed Removal** |
| Medkit Bleeding Removal | **Medkit Bleed Removal** |
| Army Medkit Bleeding Removal | **Army Medkit Bleed Removal** |
| Scientific Medkit Bleeding Removal | **Scientific Medkit Bleed Removal** |

### MASTER

| Before | RC label |
|---|---|
| Master Low-Health Mechanics | **Master Low-HP Penalties** |
| Master Low-Health Recovery Penalty | **Master Low-HP Regen Loss** |
| Severe-Fatigue Max Stamina Loss | **Severe Fatigue Stamina Loss** |
| Severe-Fatigue Carry Capacity Loss | **Severe Fatigue Carry Loss** |
| Severe-Fatigue Recoil Increase | **Severe Fatigue Recoil** |
| Severe-Fatigue Shot Spread | **Severe Fatigue Bullet Spread** |

### LOOT

| Before | RC label |
|---|---|
| NPC Medical Drop | **NPC Medical Drop Chance** |
| Bloodsucker Part Drop Chance | **Bloodsucker Part Chance** |
| Boar Part Drop Chance | **Boar Part Chance** |
| Burer Part Drop Chance | **Burer Part Chance** |
| Bayun Part Drop Chance | **Bayun Part Chance** |
| Chimera Part Drop Chance | **Chimera Part Chance** |
| Controller Part Drop Chance | **Controller Part Chance** |
| Deer Part Drop Chance | **Deer Part Chance** |
| Flesh Part Drop Chance | **Flesh Part Chance** |
| Poltergeist Part Drop Chance | **Poltergeist Part Chance** |
| Pseudodog Part Drop Chance | **Pseudodog Part Chance** |
| Pseudogiant Part Drop Chance | **Pseudogiant Part Chance** |
| Tushkan Part Drop Chance | **Tushkan Part Chance** |
| Snork Part Drop Chance | **Snork Part Chance** |

### STASHES

| Before | RC label |
|---|---|
| Bandages Stash Chance Scale | **Bandage Chance Modifier** |
| Medkits Stash Chance Scale | **Medkit Chance Modifier** |
| Army Medkits Stash Chance Scale | **Army Medkit Chance Modifier** |
| Anti-Rad SmartLoot Priority | **Anti-Rad Availability** |
| Anti-Rad Stash Chance Scale | **Anti-Rad Chance Modifier** |
| Medkits Minimum Count | **Minimum Medkits** |
| Medkits Maximum Count | **Maximum Medkits** |
| Army Medkits Minimum Count | **Minimum Army Medkits** |
| Army Medkits Maximum Count | **Maximum Army Medkits** |
| Standard medical Chance | **Standard Medical Chance** |
| Standard drinks Chance | **Standard Drinks Chance** |
| Standard coupons Chance | **Standard Coupon Chance** |
| Rare ammo Chance | **Rare Ammo Chance** |
| Rare medical Chance | **Rare Medical Chance** |
| Rare food Chance | **Rare Food Chance** |
| Rare drinks Chance | **Rare Drinks Chance** |
| Rare grenades Chance | **Rare Grenade Chance** |

### ECONOMY

| Before | RC label |
|---|---|
| Base Repair Price | **Base Repair Cost** |
| Main Quest Coupon Rewards | **Main Quest Coupons** |
| Side Quest Coupon Rewards | **Side Quest Coupons** |


## Redundancy and meaning fixes

- **Loot and stash chance:** a category **Priority** is selection preference within native loot pools, not an absolute item drop rate. **Chance Modifier/Standard/Rare Chance** operates on original item chances; **100% leaves the game's chance as-is, never promises a guaranteed physical stash or item**. MEDICAL availability (Anti-Rad) remains an ON/OFF gate; quantities remain stack counts. Previous explanation paragraphs repeated exact internal `Weight`, `Chance`, source scope or authoring rules and UI gestures; the revised copy gives what *higher/lower* changes in game, with safeguards in WORKS WITH.
- **Mutant parts:** the old HIGHER / LOWER block described an editable chance as **"not a setting"**, while WHAT THIS VALUE MEANS repeated the number and identical per-species probability examples. All thirteen species keep their own part probability, but now use compact **"<species> Part Chance"** labels, one common player-facing explanation of one part per successful eligible roll, and no repeated species-by-species WORKS WITH block. **Bayun** remains the displayed name for internal `catChance`.
- **Shop prices:** **Prices Traders Pay** still means selling gear to a trader; **Prices You Pay** still means buying. Price copy no longer repeats generic shop-price caveats verbatim. Main- and side-quest coupon rewards remain separate.
- **Hunger and survival:** the four Food Hunger Relief values are separate native effects **inside the Hunger board**; they do not change passive Hunger Rate, HP healing or Weird Kettle variants. Stamina penalties, aim/reload delays and Master fatigue captions have shorter labels without changing their signed magnitudes or activation. **Starvation Action Cost** remains action-based, not passive stamina drain. The 100 HP post-sleep tool default and original 300 HP game source remain distinct.
- **Combat, equipment and anomalies:** repeated-hit protection is a *strength range*, not a direct reduction percentage. Enemy gun damage, shot accuracy and player weapon damage remain different rules. Individual anomaly damage and global damage continue to combine before protection; weapon wear remains separate from jam chance.
- **Detail panel:** retained the five purpose-specific headings WHAT IT DOES, WHAT THIS VALUE MEANS (only where useful), HIGHER / LOWER, WORKS WITH and CONTROLS. Removed redundant generic loot choice/toggle descriptions and repeated exact mutant chance numbers from the dynamic readout; kept numeric examples where they genuinely clarify **relative versus absolute chance**. Right-click/drag/wheel/keyboard instructions belong only under CONTROLS.

## RC acceptance boundary

This pass changes **catalog copy, right-hand readout wording, audit/test text and documentation only**. It does **not** change any CFG writer behavior, parameter identity, value, slider range, source manifest, production routing, preset serialization, composite family, WPF geometry or build output. Any earlier layout and behavior specs, especially Hunger aggregate geometry, remain authoritative.

The release candidate still requires step 2: green or fully diagnosed CI/build source and writer validation, followed by final local Wine/game acceptance. A green static copy audit is evidence of wording/census consistency only, not of rendering or gameplay.

