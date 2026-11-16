"""TRADER STOCK: per role x category odds + supply amounts for every trader.

* trader-stock-entries-2.1.csv holds the source Chance/Weight and counts of
  all 608 unique trader offer entries (tools/build_trader_stock_entries_21.py
  from the 2.1 base + DLC1 dumps) and is hash-locked in the writer.
* SELL category: 0% = existing catalog toggle off (entry removed), 5–95% =
  TraderSellOdds sidecar (Chance scaled; the 5 single-entry Weight lists get
  a native Empty pick), 100% = vanilla.
* AMMO SUPPLY / MEDICAL SUPPLY scale MinCount/MaxCount of every Ammo /
  Medical offer of every trader profile in per-trader clones.
"""
import csv
import hashlib
from collections import defaultdict
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / "src" / "TrueCustomDifficulty"
read = lambda rel: (src / rel).read_text(encoding="utf-8")
entries_text = read("Data/trader-stock-entries-2.1.csv").replace("\r\n", "\n")
writer = read("Services/CfgPatchWriterService.TraderSells.cs")
main_writer = read("Services/CfgPatchWriterService.cs")
vm = read("ViewModels/MainViewModel.cs")
sidecar = read("ViewModels/TraderSellOddsViewModel.cs")
preset_service = read("Services/PresetService.cs")
preset_file = read("Models/PresetFile.cs")
ui = read("Controls/TraderBuysMatrix.cs")
xaml = read("MainWindow.xaml")
csproj = read("TrueCustomDifficulty.csproj")

digest = hashlib.sha256(entries_text.encode("utf-8")).hexdigest().upper()
assert f'"{digest}"' in writer, "entry manifest hash not locked in writer"
assert "trader-stock-entries-2.1.csv" in csproj

entries = {(r["generator_scope"], r["generator_sid"], r["entry_cfg_path"]): r
           for r in csv.DictReader(entries_text.splitlines())}
runtime = list(csv.DictReader(read("Data/trader-sells-2.1-runtime.csv").splitlines()))
assert len(entries) == 608
assert sum(1 for e in entries.values() if e["field"] == "Weight") == 5
assert all(e["list_count"] == "1" for e in entries.values() if e["field"] == "Weight")
for r in runtime:
    e = entries[(r["generator_scope"], r["generator_sid"], r["entry_cfg_path"])]
    assert e["item_sid"] == r["item_sid"], r

# Supply reaches EVERY trader profile that sells ammo / medical.
profiles = defaultdict(set)
for r in runtime:
    profiles[r["item_category"]].add((r["trade_scope"], r["trade_sid"]))
assert len(profiles["Ammo"]) == 39 and len(profiles["Medical"]) == 55, (
    len(profiles["Ammo"]), len(profiles["Medical"]))
uncounted = [r for r in runtime if r["item_category"] in ("Ammo", "Medical")
             and not entries[(r["generator_scope"], r["generator_sid"], r["entry_cfg_path"])]["min_count"]]
assert {r["item_sid"] for r in uncounted} == {"AVOG"}  # native default count, left untouched

# Writer
for token in (
    'var ammoFactor = Factor("economy.traderAmmoStock");',
    'var medicalFactor = Factor("economy.medicStock");',
    'var keep = Removed(leaf) ? 0.0 : OddsFor(leaf);',
    'document.AddAssignment(targetClone, emptyPath, "ItemPrototypeSID", "Empty");',
    'ScalePositiveInteger(sourceCount, amount)',
    'result.Count != 608',
):
    assert token in writer, token
assert "vm.TraderSellOdds.Values, hasDlc1, documents, writtenParameters);" in main_writer

# Sidecar plumbing and UI
assert "public TraderSellOddsViewModel TraderSellOdds { get; } = new();" in vm
assert vm.count("TraderSellOdds.Reset();") == 2
assert "TraderSellOdds = TraderSellOdds.ToPreset()," in vm
assert "TraderSellOdds.FromPreset(preset.TraderSellOdds);" in vm
assert "TraderSellOdds.Set(sell.Id, 1.0);" in vm  # toggle off owns 0%
assert '[JsonPropertyName("traderSellOdds")]' in preset_file
assert "Unknown trader stock category" in preset_service
assert "public static IReadOnlyCollection<string> Ids" in sidecar
assert 'SellOdds="{Binding DataContext.TraderSellOdds' in xaml
assert '"100% = GAME ODDS  ·  0% = REMOVED"' in ui

print("PASS: trader stock — 608 hash-locked entry values; category odds via Chance/Empty; ammo supply 39/39, medical supply 55/55 trader profiles")
