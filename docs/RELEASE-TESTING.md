### 2026-10-16 — Top-left radiation mark spins during Build Difficulty

The existing **38-DIP top-left radiation mark** retains its original three 120°-spaced yellow blades, center dot, size, spacing and palette. Follow the connected Localization Workbench's `UI/StalkerTheme.cs` `StalkerBrandMark.Spinning` behavior: **clockwise +10° per ~33ms (~300°/second)** while the application is busy, **stationary at 0° when idle**. In TrueCustomDifficulty, the original 34×34 Canvas uses a WPF `RotateTransform` centered at (17,17) with a linear, repeating **0→360° / 1.2-second** storyboard. A `DataTrigger` bound to the existing read-only `MainViewModel.IsBuilding` begins the animation after a real build begins and a matching `StopStoryboard` resets the icon when the build completes or fails. The already asynchronous repak/retoc pipeline keeps the animation responsive; no UI timer, artificial progress estimation, non-build spinning, profile/CFG alteration or new UI control appears. Error dialogs are displayed only after `IsBuilding=False`, so the icon must not keep spinning behind the dialog.

Regression: `python tools/audit_build_progress_contract.py` checks icon shape and geometry, exact 300deg/s equivalent speed, build-only trigger, exit reset and unchanged OneWay progress bar binding. **Manual Wine QA still required:** icon stationary at startup, spinning throughout ~10-second CFG/repak/retoc/install build, smooth animation without white background, returning to the same idle orientation on success **and** on failure/no-op/vanilla build; the default WPF title-bar border and other controls must remain unchanged. Source static acceptance never substitutes for Wine visual confirmation.

### 2026-10-16 — Wine build-time white background regression

Observed in a real Wine build: the normal dark instrument grid and window background intermittently became **white** while `Build Difficulty` showed `WRITING CFG`, returning to normal after the operation. The 2026-10-16 build progress implementation disabled entire WPF `Grid` and `TabControl` subtrees through inherited `IsEnabled=False`, which can cause disabled control template re-rendering/invalidations on Wine. Preserve the normal dark painting by switching editor and footer interaction locks to `IsHitTestVisible="{Binding IsBuildIdle}"`. This keeps visuals enabled and unchanged, blocks mouse interactions, and avoids disabled visual states. A `MainWindow`-level `PreviewKeyDown` / `PreviewTextInput` guard blocks keyboard edits while `IsBuilding` is true. `BuildCommand` still refuses a concurrent invocation, Settings still checks `IsBuilding`, and window closing remains blocked until the atomic build/install finishes. The real async writer, repak/retoc phases, output, progress milestones and dialog semantics remain unchanged.

Regression: `python tools/audit_build_progress_contract.py` now asserts the correct build input lock, absence of inherited disabled styling, and keyboard guards. **Wine QA pending:** launch, build a real modified preset, ensure the original dark grid remains throughout WRITING CFG / PACKING PAK / BUILDING IOSTORE / INSTALLING, verify clicks, keys and preset selection cannot change values while busy, test the failed-tool and vanilla build paths. A static PASS cannot establish that Wine's renderer is fixed.

### 2026-10-16 — Build progress WPF startup crash regression

An actual Wine startup on the first progress-rail revision threw `InvalidOperationException: A TwoWay or OneWayToSource binding cannot work on the read-only property 'BuildProgressStep'`. This was a **WPF binding defect**, not a Vulkan/RADV, Wine keyboard-layout or packaging problem. WPF `ProgressBar.Value` defaults to `TwoWay` binding; `MainViewModel.BuildProgressStep` is deliberately public-get/private-set. Bind `Value="{Binding BuildProgressStep, Mode=OneWay}"` explicitly. Never expose a public setter merely to silence this error: the build pipeline is the only progress author. `tools/audit_build_progress_contract.py` now rejects any regression to the default binding mode. The failure occurred during window construction, before the user could start a build. Re-publish and check actual application startup under Wine **before** rechecking stage animation, final pack/install or error paths; an unexecuted source audit is not a Wine PASS.

### 2026-10-16 — Build Difficulty: LocalizationWorkbench-inspired compact activity rail

The bottom command bar now has **one compact, bounded live build-progress rail** in the available gap **between Settings on the left and the profile selector/action buttons on the right**. **Collapsed when idle** (no reserved blank space); visible only from the start of actual asynchronous CFG/package work through final install/uninstall. It reuses the matching LocalizationWorkbench `UI/StalkerTheme.cs:StalkerProgressBar` rendering cues: flat 8-DIP graphite track, 1-DIP muted #3B4038 outline, 6-DIP warm-yellow #FFD438 to #F0C419 gradient fill, and a restrained live pulse. A small muted current-stage caption sits above the track. No full-width band, permanent status banner, numeric timer, or fabricated per-process percentage.

The six **completed-stage** values (0..6) are deliberately coarse: 0 **WRITING CFG**; 1 **PACKING PAK**; 2 **BUILDING IOSTORE**; 3 **VERIFYING PACKAGE**; 4 **PUBLISHING OUTPUT**; 5 **INSTALLING**; 6 **COMPLETE**. The no-mod case skips packing, reports **CLEARING OUTPUT**, then **INSTALLING** and remains subject to the same failure/error behavior. Because repak/retoc do not expose reliable granular percent, the bar never claims a stopwatch-derived completion fraction. Long-running packing shows a subtly pulsing current stage until the next actual milestone.

The build operation runs off the WPF dispatcher using `Task.Run` with `Progress<BuildProgressUpdate>` marshalled onto the dispatcher; editing, preset selection and Settings are disabled during the operation. Concurrent builds and closing mid-packaging are prevented. The rail collapses before the outcome dialog on both success and failure. The original `ModBuildService` packaging, validation, atomic Output replacement, `ModDeploymentService` atomic install, and vanilla/uninstall semantics remain unchanged. No CFG source, source ID, slider or gameplay mapping was modified.

**Verification:** `python tools/audit_build_progress_contract.py` (also in CI and pre-release static suite) checks XAML/footer placement, state binding, milestones and no fake percentage. **Manual Wine acceptance required:** start with a real modified preset and working repak/retoc; see live label changes over ~10 seconds, smoothly pulsing gold fill, no UI freezing, controls disabled, hidden immediately on completion, and existing success/error dialogs. Repeat with missing retoc/repak, a fully vanilla no-mod build, and a second-click attempt; verify no stale rail, no duplicate execution, and proper atomic install. At minimum 1024×576 and normal 1600×900 check the footer has no text clipping, shifted buttons or overlay.

**VS Code praktična in-game checklist-a (13 testova):** [RC CFG Signal — gameplay checklist](RC-CFG-SIGNAL-GAMEPLAY-CHECKLIST.md). Checkboxovi se ručno označavaju tek posle potvrđenog efekta.

### 2026-10-15 — RC extreme-signal functional smoke (NEW; GAMEPLAY NOT YET VERIFIED)

The application embeds and seeds **one** explicit opt-in `RC - CFG Signal` v1 diagnostic preset outside the twelve factory snapshots (13 exaggerated, source-backed scalar changes; no hidden/unsupported IDs). Its full manual A/B matrix, required New Game case, expected BPatch paths, player-rank A-Life count limits and acceptance/report template are in [RC CFG Signal test](RC-CFG-SIGNAL-TEST.md). The preset is created only when missing, so local modified diagnostic copies, normal factory roles and user files are not overwritten.

Run `python tools/audit_rc_signal_preset.py` on Linux for **static validation**. On native Windows/.NET 10, run `dotnet run --project tests/RCSignalPatchContract/RCSignalPatchContract.csproj -c Release -- --out audit/rc-signal-patch-fixtures` to check all thirteen through the actual C# writer. The test is included in the Windows `all-controls-final-writer-windows` CI job and `tools/pre_release.py writer`. After actual `./publish.sh`/Wine, build a real triplet with user-supplied repak/retoc, inspect the packed CFG with read-back and perform isolated in-game trials on matching 2.0.5 source version. **Writer output, archive creation and runtime effect are three distinct PASS gates.** No source or CI claim can substitute for the in-game check.

