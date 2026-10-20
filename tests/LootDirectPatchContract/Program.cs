using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootDirectPatchContract;

// Direct loot only: 320 manifest points, not special production/shuffle/actor writers.
// Exercises the actual WPF MainViewModel and production CFG writer.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    private static readonly string[] Difficulties = { "Novice", "Stalker", "Veteran", "Master" };
    private static readonly string[] Mutants =
    {
        "loot.mutant.blindDogChance", "loot.mutant.bloodsuckerChance",
        "loot.mutant.boarChance", "loot.mutant.burerChance",
        "loot.mutant.catChance", "loot.mutant.chimeraChance",
        "loot.mutant.controllerChance", "loot.mutant.deerChance",
        "loot.mutant.fleshChance", "loot.mutant.poltergeistChance",
        "loot.mutant.pseudodogChance", "loot.mutant.pseudogiantChance",
        "loot.mutant.snorkChance", "loot.mutant.tushkanChance"
    };
    private const string Money = "loot.expensiveStashMoneyChance";
    private const string Condition = "loot.droppedWeaponCondition";
    private const string Source = "GameData/ItemGeneratorPrototypes.cfg";
    private static string? Keep;
    private static int Passed;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0)
            {
                if (args.Length != 2 || args[0] != "--out")
                    throw new ArgumentException("Usage: LootDirectPatchContract [--out PATH]");
                Keep = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(Keep);
            }
            VerifyCatalogAndManifest();
            foreach (var difficulty in Difficulties)
                Case(difficulty, new(), "vanilla");

            // Every one of the 14 source-backed mutant parts on each of
            // the four real reference difficulties.
            foreach (var id in Mutants)
                foreach (var difficulty in Difficulties)
                    Case(difficulty, new() { [id] = ChangeFromVanilla(difficulty, id) }, "mutant");

            // Actual boundary behavior for each mutant, including vanilla
            // 100%-chance species for which 100% is an unchanged source.
            foreach (var id in Mutants)
            {
                var reference = GetParameter("Veteran", id).ReferenceNumber;
                Case("Veteran", new() { [id] = 0 }, "zero-chance");
                Case("Veteran", new() { [id] = reference < 1.0 ? 1.0 : 0.75 }, "large-chance");
            }

            // Four difficulty-specific Rare Stash reward cohorts.
            foreach (var difficulty in Difficulties)
            {
                Case(difficulty, new() { [Money] = 0 }, "no-coupons");
                Case(difficulty, new() { [Money] = 1 }, "guaranteed-coupons");
            }

            // One authored 35%-45% outlier is retained at vanilla. When
            // changed, only the selected MIN/MAX bounds are written.
            foreach (var difficulty in Difficulties)
            {
                Case(difficulty, new() { [Condition] = (0.35, 0.50) }, "condition-min");
                Case(difficulty, new() { [Condition] = (0.25, 0.75) }, "condition-max");
                Case(difficulty, new() { [Condition] = (0.15, 0.90) }, "condition-both");
                Case(difficulty, new() { [Condition] = (0.00, 1.00) }, "condition-extremes");
            }

            Case("Veteran", new() {
                ["loot.mutant.blindDogChance"] = 0.35,
                ["loot.mutant.bloodsuckerChance"] = 0.75 }, "two-mutants");
            Case("Veteran", new() {
                [Money] = 0.65,
                ["loot.mutant.boarChance"] = 0.45 }, "money-mutant");
            Case("Veteran", new() {
                [Condition] = (0.40, 0.85),
                ["loot.mutant.fleshChance"] = 0.65 }, "condition-mutant");
            Case("Veteran", new() {
                [Money] = 0.60,
                [Condition] = (0.55, 0.95) }, "money-condition");
            Case("Veteran", new() {
                [Money] = 0.80,
                [Condition] = (0.45, 0.90),
                ["loot.mutant.tushkanChance"] = 0.50 }, "all-three-domains");

            Check(Passed == 117, "Expected 117 production loot-direct scenarios; got " + Passed);
            Console.WriteLine("PASS: 117 Loot DIRECT production BPatch writer scenarios");
            Console.WriteLine("NOT VERIFIED BY THIS SUITE: special loot production writers, Unreal merging and actual gameplay");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static void VerifyCatalogAndManifest()
    {
        var catalog = Catalog.Parameters.Where(x => x.Id.StartsWith("loot.", StringComparison.Ordinal))
            .Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var live = new MainViewModel(Catalog).AllParameters
            .Where(x => x.Id.StartsWith("loot.", StringComparison.Ordinal))
            .Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var source = Baseline.EditPoints.Where(x => x.ParameterId.StartsWith("loot.", StringComparison.Ordinal))
            .ToArray();
        Check(catalog.Count == 76
            && Catalog.Parameters.Count(p => p.Tab == "loot") == 35
            && Catalog.Parameters.Count(p => p.Tab == "stashes") == 41,
            "Expected 76 loot.* IDs (35 Loot plus 41 Stashes), not just the Loot tab");
        Check(source.Length == 320, "Expected 320 direct Loot source points");
        var directIds = Mutants.Concat(new[] { Money, Condition }).ToHashSet(StringComparer.Ordinal);
        Check(source.Select(x => x.ParameterId).ToHashSet(StringComparer.Ordinal).SetEquals(directIds),
            "Loot direct ID inventory changed (special writer entries cannot be treated as direct)");
        Check(directIds.IsSubsetOf(catalog) && directIds.IsSubsetOf(live),
            "Mapped Loot source not present in actual UI-backed ViewModel");
        Check(source.All(x => x.Scope == "baseGame" && x.File == Source),
            "Unexpected DLC source in active direct loot manifest");
        Check(source.Count(x => Mutants.Contains(x.ParameterId, StringComparer.Ordinal)
            && x.Key == "Chance" && x.Operation == "set") == 14,
            "Expected 14 independent source mutant drop chances");
        Check(source.Count(x => x.ParameterId == Money && x.Key == "Chance"
            && x.Operation == "set" && !string.IsNullOrWhiteSpace(x.DifficultySid)) == 64,
            "Expected 64 difficulty-gated Rare Stash reward entries");
        Check(source.Where(x => x.ParameterId == Money).GroupBy(x => x.DifficultySid)
            .Count() == 4 &&
            source.Where(x => x.ParameterId == Money).GroupBy(x => x.DifficultySid)
            .All(g => g.Count() == 16),
            "Expected 16 vanilla coupons per difficulty SID");
        Check(source.Count(x => x.ParameterId == Condition && x.Operation == "setRangeBound"
            && x.Key is "MinDurability" or "MaxDurability") == 242,
            "Expected 242 audited NPC weapon durability bounds");
        var weaponFields = source.Where(x => x.ParameterId == Condition).ToArray();
        Check(weaponFields.Select(x => x.Prototype).Distinct(StringComparer.Ordinal).Count() == 33,
            "Expected 33 audited repeatable-human generator roots");
        Check(weaponFields.GroupBy(x => (x.Prototype, string.Join("/", x.NodePath))).Count() == 121
            && weaponFields.GroupBy(x => (x.Prototype, string.Join("/", x.NodePath))).All(g =>
                g.Count() == 2 && g.Select(p => p.Key).ToHashSet(StringComparer.Ordinal)
                    .SetEquals(new[] { "MinDurability", "MaxDurability" })),
            "Expected 121 independent Min/Max weapon rows");
        Check(weaponFields.All(x => x.Role == "production-repeatable-human"),
            "NPC weapon condition restriction drifted outside repeatable humans");
        Check(weaponFields.Count(x => x.Key == "MinDurability" && x.SourceRaw == "0.35") == 1
            && weaponFields.Count(x => x.Key == "MaxDurability" && x.SourceRaw == "0.45") == 1,
            "Original special NPC weapon-condition 35-45% outlier changed");
    }

    private static ParameterViewModel GetParameter(string difficulty, string id) =>
        new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty }
        .AllParameters.Single(x => x.Id == id);

    private static double ChangeFromVanilla(string difficulty, string id)
    {
        var reference = GetParameter(difficulty, id).ReferenceNumber;
        return reference + 0.05 <= 1.0 ? reference + 0.05 : reference - 0.05;
    }

    private static void Case(string difficulty, Dictionary<string, object> selected, string type)
    {
        // The dictionary stores numeric scalar or (min,max) tuples.
        var outRoot = Path.Combine(Keep ?? Path.GetTempPath(), "TCD_LootDirect_Patch",
            $"{difficulty}-{type}-{Passed}");
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            foreach (var (id, target) in selected)
            {
                var parameter = vm.AllParameters.Single(x => x.Id == id);
                Check(parameter.IsControlEnabled && !parameter.IsValueModifiedFromReference(),
                    "Loot direct control must be live and vanilla: " + id);
                if (target is ValueTuple<double,double> range)
                {
                    Check(parameter.IsRange, id + ": expected real NPC weapon condition range");
                    parameter.SnapCurrentMinNumberToMarker(range.Item1);
                    parameter.SnapCurrentMaxNumberToMarker(range.Item2);
                    Check(Math.Abs(parameter.CurrentMinNumber - range.Item1) < 0.000001
                        && Math.Abs(parameter.CurrentMaxNumber - range.Item2) < 0.000001,
                        id + ": range setter clamped or unexpectedly moved bounds");
                }
                else
                {
                    Check(parameter.IsSlider, id + ": expected real scalar");
                    var targetNumber = Convert.ToDouble(target, CultureInfo.InvariantCulture);
                    parameter.SnapCurrentNumberToMarker(targetNumber);
                    Check(Math.Abs(parameter.CurrentNumber - targetNumber) < 0.000001,
                        id + ": slider setter failed");
                }
                Check(parameter.IsValueModifiedFromReference(), id + ": no source edit");
            }
            var current = vm.AllParameters.Where(p => p.Id.StartsWith("loot.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(current.SetEquals(selected.Keys), type + ": accidentally changed unrelated Loot backing");

            // Rare Coupon Chance belongs to the current Stash Content Mix
            // aggregate, not the retired Stash Reward Profile. Its change
            // highlights the actual anchor, without editing the independent
            // Basic/Standard/Rare Ammo Quantity backing or creating ammo clones.
            if (selected.ContainsKey(Money))
            {
                var anchor = vm.AllParameters.Single(p => p.Id == "loot.stashMixMedical");
                var ammo = vm.AllParameters.Single(p => p.Id == "loot.cheapStashAmmoQuantity");
                Check(anchor.IsVisualAggregateAnchor && anchor.IsModified
                      && !anchor.IsValueModifiedFromReference()
                      && ammo.IsVisualAggregateMember
                      && !ammo.IsModified && !ammo.IsValueModifiedFromReference(),
                    "Rare Coupon Chance must highlight only the current Stash Content Mix aggregate");
            }

            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var live = vm.AllParameters.ToDictionary(x => x.Id, StringComparer.Ordinal);
            var points = Baseline.EditPoints
                .Where(p => selected.ContainsKey(p.ParameterId)
                    && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid == sid))
                .Where(p => Math.Abs(Expected(live[p.ParameterId], p)
                    - ParseNumber(p.SourceRaw)) >= 0.0000001)
                .ToArray();

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, outRoot);
            var expectedFiles = points.Length == 0 ? Array.Empty<string>()
                : new[] { OutputPath(outRoot, Source) };
            var actualFiles = Directory.GetFiles(outRoot, "*TrueCustomDifficulty*", SearchOption.AllDirectories);
            Check(actualFiles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(expectedFiles),
                type + ": unexpected files / missing primary direct output: actual=[" +
                string.Join(";", actualFiles) + "] expected=[" +
                string.Join(";", expectedFiles) + "] counts=" +
                result.FileCount + "/" + result.ParameterCount + "/" + result.EditCount +
                " expectedPoints=" + points.Length);
            Check(result.FileCount == expectedFiles.Length
                && result.EditCount == points.Length && result.ParameterCount == selected.Count,
                type + ": wrong emitted file/parameter/edit counts "
                + result.FileCount + "/" + result.ParameterCount + "/" + result.EditCount
                + ", expected " + expectedFiles.Length + "/" + selected.Count + "/" + points.Length);

            if (points.Length > 0)
            {
                var paths = ParseOutput(File.ReadAllText(expectedFiles[0]));
                var wanted = points.Select(p => p.Prototype + "/" + string.Join("/", p.NodePath)
                    + "/" + p.Key).ToHashSet(StringComparer.Ordinal);
                Check(paths.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(wanted),
                    type + ": missing/extra direct writer leaf assignment");
                foreach (var point in points)
                {
                    var key = point.Prototype + "/" + string.Join("/", point.NodePath) + "/" + point.Key;
                    Check(paths.TryGetValue(key, out var emitted), "Missing " + key);
                    var expected = Expected(live[point.ParameterId], point);
                    Check(Math.Abs(ParseNumber(emitted!) - expected) < 0.0000011,
                        key + ": numeric value differs, expected " + expected + ", emitted " + emitted);
                }
            }
            Console.WriteLine($"PASS {difficulty}-{type} ({points.Length} emitted direct leaves)");
            Passed++;
        }
        finally
        {
            if (Keep is null && Directory.Exists(outRoot))
                Directory.Delete(outRoot, recursive: true);
        }
    }

    private static double Expected(ParameterViewModel parameter, CfgWriterEditPoint p) =>
        p.Operation switch
        {
            "set" => parameter.CurrentNumber,
            "setRangeBound" => p.Key == "MinDurability"
                ? Math.Clamp(parameter.CurrentMinNumber, 0, 1)
                : Math.Clamp(parameter.CurrentMaxNumber, 0, 1),
            _ => throw new InvalidDataException("Unexpected direct Loot writer operation: " + p.Operation)
        };

    private static string OutputPath(string root, string source)
    {
        var file = Path.GetFileName(source);
        var stem = Path.GetFileNameWithoutExtension(source);
        var folder = Path.GetDirectoryName(source.Replace('/', Path.DirectorySeparatorChar)) ?? "";
        return Path.Combine(root, "Stalker2", "Content", "GameLite", folder,
            stem, stem + "_patch_TrueCustomDifficulty.cfg");
    }

    private static double ParseNumber(string raw)
    {
        var t = raw.Trim();
        if (t.EndsWith("%", StringComparison.Ordinal))
            return double.Parse(t[..^1], CultureInfo.InvariantCulture) / 100.0;
        if (t.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            t = t[..^1];
        return double.Parse(t, CultureInfo.InvariantCulture);
    }

    private static Dictionary<string, string> ParseOutput(string text)
    {
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        var nodes = new HashSet<string>(StringComparer.Ordinal);
        var stack = new List<string>();
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (line == "struct.end")
            {
                Check(stack.Count > 0, "Too many struct.end");
                stack.RemoveAt(stack.Count-1);
                continue;
            }
            var open = Regex.Match(line, @"^(?<n>\S+)\s*:\s*struct\.begin(?<flags>.*)$");
            if (open.Success)
            {
                Check(open.Groups["flags"].Value.Contains("{bpatch}", StringComparison.Ordinal),
                    "Missing {bpatch} for " + line);
                stack.Add(open.Groups["n"].Value);
                Check(nodes.Add(string.Join("/", stack)),
                    "Duplicate nested CFG wrapper");
                continue;
            }
            var value = Regex.Match(line, @"^(?<k>\S+)\s*=\s*(?<v>.*)$");
            Check(value.Success && stack.Count > 0, "Unknown BPatch line " + line);
            var key = string.Join("/", stack) + "/" + value.Groups["k"].Value;
            Check(output.TryAdd(key, value.Groups["v"].Value),
                "Duplicate emitted BPatch leaf " + key);
        }
        Check(stack.Count == 0, "Unclosed CFG BPatch nodes");
        return output;
    }
    private static void Check(bool ok, string description)
    {
        if (!ok) throw new InvalidDataException(description);
    }
}
