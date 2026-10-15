# Stamina Control Deep Audit — v0.2.46

### 2026-10-14 — Explicit stamina resource units (CURRENT)

Player-facing stamina amounts always use the full **STAMINA** unit, and recovery rates use **STAMINA/s** in Core Stamina, Stamina Usage, Exhaustion & Recovery, comparison/reference readouts and descriptions. Historical abbreviated units below are superseded. Keep exact native resource values, gameplay thresholds, source keys (`stamina.maxSP`, `RegenSP`), CFG edits and compact hit targets unchanged.

### 2026-10-11 — Compact Movement Limits and gamer-facing Exhaustion labels (latest authoritative contract)

**MOVEMENT LIMITS** in `StaminaCore` is now **two matching compact horizontal 116-DIP cards**, not a tall vertical vault bar paired with an oversized carry chart. **MAXIMUM VAULT HEIGHT** (`stamina.vaultMaxHeight`, native **70–300 uu**, 1-uu steps, vanilla **130 uu**) and **MAXIMUM CARRY WEIGHT** (`stamina.maxCarryWeight`, native **20–160 KG**, 5-KG steps, vanilla **80 KG**) share the **same title-left/value-right, rail, source-reference, handle and endpoint geometry**. Each card starts at **Y=191**, with its rail centered at card top +51, a 12-DIP sibling gap, and native min/max units in the bottom footer. The vault rail retains three **source-authored, clickable exact-value landmark captions**: **WAIST=130**, **EYES=225**, **ABOVE EYES=300**. At narrow widths ABOVE EYES drops one short label line to prevent overlap; clicking captions snaps the unchanged underlying source value exactly, not an approximation. The carry rail has only three intermediate kg calibration ticks (native quarter increments); it **does not** duplicate a separate “LOAD CAPACITY / KG” caption or suggest fabricated fatigue thresholds. Only a modified numeric token is highlighted, never its `uu`/`KG` unit or Vanilla/Base tick. Board height is **323 DIP** rather than 382, retaining the two top CORE STAMINA 105-DIP cards unchanged. Existing pointer, keyboard, right-click per-source reset, `R` family reset and original four CFG/preset mappings are preserved.

**EXHAUSTION & RECOVERY** retains its six independent source-native sliders and the 2+4 arrangement, 94-DIP card grammar, shared 0–60 STAMINA *visual ruler* for the thresholds, real Sprint Lock **0–32.2 STAMINA** interactive range, and compact responsive wrapping. The six current visible names, and corresponding catalog `label` metadata, are:
- **SPRINT STOPS BELOW** — `stamina.sprintDisableThreshold`: sprint lock threshold in STAMINA
- **BONUS REGEN BELOW** — `stamina.lowRecoveryThreshold`: low-stamina emergency recovery trigger in STAMINA
- **AIM SLOWS REGEN** — `stamina.aimRegenCoef`: percent penalty to regeneration *while aiming*, not a low-stamina aim penalty
- **SPRINT RECOVERY DELAY** — `stamina.sprintRecoveryDelay`: time before recovery following sprint exhaustion, seconds
- **BONUS REGEN POWER** — `stamina.lowRecoveryStrength`: additional low-stamina recovery strength in STAMINA/s
- **BONUS REGEN DURATION** — `stamina.lowRecoveryDuration`: duration of that bonus, seconds

The names are user-facing wording only: no source range, step, transformation, unit, writer, constraint or save format is modified. Supersede the older vertically oriented Vault Height design, 175-DIP Movement card, 382-DIP StaminaCore height, and technical Exhaustion captions in dated catalog sections below.

### 2026-10-11 — Exhaustion and water native-card redesign (current)

**EXHAUSTION & RECOVERY:** The six current, independently writable `ActionCostsRadar` / `Mode=Exhaustion` sources are shown in **six uniformly formatted 94-DIP native-track cards**. The two left-hand stacked cards are **SPRINT LOCK** (`stamina.sprintDisableThreshold`) and **RECOVERY BELOW** (`stamina.lowRecoveryThreshold`), each with its current value aligned to the right, its own 0–60 STAMINA physical ruler, subdued source-reference tick, and explicit 0 STAMINA / 60 STAMINA footer. The Sprint Lock portion beyond its native **32.2 STAMINA** maximum is passive/non-editable, and Recovery Below retains its native **1–60 STAMINA** active span. The right-hand 2×2 cards are **AIM RECOVERY PENALTY** (`stamina.aimRegenCoef`, %), **RECOVERY RESTART DELAY** (`stamina.sprintRecoveryDelay`, s), **low-stamina RECOVERY BOOST** (`stamina.lowRecoveryStrength`, STAMINA/s), and **low-stamina RECOVERY DURATION** (`stamina.lowRecoveryDuration`, s). Each uses the same header/readout, horizontal source-native rail/handle and own min/max footers. Numeric highlights remain limited to values modified from their reference, titles and units stay neutral, and long titles are locally ellipsized without crossing the value column. The redundant STAMINA THRESHOLDS section subtitle is removed. At viewport width >=780 DIP, thresholds occupy a bounded left column (38% up to 340 DIP) and the four right cards share the wider remaining area; height **265 DIP**. Below 780 DIP, the threshold pair stacks above a two-column recovery grid; height **475 DIP**. The six original source ranges, steps, click/drag, keyboard and per-source reset semantics are unchanged. The separate STAMINA USAGE board is untouched.

