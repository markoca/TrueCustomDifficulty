using System.Globalization;
using System.IO;
using System.Reflection;
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
            Check(defs.Length == 94, "Expected 94 Economy catalog definitions (23 existing + 40 trader buys + 31 trader sells)");
            var active = new MainViewModel(Catalog).AllParameters.Select(p => p.Id)
                .ToHashSet(StringComparer.Ordinal);
            Check(defs.Where(p => !active.Contains(p.Id)).Select(p => p.Id)
                .SequenceEqual(new[] { "economy.repairDifficultyCost" }),
                "Unexpected source-only Economy parameters");
            var points = Baseline.EditPoints.Where(p => p.ParameterId.StartsWith("economy.", StringComparison.Ordinal)).ToArray();
            Check(points.Length == 409, "Expected 409 direct Economy fields");
            Check(points.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal)
                .SetEquals(defs.Where(p => p.Id != "economy.traderMinBuyCondition"
                    && !p.Id.StartsWith(TraderBuysPolicy.Prefix, StringComparison.Ordinal)
                    && !p.Id.StartsWith(TraderSellsPolicy.Prefix, StringComparison.Ordinal)).Select(p => p.Id)),
                "Economy catalog / manifest mismatch");
            Check(points.Count(p => p.Scope == "dlc1") == 1, "Economy direct DLC1 count drift");
            Check(points.All(p => new[] { "set", "scale", "scaleNonZero", "scaleIntegerHalfUp" }
                .Contains(p.Operation, StringComparer.Ordinal)), "Unsupported operation in Economy direct manifest");

            foreach (var def in defs.Where(p => active.Contains(p.Id)
                && p.Id != "economy.traderMinBuyCondition"
                && !p.Id.StartsWith(TraderBuysPolicy.Prefix, StringComparison.Ordinal)
                && !p.Id.StartsWith(TraderSellsPolicy.Prefix, StringComparison.Ordinal)))
                TestDirect(def.Id, "Veteran");
            foreach (var id in new[] { "economy.buyCondition", "economy.sellCondition",
                         "economy.mainQuestReward", "economy.sideQuestReward" })
                foreach (var difficulty in new[] { "Novice", "Stalker", "Master" })
                    TestDirect(id, difficulty);
            TestTraderThreshold();
            TestTraderBuyLimitationsPatchShape();
            TestTraderBuysNeutral();
            TestTraderBuysAllRoles();
            TestTraderBuysCombinedWithCondition();
            TestTraderSellsNeutral();
            TestTraderSellsFiveRoles();
            TestTraderSellsDlcOnly();
            TestTraderSellsCombinedWithBuys();
            TestLinkedRoamingGuides();
            Check(_passed == 47, "Expected exactly 47 Economy writer scenarios");
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
            Check(!par.IsValueModifiedFromReference(), label + ": neutral default must not patch vanilla exceptions");
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
            var nativeZeroSids = new[] { "SelmaTrader_TradePrototype", "Trader_Soviet_Rostok_TradePrototype" };
            Check(baseRows.Count(row => nativeZeroSids.Contains(row[2], StringComparer.Ordinal)) == 2
                && baseRows.Where(row => nativeZeroSids.Contains(row[2], StringComparer.Ordinal))
                    .All(row => row[6] == "0.0f")
                && csv.Where(row => !nativeZeroSids.Contains(row[2], StringComparer.Ordinal))
                    .All(row => row[6] == "0.4f"),
                label + ": original two 0%-condition trader baselines were lost");
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

    // Invoke the *actual production PatchDocument/PatchNode serializer* without
    // registering a new gameplay parameter. This is a format/merge contract
    // only, NOT an Unreal parser or an in-game merchant acceptance test.
    private static void TestTraderBuyLimitationsPatchShape()
    {
        const string label = "Trader-BuyLimitations-BPatch-Shape";
        var docType = typeof(CfgPatchWriterService).GetNestedType("PatchDocument", BindingFlags.NonPublic)
            ?? throw new InvalidDataException(label + ": missing production PatchDocument");
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var replace = docType.GetMethod("AddReplacementNode", flags)
            ?? throw new InvalidDataException(label + ": missing production AddReplacementNode");
        var remove = docType.GetMethod("AddRemovalNode", flags)
            ?? throw new InvalidDataException(label + ": missing production AddRemovalNode");
        var assign = docType.GetMethod("AddAssignment", flags)
            ?? throw new InvalidDataException(label + ": missing production AddAssignment");
        var render = docType.GetMethod("Render", flags)
            ?? throw new InvalidDataException(label + ": missing production Render");
        const string source = "GameData/TradePrototypes.cfg";
        const string sid = "EgerTrader_TradePrototype";
        var parentPath = new[] { "TradeGenerators", "[0]" };
        var document = Activator.CreateInstance(docType, nonPublic: true)
            ?? throw new InvalidDataException(label + ": failed to construct production document");

        // Existing minimum-durability control and new buy-limit child MUST
        // coalesce into one TradeGenerators/[0] node, never overwrite siblings.
        assign.Invoke(document, new object[] { sid, parentPath, "WeaponSellMinDurability", "0.5f" });
        assign.Invoke(document, new object[] { sid, parentPath, "ArmorSellMinDurability", "0.5f" });
        var child = replace.Invoke(document, new object[] { sid, parentPath, "BuyLimitations" })
            ?? throw new InvalidDataException(label + ": no replace child");
        var set = child.GetType().GetMethod("SetAssignment", flags)
            ?? throw new InvalidDataException(label + ": missing production PatchNode.SetAssignment");
        var types = new[] { "Weapon", "Armor", "Ammo", "Grenade", "Attach", "Artifact" };
        for (var i = 0; i < types.Length; i++)
            set.Invoke(child, new object[] { $"[{i}]", "EItemType::" + types[i] });
        var rendered = (string)(render.Invoke(document, new object[] { source })
            ?? throw new InvalidDataException(label + ": serializer returned null"));
        var patch = Parse(rendered);
        var root = patch.Get(sid);
        var trade = root.Get("TradeGenerators");
        var zero = trade.Get("[0]");
        var limits = zero.Get("BuyLimitations");
        Check(root.Bpatch && trade.Bpatch && zero.Bpatch && !limits.Bpatch,
            label + ": {bpatch} ancestors and replaced child format drift");
        Check(zero.Assignments.Count == 2
            && zero.Value("WeaponSellMinDurability") == "0.5f"
            && zero.Value("ArmorSellMinDurability") == "0.5f",
            label + ": minimum condition fields lost or extra trade scalar added");
        Check(limits.Assignments.Count == types.Length && limits.Children.Count == 0,
            label + ": buy array has incorrect shape");
        for (var i = 0; i < types.Length; i++)
            Check(limits.Value($"[{i}]") == "EItemType::" + types[i],
                label + ": incorrect source-preserving array entry");

        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in patch.Children.Values)
            Collect(n, n.Name, fields);
        var wanted = new HashSet<string>(StringComparer.Ordinal) {
            sid + "/TradeGenerators/[0]/WeaponSellMinDurability",
            sid + "/TradeGenerators/[0]/ArmorSellMinDurability"
        };
        foreach (var i in Enumerable.Range(0, types.Length))
            wanted.Add(sid + $"/TradeGenerators/[0]/BuyLimitations/[{i}]");
        Check(fields.SetEquals(wanted), label + ": serializer touched unrelated trader fields");

        var empty = Activator.CreateInstance(docType, nonPublic: true)
            ?? throw new InvalidDataException(label + ": no removal document");
        remove.Invoke(empty, new object[] { "Bartender_Zalesie_TradePrototype",
            parentPath, "BuyLimitations" });
        var removal = (string)(render.Invoke(empty, new object[] { source })
            ?? throw new InvalidDataException(label + ": no removal patch"));
        Check(removal.Contains("BuyLimitations : removenode", StringComparison.Ordinal)
            && removal.Contains("TradeGenerators : struct.begin {bpatch}", StringComparison.Ordinal)
            && removal.Contains("[0] : struct.begin {bpatch}", StringComparison.Ordinal)
            && !removal.Contains("WeaponSellMinDurability", StringComparison.Ordinal)
            && !removal.Contains("ArmorSellMinDurability", StringComparison.Ordinal),
            label + ": removal touches unrelated fields or lacks BPatch ancestors");

        _passed++;
        Console.WriteLine("PASS " + label + " (production serializer: child replacement, list removal, sibling durability)");
    }

    private static string[][] TraderPolicyRows()
    {
        var csv = File.ReadAllLines(Path.Combine("src", "TrueCustomDifficulty", "Data",
            "trader-buy-policy-manifest.csv")).Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(',')).ToArray();
        Check(csv.Length == 86 && csv.All(row => row.Length == 6),
            "Trader Buys: invalid 86-profile purchase policy source");
        return csv;
    }

    private static List<string> ChangedTraderBuyTypes(string[] row,
        IReadOnlyDictionary<string, ParameterViewModel> modified)
    {
        var before = row[5].Length == 0 ? new List<string>()
            : row[5].Split(';').ToList();
        foreach (var role in TraderBuysPolicy.Roles.Where(role => role.Id == row[3]))
        {
            foreach (var category in TraderBuysPolicy.Categories)
            {
                var id = TraderBuysPolicy.ParameterId(role, category);
                if (!modified.TryGetValue(id, out var p)) continue;
                if (p.CurrentBoolean)
                    before.Remove(category.NativeType);
                else if (!before.Contains(category.NativeType, StringComparer.Ordinal))
                    before.Add(category.NativeType);
            }
        }
        return before;
    }

    private static void TestTraderBuysNeutral()
    {
        const string label = "Trader-Buys-NoChange";
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var ids = TraderBuysPolicy.Roles.SelectMany(role => TraderBuysPolicy.Categories
                .Select(category => TraderBuysPolicy.ParameterId(role, category))).ToArray();
            Check(ids.Length == 40 && ids.Distinct(StringComparer.Ordinal).Count() == 40,
                label + ": expected 5 x 8 unique buy cells");
            foreach (var id in ids)
            {
                var p = vm.AllParameters.Single(p => p.Id == id);
                Check(p.IsToggle && !p.IsValueModifiedFromReference() && p.IsControlEnabled,
                    label + ": unexpected non-neutral buying rule " + id);
            }
            var result = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, path);
            Check(result.FileCount == 0 && result.EditCount == 0 && result.ParameterCount == 0,
                label + ": neutral settings must not rewrite native Eger/Bolshak/other policies");
            _passed++;
            Console.WriteLine("PASS " + label + " (40 defaults, 86 unchanged profiles)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
    }

    private static void TestTraderBuysAllRoles()
    {
        const string label = "Trader-Buys-AllRoles";
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var selected = new[]
            {
                "economy.traderBuys.general.weapon",
                "economy.traderBuys.medic.consumable",
                "economy.traderBuys.technician.consumable",
                "economy.traderBuys.bartender.mutantLoot",
                "economy.traderBuys.genericNpc.weapon"
            };
            foreach (var id in selected)
            {
                var p = vm.AllParameters.Single(p => p.Id == id);
                p.CurrentBoolean = !p.ReferenceBoolean;
                Check(p.IsValueModifiedFromReference(), label + ": unchanged test cell " + id);
            }
            var modified = vm.AllParameters.Where(p => selected.Contains(p.Id, StringComparer.Ordinal))
                .ToDictionary(p => p.Id, StringComparer.Ordinal);
            var rows = TraderPolicyRows();
            var expected = new Dictionary<(string Scope, string Sid), List<string>>();
            foreach (var row in rows)
            {
                var before = row[5].Length == 0 ? Array.Empty<string>() : row[5].Split(';');
                var after = ChangedTraderBuyTypes(row, modified);
                if (!after.SequenceEqual(before, StringComparer.Ordinal))
                    expected.Add((row[0], row[2]), after);
            }
            Check(expected.Count > 50 && expected.Count < 86,
                label + ": atypical per-trader source cases not preserved");
            var result = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, path);
            Check(result.ParameterCount == 5 && result.EditCount == expected.Count,
                label + ": incorrect category-to-prototype writer fanout");
            var sourceFiles = rows.Where(r => expected.ContainsKey((r[0], r[2])))
                .Select(r => ExpectedFile(path, r[1])).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            Check(result.FileCount == sourceFiles.Length && sourceFiles.Length == 2,
                label + ": both base and DLC1 required");
            foreach (var file in sourceFiles)
            {
                var patch = Parse(File.ReadAllText(file));
                var candidates = rows.Where(r =>
                    string.Equals(ExpectedFile(path, r[1]), file, StringComparison.OrdinalIgnoreCase)).ToArray();
                var wanted = candidates.Where(r => expected.ContainsKey((r[0], r[2])))
                    .Select(r => r[2]).ToHashSet(StringComparer.Ordinal);
                Check(patch.Children.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(wanted),
                    label + ": unexpected or missing trade prototypes in BPatch");
                foreach (var row in candidates.Where(r => wanted.Contains(r[2])))
                {
                    var root = patch.Get(row[2]);
                    var trade = root.Get("TradeGenerators").Get("[0]");
                    Check(root.Bpatch && trade.Bpatch && trade.Assignments.Count == 0,
                        label + ": trade price/condition modified alongside buy whitelist");
                    var limits = trade.Get("BuyLimitations");
                    var types = expected[(row[0], row[2])];
                    if (types.Count == 0)
                        Check(limits.Removed && limits.Assignments.Count == 0,
                            label + ": empty list should be removed only");
                    else
                    {
                        Check(!limits.Bpatch && !limits.Removed
                            && limits.Assignments.Count == types.Count,
                            label + ": failure to replace a complete compact list");
                        for (var i = 0; i < types.Count; i++)
                            Check(limits.Value($"[{i}]") == "EItemType::" + types[i],
                                label + ": wrong native category or ordinal index");
                    }
                }
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (5 merchant roles, native exceptions, full BPatch source paths)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
    }

    private static void TestTraderBuysCombinedWithCondition()
    {
        const string label = "Trader-Buys-And-MinCondition";
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            vm.AllParameters.Single(p => p.Id == "economy.traderMinBuyCondition")
                .SnapCurrentNumberToMarker(0.55);
            vm.AllParameters.Single(p => p.Id == "economy.traderBuys.general.weapon")
                .CurrentBoolean = false;
            vm.AllParameters.Single(p => p.Id == "economy.traderBuys.bartender.mutantLoot")
                .CurrentBoolean = true;
            var result = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, path);
            Check(result.ParameterCount == 3 && result.FileCount == 2,
                label + ": combined writer leaked sibling fields / other files");

            var manifest = TraderPolicyRows();
            foreach (var row in manifest)
            {
                var patch = Parse(File.ReadAllText(ExpectedFile(path, row[1])));
                var root = patch.Get(row[2]);
                var limits = root.Get("TradeGenerators").Get("[0]");
                Check(root.Bpatch && limits.Bpatch && limits.Assignments.Count == 2,
                    label + ": missing sibling minimum-condition assignments");
                Approx(limits.Value("WeaponSellMinDurability"), 0.55, label + ": weapon");
                Approx(limits.Value("ArmorSellMinDurability"), 0.55, label + ": armor");
                if (row[2] == "EgerTrader_TradePrototype")
                    Check(!limits.Children.ContainsKey("BuyLimitations"),
                        label + ": Eger already rejects weapons, do not rewrite his source list");
                if (row[2] == "Bartender_Zalesie_TradePrototype")
                    Check(limits.Get("BuyLimitations").Removed,
                        label + ": native one-item blacklist must become a removenode");
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (86 merged profiles; DLC1, Eger, empty list)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
    }

    private static void TestTraderSellsNeutral()
    {
        const string label = "Trader-Sells-Neutral";
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var ids = TraderSellsPolicy.ParameterIds.ToArray();
            Check(ids.Length == 31 && ids.Distinct(StringComparer.Ordinal).Count() == 31,
                label + ": expected 31 real seller category cells");
            foreach (var id in ids)
            {
                var p = vm.AllParameters.Single(p => p.Id == id);
                Check(p.IsToggle && p.CurrentBoolean && !p.IsValueModifiedFromReference()
                    && p.IsControlEnabled, label + ": non-neutral seller " + id);
            }
            var result = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, path);
            Check(result.FileCount == 0 && result.EditCount == 0 && result.ParameterCount == 0,
                label + ": default should emit zero patches");
            _passed++;
            Console.WriteLine("PASS " + label + " (31 neutral seller settings)");
        }
        finally { if (_keep is null && Directory.Exists(path)) Directory.Delete(path, true); }
    }

    private static void TestTraderSellsFiveRoles()
    {
        const string label = "Trader-Sells-Five-Roles";
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var selected = new[]
            {
                "economy.traderSells.general.drinks",
                "economy.traderSells.medic.medical",
                "economy.traderSells.technician.attachments",
                "economy.traderSells.bartender.food",
                "economy.traderSells.genericNpc.grenades"
            };
            foreach (var id in selected)
            {
                var parameter = vm.AllParameters.Single(p => p.Id == id);
                parameter.CurrentBoolean = false;
                Check(parameter.IsValueModifiedFromReference(), label + ": unchanged " + id);
            }
            var result = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, path);
            Check(result.ParameterCount == 5 && result.EditCount > 40 && result.FileCount >= 3,
                label + ": source generator copies/redirects missing");
            var cfgs = Directory.GetFiles(path, "*.cfg", SearchOption.AllDirectories);
            var content = string.Join("\n", cfgs.Select(File.ReadAllText));
            Check(content.Contains("TCD_TS_", StringComparison.Ordinal)
                && content.Contains(": removenode", StringComparison.Ordinal)
                && content.Contains("ItemGeneratorPrototypeSID = TCD_TS_", StringComparison.Ordinal)
                && content.Contains("{refurl=../ItemGeneratorPrototypes.cfg;refkey=", StringComparison.Ordinal)
                && content.Contains("ItemGenerator : struct.begin {bpatch}", StringComparison.Ordinal)
                && content.Contains("PossibleItems : struct.begin {bpatch}", StringComparison.Ordinal)
                && content.Contains("TradeGenerators : struct.begin {bpatch}", StringComparison.Ordinal),
                label + ": wrong original-copy / BPatch / consumer redirect shape");
            foreach (var file in cfgs)
            {
                var patch = Parse(File.ReadAllText(file));
                Check(patch.Children.Keys.All(sid =>
                    !sid.EndsWith("_ItemGenerator", StringComparison.Ordinal)
                    || sid.StartsWith("TCD_TS_", StringComparison.Ordinal)),
                    label + ": a shared vanilla item generator was edited directly");
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (5 roles, private copies, nested deletion and trade redirects)");
        }
        finally { if (_keep is null && Directory.Exists(path)) Directory.Delete(path, true); }
    }

    private static void TestTraderSellsDlcOnly()
    {
        const string label = "Trader-Sells-DLC-Only";
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var parameter = vm.AllParameters.Single(p =>
                p.Id == "economy.traderSells.technician.weapons");
            parameter.CurrentBoolean = false;
            var inactive = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = false }, path);
            Check(inactive.FileCount == 0 && inactive.EditCount == 0
                && inactive.ParameterCount == 0,
                label + ": disabled DLC must be a saved-only preference, not a false CFG patch");
            var active = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, path);
            Check(active.EditCount > 0 && active.FileCount >= 2 && active.ParameterCount == 1,
                label + ": installed DLC weapon seller source did not route");
            var expectedDlc = ExpectedFile(path, "DLCGameData/DLC1/TradePrototypes.cfg");
            Check(File.Exists(expectedDlc)
                && File.ReadAllText(expectedDlc).Contains("ItemGeneratorPrototypeSID = TCD_TS_",
                    StringComparison.Ordinal), label + ": missing DLC merchant rewire");
            _passed++;
            Console.WriteLine("PASS " + label + " (DLC1-only stock ignored without DLC1)");
        }
        finally { if (_keep is null && Directory.Exists(path)) Directory.Delete(path, true); }
    }

    private static void TestTraderSellsCombinedWithBuys()
    {
        const string label = "Trader-Sells-Combined-TradeGenerator";
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Economy_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            vm.AllParameters.Single(p => p.Id == "economy.traderSells.general.weapons")
                .CurrentBoolean = false;
            vm.AllParameters.Single(p => p.Id == "economy.traderBuys.general.weapon")
                .CurrentBoolean = false;
            vm.AllParameters.Single(p => p.Id == "economy.traderMinBuyCondition")
                .SnapCurrentNumberToMarker(0.55);
            var result = new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, path);
            Check(result.ParameterCount == 3 && result.EditCount > 130,
                label + ": missing one of three independent merchant edits");
            var trade = Parse(File.ReadAllText(ExpectedFile(path, "GameData/TradePrototypes.cfg")));
            var zalesie = trade.Get("Trader_Zalesie_TradePrototype").Get("TradeGenerators").Get("[0]");
            Check(zalesie.Bpatch
                && zalesie.Value("ItemGeneratorPrototypeSID").StartsWith("TCD_TS_", StringComparison.Ordinal)
                && zalesie.Value("WeaponSellMinDurability") == "0.55f"
                && zalesie.Value("ArmorSellMinDurability") == "0.55f"
                && zalesie.Get("BuyLimitations").Value("[0]") == "EItemType::Weapon",
                label + ": stock redirect, buyer restriction and condition overwrote one another");
            _passed++;
            Console.WriteLine("PASS " + label + " (three independent fields on one trade profile)");
        }
        finally { if (_keep is null && Directory.Exists(path)) Directory.Delete(path, true); }
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
            var removal = Regex.Match(line, @"^(?<name>\S+)\s*:\s*removenode$");
            if (removal.Success)
            {
                var n = new Node(removal.Groups["name"].Value, "{removenode}");
                Check(nodes.Peek().Children.TryAdd(n.Name, n), "Duplicate removal CFG node " + n.Name);
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
        public bool Removed { get; } = flags.Contains("{removenode}", StringComparison.Ordinal);
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
