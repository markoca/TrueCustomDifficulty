# HUMAN TACTICS and HUMAN AWARENESS (2026-11-16)

Combat tab, after HUMAN ENEMIES: two compact card boards. All nine controls are `runtime_test_required`.

## HUMAN TACTICS — CombatSynchronization group tokens

`GameData/CombatSynchronizationPrototypes.cfg`: a squad may start an action only when its group token is free;
`CooldownMin/Max` is the pause after it was used. `CombatSynchronizerEasy/Medium/Hard` inherit `Default`,
Master (`CombatSynchronizerStalker`) owns a table with about half the pauses. Each table has four enemy-rank
blocks (Newbie / Experienced / Veteran / Master = the enemy's rank).

| Card | Id | FilterGroups | Default (Newbie) | Master table | Vanilla rate |
|---|---|---|---|---|---|
| GRENADES | `combat.npcGrenadeRate` | [9] ThrowGrenade, [16] Special.ThrowGrenade | 20–35 s | 10–17 s | 1×, Master 2× |
| SUPPRESSION | `combat.npcSuppressionRate` | [10] SuppressiveFire | 15–25 s | 7–12 s | 1×, Master 2× |
| FLANKING | `combat.npcFlankRate` | [5] Flank | 10–20 s | 5–10 s | 1×, Master 2× |
| PUSH | `combat.npcPushRate` | [6] Advance.Action, [7] Advance.Approach | 12–25 / 25–40 s | 6–12 / 12–20 s | 1×, Master 2× |
| CAMPING PENALTY | `combat.npcCamperPunishRate` | [8] CamperPunish | 2–5 s | 2–5 s | 1× |

* Range 0.25×–3×. Rate r writes **source × reference / r** into the difficulty's own table, so the vanilla
  rate reproduces it exactly (Master 2× = the Master table). The card shows the resulting Newbie-rank pause,
  e.g. `2x · grenade every 10–17 s`.
* One writer (`WriteCombatSynchronization`) handles these five and SPECIAL ABILITY RATE: when any is changed it
  writes the four full rank tables (non-bpatch; Easy/Medium/Hard have no own ranks) with every controlled group
  scaled; the loader guards every group's tag.
* `MaxScore` (how many may act at once) and the weapon-type `CombatSynchronizationScore` are not changed.

## ENEMY ACCURACY (on the HUMAN TACTICS board)

`combat.npcAccuracy`, 0.25×–2×: divides `DispersionRadius` of all 77 `*_NPC` weapon settings
(`WeaponData/CharacterWeaponSettingsPrototypes.cfg`, 75 base + 2 DLC1; e.g. AK74 300, guard pistols 12).
The distance curve and the zombie addend are unchanged. The game does not change it by difficulty.
`NPC_AttackCooldown` / `HipAccuracyMultiplier` stay retired.

## HUMAN AWARENESS (human NPCs only; mutants have their own senses)

| Card | Id | CFG | Vanilla |
|---|---|---|---|
| SIGHT | `combat.npcSightRange` 0.25–3× | `AIPrototypes/VisionScannerPrototypes.cfg` DefaultNPC Central / PeripheralVisionDistance | 8000 / 2500 (ahead 80 m / side 25 m; card: `ahead 80 m · side 25 m`; panel adds the factor) |
| HEARING | `combat.npcHearingRange` 0.25–3× | `AIPrototypes/HearingSensorPrototypes.cfg` BaseHearingSensor SoundEvents[*].HearingDistance (14 non-zero) | shots 46 m, steps 12 m, voice 30 m … |
| MEMORY | `combat.npcAlertMemory` 0.25–3× (0.05 steps, like SIGHT / HEARING) | `AIPrototypes/ThreatPrototypes.cfg` [1] DefaultNPC: freeze 30 s × m, loss 30/s ÷ m; Actions[3] SearchEnemy freeze 30 s × m, loss 20/s ÷ m | ~30 s |

Humans use DefaultNPC (ObjPrototypes NPCBase); bosses / zombies inherit where they do not override; the
mutant sensors (`MutantsHearingSensor`, `DefaultMutants` threat) are separate and untouched.

## Not used

`NPC_Armor_Strike_Add` / `Armor_Strike_Add` (too abstract), `NPC_Weapon_Rank_Add` (no consumer in the CFG),
save rules (out of scope).

## Checks

`tools/audit_human_tactics.py` (with `TCD_BASE_DUMP=<dump>/base`), `audit_cfg_source_values.py` for the 97 direct
points (exact), `audit_combat_contract.py`.

**Cards (2026-11-17):** concrete results only (`grenade every 20–35 s`, `steps 12 m`, `search 30 s`, `spread 100%`); track ends show the figures at the slider limits; the factor stays in the selected-control panel.

**Card texts (2026-11-17):** `throws a grenade every 20–35 s`, `pins you down every 15–25 s`, `flanks you every 10–20 s`, `rushes your cover every 12–25 s`, `flushes you out every 2–5 s`, `shot spread 100%`, `spots you 80 m ahead, 25 m aside`, `hears steps at 12 m`, `searches for 30 s`.

**HUMAN AWARENESS board (2026-11-17):** detection radar. The vision field is 80 m ahead, blending to 25 m at the 85° edge. The footstep circle is 12 m. The outer timer ring shows MEMORY. Square-root distance scale with true-metre rings; legend with the player texts. See UI-DESIGN-LOCK.

## HUMAN TACTICS visual editor (2026-11-17)

The six slider cards have become five interactive cooldown intervals and a circular accuracy spread editor (AggregateVisualControl.HumanTactics.cs). A common logarithmic ruler (0.5–160 seconds) shows each native min–max cooldown on one linked draggable interval; only the underlying single scalar rate changes. Readouts respect the selected reference difficulty's Default or Master combat-sync table. Pauses are not guaranteed action cadence.

The ENEMY ACCURACY reticle visualises relative DispersionRadius (100 / factor %) on a compact logarithmic ring scale (50–400%), never simulated bullet hits or chance. The source is unchanged. Right click resets a parameter; R resets all six; source step snapping and keyboard navigation remain. Layout is side-by-side at 600 DIP and stacked below that. Awareness legend stacks below the radar until 750 DIP. See UI-DESIGN-LOCK.md and VISUAL-CONTROL-SYSTEM.md for visual details.

### HUMAN TACTICS full-range sliders (2026-11-17 correction)

Each lane now has two distinct graphs: a thin noninteractive source-accurate cooldown min–max preview on the common seconds ruler, and a separate lower native-rate slider using the **entire** 0.25×–3× range. The rate handle always reaches both ends, independent of how narrow the seconds interval is. The reference-rate tick and all reset/keyboard interactions are preserved. No CFG, preset, or value range changes.

### 2026-11-17 — Simplified Human Tactics lanes

Each action has **one native-rate slider** covering its complete source range. The extra gold min–max cooldown strip, dotted reference interval and 1/5/20/80-second ruler above the slider were removed as redundant. The actual cooldown min–max remains in the right-aligned seconds readout; the native-rate reference tick remains on the active slider. Board heights are now 340/530 DIP. Existing gameplay/CFG mappings, presets, snapping and reset interactions are unchanged.

### HUMAN AWARENESS 200 m display limit (2026-11-17)

The radar's visual distance maximum is 200 m rather than 240 m. The 200 m labelled ring is now the outer distance circumference, making the 10 / 25 / 50 / 100 m central rings ~9.5% larger without increasing the physical board footprint. SIGHT's underlying 0.25–3× source still permits 240 m; the drawn sector clamps at 200 m only, while the real legend says 240 m. To keep the 200–240 m portion of the source usable, the SIGHT rim handle accepts a small drag overshoot outside the drawn border and carries a per-drag offset so clicking a capped 3× handle does not rewrite the value to 2.5×. HEARING is on the same 200 m scale; MEMORY remains a separate external time ring. Source files, CFG mappings, keyboard resets and presets are unchanged.

### HUMAN AWARENESS source maximum corrected to 200 m (2026-11-17)

Current behavior supersedes the earlier display-only cap. SIGHT is really limited to **2.5× = 200 m ahead / 62.5 m peripheral**, using source 80/25 m, while minimum 0.25× and 0.05× step remain. The editor, source catalog, CFG output and drawn 200 m radar all share this limit. Legacy user presets containing 3× SIGHT are clamped to 2.5× on load by existing ParameterViewModel logic. Removed obsolete 240 m overshoot and drag-offset path. HEARING and MEMORY are unchanged.

### FOG VISIBILITY — weather perception coefficient (2026-11-17)

- Source: STALKER 2.1 `GameData/AIGlobals.cfg` > `AISettings` > `WeatherSettings` > `[2]` with `WeatherSID = Fogy`.
- Only `VisibilityCoef` changes, default/source `0.7`; `HearingDistanceCoef = 0.7`, `FlairCoef = 0.7`, and the independent Fogy luminance coefficient `0.6f` stay untouched. This is **not** a calibrated detection range in metres and its AI-class scope is uncertain (global weather setting).
- Catalog: `combat.npcFogVisibility` (Human Awareness), raw percent 0.20–1.00, absolute step 0.05, source/Vanilla 0.70; `runtime_test_required` until an in-game 2.1 check.
- The small 270° fog knob sits under the three radar legends. It uses native source tick marks, source reference position, hover glow, right-click reset, keyboard control and R group reset. The 200 m SIGHT range remains independent and unchanged.
- Writer: exactly one direct edit point, root `AISettings`, node path `WeatherSettings/[2]`, key `VisibilityCoef`, raw source `0.7`, operation `set`, global CFG patch output. All other weather and luminance entries stay vanilla.
- Acceptance plan: compare Fogy 0.70 / 0.40 / 1.00 under constant conditions, watch human detection, hearing and mutant behavior, and verify the generated singleton patch is accepted by the game. Source structure and code audit are confirmed; runtime behavior is not.


## 2026-11-17 — CURRENT: weather hearing (RAIN NOISE / THUNDER NOISE)

* Three small 270-degree knobs are grouped side by side under the SIGHT / HEARING / MEMORY legend: FOG VISIBILITY, RAIN NOISE, THUNDER NOISE. The existing radar is unchanged. Small ticks, hover glow, reference marks, right-click reset, keyboard control, R group reset and only-changed-number accent match the catalog.
* RAIN NOISE (`combat.npcRainNoise`) changes both Rainy [3] and LightRainy [4] `HearingDistanceCoef` in `AISettings.WeatherSettings`, from the verified 2.1 source coefficient 0.7. THUNDER NOISE (`combat.npcStormNoise`) changes Thundery [5], source 0.6. Each knob ranges 10–100% with 5% steps.
* The native 2.1 source is `GameData/AIGlobals.cfg`. Both controls write only hearing values in the singleton cfg_patch and leave visibility, flair, audio, graphics and weather occurrence unchanged. The two direct routes are accounted for separately from the frozen writer-baseline blob; AI-class scope and effective metres require in-game testing.


## 2026-11-17 — CURRENT: no duplicate weather sliders below Human Awareness

* `combat.npcRainNoise` and `combat.npcStormNoise` are registered as **HumanAwareness visual-aggregate members** in `ParameterViewModel.VisualAggregateFamilyByParameterId`, like `combat.npcFogVisibility`. The single visible anchor remains `combat.npcSightRange`; the board contains exactly three weather knobs (FOG VISIBILITY, RAIN NOISE, THUNDER NOISE), with **no individual slider cards below it**.
* Both hearing parameters remain live in `MainViewModel.AllParameters` and the Human Awareness `AwarenessIds`; clicking the knobs still selects the parameter and shows its contextual panel. Preset values, the 2.1 CFG writer destinations, 5%-step snapping, right-click single reset, R group reset and hover/reference coloring are untouched. The AllControlsPatchContract now verifies the board-only visibility for all three weather knobs.


## 2026-11-17 — CURRENT: NOISE is the sound-masking percentage

* **RAIN NOISE** and **THUNDER NOISE** display the percentage of hearing masked, rather than remaining hearing. Game-native `HearingDistanceCoef = 0.70` is displayed as **30% RAIN NOISE**; `0.60` as **40% THUNDER NOISE**. Higher knob values mean stronger masking. 0% noise means no weather hearing penalty; the existing minimum coefficient of 0.10 means maximum 90% masking, in 5% steps. FOG VISIBILITY is unchanged (remaining visibility).
* The 270-degree dial orientation, thumb and grey source tick reverse for the two noise knobs. Pointer drag and keyboard Up/Right/End increase masking, Down/Left/Home decrease it; right click and R reset work as before. No redundant sliders or extra UI.
* Native catalog defaults, saved preset numbers, loading and CFG writer assignments remain unchanged. The player-facing readouts, reference label and rationale use `(1 - HearingDistanceCoef) × 100`. Rainy/LightRainy still share one source value; Thundery remains separate. Gameplay effect remains pending in-game validation.

