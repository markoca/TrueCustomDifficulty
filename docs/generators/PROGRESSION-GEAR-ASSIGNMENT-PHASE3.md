# Progression gear assignment — phase 3 source-recipe audit

The phase-2 `--patch-preview` output is **not a deployable patch**: changing `ItemPrototypeSID` alone leaves authored weapon-specific nested upgrades and attachments behind. Do not enable it in the CFG writer.

Run `python tools/audit_progression_weapon_recipes.py --cfg '/path/to/GameData/ItemGeneratorPrototypes.cfg' --manifest src/TrueCustomDifficulty/Data/world-weapon-shuffle-manifest.csv` against the extracted original game CFG. The audit resolves each of the 14 explicitly allowlisted generator+node paths and verifies the exact source weapon SID and its nested recipe. It prints per-slot recipe information and flags upgrade/attachment-bearing entries. This is source verification, **not** runtime patch generation.

Production gate: copy or reconstruct the **complete source weapon recipe** (not just its SID), including any ammo and linked gear whose semantics are established; preserve the destination's unrelated stash content, and validate resulting CFG patches against the game's parser. Do not shuffle across physical stash actors without confirmed safe destination mapping. Armor remains excluded pending an approved destination manifest.
