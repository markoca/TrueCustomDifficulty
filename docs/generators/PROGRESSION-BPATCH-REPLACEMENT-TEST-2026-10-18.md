# Progression gear phase 13 — BPatch replacement safety fixture

Added `tools/test_progression_bpatch_replacement.py`. It models two **distinct** operations on an authored weapon node: recursive field/child merge and explicit whole-node replacement. A merge of a SID-only/new-node patch retains old `Upgrades` and `Attaches`; a complete replacement removes them. The regression locks this safety distinction and rejects duplicate node names.

Run `python tools/test_progression_bpatch_replacement.py`.

**Critical scope:** the merge function is an intentionally conservative **test model**, not a reverse-engineered or verified implementation of Unreal BPatch. These tests do **not** prove that the game's `struct.begin {bpatch}` supports whole-node replacement or child deletion. No production CFG patch, UI, or random stash assignment has been enabled. Until the game loader or a validated BPatch implementation proves how to delete stale children, SID-only replacement remains unsafe for recipes with upgrades or attachments.

Next acceptance test must use a disposable in-game mod containing one controlled weapon-node replacement, verify the game actually loads it, and inspect the resulting item and attachments. Never claim a Python fixture validates Unreal runtime behavior.
