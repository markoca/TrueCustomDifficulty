# Human NPC NVG Boundary Audit

**Decision:** `NO UI / NO REPEATABLE PRODUCTION SURFACE`

The equipment audit contains five standard-human-classified source edges that reach NVG pools, but none survives the project's current repeatable-production exclusions:

- four DLC edges are `GeneralNPC_MIK_*` loadouts consumed by `GeneralNPC_MykluhaFreedom_*`; these are named Mykluha/MIK loadouts and are hard-excluded from generic-human tuning;
- one base edge is `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom -> GeneralNPC_Neutral_NVG`; the source is a custom/scripted loadout and its audit policy is already `EXCLUDE FROM GENERIC TUNING — scripted/quest`.

Therefore the eligible repeatable generic-human NVG surface is **0 edges**.

The two reachable target pools (`GeneralNPC_Freedom_NVG` and `GeneralNPC_Neutral_NVG`) are cross-context definitions reused by named NPCs, zombies, quests and/or corpse overrides. Their internal `NightVision` item `Chance` rows must not be edited to manufacture a generic-human NVG control.

This closes the former `DEFER_SHARED_POOLS` candidate. A `Human NPC NVG Chance` slider would have no legitimate repeatable-human source boundary after exclusions.