# Pre-release test and evidence runbook

### 2026-10-15 — Complete catalog-to-writer CFG acceptance gate

Before declaring a release's source mapping complete, run `python tools/audit_final_cfg_mapping.py --out audit/final-cfg-mapping` and `python tools/audit_ui_cfg_contract.py`. On Windows, also run `dotnet run --project tests/AllControlsPatchContract/AllControlsPatchContract.csproj -c Release` and all existing `tests/*PatchContract` projects. The new suite probes actual production staging for all 235 player-editable CFG sources, not only the existence of mapping metadata. Revalidate STALKER 2 runtime behavior (especially the 51 runtime-pending sources) separately. Source review never substitutes for repak/retoc packing and live game testing. See `docs/FINAL-CFG-MAPPING-AUDIT-2026-10-15.md`.

### Consumable Effects upper-panel knobs and six live outcome bars (2026-10-11)

Run `python tools/audit_project_consistency.py` and `./publish.sh`, then open Survival > **CONSUMABLE EFFECTS** in Wine at minimum (520 DIP) and large widths. Inspect exactly three rotary editors **INSTANT STAMINA**, **WATER BUFF**, **ENERGY REGEN** in one centered bounded row (620-DIP maximum bank), **0–2x / 0.05x** each, actual gold current indicator, source-aligned tick marks and grey Vanilla **1x** reference. There must be no remaining horizontal strength sliders or the former two text-only outcome cards. Click, drag, scroll, arrow and Page/Home/End each knob, right-click reset only that CFG parameter, and press R to reset the whole ConsumableMixer. Mouse hover or selection must only highlight the graph bars controlled by that knob, leaving unrelated bars neutral.

The single **LIVE EFFECT PREVIEW** frame at Y=250 DIP has two compact columns with precisely six independently scaled READ-ONLY result bars. On default 1x: **INSTANT** Water +38%, Energy +50%, Limited Energy +100%; **SUSTAINED** Water Regen +25%, Water Cost −10%, Energy Regen +200%. At 0x all linked outcomes must become 0% and at 2x the respective rows must read +76%, +100%, +200%, +50%, −20%, +400%. Water Cost must show a negative sign and its bar fill must extend leftward; all other bars extend rightward. Positive and negative bars must not share a misleading common percentage axis. Each bar has a muted 1x Base tick. The full numeric token turns gold *only when its own source knob differs from reference*; labels and base ticks stay neutral. Never interpret these percentages as current stamina or duration.

**Duration freeze:** compare the entire `DrawConsumableMixer` section starting `// A true seconds-space editor` through the end of the method to the prior committed baseline (pre-change code); it must be byte-identical. Check five original timeline tracks for WATER / ENERGY / VODKA / BEER / MAGIC VODKA, their original seconds ruler, all three separate small WATER REGEN / ENERGY -100% COST / ENERGY -30% COST sliders, dynamic upper caps, precise source steps, Vanilla tick overlays, and the unmodified 20-DIP OVERRIDE LIMITS checkbox. The frame begins at Y=475 DIP; no old source, hit target, 270-second maximum or durations are lost. Width-based `CompactConsumableHeight` must give the frame enough scrolling height at 520/700/850/920 DIP.

Test preset save/reload and generated `GameData/EffectPrototypes.cfg` writer output for the three old strength IDs and eight old duration IDs; only intentionally edited old sources change. There are NO new effect entries and no CFG changes from the bar preview. Record actual Wine screenshots and dotnet warnings before marking release acceptance complete. Source/static checks do not substitute for publish/Wine/runtime verification.


### Seven new source-backed visual editors — mandatory pre-release QA (2026-10-11)

Run `python tools/audit_project_consistency.py`, the current Combat/Health/Survival/Economy/Loot/Environment CFG/BPatch contract tests, then `./publish.sh` and Wine. Inspect all seven widgets at the normal 920-DIP board and the minimum host width; confirm unchanged source IDs, current modified-number coloring, per-source reference ticks, no text/rail collisions, pointer hit zones, source-specific right rationale, arrow/Page/Home/End, right-click one reset and R group reset. Edit independent source values, save/reopen the same preset and compare generated source CFG/BPatch records with original mappings.

1. **Repair Price by Reputation:** exactly four connected draggable points FRIENDLY, NEUTRAL, DISLIKED, HOSTILE on shared display Y=0–3x; only Hostile may exceed 2x. Click and vertical-drag each category independently from minimum to authored max. Confirm actual values and grey four-point Base curve, 0.01x precision and existing ordered source-value cascading when a point crosses an adjacent source. Curve segments must remain read-only, not another global multiplier.
2. **Trading:** horizontally mirror YOU SELL and YOU BUY native source meters. Verify dragging right increases `economy.buyCondition`, while dragging left increases `economy.sellCondition`; visual inversion must not invert saved prices or reverse their true CFG sign. Minimum accepted condition and broken-value floor remain two independent horizontal controls with native 0–100% and 0–20% ranges.
3. **Stash Rewards:** verify three independently draggable BASIC/STANDARD/RARE vertical tier quantities (0.25–2x, 0.25 steps) on one bounded row, plus RGD-5 CHANCE, RGD-5 AMOUNT and COUPON CHANCE below. At width below 740 DIP the three bonus cards wrap without covering any tier bar; board heights exactly 382/465 DIP. The count and chance remain two separate generator fields, not a guaranteed roll.
4. **Sleep System:** verify exactly fourteen clickable minimum sleep cells **1..14**; clicking every cell sets an integer `survival.minimumSleepHours` and preserves 1-hour keyboard steps. Vanilla 7-cell underline remains muted; clicked current cell distinguishes current/modified. Drag across cells, press Home/End, right-click individual and R whole family. The eligibility track (0–100 fatigue points), wake-up HP and stamina recovery tracks still work independently.
5. **Severe Fatigue:** toggle OFF disables mutation of each of five equalizer bars, toggle ON restores independent source editing. Stamina lost and Carry lost use 0–40%; recoil 0–110%; sway 0–0.8x; dispersion 0–1x. Each editable bar is relative to its own original native range, has its own current and Base marker, not a deceptive shared numeric axis. Check min/max, source step, and reset.
6. **Radiation System:** Build-up 0–2.5x / 0.01, Recovery 0–0.10 PTS/S / 0.01, HP Damage 0–2x / 0.01 are three independently draggable gauges with their own true numeric units and reference markers. No synthetic net radiation value should appear; test a nonzero recovery value such as 0.07 PTS/S and save/reload it.
7. **Vault Height Ruler:** move the vertical cap in native **70–300 uu / 1-uu** units. Confirm actual clickable WAIST **130**, EYES **225**, ABOVE EYES **300** landmarks set those exact values; right-click restores true reference 130. Stamina Capacity, Base Recovery and Maximum Carry Weight remain separate controls, and the section measures 336 DIP without cutoff at the bottom. This gauge is not a real-world centimetres/metres display.

Static source and numeric checks can pass without proving WPF compile, Wine rendering, or in-game behavior. Record screenshots and any publish warning/error before treating these UI changes as accepted.


### All rotary controls — native tick / snap alignment (2026-10-11)

Open Combat > Your Weapons, Anomalies > Global Anomaly Damage, and Loot > Breakable Boxes > Ammo Family Weights at minimum and normal window widths. Inspect every knob: each muted graduation line must be at a **real native step value**, derived from that source's own Minimum/Maximum/TickFrequency (no generic ten-way division). Click each line's direction and confirm the live indicator snaps to that angle and readout. First and last marks must align with the native slider's min and max. The selected-difficulty grey Base notch is a separate reference: Novice Weapon Damage **1.30x** deliberately falls between the 1.25x and 1.50x native edit marks; right-click reset must restore 1.30x exactly.

