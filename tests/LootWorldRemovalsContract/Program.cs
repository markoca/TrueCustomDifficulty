using System.Globalization;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootWorldRemovalsContract;

// Independent randomized outcome contract: every removed physical actor must
// belong to the corresponding ORIGINAL manifest; no seed or sample is assumed.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly string[] Difficulty = { "Novice", "Stalker", "Veteran", "Master" };
    private static readonly Dictionary<string, string> Categories = new(StringComparer.Ordinal)
    {
        ["loot.worldAmmoCratesRemoved"] = "ammo",
        ["loot.worldFoodDrinkCratesRemoved"] = "food_drink",
        ["loot.worldMedicalCratesRemoved"] = "medical"
    };
    private static readonly string[] Ids =
    {
        "loot.worldAmmoCratesRemoved", "loot.worldFoodDrinkCratesRemoved",
        "loot.worldMedicalCratesRemoved", "loot.looseWorldLootRemoved",
        "loot.worldF1GrenadeBoxes"
    };
    private static readonly List<Actor> Candidates = LoadActors();
    private static string? Keep;
    private static int Passed;

    private sealed record Actor(string ParameterId, string Scope, string SourceFile,
        string LevelName, string ActorSid, string Category, string Identity)
    {
        public string Key => SourceFile + "|" + LevelName + "|" + ActorSid;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 0)
            {
                if (args.Length != 2 || args[0] != "--out")
                    throw new ArgumentException("Usage: LootWorldRemovalsContract [--out PATH]");
                Keep = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(Keep);
            }
            var vm = new MainViewModel(Catalog);
            Check(Ids.All(id => vm.AllParameters.Any(p => p.Id == id && p.IsSlider && p.IsControlEnabled)),
                "Five independent World physical-loot source controls must be live");
            Check(Candidates.Count == 1315 + 1208 + 868 + 7233 + 101,
                "Original physical actor inventory count drift");
            Check(Candidates.Select(a => a.Key).Distinct(StringComparer.Ordinal).Count() == Candidates.Count,
                "World physical actor domain manifests overlap: duplicated physical actor identity");
            Check(Candidates.GroupBy(a => a.ParameterId).All(g =>
                g.Select(x => x.ActorSid).Distinct(StringComparer.Ordinal).Count() == g.Count()),
                "World physical actor manifest contains repeated ActorSID");
            Check(Candidates.Where(a => a.ParameterId == "loot.worldF1GrenadeBoxes").All(x =>
                x.Identity == "D_WoodenAmmoCrate_01|DestructibleStash_F1"),
                "World F1 box category is no longer specifically dedicated");
            Check(Candidates.Where(a => Categories.ContainsKey(a.ParameterId)).All(a =>
                a.Category == Categories[a.ParameterId]),
                "World box category/family mapping drift");
            Check(Candidates.Count(a => a.ParameterId == "loot.looseWorldLootRemoved"
                && a.Scope == "baseGame") == 5996
                && Candidates.Count(a => a.ParameterId == "loot.looseWorldLootRemoved"
                    && a.Scope == "dlc1") == 1237, "Loose physical actor source DLC scope drift");

            foreach (var d in Difficulty) Run(d, false, new(), "vanilla");
            foreach (var id in Ids)
            {
                Run("Veteran", false, new() { [id] = 0.25 }, "quarter-base");
                Run("Veteran", true, new() { [id] = 0.25 }, "quarter-dlc1");
                Run("Veteran", false, new() { [id] = 1.0 }, "all-base");
                Run("Veteran", true, new() { [id] = 1.0 }, "all-dlc1");
            }
            Run("Veteran", false, new() {
                ["loot.worldAmmoCratesRemoved"] = 0.5,
                ["loot.worldMedicalCratesRemoved"] = 0.5
            }, "ammo-plus-medical");
            Run("Veteran", true, new() {
                ["loot.worldFoodDrinkCratesRemoved"] = 0.25,
                ["loot.worldF1GrenadeBoxes"] = 0.5
            }, "food-plus-F1-dlc1");
            Run("Veteran", true, new() {
                ["loot.looseWorldLootRemoved"] = 0.01,
                ["loot.worldAmmoCratesRemoved"] = 0.01,
                ["loot.worldFoodDrinkCratesRemoved"] = 0.01,
                ["loot.worldMedicalCratesRemoved"] = 0.01,
                ["loot.worldF1GrenadeBoxes"] = 0.01
            }, "five-way-collision-isolation");
            Check(Passed == 27, "Expected 27 production world actor-removal scenarios");
            Console.WriteLine("PASS: " + Passed + " randomized World Loot source-to-removenode writer cases");
            Console.WriteLine("NOT VERIFIED: source manifest vs decoded original ZIP, Unreal loading or gameplay");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex); return 1; }
    }

    private static void Run(string difficulty, bool dlc, Dictionary<string, double> changes,
        string label)
    {
        var root = Path.Combine(Keep ?? Path.GetTempPath(), "TCD_World_Removals",
            Passed + "-" + label + (dlc ? "-dlc" : "-base"));
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            foreach (var (id, value) in changes)
            {
                var p = vm.AllParameters.Single(x => x.Id == id);
                Check(p.IsControlEnabled && p.IsSlider && !p.IsValueModifiedFromReference(),
                    label + ": expected untouched physical-loot source");
                p.SnapCurrentNumberToMarker(value);
                Check(Math.Abs(p.CurrentNumber - value) < 0.0000001 &&
                    p.IsValueModifiedFromReference(), label + ": actual slider change failed " + id);
            }
            var changed = vm.AllParameters.Where(p => p.Id.StartsWith("loot.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(changed.SetEquals(changes.Keys), label + ": collateral source edits from visual aggregate");

            var byKey = Candidates.Where(a => changes.ContainsKey(a.ParameterId)
                && (dlc || a.Scope != "dlc1")).GroupBy(x => x.ParameterId)
                .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
            var wantedCounts = byKey.ToDictionary(kv => kv.Key, kv =>
                (int)Math.Round(kv.Value.Length * changes[kv.Key],
                    MidpointRounding.AwayFromZero), StringComparer.Ordinal);
            var intended = wantedCounts.Values.Sum();
            var result = new CfgPatchWriterService().Write(vm, Catalog,
                dlc ? new GameInstallInfo { HasDlc1 = true } : null, root);
            var paths = Directory.GetFiles(root, "*.cfg", SearchOption.AllDirectories);
            Check(paths.Length == intended, label + ": physical output file count " +
                paths.Length + " expected " + intended);
            Check(result.FileCount == intended && result.EditCount == intended &&
                result.ParameterCount == changes.Count,
                label + ": writer file/edit/parameter counts differ (" +
                result.FileCount + "/" + result.EditCount + "/" + result.ParameterCount + ")");
            var keys = byKey.Values.SelectMany(x => x).ToDictionary(x => x.Key, StringComparer.Ordinal);
            var checkedCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var observed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in paths)
            {
                var expectedActor = CandidateKey(root, file);
                Check(keys.TryGetValue(expectedActor, out var actor),
                    label + ": output touched non-eligible physical world actor: " + expectedActor);
                Check(observed.Add(expectedActor), label + ": duplicate world actor output " + expectedActor);
                if (!checkedCounts.TryAdd(actor!.ParameterId, 1))
                    checkedCounts[actor.ParameterId]++;
                var text = File.ReadAllText(file);
                Check(text.Contains(actor.ActorSid + " : removenode", StringComparison.Ordinal),
                    label + ": wrong actor removal instruction " + file);
                Check(text.Split('\n').Count(x => x.Contains(": removenode", StringComparison.Ordinal)) == 1,
                    label + ": unrelated removal command " + file);
                Check(!text.Contains("struct.begin", StringComparison.Ordinal),
                    label + ": world removal replaced by unexpected mutation " + file);
                if (!dlc) Check(!file.Contains("DLCGameData", StringComparison.OrdinalIgnoreCase),
                    label + ": emitted DLC1 actor without installed DLC");
            }
            foreach (var (id, wanted) in wantedCounts)
                Check(checkedCounts.GetValueOrDefault(id) == wanted,
                    label + ": wrong individual domain count " + id + ": " +
                    checkedCounts.GetValueOrDefault(id) + " instead of " + wanted);
            Console.WriteLine("PASS " + difficulty + " " + label + " (" +
                paths.Length + " physically eligible removenode files)");
            Passed++;
        }
        finally { if (Keep is null && Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static string CandidateKey(string root, string file)
    {
        var relative = Path.GetRelativePath(Path.Combine(root, "Stalker2", "Content", "GameLite"), file)
            .Replace('\\', '/');
        var parts = relative.Split('/');
        Check(parts.Length >= 4 && parts[^2] == "WorldMap_WP",
            "Nonstandard SpawnActorPrototypes layout " + relative);
        var actorSid = Path.GetFileNameWithoutExtension(parts[^1]);
        var source = string.Join("/", parts.Take(parts.Length - 3)) + ".cfg";
        return source + "|WorldMap_WP|" + actorSid;
    }

    private static List<Actor> LoadActors()
    {
        var rows = new List<Actor>();
        foreach (var c in Csv("crate-spawn-production-manifest.csv", 9))
            rows.Add(new Actor(c[0], c[2] == "dlc1" ? "dlc1" : "baseGame", c[3],
                c[4], c[5], c[1], c[6] + "|" + c[7]));
        foreach (var c in Csv("loose-world-loot-manifest.csv", 7))
            rows.Add(new Actor("loot.looseWorldLootRemoved", c[2], c[3],
                c[4], c[5], "loose", c[6]));
        foreach (var c in Csv("world-f1-grenade-box-manifest.csv", 6))
            rows.Add(new Actor("loot.worldF1GrenadeBoxes", c[0] == "dlc1" ? "dlc1" : "baseGame",
                c[1], c[2], c[3], "F1", c[4] + "|" + c[5]));
        return rows;
    }

    private static IEnumerable<string[]> Csv(string file, int columns)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        string? path = null;
        while (dir is not null)
        {
            var name = Path.Combine(dir.FullName, "src", "TrueCustomDifficulty", "Data", file);
            if (File.Exists(name)) { path = name; break; }
            dir = dir.Parent;
        }
        if (path is null) throw new FileNotFoundException("Missing original production manifest " + file);
        foreach (var line in File.ReadLines(path).Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var cells = line.Split(',');
            Check(cells.Length == columns, file + " CSV shape changed");
            yield return cells;
        }
    }

    private static void Check(bool ok, string reason)
    {
        if (!ok) throw new InvalidDataException(reason);
    }
}
