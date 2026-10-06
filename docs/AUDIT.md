# v0.1.63 Baseline Audit

## Result

The project baseline was rechecked before any further generator work. This pass used the exact supplied STALKER 2 runtime 2.0.5 dump as the primary source of truth, then compared that source to both `parameters.json` and `cfg-writer-baseline.json`.

Current visible state:

- visible parameter catalog: **167 controls**;
- concrete writer coverage: **167 controls**;
- ordinary writer edit points: **699**;
- ordinary edit points matching exact runtime source file/root/path/key/value: **699/699**;
- visible deferred controls: **0**;
- deferred research entries: **15** (including the 13 removed aggregate Loot concepts).

`Starting Coupons` remains the runtime-confirmed reference case: `economy.startingMoney -> GameData/CoreVariables.cfg -> DefaultConfig -> PlayerStartingMoney -> set`.

## Runtime dump integrity

The audited runtime dump matches the recorded SHA-256:

`739ecb4a60e2d98181cc6b326894d184b76a09c29b94a66c18d7b0da2c37a83a`

CFG counts were revalidated:

- `GameData`: **485**
- `DLCGameData/DLC1`: **118**
- `DLCGameData/PreOrder`: **11**
- `DLCGameData/Deluxe`: **10**
- `DLCGameData/Ultimate`: **7**
- total: **631**

## Writer baseline verification

Every ordinary edit point was checked for:

```text
file exists
  -> physical source root exists
  -> complete nested node path exists
  -> key exists
  -> sourceRaw equals the runtime value
```

Result: **699/699 exact**. No edit point now requires resolving an emitted root indirectly through `SID`.

The four vanilla difficulty/economy reference tables were independently checked across **34 listed CFG fields** and matched the runtime dump. For direct `set` controls, **346 vanilla-marker-to-source comparisons** also matched after the Low Bleeding correction.

Separately checked special structures:

- Additional Mechanics membership;
- `CorpseSmartLoot.GeneralNPC`;
- Master PSY override entries;
- mutant cooldown-family entries/tags.

These structures match the runtime source shape used by their special writers.

## Corrections made in v0.1.63

### Low Bleeding Damage

The writer mapping was already correct, but the UI catalog carried a stale vanilla marker of `0.33`. Runtime 2.0.5 contains:

```text
LowBleedingDamage
  ValueMin = 1.0
  ValueMax = 1.0
```

`survival.bleedingLowDamage` now uses vanilla `1.0`, a matching description, and a range centered on the real source value.

### Fast Travel Cost

All **183** fast-travel `RequiredMoney` source values were correct, but `FastTravelPrototypes.cfg` uses physical root keys `[0]..[14]` with a `SID` field inside each root. The writer baseline previously stored the SID as `prototype`, which could make the generated BPatch root differ from the physical source root.

The 183 mappings now use the actual `[0]..[14]` roots. The guide SID is retained as metadata in `role`. After this correction the full direct audit resolves **699/699** through physical roots.

### Custom difficulty cleanup

The machine-readable catalog no longer exposes a `Custom` difficulty profile. Runtime `Custom` remains documented as a game fact, but the tool has exactly four Base Presets: Novice, Stalker, Veteran and Master. Stale PSY/runtime-test wording was updated accordingly.

## Support vocabulary

- **CFG confirmed**: source field/value/structure exists in runtime CFG.
- **Writer-mapped**: tool has a deterministic output contract to that source structure.
- **Runtime confirmed**: generated behavior has been verified in game.
- **Runtime test required**: mapping exists, semantics still need in-game confirmation.
- **Deferred/research**: no visible control until selectors/transforms are sufficiently proven.

Structural writer coverage must never be presented as automatic runtime confirmation.

## Retained v0.1.62 strict Loot audit

The 13 previously visible but writer-deferred Loot controls were checked again against the current extracted base `GameData` and `DLC1` CFGs. None met the same directness standard as a concrete writer-mapped control. They were therefore removed from the UI and moved to `parameters.json -> deferred`.

| Former control | Verdict | Why it is not 1:1 |
| --- | --- | --- |
| Dropped Weapon Condition | Removed | `MinDurability/MaxDurability` are real, but occur across many NPC weapon-generator families. One slider would be an aggregate transform over a curated target set. |
| NPC Grenade Drop Chance | Removed | Grenade `Chance` fields are real, but appear across normal NPC, quest/prolog, trade, stash and special generators. “Normal NPC grenade loot” is a selector we must define, not one CFG property. |
| Loose Ammo Stack Size | Removed | `Category::Ammo` count fields are real, but “loose ammo” is an inferred subset requiring exclusions for stashes, quests, player/trader generators and other contexts. |
| Novice Stash Medical Bonus | Removed | Easy-only medical branches are real, but there is no backing boolean. Enabling/disabling the branch would be a synthetic transform. |
| NPC Food Drop Chance | Removed | Food chances are split across both `GeneralNPC_Consumables*` role pools and the older four `Consumable*Generator` rank pools. The former mapping covered only part of the runtime structure. |
| NPC Medical Drop Chance | Removed | Same multi-family problem as food; no single medical-loot property exists. |
| NPC Medical Stack Size | Removed | Medical count ranges occur in multiple active consumable families; one slider is an aggregate transform. |
| NPC Utility Drop Chance | Removed | `Energetic`/`Vodka` chances exist in multiple active families; the category grouping is tool-defined. |
| Dropped Weapon Ammo | Removed | `AmmoMinCount/AmmoMaxCount` are real, but appear in the 60 EN/RU/ALL role/rank families, many `GeneralNPC_*` weapon entries, and DLC1-specific families. The previous target model was incomplete. |
| Stash Ammo Stack Size | Removed | Main stash aliases inherit `GamePass_Stash_ItemGenerator_*` bases; a generic `Stash_*` write is not a direct field mapping without inheritance-aware enumeration. |
| Stash Food & Utility Drop Chance | Removed | Some branches use direct `Chance`; others use relative `Weight`. One abundance slider would mix different semantics. |
| Stash Medical Drop Chance | Removed | Medical entries span several inherited branches and are not uniformly represented by one direct `Chance` surface. |
| Stash Medical Stack Size | Removed | Medical count ranges span several inherited stash branches; one slider is an aggregate transform. |

## Important source correction

The previous Loot documentation stated that the current base runtime no longer contained the `GeneralNPC_Consumables*` family. That was incorrect for the currently supplied `GameData` dump. The audited file contains eight `GeneralNPC_Consumables*` prototypes, while the four `ConsumableNewbie/Experienced/Veteran/MasterGenerator` pools also still exist and are referenced. Both systems must therefore be considered when any future NPC-loot control is designed.

The audit also confirmed that the current CFG contains:

- **60** EN/RU/ALL role/rank weapon generator prototypes, all carrying weapon durability and attached-ammo fields;
- many additional `GeneralNPC_*` weapon entries with their own durability/ammo ranges;
- stash aliases (`Stash_Cheap_*`, `Stash_Medium_*`, `Stash_Expensive_*`) that inherit shared `GamePass_Stash_ItemGenerator_*` prototypes where the actual item rules live.

## Policy going forward

A future aggregate control can return only after its target set is explicit and reviewable. The preferred evidence format is:

```text
UI parameter ID
  -> exact file
  -> exact prototype SID(s)
  -> exact node/index or stable item selector
  -> exact property
  -> exact operation
```

Wildcard descriptions such as “all normal NPC loot” or “non-quest loose ammo” are not sufficient by themselves.
