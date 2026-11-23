"""Source contract for direct 10%-step food/drink box Chance controls.

Standalone: python3 tests/test_food_drink_boxes_source_contract.py
Static CFG source test only. Windows/Wine build and in-game roll tests are separate.
"""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "TrueCustomDifficulty"
ROOT_IDS = [
    "loot.foodBoxBranch", "loot.drinkBoxBranch",
    "loot.foodBoxSausage", "loot.foodBoxBread", "loot.foodBoxCanned",
    "loot.foodBoxMilk", "loot.drinkBoxWater", "loot.drinkBoxBeer",
    "loot.drinkBoxVodka", "loot.drinkBoxEnergy",
]
RANKS = ["Experienced", "Veteran", "Master"]
ALL_IDS = ROOT_IDS + [root + rank for root in ROOT_IDS for rank in RANKS]
ROLES = [
    "Anomaly Runner", "Field Medic", "Hardcore+", "Hardcore",
    "Immersive", "Mercenary", "Mutant Hunter", "Outcast",
    "Scavenger", "Smuggler", "Tactical", "Wanderer",
]


class FoodDrinkBoxContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.catalog = json.loads((SOURCE / "Data/parameters.json").read_text())
        cls.baseline = json.loads((SOURCE / "Data/cfg-writer-baseline.json").read_text())
        cls.points = [p for p in cls.baseline["editPoints"]
                      if p["parameterId"] in ALL_IDS]
        cls.params = {p["id"]: p for p in cls.catalog["parameters"]}

    def test_complete_mapping(self):
        self.assertEqual(len(self.params), 496)
        self.assertEqual(self.baseline["supportedParameterCount"], 496)
        self.assertEqual(len(ALL_IDS), 40)
        self.assertEqual(len(self.points), 40)
        self.assertEqual(len({
            (p["file"], p["prototype"], tuple(p["nodePath"]), p["key"])
            for p in self.points
        }), 40)
        for root in ROOT_IDS:
            self.assertEqual(sum(p["parameterId"] == root for p in self.points), 1)
            self.assertEqual(self.params[root]["vanilla"]["All"], 1)
            self.assertEqual(self.params[root]["group"], "Breakable Containers")
            for rank in RANKS:
                pid = root + rank
                self.assertEqual(sum(p["parameterId"] == pid for p in self.points), 1)
                chance = self.params[pid]
                self.assertEqual(chance["display"]["primary"], "percent")
                self.assertEqual(chance["slider"], {
                    "min": 0, "max": 1, "step": 0.1, "stepMode": "absolute"
                })
                source = next(p for p in self.points if p["parameterId"] == pid)
                self.assertAlmostEqual(chance["vanilla"]["All"], float(source["sourceRaw"]))

    def test_native_semantics_no_cross_rank_edits(self):
        for p in self.points:
            self.assertEqual(p["file"], "GameData/ItemGeneratorPrototypes.cfg")
            self.assertEqual(p["nodePath"][:3], ["ItemGenerator", "[0]", "PossibleItems"])
            self.assertNotIn("_Food_Drink_Newbie", p["prototype"])
            base = p["parameterId"] in ROOT_IDS
            self.assertEqual(p["key"], "Weight" if base else "Chance")
            self.assertEqual(p["operation"], "scale" if base else "set")
            self.assertNotIn(p["key"], ("MinCount", "MaxCount"))
            if not base:
                self.assertTrue(0 <= float(p["sourceRaw"]) <= 1)

    def test_all_factory_roles_are_on_native_grid(self):
        for role in ROLES:
            with self.subTest(role=role):
                profile = json.loads((SOURCE / "Presets" / (role + ".json")).read_text())
                values = profile["values"]
                self.assertTrue(all(key in values for key in ALL_IDS))
                self.assertTrue(all(0 <= values[key]["number"] <= 1
                                    for key in ALL_IDS[10:]))
                self.assertTrue(all(abs(values[key]["number"] * 10 -
                                        round(values[key]["number"] * 10)) < 1e-8
                                    for key in ALL_IDS[10:]))
                self.assertGreater(values["loot.foodBoxBranch"]["number"] +
                                   values["loot.drinkBoxBranch"]["number"], 0)

    def test_factory_box_policy_has_independently_authored_ranks(self):
        policy = json.loads((ROOT / "tools/factory_preset_food_drink_boxes_2026_11_23.json").read_text(encoding="utf-8"))
        self.assertEqual(policy["rootIds"], ROOT_IDS)
        self.assertEqual(policy["rankSuffixes"], RANKS)
        self.assertEqual(set(policy["roles"]), set(ROLES))
        mixes = set()
        for name, role in policy["roles"].items():
            profile = json.loads((SOURCE / "Presets" / (name + ".json")).read_text(encoding="utf-8"))
            arrays = [role["newbieWeights"]] + [role["chances"][rank] for rank in RANKS]
            self.assertEqual([len(a) for a in arrays], [10, 10, 10, 10])
            for rank_index, array in enumerate(arrays):
                self.assertGreater(array[0] + array[1], 0)
                if array[0] > 0:
                    self.assertTrue(any(v > 0 for v in array[2:6]))
                if array[1] > 0:
                    self.assertTrue(any(v > 0 for v in array[6:10]))
                for k, root in enumerate(ROOT_IDS):
                    key = root + (RANKS[rank_index - 1] if rank_index else "")
                    self.assertEqual(profile["values"][key], {"kind": "slider", "number": array[k]})
                    if rank_index:
                        self.assertTrue(0 <= array[k] <= 1)
                        self.assertAlmostEqual(array[k] * 10, round(array[k] * 10))
                if rank_index > 1:
                    self.assertGreaterEqual(array[0], arrays[rank_index - 1][0])
                    self.assertGreaterEqual(array[1], arrays[rank_index - 1][1])
            signature = tuple(tuple(a) for a in arrays)
            self.assertNotIn(signature, mixes)
            mixes.add(signature)

    def test_compact_food_drink_layout_contract(self):
        ui = (SOURCE / "Controls/AggregateVisualControl.FoodDrinkBoxes.cs").read_text()
        aggregate = (SOURCE / "Controls/AggregateVisualControl.cs").read_text()
        self.assertIn("var tabLeft = (ActualWidth - (tabWidth * 4.0 + tabGap * 3.0)) / 2.0;", ui)
        self.assertIn("var rankOffset = FoodDrinkRankOffset(ActualWidth);", ui)
        self.assertIn("width < 600.0 ? 24.0 : 0.0", ui)
        self.assertIn("=> 616.0 + FoodDrinkRankOffset(width);", ui)
        self.assertIn("private const double FoodDrinkKnobHeight = 108.0;", ui)
        self.assertIn("private const double FoodDrinkKnobColumnGap = 18.0;", ui)
        self.assertIn("private const double FoodDrinkKnobRowPitch = 122.0;", ui)
        self.assertIn("private const double FoodDrinkKnobBankCap = 384.0;", ui)
        self.assertIn("var column = i % 2;", ui)
        self.assertIn("var row = i / 2;", ui)
        self.assertIn("var left = groupLeft + (half - bank) / 2.0;", ui)
        self.assertIn("new Point(foodLeft + half / 2.0, top + 101.0 + rankOffset)", ui)
        self.assertIn("new Point(drinkLeft + half / 2.0, top + 101.0 + rankOffset)", ui)
        self.assertIn("DrawKnobGrid(food, FoodBoxLabels, FoodPalette, foodLeft);", ui)
        self.assertIn("DrawKnobGrid(drink, DrinkBoxLabels, DrinkPalette, drinkLeft);", ui)
        self.assertIn("dialScale: 0.68,", ui)
        self.assertIn("compactLayout: true", ui)
        self.assertIn("compactLayout ? 61.0 : 97.0", aggregate)
        self.assertIn("compactLayout ? 15.0 : KnobTypography.EndpointInsetFromBottom", aggregate)
        self.assertIn("FoodDrinkBoxesHeight(width)", aggregate)

        # Both banks must be symmetric, remain inside their own halves, and
        # have no overlapping hit circles or labels at responsive widths.
        for width in (520.0, 560.0, 599.0, 600.0, 760.0, 900.0, 1600.0):
            with self.subTest(width=width):
                tab_width, tab_gap, tab_count = 59.0, 5.0, 4
                group = tab_count * tab_width + (tab_count - 1) * tab_gap
                tab_left = (width - group) / 2.0
                self.assertGreaterEqual(tab_left, 0.0)
                self.assertAlmostEqual(tab_left + group / 2.0, width / 2.0)
                self.assertLessEqual(tab_left + group, width)

                outer_margin, outer_gap = 17.0, 10.0
                half = (width - 2 * outer_margin - outer_gap) / 2.0
                bank = min(half - 16.0, 384.0)
                cell = (bank - 18.0) / 2.0
                self.assertGreaterEqual(cell, 102.0)
                food_left = outer_margin
                drink_left = outer_margin + half + outer_gap
                center_gap = drink_left - (food_left + half)
                self.assertAlmostEqual(center_gap, outer_gap)
                for group_left in (food_left, drink_left):
                    left = group_left + (half - bank) / 2.0
                    # Bank, titles, headings and outer branch sliders share
                    # the exact same group center.
                    self.assertAlmostEqual(left + bank / 2.0,
                                           group_left + half / 2.0)
                    for index in range(4):
                        column, row = index % 2, index // 2
                        x = left + column * (cell + 18.0)
                        y = 117.0 + row * 122.0
                        self.assertGreaterEqual(x, group_left)
                        self.assertLessEqual(x + cell, group_left + half)
                        if row == 0:
                            self.assertAlmostEqual(y + 108.0 + 14.0,
                                                   117.0 + 122.0)

                offset = 24.0 if width < 600.0 else 0.0
                tab_bottom = -2.0 + offset + 19.0
                track_top = 21.0 + offset
                self.assertGreaterEqual(track_top - tab_bottom, 4.0)
                first_knob_top = 117.0 + offset
                last_knob_bottom = first_knob_top + 122.0 + 108.0
                preview_caption_top = 363.0 + offset
                preview_bottom = 382.0 + offset + 217.0
                panel_height = 616.0 + offset
                self.assertGreaterEqual(preview_caption_top - last_knob_bottom, 16.0)
                self.assertAlmostEqual(panel_height - preview_bottom, 17.0)

    def test_ui_ranks_and_chance_migration(self):
        ui = (SOURCE / "Controls/AggregateVisualControl.FoodDrinkBoxes.cs").read_text()
        self.assertIn("TrySelectFoodDrinkRank", ui)
        self.assertIn("FoodDrinkBoxPolicy.ChanceId", ui)
        self.assertIn("percentageReadout: _foodDrinkSelectedRank != 0", ui)
        self.assertIn("DrawRoundedRectangle", ui)
        service = (SOURCE / "Services/PresetService.cs").read_text()
        self.assertIn("MigrateFoodDrinkBoxChances(preset)", service)
        self.assertIn("SnapChance(", service)


if __name__ == "__main__":
    unittest.main()
