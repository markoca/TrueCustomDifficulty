using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace EquipmentPatchContract;

// Actual production MainViewModel -> CfgPatchWriterService -> parsed BPatch.
// Does not validate Unreal merges, original archive contents, or gameplay effects.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    private static readonly string[] Ids =
    {
        "equipment.armorDurability",
        "equipment.weaponWearPerShot",
        "equipment.ammoWeight",
        "equipment.consumableWeight"
    };
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
                    throw new ArgumentException("Usage: EquipmentPatchContract [--out PATH]");
                _keep = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(_keep);
            }
            VerifyInventory();

            // Vanilla must not produce any Equipment BPatch, on all four baseline difficulties.
            foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
                Vanilla(difficulty);

            // Each visible source must work independently on every difficulty.
            foreach (var id in Ids)
                foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
                    Single(difficulty, id);

            // 0x/2x corners, including preserved per-weapon authored zero.
            foreach (var id in Ids.Where(x => x != "equipment.armorDurability"))
            {
                Single("Veteran", id, 0);
                Single("Veteran", id, 2);
            }

            // Explicit DLC1 installed signal: DLC1 weight and wear must be emitted too.
            foreach (var id in new[] { "equipment.weaponWearPerShot", "equipment.consumableWeight" })
            {
                Single("Veteran", id, 0.5, hasDlc1: true);
                Single("Veteran", id, 2.0, hasDlc1: true);
            }

            // Both crosshair axes / both weight controls, including a shared ItemPrototypes file.
            Pair("Veteran", "equipment.armorDurability", 1.4,
                "equipment.weaponWearPerShot", 0.5);
            Pair("Veteran", "equipment.ammoWeight", 0.5,
                "equipment.consumableWeight", 1.6, hasDlc1: true);

            Check(_passed == 32, "Expected exactly 32 Equipment writer scenarios, got " + _passed);
            Console.WriteLine("PASS: " + _passed + " Equipment production BPatch writer scenarios");
            Console.WriteLine("NOT VERIFIED: original CFG ZIPs, actual DLC installation, Unreal patch loading or gameplay");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static void VerifyInventory()
    {
        var defined = Catalog.Parameters.Where(p => p.Id.StartsWith("equipment.", StringComparison.Ordinal))
            .Select(p => p.Id).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Check(defined.SequenceEqual(Ids.OrderBy(x => x, StringComparer.Ordinal)),
            "Equipment catalog no longer contains exactly the four audited active IDs");
        var mounted = new MainViewModel(Catalog).AllParameters.Select(p => p.Id)
            .Where(id => id.StartsWith("equipment.", StringComparison.Ordinal))
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Check(mounted.SequenceEqual(defined), "Equipment catalog-to-actual-ViewModel drift");
        var points = Baseline.EditPoints.Where(p => p.ParameterId.StartsWith("equipment.", StringComparison.Ordinal))
            .ToArray();
        Check(points.Length == 113, "Expected 113 direct Equipment CFG points");
        Check(points.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal)
            .SetEquals(Ids), "Equipment catalog-to-writer source ID mismatch");
        Check(points.Count(p => p.Scope == "baseGame") == 101 &&
              points.Count(p => p.Scope == "dlc1") == 12, "Equipment source scope counts drift");
        Check(points.All(p => p.Operation is "set" or "scale" or "scaleNonZero"),
            "Unsupported direct Equipment operation");
        Check(points.Count(p => p.ParameterId == "equipment.armorDurability") == 4
           && points.Count(p => p.ParameterId == "equipment.ammoWeight") == 34
           && points.Count(p => p.ParameterId == "equipment.consumableWeight") == 23
           && points.Count(p => p.ParameterId == "equipment.weaponWearPerShot") == 52,
           "Equipment field count/ownership drift");

        var wear = points.Where(p => p.ParameterId == "equipment.weaponWearPerShot").ToArray();
        Check(wear.Count(p => p.Scope == "baseGame") == 41
            && wear.Count(p => p.Scope == "dlc1") == 11,
            "Player-weapon source inventories must remain 41 base / 11 DLC1");
        Check(wear.Count(p => Math.Abs(ParseNumber(p.SourceRaw)) < 0.0000001) == 1,
            "Expected exactly one player-weapon original zero");
        Check(wear.Single(p => Math.Abs(ParseNumber(p.SourceRaw)) < 0.0000001).Prototype
            == "UA_GLaunch_Player", "Authored zero unexpectedly moved to another weapon");

        var armor = points.Where(p => p.ParameterId == "equipment.armorDurability").ToArray();
        Check(armor.All(p => p.Key == "Armor_Durability" && p.Operation == "set"
            && p.NodePath.SequenceEqual(new[] { "EnvironmentDifficulty" })
            && !string.IsNullOrWhiteSpace(p.DifficultySid)),
            "Armor difficulty path/semantics drift");
        Check(armor.Select(p => p.DifficultySid).ToHashSet(StringComparer.Ordinal)
            .SetEquals(Catalog.DifficultyProfiles.Values.Select(p => p.Sid)),
            "Missing or duplicated armor difficulty SID");
        Check(points.Where(p => p.ParameterId is "equipment.ammoWeight" or "equipment.consumableWeight")
              .All(p => p.Key == "Weight" && p.NodePath.Count == 0 && p.Operation == "scale"),
            "Item weight path/operation changed unexpectedly");
        Check(wear.All(p => p.Key == "DurabilityDamagePerShot" && p.NodePath.Count == 0
            && p.Operation == "scaleNonZero" && p.Prototype.EndsWith("_Player", StringComparison.Ordinal)),
            "Weapon wear paths/operation changed unexpectedly");
    }

    private static void Vanilla(string difficulty)
    {
        var label = difficulty + "-Vanilla";
        var path = ScenarioPath(label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            Check(Ids.All(id => !vm.AllParameters.Single(p => p.Id == id).IsValueModifiedFromReference()),
                label + ": initial Equipment values already differ from reference");
            var result = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, path);
            Check(result.FileCount == 0 && result.EditCount == 0 && result.ParameterCount == 0,
                label + ": vanilla emitted edits");
            Check(Directory.GetFiles(path, "*TrueCustomDifficulty*", SearchOption.AllDirectories).Length == 0,
                label + ": vanilla emitted CFG files");
            Passed(label, 0);
        }
        finally { Cleanup(path); }
    }

    private static void Single(string difficulty, string id, double? chosen = null, bool hasDlc1 = false)
    {
        var label = difficulty + "-" + id.Replace(".", "-", StringComparison.Ordinal)
            + (chosen.HasValue ? "-" + chosen.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-step")
            + (hasDlc1 ? "-DLC1" : "-base");
        var path = ScenarioPath(label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var parameter = vm.AllParameters.Single(p => p.Id == id);
            Check(parameter.IsSlider && parameter.IsControlEnabled &&
                !parameter.IsValueModifiedFromReference(), label + ": expected live vanilla slider");
            var step = Math.Max(0.01, parameter.Definition.Slider?.Step ?? 0.01);
            var candidate = chosen ?? (parameter.ReferenceNumber + step <= parameter.Maximum
                ? parameter.ReferenceNumber + step : parameter.ReferenceNumber - step);
            parameter.SnapCurrentNumberToMarker(candidate);
            Check(Math.Abs(parameter.CurrentNumber - candidate) < 0.0000011 &&
                parameter.IsValueModifiedFromReference(), label + ": real source slider was not changed");
            Validate(vm, path, label, new[] { id }, hasDlc1);
        }
        finally { Cleanup(path); }
    }

    private static void Pair(string difficulty, string first, double firstValue,
                             string second, double secondValue, bool hasDlc1 = false)
    {
        var label = difficulty + "-pair-" + first.Split('.')[1] + "-" + second.Split('.')[1]
            + (hasDlc1 ? "-DLC1" : "-base");
        var path = ScenarioPath(label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var a = vm.AllParameters.Single(p => p.Id == first);
            var b = vm.AllParameters.Single(p => p.Id == second);
            a.SnapCurrentNumberToMarker(firstValue);
            b.SnapCurrentNumberToMarker(secondValue);
            Check(a.IsValueModifiedFromReference() && b.IsValueModifiedFromReference(),
                label + ": source not changed on both axes");
            Validate(vm, path, label, new[] { first, second }, hasDlc1);
        }
        finally { Cleanup(path); }
    }

    private static void Validate(MainViewModel vm, string path, string label,
                                 string[] ids, bool hasDlc1)
    {
        var edited = vm.AllParameters.Where(p => p.Id.StartsWith("equipment.", StringComparison.Ordinal)
            && p.IsValueModifiedFromReference()).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        Check(edited.SetEquals(ids), label + ": collateral source control modification");
        var selected = ids.ToHashSet(StringComparer.Ordinal);
        var sid = Catalog.DifficultyProfiles[vm.SelectedReferenceDifficulty].Sid;
        var points = Baseline.EditPoints.Where(p => selected.Contains(p.ParameterId)
            && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid == sid)
            && (hasDlc1 || p.Scope != "dlc1")).ToArray();
        Check(points.Length > 0, label + ": no mapped source fields");
        var byId = vm.AllParameters.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var changed = points.Where(p => Math.Abs(Expected(byId[p.ParameterId], p)
            - ParseNumber(p.SourceRaw)) >= 0.0000001).ToArray();
        Check(changed.Length > 0, label + ": no changed BPatch fields expected");
        var result = new CfgPatchWriterService().Write(vm, Catalog,
            hasDlc1 ? new GameInstallInfo { HasDlc1 = true } : null, path);
        var wantedFiles = changed.Select(p => ExpectedFile(path, p.File))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Check(result.FileCount == wantedFiles.Count && result.ParameterCount == ids.Length
            && result.EditCount == changed.Length,
            label + ": count mismatch observed " + result.FileCount + "/" +
            result.ParameterCount + "/" + result.EditCount + " expected " +
            wantedFiles.Count + "/" + ids.Length + "/" + changed.Length);
        var actualFiles = Directory.GetFiles(path, "*TrueCustomDifficulty*", SearchOption.AllDirectories)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Check(actualFiles.SetEquals(wantedFiles), label + ": unexpected or missing writer output file");

        foreach (var output in wantedFiles)
        {
            var entries = changed.Where(p => string.Equals(ExpectedFile(path, p.File), output,
                StringComparison.OrdinalIgnoreCase)).ToArray();
            var patch = Parse(File.ReadAllText(output));
            var expectedPaths = entries.Select(p => p.Prototype + "/"
                + (p.NodePath.Count > 0 ? string.Join("/", p.NodePath) + "/" : "") + p.Key)
                .ToHashSet(StringComparer.Ordinal);
            var actualPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in patch.Children.Values) Collect(root, root.Name, actualPaths);
            Check(expectedPaths.SetEquals(actualPaths), label + ": unexpected/missing BPatch leaf paths");

            foreach (var point in entries)
            {
                var node = patch.Get(point.Prototype);
                Check(node.Bpatch, label + ": prototype missing {bpatch}: " + point.Prototype);
                foreach (var segment in point.NodePath)
                {
                    node = node.Get(segment);
                    Check(node.Bpatch, label + ": nested node missing {bpatch}: " + segment);
                }
                Approx(node.Value(point.Key), Expected(byId[point.ParameterId], point),
                    label + ": " + point.Prototype + "." + point.Key);
            }
        }
        // An explicit zero-valued grenade launcher should never be emitted.
        if (selected.Contains("equipment.weaponWearPerShot"))
        {
            Check(changed.All(p => p.Prototype != "UA_GLaunch_Player"),
                label + ": zero-valued grenade launcher incorrectly modified");
        }
        if (!hasDlc1)
            Check(changed.All(p => p.Scope != "dlc1"), label + ": DLC1 leaked into base-only output");
        Passed(label, changed.Length);
    }

    private static string ScenarioPath(string label) =>
        Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Equipment_Patch", label);

    private static void Cleanup(string path)
    {
        if (_keep is null && Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private static void Passed(string label, int edits)
    {
        _passed++;
        Console.WriteLine("PASS " + label + " (" + edits + " emitted fields)");
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

    private static double Expected(ParameterViewModel parameter, CfgWriterEditPoint point)
    {
        var source = ParseNumber(point.SourceRaw);
        return point.Operation switch
        {
            "set" => parameter.CurrentNumber,
            "scale" => source * parameter.CurrentNumber,
            "scaleNonZero" => Math.Abs(source) < 0.0000001 ? source : source * parameter.CurrentNumber,
            _ => throw new InvalidDataException("Unexpected Equipment operation: " + point.Operation)
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
