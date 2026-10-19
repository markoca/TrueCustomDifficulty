#!/usr/bin/env python3
"""Audit physical World Loot removal boundaries against source-owned actor manifests.

This is an offline CFG/ActorSID identity check, not an Unreal runtime test.
"""
import csv
from itertools import combinations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"


def source_sids(filename: str, column: str, expected: int) -> set[str]:
    with (DATA / filename).open(encoding="utf-8", newline="") as handle:
        records = list(csv.DictReader(handle))
    assert len(records) == expected, (filename, len(records), expected)
    ids = [row[column] for row in records]
    assert len(set(ids)) == expected, (filename, "duplicate ActorSID")
    assert all(len(sid) == 32 and set(sid) <= set("0123456789ABCDEF")
               for sid in ids), (filename, "malformed ActorSID")
    return set(ids)


removal_domains = {
    "crate": source_sids("crate-spawn-production-manifest.csv", "actorSid", 3391),
    "F1": source_sids("world-f1-grenade-box-manifest.csv", "actor_sid", 101),
    "loose": source_sids("loose-world-loot-manifest.csv", "actorSid", 7233),
}
protected_domains = {
    "world_weapon": source_sids("world-weapon-direct-manifest.csv", "actor_sid", 362),
    "world_attachment": source_sids(
        "world-attachment-direct-manifest.csv", "actor_sid", 7),
    "world_detector": source_sids(
        "world-detector-direct-manifest.csv", "actor_sid", 7),
    "unique_weapon": source_sids(
        "loose-unique-weapon-shuffle-manifest.csv", "actorSid", 9),
    "protected_stash": source_sids(
        "base-stash-protected-actor-manifest.csv", "ActorSID", 106),
    "stash_bonus_actor": source_sids(
        "stash-gear-actor-ranks.csv", "ActorSID", 915),
}
domains = removal_domains | protected_domains
for (left, left_ids), (right, right_ids) in combinations(domains.items(), 2):
    collisions = left_ids & right_ids
    assert not collisions, (
        left, right, "physical actor collision", sorted(collisions)[:5])

removed = set().union(*removal_domains.values())
all_actors = set().union(*domains.values())
assert len(removed) == 10725, len(removed)
assert len(all_actors) == 12131, len(all_actors)

print("PASS: 10,725 disjoint physical removal actors; 362 regular world weapons, "
      "7 attachments, 7 detectors, 9 unique weapons, 106 protected stashes "
      "and 915 Extra Gear actor identities untouched")
