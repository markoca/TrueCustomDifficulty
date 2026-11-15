# Generic NPC corpses and ordinary world stashes — generator ownership (2.1)

**Date:** 2026-11-15
**Scope:** Static source-to-consumer ownership; ordinary repeatable human NPCs versus normal world ItemContainer stashes. This is a **classification**, not a gameplay generator rewrite or evidence that an item will drop in a live game.

**Machine-readable decision catalog:** [generic-corpse-stash-generator-ownership-2.1.csv](generic-corpse-stash-generator-ownership-2.1.csv) (**123 distinct source SIDs**, including deliberately excluded/special contexts). Each row records `owner`, `source_sid`, `source_scope`, `writer_policy`, and `evidence_path`. The catalog is intentionally a **scoped decision inventory**, not a claim to enumerate the entire 3,452-root original ItemGenerator universe.

## 1. Classification result

| Domain | Registered source SIDs | Allowed future editor boundary |
| --- | ---: | --- |
| Ordinary human loadout | **33** `GeneralNPC_*` / `GuardNPC_*` local roots | Only already audited local weapon/equipment fields and specific redirected consumable links; not all `GeneralNPC` / `GuardNPC` by name |
| Ordinary human consumables | **6** `GeneralNPC_Consumables_*` role pools | Private TCD-owned clones; redirect the exact **33** approved normal-human source links; 104 native Chance rows (58 medical) |
| Optional corpse SmartLoot | **3** `NPC_Medicine_Smart`, `NPC_Ammo_Smart`, `NPC_Water_Smart` | One selected-difficulty `EconomyDifficulty.CorpseSmartLoot.GeneralNPC` list; never merge it with the regular NPC ItemGenerator |
| World stash tier parents | **3** `GamePass_Stash_ItemGenerator_*` bases | Cheap / Common_Var1 / Rare through guarded, TCD-owned clones, wrappers and actor source redirects |
| World stash tier wrappers | **12** `Stash_Cheap_*`, `Stash_Medium_*`, `Stash_Expensive_*` | Four rank wrappers per tier; don't count four ranks as four distinct physical containers |
| World stash SmartLoot | **13** `StashMedicine_*`, `StashFood_*`, `Stash_Ammo*`, `StashGrenade_*` source prototypes | Independent `StashPrototypeSID` layer with protected-actor/quest/installed-DLC checks |
| Physical `CorpseStashSID` roots used elsewhere | **31** | **Not** safe for global ordinary-corpse editing; may also feed named, quest, zombie, container or other contexts |
| Physical `CorpseStashSID` roots with corpse-only context | **8** | **Review/exclude**, not a generic loot pool: five faction loadouts plus three script/prologue/special bodies |
| Corpse-named SmartLoot without resolved consumers | **2** `StashMedicine_Corpse`, `StashVodka_Corpse` | **Unresolved / no editor**, do not adopt based on naming |
| Destructible/breakable generators | **12** `DestructibleStash_*` source variants | Separate breakable-container domain, not the ordinary Basic/Standard/Rare stash tiers |

Counts are **distinct generator/prototype names by category**, not number of physically spawned NPCs, stashes or independent rolls.

### 2. Ordinary human corpse path

```text
repeatable human SpawnActor/archetype
  -> approved 33 GeneralNPC_* / GuardNPC_* loadout roots
     -> 6 shared GeneralNPC_Consumables_* role pools
          (TCD private clone + 33 source-link redirects for user changes)
  + selected-difficulty CorpseSmartLoot.GeneralNPC (optional separate source)
     -> NPC_Medicine_Smart / NPC_Ammo_Smart / NPC_Water_Smart
  + audited opt-in mod-added corpse generator branches (third-party, separate)
```

The approved 33 roots are the **exact rows of** `src/TrueCustomDifficulty/Data/npc-medical-production-redirect-manifest.csv`, not every NPC-related generator in the raw reference graph. Those roots are the current safe default for ordinary-human controls such as dropped weapon condition and the ten explicit Food/Drinks/Medical source-Chance overrides. Each item-specific control writes one literal Chance only where the source SID exists, subject to original difficulty/rank gates; 100% Chance is not a per-corpse guarantee.

The **six role pools** are `GeneralNPC_Consumables_CloseCombat`, `..._Heavy`, `..._Noon`, `..._Recon`, `..._Sniper`, `..._Stormtrooper`. They are not globally safe source definitions because they can be consumed in contexts outside the approved normal-human set. Do not edit the original definition directly when a private clone and exact redirect are available.

The **three corpse bonus** prototypes are independent of normal NPC medical, weapon attached ammo and physical `CorpseStashSID`. Original list defaults: Novice = Medicine + Ammo, Stalker = Water, Veteran/Master = none. The editor can select any of the three authored lists for the current reference difficulty; changing ammo quantity only affects `NPC_Ammo_Smart` while selected.

### 3. Generic world stash path

