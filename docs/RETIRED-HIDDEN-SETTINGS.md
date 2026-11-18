# Retired hidden settings (2026-11-18)

Rule: **nothing may change the game that the player cannot see.**

Four stash settings have no control on any board any more, but older presets still carry values for them:

| ID | Old name |
|---|---|
| `loot.stashMixAmmo` | Ammo Stash Weight |
| `loot.stashMixGrenades` | Grenades Stash Weight |
| `loot.standardStashChanceScale` | Standard Chance Scale |
| `loot.rareStashChanceScale` | Rare Chance Scale |

Behaviour:

* Loading any preset skips these four (`MainViewModel.RetiredHiddenPresetIds`), so they stay at vanilla 1×. An older preset that had them at, say, 0.5× now loads with that part at vanilla. Every visible control still loads exactly.
* Saving writes them at 1×. All factory presets already had 1×, so nothing changes for them.
* The CFG writer still reads them, so the writer contract tests that set them directly keep working; only preset loading ignores them.
* No notice is shown, because the UI stays compact and there is nothing the player could change about it.
* Guarded by `tools/audit_stashes_mixer_ui.py` (*retired hidden stash settings load at vanilla 1×*).
