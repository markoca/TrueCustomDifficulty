# World Armor Removal — Exact Non-Quest Loot Surface

## 2026-11-13 — Remove World Armor (active source-backed control)

A single default-OFF **REMOVE WORLD ARMOR** checkbox is added to **World Item Spawns** in the Loot tab. It shares the existing compact right-hand toggle column, which now has four 44-pixel-high toggles at 49-pixel pitch, rather than adding a new card or slider. The World Item Spawns aggregate retains one visible anchor; this checkbox is an independently bound control.

When enabled at Build, exactly **76 source-audited direct base-world suit/helmet actors**, plus **18 DLC1 direct actors** when installed, are removed using per-ActorSID `removenode` patches. Six independently approved **regular physical-stash helmet item nodes** are removed with indexed ItemGenerator BPatch paths. No broad removal by armor item name is allowed. Vanilla source slots are otherwise inherited.

The source domains are `src/TrueCustomDifficulty/Data/world-armor-direct-manifest.csv` and `world-armor-stash-manifest.csv`; only safe `WorldMap_WP` normal wearable pickups and independently established non-quest stash generator/actor pairs qualify. Quest/story suits including X7, quest-linked stash armor, named-clue/shared reward generator slots, protected stashes, NPC/player equipment and merchant inventory are excluded. If Extra Stash Gear is also enabled, it keeps other bonus gear classes but suppresses its Armor bonus class, so this removal is not silently undone.

One toggle, not a second stash checkbox: `loot.worldArmorRemoved` with independently serialized ON/OFF default OFF. The source control is #286 (previously 285) and the player-visible control inventory is #181 (previously 180). The historical 180-control RC copy freeze predates this addition and remains an archive; this control has a new reviewed copy row. The real-world game result requires in-game validation, particularly with previously opened stashes.

### Operational notes

- OFF: no world or stash armor removal patches; existing regular wearable spawns remain.
- ON: 76 base direct actors + 6 safe stash helmet entries, and an additional 18 direct DLC1 actors if DLC1 is installed. Quest and scripted loot remains untouched.
- Preserve normal world weapon and detector settings; only additional armor candidates in Extra Stash Gear are suppressed while the removal is ON.
- Tests cover physical actor and ItemGenerator node paths and output accounting; game runtime and existing-save behavior remain to be tested.


## Preset upgrade compatibility

The exact SHA-1 Git blob IDs of the immediately preceding 12 pristine 285-source factory presets are recorded in `PresetDirectoryService.PreWorldArmorRemovalFactoryBlobs`. Only byte-identical stock installations upgrade to the new 286-source defaults. Player-edited factory-named presets remain intact and the new checkbox retains its OFF default when absent from an older custom preset.