**WATER STAMINA DRAIN:** Preserve the two independent source editors `stamina.waterMediumDrain` (native **0–2×**) and `stamina.waterStrongDrain` (native **0–3×**) with the existing **Medium <= Deep** value-based cascade. Both are now compact **105-DIP primary resource rulers** rendered through the exact `DrawCoreResourceRuler` grammar used by CORE HEALTH and CORE STAMINA: same title/value row, 19-DIP current numeric, 5-DIP filled track, 7-DIP handles, muted reference tick and 10.4-DIP native min/max labels. Readouts and endpoint units are **1x / 2x** (not “x drain” or “x x”), and only modified numeric tokens turn gold. Two equal cards start at Y=51 with a 12-DIP gap in a **171-DIP** board. No extra instruction labels, full-width controls or synthetic percentages. All native edit/reset and linked-source mapping behavior remains unchanged.

This specification supersedes historical 240/419-DIP Exhaustion and 149-DIP Water card layouts in older dated passages below; the underlying writer/preset/CFG contract is identical.

### 2026-10-11 — Core Stamina matches Core Health (current authoritative layout)

The existing `StaminaCore` family is rendered under **CORE STAMINA** (replacing the former **STAMINA & MOVEMENT** outer title and redundant **STAMINA CORE** subheading). The top two 105-DIP primary resource cards are **MAX STAMINA** (`stamina.maxSP`, native **STAMINA**) and **BASE RECOVERY** (`stamina.regeneration`, native **STAMINA/s**). They share the **exact** `DrawCoreResourceRuler` renderer also used by the Core Health MAX HEALTH / PASSIVE RECOVERY cards: the same 12-DIP heading at +15/+12, 19-DIP right-aligned current value at -15/+6, 5-DIP horizontal rail and 7-DIP handle at card top +65, muted reference marker, and 10.4-DIP native min/max labels at bottom -20. Only edited **numeric tokens** become gold; neutral units, scale ticks and baseline markers remain subdued. Native STAMINA-capacity and regeneration values/ranges/steps are not interchanged.

The new layout uses the Core Health header position (Y=21) and identical two-card geometry (Y=51, H=105, gap=12). The next line is **MOVEMENT LIMITS** (Y=173), followed by the **unchanged native vault-height vertical ruler** and **calibrated maximum carry-weight rail** (Y=191, H=175, gap=12). These lower controls retain their exact clickable landmarks, underlying `stamina.vaultMaxHeight` / `stamina.maxCarryWeight` values, drag/keyboard/reset and reference semantics. Board height is **382 DIP** (366-DIP bottom plus 16-DIP padding). There is **no duplicate heading**, no new gameplay control, no full-width slider and no change to the separate Stamina Usage panel.

All four parameters keep their original per-source mappings, preset serialization, validation, selection, reset, and CFG writer operations. This overrides older 351/336-DIP and 72-DIP StaminaCore visual descriptions without changing their gameplay audits.

Runtime reference: **STALKER 2 CFG dump 2.0.5**.

## Result

The current Stamina UI exposes **18 editable dimensions through 7 visible cards**. The Stamina Pressure card contains 12 backing dimensions: the original seven action/aiming axes plus five confirmed exhaustion/recovery axes.

- **17 confirmed** direct dimensions.
- **1 runtime-test-required** traversal control: `stamina.vaultMaxHeight`.
- **0 invalid or partial controls**.
- **21 concrete writer edit points** across the 18 editable dimensions.

The newly promoted backing axes are not separate engine-style cards. They are presented as one semantic **EXHAUSTION & low-stamina RECOVERY** block inside Stamina Pressure.

## v0.2.46 corrections

### Aiming stamina sign boundary

The previous UI exposed the engine value `StaminaRegenStateCoefs[EStateTag::Aim].Value = -0.25` directly under the label **Aiming Stamina Cost**. That mapping was technically writable but player-facing direction was inverted: a stronger penalty meant moving the slider farther negative.

