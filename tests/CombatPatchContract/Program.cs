using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace CombatPatchContract;

// Runs the PRODUCTION CfgPatchWriterService, not a Python simulation.
// Windows/WPF runtime is required. No game, mod packer or test framework is required.
internal static class Program
{
    private static readonly CfgWriterBaseline Source = new CfgWriterBaselineService().Load();
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static string? _keepRoot;
    private static int _passed;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 0)
            {
                if (args.Length != 2 || args[0] != "--out")
                    throw new ArgumentException("Usage: CombatPatchContract [--out DIRECTORY]");
                _keepRoot = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(_keepRoot);
            }

            Check(!Catalog.Parameters.Any(p => p.Id is "combat.npcAttackCooldown" or "combat.npcHipAccuracy"),
                "Retired human attack-pacing and accuracy settings must not ship in the catalog");
            Check(!Source.EditPoints.Any(p => p.ParameterId is "combat.npcAttackCooldown" or "combat.npcHipAccuracy"),
                "Retired settings must not have writer edit points");
            TestMutantHitDamageReferenceMarkers();

            Case("Master-no-changes", "Master", _ => { }, (_, result) =>
                Check(result.FileCount == 0 && result.EditCount == 0,
                    "unchanged Master must not emit any CFG"));

            Case("Veteran-no-changes", "Veteran", _ => { }, (_, result) =>
                Check(result.FileCount == 0 && result.EditCount == 0,
                    "unchanged Veteran must not emit any CFG"));

            Case("Master-PSY-count-only", "Master",
                vm => Set(vm, "combat.psyPhantomCount", 2.5),
                (root, result) =>
                {
                    Check(result.EditCount == 2 && result.ParameterCount == 1,
                        "Master PSY count must edit only two effect fields");
                    MasterPsy(root, "PsyPhantomNPCCountMultiplier", "2.5");
                    Check(!root.Children.ContainsKey("MutantCombatDifficulty"),
                        "PSY-only patch unexpectedly added mutant branch");
                });

            Case("Veteran-PSY-bleeding-only", "Veteran",
                vm => Get(vm, "combat.psyBleeding").CurrentBoolean = true,
                (root, result) =>
                {
                    Check(result.EditCount == 8 && result.ParameterCount == 1,
                        "Veteran PSY single change must emit both complete four-field entries");
                    FullPsy(root, bleed: true);
                    Check(!root.Children.ContainsKey("MutantCombatDifficulty"),
                        "PSY-only patch unexpectedly added mutant branch");
                });

            // Phase 5: prove live UI VM values reach remaining difficulty-specific
            // direct edit routes, including human HP/damage, toggles and
            // value-ordered repeated-hit sliders.
            foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
            {
                foreach (var spec in DirectCombatSpecs)
                    DirectCombatCase(difficulty, spec);
            }

            // Phase 5: all 14 live mutant HP cards, including both dual-variant
            // families, must emit nested VitalParams.MaxHP patches.
            foreach (var parameter in Catalog.Parameters.Where(p =>
                         p.Id.StartsWith("combat.mutantHealth.", StringComparison.Ordinal)))
                MutantHealthCase(parameter.Id);

            // Phase 7 uses three real, loadable user preset JSON fixtures.
            // The engine's handling of nested inherited BPatch is NOT simulated.
            InheritedSourcePresetCase("Stalker Vanilla.json", "Stalker", null);
            InheritedSourcePresetCase("Stalker Mutant Damage.json", "Stalker", 0.5);
            InheritedSourcePresetCase("Veteran Control.json", "Veteran", 0.5);

            Console.WriteLine($"PASS: {_passed} executed production CFG writer scenarios.");
            Console.WriteLine("NOT VERIFIED: game runtime, vanilla decoded source arrays, UE BPatch loader semantics.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    // In-game Stalker difficulty = CFG Medium; Veteran = CFG Hard.
    // Original Medium has no local MutantCombatDifficulty, inheriting
    // the section from Empty. Here we verify the real UI→writer output,
    // not the Unreal engine's resolution of inherited BPatch sections.
    private static void InheritedSourcePresetCase(
        string fileName, string difficulty, double? damage)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(),
            "tests", "CombatRuntimeAcceptance", "Presets", fileName);
        Check(File.Exists(path), "Missing runtime acceptance preset " + path);
        var preset = new PresetService().Load(path);
        Check(preset.ReferenceDifficulty == difficulty, "Preset base difficulty drift: " + fileName);

        var expectedIds = new HashSet<string>(StringComparer.Ordinal);
        if (damage.HasValue) expectedIds.Add("combat.mutantDamage");
        Check(preset.Values.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expectedIds),
              fileName + ": unexpected extra/missing control IDs");

        Case("runtime-" + Path.GetFileNameWithoutExtension(fileName)
                        .Replace(" ", "-", StringComparison.Ordinal),
            difficulty,
            vm =>
            {
                // Same PresetService + ParameterViewModel.ApplyPresetValue as
                // the production Load Preset path after its baseline reset.
                foreach (var pair in preset.Values)
                    Get(vm, pair.Key).ApplyPresetValue(pair.Value);
                Check(vm.SelectedReferenceDifficulty == difficulty, "Wrong loaded baseline");
                foreach (var id in expectedIds)
                    Check(Get(vm, id).IsValueModifiedFromReference(),
                        fileName + ": imported value is not an active edit " + id);
            },
            (root, result) =>
            {
                if (expectedIds.Count == 0)
                {
                    Check(result.FileCount == 0 && result.EditCount == 0
                          && result.ParameterCount == 0,
                        "vanilla Stalker preset emitted a patch");
                    return;
                }

                Check(result.FileCount == 1
                      && result.EditCount == expectedIds.Count
                      && result.ParameterCount == expectedIds.Count,
                    fileName + ": generated an unexpected edit or file");
                Check(root.Bpatch && root.Children.Count == 1
                      && root.Assignments.Count == 0
                      && root.Children.ContainsKey("MutantCombatDifficulty"),
                    fileName + ": patch must contain exactly one mutant section");
                var mutant = root.Get("MutantCombatDifficulty");
                Check(mutant.Bpatch && mutant.Children.Count == 0
                      && mutant.Assignments.Count == expectedIds.Count,
                    fileName + ": nested BPatch contents or flags invalid");
                if (damage.HasValue)
                    Approx(mutant.Value("Mutant_BaseDamage"), damage.Value);
                else
                    Check(!mutant.Assignments.ContainsKey("Mutant_BaseDamage"),
                        fileName + ": unmodified damage leaked into patch");

                var expectedSid = difficulty == "Stalker" ? "Medium" : "Hard";
                Check(Catalog.DifficultyProfiles[difficulty].Sid == expectedSid,
                    fileName + ": game difficulty-to-CFG SID mismatch");
            });
    }

    // Deliberately pin the expected vanilla field identities independently
    // of the live writer baseline. The baseline supplies only original numeric
    // source values for the per-difficulty transformation expectation.
    private sealed record DirectSpec(
        string Id, string Node, string Key, string Operation, double Value);

    private static readonly DirectSpec[] DirectCombatSpecs =
    [
        new("combat.npcHealth", "NPCCombatDifficulty", "NPC_HP", "set", 1.7),
        new("combat.npcWeaponDamage", "NPCCombatDifficulty", "NPC_Weapon_BaseDamage", "set", 1.7),
        new("combat.explosionDamage", "EnvironmentDifficulty", "Explosion_BaseDamage", "set", 1.5),
        new("combat.playerWeaponDamage", "EnvironmentDifficulty", "Weapon_BaseDamage", "set", 1.25),
        new("combat.playerHeadshotMultiplier", "NPCCombatDifficulty", "PlayerWeapon_HeadshotMultiplier", "set", 4.0),
        new("combat.weaponJamming", "NPCCombatDifficulty", "Weapon_JammingMultiplier", "set", 0.5),
        new("combat.mutantDamage", "MutantCombatDifficulty", "Mutant_BaseDamage", "set", 0.8),
        new("combat.accumulatedDamageWeightMax", "NPCCombatDifficulty", "AccumulatedDamageReductionCurveWeightMax", "set", 0.9),
        new("combat.accumulatedDamageWeightMin", "NPCCombatDifficulty", "AccumulatedDamageReductionCurveWeightMin", "set", 0.1),
        new("combat.accumulatedDamageIncludesHealedHealth", "NPCCombatDifficulty", "AccumulatedDamageReductionIncludesHealedHealth", "set", double.NaN),
    ];

    private static void DirectCombatCase(string difficulty, DirectSpec spec)
    {
        var name = difficulty + "-direct-" + spec.Id.Replace("combat.", "", StringComparison.Ordinal);
        var output = FixturePath(name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var parameter = Get(vm, spec.Id);
            if (spec.Id == "combat.playerHeadshotMultiplier")
                Check(Math.Abs(parameter.Minimum - 0.5) < 1e-9
                      && Math.Abs(parameter.Maximum - 4.0) < 1e-9
                      && Math.Abs(parameter.TickFrequency - 0.25) < 1e-9,
                    name + ": headshot native 0.5x–4x / 0.25x range drifted");
            if (parameter.IsToggle)
            {
                Check(double.IsNaN(spec.Value), name + ": unexpected boolean test declaration");
                parameter.CurrentBoolean = !parameter.ReferenceBoolean;
            }
            else
            {
                Set(vm, spec.Id, spec.Value);
                Approx(parameter.CurrentNumber.ToString(CultureInfo.InvariantCulture), spec.Value);
            }
            Check(parameter.IsValueModifiedFromReference(), name + ": requested edit was not applied");

            // Vanilla Novice Min/Max are both 1: pulling Max down to 0.9
            // must also pull Min down to 0.9. The other 51 cases have one
            // independent concrete edit, not an implicit extra assignment.
            var isLinkedNoviceMax = difficulty == "Novice"
                && spec.Id == "combat.accumulatedDamageWeightMax";
            var expectedIds = isLinkedNoviceMax
                ? new[] { spec.Id, "combat.accumulatedDamageWeightMin" }
                : new[] { spec.Id };
            var changed = vm.AllParameters.Where(p =>
                p.Id.StartsWith("combat.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p => p.Id)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            Check(changed.SequenceEqual(expectedIds.OrderBy(id => id, StringComparer.Ordinal)),
                name + ": changed more/other UI parameter values than intended: "
                + string.Join(", ", changed));
            if (isLinkedNoviceMax)
                Approx(Get(vm, "combat.accumulatedDamageWeightMin").CurrentNumber
                    .ToString(CultureInfo.InvariantCulture), 0.9);

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, output);
            Check(result.FileCount == 1 && result.ParameterCount == expectedIds.Length
                && result.EditCount == expectedIds.Length,
                name + ": unexpected written parameter/field count: "
                + result.FileCount + "/" + result.ParameterCount + "/" + result.EditCount);

            var file = Path.Combine(output, "Stalker2", "Content", "GameLite",
                "GameData", "DifficultyPrototypes",
                "DifficultyPrototypes_patch_TrueCustomDifficulty.cfg");
            Check(File.Exists(file), name + ": difficulty BPatch not generated");
            var document = Parse(File.ReadAllText(file));
            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            Check(document.Children.Count == 1 && document.Children.ContainsKey(sid),
                name + ": writer targeted wrong Base Preset prototype");
            var root = document.Get(sid);
            Check(root.Bpatch && root.Assignments.Count == 0
                && root.Children.Count == 1 && root.Children.ContainsKey(spec.Node),
                name + ": wrong root or unexpected extra difficulty section");
            var target = root.Get(spec.Node);
            Check(target.Bpatch && target.Children.Count == 0
                && target.Assignments.Count == expectedIds.Length,
                name + ": nested target should be a sparse BPatch with no collateral fields");

            foreach (var id in expectedIds)
            {
                var expected = id == spec.Id
                    ? spec
                    : DirectCombatSpecs.Single(p => p.Id == id);
                var baselinePoint = Source.EditPoints.Single(p =>
                    p.ParameterId == id && p.DifficultySid == sid);
                Check(baselinePoint.File == "GameData/DifficultyPrototypes.cfg"
                    && baselinePoint.Prototype == sid
                    && baselinePoint.NodePath.SequenceEqual(new[] { expected.Node })
                    && baselinePoint.Key == expected.Key
                    && baselinePoint.Operation == expected.Operation,
                    name + ": catalog baseline no longer matches pinned CFG source route " + id);
                var actualVm = Get(vm, id);
                var observed = target.Value(expected.Key);
                if (actualVm.IsToggle)
                {
                    Check(observed == (actualVm.CurrentBoolean ? "true" : "false"),
                        name + ": boolean serialization differs from UI state");
                }
                else
                {
                    var source = double.Parse(baselinePoint.SourceRaw, CultureInfo.InvariantCulture);
                    var expectedNumber = expected.Operation == "inverseScale"
                        ? source / actualVm.CurrentNumber
                        : actualVm.CurrentNumber;
                    Approx(observed, expectedNumber);
                }
            }

            _passed++;
            Console.WriteLine("PASS " + name);
        }
        finally
        {
            if (_keepRoot is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private static void MutantHealthCase(string parameterId)
    {
        var name = "mutant-health-" + parameterId["combat.mutantHealth.".Length..];
        var output = FixturePath(name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var parameter = Get(vm, parameterId);
            Check(!parameter.IsValueModifiedFromReference(),
                name + ": original UI baseline should be 1x");
            Set(vm, parameterId, 1.5);
            Approx(parameter.CurrentNumber.ToString(CultureInfo.InvariantCulture), 1.5);
            Check(parameter.IsValueModifiedFromReference(), name + ": UI state did not change");

            var records = Source.EditPoints.Where(p => p.ParameterId == parameterId).ToArray();
            var expectedVariantCount = parameterId is "combat.mutantHealth.bloodsucker"
                or "combat.mutantHealth.burer" ? 2 : 1;
            Check(records.Length == expectedVariantCount
                && records.All(p =>
                    p.File == "GameData/ObjPrototypes.cfg"
                    && p.Key == "MaxHP" && p.Operation == "scale"
                    && p.NodePath.SequenceEqual(new[] { "VitalParams" })),
                name + ": concrete MaxHP source path/variant inventory drifted");

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, output);
            Check(result.FileCount == 1 && result.ParameterCount == 1
                  && result.EditCount == expectedVariantCount,
                name + ": health writer emitted unexpected number of prototype edits");
            var file = Path.Combine(output, "Stalker2", "Content", "GameLite",
                "GameData", "ObjPrototypes",
                "ObjPrototypes_patch_TrueCustomDifficulty.cfg");
            Check(File.Exists(file), name + ": ObjPrototypes BPatch not written");
            var document = Parse(File.ReadAllText(file));
            Check(document.Children.Count == expectedVariantCount,
                name + ": unexpected extra mutant prototype written");
            foreach (var record in records)
            {
                var root = document.Get(record.Prototype);
                Check(root.Bpatch && root.Assignments.Count == 0
                      && root.Children.Count == 1,
                    name + ": unexpected node structure at " + record.Prototype);
                var vital = root.Get("VitalParams");
                Check(vital.Bpatch && vital.Assignments.Count == 1
                      && vital.Children.Count == 0,
                    name + ": MaxHP must be the only nested value");
                var baselineHp = double.Parse(record.SourceRaw, CultureInfo.InvariantCulture);
                Approx(vital.Value("MaxHP"), baselineHp * 1.5);
            }
            _passed++;
            Console.WriteLine("PASS " + name);
        }
        finally
        {
            if (_keepRoot is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private static string FixturePath(string name) =>
        Path.Combine(_keepRoot ?? Path.GetTempPath(),
            "TCD_Combat_Patch_Contract", name);

    // The UI marker click deliberately uses SnapCurrentNumberToMarker, not the
    // normal 0.1x drag grid. Verify all four real reference values round-trip.
    private static void TestMutantHitDamageReferenceMarkers()
    {
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Stalker" };
        var damage = Get(vm, "combat.mutantDamage");
        var references = new (string Name, double Value)[]
        {
            ("Novice", 0.35),
            ("Stalker", 1.0),
            ("Veteran", 1.35),
            ("Master", 1.5)
        };
        Check(damage.VanillaMarkers.Count == references.Length,
            "Mutant Hit Damage must expose exactly four difficulty markers");
        foreach (var (name, expected) in references)
        {
            var marker = damage.VanillaMarkers.Single(m =>
                string.Equals(m.Label, name, StringComparison.Ordinal));
            Check(Math.Abs(marker.Value - expected) < 0.000001,
                "Mutant Hit Damage reference marker value drifted for " + name);
            damage.SnapCurrentNumberToMarker(marker.Value);
            Check(Math.Abs(damage.CurrentNumber - expected) < 0.000001,
                "Mutant Hit Damage marker must snap exactly to " + name);
        }
        _passed++;
    }

    private static void Case(string name, string difficulty, Action<MainViewModel> modify,
        Action<Node, CfgWriteResult> verify)
    {
        // Isolate test output and references. Do not use the user's game installation.
        var root = Path.Combine(_keepRoot ?? Path.GetTempPath(),
            "TCD_Combat_Patch_Contract", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            modify(vm);
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, root);
            var patch = Path.Combine(root, "Stalker2", "Content", "GameLite", "GameData",
                "DifficultyPrototypes", "DifficultyPrototypes_patch_TrueCustomDifficulty.cfg");
            if (result.FileCount == 0)
            {
                verify(new Node("empty", ""), result);
            }
            else
            {
                Check(result.FileCount == 1 && File.Exists(patch),
                    $"{name}: expected exactly one DifficultyPrototypes BPatch output");
                var document = Parse(File.ReadAllText(patch));
                var sid = Catalog.DifficultyProfiles[difficulty].Sid;
                Check(document.Children.Count == 1 && document.Children.ContainsKey(sid),
                    $"{name}: wrong or additional difficulty root");
                var difficultyRoot = document.Get(sid);
                Check(difficultyRoot.Bpatch, $"{name}: difficulty root must use bpatch");
                verify(difficultyRoot, result);
            }
            _passed++;
            Console.WriteLine("PASS " + name);
        }
        finally
        {
            if (_keepRoot is null && Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static void MasterPsy(Node root, string onlyChangedKey, string expectedValue)
    {
        var npc = root.Get("NPCCombatDifficulty");
        var container = npc.Get("PsyPhantomNPCOverrides");
        Check(npc.Bpatch && container.Bpatch && container.Children.Count == 2,
            "Master PSY entries must both use BPatch");
        foreach (var entry in Source.Special.PsyOverrides.Values)
        {
            var actual = container.Get(entry.Index);
            Check(actual.Bpatch && actual.Value("OverrideEffectPrototypeSID") == entry.EffectSid,
                "Master PSY effect identity drift");
            Check(actual.Assignments.Count == 2 && actual.Value(onlyChangedKey) == expectedValue,
                "Master PSY changed unexpected fields");
        }
    }

    private static void FullPsy(Node root, bool bleed)
    {
        var npc = root.Get("NPCCombatDifficulty");
        var container = npc.Get("PsyPhantomNPCOverrides");
        Check(npc.Bpatch && !container.Bpatch && container.Children.Count == 2,
            "non-Master PSY must insert two full overrides, not sparse BPatch");
        foreach (var entry in Source.Special.PsyOverrides.Values)
        {
            var actual = container.Get(entry.Index);
            Check(!actual.Bpatch && actual.Value("OverrideEffectPrototypeSID") == entry.EffectSid,
                "non-Master PSY effect identity drift");
            Check(actual.Assignments.Count == 5,
                "non-Master PSY entry must contain four authored controls plus effect SID");
            Check(actual.Value("bPsyNPCApplyBleeding") == (bleed ? "true" : "false"),
                "non-Master PSY bleeding scalar drift");
            Approx(actual.Value("PSYDelayBeforeNewSpawnCycleModifier"), 1);
            Approx(actual.Value("PsyPhantomNPCCountMultiplier"), 1);
            Approx(actual.Value("PsyNPCDurabilityDamageMultiplier"), 1);
        }
    }

    private static ParameterViewModel Get(MainViewModel vm, string id) =>
        vm.AllParameters.Single(x => x.Id == id);

    private static void Set(MainViewModel vm, string id, double value) =>
        Get(vm, id).SnapCurrentNumberToMarker(value);

    private static void Approx(string raw, double expected)
    {
        var value = double.Parse(raw, CultureInfo.InvariantCulture);
        Check(Math.Abs(value - expected) < 0.000001,
            $"scalar {value} does not equal {expected}");
    }

    private static Node Parse(string source)
    {
        var root = new Node("document", "");
        var stack = new Stack<Node>();
        stack.Push(root);
        foreach (var rawLine in source.Replace("\r", "").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (line == "struct.end")
            {
                Check(stack.Count > 1, "CFG unmatched struct.end");
                stack.Pop();
                continue;
            }
            var start = Regex.Match(line, @"^(?<name>\S+)\s*:\s*struct\.begin(?<flags>.*)$");
            if (start.Success)
            {
                var node = new Node(start.Groups["name"].Value, start.Groups["flags"].Value.Trim());
                Check(stack.Peek().Children.TryAdd(node.Name, node),
                    "duplicate CFG child: " + node.Name);
                stack.Push(node);
                continue;
            }
            var assignment = Regex.Match(line, @"^(?<name>\S+)\s*=\s*(?<value>.*)$");
            Check(assignment.Success, "unparsed CFG line: " + line);
            Check(stack.Peek().Assignments.TryAdd(
                    assignment.Groups["name"].Value, assignment.Groups["value"].Value),
                "duplicate CFG scalar assignment: " + line);
        }
        Check(stack.Count == 1, "CFG missing closing struct.end");
        return root;
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidDataException(message);
    }

    private sealed class Node(string name, string flags)
    {
        public string Name { get; } = name;
        public bool Bpatch { get; } = flags.Contains("{bpatch}", StringComparison.Ordinal);
        public Dictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Assignments { get; } = new(StringComparer.Ordinal);
        public Node Get(string key) => Children.TryGetValue(key, out var node)
            ? node : throw new InvalidDataException($"CFG {Name} missing child {key}");
        public string Value(string key) => Assignments.TryGetValue(key, out var value)
            ? value : throw new InvalidDataException($"CFG {Name} missing assignment {key}");
    }
}
