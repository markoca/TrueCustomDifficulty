using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace ALifePatchContract;

// Tests real production ViewModel -> CFG writer. Unreal/gameplay is not executed.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    private static readonly string[] Difficulties = { "Novice", "Stalker", "Veteran", "Master" };
    private const string Source = "GameData/ALifePrototypes/ALifeDirectorScenarioPrototypes.cfg";
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
                    throw new ArgumentException("Usage: ALifePatchContract [--out PATH]");
                _keep = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(_keep);
            }

            var catalogIds = Catalog.Parameters.Where(p => p.Id.StartsWith("alife.", StringComparison.Ordinal))
                .Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            var actualVm = new MainViewModel(Catalog);
            var live = actualVm.AllParameters.Where(p => p.Id.StartsWith("alife.", StringComparison.Ordinal))
                .ToArray();
            var ids = live.Select(p => p.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var sourceOnly = catalogIds.Except(ids, StringComparer.Ordinal).ToArray();
            Check(catalogIds.Count == 19 && ids.Length == 18, "Expected 19 catalog / 18 active A-Life sources");
            Check(sourceOnly.SequenceEqual(new[] { "alife.mutantSpawn.generic" }),
                "MutantGeneric must remain a hidden source-only mapping, not a normal UI control");
            Check(live.All(p => p.IsSlider && p.IsControlEnabled), "Missing enabled live A-Life sliders");

            var all = Baseline.EditPoints.Where(p => p.ParameterId.StartsWith("alife.", StringComparison.Ordinal))
                .ToArray();
            Check(all.Length == 64 && all.All(p => p.Scope == "baseGame" && p.File == Source
                && p.Prototype == "ALifeDirectorPreset" && p.Key == "MaxCount"),
                "Expected 64 exactly scoped ALife Director MaxCount source rows");
            Check(all.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal).SetEquals(catalogIds),
                "A-Life catalog / direct writer point ID mismatch");
            Check(all.Count(p => p.Operation == "scaleIntegerHalfUp") == 4
                && all.Count(p => p.Operation == "scaleIntegerHalfUpPreserveZero") == 60,
                "Integer half-up source operations changed");
            Check(all.Count(p => p.ParameterId == "alife.mutantSpawn.generic") == 4,
                "Generic non-UI archetype must stay mapped, not promoted to a slider");
            Check(all.Count(p => p.SourceRaw == "0") == 2
                && all.Where(p => p.SourceRaw == "0").All(p =>
                    p.NodePath[1] == "[0]" && p.Operation == "scaleIntegerHalfUpPreserveZero"),
                "Newbie Bloodsucker and Controller source zeros changed");

            var ranks = new[] { "[0]", "[1]", "[2]", "[3]" };
            var species = new Dictionary<int, string>
            {
                [0] = "humanGroupSize", [1] = "mutantSpawn.generic",
                [2] = "mutantSpawn.bloodsucker", [3] = "mutantSpawn.blindDog",
                [4] = "mutantSpawn.flesh", [5] = "mutantSpawn.controller",
                [6] = "mutantSpawn.poltergeist", [7] = "mutantSpawn.snork",
                [8] = "mutantSpawn.boar", [9] = "mutantSpawn.chimera",
                [10] = "mutantSpawn.bayun", [11] = "mutantSpawn.tushkan",
                [12] = "mutantSpawn.ratSwarm", [13] = "mutantSpawn.burer",
                [14] = "mutantSpawn.pseudodog", [15] = "mutantSpawn.deer"
            };
            var humanRanks = new[] { "newbie", "experienced", "veteran", "master" };
            foreach (var point in all)
            {
                Check(point.NodePath.Count == 4 && point.NodePath[0] == "ALifeScenarioNPCArchetypesLimitsPerPlayerRank"
                    && point.NodePath[2] == "Restrictions", "Wrong A-Life nested array shape");
                var rank = Array.IndexOf(ranks, point.NodePath[1]);
                var slot = Array.FindIndex(species.Keys.OrderBy(n => n).ToArray(),
                    idx => "[" + idx.ToString(CultureInfo.InvariantCulture) + "]" == point.NodePath[3]);
                Check(rank >= 0 && slot >= 0, "A-Life writer array index out of supported range");
                var wantedId = slot == 0 ? "alife.humanGroupSize." + humanRanks[rank]
                    : "alife." + species[slot];
                Check(point.ParameterId == wantedId, "A-Life rank/species array ownership drift: " + point.ParameterId);
            }

            foreach (var difficulty in Difficulties)
                Case(difficulty, new Dictionary<string, double>(), "vanilla");
            foreach (var id in ids)
                foreach (var difficulty in Difficulties)
                    Case(difficulty, new Dictionary<string, double> { [id] = 2.0 }, "single");
            foreach (var id in new[] {
                "alife.humanGroupSize.newbie", "alife.humanGroupSize.master",
                "alife.mutantSpawn.bloodsucker", "alife.mutantSpawn.controller",
                "alife.mutantSpawn.blindDog", "alife.mutantSpawn.tushkan" })
            {
                Case("Veteran", new Dictionary<string, double> { [id] = 0.25 }, "min");
                Case("Veteran", new Dictionary<string, double> { [id] = 3.0 }, "max");
            }

            Case("Veteran", new() {
                ["alife.humanGroupSize.newbie"] = 0.5,
                ["alife.humanGroupSize.master"] = 1.5 }, "two-human-ranks");
            Case("Veteran", new() {
                ["alife.mutantSpawn.bloodsucker"] = 3.0,
                ["alife.mutantSpawn.controller"] = 2.0 }, "two-zero-preserving-mutants");
            Case("Veteran", new() {
                ["alife.mutantSpawn.blindDog"] = 0.5,
                ["alife.mutantSpawn.tushkan"] = 1.5 }, "two-mutant-species");
            Case("Veteran", new() {
                ["alife.humanGroupSize.veteran"] = 2.0,
                ["alife.mutantSpawn.bloodsucker"] = 3.0 }, "human-mutant-intersection");
            Case("Veteran", new() {
                ["alife.humanGroupSize.experienced"] = 0.25,
                ["alife.mutantSpawn.boar"] = 3.0 }, "rank-and-boar");
            Case("Veteran", new() {
                ["alife.mutantSpawn.bayun"] = 2.0,
                ["alife.mutantSpawn.burer"] = 2.0 }, "parallel-muted-radar-members");
            Check(_passed == 94, "Expected exactly 94 actual A-Life writer scenarios");
            Console.WriteLine($"PASS: {_passed} A-Life production BPatch writer scenarios");
            Console.WriteLine("NOT VERIFIED: original decoded ZIP values, Unreal array merge/load, in-game population");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine("FAIL: " + e); return 1; }
    }

    private static void Case(string difficulty, Dictionary<string, double> values, string kind)
    {
        var outPath = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_ALife_Patch",
            difficulty + "-" + kind + "-" + _passed.ToString(CultureInfo.InvariantCulture));
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            foreach (var (id, value) in values)
            {
                var parameter = vm.AllParameters.Single(p => p.Id == id);
                Check(parameter.IsSlider && parameter.IsControlEnabled && !parameter.IsValueModifiedFromReference(),
                    kind + ": missing vanilla live source: " + id);
                parameter.SnapCurrentNumberToMarker(value);
                Check(parameter.IsValueModifiedFromReference() && Math.Abs(parameter.CurrentNumber - value) < 0.0000011,
                    kind + ": failed to set actual source control: " + id);
            }
            var edited = vm.AllParameters.Where(p => p.Id.StartsWith("alife.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(edited.SetEquals(values.Keys), kind + ": editor unexpectedly changed another A-Life source");

            var expectedPoints = Baseline.EditPoints.Where(p => values.ContainsKey(p.ParameterId))
                .Where(p => Math.Abs(Value(vm.AllParameters.Single(x => x.Id == p.ParameterId), p)
                    - double.Parse(p.SourceRaw, CultureInfo.InvariantCulture)) > 0.0000001).ToArray();
            Check(expectedPoints.All(p => p.Scope == "baseGame" && p.File == Source),
                kind + ": unexpected DLC1 / unrelated CFG edit target");
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, outPath);
            var file = Path.Combine(outPath, "Stalker2", "Content", "GameLite", "GameData",
                "ALifePrototypes", "ALifeDirectorScenarioPrototypes", "ALifeDirectorScenarioPrototypes_patch_TrueCustomDifficulty.cfg");
            var expectedFiles = expectedPoints.Length == 0 ? Array.Empty<string>() : new[] { file };
            var actualFiles = Directory.GetFiles(outPath, "*TrueCustomDifficulty*", SearchOption.AllDirectories);
            Check(actualFiles.ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(expectedFiles), kind + ": unexpected / missing CFG output");
            Check(result.ParameterCount == values.Count && result.FileCount == expectedFiles.Length
                && result.EditCount == expectedPoints.Length,
                kind + ": production writer file/control/field count disagreement");

            if (expectedPoints.Length > 0)
            {
                var parsed = Parse(File.ReadAllText(file));
                var wanted = expectedPoints.Select(p => p.Prototype + "/" +
                    string.Join("/", p.NodePath) + "/" + p.Key).ToHashSet(StringComparer.Ordinal);
                Check(parsed.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(wanted),
                    kind + ": missing or collateral BPatch leaves");
                foreach (var p in expectedPoints)
                {
                    var key = p.Prototype + "/" + string.Join("/", p.NodePath) + "/" + p.Key;
                    Check(parsed.TryGetValue(key, out var raw) &&
                        Math.Abs(double.Parse(raw!, CultureInfo.InvariantCulture) -
                            Value(vm.AllParameters.Single(x => x.Id == p.ParameterId), p)) < 0.0000011,
                        kind + ": wrong integer half-up transform: " + key);
                }
                Check(parsed.Keys.All(key => key.EndsWith("/MaxCount", StringComparison.Ordinal)),
                    kind + ": authored Rank, AgentType or timing overwritten");
            }
            Console.WriteLine("PASS " + difficulty + "-" + kind + " (" + expectedPoints.Length + " emitted fields)");
            _passed++;
        }
        finally { if (_keep is null && Directory.Exists(outPath)) Directory.Delete(outPath, recursive: true); }
    }

    private static double Value(ParameterViewModel par, CfgWriterEditPoint point)
    {
        var source = double.Parse(point.SourceRaw, CultureInfo.InvariantCulture);
        if (point.Operation == "scaleIntegerHalfUp")
            return Math.Max(1, Math.Floor(source * par.CurrentNumber + 0.5));
        if (point.Operation == "scaleIntegerHalfUpPreserveZero")
            return source == 0 ? 0 : Math.Max(0, Math.Floor(source * par.CurrentNumber + 0.5));
        throw new InvalidDataException("Unmapped A-Life production operation: " + point.Operation);
    }

    private static Dictionary<string, string> Parse(string cfg)
    {
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        var nodeSet = new HashSet<string>(StringComparer.Ordinal);
        var stack = new List<string>();
        foreach (var raw in cfg.Replace("\r", "").Split('\n'))
        {
            var s = raw.Trim();
            if (s.Length == 0 || s.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (s == "struct.end")
            {
                Check(stack.Count > 0, "Unmatched CFG struct.end");
                stack.RemoveAt(stack.Count - 1);
                continue;
            }
            var open = Regex.Match(s, @"^(?<name>\S+)\s*:\s*struct\.begin(?<attrs>.*)$");
            if (open.Success)
            {
                Check(open.Groups["attrs"].Value.Contains("{bpatch}", StringComparison.Ordinal),
                    "Missing production {bpatch} tag: " + s);
                stack.Add(open.Groups["name"].Value);
                Check(nodeSet.Add(string.Join("/", stack)), "Duplicate CFG array/prototype node");
                continue;
            }
            var field = Regex.Match(s, @"^(?<key>\S+)\s*=\s*(?<value>.*)$");
            Check(field.Success && stack.Count > 0, "Unexpected BPatch statement: " + s);
            Check(paths.TryAdd(string.Join("/", stack) + "/" + field.Groups["key"].Value,
                field.Groups["value"].Value), "Duplicate BPatch leaf");
        }
        Check(stack.Count == 0, "Unterminated BPatch nesting");
        return paths;
    }

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
