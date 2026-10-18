using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

// Phase 14B1: real production writer, cloned template bodies, original
// redirect CSV fanouts, and exact source-level control ownership.
// World actor-removal/shuffle writers have their own separate acceptance gate.
internal static partial class Program
{
    static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    static readonly string[] ScalarIds =
    {
        "loot.npcMedicalChance",
        "loot.cheapStashAmmoQuantity", "loot.cheapStashGrenadeChance",
        "loot.cheapStashGrenadeQuantity", "loot.mediumStashAmmoQuantity",
        "loot.expensiveStashAmmoQuantity", "loot.destructibleAmmoQuantity",
        "loot.destructibleGrenadeQuantity", "loot.ammoBoxWeight918",
        "loot.ammoBoxWeight045", "loot.ammoBoxWeight545",
        "loot.ammoBoxWeight012"
    };
    static readonly JsonElement Special = LoadSpecial();
    static int Passed;
    static string? Keep;

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=0)
            {
                if(args.Length!=2 || args[0]!="--out")throw new ArgumentException("Use [--out PATH]");
                Keep=Path.GetFullPath(args[1]);Directory.CreateDirectory(Keep);
            }
            Inventory();
            foreach(var d in new[]{"Novice","Stalker","Veteran","Master"})
                Run(d,false,new(),"vanilla");
            foreach(var id in ScalarIds)
            {
                var par=Get("Veteran",id);
                var lo=id=="loot.cheapStashGrenadeQuantity" ? 0.0 :
                    id=="loot.cheapStashGrenadeChance" || id.StartsWith("loot.ammoBoxWeight",StringComparison.Ordinal)
                    || id=="loot.npcMedicalChance" ? 0.0 : par.Minimum;
                var hi=par.Maximum;
                foreach(var dlc in new[]{false,true})
                {
                    Run("Veteran",dlc,new(){[id]=lo},"low-"+id);
                    Run("Veteran",dlc,new(){[id]=hi},"high-"+id);
                }
            }
            // Aggregate anchors and other original CFG domains must never
            // trigger unedited special clones or duplicate redirect sets.
            Run("Veteran",false,new() {
                ["loot.cheapStashAmmoQuantity"]=0.5,
                ["loot.cheapStashGrenadeChance"]=0.7,
                ["loot.cheapStashGrenadeQuantity"]=3.0
            },"cheap-triple");
            Run("Veteran",true,new() {
                ["loot.destructibleAmmoQuantity"]=1.5,
                ["loot.ammoBoxWeight918"]=0.5,
                ["loot.ammoBoxWeight545"]=2.0,
                ["loot.destructibleGrenadeQuantity"]=1.5
            },"breakable-mixed-dlc");
            Run("Veteran",false,new() {
                ["loot.mediumStashAmmoQuantity"]=0.5,
                ["loot.expensiveStashAmmoQuantity"]=2.0,
                ["loot.npcMedicalChance"]=0.5
            },"medium-rare-medic");
            Run("Veteran",true,new() {
                ["loot.cheapStashAmmoQuantity"]=1.5,
                ["loot.mediumStashAmmoQuantity"]=1.5,
                ["loot.expensiveStashAmmoQuantity"]=1.5
            },"all-stash-dlc");
            // The source-only visual anchor bug found in 14A must stay fixed
            // even when multiple special generator families share one file.
            Run("Veteran",false,new() {
                ["loot.ammoBoxWeight045"]=0.0,
                ["loot.ammoBoxWeight012"]=2.0,
                ["loot.destructibleAmmoQuantity"]=2.0
            },"ammo-distribution");
            // Corpse package can be switched; ammo counts only matter when the
            // configured smart loot package contains NPC_Ammo_Smart.
            Run("Novice",false,new(),"corpse-none","");  // original Novice includes ammo
            Run("Stalker",false,new(),"corpse-medicine","NPC_Medicine_Smart, NPC_Ammo_Smart");
            Run("Veteran",false,new(),"corpse-water","NPC_Water_Smart");
            Run("Novice",false,new(){["loot.npcWeaponAmmoSupply"]=0.5},"corpse-ammo-present");
            Run("Novice",true,new(){["loot.npcWeaponAmmoSupply"]=2.0},"corpse-ammo-present-dlc");
            Run("Veteran",false,new(){["loot.npcWeaponAmmoSupply"]=2.0},"corpse-ammo-absent");
            Check(Passed==63,"Expected 63 special Loot scenarios, got "+Passed);
            VerifyBaseTierRedirectProtection();
            VerifyFixedSleepRestoreBaseline();
            VerifyMedicalZeroExclusion();
            VerifyWorldStashSmartLoot();
            VerifyCabinDirectMedical();
            VerifyRegularStashDirectMedical();
            VerifyDlcStashExpansion();
            VerifyStashEffectiveParity();
            VerifyStashBranchBrowserParity();
            VerifyGenericLootPreGame();
            VerifyExtraStashGearWeaponRemoval();
            VerifyStashGearCoverage();
            Console.WriteLine($"PASS: {Passed} Loot SPECIAL generator and corpse writer scenarios plus 3 medical-zero production CFG scenarios");
            Console.WriteLine("NOT VERIFIED: original decoded CFG source values, world shuffle/removal writers, Unreal patch loading or gameplay");
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine("FAIL: "+ex);return 1;}
    }


    // The app deliberately starts Sleep Health Restore at 100 HP although the
    // original source has 300 HP. The UI editor is capped at 200 HP: do NOT
    // "neutralize" the test by trying to set an out-of-range 300 HP.
    // Every loot-only contract must expect this independent fixed-default CFG
    // override, and must reject any *other* non-loot writes.
    static void AssertOnlyExpectedNonLootDefault(MainViewModel vm)
    {
        var modified = vm.AllParameters
            .Where(p => p.IsControlEnabled && p.IsValueModifiedFromReference()
                && !p.Id.StartsWith("loot.", StringComparison.Ordinal))
            .Select(p => p.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        Check(modified.SequenceEqual(new[] { "survival.sleepHealthRestore" }),
            "Unexpected non-Loot modified controls: " + string.Join(",", modified));
        var sleep = vm.AllParameters.Single(p => p.Id == "survival.sleepHealthRestore");
        Check(sleep.IsSlider && sleep.CurrentNumber == 100
            && sleep.ReferenceNumber == 300 && sleep.Maximum == 200,
            "The intentional 100-HP fixed default / 300-HP source contract changed");
    }

    static void VerifyFixedSleepRestoreBaseline()
    {
        var dir = Path.Combine(Keep ?? Path.GetTempPath(),
            "TCD_LootSpecial_FixedDefault", "default-only");
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Novice" };
            AssertOnlyExpectedNonLootDefault(vm);
            Check(!vm.AllParameters.Any(p => p.Id.StartsWith("loot.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()),
                "A freshly initialized vanilla Loot tab has modified sources");

            var built = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
            var output = ReadOutput(dir);
            Check(output.Count == 1
                && output.TryGetValue("GameData/EffectPrototypes.cfg", out var fields)
                && fields!.Count == 2
                && fields.TryGetValue("SleepHealthRestore/ValueMin", out var min)
                && fields.TryGetValue("SleepHealthRestore/ValueMax", out var max)
                && min == "100" && max == "100",
                "Intentional fixed Sleep Health Restore should produce exactly two 100-HP CFG leaves");
            Check(built.FileCount == 1 && built.EditCount == 2
                && built.ParameterCount == 1,
                "The one modified non-Loot default generated unexpected output");
            Console.WriteLine("PASS baseline fixed-default: 100-HP SleepHealthRestore ValueMin/ValueMax only");
        }
        finally
        {
            if (Keep is null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    // Regression: zero medical bias must suppress actual stack counts as
    // well as Chance/Weight in all eligible production stash clone templates.
    // This is a generated-CFG check, never a claim about prefilled or quest
    // stashes, engine parsing or runtime drops.
    // A medical Weight modifier must never control native Chance, and
    // the independent Chance scale must never control native Weight.
    static void VerifyMedicalZeroExclusion()
    {
        var sids = new[] { "Bandage", "Medkit", "ArmyMedkit", "AntiRad" };
        var weightIds = new[] {
            "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
            "loot.stashMedicalArmyMedkitBias", "loot.stashMedicalAntiRadBias"
        };
        var chanceIds = new[] {
            "loot.stashMedicalBandageChanceScale",
            "loot.stashMedicalMedkitChanceScale",
            "loot.stashMedicalArmyMedkitChanceScale",
            "loot.stashMedicalAntiRadChanceScale"
        };
        for (var i = 0; i < sids.Length; i++)
        {
            var weight = new StashEffectiveState(
                new Dictionary<string,double> { [weightIds[i]] = 0 },
                new[] { weightIds[i] }, "Easy");
            Check(Num(weight.TuneSelection(sids[i], "Weight", "2")) == 0,
                "Medical weight zero failed: " + sids[i]);
            Check(Math.Abs(Num(weight.TuneSelection(sids[i], "Chance", "0.4")) - .4) < 1e-8,
                "Weight changed Chance: " + sids[i]);

            var chance = new StashEffectiveState(
                new Dictionary<string,double> { [chanceIds[i]] = .5 },
                new[] { chanceIds[i] }, "Easy");
            Check(Math.Abs(Num(chance.TuneSelection(sids[i], "Weight", "2")) - 2) < 1e-8,
                "Chance changed Weight: " + sids[i]);
            Check(Math.Abs(Num(chance.TuneSelection(sids[i], "Chance", "0.4")) - .2) < 1e-8,
                "Medical 50% Chance scale failed: " + sids[i]);
            var off = new StashEffectiveState(
                new Dictionary<string,double> { [chanceIds[i]] = 0 },
                new[] { chanceIds[i] }, "Easy");
            Check(Num(off.TuneSelection(sids[i], "Chance", "0.4")) == 0,
                "Medical 0% Chance scale failed: " + sids[i]);
        }
        var countOff = new StashEffectiveState(
            new Dictionary<string,double> { ["loot.stashMedkitMaxCount"] = -1 },
            new[] { "loot.stashMedkitMaxCount" }, "Easy");
        Check(countOff.MedicalDisabled("Medkit") &&
            Num(countOff.TuneSelection("Medkit", "Chance", "0.4")) == 0 &&
            Num(countOff.TuneSelection("Medkit", "Weight", "2")) == 0,
            "Explicit medical 0-items exclusion must suppress both fields");
        Console.WriteLine("PASS independent medical Weight / Chance + count exclusion");
    }

    static void VerifyWorldStashSmartLoot()
    {
        // This tests the exact generated CFG fields, not engine loot telemetry.
        // No novel quest/blueprint ItemGenerator should be overwritten.
        var cases = new[]
        {
            ("smart-medkit-zero", "loot.stashMedicalMedkitBias", 0.0,
                "TCD_StashMedicine_Smart", "ConsumablesParams", "0", "0", "Weight"),
            ("smart-bandage-zero", "loot.stashMedicalBandageBias", 0.0,
                "TCD_StashMedicine_Smart", "ConsumablesParams", "1", "0", "Weight"),
            ("smart-ammo-zero", "loot.stashMixAmmo", 0.0,
                "TCD_Stash_Ammo_Smart_CommonRare", "PrimaryWeaponParams", "0", "0", "Weight"),
            ("smart-food-zero", "loot.stashMixFood", 0.0,
                "TCD_StashFood_Cheap", "ConsumablesParams", "0", "0", "Weight"),
            ("smart-grenade-weight-zero", "loot.stashMixGrenades", 0.0,
                "TCD_StashGrenade_Cheap", "GrenadesParams", "0", "0", "Weight")
        };
        foreach (var (name, id, value, prototype, group, set, item, field) in cases)
        {
            var dir = Path.Combine(Keep ?? Path.GetTempPath(), "TCD_SmartLoot_Source", name);
            try
            {
                var vm = new MainViewModel(Catalog)
                    { SelectedReferenceDifficulty = "Stalker" };
                var p = vm.AllParameters.Single(x => x.Id == id);
                p.SnapCurrentNumberToMarker(value);
                Check(p.IsValueModifiedFromReference(), name + ": source did not change");
                AssertOnlyExpectedNonLootDefault(vm);
                var result = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
                Check(result.ParameterCount == 2,
                    name + ": unexpected modified parameter count");
                var outp = ReadOutput(dir);
                Check(outp.TryGetValue("GameData/StashPrototypes.cfg", out var smart),
                    name + ": missing SmartLoot prototype patch");
                Check(outp.TryGetValue("GameData/SpawnActorPrototypes.cfg", out var actors),
                    name + ": missing filtered production actor redirects");
                var prefix = prototype + "/ItemGenerators/[0]/SmartLootParams/"
                    + group + "/[" + set + "]/Items/[" + item + "]/";
                foreach (var key in new[] { field, "MinCount", "MaxCount" })
                {
                    Check(smart!.TryGetValue(prefix + key, out var n),
                        name + ": absent SmartLoot source " + prefix + key);
                    Check(Math.Abs(Num(n!)) < 0.0000001,
                        name + ": source still positive " + prefix + key + "=" + n);
                }
                // Includes full ItemGenerator redirects in the same document,
                // not just StashPrototypeSID leaves. The 106-actor exclusion
                // must protect ALL fields of blueprints and named-clue stashes.
                AssertProtectedBaseStashesUntouched(outp, name);
                Check(actors!.Keys.Any(x => x.EndsWith("/StashPrototypeSID",
                    StringComparison.Ordinal) && x.Length >= 33),
                    name + ": no normal world actors were redirected");
                Console.WriteLine("PASS " + name
                    + ": source-zero and filtered actor redirect");
            }
            finally
            {
                if (Keep is null && Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }
    }


    // Protect the full actor, not just its StashPrototypeSID. A blueprint bag
    // can have a normal Rare generator next to its special blueprint reward.
    // The base-game tier manifests contain 30 such redirects; they must never
    // enter BPatch output for an ordinary Stashes slider.
    // Phase 3: every field of all three tier clones is predicted from the
    // same StashEffectiveState.TierField implementation the writer calls.
    // This checks ACTUAL generated CFG fields, not a source-only example.
    static void VerifyStashEffectiveParity()
    {
        var samples = new[]
        {
            (Name: "easy-bonus-composition", Difficulty: "Novice", Dlc: false,
                Changes: new Dictionary<string,double>
                {
                    ["loot.stashMixMedical"] = .5,
                    ["loot.stashMedicalBandageBias"] = 2,
                    ["loot.cheapStashGrenadeChance"] = .65,
                    ["loot.stashMixGrenades"] = 1.5,
                    ["loot.expensiveStashMoneyChance"] = .35,
                    ["loot.stashMixCoupons"] = 1.75,
                    ["loot.stashMedkitMaxCount"] = -1
                }),
            (Name: "hard-bonus-zero", Difficulty: "Veteran", Dlc: true,
                Changes: new Dictionary<string,double>
                {
                    ["loot.stashMixMedical"] = 0,
                    ["loot.stashMixFood"] = 3,
                    ["loot.stashMixAmmo"] = .5,
                    ["loot.cheapStashGrenadeChance"] = .4,
                    ["loot.expensiveStashMoneyChance"] = .8
                })
        };
        foreach (var scenario in samples)
        {
            var dir = Path.Combine(Keep ?? Path.GetTempPath(),
                "TCD_Stash_Parity", scenario.Name);
            try
            {
                var vm = new MainViewModel(Catalog)
                    { SelectedReferenceDifficulty = scenario.Difficulty };
                foreach (var (id, target) in scenario.Changes)
                {
                    var source = vm.AllParameters.Single(p => p.Id == id);
                    source.SnapCurrentNumberToMarker(target);
                    Check(source.IsValueModifiedFromReference(),
                        scenario.Name + ": unchanged source " + id);
                }
                var modified = vm.AllParameters
                    .Where(p => p.IsValueModifiedFromReference())
                    .Select(p => p.Id).ToArray();
                var difficultySid = Catalog.DifficultyProfiles[
                    scenario.Difficulty].Sid;
                var snapshot = new StashEffectiveState(
                    vm.AllParameters, modified, difficultySid);
                var result = new CfgPatchWriterService().Write(
                    vm, Catalog, scenario.Dlc
                        ? new GameInstallInfo { HasDlc1 = true } : null, dir);
                Check(result.ParameterCount > 0, "parity writer is empty");
                var outputs = ReadOutput(dir);
                // Use homogeneous template category type, not a tuple of
                // heterogeneous tier-specific baseline config classes.
                var sources = new[]
                {
                    (Tier: "Cheap", File: Baseline.Special.CheapStashProduction.File,
                        Sid: Baseline.Special.CheapStashProduction.CustomGeneratorSid,
                        Categories: Baseline.Special.CheapStashProduction.TemplateCategories),
                    (Tier: "Medium", File: Baseline.Special.MediumStashAmmoProduction.File,
                        Sid: Baseline.Special.MediumStashAmmoProduction.CustomGeneratorSid,
                        Categories: Baseline.Special.MediumStashAmmoProduction.TemplateCategories),
                    (Tier: "Expensive", File: Baseline.Special.ExpensiveStashProduction.File,
                        Sid: Baseline.Special.ExpensiveStashProduction.CustomGeneratorSid,
                        Categories: Baseline.Special.ExpensiveStashProduction.TemplateCategories)
                };
                var compared = 0;
                foreach (var tier in sources)
                {
                    Check(outputs.TryGetValue(tier.File, out var file),
                        scenario.Name + ": missing clone file " + tier.File);
                    foreach (var category in tier.Categories)
                    foreach (var item in category.Items)
                    foreach (var field in item.Fields)
                    {
                        var expected = snapshot.TierField(
                            Baseline, tier.Tier, category, item, field.Key);
                        var key = tier.Sid + "/ItemGenerator/" + category.Node
                            + "/PossibleItems/" + item.Node + "/" + field.Key;
                        Check(file!.TryGetValue(key, out var actual),
                            scenario.Name + ": missing emitted field " + key);
                        Check(string.Equals(actual, expected,
                            StringComparison.OrdinalIgnoreCase),
                            scenario.Name + ": preview/writer mismatch " + key
                            + " preview=" + expected + ", writer=" + actual);
                        compared++;
                    }
                }
                // SmartLoot is independently gated, and the source group
                // here is an original Weight entry with an affected Medical
                // multiplier. The shared SmartSelection result must equal
                // the actual emitted TCD_ reference clone leaf.
                Check(outputs.TryGetValue("GameData/StashPrototypes.cfg", out var smart),
                    scenario.Name + ": missing SmartLoot clone");
                const string smartKey = "TCD_StashMedicine_Smart/ItemGenerators/[0]"
                    + "/SmartLootParams/ConsumablesParams/[0]/Items/[0]/Weight";
                var predicted = snapshot.SmartSelection("Medkit", "Weight", "1");
                Check(smart!.TryGetValue(smartKey, out var emitted)
                    && string.Equals(predicted, emitted, StringComparison.OrdinalIgnoreCase),
                    scenario.Name + ": SmartLoot shared calculation mismatch");
                AssertProtectedBaseStashesUntouched(outputs, scenario.Name);
                Console.WriteLine("PASS " + scenario.Name
                    + ": " + compared + " ItemGenerator fields + SmartLoot Weight"
                    + " match actual generated CFG");
            }
            finally
            {
                if (Keep is null && Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }
    }

    static readonly Lazy<HashSet<string>> BaseProtectedActorIds = new(() =>
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var file = Path.Combine(dir.FullName, "src", "TrueCustomDifficulty",
                "Data", "base-stash-protected-actor-manifest.csv");
            if (File.Exists(file))
            {
                var lines = File.ReadAllLines(file).Skip(1)
                    .Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                var ids = lines.Select(line => line.Split(',')[0])
                    .ToHashSet(StringComparer.Ordinal);
                Check(lines.Length == 106 && ids.Count == 106
                    && ids.Contains("098E76F14ADD5CFF9C62508DB31013E3")
                    && ids.Contains("02E9BD8341141FEC43FBBB9B6E8F8891"),
                    "Protected base-world actor manifest drift");
                return ids;
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Missing base stash actor protection manifest");
    });

    // A shared tier wrapper is protected if even ONE physical consumer is.
    // Keep this source-derived test independent from the private writer guard.
    static bool IncludesProtectedPhysicalStash(string actorSids)
    {
        var ids = actorSids.Split('|', StringSplitOptions.TrimEntries);
        Check(ids.Length > 0 && ids.All(id => id.Length == 32
            && id.All(Uri.IsHexDigit))
            && ids.Distinct(StringComparer.Ordinal).Count() == ids.Length,
            "Malformed physical actor SID roster in stash tier manifest");
        return ids.Any(BaseProtectedActorIds.Value.Contains);
    }

    static void AssertProtectedBaseStashesUntouched(
        IReadOnlyDictionary<string, Dictionary<string, string>> generated,
        string label)
    {
        if (generated.TryGetValue("GameData/SpawnActorPrototypes.cfg", out var actors))
        {
            var touched = actors.Keys.Where(key =>
                BaseProtectedActorIds.Value.Contains(key.Split('/')[0])).ToArray();
            Check(touched.Length == 0,
                label + ": source-protected world stash actor redirect: "
                + string.Join(", ", touched.Take(8)));
        }

        // A protected physical stash can ALSO be redirected indirectly through
        // a named ItemGeneratorPrototypes wrapper. Check the precise real CFG
        // leaf, not only its actor SID in SpawnActorPrototypes.cfg.
        foreach (var name in new[] {
            "cheapStashProduction", "mediumStashAmmoProduction",
            "expensiveStashProduction" })
        {
            foreach (var row in Redirects(Special.GetProperty(name), includeDlc: true))
            {
                if (!IncludesProtectedPhysicalStash(row.PhysicalContainerSid)
                    || !generated.TryGetValue(row.File, out var emitted))
                    continue;
                var leaf = row.Prototype + "/" + row.NodePath.Replace(".", "/")
                    + "/" + row.Key;
                Check(!emitted.ContainsKey(leaf),
                    label + ": protected " + name
                    + " actor indirectly redirected through " + row.File + "|" + leaf);
            }
        }
    }

    static void VerifyBaseTierRedirectProtection()
    {
        var tiers = new[]
        {
            (Name: "cheapStashProduction", Direct: 2, Indirect: 10),
            (Name: "mediumStashAmmoProduction", Direct: 17, Indirect: 26),
            (Name: "expensiveStashProduction", Direct: 11, Indirect: 40)
        };
        foreach (var (name, direct, indirect) in tiers)
        {
            var manifest = Special.GetProperty(name);
            var raw = Redirects(manifest, includeDlc: true).ToArray();
            var safe = Redirects(manifest, includeDlc: true,
                excludeProtectedActors: true).ToArray();
            var guarded = raw.Where(row =>
                IncludesProtectedPhysicalStash(row.PhysicalContainerSid))
                .ToArray();
            Check(guarded.Count(row => row.File == "GameData/SpawnActorPrototypes.cfg")
                    == direct && guarded.Length == direct + indirect,
                name + ": direct/indirect protected source redirect counts changed");
            Check(raw.Length >= safe.Length + guarded.Length,
                name + ": physical actor protection was not applied");
            Check(safe.All(row =>
                !IncludesProtectedPhysicalStash(row.PhysicalContainerSid)
                && (row.File != "GameData/SpawnActorPrototypes.cfg"
                    || !BaseProtectedActorIds.Value.Contains(row.Prototype))),
                name + ": protected physical actor passed tier redirect gate");
        }
        Console.WriteLine("PASS base-world tier guard: 106 protected actors, 106 suppressed redirects (30 direct / 76 indirect)");
    }

    static void VerifyCabinDirectMedical()
    {
        // These two no-clue ordinary Cabin containers have a guaranteed
        // Medkit on ItemGenerator.[1], IN ADDITION to their regular Cheap
        // subgenerator and the separate StashMedicine_Smart overlay.
        // Regression uses the same real CFG writer and exact source root
        // spellings to prevent bypass of Medical = 0x.
        var actors = new[]
        {
            "LesserZone_Cabin_StashGenerator1_1C2FD1C141D6DA11E518758CC1CD36AE",
            "LesserZone_Cabin_StashGenerator3_59FDC1CD4D0D094270C695A18D84373A"
        };
        var cases = new[]
        {
            ("medkit-max-zero", "loot.stashMedkitMaxCount", -1.0, 0.0, 0, 0)
        };
        foreach (var (name, controlId, target, chance, min, max) in cases)
        {
            var dir = Path.Combine(Keep ?? Path.GetTempPath(), "TCD_Cabin_Stash", name);
            try
            {
                var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Stalker" };
                var control = vm.AllParameters.Single(p => p.Id == controlId);
                control.SnapCurrentNumberToMarker(target);
                Check(control.IsValueModifiedFromReference(), name + ": control is unchanged");
                AssertOnlyExpectedNonLootDefault(vm);
                var result = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
                Check(result.ParameterCount == 2,
                    name + ": missing fixed Sleep Health default or medical control");
                var output = ReadOutput(dir);
                Check(output.TryGetValue("GameData/ItemGeneratorPrototypes.cfg", out var gen),
                    name + ": missing Cabin ItemGenerator patch");
                foreach (var actor in actors)
                {
                    var prefix = actor + "/ItemGenerator/[1]/PossibleItems/[0]/";
                    Check(gen!.TryGetValue(prefix + "Chance", out var raw)
                        && Math.Abs(Num(raw!) - chance) < 0.0000001,
                        name + ": missed guaranteed Cabin Medkit " + actor);
                    if (chance == 0)
                    {
                        Check(gen.TryGetValue(prefix + "MinCount", out var rawMin)
                            && rawMin == min.ToString(CultureInfo.InvariantCulture),
                            name + ": Cabin Medkit MinCount not zero");
                        Check(gen.TryGetValue(prefix + "MaxCount", out var rawMax)
                            && rawMax == max.ToString(CultureInfo.InvariantCulture),
                            name + ": Cabin Medkit MaxCount not zero");
                    }
                    else
                    {
                        Check(!gen.Keys.Any(path => path.StartsWith(prefix + "MinCount")
                            || path.StartsWith(prefix + "MaxCount")),
                            name + ": positive chance modified fixed count on " + actor);
                    }
                }
                AssertProtectedBaseStashesUntouched(output, "Cabin " + name);
                Console.WriteLine("PASS Cabin source-native direct Medkit: " + name);
            }
            finally
            {
                if (Keep == null && Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        // A food-only edit must not touch the explicit medical reward rows.
        var foodDir = Path.Combine(Keep ?? Path.GetTempPath(),
            "TCD_Cabin_Stash", "food-only");
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Stalker" };
            vm.AllParameters.Single(p => p.Id == "loot.stashMixFood")
                .SnapCurrentNumberToMarker(0.0);
            new CfgPatchWriterService().Write(vm, Catalog, null, foodDir);
            var output = ReadOutput(foodDir);
            if (output.TryGetValue("GameData/ItemGeneratorPrototypes.cfg", out var gen))
                foreach (var actor in actors)
                    Check(!gen.Keys.Any(k =>
                        k.StartsWith(actor + "/ItemGenerator/[1]/PossibleItems/[0]/",
                            StringComparison.Ordinal)),
                        "Food slider rewrote an unrelated Cabin direct Medkit");
        }
        finally
        {
            if (Keep == null && Directory.Exists(foodDir))
                Directory.Delete(foodDir, true);
        }
    }


    // These 10 100%-chance Medkit/ArmyMedkit/EcoMedkit/Bandage entries
    // coexist with, but are independent of, ordinary generic stash subpools.
    // Existing medical controls must modify their exact physical source item
    // fields without touching authored weapon/gear or stimulant sibling items.
    static void VerifyRegularStashDirectMedical()
    {
        var original = LoadDirectStashAuditCsv();
        var medical = original.Where(r =>
            r["ItemClass"] == "MEDICAL"
            && r["CoverageStatus"] == "ADDITIONAL_GENERIC_SOURCE_UNTUNED").ToArray();
        var gear = original.Where(r => r["ItemClass"] == "OTHER_AUTHORED").ToArray();
        var boosters = original.Where(r => r["ItemClass"] == "BOOSTER").ToArray();
        var unresolved = original.Where(r => r["ItemClass"] == "MEDICAL_SID_CASE_REVIEW").ToArray();
        Check(medical.Length == 10
            && medical.Select(r => r["ActorSID"]).Distinct().Count() == 6
            && gear.Length == 45
            && gear.Select(r => r["ItemPrototypeSID"]).Distinct().Count() == 31
            && boosters.Length == 9 && unresolved.Length == 1
            && unresolved[0]["ItemPrototypeSID"] == "medkit",
            "Additional ordinary stash content source class drift");

        var tests = new[]
        {
            (Name: "medkit-max-0", Source: "loot.stashMedkitMaxCount", Value: -1.0,
                Relevant: "Medkit", Chance: 0.0, Disabled: true),
            (Name: "army-max-0", Source: "loot.stashArmyMedkitMaxCount", Value: -1.0,
                Relevant: "ArmyMedkit", Chance: 0.0, Disabled: true)
        };
        foreach (var scenario in tests)
        {
            var dir = Path.Combine(Keep ?? Path.GetTempPath(),
                "TCD_RegularStashDirectMedical", scenario.Name);
            try
            {
                var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Stalker" };
                var value = vm.AllParameters.Single(p => p.Id == scenario.Source);
                value.SnapCurrentNumberToMarker(scenario.Value);
                Check(value.IsValueModifiedFromReference(), scenario.Name + ": unchanged control");
                AssertOnlyExpectedNonLootDefault(vm);
                var result = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
                Check(result.ParameterCount == 2, scenario.Name + ": wrong modified sources");
                var output = ReadOutput(dir);
                Check(output.TryGetValue("GameData/ItemGeneratorPrototypes.cfg", out var doc),
                    scenario.Name + ": missing direct source CFG");
                foreach (var entry in medical)
                {
                    var sid = entry["ItemPrototypeSID"];
                    var prefix = entry["GeneratorRoot"] + "/ItemGenerator/["
                        + entry["CategoryIndex"] + "]/PossibleItems/["
                        + entry["ItemIndex"] + "]/";
                    var affected = scenario.Relevant == "all" || scenario.Relevant == sid;
                    if (affected)
                    {
                        Check(doc!.TryGetValue(prefix + "Chance", out var chance)
                            && Math.Abs(Num(chance!) - scenario.Chance) < 0.0000001,
                            scenario.Name + ": medical direct chance not tuned: " + prefix);
                        foreach (var field in new[] { "MinCount", "MaxCount" })
                            if (scenario.Disabled)
                                Check(doc!.TryGetValue(prefix + field, out var count)
                                    && count == "0",
                                    scenario.Name + ": medical direct count not zero: " + prefix + field);
                            else
                                Check(!doc!.ContainsKey(prefix + field),
                                    scenario.Name + ": unchanged count was rewritten: " + prefix + field);
                    }
                    else
                        Check(!doc!.Keys.Any(key => key.StartsWith(prefix, StringComparison.Ordinal)),
                            scenario.Name + ": unrelated medical item was rewritten: " + prefix);
                }

                // Every authored item, including all 45 reserved weapon/gear
                // rows and all nine booster rows, MUST remain untouched.
                foreach (var row in gear.Concat(boosters).Concat(unresolved))
                {
                    var prefix = row["GeneratorRoot"] + "/ItemGenerator/["
                        + row["CategoryIndex"] + "]/PossibleItems/["
                        + row["ItemIndex"] + "]/";
                    Check(!doc!.Keys.Any(key => key.StartsWith(prefix, StringComparison.Ordinal)),
                        scenario.Name + ": reserved or unresolved item was rewritten: " + prefix);
                }
                AssertProtectedBaseStashesUntouched(output, scenario.Name);
                Console.WriteLine("PASS direct medical source scope: " + scenario.Name);
            }
            finally
            {
                if (Keep == null && Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }
    }

    static IReadOnlyList<Dictionary<string, string>> LoadDirectStashAuditCsv()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var path = Path.Combine(dir.FullName, "docs", "generators",
                "regular-stash-direct-additions-2026-10-16.csv");
            if (File.Exists(path))
            {
                var rows = File.ReadAllLines(path);
                var header = rows[0].Split(',');
                var result = rows.Skip(1).Where(s => s.Length > 0)
                    .Select(line =>
                    {
                        var values = line.Split(',');
                        Check(values.Length == header.Length,
                            "Additional source CSV changed shape");
                        return header.Zip(values).ToDictionary(pair => pair.First,
                            pair => pair.Second, StringComparer.Ordinal);
                    }).ToArray();
                Check(result.Length == 80, "Additional source CSV must have 80 rows");
                return result;
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Missing direct stash item source audit CSV");
    }

    // The machine roster is the sole DLC1 approval authority for generator
    // rewrites. Read it from the current source tree, not generated artifacts.
    static readonly Lazy<HashSet<string>> DlcEligibleActorIds = new(() =>
    {
        var dir=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(dir!=null)
        {
            var file=Path.Combine(dir.FullName,"src","TrueCustomDifficulty",
                "Data","dlc1-stash-eligible-actor-manifest.csv");
            if(File.Exists(file))
            {
                var entries=File.ReadAllLines(file).Skip(1).Where(x=>x.Length>0)
                    .Select(x=>x.Substring(0,32)).ToHashSet(StringComparer.Ordinal);
                Check(entries.Count==112,"DLC1 eligible actor SID list count drift");
                return entries;
            }
            dir=dir.Parent;
        }
        throw new FileNotFoundException("Missing DLC1 stash allowlist");
    });

    static void VerifyDlcStashExpansion()
    {
        foreach(var (label, values, expectFresh) in new[]
        {
            ("bandage-zero",new Dictionary<string,double> { ["loot.stashMedicalBandageChanceScale"]=0 },true),
            ("medkit-zero",new Dictionary<string,double> { ["loot.stashMedkitMaxCount"]=-1 },true),
            ("cheap-ammo-half",new Dictionary<string,double> { ["loot.cheapStashAmmoQuantity"]=0.5 },true),
            ("medium-ammo-half",new Dictionary<string,double> { ["loot.mediumStashAmmoQuantity"]=0.5 },false)
        })
        {
            var dir=Path.Combine(Keep??Path.GetTempPath(),"TCD_DLC1_Stashes",label);
            try
            {
                var vm=new MainViewModel(Catalog) {SelectedReferenceDifficulty="Stalker"};
                foreach(var (key,value) in values)
                {
                    var control=vm.AllParameters.Single(x=>x.Id==key);
                    control.SnapCurrentNumberToMarker(value);
                    Check(control.IsValueModifiedFromReference(),
                        label+": control did not modify "+key);
                }
                AssertOnlyExpectedNonLootDefault(vm);
                var result=new CfgPatchWriterService().Write(vm,Catalog,
                    new GameInstallInfo {HasDlc1=true},dir);
                var files=ReadOutput(dir);
                Check(result.ParameterCount==2,label+": modified parameter count drift");
                Check(files.TryGetValue("DLCGameData/DLC1/SpawnActorPrototypes.cfg",out var dlc),
                    label+": missing DLC1 actor patch");
                var actorSids=dlc!.Keys.Select(x=>x.Split('/')[0]).ToHashSet();
                Check(actorSids.Count>0 && actorSids.IsSubsetOf(DlcEligibleActorIds.Value),
                    label+": touched protected DLC1 actor");
                if(label=="medium-ammo-half")
                {
                    Check(actorSids.Count==59,
                        label+": expected 59 ordinary medium actor redirects, got "+actorSids.Count);
                    Check(!files.GetValueOrDefault("GameData/SpawnActorPrototypes.cfg",
                        new Dictionary<string,string>()).Keys.Any(x=>
                            x.StartsWith("04D6AF3C427C5AAA381355A460EEFFF9/")),
                        label+": medium DLC root incorrectly patched in base GameData");
                }
                if(expectFresh)
                {
                    Check(files.TryGetValue("GameData/ItemGeneratorPrototypes.cfg",out var gens)
                        && gens!.ContainsKey("TCD_GamePass_Stash_ItemGenerator_CheapNoFresh/SID"),
                        label+": missing cheap-no-fresh clone");
                    foreach(var id in new[]{
                        "19EB12D645F5460C36D417AA5A224259",
                        "F943F3C84EB1391403756181AF2AB692" })
                        Check(dlc.ContainsKey(id+"/ItemGeneratorSettings/[0]/PrototypeSID")
                            && dlc[id+"/ItemGeneratorSettings/[0]/PrototypeSID"]==
                                "TCD_GamePass_Stash_ItemGenerator_CheapNoFresh",
                            label+": missing ordinary CheapNoFresh redirect "+id);
                    var root="TCD_GamePass_Stash_ItemGenerator_CheapNoFresh/ItemGenerator/";
                    if(label=="bandage-zero")
                    {
                        Check(gens![root+"[0]/PossibleItems/[0]/Chance"]=="0",
                            label+": Bandage Chance 0% did not reach generated CFG");
                        // Separate Weight/Chance modes must never overwrite
                        // a native stack count for a chance-only edit.
                    }
                    if(label=="medkit-zero")
                        foreach(var field in new[]{"Chance","MinCount","MaxCount"})
                            Check(gens![root+"[0]/PossibleItems/[2]/"+field]=="0",
                                label+": Medkit MAX 0 did not hard-disable "+field);
                    if(label=="cheap-ammo-half")
                        Check(gens![root+"[3]/PossibleItems/[0]/MinCount"]=="1",
                            label+": CheapNoFresh ammo 2 -> 1 source scaling failed");
                }
                if(label=="bandage-zero" || label=="medkit-zero")
                {
                    Check(files.ContainsKey("GameData/StashPrototypes.cfg"),
                        label+": base SmartLoot source clone missing");
                    Check(dlc.Keys.Any(x=>x.EndsWith("/StashPrototypeSID",
                        StringComparison.Ordinal)),
                        label+": DLC1 SmartLoot redirect missing");
                }
            }
            finally
            {
                if(Keep==null && Directory.Exists(dir))
                    Directory.Delete(dir,true);
            }
        }

        // Same medical edit without DLC must not emit a DLC actor target or
        // a CheapNoFresh clone, even though base SmartLoot remains active.
        var baseDir=Path.Combine(Keep??Path.GetTempPath(),"TCD_DLC1_Stashes","base-only");
        try
        {
            var vm=new MainViewModel(Catalog) {SelectedReferenceDifficulty="Stalker"};
            vm.AllParameters.Single(x=>x.Id=="loot.stashMixMedical")
                .SnapCurrentNumberToMarker(0);
            AssertOnlyExpectedNonLootDefault(vm);
            new CfgPatchWriterService().Write(vm,Catalog,null,baseDir);
            var onlyBase=ReadOutput(baseDir);
            Check(!onlyBase.ContainsKey("DLCGameData/DLC1/SpawnActorPrototypes.cfg"),
                "base-only wrote DLC1 actor file");
            Check(!onlyBase.GetValueOrDefault("GameData/ItemGeneratorPrototypes.cfg",
                new Dictionary<string,string>()).ContainsKey(
                    "TCD_GamePass_Stash_ItemGenerator_CheapNoFresh/SID"),
                "base-only generated DLC1 CheapNoFresh pool");
        }
        finally
        {
            if(Keep==null && Directory.Exists(baseDir))
                Directory.Delete(baseDir,true);
        }
        Console.WriteLine("PASS DLC1 stash medical native Chance, quantity exclusion and safe actor scope");
    }

    static JsonElement LoadSpecial()
    {
        var dir=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(dir!=null)
        {
            var p=Path.Combine(dir.FullName,"src","TrueCustomDifficulty","Data","cfg-writer-baseline.json");
            if(File.Exists(p))return JsonDocument.Parse(File.ReadAllText(p)).RootElement.GetProperty("special");
            dir=dir.Parent;
        }
        throw new FileNotFoundException("Original writer manifest file is required by standalone regression tests");
    }

    static ParameterViewModel Get(string d,string id)=>new MainViewModel(Catalog)
    {SelectedReferenceDifficulty=d}.AllParameters.Single(x=>x.Id==id);

    static void Inventory()
    {
        var defs=Catalog.Parameters.Where(p=>p.Id.StartsWith("loot.",StringComparison.Ordinal)).ToArray();
        var direct=Baseline.EditPoints.Where(p=>p.ParameterId.StartsWith("loot.",StringComparison.Ordinal))
            .Select(p=>p.ParameterId).ToHashSet(StringComparer.Ordinal);
        var specials=defs.Where(x=>!direct.Contains(x.Id)).Select(x=>x.Id).ToHashSet(StringComparer.Ordinal);
        var ours=ScalarIds.Concat(new[]{"loot.npcCorpseBonus","loot.npcWeaponAmmoSupply"})
            .ToHashSet(StringComparer.Ordinal);
        Check(defs.Length==61 && direct.Count==16 && specials.Count==45
            && ours.IsSubsetOf(specials),"Loot taxonomy changed: 16 direct, 45 special, 14 legacy generator inputs; 8 new source-native medical editors");
        var mounted=new MainViewModel(Catalog).AllParameters;
        Check(ours.All(id=>mounted.Any(x=>x.Id==id && x.IsControlEnabled)),
            "A generator source is not in the actual ViewModel");
        var c=Special.GetProperty("cheapStashProduction");
        var m=Special.GetProperty("mediumStashAmmoProduction");
        var e=Special.GetProperty("expensiveStashProduction");
        var med=Special.GetProperty("npcMedicalLootProduction");
        var a=Special.GetProperty("destructibleAmmoProduction");
        var g=Special.GetProperty("destructibleGrenadeProduction");
        Check(c.GetProperty("templateCategories").GetArrayLength()==12 &&
            c.GetProperty("ammoEntries").GetArrayLength()==3 &&
            m.GetProperty("ammoEntries").GetArrayLength()==25 &&
            e.GetProperty("ammoEntries").GetArrayLength()==25 &&
            e.GetProperty("couponEntries").GetArrayLength()==8 &&
            med.GetProperty("pools").GetArrayLength()==6 &&
            a.GetProperty("pools").GetArrayLength()==2 &&
            g.GetProperty("pools").GetArrayLength()==2 &&
            Special.GetProperty("npcCorpseAmmoProduction").GetProperty("entries").GetArrayLength()==43,
            "Audited Loot production template source inventories drift");
        foreach(var (config,count) in new[]{(c,0),(m,450),(e,305),(med,33),(a,1235),(g,182)})
        {
            var rows=Redirects(config,includeDlc:true).ToArray();
            if(count!=0)Check(rows.Length==count,"Embedded production redirect manifest drift "+count+" / "+rows.Length);
        }
    }

    static void Run(string difficulty,bool dlc,Dictionary<string,double> changes,
                    string label,string? corpseChoice=null)
    {
        var dir=Path.Combine(Keep??Path.GetTempPath(),"TCD_LootSpecial_Contract",Passed+"-"+label+(dlc?"-DLC1":""));
        try
        {
            var vm=new MainViewModel(Catalog){SelectedReferenceDifficulty=difficulty};
            foreach(var (id,target) in changes)
            {
                var par=vm.AllParameters.Single(x=>x.Id==id);
                Check(par.IsSlider && par.IsControlEnabled && !par.IsValueModifiedFromReference(),
                      label+": not a live vanilla source "+id);
                par.SnapCurrentNumberToMarker(target);
                Check(Math.Abs(par.CurrentNumber-target)<0.0000001 &&
                      par.IsValueModifiedFromReference(),label+": source did not change "+id);
            }
            if(corpseChoice!=null)
            {
                var corpse=vm.AllParameters.Single(x=>x.Id=="loot.npcCorpseBonus");
                Check(corpse.IsChoiceSlider && corpse.ChoiceOptions.Any(x=>x.Value==corpseChoice),
                    "Invalid corpse smart-loot option "+corpseChoice);
                corpse.CurrentText=corpseChoice;
                Check(corpse.IsValueModifiedFromReference(),"Corpse switch failed");
            }

            AssertOnlyExpectedNonLootDefault(vm);
            var changed=vm.AllParameters.Where(p=>p.Id.StartsWith("loot.",StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p=>p.Id).ToHashSet(StringComparer.Ordinal);
            var expectedIDs=changes.Keys.ToHashSet(StringComparer.Ordinal);
            if(corpseChoice!=null)expectedIDs.Add("loot.npcCorpseBonus");
            Check(changed.SetEquals(expectedIDs),"Unexpected unrelated Loot changes: "+string.Join(",",changed));
            // Count the intentional non-Loot 100-HP default only after checking
            // the requested Loot parameter set itself.
            expectedIDs.Add("survival.sleepHealthRestore");
            // Only source-level modifications can activate special generators.
            var actual=new CfgPatchWriterService().Write(vm,Catalog,
                dlc?new GameInstallInfo{HasDlc1=true}:null,dir);
            var paths=ReadOutput(dir);
            AssertProtectedBaseStashesUntouched(paths, label);
            var expected=new Dictionary<string,string>(StringComparer.Ordinal);
            var refs=new Dictionary<string,string>(StringComparer.Ordinal);
            void Add(string file,string path,string value)
            {
                var key=file+"|"+path;
                Check(expected.TryAdd(key,value),"Expected special CFG leaf collision: "+key);
            }
            // Explicitly account for the source-backed default outside Loot.
            // Otherwise a valid fresh app profile incorrectly looks like two
            // unexpected edits in every vanilla Loot-only scenario.
            Add("GameData/EffectPrototypes.cfg", "SleepHealthRestore/ValueMin", "100");
            Add("GameData/EffectPrototypes.cfg", "SleepHealthRestore/ValueMax", "100");
            void Generator(string configName,double quantity,double? chance=null,double? grenadeQty=null)
            {
                var config=Special.GetProperty(configName);
                var file=Str(config,"file");
                var roots=new[]{Str(config,"customGeneratorSid")}
                    .Concat(config.GetProperty("rankWrappers").EnumerateArray()
                        .Select(r=>Str(r,"targetSid"))).ToArray();
                foreach(var root in roots)
                {
                    refs[file+"|"+root]="refurl="+Str(config,"baseTemplateRefUrl")
                        +";refkey="+Str(config,"baseTemplateRefKey");
                    Add(file,root+"/SID",root);
                    foreach(var category in config.GetProperty("templateCategories").EnumerateArray())
                    {
                        var cat=Str(category,"node");
                        foreach(var field in category.GetProperty("fields").EnumerateObject())
                            Add(file,root+"/ItemGenerator/"+cat+"/"+field.Name,field.Value.GetString()!);
                        foreach(var item in category.GetProperty("items").EnumerateArray())
                        {
                            var entry=Str(item,"node");
                            foreach(var field in item.GetProperty("fields").EnumerateObject())
                            {
                                var val=field.Value.GetString()!;
                                if(field.Name is "MinCount" or "MaxCount")
                                {
                                    var sourceEntry=category.GetProperty("node").GetString();
                                    var ammo=(configName=="cheapStashProduction"
                                        ? cat=="[3]" && config.GetProperty("ammoEntries").EnumerateArray()
                                            .Any(x=>Str(x,"entryNode")==entry)
                                        : config.GetProperty("ammoEntries").EnumerateArray().Any(x=>
                                            Str(x,"categoryNode")==cat && Str(x,"entryNode")==entry));
                                    if(ammo)
                                    {
                                        var rec=config.GetProperty("ammoEntries").EnumerateArray().First(x=>
                                            Str(x,"entryNode")==entry &&
                                            (configName=="cheapStashProduction" || Str(x,"categoryNode")==cat));
                                        val=HalfUpPositive(rec.GetProperty(field.Name=="MinCount"?"sourceMinCount":"sourceMaxCount").GetInt32(),quantity).ToString(CultureInfo.InvariantCulture);
                                    }
                                    if(configName=="cheapStashProduction" && grenadeQty.HasValue
                                        && cat==Str(config.GetProperty("grenadeEntry"),"categoryNode")
                                        && entry==Str(config.GetProperty("grenadeEntry"),"entryNode"))
                                        val=grenadeQty.Value.ToString("0",CultureInfo.InvariantCulture);
                                }
                                else if(field.Name=="Chance" && configName=="cheapStashProduction"
                                    && chance.HasValue && cat==Str(config.GetProperty("grenadeEntry"),"categoryNode")
                                    && entry==Str(config.GetProperty("grenadeEntry"),"entryNode"))
                                    val=chance.Value.ToString("0.###",CultureInfo.InvariantCulture);
                                else if(field.Name=="Chance" && configName=="expensiveStashProduction"
                                    && chance.HasValue && config.GetProperty("couponEntries").EnumerateArray().Any(x=>
                                        Str(x,"categoryNode")==cat && Str(x,"entryNode")==entry
                                        && x.GetProperty("difficultySids").EnumerateArray()
                                           .Any(d=>d.GetString()==Catalog.DifficultyProfiles[difficulty].Sid)))
                                    val=chance.Value.ToString("0.###",CultureInfo.InvariantCulture);
                                Add(file,root+"/ItemGenerator/"+cat+"/PossibleItems/"+entry+"/"+field.Name,val);
                            }
                        }
                    }
                }
                foreach(var row in Redirects(config,dlc,excludeProtectedActors:true))
                    Add(row.File,row.Prototype+"/"+row.NodePath.Replace(".","/")+"/"+row.Key,row.Target);
            }
            void Medical(double multiplier)
            {
                var config=Special.GetProperty("npcMedicalLootProduction");
                var file=Str(config,"file");
                foreach(var pool in config.GetProperty("pools").EnumerateArray())
                {
                    var root=Str(pool,"customSid");
                    refs[file+"|"+root]="refurl="+Str(config,"baseTemplateRefUrl")
                        +";refkey="+Str(config,"baseTemplateRefKey");
                    Add(file,root+"/SID",root);
                    foreach(var category in pool.GetProperty("templateCategories").EnumerateArray())
                    {
                        var cat=Str(category,"node");
                        foreach(var field in category.GetProperty("fields").EnumerateObject())
                            Add(file,root+"/ItemGenerator/"+cat+"/"+field.Name,field.Value.GetString()!);
                        foreach(var item in category.GetProperty("items").EnumerateArray())
                        {
                            var entry=Str(item,"node");
                            foreach(var field in item.GetProperty("fields").EnumerateObject())
                            {
                                var value=field.Value.GetString()!;
                                if(field.Name=="Chance")
                                {
                                    var src=pool.GetProperty("medicalEntries").EnumerateArray()
                                        .Where(x=>Str(x,"categoryNode")==cat && Str(x,"entryNode")==entry)
                                        .ToArray();
                                    if(src.Length==1)value=Math.Clamp(Num(Str(src[0],"sourceChanceRaw"))*multiplier,0,1)
                                        .ToString("0.###",CultureInfo.InvariantCulture);
                                }
                                Add(file,root+"/ItemGenerator/"+cat+"/PossibleItems/"+entry+"/"+field.Name,value);
                            }
                        }
                    }
                }
                foreach(var row in Redirects(config,dlc))
                    Add(row.File,row.Prototype+"/"+row.NodePath.Replace(".","/")+"/"+row.Key,row.Target);
            }
            void Boxes(string configName,double quantity)
            {
                var config=Special.GetProperty(configName);
                var file=Str(config,"file");
                foreach(var pool in config.GetProperty("pools").EnumerateArray())
                {
                    var root=Str(pool,"targetSid");
                    refs[file+"|"+root]="refurl="+Str(config,"baseTemplateRefUrl")
                        +";refkey="+Str(config,"baseTemplateRefKey");
                    Add(file,root+"/SID",root);
                    var cat=Str(pool,"categoryNode");
                    foreach(var field in pool.GetProperty("categoryFields").EnumerateObject())
                        Add(file,root+"/ItemGenerator/"+cat+"/"+field.Name,field.Value.GetString()!);
                    foreach(var item in pool.GetProperty("items").EnumerateArray())
                    {
                        var entry=Str(item,"node");
                        foreach(var field in item.GetProperty("fields").EnumerateObject())
                        {
                            var value=field.Value.GetString()!;
                            if(field.Name is "MinCount" or "MaxCount")
                                value=HalfUpPositive(item.GetProperty(field.Name=="MinCount"?"sourceMinCount":"sourceMaxCount").GetInt32(),quantity)
                                    .ToString(CultureInfo.InvariantCulture);
                            if(field.Name=="Weight" && configName=="destructibleAmmoProduction")
                            {
                                var sid=Str(item.GetProperty("fields"),"ItemPrototypeSID");
                                var parameterId=Str(config.GetProperty("weightParameterIds"),sid);
                                var weight=changes.GetValueOrDefault(parameterId,1.0);
                                value=(Num(value)*weight).ToString("0.###",CultureInfo.InvariantCulture);
                            }
                            Add(file,root+"/ItemGenerator/"+cat+"/PossibleItems/"+entry+"/"+field.Name,value);
                        }
                    }
                }
                foreach(var row in Redirects(config,dlc))
                    Add(row.File,row.Prototype+"/"+row.NodePath.Replace(".","/")+"/"+row.Key,row.Target);
            }

            var cheapIDs=new[]{"loot.cheapStashAmmoQuantity","loot.cheapStashGrenadeChance","loot.cheapStashGrenadeQuantity"};
            if(cheapIDs.Any(changes.ContainsKey))
                Generator("cheapStashProduction",changes.GetValueOrDefault(cheapIDs[0],1),
                    changes.GetValueOrDefault(cheapIDs[1],0.1),
                    changes.GetValueOrDefault(cheapIDs[2],1));
            if(changes.ContainsKey("loot.mediumStashAmmoQuantity"))
                Generator("mediumStashAmmoProduction",changes["loot.mediumStashAmmoQuantity"]);
            if(changes.ContainsKey("loot.expensiveStashAmmoQuantity"))
                Generator("expensiveStashProduction",changes["loot.expensiveStashAmmoQuantity"]);
            if(changes.ContainsKey("loot.npcMedicalChance"))
                Medical(changes["loot.npcMedicalChance"]);
            var boxIDs=new[]{"loot.destructibleAmmoQuantity","loot.ammoBoxWeight918",
                "loot.ammoBoxWeight045","loot.ammoBoxWeight545","loot.ammoBoxWeight012"};
            if(boxIDs.Any(changes.ContainsKey))
                Boxes("destructibleAmmoProduction",changes.GetValueOrDefault(boxIDs[0],1));
            if(changes.ContainsKey("loot.destructibleGrenadeQuantity"))
                Boxes("destructibleGrenadeProduction",changes["loot.destructibleGrenadeQuantity"]);

            // Corpse choice is difficulty-specific and intentionally replaces
            // only EconomyDifficulty.CorpseSmartLoot rather than vanilla NPC pools.
            if(corpseChoice!=null)
            {
                var sid=Catalog.DifficultyProfiles[difficulty].Sid;
                var first="GameData/DifficultyPrototypes.cfg|"+sid+"/EconomyDifficulty/CorpseSmartLoot";
                if(corpseChoice.Length==0)Add("GameData/DifficultyPrototypes.cfg",sid+"/EconomyDifficulty/CorpseSmartLoot","");
                else Add("GameData/DifficultyPrototypes.cfg",sid+"/EconomyDifficulty/CorpseSmartLoot/GeneralNPC",corpseChoice);
            }

            if(changes.ContainsKey("loot.npcWeaponAmmoSupply") &&
                vm.AllParameters.Single(p=>p.Id=="loot.npcCorpseBonus").CurrentText
                    .Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)
                    .Contains("NPC_Ammo_Smart",StringComparer.Ordinal))
            {
                var config=Special.GetProperty("npcCorpseAmmoProduction");
                var root=Str(config,"targetSid");
                var file=Str(config,"file");
                refs[file+"|"+root]="refurl="+Str(config,"baseTemplateRefUrl")
                    +";refkey="+Str(config,"baseTemplateRefKey");
                Add(file,root+"/SID",root);
                var multiplier=changes["loot.npcWeaponAmmoSupply"];
                foreach(var entry in config.GetProperty("entries").EnumerateArray())
                {
                    var path=root+"/ItemGenerators/["+entry.GetProperty("rankIndex").GetInt32()+"]/SmartLootParams/PrimaryWeaponParams/["
                        +entry.GetProperty("groupIndex").GetInt32()+"]/Items/["+entry.GetProperty("itemIndex").GetInt32()+"]";
                    Add(file,path+"/MinCount",HalfUpZero(entry.GetProperty("minCount").GetInt32(),multiplier).ToString(CultureInfo.InvariantCulture));
                    Add(file,path+"/MaxCount",HalfUpZero(entry.GetProperty("maxCount").GetInt32(),multiplier).ToString(CultureInfo.InvariantCulture));
                }
                var sid=Catalog.DifficultyProfiles[difficulty].Sid;
                var packages=vm.AllParameters.Single(p=>p.Id=="loot.npcCorpseBonus").CurrentText
                    .Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
                var altered=string.Join(", ",packages.Select(x=>x=="NPC_Ammo_Smart"?root:x));
                Add("GameData/DifficultyPrototypes.cfg",sid+"/EconomyDifficulty/CorpseSmartLoot/GeneralNPC",altered);
            }
            var dlcFreshExtension=dlc && new[]{
                "loot.cheapStashAmmoQuantity","loot.cheapStashGrenadeChance",
                "loot.cheapStashGrenadeQuantity" }.Any(changes.ContainsKey);
            Check(dlcFreshExtension
                ? paths.Values.SelectMany(x=>x.Keys).Count()>=expected.Count
                : paths.Values.SelectMany(x=>x.Keys).Count()==expected.Count,
                label+": unexpected number of production assignments observed "
                +paths.Values.SelectMany(x=>x.Keys).Count()+" expected "+expected.Count);
            foreach(var (key,expectedValue) in expected)
            {
                var parts=key.Split('|',2);
                Check(paths.TryGetValue(parts[0],out var doc),
                    label+": missing CFG document "+parts[0]);
                Check(doc!.TryGetValue(parts[1],out var actualVal),
                    label+": missing CFG field "+key);
                if(double.TryParse(expectedValue,CultureInfo.InvariantCulture,out var expectedNum)
                    && double.TryParse(actualVal,CultureInfo.InvariantCulture,out var actualNum))
                    Check(Math.Abs(expectedNum-actualNum)<0.0000011,label+": numeric mismatch "+key
                        +" expected="+expectedValue+" actual="+actualVal);
                else Check(string.Equals(expectedValue,actualVal,StringComparison.Ordinal),
                    label+": raw CFG text mismatch "+key+" expected="+expectedValue+" actual="+actualVal);
            }
            if (!dlcFreshExtension)
                foreach(var file in paths)
                    foreach(var leaf in file.Value.Keys)
                        Check(expected.ContainsKey(file.Key+"|"+leaf),label+": unrelated writer field "
                            +file.Key+"|"+leaf);
            foreach(var (key,refval) in refs)
            {
                var split=key.Split('|',2);
                var filePath=OutputPath(dir,split[0]);
                var text=File.ReadAllText(filePath);
                Check(text.Contains(split[1]+" : struct.begin",StringComparison.Ordinal)
                    && text.Contains(refval,StringComparison.Ordinal),
                    label+": missing clone/physical refurl root: "+key);
            }
            Check(actual.FileCount==paths.Count && actual.ParameterCount==expectedIDs.Count,
                label+": writer file/control counts disagree with source modifications: "
                +actual.FileCount+"/"+actual.ParameterCount+"/"+actual.EditCount);
            if(!dlcFreshExtension && corpseChoice==null && !changes.ContainsKey("loot.npcWeaponAmmoSupply"))
                Check(actual.EditCount==expected.Count,label+": one emitted assignment per intended field drift");
            Console.WriteLine("PASS "+difficulty+" "+label+(dlc?" +DLC1":" BASE")
                +" ("+paths.Count+" files / "+expected.Count+" exact assigned fields)");
            Passed++;
        }
        finally{if(Keep==null && Directory.Exists(dir))Directory.Delete(dir,true);}
    }

    static JsonElement StrGet(JsonElement obj,string key)=>obj.GetProperty(key);
    static string Str(JsonElement obj,string key)=>obj.GetProperty(key).GetString()!;
    static int HalfUpPositive(int source,double scale)=>Math.Max(1,(int)Math.Floor(source*scale+0.5));
    static int HalfUpZero(int source,double scale)=>Math.Max(0,(int)Math.Floor(source*scale+0.5));
    static double Num(string s)=>double.Parse(s.TrimEnd('f','F'),CultureInfo.InvariantCulture);

    readonly record struct Redirect(string File,string Prototype,string NodePath,
                                    string Key,string Target,string PhysicalContainerSid);
    static IEnumerable<Redirect> Redirects(JsonElement config,bool includeDlc,
                                           bool excludeProtectedActors=false)
    {
        if(!config.TryGetProperty("redirectManifestResource",out var prop))
            yield break;
        var resource=prop.GetString()!;
        var file=resource.Replace("TrueCustomDifficulty.Data.","",StringComparison.Ordinal);
        var dir=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(dir!=null && !File.Exists(Path.Combine(dir.FullName,"src","TrueCustomDifficulty","Data",file)))
            dir=dir.Parent;
        if(dir==null)throw new FileNotFoundException("Missing real production redirect CSV "+file);
        var path=Path.Combine(dir.FullName,"src","TrueCustomDifficulty","Data",file);
        var lines=File.ReadAllLines(path);
        foreach(var line in lines.Skip(1).Where(x=>!string.IsNullOrWhiteSpace(x)))
        {
            var c=line.Split(',');
            Check(c.Length>=8,"Wrong production CSV columns "+file);
            if(!includeDlc && c[1].StartsWith("DLCGameData/",StringComparison.OrdinalIgnoreCase))
                continue;
            if(excludeProtectedActors)
            {
                if(IncludesProtectedPhysicalStash(c[7])
                   || c[1]=="GameData/SpawnActorPrototypes.cfg"
                      && BaseProtectedActorIds.Value.Contains(c[2]))
                    continue;
                if(c[1]=="DLCGameData/DLC1/SpawnActorPrototypes.cfg"
                   && !DlcEligibleActorIds.Value.Contains(c[2]))
                    continue;
            }
            yield return new Redirect(c[1].Trim(),c[2].Trim(),c[3].Trim(),c[4].Trim(),c[6].Trim(),c[7].Trim());
        }
    }

    static string OutputPath(string root,string source)
    {
        var file=Path.GetFileName(source);
        var stem=Path.GetFileNameWithoutExtension(file);
        var parent=Path.GetDirectoryName(source.Replace('/',Path.DirectorySeparatorChar))??"";
        var folder=Path.Combine(root,"Stalker2","Content","GameLite",parent);
        return file.EndsWith("Prototypes.cfg",StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(folder,stem,stem+"_patch_TrueCustomDifficulty.cfg")
            : Path.Combine(folder,file+"_patch_TrueCustomDifficulty");
    }

    // No naive text substring check: each generated file is parsed into
    // concrete full-path leaf identities, retaining clone and bpatch nesting.
    static Dictionary<string,Dictionary<string,string>> ReadOutput(string folder)
    {
        var docs=new Dictionary<string,Dictionary<string,string>>(StringComparer.Ordinal);
        foreach(var path in Directory.GetFiles(folder,"*TrueCustomDifficulty*",SearchOption.AllDirectories))
        {
            var full=path.Replace('\\','/');
            var start=full.IndexOf("/Stalker2/Content/GameLite/",StringComparison.Ordinal);
            Check(start>=0,"Unknown output layout "+full);
            var rel=full[(start+"/Stalker2/Content/GameLite/".Length)..];
            var segments=rel.Split('/');
            string file;
            if(rel.EndsWith("_patch_TrueCustomDifficulty.cfg",StringComparison.Ordinal)
                && segments.Length>=2 && segments[^2]==segments[^1].Replace("_patch_TrueCustomDifficulty.cfg",""))
            {
                var f=segments[^1].Replace("_patch_TrueCustomDifficulty.cfg",".cfg");
                file=string.Join("/",segments.Take(segments.Length-2).Append(f));
            }
            else file=rel.Replace("_patch_TrueCustomDifficulty","");
            var fields=new Dictionary<string,string>(StringComparer.Ordinal);
            var stack=new List<string>();
            foreach(var original in File.ReadAllText(path).Replace("\r","").Split('\n'))
            {
                var line=original.Trim();
                if(line.Length==0 || line.StartsWith("//",StringComparison.Ordinal))continue;
                if(line=="struct.end")
                {
                    Check(stack.Count>0,"Extra struct.end "+file);
                    stack.RemoveAt(stack.Count-1);continue;
                }
                var open=Regex.Match(line,@"^(?<name>.+?)\s*:\s*struct\.begin(?<extra>.*)$");
                if(open.Success)
                {
                    var name=open.Groups["name"].Value;
                    var extra=open.Groups["extra"].Value;
                    Check(stack.Count!=0 || extra.Contains("{bpatch}",StringComparison.Ordinal)
                        || extra.Contains("{refurl=",StringComparison.Ordinal),
                        "Top-level root is neither patch nor referenced clone: "+line);
                    stack.Add(name);continue;
                }
                var kv=Regex.Match(line,@"^(?<name>\S+)\s*=\s*(?<value>.*)$");
                Check(kv.Success && stack.Count>0,"Unparsed special CFG syntax: "+line);
                Check(fields.TryAdd(string.Join("/",stack)+"/"+kv.Groups["name"].Value,
                    kv.Groups["value"].Value),"Duplicate output field "+file);
            }
            Check(stack.Count==0,"Unclosed CFG doc "+file);
            Check(docs.TryAdd(file,fields),"Duplicate emitted physical CFG "+file);
        }
        return docs;
    }
    static void Check(bool valid,string msg)
    {
        if(!valid)throw new InvalidDataException(msg);
    }
}