Expect **8 marks** for Weapon Damage (0.25–2x, 0.25x), **11 marks** for Headshot Damage (0.5–3x, 0.25x), **9 marks** for Jam Frequency (0–2x, 0.25x), and **13 marks each** for the four Ammo Family Weights (0–3x, 0.25x). Mark count includes both endpoints. For Global Anomaly Damage (0–2.5x, 0.01x) the 47-DIP-radius ring cannot show all 251 reachable values without illegible overlap, so expect **26 legible marks exactly 0.10x apart** (major every 0.5x); the knob must nonetheless still change in **0.01x** increments by drag, mouse wheel and keyboard. Validate on an intermediate source value such as 1.47x: it must stay at 1.47x, with its handle between the 1.40x and 1.50x graduation lines. Do not round its displayed current value to a visible tick.

On the Loot row, verify tick marks stay within narrow cards and SNG last-positive-weight protection still applies to pointer drag, mouse wheel and keyboard. Verify source-specific reset (right-click) and family reset (R); changing any knob leaves unrelated family/native CFG values untouched. Check screenshots at normal and narrow widths for distinct endpoint/major/minor ticks, legible center readouts, no overlap. Run the source consistency audit and environment/combat/loot BPatch contract tests, then publish and test under Wine. Visual/runtime results require local confirmation.


### Loot / Breakable Boxes — four Ammo Family Weights rotary knobs (2026-10-11)

After `./publish.sh`, navigate to Loot > **BREAKABLE BOXES**. Check the **AMMO FAMILY WEIGHTS** row shows **exactly four rotary knobs**, centered and equal-sized, labeled **9×18 / 9×19**, **.45 ACP**, **5.45 / 5.56**, **12 GAUGE**, rather than four horizontal rails. At normal 920-DIP and narrow minimum WPF width, verify captions, centered numeric x values, muted 0–3x limits, 270° gold arc, grey selected reference tick, no clipping, and no redundant Base badges. The parent board is **715 DIP** high; verify SNG/NATO charts remain below the knobs and are fully visible when scrolling.

Edit each knob by clicking/dragging its arc clockwise and counterclockwise, then by wheel and arrow keys. All four edit native `loot.ammoBoxWeight918/045/545/012` independently at **0–3x in 0.25x increments**. Right-click resets only that weight to 1x; `R` resets the six actual editor members. The two original horizontal **AMMO YIELD / GRENADE YIELD** editors and the read-only **AMMO YIELD PREVIEW** (eight exact caliber ranges) must not be converted, and modifying a weight must not change those round counts.

Confirm the **SNG/NATO POOL** bars and permanent caliber-color/percent legends update live from native weight sources, and that their charts are not draggable. Set the three SNG-affecting families (`918`, `545`, `012`) to zero one by one while `.45 ACP` remains nonzero. The last SNG weight MUST remain at least **0.25x**: test both pointer-drag and mouse-wheel decrement on the last surviving knob; the guard must not rewrite a different family. Confirm the same guard under keyboard input. Zeroing only `.45 ACP` remains valid; no synthetic ammo families appear.

Finally save/reload all four different weights, export BPatch, confirm only the original `TCD_DestructibleStash_AmmoSNG`/`TCD_DestructibleStash_AmmoNATO` ItemGenerator PossibleItems Weight entries are changed; the original eight yield MinCount/MaxCount fields are untouched unless Ammo Yield was also edited. Run `python tools/audit_project_consistency.py` and the relevant Loot writer tests before release. Build, Wine UI and actual-game acceptance are pending.


### Global Anomaly Damage single rotary knob — acceptance (2026-10-11)

In Anomalies, verify **GLOBAL ANOMALY DAMAGE** shows exactly one centered 270° **OVERALL DAMAGE MULTIPLIER** knob, not the previous long horizontal slider. It must use the existing Your Weapons dark face, yellow arc/notch, muted source reference tick, center current multiplier, and neutral 0x/2.5x endpoints. The inner card is 194×181 DIP, frame 242 DIP. Check standard-width and minimum-width WPF/Wine layouts for centering and absence of clipping.

The only backing parameter is `environment.anomalyDamage`, 0–2.5x at 0.01x steps. For each vanilla difficulty confirm the grey reference marker matches **Novice 0.30x, Stalker 1x, Veteran 1.45x, Master 1.75x**. Test clockwise and counterclockwise knob clicks/drags across minimum, mid and maximum, the 90° bottom gap clamping, scroll wheel at 0.01x, arrows, Page Up/Down, Home/End, right-click single reset, and panel R reset. Clicking the center selects the source without changing it. Only changed current values get accent gold; baseline tick remains grey. The right rationale should still describe the global setting.

Confirm a Global Anomaly Damage change updates derived effective damage in the eleven separate Individual Anomaly Damage readouts, while **none** of their individual native source values is rewritten. Save/reload a nondefault global multiplier in a preset, export BPatch, and inspect only the existing `GameData/DifficultyPrototypes.cfg` `EnvironmentDifficulty.Anomaly_Damage` route for this knob; there must be no new synthetic CFG or extra applied multiplier. Run `python tools/audit_project_consistency.py` and the environment writer contract, then do the local publish/Wine and game check. This is a required follow-up; success is not yet claimed.


### Your Weapons 3-knob WPF acceptance (2026-10-11)

After a clean `./publish.sh`, open Combat > **YOUR WEAPONS** on Wine and confirm exactly three independent **rotary controls** in one compact centered row: WEAPON DAMAGE, HEADSHOT DAMAGE, JAM FREQUENCY; there must be no horizontal weapon sliders or stale inactive lanes. Confirm the dark face, muted 270° rail, gold current arc/notch, small grey actual Base tick, current multiplier in the center, and original neutral native bounds beneath. The whole group must stay within 620 DIP even in a wide panel and fit at the 520-DIP measured minimum. Expect a 242-DIP frame; check no clipping/overlap at both window widths.

Native ranges and 0.25x tick: WEAPON DAMAGE 0.25–2x; HEADSHOT DAMAGE 0.5–3x; JAM FREQUENCY 0–2x. For each: (1) click near the angular minimum, middle and maximum and confirm correct snapped values; (2) drag clockwise and counterclockwise through the ring, confirming the bottom dead arc clamps to the nearer endpoint; (3) center-click must select without changing a number; (4) mouse wheel changes only that knob in 0.25x steps; (5) focus it and verify arrows, Page Up/Down and Home/End, without exceeding range; (6) right-click restores only that source; R resets all three. Confirm selection updates the source-specific right pane and hover only changes the live notch to white (never the muted Base marker). Only source-modified centered readouts become gold.

Change the selected vanilla difficulty and inspect the **grey reference marker** for each native source (for Weapon Damage Novice 1.30x versus Veteran/Master 0.75x; Headshot Damage Master 2x; Jam Frequency Master 1.5x). Reset to reference must preserve non-step-aligned authored values such as Novice 1.30x rather than inventing 1.25x. Save a preset with three distinct modified knob values, reload and re-export the BPatch; confirm original `EnvironmentDifficulty.Weapon_BaseDamage`, `NPCCombatDifficulty.PlayerWeapon_HeadshotMultiplier`, and `NPCCombatDifficulty.Weapon_JammingMultiplier` targets are still written independently, with no extra synthetic dial field. Run `python tools/audit_project_consistency.py` plus existing CombatPatchContract as part of the pre-release gate. WPF/Wine visual and game runtime outcomes are still unverified here.


### Four independent RAD bars including Beer/Vodka — mandatory Health/Survival acceptance

