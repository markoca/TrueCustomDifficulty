# 2.1 Trader SELL slot extraction

The 2.1 `TradePrototypes.cfg` source has been joined to the 3,150-leaf staging manifest by exact `(scope, trade SID, root generator SID)`. The resulting CSV `STALKER2_2.1_trader_slot_manifest.csv` is a conversation export, SHA256 `0a01c763796f639084556c45282a5ce91f27f980460ef32923004d0b792a893b`.

- Slot `[0]`: 3,143 item occurrences, condition `ConstTrue`.
- Slot `[1]`: 7 item occurrences, condition `PlayerRankExperienced`.
- No unmatched source root, no duplicate-root multi-slot expansion; output remains exactly 3,150 occurrences.

The slot 1 entries belong to the `TradeTest` profile. Their original condition must not be lost. All four extra 2.1 profiles remain source-backed but excluded from unique-trader UI pending explicit classification.

Reproduce using `python3 tools/enrich_trader_21_slots.py 'STALKER2-2.1 cfg dump.zip' STALKER2_2.1_trader_staging_manifest.csv STALKER2_2.1_trader_slot_manifest.csv`. This tool requires `enrich_trader_21_links.py` as a Python module; the extraction helper is not yet checked into the repo, so the command is **not standalone runnable from a fresh clone** until that dependency is included.

**Status:** slot enrichment is complete as an offline artifact. The executable still uses the 2.0.5 embedded resource. Do not claim the 2.1 production writer is active.
