# Combat inherited-section runtime acceptance

These are **five importable, separate TCD user preset JSONs** for controlled
testing of a missing-but-inherited CFG section. In the original vanilla
`GameData/DifficultyPrototypes.cfg`, in-game **Stalker** uses CFG `Medium`,
which explicitly declares `{refkey=Empty}` but does **not** locally define
`MutantCombatDifficulty`. In-game **Veteran** uses CFG `Hard`, where the
mutant section is physically authored.

| File | In-game difficulty | UI edit | Expected generated CFG |
| --- | --- | --- | --- |
| Stalker Vanilla.json | Stalker | unchanged | **no patch** |
| Stalker Mutant Damage.json | Stalker | Mutant Hit Damage = 0.5x | `Medium.MutantCombatDifficulty.Mutant_BaseDamage = 0.5` |
| Stalker Mutant Frequency.json | Stalker | Mutant Attack Frequency = 1.5x | `Medium.MutantCombatDifficulty.Mutant_AttackCooldown = 0.666667` |
| Stalker Combined.json | Stalker | both edits | one nested Medium BPatch, both fields |
| Veteran Control.json | Veteran | both edits | same fields under `Hard` (local vanilla section) |

## Safe test protocol

1. **Back up** saves and existing mod output. Isolate the test: remove or
   disable mods that modify GameData/DifficultyPrototypes.cfg. Do not
   overwrite your ordinary gameplay mods; use just **one** test triplet.
2. Publish TCD from the intended Git SHA and open it with Wine. Use the
   application's **Load Preset** action to select a JSON in this folder,
   or copy these five files into `publish/win-x64/Presets/` to list them
   beside user presets. Confirm the actual **Base Preset** shown in TCD.
3. Test Stalker Vanilla first. It should produce no CFG difference.
   To measure real baseline behavior, launch the game **without a test
   mod** on Stalker difficulty. Record the same type of enemy, armor,
   location and a repeatable save/encounter.
4. Load Stalker Mutant Damage (0.5x), **Build Mod**, then copy the
   completed .pak/.utoc/.ucas triplet from `publish/win-x64/Output/`
   somewhere safe: Build Mod regenerates Output. Install the triplet
   using your usual STALKER 2 mod location. Restart the game on
   **Stalker** difficulty and compare damage received per comparable
   hit with the baseline.
5. Remove the prior test triplet. Build/install Stalker Mutant
   Frequency (1.5x) separately. Compare **general** mutant attack gaps,
   preferably from repeated observations/video, not special abilities.
6. Remove the prior test and repeat with Stalker Combined, then
   with Veteran Control **on Veteran difficulty**. Veteran is the
   control where the original Hard CFG has a local MutantCombatDifficulty
   section. Do not compare absolute Stalker/Veteran damage directly:
   their other vanilla values differ.
7. Verify one-test-mod-only, mod loading and no conflicting mod.
   After testing, remove all experimental triplets and restore your
   earlier setup.

The existing Windows CI contract suite reads these same five
files through the production PresetService and ViewModels, generates
real BPatch output and checks that the proper CFG SID, section,
field values, inverse-scale transformation and sparse scope are
correct. **It does not run the game, load the mod or emulate UE
inheritance/merge semantics.**

## Observed outcomes — manually fill

| Test | In-game evidence / version / conditions | Status |
| --- | --- | --- |
| Stalker vanilla reference, no mod | | NOT RUN |
| Stalker Mutant Damage 0.5x | | NOT RUN |
| Stalker Mutant Attack Frequency 1.5x | | NOT RUN |
| Stalker combined | | NOT RUN |
| Veteran combined control | | NOT RUN |

Document the actual commit SHA, game build, mod file names,
packaging and enabled mods, compared saves, screenshots/video and
observations. A generated CFG is not sufficient to mark these PASS.

**Interpretation:** If Veteran works but Stalker fails, investigate
whether the engine merges nested `{bpatch}` into a field inherited from
`Empty`. Do **not** automatically replace the complete inherited
section in production: this may affect other fields and mods.
If both fail, first investigate packaging, active selected game
difficulty and mod conflicts. If both work, that does not automatically
validate Master PSY or special-ability cooldown arrays.
