using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace SurvivalPatchContract;

// Real production WPF ViewModel -> CfgPatchWriterService -> parsed BPatch.
// This DOES NOT load UE, prove original source ZIPs or test gameplay.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    private static string? _keep;
    private static int _passed;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0)
            {
                if (args.Length != 2 || args[0] != "--out")
                    throw new ArgumentException("Usage: SurvivalPatchContract [--out PATH]");
                _keep = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(_keep);
            }

            var cohort = Catalog.Parameters
                .Where(p => p.Id.StartsWith("survival.", StringComparison.Ordinal))
                .OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
            Check(cohort.Length == 54, "Survival catalog no longer contains exactly 54 parameters");

            var vm = new MainViewModel(Catalog);
            var loaded = vm.AllParameters.Select(p => p.Id)
                .ToHashSet(StringComparer.Ordinal);
            var unmounted = cohort.Where(p => !loaded.Contains(p.Id))
                .Select(p => p.Id).ToArray();
            Check(unmounted.SequenceEqual(new[] { "survival.drunknessThresholdScale" }),
                "Survival catalog-to-live-VM exclusions changed: "
                + string.Join(", ", unmounted));
            var live = cohort.Where(p => loaded.Contains(p.Id)).ToArray();
            Check(live.Length == 53, "Expected 53 live Survival source-backed parameters");

            var points = Baseline.EditPoints
                .Where(p => p.ParameterId.StartsWith("survival.", StringComparison.Ordinal))
                .ToArray();
            Check(points.Length == 139, "Survival manifest no longer contains 139 source edit points");
            Check(points.All(p => p.Scope == "baseGame"), "Survival source unexpectedly targets DLC");
            Check(points.Select(p => p.ParameterId).Distinct().Count() == 54,
                "Survival manifest-to-catalog parameter inventory differs");
            Check(points.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal)
                      .SetEquals(cohort.Select(p => p.Id)),
                "Survival catalog-to-manifest parameter mapping differs");

            foreach (var def in live)
            {
                var difficulty = string.IsNullOrEmpty(def.RequiresToggle) ? "Veteran" : "Master";
                Test(def.Id, difficulty);
            }
            // Difficulty-sensitive fields: Easy/Medium/Hard/Stalker SID mapping.
            foreach (var difficulty in new[] { "Novice", "Stalker", "Master" })
            {
                Test("survival.radiationAccumulation", difficulty);
                Test("survival.bleedingSeverity", difficulty);
            }
            foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
                TestVanilla(difficulty);

            foreach (var id in new[] { "survival.starvationDamage",
                "survival.severeFatigueRecoil", "survival.radiationStaminaDrainPenalty" })
                TestDisabled(id);

            TestCombined("Veteran-Consumable-Overlap", "Veteran", new[]
            {
                ("survival.instantStaminaRestore", 1.2),
                ("survival.energyDrinkEffectDuration", 1.3),
                ("survival.energyDrinkStaminaRecoveryStrength", 1.25),
                ("survival.energyDrinkFatigueRelief", 1.4)
            });
            TestCombined("Veteran-Hunger-Fatigue", "Veteran", new[]
            {
                ("survival.hungerMediumStaminaRegen", 0.43),
                ("survival.hungerStrongStaminaRegen", 0.8),
                ("survival.fatigueLowAimPenalty", 0.2),
                ("survival.fatigueMediumAimPenalty", 0.3),
                ("survival.fatigueStrongAimPenalty", 0.45)
            });
            TestCombined("Master-Mechanics-Shared-Prototypes", "Master", new[]
            {
                ("survival.starvationDamage", 4.5),
                ("survival.starvationDamageInterval", 11.0),
                ("survival.severeFatigueRecoil", 0.65),
                ("survival.radiationStaminaDrainPenalty", 0.6)
            });

            Console.WriteLine("SOURCE ONLY (no production VM): survival.drunknessThresholdScale");
            Console.WriteLine($"PASS: {_passed} Survival production BPatch writer scenarios.");
            Console.WriteLine("NOT VERIFIED: original archived CFG values, UE runtime, effects in game.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static void TestVanilla(string difficulty)
    {
        var name = "Vanilla-" + difficulty;
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            Check(result.FileCount == 0 && result.EditCount == 0 && result.ParameterCount == 0,
                name + ": vanilla preset emitted unexpected CFG");
            Check(!Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any(),
                name + ": vanilla output directory contains generated files");
            _passed++;
            Console.WriteLine("PASS " + name + " (no CFG)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    private static void TestDisabled(string id)
    {
        var name = "Veteran-Gated-" + id.Replace(".", "-", StringComparison.Ordinal);
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var child = vm.AllParameters.Single(p => p.Id == id);
            Check(!child.IsControlEnabled, name + ": Master parent unexpectedly ON");
            child.SnapCurrentNumberToMarker(child.ReferenceNumber + 0.1);
            Check(child.IsValueModifiedFromReference() && !child.IsControlEnabled,
                name + ": edited child should remain inactive");
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            Check(result.FileCount == 0 && result.EditCount == 0 && result.ParameterCount == 0,
                name + ": inactive child leaked into CFG writer output");
            Check(!Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any(),
                name + ": inactive child unexpectedly wrote a CFG file");
            _passed++;
            Console.WriteLine("PASS " + name + " (gated value not emitted)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    private static void TestCombined(string label, string difficulty,
        (string Id, double Value)[] changes)
    {
        var output = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var ids = changes.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            Check(ids.Count == changes.Length, label + ": duplicate parameter ID");
            foreach (var (id, value) in changes)
            {
                var p = vm.AllParameters.Single(x => x.Id == id);
                Check(p.IsControlEnabled, label + ": inactive child " + id);
                p.SnapCurrentNumberToMarker(value);
                Check(p.IsValueModifiedFromReference(), label + ": unchanged value " + id);
            }
            var changed = vm.AllParameters.Where(p =>
                p.Id.StartsWith("survival.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference())
                .Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(changed.SetEquals(ids), label + ": linked slider/anchor changed "
                  + string.Join(", ", changed) + ", expected " + string.Join(", ", ids));
            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var points = Baseline.EditPoints.Where(p => ids.Contains(p.ParameterId)
                && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid == sid))
                .ToArray();
            Check(points.Length >= ids.Count, label + ": insufficient CFG source fields");

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, output);
            var files = points.Select(p => ExpectedFile(output, p.File))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var actualFiles = Directory.GetFiles(output, "*TrueCustomDifficulty*",
                SearchOption.AllDirectories);
            Check(result.ParameterCount == ids.Count && result.EditCount == points.Length
                  && result.FileCount == files.Length
                  && actualFiles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(files),
                label + ": extra or missing CFG files, edits, or parameter IDs");

            foreach (var file in files)
            {
                var doc = Parse(File.ReadAllText(file));
                var relevant = points.Where(p => string.Equals(
                    ExpectedFile(output, p.File), file, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var expected = relevant.Select(p => p.Prototype + "/"
                    + (p.NodePath.Count > 0 ? string.Join("/", p.NodePath) + "/" : "")
                    + p.Key).ToHashSet(StringComparer.Ordinal);
                var actual = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in doc.Children.Values)
                    Collect(root, root.Name, actual);
                Check(actual.SetEquals(expected), label + ": merged patch lost or added fields:"
                      + " wanted " + string.Join(", ", expected)
                      + ", got " + string.Join(", ", actual));
                foreach (var point in relevant)
                {
                    var node = doc.Get(point.Prototype);
                    Check(node.Bpatch, label + ": prototype missing bpatch flag");
                    foreach (var segment in point.NodePath)
                    {
                        node = node.Get(segment);
                        Check(node.Bpatch, label + ": nested flag missing at " + segment);
                    }
                    var parameter = vm.AllParameters.Single(p => p.Id == point.ParameterId);
                    Approx(node.Value(point.Key), Expected(parameter, point),
                        label + ": " + point.ParameterId + "/" + point.Key);
                }
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (" + points.Length
                              + " merged source fields)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private static void Test(string id, string difficulty)
    {
        var label = difficulty + "-" + id.Replace(".", "-", StringComparison.Ordinal);
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var par = vm.AllParameters.Single(p => p.Id == id);
            Check(par.IsSlider && par.IsControlEnabled,
                label + ": expected enabled real numeric editor");
            Check(!par.IsValueModifiedFromReference(), label + ": nonvanilla starting value");
            var step = Math.Max(0.01, par.Definition.Slider?.Step ?? 0.1);
            var candidate = par.ReferenceNumber + step;
            if (candidate > par.Maximum)
                candidate = par.ReferenceNumber - step;
            par.SnapCurrentNumberToMarker(candidate);
            Check(par.IsValueModifiedFromReference() && par.IsControlEnabled,
                label + ": slider not active/modified");

            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var points = Baseline.EditPoints.Where(p => p.ParameterId == id
                && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid == sid))
                .ToArray();
            Check(points.Length > 0, label + ": no matching CFG source mapping");
            Check(points.All(p => p.Scope == "baseGame"), label + ": unexpected DLC1 point");

            var allChanged = vm.AllParameters.Where(p =>
                p.Id.StartsWith("survival.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p => p.Id).ToArray();
            Check(allChanged.SequenceEqual(new[] { id }),
                label + ": slider constraint moved another actual parameter: "
                + string.Join(", ", allChanged));

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            var files = points.Select(p => ExpectedFile(path, p.File))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            Check(result.FileCount == files.Length
                  && result.ParameterCount == 1 && result.EditCount == points.Length,
                label + ": incorrect file/parameter/edit counts ("
                + result.FileCount + "/" + result.ParameterCount + "/"
                + result.EditCount + ", wanted " + files.Length + "/1/" + points.Length + ")");
            var actualFiles = Directory.GetFiles(path, "*TrueCustomDifficulty*",
                SearchOption.AllDirectories);
            Check(actualFiles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(files),
                label + ": unrelated/missing output files: " + string.Join(",", actualFiles));

            foreach (var file in files)
            {
                var sourcePoints = points.Where(p =>
                    string.Equals(ExpectedFile(path, p.File), file,
                                  StringComparison.OrdinalIgnoreCase)).ToArray();
                var patch = Parse(File.ReadAllText(file));
                var expectedPaths = sourcePoints.Select(p =>
                    p.Prototype + "/" + (p.NodePath.Count > 0
                        ? string.Join("/", p.NodePath) + "/" : "") + p.Key)
                    .ToHashSet(StringComparer.Ordinal);
                var observedPaths = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in patch.Children.Values)
                    Collect(root, root.Name, observedPaths);
                Check(expectedPaths.SetEquals(observedPaths),
                    label + ": unexpected BPatch paths expected={"
                    + string.Join(",", expectedPaths) + "} actual={"
                    + string.Join(",", observedPaths) + "}");

                foreach (var point in sourcePoints)
                {
                    var node = patch.Get(point.Prototype);
                    Check(node.Bpatch, label + ": prototype missing bpatch flag");
                    foreach (var segment in point.NodePath)
                    {
                        node = node.Get(segment);
                        Check(node.Bpatch, label + ": nested segment missing bpatch: " + segment);
                    }
                    var expected = Expected(par, point);
                    Approx(node.Value(point.Key), expected, label + ": " + point.Prototype + "." + point.Key);
                }
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (" + points.Length + " source fields)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    private static string ExpectedFile(string outRoot, string source)
    {
        var file = Path.GetFileName(source);
        var stem = Path.GetFileNameWithoutExtension(file);
        var parent = Path.GetDirectoryName(source.Replace('/', Path.DirectorySeparatorChar))
                     ?? string.Empty;
        var folder = Path.Combine(outRoot, "Stalker2", "Content", "GameLite", parent);
        return file.EndsWith("Prototypes.cfg", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(folder, stem, stem + "_patch_TrueCustomDifficulty.cfg")
            : Path.Combine(folder, file + "_patch_TrueCustomDifficulty");
    }

    private static double Expected(ParameterViewModel par, CfgWriterEditPoint point)
    {
        return point.Operation switch
        {
            "set" => par.CurrentNumber,
            "negateSet" => -par.CurrentNumber,
            "scale" => ParseNumber(point.SourceRaw) * par.CurrentNumber,
            _ => throw new InvalidDataException(
                point.ParameterId + ": unsupported Survival operation " + point.Operation),
        };
    }

    private static double ParseNumber(string raw)
    {
        var text = raw.Trim();
        var percent = text.EndsWith("%", StringComparison.Ordinal);
        if (percent) text = text[..^1];
        if (text.EndsWith("f", StringComparison.OrdinalIgnoreCase)) text = text[..^1];
        return double.Parse(text, CultureInfo.InvariantCulture) / (percent ? 100 : 1);
    }

    private static void Approx(string raw, double expected, string description)
    {
        var actual = ParseNumber(raw);
        Check(Math.Abs(actual - expected) < 0.0000011,
            description + ": wrong value " + actual + ", expected " + expected);
    }

    private static void Collect(Node node, string prefix, HashSet<string> result)
    {
        foreach (var key in node.Assignments.Keys)
            Check(result.Add(prefix + "/" + key), "Duplicate CFG field " + prefix + "/" + key);
        foreach (var child in node.Children.Values)
            Collect(child, prefix + "/" + child.Name, result);
    }

    private static Node Parse(string cfg)
    {
        var root = new Node("document", "");
        var nodes = new Stack<Node>();
        nodes.Push(root);
        foreach (var raw in cfg.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (line == "struct.end")
            {
                Check(nodes.Count > 1, "Unmatched CFG struct.end");
                nodes.Pop();
                continue;
            }
            var open = Regex.Match(line, @"^(?<name>\S+)\s*:\s*struct\.begin(?<flags>.*)$");
            if (open.Success)
            {
                var n = new Node(open.Groups["name"].Value, open.Groups["flags"].Value);
                Check(nodes.Peek().Children.TryAdd(n.Name, n), "Duplicate CFG node " + n.Name);
                nodes.Push(n);
                continue;
            }
            var value = Regex.Match(line, @"^(?<key>\S+)\s*=\s*(?<value>.*)$");
            Check(value.Success, "Unknown CFG line: " + line);
            Check(nodes.Peek().Assignments.TryAdd(value.Groups["key"].Value,
                                                   value.Groups["value"].Value),
                "Duplicate CFG assignment " + line);
        }
        Check(nodes.Count == 1, "CFG missing closing struct.end");
        return root;
    }

    private static void Check(bool ok, string description)
    {
        if (!ok) throw new InvalidDataException(description);
    }

    private sealed class Node(string name, string flags)
    {
        public string Name { get; } = name;
        public bool Bpatch { get; } = flags.Contains("{bpatch}", StringComparison.Ordinal);
        public Dictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Assignments { get; } = new(StringComparer.Ordinal);
        public Node Get(string name) =>
            Children.TryGetValue(name, out var node) ? node :
            throw new InvalidDataException($"CFG {Name} missing child {name}");
        public string Value(string key) =>
            Assignments.TryGetValue(key, out var value) ? value :
            throw new InvalidDataException($"CFG {Name} missing assignment {key}");
    }
}
