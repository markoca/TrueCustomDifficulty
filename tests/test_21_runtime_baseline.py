#!/usr/bin/env python3
"""2.1 source-target and legacy-fallback regression; no game ZIP required."""
import json
import sys
import unittest
sys.path.insert(0, str(__import__("pathlib").Path(__file__).resolve().parents[1] / "tools"))
from catalog_census import CATALOG, EDIT_POINTS
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"
SRC = ROOT / "src" / "TrueCustomDifficulty"

class Game21BaselineContract(unittest.TestCase):
    def test_catalog_writer_target_and_shape(self):
        catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
        baseline = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
        self.assertEqual("2.1", catalog["source"]["gameVersion"])
        self.assertEqual("2.1", baseline["gameVersion"])
        self.assertEqual(CATALOG, len(catalog["parameters"]))
        # 1448 + 1234 roaming-NPC weapon-condition leaves (2026-11-16 scope).
        self.assertEqual(EDIT_POINTS, len(baseline["editPoints"]))
        self.assertEqual(30, len(baseline["special"]))

    def test_factory_presets_target_21_and_protect_user_edits(self):
        names = ("Anomaly Runner", "Field Medic", "Hardcore", "Hardcore+",
                 "Immersive", "Mercenary", "Mutant Hunter", "Outcast",
                 "Scavenger", "Smuggler", "Tactical", "Wanderer")
        for name in names:
            preset = json.loads((SRC / "Presets" / (name + ".json")).read_text(encoding="utf-8"))
            self.assertEqual("2.1", preset["gameVersion"], name)
            self.assertEqual(CATALOG, len(preset["values"]), name)
        service = (SRC / "Services" / "PresetDirectoryService.cs").read_text(encoding="utf-8")
        self.assertIn("Pre21FactoryBlobs", service)
        self.assertIn("&& !Pre21FactoryBlobs.TryGetValue(name, out knownHash)", service)
        self.assertIn("actual.Equals(pre21Hash, StringComparison.OrdinalIgnoreCase)", service)

    def test_writer_rejects_old_target(self):
        source = (SRC / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
        self.assertIn('string.Equals(baseline.GameVersion, "2.1"', source)
        self.assertIn('string.Equals(catalog.Source.GameVersion, "2.1"', source)
        self.assertIn("Only the S.T.A.L.K.E.R. 2.1 CFG baseline is supported.", source)

    def test_trader_sells_excludes_old_runtime(self):
        project = (SRC / "TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
        seller = (SRC / "Services" / "CfgPatchWriterService.TraderSells.cs").read_text(encoding="utf-8")
        baseline = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
        resource = "TrueCustomDifficulty.Data.trader-sells-2.1-runtime.csv"
        self.assertIn("trader-sells-2.1-runtime.csv", project)
        self.assertIn(resource, seller)
        self.assertEqual(resource, baseline["special"]["traderSells"]["manifestResource"])
        self.assertNotIn("trader-offers-source-rows.csv", project + seller)

if __name__ == "__main__":
    unittest.main()
