# Stashes Phase 3 — shared source-to-effective CFG calculation

**Status (2026-10-16): implemented in code; .NET/Wine runtime test not executed in this session.** This phase changes tier/SmartLoot writers and the existing Stashes sample preview to use a shared source-field transformer. It does not add the Phase 4 branch selector or independent per-branch editing.

## Shared engine

The source of effective field calculations is **src/TrueCustomDifficulty/Services/StashEffectiveState.cs**. Its constructor captures current numeric parameter values, a set of **explicitly modified parameter IDs**, and the selected native difficulty SID. It contains no WPF graphics, editable UI controls, actor patches or RNG assumptions.

| Writer / consumer | Shared API | Source behavior |
|---|---|---|
| Basic / Standard / Rare ItemGenerator | **TierField** | Authored field value, rank/difficulty-qualified special bonus, category × medical bias, then medical count cap |
| Cabin / direct-medical / DLC1 fields | **TuneSelection**, **MedicalDisabled**, **TuneCount** | Existing wrappers now call shared functions instead of duplicating formulas |
| World SmartLoot | **SmartFactor**, **SmartSelection**, **UniformSmartFactor**, **SmartSpawnEndpoint**, **TuneCount** | Per-item Weight/Chance, uniform group spawn endpoints and count suppression |
| Current Content Mix / Medical Composition preview | **TuneSelection** for two Basic source Weight pools, **TierField** for original Chance examples | Exactly the same numeric field calculation as writer, not an independent UI guess |

### Source priority and calculation

- **Basic RGD-5:** original Chance 0.1; only if the RGD-5 Chance control is explicitly changed, first substitute its authored new Chance; then multiply by global Grenades factor, clamp Chance to [0,1]. Quantity is a separate count transformation.
- **Rare Coupons:** the modified Rare Coupons control substitutes a source Chance only for the original entry whose difficulty gate matches the actual selected native difficulty; then global Coupons multiplier and clamp. Unchanged control retains raw native Chance.
- **Medical:** global Medical × item-specific bias; zero GLOBAL/DETAIL and Medkit/ArmyMedkit MAX=-1 suppress selection and corresponding counts. MAX=0 still means AUTO; source min/max floor/cap constraints stay in force.
- **Weight:** never normalize across unrelated native generator groups. Preserve native Weight arithmetic and each original item's independent entry.
- **SmartLoot:** when all items in exactly one original rank/group/set have equal non-1 factor, scale/clamp that group's original MinSpawnChance and MaxSpawnChance; if zero, also set ItemSetCount=0. With heterogeneous biases no invented group-spawn scaling is performed.
- **No-op:** the writer still creates no TCD clone for a vanilla profile. Phase 2's static ELIGIBLE_CONDITIONAL edges are not automatically ACTIVE. Existing 106 protected actor SIDs, 803 base SmartLoot allowlist and 112 DLC1 approved actors retain their separate writer gates.

The basic, medium and rare writers now call TierField for every emitted original item field. The source preview calls this same TierField for the displayed native Chance example, including *previously omitted* RGD-5 and Rare Coupon bonus controls. The Content Mix board subscribes to these two cross-panel sources read-only; the Rare Coupon example is explicitly **Easy** until Phase 4 adds a selector. The two actual Basic Weight pool examples call TuneSelection directly. The four Medical Composition examples also call TierField.

### Remaining distinctions

These previews reflect **actual native field transformations**, not the probability of whole stashes spawning those items. All-zero Weight group engine fallback behavior remains unverified. The SmartLoot writer decides whether a prototype clone and consumer redirect exist **based on the changed parameter set**; a source relationship alone does not imply a generated output entry.

The six approved DLC1 **CheapNoFresh** actors are a separate 35-original-item overlay, outside the 463-index; their category/medical selection and count use the shared transformer, while their **DLC-only original grenade bonus/amount and ammo count preprocessing** remains specific to that overlay. Additional authored direct rewards, unique/quest loot, added gear and game-world RNG remain outside the current preview samples and should not be presented as complete stash outcome predictions.

No Stashes persistent parameter ID, preset schema, actor safety rule, generator scope or current visual control geometry has been changed in this phase. Branch-aware UI remains **Phase 4**.

## Verification and acceptance

Static source contracts:

    python tools/audit_stash_generator_branches.py
    python tools/audit_stash_consumer_graph.py
    python tools/audit_stashes_mixer_ui.py
    python tools/audit_project_consistency.py

Actual generated-CFG regression harness:

    ./tests/run-wine.sh LootSpecialGeneratorsContract

The new **VerifyStashEffectiveParity** test generates a real Easy composite profile (Medical, Bandage, RGD-5 bonus, Grenades, Rare Coupons, Coupons and MAX=-1) and a real Hard/DLC composite profile (Medical zero, Food 3x, Ammo 0.5x, changed RGD-5/Rare Coupons). It compares the shared TierField result against **every field of every item in the emitted Basic, Standard and Rare TCD source clones**, then checks a generated SmartLoot Medkit Weight against SmartSelection. Existing comprehensive generated-CFG contract cases still verify protected actors, extra DLC and medical-zero behavior.

**Not run here:** the connected GitHub tooling can verify committed source/blobs, but not compile net10.0-windows or run Wine/game instances. Passing static checks only verifies code/source structure, not successful runtime parity. If the user test fails, correct code + associated docs on main; do not dilute expected-output assertions.

The next step is Phase 4's compact **generator → rank/difficulty → native group** selector consuming this same effective field engine and the guarded Phase 2 consumer graph. Per-branch independent editing/persistence would require its own explicitly scoped controls and writer/preset contract.
