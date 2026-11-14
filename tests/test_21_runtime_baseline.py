#!/usr/bin/env python3
"""2.1 source-target and legacy-fallback regression; no game ZIP required."""
import json
import unittest
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
        self.assertEqual(359, len(catalog["parameters"]))
        self.assertEqual(1448, len(baseline["editPoints"]))
        self.assertEqual(25, len(baseline["special"]))

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
