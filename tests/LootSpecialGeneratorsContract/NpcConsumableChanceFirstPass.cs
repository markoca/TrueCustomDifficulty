using System.IO;
using System.Globalization;
using TrueCustomDifficulty.Controls;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    private static void VerifyNpcConsumableChanceFirstPass()
    {
        var sourceCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Bread"] = 8, ["Sausage"] = 8, ["CannedFood"] = 10,
            ["Energetic"] = 12, ["Beer"] = 4, ["Vodka"] = 4,
            ["Bandage"] = 24, ["Medkit"] = 20, ["ArmyMedkit"] = 6, ["AntiRad"] = 8
        };
        var config = Baseline.Special.NpcMedicalLootProduction;
        var sourceRows = config.Pools.SelectMany(pool => pool.TemplateCategories)
            .SelectMany(category => category.Items).ToArray();
        Check(config.Pools.Count == 6 && sourceRows.Length == 104,
            "NPC consumable source 6/104 inventory drifted");
        Check(sourceRows.GroupBy(item => item.Fields["ItemPrototypeSID"])
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal)
            .OrderBy(p => p.Key).SequenceEqual(sourceCounts.OrderBy(p => p.Key)),
            "NPC consumable native SID/count inventory drifted");
        Check(sourceRows.All(item => item.Fields.ContainsKey("Chance")
            && item.Fields.ContainsKey("MinCount")
            && item.Fields.ContainsKey("MaxCount")),
            "NPC consumable source Chance/count fields are incomplete");

        // Guard the companion 1:1 source manifest. A literal Chance of
        // "1.0" must never be silently normalized to "1" on source import.
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        const string chanceCsv = "npc-standard-consumable-original-chances-2.1.csv";
        while (root is not null && !File.Exists(Path.Combine(
                   root.FullName, "docs", "generators", chanceCsv)))
            root = root.Parent;
        Check(root is not null, "Cannot locate the original 104-row NPC Chance CSV");
        string CsvValue(string raw) =>
            raw.Contains(',') || raw.Contains('"') || raw.Contains('\n')
                ? "\"" + raw.Replace("\"", "\"\"") + "\"" : raw;
        var originalRows = new List<string>
        {
            "PoolSID,OriginalCategoryNode,OriginalItemNode,DiffRaw,PlayerRankRaw,"
            + "ItemPrototypeSID,ChanceRaw,MinCountRaw,MaxCountRaw"
        };
        foreach (var pool in config.Pools)
            foreach (var category in pool.TemplateCategories)
                foreach (var item in category.Items)
                    originalRows.Add(string.Join(",", new[]
                    {
                        pool.VanillaSid, category.Node, item.Node,
                        category.Fields["Diff"], category.Fields["PlayerRank"],
                        item.Fields["ItemPrototypeSID"], item.Fields["Chance"],
                        item.Fields["MinCount"], item.Fields["MaxCount"]
                    }.Select(CsvValue)));
        var expectedCsv = string.Join("\n", originalRows) + "\n";
        var originalCsv = File.ReadAllText(Path.Combine(root!.FullName,
            "docs", "generators", chanceCsv)).Replace("\r\n", "\n");
        Check(originalCsv == expectedCsv && originalRows.Count == 105,
            "The 104 source literal NPC Chance entries no longer match the pinned 2.1 baseline");

        var uiVm = new MainViewModel(Catalog);
        var npcBoard = new AggregateVisualControl
        {
            Family = "NpcLootLoadout",
            Parameters = uiVm.AllParameters,
            NpcConsumables = uiVm.NpcConsumableItems
        };
        // All three native options must remeasure without hidden ammo space.
        var smart = uiVm.AllParameters.Single(p => p.Id == "loot.npcCorpseBonus");
        var ammoPackage = "NPC_Medicine_Smart, NPC_Ammo_Smart";
        var waterPackage = "NPC_Water_Smart";
        var nonePackage = string.Empty;
        foreach (var (option, wide, middle, narrow) in new[]
        {
            // Three-tier NPC LOOT board (2026-11-16/17): smart card, modifier row, items.
            (ammoPackage, 510.0, 510.0, 706.0),
            (waterPackage, 510.0, 510.0, 706.0),
            (nonePackage, 510.0, 510.0, 706.0)
        })
        {
            smart.CurrentText = option;
            npcBoard.Measure(new System.Windows.Size(920.0, 1000.0));
            Check(Math.Abs(npcBoard.DesiredSize.Height - wide) < 0.000001
                && ReferenceEquals(npcBoard.NpcConsumables, uiVm.NpcConsumableItems),
                "Wide NPC SmartLoot mode height drifted for " + option);
            npcBoard.Measure(new System.Windows.Size(860.0, 1000.0));
            Check(Math.Abs(npcBoard.DesiredSize.Height - middle) < 0.000001,
                "Medium NPC SmartLoot mode height drifted for " + option);
            npcBoard.Measure(new System.Windows.Size(600.0, 1000.0));
            Check(Math.Abs(npcBoard.DesiredSize.Height - narrow) < 0.000001,
                "Narrow NPC SmartLoot mode height drifted for " + option);
        }
        smart.ResetToReference();

        var npcRationale = uiVm.AllParameters.Single(p => p.Id == "loot.npcCorpseBonus")
            .RationaleDisplay;
        Check(npcRationale.Contains("0–3×, 1× = vanilla", StringComparison.Ordinal)
            && !npcRationale.Contains("100% = 1", StringComparison.Ordinal),
            "NPC corpse Chance rationale must describe the source-relative multiplier");

        // Checked rows multiply each matching native Chance (0–3x, 0.05x
        // grid, clamped to 1), preserving the difficulty / rank progression.
        var cases = new[] { 0.0, 0.05, 0.5, 1.5, 3.0 };
        foreach (var id in sourceCounts.Keys)
            foreach (var factor in cases)
            {
                var vm = new MainViewModel(Catalog);
                vm.SelectedReferenceDifficulty = "Veteran";
                var target = vm.NpcConsumableItems.Single(x => x.Sid == id);
                Check(vm.NpcConsumableItems.Count == 10
                    && vm.NpcConsumableItems.All(x => !x.Enabled
                        && Math.Abs(x.Multiplier - 1.0) < 0.0000001
                        && x.LegacyChancePercent is null
                        && x.PercentText == NpcConsumableItemViewModel.UntouchedText),
                    "Optional NPC Chance controls are not OFF/source-backed by default");
                target.Enabled = true;
                target.Multiplier = factor;
                Check(target.PercentText == factor.ToString("0.##",
                        System.Globalization.CultureInfo.InvariantCulture) + "×"
                    && target.IsEffectiveChange,
                    "A checked NPC consumable row must display its source multiplier");
                var dir = Path.Combine(Path.GetTempPath(), "TCD_NpcConsumables",
                    Guid.NewGuid().ToString("N"));
                try
                {
                    var result = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
                    Check(result.EditCount > 0 && result.FileCount == 1
                        && result.ParameterCount == 1,
                        "Checked NPC Chance multiplier did not generate one CFG: " + id);
                    var output = ReadOutput(dir);
                    CheckNpcConsumableClones(output, id + "/" + factor,
                        target.Sid, factor, 1.0);
                }
                finally
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
                }
            }

        // Snapping and range: drag/keys can never author off-grid values.
        var snap = new MainViewModel(Catalog).NpcConsumableItems.Single(x => x.Sid == "Bread");
        snap.Multiplier = 1.234;
        Check(Math.Abs(snap.Multiplier - 1.25) < 0.0000001, "Multiplier must snap to 0.05x");
        snap.Multiplier = 9.0;
        Check(Math.Abs(snap.Multiplier - 3.0) < 0.0000001, "Multiplier must clamp at 3x");

        // A checked medical multiplier REPLACES MED DROPS for its SID,
        // never multiplies against it.
        var mixed = new MainViewModel(Catalog);
        mixed.SelectedReferenceDifficulty = "Veteran";
        mixed.AllParameters.Single(p => p.Id == "loot.npcMedicalChance")
            .SnapCurrentNumberToMarker(0.5);
        var bread = mixed.NpcConsumableItems.Single(x => x.Sid == "Bread");
        bread.Enabled = true;
        bread.Multiplier = 0;
        var medkit = mixed.NpcConsumableItems.Single(x => x.Sid == "Medkit");
        medkit.Enabled = true;
        medkit.Multiplier = 3.0;
        var bandage = mixed.NpcConsumableItems.Single(x => x.Sid == "Bandage");
        bandage.Enabled = true;
        bandage.Multiplier = 1.0;

        var mixedDir = Path.Combine(Path.GetTempPath(), "TCD_NpcConsumables",
            Guid.NewGuid().ToString("N"));
        try
        {
            var result = new CfgPatchWriterService().Write(mixed, Catalog, null, mixedDir);
            Check(result.FileCount == 1 && result.ParameterCount == 4,
                "Multiple per-SID Chance overrides generated extra files or incorrect logical editor counts");
            var output = ReadOutput(mixedDir);
            CheckNpcConsumableClones(output, "mixed-global-and-SID", null, 1.0, 0.5,
                new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["Bread"] = 0.0, ["Medkit"] = 3.0, ["Bandage"] = 1.0
                });
        }
        finally
        {
            if (Directory.Exists(mixedDir)) Directory.Delete(mixedDir, recursive: true);
        }

        // Disabled overrides keep stored multipliers but leave the entire
        // writer dormant until at least one effective source is changed.
        var disabled = new MainViewModel(Catalog);
        var disabledDrink = disabled.NpcConsumableItems.Single(x => x.Sid == "Beer");
        disabledDrink.Multiplier = 0;
        Check(!disabledDrink.Enabled && !disabledDrink.IsEffectiveChange,
            "Disabled override was incorrectly counted as active");
        var offDir = Path.Combine(Path.GetTempPath(), "TCD_NpcConsumables",
            Guid.NewGuid().ToString("N"));
        try
        {
            var result = new CfgPatchWriterService().Write(disabled, Catalog, null, offDir);
            Check(result.EditCount == 0 && result.FileCount == 0,
                "A disabled NPC consumable multiplier unexpectedly changed the CFG");
        }
        finally
        {
            if (Directory.Exists(offDir)) Directory.Delete(offDir, recursive: true);
        }

        // Round-trip all ten independent sidecars through the *real* preset
        // serializer; older versions omit the dictionary and default to OFF.
        var presetPath = Path.Combine(Path.GetTempPath(),
            "TCD_NpcConsumables_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var file = new PresetFile
            {
                PresetFormatVersion = PresetService.CurrentPresetVersion,
                ReferenceDifficulty = "Veteran",
                NpcConsumableChances = mixed.NpcConsumableItems.ToDictionary(
                    item => item.Sid, item => item.ToPreset(), StringComparer.Ordinal)
            };
            new PresetService().Save(file, presetPath);
            var roundtrip = new PresetService().Load(presetPath);
            Check(roundtrip.NpcConsumableChances.Count == 10
                && roundtrip.NpcConsumableChances["Bread"].Enabled
                && roundtrip.NpcConsumableChances["Bread"].Multiplier == 0
                && roundtrip.NpcConsumableChances["Medkit"].Enabled
                && Math.Abs(roundtrip.NpcConsumableChances["Medkit"].Multiplier!.Value - 3.0) < 0.0000001
                && roundtrip.NpcConsumableChances["Medkit"].ChancePercent is null
                && !roundtrip.NpcConsumableChances["Vodka"].Enabled,
                "NPC Chance multiplier preset roundtrip lost exact values");
        }
        finally
        {
            if (File.Exists(presetPath)) File.Delete(presetPath);
        }

        // Presets saved by the short-lived absolute editor keep writing their
        // literal Chance until the user explicitly moves the control.
        var oldPreset = new NpcConsumableChancePreset
            { Enabled = true, ChancePercent = 40 };
        var legacy = new MainViewModel(Catalog);
        var legacyBread = legacy.NpcConsumableItems.Single(x => x.Sid == "Bread");
        legacyBread.ApplyPreset(oldPreset);
        Check(legacyBread.LegacyChancePercent == 40
            && legacyBread.PercentText == "40%"
            && legacyBread.ToPreset().ChancePercent == 40
            && legacyBread.ToPreset().Multiplier is null,
            "Legacy absolute preset was silently converted to a multiplier");
        var legacyDir = Path.Combine(Path.GetTempPath(), "TCD_NpcConsumables",
            Guid.NewGuid().ToString("N"));
        try
        {
            var result = new CfgPatchWriterService().Write(legacy, Catalog, null, legacyDir);
            Check(result.FileCount == 1 && result.ParameterCount == 1,
                "Legacy absolute Chance no longer generates one compatible CFG");
            CheckNpcConsumableClones(ReadOutput(legacyDir), "legacy-absolute-40",
                "Bread", 0.4, 1.0, absoluteMode: true);
        }
        finally
        {
            if (Directory.Exists(legacyDir)) Directory.Delete(legacyDir, recursive: true);
        }
        legacyBread.Multiplier = 1.5;
        Check(legacyBread.LegacyChancePercent is null && legacyBread.PercentText == "1.5×"
            && legacyBread.ToPreset().ChancePercent is null,
            "Editing an old absolute preset did not switch to the source multiplier");
        legacyBread.Enabled = false;
        Check(legacyBread.PercentText == NpcConsumableItemViewModel.UntouchedText && !legacyBread.IsEffectiveChange,
            "Unchecked NPC Chance must display source mode and remain inert");

        // Rank preview: the profile reproduces the native branch values.
        var profile = NpcConsumableRankProfile.FromBaseline(Baseline);
        var bandageHard = profile.Average("Bandage", "Hard", c => c)!;
        var antiRadHard = profile.Average("AntiRad", "Hard", c => c)!;
        Check(bandageHard.Length == 4
            && Math.Abs(bandageHard[0] - bandageHard[1]) < 0.0000001
            && bandageHard[2] > bandageHard[0]
            && Math.Abs(bandageHard[2] - 1.0) < 0.0000001
            && antiRadHard[0] == 0.0 && antiRadHard[2] > 0.0,
            "Rank preview no longer reflects native Newbie/Experienced vs Veteran/Master branches");
        Check(NpcConsumableRankProfile.DifficultySid("Master") == "Stalker"
            && NpcConsumableRankProfile.DifficultySid("Novice") == "Easy",
            "Rank preview difficulty mapping drifted");

        Console.WriteLine("PASS 50 per-item Chance multipliers + 104 field parity + "
            + "92 scope-limited redirects + medical precedence + legacy absolute migration + rank preview");
    }

    private static void CheckNpcConsumableClones(
        IReadOnlyDictionary<string, Dictionary<string, string>> output,
        string caseName, string? changedSid, double changedChance,
        double globalMedicalFactor, IReadOnlyDictionary<string, double>? extraOverrides = null,
        bool absoluteMode = false)
    {
        Check(output.Count == 1, caseName
            + ": source isolation failed; expected only NPC ItemGenerator CFG");
        var generated = output.TryGetValue(
            "GameData/ItemGeneratorPrototypes.cfg", out var source)
            ? source : throw new InvalidDataException(
                caseName + ": NPC ItemGenerator CFG output is missing");

        var medical = new HashSet<string>(StringComparer.Ordinal)
            { "Bandage", "Medkit", "ArmyMedkit", "AntiRad" };
        var expected = new Dictionary<string, string>(StringComparer.Ordinal);
        var config = Baseline.Special.NpcMedicalLootProduction;
        foreach (var pool in config.Pools)
        {
            var root = pool.CustomSid + "/ItemGenerator/";
            foreach (var cat in pool.TemplateCategories)
            {
                foreach (var item in cat.Items)
                {
                    var prefix = root + cat.Node + "/PossibleItems/" + item.Node + "/";
                    var sid = item.Fields["ItemPrototypeSID"];
                    var native = item.Fields["Chance"];
                    var modified = false;
                    var chance = Num(native);
                    // changedChance / extraOverrides are source multipliers;
                    // absoluteMode = legacy literal Chance for changedSid.
                    if (changedSid == sid)
                    {
                        chance = absoluteMode
                            ? changedChance
                            : Math.Clamp(chance * changedChance, 0.0, 1.0);
                        modified = absoluteMode || changedChance != 1.0;
                    }
                    else if (extraOverrides is not null
                        && extraOverrides.TryGetValue(sid, out var specific))
                    {
                        chance = Math.Clamp(chance * specific, 0.0, 1.0);
                        modified = specific != 1.0;
                    }
                    else if (medical.Contains(sid) && globalMedicalFactor != 1.0)
                    {
                        chance = Math.Clamp(chance * globalMedicalFactor, 0.0, 1.0);
                        modified = true;
                    }
                    Check(generated!.TryGetValue(prefix + "Chance", out var actual)
                        && Math.Abs(Num(actual!) - chance) < 0.0000001,
                        caseName + ": incorrect literal CFG Chance for " + sid);
                    if (!modified)
                        Check(actual == native,
                            caseName + ": unchanged raw Chance was not kept 1:1: " + sid);
                    foreach (var field in item.Fields)
                    {
                        if (field.Key == "Chance") continue;
                        Check(generated!.TryGetValue(prefix + field.Key, out var literal)
                            && literal == field.Value,
                            caseName + ": non-Chance native item field changed: " + sid + "/" + field.Key);
                    }
                    Check(expected.TryAdd(prefix + "Chance", actual!),
                        caseName + ": original source item node collision");
                }
            }
        }
        Check(expected.Count == 104, caseName + ": not all 104 chances were written");
        Check(generated!.Keys.Count(k => k.EndsWith("/Chance", StringComparison.Ordinal)) == 104,
            caseName + ": duplicate/extra item Chance leaves were emitted");
        var manifest = Baseline.Special.NpcMedicalLootProduction.RedirectManifestResource;
        Check(!string.IsNullOrWhiteSpace(manifest),
            caseName + ": NPC human-only redirect manifest missing");
        // Leverage the 92-link native fixture for exact consumer scope.
        CheckNpcMedicalSourceFieldsForRedirects(output, caseName);
    }

    private static void CheckNpcMedicalSourceFieldsForRedirects(
        IReadOnlyDictionary<string, Dictionary<string, string>> output, string name)
    {
        var repo = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repo is not null && !File.Exists(Path.Combine(repo.FullName,
                   "src", "TrueCustomDifficulty", "Data",
                   "npc-medical-production-redirect-manifest.csv")))
            repo = repo.Parent;
        Check(repo is not null, name + ": missing original redirect manifest");
        var rows = File.ReadAllLines(Path.Combine(repo!.FullName, "src",
            "TrueCustomDifficulty", "Data", "npc-medical-production-redirect-manifest.csv"))
            .Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        Check(rows.Length == 92, name + ": normal NPC redirect count changed");
        foreach (var raw in rows)
        {
            var fields = raw.Split(',');
            // DLC1 NORD rows are only emitted on DLC1 builds.
            if (!output.TryGetValue(fields[1], out var generated))
            {
                Check(fields[0] == "dlc1", name + ": missing base NPC redirect document");
                continue;
            }
            var path = fields[2] + "/" + fields[3].Replace(".", "/") + "/" + fields[4];
            Check(generated.TryGetValue(path, out var actual)
                && actual == fields[6] && fields[6] == "TCD_" + fields[5],
                name + ": unexpected human redirect or shared pool change: " + path);
        }
        Check(!output["GameData/ItemGeneratorPrototypes.cfg"].Keys.Any(k => k.StartsWith("GeneralNPC_Consumables_",
                StringComparison.Ordinal)),
            name + ": patched a vanilla shared consumable pool");
    }
}