Verify **four** teal radiation-removal sliders in the compact **MEDICAL ITEMS** right bank, in order Scientific Medkit, Anti-Rad, Beer, Vodka, with separate 0–200 RAD Y-axis and white-on-hover horizontal cap. All four must fit in the same row as the four HEAL/BLEED pairs; check standard 920-DIP board and required 820-DIP narrow minimum, including legibility of numbers, names, axes and individual hit targets. Vanilla RAD quantities are **60 / 100 / 10 / 50**; per-source maxima **120 / 200 / 20 / 100**; all native edit steps **5 RAD**. Confirm clicking/dragging Beer does not adjust Vodka, and vice versa. Food Healing and healing/bleeding controls are unaffected; `R` resets all source controls together. Right-click resets only selected bar.

Generate BPatch independently with Beer=15, Vodka untouched; confirm only `BeerAntirad1` ValueMin and ValueMax are written as `-15`. Repeat with Vodka=55, Beer untouched; only `VodkaAntirad3` ValueMin/ValueMax should be `-55`. With both changed, expect independent non-overlapping source nodes. Load a legacy preset containing `survival.alcoholRadiationRemoval: 1.25` but no new keys: expect **Beer 12.5 RAD and Vodka 62.5 RAD** (preserving old gameplay), while any explicitly present new key wins for that drink. Save/reload must persist the new IDs. Confirm the old shared alcohol multiplier track/readout is no longer present in Consumable Effects, and the new HealthStamina / Survival source counts and A3 audits pass. The game/Wine UI behavior requires local acceptance; CFG direct source provenance is confirmed, not simulated.

### Medical Items unified HEAL / BLEED / RAD horizontal layout acceptance

At a standard 920-DIP board, verify **exactly one MEDICAL ITEMS frame**: four left item pairs, gold HEAL bar + matching gold HEAL caption; copper BLEED bar + copper caption; two narrower teal radiation-removal vertical controls grouped **to their right** without wrapping. Scientific Medkit appears both as one HEAL/BLEED item cluster and as its **independent radiation-removal source** on the right. Verify three distinguishable vertical Y-axes and horizontal tick values: left **HP 0–200**, between groups **PTS 0–100**, far right **RAD 0–200**. No duplicate horizontal Radiation Treatment card should appear. Food Healing checkbox stays at top right.

Check initial RAD values: **Scientific Medkit 60 RAD** with editable ceiling **120 RAD**, **Anti-Rad 100 RAD** with ceiling **200 RAD**; both have 5-RAD increments. At narrow board widths, inspect captions and values for legibility and overlaps. Verify direct bar dragging, subtle white hover cap tracking its actual height, right-click individual reset, keyboard edits, and R reset for all ten numeric and one toggle controls. Changing Sci RAD or Anti-Rad should mark Medical Items modified without altering HEAL/BLEED or the other RAD source. Validate that Save/Load Preset and BPatch retain all original CFG targets and `negateSet` signed semantics. A3 audit and HealthStaminaPatchContract check source aggregation, source maxima and UI wiring; final Wine/actual-game behavior remains to be verified.

### Medical Items colored vertical HP/PTS axes

In Health > MEDICAL ITEMS, verify **HP** is written vertically in healing gold on the **left**, and **PTS** vertically in bleeding copper on the **right**. Both captions should be centered along the height of their respective scales, with 0–200 HP and 0–100 PTS tick numbers still horizontal, muted and legible. At narrow and normal window widths, ensure no collision with numeric ticks, vertical bars, food checkbox or outer frame. All four HEAL/BLEED pairs, independent editing, native maximums, hover caps and CFG output must remain unchanged. The static audit enforces the code geometry, colors and rotation; local Wine screenshot acceptance remains pending.

### Medical Items final polish acceptance — spacing, reference contrast, Food Healing

At both the normal 920-DIP-wide board and the narrow 560-DIP minimum, verify that HEAL and BLEED bars now sit closer together **within each item pair**, without touching, without merging click/drag zones, and without colliding HP/PTS captions. Ensure distinct pair groups remain separated. The native maximum guide must be visibly quieter than the live gold/copper cap; the muted reference tick remains visible but unobtrusive. Verify Food Healing's OFF-state 20-DIP checkbox outline is clearly visible against the dark board, brightens on hover, toggles on click/Space when appropriate, and shows the original checkmark when ON. All four pairs' mouse drag, white hover cap, native 5-PTS step, keyboard adjustment, right-click reset, `R` group reset and independent CFG writes remain unchanged. Static A3 source test locks the new appearance; final Wine screenshot review is still required.

### Medical Items paired columns — visual, interaction and source regression

Build the current `main` commit, open Health, confirm exactly **one MEDICAL ITEMS frame** (no separate BLEEDING TREATMENT card) containing **four side-by-side item pairs in one row**, with left gold HEAL + right narrower copper BLEED for Bandage, Medkit, Army Medkit, Scientific Medkit. The Food Healing toggle remains at top right. Each cluster has separate HP and PTS numbers and compact HEAL/BLEED captions. Check both Y rulers (left 0–200 HP; right 0–100 PTS), correct current vanilla values **HEAL 20/70/85/100 HP**, **BLEED 100/15/35/25 PTS**, and per-source native ceilings **HEAL 40/140/170/200 HP**, **BLEED 100/30/70/50 PTS**. Values for both types must keep **5-point ticks**. Hover and drag each bar individually: only its own horizontal cap responds, changing white-on-hover and staying on the actual current height; moving the HEAL bar must not move BLEED and vice versa. Right-click each resets only that member; focused arrow keys change the selected member; `R` resets all eight and the Food Healing switch. Verify Save/Load Preset preserves independent values and that a change to a BLEED sub-bar marks the one Medical Items group modified. Check generated CFG for the exact original eight independent source routes (bleeding retains `negateSet`). The static A3 audit rejects revived standalone bleeding family or incorrect visual membership. HealthStaminaPatchContract exercises real ViewModel visibility, modification and all direct writer fields. WPF/Wine graphical and UE runtime approval remain pending.

### Bleeding Treatment 100-PTS axis and Bandage 1x ceiling (Health/Stamina acceptance)

Verify that Bleeding Treatment Y ticks show **0, 25, 50, 75, 100 PTS**; an unchanged Bandage at **100 PTS** reaches the chart top. Bandage cannot exceed **100 PTS / 1x**, including mouse dragging, keyboard, reference snapping and import of an older 200-PTS preset. Medkit / Army / Scientific native maxima remain **30 / 70 / 50 PTS**, with all sources retaining **5-PTS** steps. Check that white-on-hover horizontal cap stays aligned to each active bar top, and muted native max/reference ticks remain visible. The positive source magnitude still maps to the original `BandageBleeding4.ValueMin|ValueMax` **negative** CFG operation and must not generate extra fields. The HealthStaminaPatchContract includes old-preset clamping and true one-times upper bound; A3 source audit checks the axis and caps. Local Wine and UE runtime acceptance remain required.

### Medical Healing / Bleeding Treatment vertical bar hover acceptance

Test three **MEDKIT HEALING** columns and all four **BLEEDING TREATMENT** columns under Wine: default current values show a short gold horizontal cap aligned to each bar's actual top at current HP/PTS; moving over a column turns **only that cap white with a restrained horizontal glow**, never an unrelated round ring. Drag and verify the highlighted cap tracks the correct source-value height, stays inside the genuine native range and returns to gold when pointer leaves. Gray original-value and native-max lines remain muted; changed numeric values still use the usual yellow highlight. Check Bandage's separate horizontal healing slider still has its standard round hover marker. A3 static audit also checks the shared bar cap, both production renderer calls and the narrow suppression of Bleeding Treatment generic circular overlay. Manual Wine visual acceptance remains pending.

### Hunger Survival Profile LOW hover acceptance

Move the mouse over the LOW handle in the Hunger Survival Profile: **only the actual LOW handle at 0%** may show a golden hover halo; no ghost halo may appear at the plot center around −50%. Move away to confirm the halo disappears. Drag and arrow-edit LOW, then verify MEDIUM/SEVERE and the other compact tracks still work normally. The static consistency audit now guards both the legitimate native LOW halo call and suppression of the duplicate generic map-center overlay. Local Wine visual acceptance is still required.

