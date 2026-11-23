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

    def test_compact_food_drink_layout_contract(self):
        ui = (SOURCE / "Controls/AggregateVisualControl.FoodDrinkBoxes.cs").read_text()
        aggregate = (SOURCE / "Controls/AggregateVisualControl.cs").read_text()
        self.assertIn("var tabLeft = (ActualWidth - (tabWidth * 4.0 + tabGap * 3.0)) / 2.0;", ui)
        self.assertIn("var rankOffset = FoodDrinkRankOffset(ActualWidth);", ui)
        self.assertIn("width < 600.0 ? 24.0 : 0.0", ui)
        self.assertIn("=> 621.0 + FoodDrinkRankOffset(width);", ui)
        self.assertIn("private const double FoodDrinkKnobHeight = 108.0;", ui)
        self.assertIn("private const double FoodDrinkKnobGap = 14.0;", ui)
        self.assertIn("var bank = Math.Min(full, 744.0);", ui)
        self.assertIn("dialScale: 0.68,", ui)
        self.assertIn("compactLayout: true", ui)
        self.assertIn("compactLayout ? 61.0 : 97.0", aggregate)
        self.assertIn("compactLayout ? 15.0 : KnobTypography.EndpointInsetFromBottom", aggregate)
        self.assertIn("FoodDrinkBoxesHeight(width)", aggregate)

        # Check the same math as the renderer at narrow, breakpoint and wide widths.
        # Rank chips must stay centered and in-bounds, all four compact dials
        # must have equal widths/gaps, and measured height must contain preview.
        for width in (520.0, 560.0, 599.0, 600.0, 760.0, 900.0, 1600.0):
            with self.subTest(width=width):
                tab_width, tab_gap, tab_count = 59.0, 5.0, 4
                group = tab_count * tab_width + (tab_count - 1) * tab_gap
                tab_left = (width - group) / 2.0
                self.assertGreaterEqual(tab_left, 0.0)
                self.assertAlmostEqual(tab_left + group / 2.0, width / 2.0)
                self.assertLessEqual(tab_left + group, width)

                inner = width - 34.0
                bank = min(inner, 744.0)
                gap = 14.0
                cell = (bank - 3.0 * gap) / 4.0
                self.assertGreaterEqual(cell, 0.0)
                left = (width - bank) / 2.0
                for i in range(4):
                    x = left + i * (cell + gap)
                    self.assertGreaterEqual(x, 0.0)
                    self.assertLessEqual(x + cell, width)
                    if i < 3:
                        self.assertAlmostEqual(
                            left + (i + 1) * (cell + gap) - (x + cell), gap)

                offset = 24.0 if width < 600.0 else 0.0
                tab_bottom = -2.0 + offset + 19.0
                track_top = 21.0 + offset
                self.assertGreaterEqual(track_top - tab_bottom, 4.0)
                food_knob_bottom = 117.0 + offset + 108.0
                drink_caption_top = 234.0 + offset
                drink_knob_bottom = 250.0 + offset + 108.0
                preview_caption_top = 368.0 + offset
                preview_bottom = 387.0 + offset + 217.0
                panel_height = 621.0 + offset
                self.assertGreaterEqual(drink_caption_top - food_knob_bottom, 9.0)
                self.assertGreaterEqual(preview_caption_top - drink_knob_bottom, 10.0)
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
