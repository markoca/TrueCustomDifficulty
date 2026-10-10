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

            // Verify 1x Bandage cap and legacy preset bounds with the live VM.
            BandageBleedingUpperBoundCase();

            MedicalItemsPairingCase();

            // A unique concrete edit for every live Health/Stamina catalog
            // parameter. Master-only modifiers use Master because their
            // parent AdditionalMechanics switch is naturally enabled there.
            var cohort = Catalog.Parameters
                .Where(p => p.Id.StartsWith("health.", StringComparison.Ordinal)
                         || p.Id.StartsWith("stamina.", StringComparison.Ordinal))
                .OrderBy(p => p.Id, StringComparer.Ordinal)
                .ToArray();
            Check(cohort.Length == 43, "Health/Stamina catalog cohort is no longer 43 controls");

            // These five source-backed speed-state definitions are intentionally
            // excluded from MainViewModel. They are documented internal CFG
            // research, not exposed/active UI controls or valid writer inputs.
            // Pin the exclusion rather than inventing UI coverage for them.
            var internalOnly = new HashSet<string>(StringComparer.Ordinal)
            {
                "stamina.sprintUnderRunSpeedRegenCoef",
                "stamina.joggingRegenCoef",
                "stamina.slowRunRegenCoef",
                "stamina.sprintRegenCoef",
                "stamina.runRegenCoef"
            };
            var initialVm = new MainViewModel(Catalog);
            var liveIds = initialVm.AllParameters.Select(p => p.Id)
                .ToHashSet(StringComparer.Ordinal);
            var notLoaded = cohort.Where(p => !liveIds.Contains(p.Id))
                .Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(notLoaded.SetEquals(internalOnly),
                "Unexpected Health/Stamina app VM exclusions: "
                + string.Join(", ", notLoaded.OrderBy(s => s)));
            var liveCohort = cohort.Where(p => liveIds.Contains(p.Id)).ToArray();
            Check(liveCohort.Length == 38,
                "Health/Stamina live VM count is not 38");
            foreach (var def in liveCohort)
            {
                var diff = def.Id.StartsWith("health.master", StringComparison.Ordinal)
                    ? "Master" : "Veteran";
                Test(def.Id, diff);
            }
            Console.WriteLine("INTERNAL SOURCE ONLY: 5 intentionally non-VM stamina speed-state CFG definitions");

            // The 4 selected Base Presets must map scalar Regen_HP to the
            // correct Easy/Medium/Hard/Stalker DifficultyPrototypes row.
            foreach (var difficulty in new[] { "Novice", "Stalker", "Master" })
                Test("health.regenerationScalar", difficulty);

            // Parent toggles are intentionally full-list replacements.
            // Regression: the child-only modifier tests above must NOT
            // trigger this list; a genuinely changed parent must still do so.
            foreach (var (id, member) in new[]
            {
                ("advanced.starvationMechanics", "HungerNegativeMechanics"),
                ("advanced.radiationMechanics", "RadiationNegativeMechanics"),
                ("advanced.fatigueMechanics", "SleepinessNegativeMechanics"),
                ("advanced.lowHealthMechanics", "RegenHealthNegativeMechanics"),
                ("advanced.masterEffectModifierMechanics", "MasterEffectModifierNegativeMechanics")
            })
                MasterParentToggleCase(id, member);

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

    private static void MedicalItemsPairingCase()
    {
        // All eight actual healing/bleeding source values remain independent
        // while their UI routes through exactly one visible medical card.
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        var idPairs = new[]
        {
            ("health.bandageHealing", "health.bandageBleeding"),
            ("health.medkitHealing", "health.medkitBleeding"),
            ("health.armyMedkitHealing", "health.armyMedkitBleeding"),
            ("health.ecoMedkitHealing", "health.ecoMedkitBleeding")
        };
        var anchor = vm.AllParameters.Single(p => p.Id == "health.bandageHealing");
        Check(anchor.IsMedicalHealingAnchor && anchor.IsControlVisible,
            "Unified Medical Items editor must retain its bandage-healing anchor");
        foreach (var (healId, bleedId) in idPairs)
        {
            var healing = vm.AllParameters.Single(p => p.Id == healId);
            var bleeding = vm.AllParameters.Single(p => p.Id == bleedId);
            Check(healing.IsMedicalHealingMember && bleeding.IsMedicalHealingMember,
                "Paired medical treatment must use one source-backed visual family");
            Check(!bleeding.IsControlVisible && !bleeding.IsVisualAggregateAnchor,
                "Bleeding source must not create a separate top-level card");
            if (healing != anchor)
                Check(!healing.IsControlVisible,
                    "All paired healing values must be rendered only in the medical card");
            Check(healing.IsSlider && bleeding.IsSlider,
                "Paired healing and bleeding must remain independent numeric sliders");
        }
        var food = vm.AllParameters.Single(p => p.Id == "health.foodHealing");
        Check(food.IsMedicalHealingMember && !food.IsControlVisible && food.IsToggle,
            "Food Healing must remain a checkbox in the same medical card");

        var bleed = vm.AllParameters.Single(p => p.Id == "health.medkitBleeding");
        Check(!anchor.IsModified, "Unedited medical group should not be marked modified");
        bleed.CurrentNumber = 20.0;
        Check(anchor.IsModified && bleed.IsValueModifiedFromReference(),
            "Changing a hidden bleeding child must mark its visible medical group modified");
        bleed.ResetToReference();
        Check(!anchor.IsModified,
            "Resetting a bleeding child must clear the medical group modified state");
        _passed++;
        Console.WriteLine("PASS medical-items-paired-4x2-visual-membership");
    }

    private static void BandageBleedingUpperBoundCase()
    {
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        var bandage = vm.AllParameters.Single(p => p.Id == "health.bandageBleeding");
        Check(bandage.IsSlider && bandage.Minimum == 0.0
              && bandage.Maximum == 100.0 && bandage.ReferenceNumber == 100.0
              && bandage.TickFrequency == 5.0,
            "Bandage Bleeding maximum must equal vanilla 1x / 100 PTS");
        foreach (var (id, maximum) in new[]
        {
            ("health.medkitBleeding", 30.0),
            ("health.armyMedkitBleeding", 70.0),
            ("health.ecoMedkitBleeding", 50.0)
        })
            Check(vm.AllParameters.Single(p => p.Id == id).Maximum == maximum,
                id + ": the common 100-PTS ruler must not enlarge native maxima");

        bandage.ApplyPresetValue(new PresetValue { Kind = "slider", Number = 200.0 });
        Check(bandage.CurrentNumber == 100.0,
            "Old Bandage 200-PTS presets must clamp to 1x");
        bandage.SnapCurrentNumberToMarker(200.0);
        Check(bandage.CurrentNumber == 100.0,
            "Marker setter must not bypass Bandage ceiling");
        bandage.CurrentNumber = 95.0;
        Check(bandage.CurrentNumber == 95.0 && bandage.IsValueModifiedFromReference(),
            "Bandage must keep native 5-PTS editing below 1x");
        bandage.ResetToReference();
        Check(bandage.CurrentNumber == 100.0,
            "Bandage reset must restore original 1x");
        _passed++;
        Console.WriteLine("PASS bandage-bleeding-1x-cap");
    }

    private static void MasterParentToggleCase(string toggleId, string removedMember)
    {
        var label = "Master-mechanics-" + toggleId.Replace(".", "-", StringComparison.Ordinal);
        var output = Path.Combine(_keepRoot ?? Path.GetTempPath(),
            "TCD_Health_Stamina", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Master" };
            var toggle = vm.AllParameters.Single(p => p.Id == toggleId);
            Check(toggle.IsToggle && toggle.ReferenceBoolean && toggle.CurrentBoolean,
                label + ": Master mechanics parent no longer has its original ON baseline");
            toggle.CurrentBoolean = false;
            Check(toggle.IsValueModifiedFromReference(),
                label + ": deliberate parent change did not register");

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, output);
            Check(result.FileCount == 1 && result.ParameterCount == 1
                  && result.EditCount == 5,
                label + ": complete four-member list replacement expected (5 edits)");
            var file = ExpectedFile(output, "GameData/DifficultyPrototypes.cfg");
            var document = Parse(File.ReadAllText(file));
            Check(document.Children.Count == 1
                  && document.Children.ContainsKey("Stalker"),
                label + ": wrong Master CFG SID");
            var root = document.Get("Stalker");
            var environment = root.Get("EnvironmentDifficulty");
            Check(root.Bpatch && environment.Bpatch
                  && environment.Children.Count == 1
                  && environment.Assignments.Count == 0,
                label + ": parent replacement has unrelated assignments");
            var list = environment.Get("AdditionalMechanicsEffects");
            Check(!list.Bpatch && list.Children.Count == 0
                  && list.Assignments.Count == 4,
                label + ": mechanics array must be a four-entry full replacement");
            var vanillaMembers = new[]
            {
                "HungerNegativeMechanics",
                "RadiationNegativeMechanics",
                "SleepinessNegativeMechanics",
                "RegenHealthNegativeMechanics",
                "MasterEffectModifierNegativeMechanics"
            };
            var expected = vanillaMembers.Where(s => s != removedMember).ToArray();
            for (var index = 0; index < expected.Length; index++)
                Check(list.Value($"[{index}]") == expected[index],
                    label + ": original mechanics list order/member mismatch at " + index);

            _passed++;
            Console.WriteLine("PASS " + label + " (full 4-item mechanics list replacement)");
        }
        finally
        {
            if (_keepRoot is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
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
                    p.Prototype + "/" + (p.NodePath.Count > 0
                        ? string.Join("/", p.NodePath) + "/" : "") + p.Key)
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