### Mutant Properties clickable reference markers (Combat / Wine manual acceptance)

With the embedded `MUTANT HIT DAMAGE` slider visible, verify all four labels are **NOVICE / STALKER / VETERAN / MASTER** (no one-letter abbreviations). Hover each caption and corresponding dot: only that marker turns white and the pointer becomes a hand; moving away restores muted colors. Click each caption and each dot: the live slider must jump to the exact authored value **0.35x / 1.00x / 1.35x / 1.50x**, including the values outside the regular 0.10x drag grid. Verify no mouse drag starts on a marker click, normal rail dragging still works, right-clicking the markers and rail resets to the current Base Preset, and a modified value propagates through Save Preset and the existing Combat BPatch writer. The CombatPatchContract suite now verifies the exact four marker values and the production ViewModel snap method; WPF hit targeting and hover rendering still require the local Wine check.

### C3 duration stepping and parent-cap regression extension

In BOTH Override OFF and ON, verify Water Regen <= Water main and both Energy Cost effect durations <= Energy main, including after reducing the parent and after toggling override. With ON, parents may reach 270s but secondary effects must stop at their *current* parent seconds, not 270s. Test dragging and keyboard on all eight duration sources: OFF requires 1s increments, ON 5s increments (never 0.5s). Test that ViewModel CurrentNumber assignments obey these steps too, not just mouse snapping, and generated CFG retains source-exact targets. Local WPF/Wine and game-runtime acceptance remains pending.

### Drink Duration step manual acceptance (C3 extension)

With Override OFF, drag/click and arrow-step all five main and three secondary durations in 1s increments. With Override ON, repeat in 5s increments. Page Up/Down must move ten steps; verify independent native and extended caps, live child bounds in OFF, no value changes solely from switching ON, and no additional CFG targets. Local Wine UI and runtime verification pending.

**Canonical checklist for True Custom Difficulty before each release candidate.**
Run on the **exact Git commit** to be published. A static PASS is not
gameplay verification. Do not issue v1.0 without explicit approval.

## Test inventory

| ID | Command / suite | Host | Verified scope |
| --- | --- | --- | --- |
| A1 | `python tools/audit_combat_contract.py` | Linux / Windows | 39 Combat settings, 68 direct routes, special-family mappings |
| A2 | `python tools/audit_ui_cfg_contract.py` | Linux / Windows | 245 catalog settings, 232 effective controls, 152 visible + 93 hidden, including registered nine-actor unique weapon shuffle writer |
| A3 | `python tools/audit_project_consistency.py` | Linux / Windows | Live catalog, manifest and UI source consistency; historical `[NOT RUN]` is not PASS |
| A4 | `python3 tools/audit_rationale_ui.py` | Linux / Windows | Global right-side WHAT IT DOES vs CONTROLS separation, 33 family guides and muted-footer ordering (static, not Wine proof) |
| A5 | `python3 tools/audit_master_dependency_ui.py` | Linux / Windows | Master starvation anchor respects OFF; five child controls inactive but still visible; mouse/keyboard safety and regression presence (source-level) |
| B1 | `python tools/audit_cfg_dump_provenance.py --base-zip BASE --dlc1-zip DLC1` | Linux / Windows | Actual source CFG filenames and 16 Mutant Health MaxHP fields |
| B2 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json --strict` | Linux / Windows | Every 1,224 direct source/CFG field check (strict) |
| B3 | Same as B2 plus `--parameter-prefix combat.` | Linux / Windows | 68 direct Combat source checks (strict) |
| C1 | `dotnet run --project tests/CombatPatchContract/CombatPatchContract.csproj -c Release -- --out audit/combat-patch-fixtures` | Windows / .NET 10 | 84 **real production writer** Master/Novice/Stalker/Veteran/PSY BPatch scenarios, including all 11 quarter-rate ticks for eight mutant abilities, 1.75× and exact 3× BPatch endpoints, and legacy clamping |
| B5 | `python tools/audit_health_stamina_source.py --base-zip BASE --dlc1-zip DLC1` | Linux / Windows | 69 Health/Stamina original source fields, including Player→[0] inheritance |
| C2 | `dotnet run --project tests/HealthStaminaPatchContract/HealthStaminaPatchContract.csproj -c Release -- --out audit/health-stamina-patch-fixtures` | Windows / .NET 10 | 46 writer Health/Stamina BPatch writer cases |
| B6 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix survival. --out REPORT.json --strict` | Linux / Windows | All 139 Survival original CFG source points (requires matching original ZIPs) |
| C3 | `dotnet run --project tests/SurvivalPatchContract/SurvivalPatchContract.csproj -c Release -- --out audit/survival-patch-fixtures` | Windows / .NET 10 | 87 Survival writer scenarios including Starvation visual-anchor OFF/ON, disabled child output gates, linked cascades and parent toggle round-trips |
| B8 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix equipment. --out REPORT.json --strict` | Linux / Windows | 113 active Equipment source points (101 base, 12 DLC1; inherited source reported explicitly) |
| C5 | `dotnet run --project tests/EquipmentPatchContract/EquipmentPatchContract.csproj -c Release -- --out audit/equipment-patch-fixtures` | Windows / .NET 10 | 32 real ViewModel-to-BPatch scenarios: four difficulties, edge cases, combined axes and DLC1 conditional routes |
| B9 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix environment. --out REPORT.json --strict` | Linux / Windows | 42 Environment raw-value source paths, 4 difficulty and 38 effects |
| C6 | `dotnet run --project tests/EnvironmentPatchContract/EnvironmentPatchContract.csproj -c Release -- --out audit/environment-patch-fixtures` | Windows / .NET 10 | 90 live Environment ViewModel-to-BPatch writer scenarios |
| B10 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix alife. --out REPORT.json --strict` | Linux / Windows | 64 original A-Life Director MaxCount raw-value targets |
| B11 | `python tools/audit_alife_director_source.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json` | Linux / Windows | Four exact Rank identities, 64 AgentType/MaxCount slots, index-to-species identity, two original zero ceilings |
| C7 | `dotnet run --project tests/ALifePatchContract/ALifePatchContract.csproj -c Release -- --out audit/alife-patch-fixtures` | Windows / .NET 10 | 94 real WPF ViewModel-to-Director BPatch scenarios |
| B12 | `python tools/audit_cfg_source_values.py --base-zip BASE --dlc1-zip DLC1 --parameter-prefix loot. --out REPORT.json --strict` | Linux / Windows | 320 Loot direct source fields, including verified physical rare-stash template inheritance |
| B13 | `python tools/audit_loot_direct_source.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json` | Linux / Windows | 14 mutant one-part drops, 121 NPC weapon rows in 33 generators, Gamepass coupon progression identities |
| C8 | `dotnet run --project tests/LootDirectPatchContract/LootDirectPatchContract.csproj -c Release -- --out audit/loot-direct-patch-fixtures` | Windows / .NET 10 | 117 real direct Loot BPatch writer scenarios across four difficulties and mixed controls |
| D1 | `chmod +x publish.sh && ./publish.sh` | Linux / .NET 10 | Published executable and matching `BUILD-INFO.txt` source receipt |
| D2 | Wine UI + in-game manual test | Linux / game | Visual design lock, preset round-trip, PAK/UTOC/UCAS build, actual game effects |


**B4 — original Combat reference inheritance and special arrays:**
`python tools/audit_combat_source_structures.py --base-zip BASE --dlc1-zip DLC1 --out REPORT.json`.
Checks 68 Combat direct source routes, separately identifying two
verified Medium-to-Empty inherited source fields, all 14 Master
agents / 66 tags, and both PSY effects. Game BPatch integration
remains a manual acceptance gate. The source runner now executes
B1 through B4 with genuine base/DLC1 ZIPs.

### Global Drink Duration Override test (C3, required)

The 94-case Survival writer contract now includes one real **global range
override** end-to-end case. Test both modes on all eight independently writable
Duration sources: OFF 90/135/60/60/270s per-drink limits, ON identical 270s
maxima on all sources, **all eight set to 180 seconds simultaneously** with
actual production WPF ViewModel → CFG writer BPatch equality, and OFF restoration
with minimal clamping. The checkbox is metadata and must never become a new
CFG edit. Preset JSON round-trips the `drinkDurationOverride` key.

Wine manual follow-up: at narrow and wide windows, verify the compact universal
checkbox and ruler axis labels do not overlap, Space toggles a focused checkbox,
right-click resets it, R resets the entire group, each independent native
duration has full 270s reach only with Override ON, and loading a saved
high-duration preset sets the mode before its values. Runtime UE acceptance is
still separate.

## Single repeatable runner

`tools/pre_release.py` invokes the registered subprocesses, preserves raw
logs, and saves `summary.json` under ignored
`audit/pre-release/<UTC>-<SHA>/` with exact Git SHA, command, platform and
per-step **PASS / FAIL / NOT RUN** statuses. Source runs additionally
save raw-value audit JSON; Windows runs save generated CFG fixtures.

```bash
# Inventory; does not execute any test.
python tools/pre_release.py list

