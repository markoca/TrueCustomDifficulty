# Generator Base Check — v0.2.38

This is a **baseline integrity check**, not a new control-design pass. Raw/local generator catalogs and the consumer graph remain the source of truth; existing Markdown/CSV decisions are checked against them rather than assumed correct.

## Baseline topology reproduced

| Surface | Reproduced baseline |
| --- | ---: |
| Raw/local Item Generator roots | 3,452 |
| Inheritance edges | 2,968 |
| Composition rows | 5,783 |
| Consumer-context edges | 13,647 |
| Raw/local item/nested-generator rows | 30,991 |
| Effective item/nested-generator rows | 33,680 |

Inheritance remains acyclic in the audited baseline; 2,880 inheritance edges resolve to named generator SIDs and 88 retain exported numeric/template-style parent metadata. Composition contains 312 `Empty` sentinel targets and no unresolved real nested-generator target in the existing catalog.

## Consumer graph sanity

The largest consumer classes remain `destructible_world` (8,253 edges), `item_container_world` (2,372), `human_npc_named` (1,709), `quest_scripted` (759), and `human_npc_standard` (139). This confirms why generator names alone are unsafe for tuning decisions: many reusable roots have consumers in more than one gameplay context.

### Human NPC boundary

`human_npc_standard` contains **139 consumer edges across 99 generator roots**, but that is **not** the safe writer surface. The isolated production surface used by Human NPC Medical, Dropped Weapon Condition and Weapon Ammo is the same exact **33-root set**. Shared `GeneralNPC_*` roots outside that set also reach named, quest/scripted, corpse or zombie contexts and must not be globally edited merely because they have a standard-human consumer.

The 33-root medical and weapon production sets are identical. Existing hard exclusions for Prologue and MIK/Mykluha remain intact. `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom` and `GeneralNPC_SIRCAA_Scientist_Recon_ItemGenerator` remain in the isolated set because the consumer graph resolves them to standard archetype prototypes; they are not excluded merely from their names.

## Redirect / clone integrity

The six production redirect manifests remain synchronized across their embedded/project mirrors:

- Cheap stash: 1,271 redirect assignments.
- Medium stash: 450 redirect assignments.
- Expensive stash: 305 redirect assignments.
- Destructible ammo: 1,235 production redirects.
- Destructible grenade: 182 production redirects.
- Human NPC medical: 33 production redirects into 6 TCD-owned pools.

Shared vanilla pool definitions remain protected where the audited blast radius crosses gameplay contexts. No base-check finding justifies widening an existing writer target.

## Trader root-count clarification

The trader catalog contains **96 domain-discovery roots**, but only **92** currently have resolved `trader_stock` runtime consumers and form the strict decision-layer surface. The four review-only roots are:

- `HimzavodTrader_ItemGenerator`
- `Trader_Ammo_Experienced_ItemGenerator`
- `Trader_Ammo_ItemGenerator`
- `VezaMedic_TradeItemGenerator`

This is a documented 96 → 92 eligibility boundary, not a change to the existing `NO_UI_AGGREGATE_MIXED_SEMANTICS` decision.

## Baseline issue found and fixed

The root `control-candidate-triage-summary.json` had remained at **v0.2.36** while the `generators/` and `docs/generators/` mirrors were **v0.2.37**. The root mirror is synchronized and the consistency audit now requires all three summary files to be byte-for-byte identical.

The consistency audit also now locks the core generator topology and the **99 standard-human roots → 33 isolated production roots** distinction so a future catalog regeneration cannot silently widen the tuning surface.

## Result

**BASELINE PASS WITH ONE STALE-MIRROR FIX.** No evidence was found that requires changing the current generator-control decisions, production redirect boundaries, or 175-parameter production writer. Runtime-proof candidates remain runtime-proof candidates; audit-only / no-UI decisions remain unchanged.
