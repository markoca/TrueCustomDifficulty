"""Structural contract for rank-specific 2.1 food/drink destructible controls.

Run: python3 tests/test_food_drink_boxes_source_contract.py
This verifies repo sources, not in-game RNG or Windows compilation.
"""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "TrueCustomDifficulty"
IDS = [
    "loot.foodBoxBranch", "loot.drinkBoxBranch",
    "loot.foodBoxSausage", "loot.foodBoxBread",
    "loot.foodBoxCanned", "loot.foodBoxMilk",
    "loot.drinkBoxWater", "loot.drinkBoxBeer",
    "loot.drinkBoxVodka", "loot.drinkBoxEnergy",
]
ROLES = [
    "Anomaly Runner", "Field Medic", "Hardcore+", "Hardcore",
    "Immersive", "Mercenary", "Mutant Hunter", "Outcast",
    "Scavenger", "Smuggler", "Tactical", "Wanderer",
]


class BoxContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.catalog = json.loads((SOURCE / "Data/parameters.json").read_text())
        cls.baseline = json.loads((SOURCE / "Data/cfg-writer-baseline.json").read_text())
        cls.points = [p for p in cls.baseline["editPoints"] if p["parameterId"] in IDS]

    def test_complete_and_unique_source_coverage(self):
        params = {p["id"]: p for p in self.catalog["parameters"]}
        self.assertEqual(len(params), 466)
        self.assertEqual(self.baseline["supportedParameterCount"], 466)
        self.assertEqual(len(self.points), 40)
        self.assertEqual(len({
            (p["prototype"], tuple(p["nodePath"]), p["key"]) for p in self.points
        }), 40)
        for pid in IDS:
            self.assertIn(pid, params)
            self.assertEqual(sum(p["parameterId"] == pid for p in self.points), 4)
            self.assertEqual(params[pid]["vanilla"]["All"], 1)
            self.assertEqual(params[pid]["group"], "Breakable Containers")

    def test_native_semantics_and_safety_boundary(self):
        for p in self.points:
            self.assertEqual(p["file"], "GameData/ItemGeneratorPrototypes.cfg")
            self.assertEqual(p["nodePath"][:3], ["ItemGenerator", "[0]", "PossibleItems"])
            self.assertNotIn("_Food_Drink_Newbie", p["prototype"])
            base = not p["prototype"].endswith(("_Experienced", "_Veteran", "_Master"))
            self.assertEqual(p["key"], "Weight" if base else "Chance")
            self.assertEqual(p["operation"], "scale" if base else "scaleClamp01")
            if not base:
                self.assertTrue(0 <= float(p["sourceRaw"]) <= 1)
            self.assertNotIn(p["key"], ("MinCount", "MaxCount"))

    def test_factory_profiles(self):
        for role in ROLES:
            with self.subTest(role=role):
                data = json.loads((SOURCE / "Presets" / (role + ".json")).read_text())
                nums = [data["values"][key]["number"] for key in IDS]
                self.assertTrue(all(0 <= v <= (2 if i < 2 else 3)
                                    for i, v in enumerate(nums)))
                self.assertGreater(nums[0] + nums[1], 0)
                if nums[0] > 0:
                    self.assertGreater(sum(nums[2:6]), 0)
                if nums[1] > 0:
                    self.assertGreater(sum(nums[6:]), 0)


if __name__ == "__main__":
    unittest.main()