# A1 / A2 / A3: standard Linux/Windows static check.
python tools/pre_release.py static

# B1 / B2 / B3: actual matching decoded CFG archives required.
python tools/pre_release.py source \
  --base-zip "/path/to/STALKER2-GameData-CFG(4).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(4).zip"
```

On **Windows + .NET 10**:

```powershell
# C1 (82 Combat cases) + C2 (46 Health/Stamina cases).
python tools/pre_release.py writer

# A1/A2/A3 + B1/B2/B3/B4/B5 + C1.
python tools/pre_release.py all --base-zip "C:\path\to\GameData-CFG.zip" --dlc1-zip "C:\path\to\DLC1-CFG.zip"
```

On **Linux**, `all` runs static + source but reports **INCOMPLETE**
(exit code **2**) because C1 and C2 require Windows WPF. Verify C1 using the
matching GitHub CI run. Exit codes: `0` requested automated tests passed;
`1` a test failed; `2` Windows-required coverage skipped.
**A zero exit code never indicates S.T.A.L.K.E.R. 2 gameplay acceptance.**

## GitHub CI at the exact release commit

[Static source and CFG contracts](https://github.com/markoca/TrueCustomDifficulty/actions/workflows/static-audit.yml)
executes A1–A3 on Ubuntu and C1 on Windows automatically for `main`,
pull requests and manual dispatch.

```bash
# Optional: gh CLI requires connected GitHub credentials.
gh run list --repo markoca/TrueCustomDifficulty --workflow static-audit.yml --limit 5
gh run view RUN_ID --repo markoca/TrueCustomDifficulty
```

Require both `audit` and `combat-writer-windows` jobs green for the
**intended SHA**, not some older commit. Save/download the CI artifact
`combat-patch-contract` containing generated CFG patches.

## Local Linux publish and manual runtime acceptance

```bash
cd /home/marko/s2tools/TrueCustomDifficulty/

git pull
git rev-parse HEAD

python tools/pre_release.py static

chmod +x publish.sh
./publish.sh
cat publish/win-x64/BUILD-INFO.txt

