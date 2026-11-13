# Reproducible 2.1 trader source pipeline

This repository now includes the complete Python-standard-library pipeline to reproduce the source-exact **offline staging data** from the nested 2.1 CFG ZIP:

```bash
python3 tools/extract_trader_21_graph.py 'STALKER2-2.1 cfg dump.zip' graph.csv
python3 tools/enrich_trader_21_links.py 'STALKER2-2.1 cfg dump.zip' graph.csv links.csv
python3 tools/build_trader_21_manifest.py links.csv staging.csv
python3 tools/enrich_trader_21_slots.py 'STALKER2-2.1 cfg dump.zip' staging.csv slots.csv
python3 tools/audit_trader_21_migration.py docs/generators/trader-buy-limits-86.csv staging.csv
```

The migration audit deliberately blocks activation if new 2.1 trade profiles lack reviewed roles. Its nonzero exit status is expected until role review is complete. All tools resolve their sibling imports from the `tools/` directory; there is no dependency on `/mnt/data`.

**Scope:** generator extraction, source parent-link reconstruction, category normalization, source slot/condition enrichment. This is not the final 2.1 production SELL manifest, nor a compiled/writer-output test. Do not delete the existing 2.0.5 embedded manifest until the 2.1 source-backed replacement and tests are ready in the same commit.
