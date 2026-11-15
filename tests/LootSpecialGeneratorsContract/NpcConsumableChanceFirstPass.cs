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
        npcBoard.Measure(new System.Windows.Size(760.0, 1000.0));
        Check(Math.Abs(npcBoard.DesiredSize.Height - 836.0) < 0.000001
            && ReferenceEquals(npcBoard.NpcConsumables, uiVm.NpcConsumableItems),
            "Ten source-backed item editors did not mount inside the compact NPC board");

        var cases = new[] { 0.0, 0.5, 1.0, 2.0 };
        foreach (var id in sourceCounts.Keys)
            foreach (var fraction in cases)
            {
                var vm = new MainViewModel(Catalog);
                vm.SelectedReferenceDifficulty = "Veteran";
                var target = vm.NpcConsumableItems.Single(x => x.Sid == id);
                Check(vm.NpcConsumableItems.Count == 10
                    && vm.NpcConsumableItems.All(x => !x.Enabled && x.Multiplier == 1.0),
                    "Optional item Chance overrides are not OFF at source");
                target.Enabled = true;
                target.Multiplier = fraction;
                var dir = Path.Combine(Path.GetTempPath(), "TCD_NpcConsumables",
                    Guid.NewGuid().ToString("N"));
                try
                {
                    var result = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
                    if (fraction == 1.0)
                    {
                        Check(result.EditCount == 0 && result.ParameterCount == 0,
                            "1x per-item source chance should not emit a redundant generator");
                        continue;
                    }
                    Check(result.EditCount > 0 && result.FileCount == 1
                        && result.ParameterCount == 1,
                        "Changed NPC item Chance failed to generate a single CFG file: " + id);
                    var output = ReadOutput(dir);
                    CheckNpcConsumableClones(output, id + "/" + fraction,
                        target.Sid, target.Multiplier, 1.0);
                }
                finally
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
                }
            }

        // Independent medical override must REPLACE the global medical
        // multiplier for its SID, not multiply 0.5x by 1.0x or by 2.0x.
        var mixed = new MainViewModel(Catalog);
        mixed.SelectedReferenceDifficulty = "Veteran";
        mixed.AllParameters.Single(p => p.Id == "loot.npcMedicalChance")
            .SnapCurrentNumberToMarker(0.5);
        var bread = mixed.NpcConsumableItems.Single(x => x.Sid == "Bread");
        bread.Enabled = true;
        bread.Multiplier = 0.0;
        var medkit = mixed.NpcConsumableItems.Single(x => x.Sid == "Medkit");
        medkit.Enabled = true;
        medkit.Multiplier = 1.0;
        var bandage = mixed.NpcConsumableItems.Single(x => x.Sid == "Bandage");
        bandage.Enabled = true;
        bandage.Multiplier = 2.0;

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
                    ["Bread"] = 0.0, ["Medkit"] = 1.0, ["Bandage"] = 2.0
                });
        }
        finally
        {
            if (Directory.Exists(mixedDir)) Directory.Delete(mixedDir, recursive: true);
        }

        // Disabled overrides keep stored percentages but leave the entire
        // writer dormant until at least one effective source is changed.
        var disabled = new MainViewModel(Catalog);
        var disabledDrink = disabled.NpcConsumableItems.Single(x => x.Sid == "Beer");
        disabledDrink.Multiplier = 0.0;
        Check(!disabledDrink.Enabled && !disabledDrink.IsEffectiveChange,
            "Disabled override was incorrectly counted as active");
        var offDir = Path.Combine(Path.GetTempPath(), "TCD_NpcConsumables",
            Guid.NewGuid().ToString("N"));
        try
        {
            var result = new CfgPatchWriterService().Write(disabled, Catalog, null, offDir);
            Check(result.EditCount == 0 && result.FileCount == 0,
                "A disabled NPC consumable percentage unexpectedly changed the CFG");
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
                && roundtrip.NpcConsumableChances["Bread"].Multiplier == 0.0
                && roundtrip.NpcConsumableChances["Medkit"].Enabled
                && roundtrip.NpcConsumableChances["Medkit"].Multiplier == 1.0
                && !roundtrip.NpcConsumableChances["Vodka"].Enabled,
                "NPC consumable preset roundtrip lost enabled state or exact 1x");
        }
        finally
        {
            if (File.Exists(presetPath)) File.Delete(presetPath);
        }

        Console.WriteLine("PASS 40 per-item source Chance boundaries + 104 field parity + "
            + "33 scope-limited redirects + medical override precedence + preset sidecars");
    }

    private static void CheckNpcConsumableClones(
        IReadOnlyDictionary<string, Dictionary<string, string>> output,
        string caseName, string? changedSid, double changedFactor,
        double globalMedicalFactor, IReadOnlyDictionary<string, double>? extraOverrides = null)
    {
        Check(output.Count == 1 && output.TryGetValue(
            "GameData/ItemGeneratorPrototypes.cfg", out var generated),
            caseName + ": source isolation failed; expected only NPC ItemGenerator CFG");

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
                    var factor = changedSid == sid ? changedFactor
                        : medical.Contains(sid) ? globalMedicalFactor : 1.0;
                    if (extraOverrides is not null && extraOverrides.TryGetValue(sid, out var specific))
                        factor = specific;
                    var chance = Math.Clamp(Num(native) * factor, 0.0, 1.0);
                    Check(generated!.TryGetValue(prefix + "Chance", out var actual)
                        && Math.Abs(Num(actual!) - chance) < 0.0000001,
                        caseName + ": wrong source-relative Chance for " + sid);
                    if (factor == 1.0)
                        Check(actual == native,
                            caseName + ": original raw Chance was not kept 1:1: " + sid);
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
        // Leverage the preexisting 33-link native fixture for exact consumer scope.
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
        Check(rows.Length == 33, name + ": normal NPC redirect count changed");
        var generated = output["GameData/ItemGeneratorPrototypes.cfg"];
        foreach (var raw in rows)
        {
            var fields = raw.Split(',');
            var path = fields[2] + "/" + fields[3].Replace(".", "/") + "/" + fields[4];
            Check(generated.TryGetValue(path, out var actual)
                && actual == fields[6] && fields[6] == "TCD_" + fields[5],
                name + ": unexpected human redirect or shared pool change: " + path);
        }
        Check(!generated.Keys.Any(k => k.StartsWith("GeneralNPC_Consumables_",
                StringComparison.Ordinal)),
            name + ": patched a vanilla shared consumable pool");
    }
}