WINEPREFIX="$HOME/.wine-s2" \
WINEDEBUG=-all \
wine "publish/win-x64/TrueCustomDifficulty.exe"
```

Verify UI against `docs/UI-DESIGN-LOCK.md` and
`docs/VISUAL-CONTROL-SYSTEM.md`: all tabs, values and reference markers,
changed-number highlighting, hit targets, resets, rationale, dependencies,
base presets and preset round-trips. With repak/retoc installed, build a
mod and check actual `.pak/.utoc/.ucas` output, installation and load order.
Run S.T.A.L.K.E.R. 2; neither the Python tests nor a successful publish
does this.

### Water / Energy live short-duration regression (required)

The current Survival writer suite checks three new independent CFG parameter IDs, ensures the main Water/Energy duration routes never own the same source prototype as their children, checks dynamic child maxima and cascade when the parent duration shrinks, and validates a real combined BPatch for all five edits. Required manual UI checks: Water 5s → up to current 30s main, Energy −100% 5s and −30% 15s → up to current 45s main, increasing parent does not automatically expand the child, shrinking parent clamps it, reference resets and old preset migration work. At minimum main, children still show their authored vanilla minimum, and no non-editable tail responds to input. Source archive checks must verify unchanged physical CFG targets; engine stacking of SPDrain modifiers is not yet in-game verified.

### Consumables: original Water / Energy Drink source audit (automatable)

Use the exact game CFG ZIP supplied for the target game version. Run
`python3 tools/audit_consumable_effect_source.py --base-zip "/path/to/STALKER2-GameData-CFG.zip" --out /tmp/tcd-consumable-effects.json`
to validate both `EffectPrototypes.cfg` and `ItemPrototypes.cfg`,
the native -30% / 15s and -100% / 5s SPDrain structures, inheritance,
the eight action types, Regular/Limited/Weird Kettle item membership and
the production duration-writer baseline. The same check is included in
`python3 tools/pre_release.py source --base-zip BASE.zip --dlc1-zip DLC1.zip`.
Source PASS does not establish how concurrent effects combine in-game.
See [source audit](ENERGY-DRINK-EFFECT-SOURCE-AUDIT-2026-10-10.md).

### Combat in-game matrix (manual, never automatically PASS)

Refer to [Combat Phase 3/4](DEEP-CFG-AUDIT-PHASE3-COMBAT-2026-10-09.md):

- [ ] Veteran Human Attack Frequency: source inverse cooldown and actual NPC cadence.
- [ ] Veteran Bloodsucker/Burer Health: distinct source variants and real HP.
- [ ] Master special cooldown: only selected family changes and ability cadence follows.
- [ ] Veteran special cooldown: inserted 14-agent array loads without side effects.
- [ ] Master/Veteran PSY: both effect SIDs, delay, count, bleed and durability tested.
- [ ] Legacy preset with hidden repeated-hit/PSY values: generated output is intentional.

The C1 tests only validate produced BPatch CFG text, **not Unreal Engine
merge semantics** or quest-specific side effects. Preserve
`runtime_test_required` until in-game behavior is verified.

## Release sign-off (copy and fill for every tag)

| Evidence | Status |
| --- | --- |
| Candidate SHA, 0.x version and `git status --short` | NOT RECORDED |
| A1–A3 `summary.json` static PASS at that SHA | NOT RUN |
| B1–B5 original matching decoded archive evidence | NOT RUN |
| C1 Windows CI 69/69 + C2 46/46 writer at that SHA | NOT RUN |
| D1 publish and `BUILD-INFO.txt` SHA match | NOT RUN |
| D2 Wine UI and preset smoke | NOT RUN |
| D2 PAK/UTOC/UCAS build, mod load and in-game test | NOT RUN |
| Combat and other runtime-pending gameplay acceptance | NOT RUN |
| Decision: READY / BLOCKED / approved exception | BLOCKED |

Do not release merely because the automated checks are green. Retain
the evidence folder, completed checklist and any approved exceptions
together with the candidate commit SHA.

### Phase 5 runtime contract coverage

The Windows production-writer job is extended to **82 separate executable
scenarios**: 11 special-array PSY/mutant cases, 52 direct difficulty
control cases and 14 Mutant Health family cases (16 nested MaxHP fields).
The current bound ViewModels and real `CfgPatchWriterService` are used.
These scenarios prove output structure/value and no collateral edits,
not independent correctness of every vanilla source value or in-game load.
The original GameData/DLC1 ZIP checks remain an independent B1–B5 gate.

### Phase 7 runtime acceptance: inherited mutant section

The Windows writer job now tests **82 scenarios**, including five importable
JSON presets for Stalker/Medium (inherited from Empty) and Veteran/Hard
(directly authored). Use
[Combat runtime test kit](../tests/CombatRuntimeAcceptance/README.md)
for isolated in-game tests. Even a green 82/82 C# result is **not**
gameplay evidence: game loading, Unreal inheritance/merge behavior and
mod packaging remain manual gates.

### Phase 8 source and writer tests

The fifth archive source gate, `B5`, confirms all **69 original
Health/Stamina direct writer field paths** and explicitly reports
`stamina.vaultMaxHeight` as inherited from
`Player : struct.begin {refkey=[0]}`. The Windows `C2` writer job
runs **46 independent production ViewModel→BPatch scenarios**,
with separate generated CFG evidence. Both CI jobs must be green
on the **same release commit**. Never confuse original source
ancestry with in-game BPatch merging.

See [Phase 8 detailed audit](DEEP-CFG-AUDIT-PHASE8-HEALTH-STAMINA-2026-10-09.md).

The five catalog-only Stamina internal speed-state parameters are intentionally absent from the actual application ViewModel and are **not** counted among C2 active writer scenarios. See Phase 8 for their precise IDs.

C2 now includes five Master original-mechanics full-list replacement cases in addition to its 41 live Health/Stamina scalar/toggle tests. The five internal-only stamina CFG definitions remain non-VM and are not counted as writer inputs.

### Phase 9: Survival real-writer validation

The `survival-writer-windows` CI job executes the real WPF ViewModel and production CFG writer for **53 live Survival parameters + 6 difficulty variants (69 cases (59 direct + 7 baseline/gating + 3 combined))**. All **139 direct source fields** are covered separately by strict original ZIP value comparison (B6) during `pre_release.py source`. One source-mapped alcohol threshold remains catalog-only, not an app control. Game loading/effects stay NOT RUN. See [Phase 9](DEEP-CFG-AUDIT-PHASE9-SURVIVAL-2026-10-09.md).

### Phase 9 original decoded Survival evidence (2026-10-09)

Using the matching uploaded original CFG ZIPs, all **139/139 Survival direct source field identities and literal raw values** matched, with no refkey inheritance required. The immutable source ZIP SHA-256 fingerprints, counts by all 54 IDs and limitations are recorded in [Survival source evidence](evidence/SURVIVAL-ORIGINAL-CFG-2026-10-09.json). The same source comparison is automatically repeated by the B6 gate when matching archives are provided to `pre_release.py source`. This is **not** a gameplay acceptance result.

### Phase 10 Economy (2026-10-09)

A strict original-source Economy prefix gate audits **409 direct CFG fields**, while Windows `economy-writer-windows` runs **34 real application-to-BPatch scenarios** including 68 base trader profile minimum-condition cases. Source ZIPs are required for the independent source gate, and installed-DLC plus game-load evidence remain NOT RUN. See [Phase 10](DEEP-CFG-AUDIT-PHASE10-ECONOMY-2026-10-09.md).

### Phase 11: Equipment (2026-10-09)

The four current Equipment controls map to **113 direct writer fields**:
4 difficulty-based armor toughness values, 52 player-weapon wear entries (41 base /
11 DLC1), 34 base ammo weights and 23 consumable weights (22 base / 1 DLC1).
The 41 base player weapons include one authored zero,
`UA_GLaunch_Player.DurabilityDamagePerShot=0.0`; the
`scaleNonZero` writer must preserve this value without emitting an unrelated leaf.

Windows `equipment-writer-windows` runs **32 real production scenarios**, including
vanilla no-op across all four difficulties, each control separately, 0x and 2x,
conditional DLC1 output and combined-axis/shared-file cases. This suite checks
exact paths, {bpatch} flags, values and no collateral leaf writes.
The independent **B8** source gate checks exact original ZIP content and
reports inherited {refkey} resolution separately. Both gates are required for
source confidence; neither is in-game acceptance. See
[Phase 11](DEEP-CFG-AUDIT-PHASE11-EQUIPMENT-2026-10-09.md).

### Phase 11 original Equipment archive evidence (2026-10-09)

The two uploaded original decoded CFG archives were checked independently
against the full 113-point current Equipment writer mapping: **103 directly
authored exact values + 10 explicit physically traced refkey values = 113/113**.
Three quest/special consumables use `{refurl=ConsumablePrototypes.cfg;refkey=...}`;
the required parents and original Weight fields exist in the runtime dump's
consolidated `GameData/ItemPrototypes.cfg`. The source validator's specifically
allowlisted flattened-refurl resolver and CI regression cover that layout; all
other unknown refurls remain findings. See
[original archive evidence](evidence/EQUIPMENT-ORIGINAL-CFG-2026-10-09.json)
and [Phase 11 details](DEEP-CFG-AUDIT-PHASE11-EQUIPMENT-2026-10-09.md).
Windows 32/32 production Equipment writer scenarios were PASS at 0ab7938.
**This does not verify Unreal BPatch interpretation or in-game effects.**

### Phase 12: Environment / Anomalies original source and BPatch contracts (2026-10-09)

All **17** live Environment backing source IDs are mapped to **42** original
base-game CFG edit points. The supplied original ZIPs confirm **42/42 physically
authored raw-value matches**, no inherited/missing/mismatched fields.
See [Phase 12](DEEP-CFG-AUDIT-PHASE12-ENVIRONMENT-2026-10-09.md) and
[original archive SHA-256 evidence](evidence/ENVIRONMENT-ORIGINAL-CFG-2026-10-09.json).
The Windows `environment-writer-windows` job independently executes **90**
production ViewModel-to-BPatch scenarios. Both checks are pre-release gates;
they are NOT evidence of Unreal runtime merge or gameplay effectiveness.

### Phase 13: A-Life Director rank/agent original source and writer (2026-10-10)

The current A-Life catalog maps **19 source IDs / 64 direct MaxCount
fields** into one base-game Director prototype. **18 inputs** are loaded in
the normal ViewModel; `alife.mutantSpawn.generic` is catalogued for
source auditing only, and must not become an in-game general mutant cap.
The original decoded ZIP source was independently checked: **64/64 exact
MaxCount values, 64/64 physical AgentType identities, 4/4 rank
identities**, two intentionally zero Newbie predator counts. No differences
or inheritance were required. See [Phase 13 detail](DEEP-CFG-AUDIT-PHASE13-ALIFE-2026-10-10.md)
and [original file SHA-256 and 16-slot matrix](evidence/ALIFE-DIRECTOR-ORIGINAL-CFG-2026-10-10.json).

`pre_release.py source` runs both B10 direct-value and B11 structural
AgentType / rank consistency checks. `alife-writer-windows` runs **94**
production WPF ViewModel to BPatch scenarios (C7). **Verified C7 PASS 94/94** at commit `bb2c08bffba8b9afe5827fb8d1eb282e602b6971`, GitHub Actions run `37997251466`. Source verification
and writer verification do not prove Unreal merge semantics or the
actual A-Life world/encounter results.

### Phase 14A: Direct Loot original source and production writer (2026-10-10)

Of the **40 current Loot parameters**, only **16** have **320 direct
CFG source points**: 14 mutant drop chances, 64 rare-stash
coupon assignments and 242 NPC weapon condition bounds.
All 320 original effective values match the uploaded decoded ZIP:
**256 authored exact + 64 physically traced inherited exact**,
zero source-value mismatches. Coupon routes are explicitly
refkey/refurl inherited from the *physically present consolidated*
`GamePass_Stash_ItemGenerator_Rare` source prototype. The 121
generic NPC weapon entries span 33 production generator roots and
include one original 35-45% condition outlier. The new B12 and B13
source gates reproduce the independent source and identity checks.
C8 runs 117 actual WPF ViewModel-to-CFG writer scenarios.

**Do not conflate this direct audit with complete Loot verification.**
The remaining **24 Loot catalog parameters use specialized writers**,
including stash production, corpse loot, breakable boxes, world
placements/removals and shuffle mechanics. They require distinct
Phase 14B writer/fixture audits, original generator provenance and
manual in-game acceptance. See
[Phase 14A](DEEP-CFG-AUDIT-PHASE14A-LOOT-DIRECT-2026-10-10.md)
and [archive fingerprints](evidence/LOOT-DIRECT-ORIGINAL-CFG-2026-10-10.json).

### Phase 14A Windows verification result (2026-10-10)

The real `loot-direct-writer-windows` test suite passed **117/117**
on commit `2c4d8b5770ef0199a0f814df367001a4bcfd8e97` in
GitHub Actions run `37999482003`. The first run had caught a
production defect: writer selection incorrectly used visual
`IsModified` (which includes aggregate highlights) instead of the
actual `IsValueModifiedFromReference()` source-change predicate.
Production selection was corrected; subsequent tests confirmed that
changing rare-stash coupon chance does **not** fan out into unrelated
stash ammo writer output. This is a real writer regression fix, not
a change in the UI. Source 320-point result is independent of this
C# validation; special Loot writers are still a separate Phase 14B.

### Phase 14B1: Loot special generator production BPatch gate

`dotnet run --project tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release -- --out audit/loot-special-generators-fixtures`
runs **63 Windows production writer cases**: real source-backed values, NPC
medical/corpse smart loot and ammo, three stash tier clones, RGD reward,
breakable ammo/grenades, ammo family weight distribution, inherited
template category item fields, complete audited redirect CSV fanouts,
DLC1 routing, and cross-control isolation. Every actual generated leaf is
compared against independently computed manifest and input transforms.
The source manifest is a contract, not proof that the original game
accepts generated prototypes.

This suite does **not** claim coverage of the remaining world-actor
removal, weapon/attachment/blueprint shuffle, detectors or loose loot.
Those require Phase 14B2. The generator templates and embedded redirect
manifests also require original-source provenance and gameplay checks.
Do not treat registered tests as PASS before CI execution.

### Phase 14B2: Physical world Loot writer contracts

Run `dotnet run --project tests/LootWorldRemovalsContract/LootWorldRemovalsContract.csproj -c Release -- --out audit/loot-world-removals-fixtures` (**27 randomized-domain physical actor removal tests**) and `dotnet run --project tests/LootWorldPlacementContract/LootWorldPlacementContract.csproj -c Release -- --out audit/loot-world-placement-fixtures` (**20 weapon/attachment/blueprint/detector placement tests**) in Windows .NET 10. Both are registered in the pre-release writer runner and as `loot-world-removals-windows` and `loot-world-placement-windows` CI jobs. Expected results are invariant-based, never seeded snapshots. Covers all 10 previously untested special world Loot source controls. See [Phase 14B2](DEEP-CFG-AUDIT-PHASE14B2-WORLD-LOOT-2026-10-10.md).

These two suites validate actual app → BPatch paths and randomized target legality, not the original archive provenance or Unreal runtime.

### World Loot decoded original archive source gate (B14)

The `loot-world-original-source-structures` gate invokes:
`python tools/audit_loot_world_source.py --base-zip BASE --dlc1-zip DLC1 --out audit/loot-world-original-source.json`.
It streams only the selected physical root prototypes from the large
original GameData/DLC1 `SpawnActorPrototypes.cfg` files and checks
the physical actor identities, direct item prototype fields, each
box's generator family, precise blueprint and weapon/attachment
generator item indices, physical generator redirects and the one
DLC1 detector stash item. Every mismatch is a failing finding.
**This requires matching source ZIPs supplied locally and is not a
GitHub Windows CI original-source check** (CI has only the repo).
Run `python tools/pre_release.py source --base-zip ... --dlc1-zip ...`
before release; store the gate's output JSON. This still does not
prove in-game Unreal BPatch loading.

### Phase 14B2 verified Windows writer receipt

The same-SHA production CI run `38003085576`, commit
`fb59fb36b14a7e8d8593aaf0dcc67e5cdf8736d1`, has
**27/27 PASS** for randomized World Loot physical actor removals and
**20/20 PASS** for world weapon/shuffle/density, attachment multiset,
77-blueprint derangement and detector removal. The physical-removal
artifact retains per-scenario SHA-256 actor identity receipts and samples
after verifying every emitted actor. Also audited and corrected the
outdated Loot catalog statement that dedicated F1 boxes overlap ordinary
ammo/RGD boxes; these source manifests are disjoint, and the current
Ammo Box population is 981 base + 334 DLC1, not the older numbers.
`loot-world-original-source-structures` has been registered as a
separate strict original-ZIP source gate but **must actually run
against the original decoded archives** before B14 source provenance
can be marked PASS. Gameplay and Unreal mod loading remain NOT RUN.

### World Loot Phase 14B2 — original GameData/DLC1 source proof and post-fix gate (2026-10-10)

**Original decoded source verification is independently PASS** against the two
uploaded physical CFG ZIP archives: 1,315 Ammo/RGD Boxes, 1,208 Food/Drink
Boxes, 868 Medical Boxes, 101 dedicated F1 Boxes, 5,395 loose dynamic
packs and 1,838 loose static items; 362 direct safe world weapon actors,
seven direct attachments, 77 world-only blueprint generators and seven
direct detectors. Verified **131 unique ItemGenerator direct leaf
identities/values** and **58 distinct original SpawnActor generator
redirect fields**. Normalized source manifest identities, physical
original field values, cardinalities and original archive SHA-256 provenance:
[World Loot source evidence](evidence/LOOT-WORLD-ORIGINAL-CFG-2026-10-10.json).
This source proof was computed by an **independent decoded-archive
scanner**; keep the in-repository `B14`
`tools/audit_loot_world_source.py` CLI as a required exact release gate
and run it with locally supplied original ZIPs.

**Critical defect fixed, post-fix CI PENDING:** original decoded
`ItemGeneratorPrototypes.cfg` proves that six world weapon stash
generators carry 15 Chance/MinCount/MaxCount entries only under nested
`Upgrades` or `Attaches`, not as immediate weapon item fields. The
old `AddReplacementNode` writer stripped those nested rewards and
copied unrelated nested values to the wrong node. Production now
writes only a minimal indexed `ItemPrototypeSID` `{bpatch}` and
leaves original item mechanics/upgrade subtrees intact. Those 15
misclassified source columns were removed and the Windows C11 suite
now rejects their reintroduction and validates the minimal patch.

**The earlier 20/20 World Loot placement PASS was on the OLD writer**,
not the source-corrected writer. GitHub Actions subsequent attempts
were failing at the runner provision stage with **zero executed steps
on every job**, so they cannot be described as code or contract PASS/FAIL.
Re-run C11, plus all regression jobs, on the final release SHA as soon
as GitHub Actions can execute jobs normally. The original archive source
PASS is separate from the production code validation and Unreal runtime
merge/gameplay acceptance.

### Survival Phase 9B — baseline and parent-gating cases

Seven more Windows real writer regression cases cover the four untouched base difficulties and three modified-but-disabled Master Survival children on Veteran. C3 now comprises **69 cases**, with no change to original 139/139 source-value evidence. All engine/gameplay acceptance remains manual.

### Survival Phase 9C — 3 combined-family writer regressions

Three additional **real** Windows BPatch tests check simultaneous hunger/fatigue controls, overlapping energy-drink source prototypes and three distinct Master negative-mechanics families. C3 now comprises **69 scenarios**, maintaining the independently established 139/139 original-source status. Source correctness and in-game behavior are still separate gates.


### Phase 9D — Survival constraint cascade release gate

The Windows SurvivalPatchContract now runs **85 production writer
scenarios**: 69 earlier cases plus 13 linked slider cascade tests
(all seven actual Survival constraint families) and 3 Master parent
OFF/ON round-trips. It parses the real generated CFG to verify
exact path sets, accurate transformed values and no unrelated edits.
Original-source parity is independently verified by B6 against
the real decoded ZIP pair; Unreal runtime remains untested.