```text
ordinary world ItemContainer physical ActorSID
   -> ItemGeneratorSettings / local location wrapper
        -> Stash_Cheap_{Newbie,Experienced,Veteran,Master}
           -> GamePass_Stash_ItemGenerator_Cheap
        -> Stash_Medium_{Newbie,Experienced,Veteran,Master}
           -> GamePass_Stash_ItemGenerator_Common_Var1
        -> Stash_Expensive_{Newbie,Experienced,Veteran,Master}
           -> GamePass_Stash_ItemGenerator_Rare
   AND/OR separate StashPrototypeSID
        -> world StashMedicine / StashFood / Stash_Ammo / StashGrenade
           SmartLoot prototype
```

There are **41** authored branch groups in the three tier-parent sources (12 Cheap, 10 Common, 19 Rare), and **422** branch groups in the separate **13** world SmartLoot prototypes: **463** branch groups, not 463 physical stashes. The pinned inventory is `docs/generators/stash-generator-branches.csv`. The world SmartLoot audit has **991** item rows and **2,143** production consumer edges; **2,105** production SmartLoot edges coexist with ItemGenerator wiring. This is often a dual-layer container, so neither layer alone is a truthful total-stash probability.

The guarded tier-redirect manifests presently contain **1,271** Cheap, **450** Medium and **305** Expensive source redirect assignments (2,026 manifest rows before per-actor gate decisions). The source graph distinguishes approved, protected and DLC1 conditions. Their rows represent **reference edges**, not unique physical stashes. World SmartLoot likewise requires exact protected actor and source prototype checks. `GamePass_Stash_ItemGenerator_Rare` is also reachable from a standard-human NPC path: this is a **shared target** despite its stash-oriented name. Only modify it through correctly scoped clones/redirects; never globally mutate the shared parent.

### 4. Physical corpse overrides are NOT the generic corpse set

`SpawnActorPrototypes.CorpseStashSID` has **99** mapped physical DeadBody edges (**57 base + 42 DLC1**) targeting **39** direct ItemGenerator roots. Of those, **31** roots have non-corpse consumers. The other **8** have corpse-only propagated scope, but are heterogeneous: five faction/rank composite loadouts (`DutyNewbieItemGenerator`, `FreedomExperiencedItemGenerator`, `MonolithVeteranItemGenerator`, `ScientistExperiencedItemGenerator`, `SparkVeteranItemGenerator`), plus bespoke `GarbageDetCenterCorpseItemGenerator`, `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` and `YantarDeadBody_Spark_ItemGenerator`. **None are approved as a uniform generic corpse drop pool.**

The direct-root names in this 39-SID override inventory have **no overlap** with the approved 33 ordinary-human redirect roots, but a separate special corpse may still traverse shared nested pools. The root-level disjointness does **not** imply total descendant isolation.

### 5. Scope decisions for upcoming TCD features

- **Safe existing control ownership:** 33 audited ordinary-human local roots + six private cloned consumable pools; optional corpse bonus packages only by exact list; stash tier sources only with protected actor redirects and private clones; world stash SmartLoot only with physical allowlisting.
- **Shared or unverified:** all other raw `GeneralNPC_*`, `GuardNPC_*`, `GamePass_*` and inherited/nested source definitions. Treat as cross-context until a consumer manifest proves an isolated edit.
- **Never include automatically:** named and quest corpses, Prologue, zombies, mutants, traders, scripted `CorpseStashSID`, clue/protected stashes, destructible crates, DLC targets without eligible DLC1 gate, or mod-owned generators absent from the exact audited PAK allowlist.
- **Explicit exclusion:** **Echo Detector Chance** must not be introduced into the UI or writer, even though older audits cataloged source rows.
- **Numerical semantics:** original `Chance`, relative `Weight`, per-item `MinCount/MaxCount`, weapon `AmmoMinCount/AmmoMaxCount`, SmartLoot set count and group spawn endpoints are distinct. Never present one synthetic generic corpse/stash percentage that claims to cover all of them.

## 6. Data provenance and acceptance

**Primary sources:** `src/TrueCustomDifficulty/Data/npc-medical-production-redirect-manifest.csv`, `src/TrueCustomDifficulty/Data/cfg-writer-baseline.json` (`gameVersion=2.1`), `docs/generators/stash-generator-branches.csv`, `src/TrueCustomDifficulty/Data/{cheap,medium,expensive}-stash-production-redirect-manifest.csv`, `docs/generators/CORPSE-OVERRIDE-AUDIT.md`, `docs/generators/SMARTLOOT-WORLD-AUDIT.md`, `docs/generators/ITEM-GENERATOR-USAGE-FOOTPRINT.md`, and `docs/generators/STASH-CONSUMER-GRAPH-2026-10-16.md`.

**Audit:** `python3 tools/audit_generic_corpse_stash_ownership_21.py`. The script verifies exact classified SIDs and disjoint boundaries against the checked-in manifests and source audits. Run the existing source and writer tests separately. The referenced original 2.1 archive was not freshly re-extracted in this pass; this is pinned repository provenance, not live game RNG observation. Game acceptance still needs newly spawned ordinary humans and unopened world stashes, with old corpses/quest bodies excluded.
