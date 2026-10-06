# v0.9.26.52 — ALIFE group-size radars

- Added an **ALIFE** tab with separate **Human Group Size** and **Mutant Group Size** radars.
- Each radar has Newbie / Experienced / Veteran / Master axes and keeps **1x = vanilla**.
- Human axes scale the Director `EAgentType::Human` `MaxCount`; mutant axes scale all mutant-type `MaxCount` restrictions for that rank while preserving authored zero caps.
- These controls do **not** modify lair capacity, lair respawn, encounter frequency, or world-population counts.

Source repository for True Custom Difficulty.