v0.2.46 changes the UI contract to **Aiming Stamina Recovery Penalty**:

- UI vanilla: `25%` (`0.25`).
- UI range: `0% .. 50%` (`0.00 .. 0.50`).
- Writer operation: `negateSet`.
- Vanilla output remains exactly `-0.25` in `CoreVariables.cfg`.
- Pre-v0.2.46 presets that stored a negative Aim coefficient are migrated with `abs(value)` on load, preserving gameplay.

The UI now follows the same direction as the other stamina cost/penalty controls: farther right means a stronger stamina penalty.

### Maximum Vault Height range

`stamina.vaultMaxHeight` already declared the explicit orientation marker `ABOVE EYES = 300` and its mapping/rationale described `300 uu` as the upper tuner limit, but the actual catalog slider stopped at `260`.

v0.2.46 fixes `slider.max` to **300**. The runtime status remains `runtime_test_required`; only the inconsistent UI ceiling was corrected.

## Control verification

| Control | Parameter ID | Vanilla UI value | CFG target | Writer | Status |
|---|---|---:|---|---|---|
| Maximum Stamina | `stamina.maxSP` | 100 STAMINA | `Player.VitalParams.MaxSP` | `set` | confirmed |
| Base Stamina Recovery | `stamina.regeneration` | 5 STAMINA/s | `Player.VitalParams.RegenSP` | `set` | confirmed |
| Sprint Stamina Drain | `stamina.sprintCost` | 5.25 STAMINA | `Player.StaminaPerAction.Sprint` | `set` | confirmed |
| Aiming Stamina Recovery Penalty | `stamina.aimRegenCoef` | 25% | `StaminaRegenStateCoefs[Aim].Value` | `negateSet` -> `-0.25` | confirmed |
| Sprint Stops Below | `stamina.sprintDisableThreshold` | 16.1 STAMINA | `Player.VitalParams.StaminaDisableThresholds[0].Threshold` | `set` | confirmed |
| Sprint Restart Delay | `stamina.sprintRecoveryDelay` | 1 s | `Player.VitalParams.StaminaDisableThresholds[0].RegenerationDelay` | `set` | confirmed |
| Emergency Recovery Starts Below | `stamina.lowRecoveryThreshold` | 30 STAMINA | `AdditionalRegenStaminaMechanics...NormalSP.ConditionValue` | `set` | confirmed |
| Emergency Recovery Amount | `stamina.lowRecoveryStrength` | 10 STAMINA/s | `IncreaseStaminaPoints.ValueMin/ValueMax` | `set` x2 | confirmed |
| Emergency Recovery Duration | `stamina.lowRecoveryDuration` | 4 s | `IncreaseStaminaPoints.Duration` | `set` | confirmed |
| Jump Stamina Cost | `stamina.jumpCost` | 16 STAMINA | `Player.StaminaPerAction.Jump` | `set` | confirmed |
| Vault Stamina Cost | `stamina.vaultCost` | 12 STAMINA | `Player.StaminaPerAction.Vault` | `set` | confirmed |
| Normal Melee Cost | `stamina.meleeNormalCost` | 16 STAMINA | `Player.StaminaPerAction.MeleeNormal` | `set` | confirmed |
| Strong Melee Cost | `stamina.meleeStrongCost` | 25 STAMINA | `Player.StaminaPerAction.MeleeStrong` | `set` | confirmed |
| Buttstock Melee Cost | `stamina.butstockCost` | 12 STAMINA | `Player.StaminaPerAction.MeleeButstock` | `set` | confirmed |
| Maximum Vault Height | `stamina.vaultMaxHeight` | 130 uu | `Player.VaultingParams.MaxObstacleHeight` | `set` | runtime test required |
| Medium-Water Stamina Drain | `stamina.waterMediumDrain` | +100% | `WaterMediumStaminaPenalty.ValueMin/ValueMax` | `set` x2 | confirmed |
| Deep-Water Stamina Drain | `stamina.waterStrongDrain` | +200% | `WaterStrongStaminaPenalty.ValueMin/ValueMax` | `set` x2 | confirmed |
| Maximum Carry Weight | `stamina.maxCarryWeight` | 80 kg | `DefaultWeightParams.MaxInventoryMass` | `set` | confirmed |

## Value-order constraints

Two linked Stamina relationships remain correct because they operate on the actual CFG/gameplay values:

- `Medium-Water Stamina Drain <= Deep-Water Stamina Drain`.
- `Normal Melee Cost <= Strong Melee Cost`.

The actively moved slider keeps priority and the neighbor moves only as far as required to restore the ordering.

## Explicit NO-UI boundary

