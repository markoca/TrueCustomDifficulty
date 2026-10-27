using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace EconomyPatchContract;

// Executes production ViewModels and CFG writer. Does not execute Unreal/gameplay.
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
                    throw new ArgumentException("Usage: EconomyPatchContract [--out PATH]");
                _keep = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(_keep);
            }
            var defs = Catalog.Parameters.Where(p => p.Id.StartsWith("economy.", StringComparison.Ordinal))
                .OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
            Check(defs.Length == 23, "Expected 23 Economy catalog definitions");
            var active = new MainViewModel(Catalog).AllParameters.Select(p => p.Id)
                .ToHashSet(StringComparer.Ordinal);
            Check(defs.Where(p => !active.Contains(p.Id)).Select(p => p.Id)
                .SequenceEqual(new[] { "economy.repairDifficultyCost" }),
                "Unexpected source-only Economy parameters");
            var points = Baseline.EditPoints.Where(p => p.ParameterId.StartsWith("economy.", StringComparison.Ordinal)).ToArray();
            Check(points.Length == 409, "Expected 409 direct Economy fields");
            Check(points.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal)
                .SetEquals(defs.Where(p => p.Id != "economy.traderMinBuyCondition").Select(p => p.Id)),
                "Economy catalog / manifest mismatch");
            Check(points.Count(p => p.Scope == "dlc1") == 1, "Economy direct DLC1 count drift");
            Check(points.All(p => new[] { "set", "scale", "scaleNonZero", "scaleIntegerHalfUp" }
                .Contains(p.Operation, StringComparer.Ordinal)), "Unsupported operation in Economy direct manifest");

            foreach (var def in defs.Where(p => active.Contains(p.Id) && p.Id != "economy.traderMinBuyCondition"))
                TestDirect(def.Id, "Veteran");
            foreach (var id in new[] { "economy.buyCondition", "economy.sellCondition",
                         "economy.mainQuestReward", "economy.sideQuestReward" })
                foreach (var difficulty in new[] { "Novice", "Stalker", "Master" })
                    TestDirect(id, difficulty);
            TestTraderThreshold();
            TestLinkedRoamingGuides();
            Check(_passed == 39, "Expected exactly 39 Economy writer scenarios");
            Console.WriteLine($"PASS: {_passed} Economy production BPatch writer scenarios");
            Console.WriteLine("NOT VERIFIED: original CFG archives, DLC1-installed patches or game behavior");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static void TestDirect(string id, string difficulty)
    {
        var label = difficulty + "-" + id.Replace(".", "-", StringComparison.Ordinal);
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var par = vm.AllParameters.Single(p => p.Id == id);
            Check(par.IsSlider && par.IsControlEnabled && !par.IsValueModifiedFromReference(),
                label + ": expected live enabled vanilla slider");
            var step = Math.Max(0.01, par.Definition.Slider?.Step ?? 0.01);
            var value = id is "economy.medicStock" or "economy.traderAmmoStock"
                ? 3.0 : par.ReferenceNumber + step;
            if (value > par.Maximum) value = par.ReferenceNumber - step;
            par.SnapCurrentNumberToMarker(value);
            Check(par.IsValueModifiedFromReference() && par.IsControlEnabled,
                label + ": cannot change real slider");
            Check(vm.AllParameters.Where(p => p.Id.StartsWith("economy.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p => p.Id)
                .SequenceEqual(new[] { id }), label + ": unrelated edited source values");
            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var allPoints = Baseline.EditPoints.Where(p => p.ParameterId == id
                && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid == sid)
                && p.Scope != "dlc1").ToArray();
            Check(allPoints.Length > 0, label + ": no base-game source");
            var changed = allPoints.Where(p => Math.Abs(Expected(par, p) - ParseNumber(p.SourceRaw)) >= 0.0000001).ToArray();
            Check(changed.Length > 0, label + ": mutation cannot emit a different value");
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            var files = changed.Select(p => ExpectedFile(path, p.File))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check(result.FileCount == files.Count && result.ParameterCount == 1 && result.EditCount == changed.Length,
                label + ": file/parameter/edit count mismatch: "
                + result.FileCount + "/" + result.ParameterCount + "/" + result.EditCount
                + " expected " + files.Count + "/1/" + changed.Length);
            Check(Directory.GetFiles(path, "*TrueCustomDifficulty*", SearchOption.AllDirectories)
                .ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(files),
                label + ": output includes unexpected or missing CFG files");
            foreach (var file in files)
            {
                var entries = changed.Where(p => string.Equals(ExpectedFile(path,p.File),file,
                    StringComparison.OrdinalIgnoreCase)).ToArray();
                var patch = Parse(File.ReadAllText(file));
                var wanted = entries.Select(p => p.Prototype + "/" + (p.NodePath.Count>0
                    ? string.Join("/", p.NodePath)+"/" : "") + p.Key).ToHashSet(StringComparer.Ordinal);
                var got = new HashSet<string>(StringComparer.Ordinal);
                foreach (var node in patch.Children.Values) Collect(node,node.Name,got);
                Check(wanted.SetEquals(got), label + ": incorrect/unrelated BPatch leaf paths");
                foreach (var entry in entries)
                {
                    var node = patch.Get(entry.Prototype);
                    Check(node.Bpatch, label + ": missing root {bpatch}");
                    foreach (var segment in entry.NodePath)
                    {
                        node = node.Get(segment);
                        Check(node.Bpatch, label + ": missing nested {bpatch}");
                    }
                    Approx(node.Value(entry.Key), Expected(par,entry), label+": "+entry.Prototype+"."+entry.Key);
                }
            }
            _passed++;
            Console.WriteLine("PASS "+label+" ("+changed.Length+" emitted fields)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path)) Directory.Delete(path,recursive:true);
        }
    }

    private static void TestLinkedRoamingGuides()
    {
        Check(RoamingGuideSourceDetector.ContainsRoamingGuideSource(
            "../../../Stalker2/Content/GameLite/GameData/FastTravelEconomyPrototypes.cfg.bin"),
            "2.1 CFG feature probe did not find source");
        Check(!RoamingGuideSourceDetector.ContainsRoamingGuideSource(
            "../../../Stalker2/Content/GameLite/GameData/FastTravelPrototypes.cfg.bin"),
            "Legacy fast travel source falsely matched 2.1 feature probe");
        foreach (var (scale, hasSource) in new[] { (0.0, true), (0.5, true),
                 (1.0, true), (2.0, true), (0.5, false) })
        {
            var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_RoamingGuide",
                scale.ToString(CultureInfo.InvariantCulture) + "_" + hasSource);
            try
            {
                var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
                vm.AllParameters.Single(p => p.Id == "economy.fastTravelCost")
                    .SnapCurrentNumberToMarker(scale);
                var result = new CfgPatchWriterService().Write(vm, Catalog,
                    new GameInstallInfo { SupportsRoamingGuideEconomy = hasSource }, path);
                var dest = ExpectedFile(path, "GameData/FastTravelEconomyPrototypes.cfg");
                Check(File.Exists(dest) == (hasSource && scale != 1.0),
                    "Roaming guide CFG version gate or vanilla no-op broken");
                if (hasSource && scale != 1.0)
                {
                    var root = Parse(File.ReadAllText(dest)).Get("[1]");
                    Check(root.Bpatch && root.Assignments.Count == 2,
                        "Roaming economy must patch two fields in existing [1] root only");
                    Approx(root.Value("GeneralCoefficient"), 1.3 * scale,
                        "Roaming GeneralCoefficient");
                    Approx(root.Value("MinPrice"), 100 * scale, "Roaming MinPrice");
                    Check(result.FileCount == 2 && result.ParameterCount == 1,
                        "Linked price knob did not emit exactly two CFG sources");
                }
                if (!hasSource)
                    Check(!File.Exists(dest), "Pre-2.1 target unexpectedly emitted");
                _passed++;
            }
            finally
            {
                if (_keep is null && Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
        }
    }

    private static void TestTraderThreshold()
    {
        const string id = "economy.traderMinBuyCondition";
        const string label = "Veteran-Trader-MinBuy";
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var par = vm.AllParameters.Single(p => p.Id == id);
            Check(par.IsSlider && par.IsControlEnabled, label + ": missing live editor");
            par.SnapCurrentNumberToMarker(0.5);
            Check(par.IsValueModifiedFromReference(), label + ": unchanged threshold");
            Check(vm.AllParameters.Where(p => p.Id.StartsWith("economy.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p=>p.Id)
                .SequenceEqual(new[]{id}), label+": collateral Economy changes");
            var csv = File.ReadAllLines(Path.Combine("src","TrueCustomDifficulty","Data",
                "trader-min-buy-condition-manifest.csv")).Skip(1)
                .Where(line=>!string.IsNullOrWhiteSpace(line)).Select(line=>line.Split(',')).ToArray();
            Check(csv.Length == 86 && csv.All(row=>row.Length==7), label+": manifest drift");
            var baseRows = csv.Where(row=>row[0]=="base").ToArray();
            Check(baseRows.Length==68 && csv.Count(row=>row[0]=="dlc1")==18,
                label+": unexpected 68/18 scope inventory");
            var result = new CfgPatchWriterService().Write(vm,Catalog,null,path);
            var files = baseRows.Select(row=>ExpectedFile(path,row[1]))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check(result.FileCount==files.Count && result.ParameterCount==1 && result.EditCount==136,
                label+": expected 136 actual trader leaf writes");
            Check(Directory.GetFiles(path,"*TrueCustomDifficulty*",SearchOption.AllDirectories)
                .ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(files),
                label+": unrelated/missing trader output files");
            foreach(var file in files)
            {
                var rows = baseRows.Where(row=>string.Equals(ExpectedFile(path,row[1]),file,
                    StringComparison.OrdinalIgnoreCase)).ToArray();
                var patch = Parse(File.ReadAllText(file));
                var wanted = new HashSet<string>(StringComparer.Ordinal);
                foreach(var row in rows)
                    foreach(var key in new[]{row[4],row[5]})
                        Check(wanted.Add(row[2]+"/"+row[3]+"/"+key),
                            label+": duplicate manifest source route");
                var got = new HashSet<string>(StringComparer.Ordinal);
                foreach(var n in patch.Children.Values) Collect(n,n.Name,got);
                Check(wanted.SetEquals(got),label+": unrelated/missing trader CFG leaves");
                foreach(var row in rows)
                {
                    var node = patch.Get(row[2]);
                    Check(node.Bpatch,label+": missing prototype {bpatch}");
                    foreach(var segment in row[3].Split('/',StringSplitOptions.RemoveEmptyEntries))
                    {
                        node=node.Get(segment);
                        Check(node.Bpatch,label+": missing nested {bpatch}");
                    }
                    Approx(node.Value(row[4]),0.5,label+": weapon condition");
                    Approx(node.Value(row[5]),0.5,label+": armor condition");
                }
            }
            _passed++;
            Console.WriteLine("PASS "+label+" (68 base profiles, 136 assignments)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path)) Directory.Delete(path,recursive:true);
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
        var source = ParseNumber(point.SourceRaw);
        return point.Operation switch
        {
            "set" => par.CurrentNumber,
            "scale" => source * par.CurrentNumber,
            "scaleNonZero" => Math.Abs(source) < 0.0000001 ? source : source * par.CurrentNumber,
            "scaleIntegerHalfUp" => Math.Max(1, Math.Floor(source * par.CurrentNumber + 0.5)),
            _ => throw new InvalidDataException("Unsupported Economy operation " + point.Operation)
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
