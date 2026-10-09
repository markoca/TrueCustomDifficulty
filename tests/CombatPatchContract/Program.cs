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

            Case("Master-no-changes", "Master", _ => { }, (_, result) =>
                Check(result.FileCount == 0 && result.EditCount == 0,
                    "unchanged Master must not emit any CFG"));

            Case("Veteran-no-changes", "Veteran", _ => { }, (_, result) =>
                Check(result.FileCount == 0 && result.EditCount == 0,
                    "unchanged Veteran must not emit any CFG"));

            Case("Master-Chimera-only", "Master",
                vm => Set(vm, "combat.mutantCooldown.chimera", 1.4),
                (root, result) =>
                {
                    Check(result.EditCount == 6 && result.ParameterCount == 1,
                        "Master Chimera must emit precisely six edits / one parameter");
                    MasterCooldown(root, new[] { "Chimera" }, 1.4);
                    Check(!root.Children.ContainsKey("NPCCombatDifficulty"),
                        "mutant-only edit unexpectedly added PSY branch");
                });

            Case("Master-Burer-only", "Master",
                vm => Set(vm, "combat.mutantCooldown.burer", 1.2),
                (root, result) =>
                {
                    Check(result.EditCount == 12 && result.ParameterCount == 1,
                        "Master Burer (two agent variants) must emit 12 edits");
                    MasterCooldown(root, new[] { "Burer", "Burer_ShootingSpecial" }, 1.2);
                    foreach (var sid in new[] { "Burer", "Burer_ShootingSpecial" })
                    {
                        var family = Family(sid);
                        Check(family.Cooldowns.Count(x => x.Tag == "Ability.Cooldown.WeaponDrag") == 2,
                            $"must preserve distinct duplicate WeaponDrag indices: {sid}");
                    }
                });

            foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran" })
            {
                Case(difficulty + "-Chimera-only", difficulty,
                    vm => Set(vm, "combat.mutantCooldown.chimera", 1.4),
                    (root, result) =>
                    {
                        var authoredCount = Source.Special.MutantCooldownFamilies.Values
                            .Sum(x => x.Cooldowns.Count);
                        Check(result.EditCount == authoredCount && result.ParameterCount == 1,
                            "non-Master inserted cooldown array must contain all authored tags, one edited parameter");
                        FullCooldown(root, "Chimera", 1.4);
                        Check(!root.Children.ContainsKey("NPCCombatDifficulty"),
                            "cooldown-only patch unexpectedly added PSY branch");
                    });
            }

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

            Case("Master-combined", "Master",
                vm =>
                {
                    Set(vm, "combat.mutantCooldown.chimera", 1.4);
                    Set(vm, "combat.psyPhantomCount", 2.5);
                },
                (root, result) =>
                {
                    Check(result.EditCount == 8 && result.ParameterCount == 2,
                        "Master combined writer must emit only 6 cooldown + 2 PSY edits");
                    MasterCooldown(root, new[] { "Chimera" }, 1.4);
                    MasterPsy(root, "PsyPhantomNPCCountMultiplier", "2.5");
                });

            Case("Veteran-combined", "Veteran",
                vm =>
                {
                    Set(vm, "combat.mutantCooldown.chimera", 1.4);
                    Get(vm, "combat.psyBleeding").CurrentBoolean = true;
                },
                (root, result) =>
                {
                    var count = Source.Special.MutantCooldownFamilies.Values.Sum(x => x.Cooldowns.Count);
                    Check(result.EditCount == count + 8 && result.ParameterCount == 2,
                        "Veteran combined writer must emit full cooldown and PSY arrays");
                    FullCooldown(root, "Chimera", 1.4);
                    FullPsy(root, bleed: true);
                });

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

    private static void MasterCooldown(Node root, IReadOnlyList<string> onlySids, double multiplier)
    {
        var parent = root.Get("MutantCombatDifficulty");
        var container = parent.Get("AgentCooldownMultipliers");
        Check(parent.Bpatch && container.Bpatch,
            "Master cooldown parent and array must be bpatch updates");
        Check(container.Children.Count == onlySids.Count,
            "Master patch contains an unrelated mutant family (aggregate anchor leak)");
        foreach (var sid in onlySids)
        {
            var family = Family(sid);
            Check(container.Children.Keys.Contains(family.Index),
                $"missing Master cooldown agent {sid}");
            var entry = container.Get(family.Index);
            Check(entry.Bpatch && entry.Value("AgentPrototypeSID") == sid,
                $"wrong Master agent at {family.Index}");
            var list = entry.Get("CooldownMultipliers");
            Check(list.Bpatch && list.Children.Count == family.Cooldowns.Count,
                $"{sid}: Master cooldown list must be sparse BPatch");
            foreach (var item in family.Cooldowns)
            {
                var field = list.Get(item.Index);
                Check(field.Bpatch, $"{sid}: Master cooldown item must bpatch");
                Check(field.Value("CooldownTag") == item.Tag,
                    $"{sid} {item.Index}: cooldown tag drift");
                Approx(field.Value("CooldownMultiplier"), multiplier);
            }
        }
    }

    private static void FullCooldown(Node root, string modifiedAgent, double modifiedValue)
    {
        var parent = root.Get("MutantCombatDifficulty");
        var container = parent.Get("AgentCooldownMultipliers");
        Check(parent.Bpatch && !container.Bpatch,
            "non-Master cooldown must replace absent array, not sparse-bpatch indices");
        Check(container.Children.Count == 14,
            "non-Master cooldown insertion must contain all 14 original agent SIDs");
        foreach (var family in Source.Special.MutantCooldownFamilies.Values)
        {
            var entry = container.Get(family.Index);
            Check(!entry.Bpatch && entry.Value("AgentPrototypeSID") == family.AgentPrototypeSid,
                "wrong non-Master agent SID / index");
            var list = entry.Get("CooldownMultipliers");
            Check(!list.Bpatch && list.Children.Count == family.Cooldowns.Count,
                $"{family.AgentPrototypeSid}: cooldown tag inventory drift");
            var isModified = family.AgentPrototypeSid == modifiedAgent;
            var expected = isModified ? modifiedValue : 1.0;
            foreach (var item in family.Cooldowns)
            {
                var tag = list.Get(item.Index);
                Check(!tag.Bpatch && tag.Value("CooldownTag") == item.Tag,
                    $"{family.AgentPrototypeSid} {item.Index}: cooldown tag/index drift");
                Approx(tag.Value("CooldownMultiplier"), expected);
            }
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

    private static CfgWriterCooldownFamily Family(string sid) =>
        Source.Special.MutantCooldownFamilies.Values.Single(x => x.AgentPrototypeSid == sid);

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
