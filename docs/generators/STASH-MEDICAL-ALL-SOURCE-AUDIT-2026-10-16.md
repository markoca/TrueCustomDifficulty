# Medical-item sources in stash and container generators — source audit

**Audited:** 2026-10-16; source corpus: STALKER 2 game v2.0.5 as previously catalogued in this repository. **Status: source inventory, not a gameplay fix.** The observed post-Richter/early Lesser Zone medical items are not linked to a verified actor SID or spawn prototype by the present evidence. Avoid global changes to NPC/quest/one-off reward generators without that mapping.

## Key architectural finding

A single physical `ItemContainer` can carry **two separate independent generation paths**:

```text
WorldMap_WP SpawnActor / ItemContainer
  + ItemGeneratorSettings -> ItemGeneratorPrototypes.cfg
  + StashPrototypeSID     -> StashPrototypes.cfg / SmartLoot
```

The current TrueCustomDifficulty `MEDICAL=0x` and four medical item biases work only on production-only TCD clones of **three regular ItemGenerator parents** plus their audited production redirects. Generated output in the user's supplied `zzzzz_TrueCustomDifficulty_test_diff.pak` zeroes medical selection and counts in those clones, but **does not patch StashPrototypes.cfg**. The source audit proves that SmartLoot medical rows are independently reachable on many production containers, and is a plausible missing layer, but is not yet mapped to the reported concrete stash.

## A. All explicit medical-bearing GamePass stash core pools in the tracked raw-row catalogue

Source: [WORLD-STASH-CORE-POOL-CATALOG.md](WORLD-STASH-CORE-POOL-CATALOG.md) and [WORLD-CONTAINERS-ITEMGEN-AUDIT.md](WORLD-CONTAINERS-ITEMGEN-AUDIT.md). Counts below are **raw medical item rows per local pool**, not physical stash counts, random-drop percentages or fully inheritance-resolved scopes.

| SID in ItemGeneratorPrototypes.cfg | Medical SIDs in raw rows | Raw rows | Current TCD Medical 0x | Source/runtime scope |
| --- | --- | ---: | --- | --- |
| `GamePass_Stash_ItemGenerator_Cheap` | Bandage (9), Medkit (3) | 12 | **Yes, TCD-owned production clone** | Mixed world + quest + destructibles; global edit forbidden |
| `GamePass_Stash_ItemGenerator_Common_Var1` | Bandage (3), Medkit (3) | 6 | **Yes, TCD-owned production clone** | World + quest; global edit forbidden |
| `GamePass_Stash_ItemGenerator_Rare` | Bandage (1), Medkit (2), AntiRad (2), ArmyMedkit (1) | 6 | **Yes, TCD-owned production clone** | World + standard-human NPC path; global edit forbidden |
| `GamePass_Stash_ItemGenerator_CheapNoFresh` | Bandage (9), Medkit (3) | 12 | **No** | Separate main-world ItemContainer candidate; needs consumer allowlist |
| `CNPPGamePass_Stash_ItemGenerator_Rare` | Bandage (1), Medkit (2), AntiRad (2), ArmyMedkit (1) | 6 | **No** | Container-world candidate; not a TCD cloned generator |
| `CNPPGamePass_Stash_ItemGenerator_Cheap` | Bandage (4), Medkit (3), **Antirad** (1) | 8 | **No** | No resolved runtime consumer in existing audit; do not assume active |

**Total: 6 explicit medical-bearing core pool SIDs / 50 local medical item rows** in this particular archived raw-row catalogue, of which **3 pool SIDs / 24 raw rows** have current regular-stash TCD production clone tuning. Raw-row totals are not a claim of complete 3,451-SID effective generator reachability. `GamePass_Stash_ItemGenerator_Common_Var2`, `Gamepass_Stash_ItemGenerator_Common`, and the surveyed ammo-only pools have no local medical item rows in this catalogue; this alone cannot exclude an independent inherited or nested generator path.

The audited TCD regular-stash production redirect manifests have **1,271 Cheap, 450 Medium and 305 Rare assignments**; these counts represent redirected source assignments, *not* distinct physical boxes.

## B. World SmartLoot medical generators that remain untouched

Source: [SMARTLOOT-WORLD-AUDIT.md](SMARTLOOT-WORLD-AUDIT.md), [SMARTLOOT-WORLD-ENTRY-CATALOG.md](SMARTLOOT-WORLD-ENTRY-CATALOG.md) and [SMARTLOOT-BRANCH-CATALOG.md](SMARTLOOT-BRANCH-CATALOG.md), all derived from StashPrototypes.cfg.

| SID in StashPrototypes.cfg | Medical entries | Exact local item rows | Raw consumers | Production consumers | Dual-layer production |
| --- | --- | ---: | ---: | ---: | ---: |
| `StashMedicine_Cheap` | Medkit, Bandage | 2 | 22 | 18 (10 ItemContainers + 8 destructibles) | 8 of 18 |
| `StashMedicine_Smart` | Medkit, Bandage, ArmyMedkit, **Antirad** | 13 | 1,044 | 1,036 (1,035 ItemContainers + 1 destructible) | 1,035 of 1,036 |

