# Every documented weapon-containing world stash — original CFG source inventory (2026-10-18)

Primary machine-readable inventory: [39 rows, each with origin/physical ActorSID/level/clue/generator/category/item/status/reason](../../src/TrueCustomDifficulty/Data/stash-weapon-source-inventory.csv).

## What the source confirms

- Original base GameData: **25** weapon category item entries across ordinary/quest world stash actors.
- Original DLC1: **14** such entries (six defined in DLC CFG, **eight referencing generators defined in base GameData**).
- **39 rows / 38 physical actors**, of which **13 independently validated ordinary base-world stash weapon entries** are approved for deletion when Extra Stash Gear is enabled.
- **26 exclusions**: 16 quest-associated, one special/protected STC Malachite, one possibly unique Trophy AR, eight unapproved DLC1 special/cross-archive generator actors.
- `GameData/StashPrototypes.cfg` declares no direct `Gun*` source items. Native procedural SmartLoot can still generate things from other layers; source row presence does not mean a guaranteed spawn.
- The 13 approved source entries preserve weapon category context but remove only their exact original `PossibleItems/[0]` node, not the stash actor, ammo, non-weapon sibling categories or other mods' content.

## Fail-closed policy

The existing 13-entry removal manifest is NOT expanded merely because a generator contains a weapon. Quest labels, native `ClueVariableSID`, actor location in a quest sublevel, unique identity and validated physical source allowlists determine approval. DLC1's eight cross-archive references are intentionally withheld. Even a source with no explicit clue may be quest-owned (e.g. MQ/SQ/ANCQ source names); an empty clue alone is never proof of safety. The existing 106 protected base ActorSIDs are always excluded. The generator only removes the 13 independently audited regular entries; source inventory and writer checks reject status/identity drift.

## Reproducing from archives

```bash
python3 tools/audit_all_stash_weapons.py \
  --game-zip "/path/to/STALKER2-GameData-CFG(20261017-181645).zip" \
  --dlc-zip "/path/to/STALKER2-DLC1-CFG(10).zip"
python3 tools/audit_extra_stash_gear.py
```

First command scans exact CFG original source roots and physical actors, resolving DLC→GameData generator references; emits a failure if new/removed source weapon entries are not reviewed in the checked-in inventory. The scanner reports unresolved external generator references rather than pretending to establish runtime absence of weapons. Second checks the frozen manifest and source-to-writer safety contract without requiring ZIPs.

**Limits:** no experimental evaluation of a physical inventory spawn, no guarantee on already generated stash contents, no claim that these rows exhaust dynamic script-injected weapons or third-party mods. To approve any of eight DLC1 pending entries later, audit their quest and prototype references, source-specific actors and dependencies, update both catalogs and writer allowlist together, and add generated-CFG negative tests.
