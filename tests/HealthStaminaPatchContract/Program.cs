using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace HealthStaminaPatchContract;

// This executes the actual WPF ViewModel and CfgPatchWriterService. No
// independent CFG serialization or game runtime is emulated. Source ZIP
// parity is independently checked by tools/audit_cfg_source_values.py.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    private static string? _keepRoot;
    private static int _passed;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0)
            {
                if (args.Length != 2 || args[0] != "--out")
                    throw new ArgumentException("Usage: HealthStaminaPatchContract [--out PATH]");
                _keepRoot = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(_keepRoot);
            }

            // A unique concrete edit for every Health/Stamina catalog
            // parameter. Master-only modifiers use Master because their
            // parent AdditionalMechanics switch is naturally enabled there.
            var cohort = Catalog.Parameters
                .Where(p => p.Id.StartsWith("health.", StringComparison.Ordinal)
                         || p.Id.StartsWith("stamina.", StringComparison.Ordinal))
                .OrderBy(p => p.Id, StringComparer.Ordinal)
                .ToArray();
            Check(cohort.Length == 43, "Health/Stamina catalog cohort is no longer 43 controls");

            foreach (var def in cohort)
            {
                var diff = def.Id.StartsWith("health.master", StringComparison.Ordinal)
                    ? "Master" : "Veteran";
                Test(def.Id, diff);
            }

            // The 4 selected Base Presets must map scalar Regen_HP to the
            // correct Easy/Medium/Hard/Stalker DifficultyPrototypes row.
            foreach (var difficulty in new[] { "Novice", "Stalker", "Master" })
                Test("health.regenerationScalar", difficulty);

            Console.WriteLine($"PASS: {_passed} Health/Stamina production BPatch writer scenarios.");
            Console.WriteLine("NOT VERIFIED: in-game runtime, UE nested inherited VaultingParams BPatch, original-source archive parity.");
            return 0;
        }
        catch (Exception exc)
        {
            Console.Error.WriteLine("FAIL: " + exc);
            return 1;
        }
    }

    private static void Test(string id, string difficulty)
    {
        var label = difficulty + "-" + id.Replace(".", "-", StringComparison.Ordinal);
        var output = Path.Combine(_keepRoot ?? Path.GetTempPath(), "TCD_Health_Stamina", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var parameter = vm.AllParameters.Single(p => p.Id == id);
            Check(parameter.IsControlEnabled, label + ": backing UI control is disabled");
            Check(!parameter.IsValueModifiedFromReference(), label + ": baseline starts edited");

            if (parameter.IsToggle)
                parameter.CurrentBoolean = !parameter.ReferenceBoolean;
            else
            {
                Check(parameter.IsSlider, label + ": unsupported catalog editor");
                // Marker setter changes the real CurrentNumber and triggers
                // any WPF-linked value constraints without rounding drift.
                var step = Math.Max(0.01, parameter.Definition.Slider?.Step ?? 0.1);
                var candidate = parameter.ReferenceNumber + step;
                if (candidate > parameter.Maximum)
                    candidate = parameter.ReferenceNumber - step;
                parameter.SnapCurrentNumberToMarker(candidate);
            }
            Check(parameter.IsValueModifiedFromReference(), label + ": value did not change");
            Check(parameter.IsControlEnabled, label + ": value change disabled control");

            // The direct route inventory is evaluated by the actual selected
            // difficulty SID, not the human-facing text used in the UI.
            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var points = Baseline.EditPoints
                .Where(p => p.ParameterId == id
                         && (string.IsNullOrWhiteSpace(p.DifficultySid)
                             || p.DifficultySid == sid))
                .ToArray();
            Check(points.Length > 0, label + ": no direct writer edit points");
            Check(points.All(p => p.Scope == "baseGame"),
                label + ": Health/Stamina moved out of baseGame");

            var modified = vm.AllParameters
                .Where(p => (p.Id.StartsWith("health.", StringComparison.Ordinal)
                          || p.Id.StartsWith("stamina.", StringComparison.Ordinal))
                         && p.IsValueModifiedFromReference())
                .Select(p => p.Id).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            Check(modified.SequenceEqual(new[] { id }),
                label + ": a slider change unexpectedly changed other Health/Stamina values");

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, output);
            var expectedFiles = points.Select(p => ExpectedFile(output, p.File))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            Check(result.EditCount == points.Length
                  && result.ParameterCount == 1
                  && result.FileCount == expectedFiles.Length,
                label + ": wrong edit/parameter/file totals ("
                + result.EditCount + "/" + result.ParameterCount + "/" + result.FileCount
                + "; expected " + points.Length + "/1/" + expectedFiles.Length + ")");
            var actualFiles = Directory.GetFiles(output, "*TrueCustomDifficulty*",
                SearchOption.AllDirectories);
            Check(actualFiles.Length == expectedFiles.Length
                  && actualFiles.ToHashSet(StringComparer.OrdinalIgnoreCase)
                      .SetEquals(expectedFiles),
                label + ": wrong CFG output paths or additional files");

            foreach (var file in expectedFiles)
            {
                Check(File.Exists(file), label + ": expected CFG not written: " + file);
                var document = Parse(File.ReadAllText(file));
                var editPoints = points.Where(p => ExpectedFile(output, p.File) == file).ToArray();
                var expectedPaths = editPoints.Select(p =>
                    p.Prototype + "/" + string.Join("/", p.NodePath) + "/" + p.Key)
                    .ToHashSet(StringComparer.Ordinal);
                var observedPaths = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in document.Children.Values)
                    Collect(root, root.Name, observedPaths);
                Check(expectedPaths.SetEquals(observedPaths),
                    label + ": unrelated or missing BPatch fields "
                    + "expected={" + string.Join(", ", expectedPaths) + "} observed={"
                    + string.Join(", ", observedPaths) + "}");

                foreach (var point in editPoints)
                {
                    var root = document.Get(point.Prototype);
                    Check(root.Bpatch, label + ": prototype must be {bpatch}");
                    var node = root;
                    foreach (var segment in point.NodePath)
                    {
                        node = node.Get(segment);
                        Check(node.Bpatch,
                            label + ": nested source path " + segment + " not {bpatch}");
                    }
                    var actual = node.Value(point.Key);
                    var expected = ExpectedValue(parameter, point);
                    if (parameter.IsToggle && point.Operation == "set")
                        Check(actual == (parameter.CurrentBoolean ? "true" : "false"),
                            label + ": boolean output mismatch");
                    else
                        Approx(actual, expected, label + ": " + point.Key);

                    // In particular, Player.VaultingParams may be inherited
                    // from the original [0] prototype. Test the generated
                    // structure; runtime semantics remain NOT VERIFIED.
                    if (id == "stamina.vaultMaxHeight")
                        Check(root.Name == "Player" && node.Name == "VaultingParams"
                              && point.Key == "MaxObstacleHeight",
                            label + ": inherited VaultingParams path changed");
                }
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (" + points.Length + " CFG edits)");
        }
        finally
        {
            if (_keepRoot is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private static string ExpectedFile(string root, string source)
    {
        var file = Path.GetFileName(source);
        var stem = Path.GetFileNameWithoutExtension(file);
        var parent = Path.GetDirectoryName(source.Replace('/', Path.DirectorySeparatorChar))
                     ?? string.Empty;
        var location = Path.Combine(root, "Stalker2", "Content", "GameLite", parent);
        if (file.EndsWith("Prototypes.cfg", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(location, stem, stem + "_patch_TrueCustomDifficulty.cfg");
        return Path.Combine(location, file + "_patch_TrueCustomDifficulty");
    }

    private static double ExpectedValue(ParameterViewModel parameter, CfgWriterEditPoint point)
    {
        return point.Operation switch
        {
            "set" => parameter.CurrentNumber,
            "negateSet" => -parameter.CurrentNumber,
            "toggleSourceOrZero" => parameter.CurrentBoolean ? ParseRaw(point.SourceRaw) : 0.0,
            _ => throw new InvalidDataException(
                point.ParameterId + ": unexpected Health/Stamina operation " + point.Operation)
        };
    }

    private static double ParseRaw(string raw)
    {
        var txt = raw.Trim();
        var percent = txt.EndsWith("%", StringComparison.Ordinal);
        if (percent) txt = txt[..^1];
        if (txt.EndsWith("f", StringComparison.OrdinalIgnoreCase)) txt = txt[..^1];
        return double.Parse(txt, CultureInfo.InvariantCulture) / (percent ? 100.0 : 1.0);
    }

    private static void Approx(string raw, double expected, string context)
    {
        var actual = ParseRaw(raw);
        Check(Math.Abs(actual - expected) < 0.000001,
            context + ": output " + actual + ", expected " + expected);
    }

    private static void Collect(Node node, string prefix, HashSet<string> fields)
    {
        foreach (var key in node.Assignments.Keys)
            Check(fields.Add(prefix + "/" + key), "duplicate field " + prefix + "/" + key);
        foreach (var child in node.Children.Values)
            Collect(child, prefix + "/" + child.Name, fields);
    }

    private static Node Parse(string source)
    {
        var root = new Node("document", "");
        var stack = new Stack<Node>();
        stack.Push(root);
        foreach (var lineRaw in source.Replace("\r", "").Split('\n'))
        {
            var line = lineRaw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (line == "struct.end")
            {
                Check(stack.Count > 1, "Unmatched struct.end in CFG output");
                stack.Pop();
                continue;
            }
            var start = Regex.Match(line, @"^(?<name>\S+)\s*:\s*struct\.begin(?<flags>.*)$");
            if (start.Success)
            {
                var node = new Node(start.Groups["name"].Value, start.Groups["flags"].Value);
                Check(stack.Peek().Children.TryAdd(node.Name, node),
                    "Duplicate CFG section " + node.Name);
                stack.Push(node);
                continue;
            }
            var assignment = Regex.Match(line, @"^(?<key>\S+)\s*=\s*(?<value>.*)$");
            Check(assignment.Success, "Unparsed CFG statement: " + line);
            Check(stack.Peek().Assignments.TryAdd(
                assignment.Groups["key"].Value, assignment.Groups["value"].Value),
                "Duplicate CFG field " + line);
        }
        Check(stack.Count == 1, "Missing struct.end in CFG output");
        return root;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private sealed class Node(string name, string flags)
    {
        public string Name { get; } = name;
        public bool Bpatch { get; } = flags.Contains("{bpatch}", StringComparison.Ordinal);
        public Dictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Assignments { get; } = new(StringComparer.Ordinal);
        public Node Get(string segment) =>
            Children.TryGetValue(segment, out var node) ? node :
            throw new InvalidDataException($"Missing CFG {Name}/{segment}");
        public string Value(string field) =>
            Assignments.TryGetValue(field, out var value) ? value :
            throw new InvalidDataException($"Missing CFG {Name}.{field}");
    }
}