- `StashMedicine_Cheap`: two independent Newbie `ConsumablesParams` sets, each `MinSpawnChance=0`, `MaxSpawnChance=1`, `ItemSetCount=3`; Medkit `Weight=5`, **3–5 items**; Bandage `Weight=5`, **4–8 items**.
- `StashMedicine_Smart`: eleven sets across Newbie/Experienced/Veteran/Master; item-level `Weight`, `ItemSetCount`, `MinSpawnChance`/`MaxSpawnChance` and per-item `MinCount`/`MaxCount` are distinct. Endpoints include 0.1–0.7 and 0.1–0.8. This cannot be safely rewritten as a single chance percentage.
- **Important:** These are independent SmartLoot overlays. A container whose TCD ItemGenerator produces zero medical items can still have separately defined medical SmartLoot. A patch must address production consumers through an actor/consumer allowlist and preserve quest/test exclusions; do not globally zero a shared `StashMedicine_Smart` SID without checking consumers.

## C. Separate medical destructible ItemGenerator family

Source: [DESTRUCTIBLE-MEDICAL-PRODUCTION-AUDIT.md](DESTRUCTIBLE-MEDICAL-PRODUCTION-AUDIT.md).

| Root / wrapper SID | Reachable medical SIDs | Selection |
| --- | --- | --- |
| `DestructibleStash_Med` | Bandage, Medkit | `Weight`; 1..2 item ranges |
| `DestructibleStash_Med_Experienced` → `DestructibleStash_Medkit_Experienced` | Medkit, ArmyMedkit, **ScientificMedkit** | wrapper `Chance` + child `Weight` |
| `DestructibleStash_Med_Veteran` → `DestructibleStash_Medkit_Veteran` | Medkit, ArmyMedkit, **ScientificMedkit** | wrapper `Chance` + child `Weight` |
| `DestructibleStash_Med_Master` → `DestructibleStash_Medkit_Master` | Medkit, ArmyMedkit, **ScientificMedkit** | wrapper `Chance` + child `Weight` |

Exactly **869 distinct production medical destructible actors**, mostly `D_MetallCrate_01`, are counted in the strict medical audit. These are not the same as ordinary stash containers. One `DestructibleStash_Medkit_Master` consumer is a non-destructible production ItemContainer, so editing that global leaf leaks across domains. The unresolved `DestructibleStash_Med_Newbie` is referenced only from quest/test scope and has no source definition in the audit. This family is **not** currently governed by Stashes > Medical 0x, and should have an explicitly named scope if extended.

## D. Medical SmartLoot that is NOT proven to be normal world stash content

- `StashMedicine_Corpse`: 2 medical item rows (Medkit/Bandage) but **no resolved external consumer**; not counted as normal-world coverage.
- `NPC_Medicine_Smart`: 13 medical item rows, dedicated NPC corpse SmartLoot path, not regular world stashes; governed by a separate NPC bonus system.
- Other NPC consumable generators, quest/scripted/unique stashes, scene rewards, and already placed pickup items require separate traces and should not be broadly modified as part of regular stashes.
- Note exact SID spelling across corpora: `AntiRad` vs `Antirad`, and `EcoMedkit` vs `ScientificMedkit`. A case-sensitive selection map cannot safely assume that these are one identical item ID.

## E. What the actual generated user mod can and cannot prove

The provided PAK contains a generated `GameData/ItemGeneratorPrototypes.cfg` patch with TCD clone roots and production instance redirects, but **no `StashPrototypes.cfg` patch**. The earlier inspection of the user-provided 0x preset and packed CFG confirmed that medical entries in the **three TCD clone families** have zero Chance/Weight and zero MinCount/MaxCount. That does not change any of B, C, D or hand-authored physical items.

The prologue/first-Richter path itself contains non-random medical rewards and fixed pickups according to walkthrough evidence. A medical item **received from dialogue or found loose on a shelf/floor** is not proof of an active item-container generator. A specific actor/container SID is still required to identify the actual stash tested in-game.

## Proposed verification/implementation order — no scope creep

1. **Trace one actual physical container near Richter**, distinguishing dialogue/loose/quest loot from an unopened ItemContainer with `ItemGeneratorSettings` and `StashPrototypeSID`. Identify the exact actor SID and both producer paths.
2. **Audit / implement StashMedicine_Smart** only for verified production consumers, retaining its full rank/set semantics. It has the largest confirmed medical SmartLoot footprint; change both eligible medical selection and generated item-count paths where semantics are source-supported.
3. **Audit / implement StashMedicine_Cheap**, using its separate two-set specification and preserving the 4 quest/test/nonproduction edges.
4. **Audit additional world-only ItemGenerator cores** `CheapNoFresh` and `CNPP Rare` with production-specific clones/redirects, never by editing a shared global target without checking other consumers. `CNPP Cheap` stays deferred until runtime reachability is proven.
5. **Handle medical destructible boxes separately**, since their gameplay/scarcity intent can differ from world stash items. Retain their production actor allowlist and the shared Master leaf exclusion.
6. **Explicitly exclude** NPC corpse loot, scripted/quest awards and already authored world pickups from ordinary stash scaling, unless the user separately requests them. Confirm all layers with generated output tests and gameplay on unpopulated containers.

## Important evidence boundary

This is a consolidated audit of the **tracked source-derived raw-row and wiring catalogs**, not an exhaustive proven enumeration of every dynamically or script-generated reward in all game assets. A full raw-source recursion and exact actor mapping would be needed for an all-generator total. No gameplay changes have been implemented by this document.
