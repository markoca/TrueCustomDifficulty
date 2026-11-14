#!/usr/bin/env python3
"""Offline regressions of 2.1-only added-actor classification."""
from __future__ import annotations
import importlib.util
from pathlib import Path
import unittest

PATH = Path(__file__).resolve().parents[1] / "tools/audit_21_new_actor_scope.py"
spec = importlib.util.spec_from_file_location("new_actor_scope", PATH)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class AddedActorScopeTests(unittest.TestCase):
    def test_travel_is_not_loot(self):
        row = module.classify("base", "A" * 32, {
            "SpawnType": "ESpawnType::FastTravelPoint",
            "LevelName": "WorldMap_WP",
        })
        self.assertEqual(row["classification"], "FAST_TRAVEL_ONLY")

    def test_no_automatic_stash_enrollment(self):
        with self.assertRaisesRegex(ValueError, "unreviewed stash/generator"):
            module.classify("base", "B" * 32, {
                "SpawnType": "ESpawnType::FastTravelPoint",
                "LevelName": "WorldMap_WP",
                "StashPrototypeSID": "AnyStash",
            })

    def test_quest_snork_smartloot_isolated(self):
        row = module.classify("base", "C" * 32, {
            "SpawnType": "ESpawnType::Obj",
            "SpawnedPrototypeSID": "Snork",
            "EnableSmartLootIfPossible": "true",
            "LevelName": "WorldMap_WP/E11_MQ01_LogicLevel_WP",
        })
        self.assertEqual(row["classification"], "QUEST_OR_NON_LOOT_REVIEW")
        with self.assertRaisesRegex(ValueError, "Quest NPC SmartLoot"):
            module.classify("base", "D" * 32, {
                "SpawnType": "ESpawnType::Obj",
                "SpawnedPrototypeSID": "Snork",
                "EnableSmartLootIfPossible": "true",
                "LevelName": "WorldMap_WP",
            })

    def test_source_root_parser_only_top_level(self):
        raw = (b"A" * 32 + b" : struct.begin\n"
               b"   SID = " + b"A" * 32 + b"\n"
               b"   LevelName = WorldMap_WP\n"
               b"   SpawnType = ESpawnType::FastTravelPoint\n"
               b"   Child : struct.begin\n"
               b"      SpawnType = ESpawnType::Obj\n"
               b"   struct.end\n"
               b"struct.end\n")
        rows = list(module.source_actor_blocks(raw))
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0][1]["SpawnType"], "ESpawnType::FastTravelPoint")


if __name__ == "__main__":
    unittest.main()