The audit does **not** promote any of the following:

- Walk stamina drain or zero-valued Walk/Run/Jogging/Crouch action-cost entries.
- Sprint, Run, Jogging, SlowRun or SprintUnderRunSpeed regeneration coefficients.
- `SlowRunThreshold`.
- Jump movement coefficients or vault search/landing geometry.
- Energy-drink tolerance/overuse mechanics.

These fields are either internal state machinery, overlap a clearer normal control, or lack a complete player-facing 1:1 semantic contract.


## 2026-10-15 dead-code cleanup — pass 1

Confirmed by source mapping: `PhysicalLimits` had no owning parameter, as Vault Height and Carry Weight already belong to `StaminaCore`. Removed the dead PhysicalLimits draw dispatch, family height/source-ID registration and helper text, plus its sole `DrawPairedSystemBoard` renderer (which also included unreachable historical Core Stamina, Water, Trader Stock and Item Weight branches). The corresponding inactive stamina comparison formatters were removed; the inactive `stamina.vaultMaxHeight` modified-state aggregate-only subscription was deleted. Active `StaminaCore` and `WaterStaminaPressure` renderers, source IDs, preset/writer mapping and controls remain unchanged. Static audits cover removal and keep live family bindings. Runtime .NET/Wine verification is still required locally.

## 2026-10-15 dead-code cleanup — pass 2 (ActionCostsRadar)

Removed unobserved `VisualStatusMessage` state, timer-backed `ShowTransient`/`Clear` notifications, and the otherwise-unused `ShortLabel` helper from `ActionCostsRadar`. Neither Usage nor Exhaustion reads or draws the status text, and native edit/reset event paths already invalidate the WPF visual. Also removed the obsolete `|` separator accent loop in `CreateText`; active stamina labels and dynamic readouts do not contain pipe separators. Existing six Movement/Melee actions, six Exhaustion parameters, the read-only Max Stamina relation, drag/keyboard/right-click/`R` behavior, native source ranges, saved presets and CFG editing remain intact. The shared `VisualStatusMessage` helper was not deleted. Static audit coverage added; local WPF compilation and Wine tests remain pending.

## 2026-10-15 dead-code cleanup — pass 3 (ActionCostsRadar binding ownership)

`ActionCostsRadar.RebindParameters` previously detached `PropertyChanged` from whichever sources `EnumerateBoundParameters()` saw at callback time. For a `Mode` transition that is the *new* group, so listeners on the old Usage/Exhaustion sources could remain attached and cause duplicate/invisible invalidations later. The control now tracks the actual subscribed `ParameterViewModel` instances, detaches that set before rebuilding its references, and attaches each current source only once. `stamina.maxSP` is still a read-only redraw dependency (capacity comparison and threshold ruler), never an editable or R-reset member of either board. Current interaction mapping, native ranges and steps, six Usage + six Exhaustion values, compact card geometry and CFG/preset output have not changed. Static audit updated; local compilation and Wine visual test still pending.

## 2026-10-15 dead-code cleanup — pass 4 (lifecycle and hidden UI sorting)

The six Usage and six Exhaustion sources still have exactly the same rendering and editing scope. `ActionCostsRadar` now cancels an active drag and drops old hit targets during `Mode`/`Parameters` rebinding; unload detaches source listeners and releases capture, and reload rebuilds the relevant subscriptions. `OnLostMouseCapture` also ends a gesture if WPF takes capture away unexpectedly. The default cursor is restored on mouse exit. Removed five unreachable `ParameterSortOrder` ranks for UI-hidden sprint/run/jogging movement-state regeneration coefficients; the underlying parameters, writer, presets and no-UI policy remain unchanged. No dimensions, styling, native value domains, saved data or CFG targets were changed. Static audit extended; .NET/Wine UI runtime verification remains pending.

## 2026-10-15 dead-code cleanup — pass 5 (native-vs-derived readout highlights)

In the Stamina Usage rows, the changed native action-cost numeric value continues to be the only gold token. The read-only `cost/current maxSP` percentage is now always neutral; before this change, a `nativeOnly` formatter branch turned the derived second numeric token gold unconditionally, even at factory values. The hover-only `FROM FULL` and continuous Sprint `POOL/COST` ratios remain unchanged mathematically, but their highlighted number uses the white text brush rather than modified-source gold. Removed the single-use `DrawSourceValue` forwarding helper and the second-token-special-case formatter state. The existing label `SPENT PER ACTION` is retained per catalog approval, without asserting a discrete Sprint activation count. No layout, 2x rail geometry, input/reset scope, native parameters, presets or CFG outputs were changed. Static audit updated; local WPF/Wine visual validation still required.
