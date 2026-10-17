using System.Globalization;
using System.IO;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

// Phase 5: executable native branch browser -> production CFG field contract.
// This is emitted-patch verification, NOT an Unreal RNG/drop-rate simulation.
internal static partial class Program
{
    private static StashBranchPreview NativeBranch(
        IReadOnlyList<StashBranchPreview> branches, string prototype, string groupPath)
        => branches.Single(b => b.Prototype == prototype && b.GroupPath == groupPath);

    private static StashEffectiveState NativeState(string difficulty,
        params (string Id, double Value)[] values)
    {
        var map = values.ToDictionary(x => x.Id, x => x.Value, StringComparer.Ordinal);
        return new StashEffectiveState(map, map.Keys, difficulty);
    }

    private static double NativeValue(StashBranchPreview group,
        StashEffectiveState state, int itemIndex = 0)
        => StashEffectiveState.Number(group.EffectiveSelection(
            Baseline, state, group.Items[itemIndex]));

    static void VerifyStashBranchBrowserParity()
    {
        var branches = StashBranchPreviewCatalog.Load(Baseline);
        Check(branches.Count == 463 && branches.Count(b => !b.IsSmart) == 41
            && branches.Count(b => b.IsSmart) == 422
            && branches.Sum(b => b.Items.Count) == 1124,
            "Native browser branch/item inventory drift.");
        Check(branches.Count(b => !b.IsSmart && b.Field == "Weight") == 19
            && branches.Count(b => !b.IsSmart && b.Field == "Chance") == 22
            && branches.Count(b => b.IsSmart && b.Field == "Weight") == 410
            && branches.Count(b => b.IsSmart && b.Field == "Chance") == 12,
            "Native Weight/Chance group split drift.");
        Check(branches.Select(b => (b.Layer, b.Prototype, b.GroupPath))
                .Distinct().Count() == 463,
            "Native group paths collided or were deduplicated.");

        // Every native source SID must resolve to the same six UI palettes
        // used by ItemGenerator / SmartLoot calculations; "Antirad" is a
        // deliberate case-sensitive SmartLoot alias and cannot be omitted.
        var categoryIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "loot.stashMixAmmo", "loot.stashMixMedical", "loot.stashMixFood",
            "loot.stashMixDrinks", "loot.stashMixGrenades",
            "loot.stashMixCoupons"
        };
        var itemSids = branches.SelectMany(b => b.Items)
            .Select(item => item.ItemSid).Distinct(StringComparer.Ordinal).ToArray();
        Check(itemSids.Length == 46 && itemSids.Contains("Antirad"),
            "Native item SID coverage or case-sensitive AntiRad alias drift.");
        Check(itemSids.All(sid => categoryIds.Contains(
                StashEffectiveState.ItemBiasId(sid)
                    ?? StashEffectiveState.SmartItemBiasId(sid) ?? "")),
            "A source item lost its native six-color category mapping.");

        // Every category factor is the SAME for all ranks and tier/world
        // sources; original generator eligibility gates remain native.
        foreach (var categoryId in categoryIds)
        {
            var categorySids = itemSids
                .Where(sid => StashEffectiveState.ItemBiasId(sid) == categoryId)
                .ToArray();
            Check(categorySids.Length > 0, categoryId + ": empty global category");
            foreach (var difficulty in new[] { "Easy", "Medium", "Hard", "Stalker" })
            {
                var off = NativeState(difficulty, (categoryId, 0));
                var half = NativeState(difficulty, (categoryId, .5));
                var original = NativeState(difficulty, (categoryId, 1));
                Check(categorySids.All(sid =>
                        Math.Abs(off.TierFactor(sid)) < 1e-9
                        && Math.Abs(off.SmartFactor(sid)) < 1e-9
                        && Math.Abs(half.TierFactor(sid) - .5) < 1e-9
                        && Math.Abs(half.SmartFactor(sid) - .5) < 1e-9
                        && Math.Abs(original.TierFactor(sid) - 1) < 1e-9
                        && Math.Abs(original.SmartFactor(sid) - 1) < 1e-9),
                    categoryId + ": global OFF / REDUCED / ORIGINAL state "
                    + "changed with rank, tier or difficulty");
            }
        }

        var basic = NativeBranch(branches, "GamePass_Stash_ItemGenerator_Cheap",
            "ItemGenerator.[7].PossibleItems");
        var hard = NativeBranch(branches, "GamePass_Stash_ItemGenerator_Cheap",
            "ItemGenerator.[1].PossibleItems");
        Check(basic.AppliesTo(1, "Easy") && basic.AppliesTo(1, "Medium")
            && !basic.AppliesTo(0, "Easy") && !basic.AppliesTo(1, "Hard")
            && hard.AppliesTo(0, "Hard") && hard.AppliesTo(3, "Hard")
            && !hard.AppliesTo(0, "Easy"),
            "Native ItemGenerator rank/difficulty filter lost its authored gates.");
        var neutral = NativeState("Easy");
        var sum = basic.Items.Sum(item => StashEffectiveState.Number(
            basic.EffectiveSelection(Baseline, neutral, item)));
        Check(Math.Abs(sum - 17.77) < 1e-7,
            "Native Easy/Medium Experienced Weight total must be 17.77.");
        var foodTriple = NativeState("Easy", ("loot.stashMixFood", 3));
        Check(Math.Abs(basic.Items.Sum(item => StashEffectiveState.Number(
            basic.EffectiveSelection(Baseline, foodTriple, item))) - 43.77) < 1e-7,
            "3x food bias must create 43.77, never synthetic category odds.");
        var zero = NativeState("Easy", ("loot.stashMixFood", 0),
            ("loot.stashMixMedical", 0), ("loot.stashMixDrinks", 0),
            ("loot.stashMixCoupons", 0));
        Check(basic.Items.All(item => NativeSelectionIsZero(basic, zero, item)),
            "All-zero native Weight group must have zero sum and undefined relative shares.");

        var standardAmmo = NativeBranch(branches,
            "GamePass_Stash_ItemGenerator_Common_Var1",
            "ItemGenerator.[1].PossibleItems");
        Check(standardAmmo.Items.Count(x => x.ItemSid == "A919D") == 2,
            "Duplicate native ammo SID rows must not be deduplicated.");

        var grenade = NativeBranch(branches, "GamePass_Stash_ItemGenerator_Cheap",
            "ItemGenerator.[5].PossibleItems");
        var chance = NativeState("Easy",
            ("loot.cheapStashGrenadeChance", .8),
            ("loot.stashMixGrenades", 3));
        Check(Math.Abs(NativeValue(grenade, chance) - 1) < 1e-9,
            "Grenade bonus must precede bias and clamp native Chance to 1.");
        var rareEasy = NativeBranch(branches, "GamePass_Stash_ItemGenerator_Rare",
            "ItemGenerator.[11].PossibleItems");
        var rareHard = NativeBranch(branches, "GamePass_Stash_ItemGenerator_Rare",
            "ItemGenerator.[15].PossibleItems");
        var couponEasy = NativeState("Easy",
            ("loot.expensiveStashMoneyChance", .6),
            ("loot.stashMixCoupons", 1.5));
        var couponHard = NativeState("Hard",
            ("loot.expensiveStashMoneyChance", .6),
            ("loot.stashMixCoupons", 1.5));
        Check(rareEasy.AppliesTo(0, "Easy") && !rareEasy.AppliesTo(0, "Hard")
            && rareHard.AppliesTo(0, "Hard") && !rareHard.AppliesTo(0, "Easy")
            && Math.Abs(NativeValue(rareEasy, couponEasy) - .9) < 1e-9
            && Math.Abs(NativeValue(rareHard, couponHard) - .9) < 1e-9
            && Math.Abs(NativeValue(rareEasy, couponHard) - .3) < 1e-9,
            "Rare Coupons bonus must obey actual native difficulty gates.");

        var smartMixed = NativeBranch(branches, "StashMedicine_Smart",
            "ItemGenerators.[2].SmartLootParams.ConsumablesParams.[0].Items");
        var mixedState = NativeState("Easy",
            ("loot.stashMedicalMedkitBias", 0),
            ("loot.stashMedicalArmyMedkitBias", 2));
        var mixedSpawn = smartMixed.EffectiveSpawn(mixedState);
        Check(smartMixed.Items.Count == 2
            && smartMixed.Items[0].ItemSid == "Medkit"
            && smartMixed.Items[1].ItemSid == "ArmyMedkit"
            && mixedSpawn.Min == smartMixed.MinSpawnChance
            && mixedSpawn.Max == smartMixed.MaxSpawnChance
            && mixedSpawn.Sets == smartMixed.ItemSetCount,
            "Heterogeneous SmartLoot item factors must not scale group endpoints.");
        Check(smartMixed.EffectiveCount(Baseline, mixedState,
                smartMixed.Items[0], "MinCount") == "0"
            && smartMixed.EffectiveCount(Baseline, mixedState,
                smartMixed.Items[0], "MaxCount") == "0",
            "Disabled SmartLoot Medkit must show actual zero-count patch.");

        var smartGrenades = NativeBranch(branches, "StashGrenade_Cheap",
            "ItemGenerators.[0].SmartLootParams.GrenadesParams.[0].Items");
        var smartOff = NativeState("Easy", ("loot.stashMixGrenades", 0));
        Check(smartGrenades.Items.All(item =>
                smartGrenades.EffectiveCount(Baseline, smartOff, item, "MinCount") == "0"
                && smartGrenades.EffectiveCount(Baseline, smartOff, item, "MaxCount") == "0")
            && smartGrenades.EffectiveSpawn(smartOff).Sets == "0",
            "SmartLoot zero-factor count and ItemSetCount must be disabled together.");

        Console.WriteLine("PASS Phase 5: six global category factors across "
            + "463 native source groups and all 1124 original item entries");

        // Each profile generates REAL CFG patch files, then checks every
        // emitted field belonging to an actually affected native group.
        var cases = new[]
        {
            (Name: "food-3x", Difficulty: "Novice", Dlc: false,
                Changes: new Dictionary<string, double>
                {
                    ["loot.stashMixFood"] = 3
                }),
            (Name: "all-category-zero", Difficulty: "Master", Dlc: false,
                Changes: new Dictionary<string, double>
                {
                    ["loot.stashMixAmmo"] = 0,
                    ["loot.stashMixMedical"] = 0,
                    ["loot.stashMixFood"] = 0,
                    ["loot.stashMixDrinks"] = 0,
                    ["loot.stashMixGrenades"] = 0,
                    ["loot.stashMixCoupons"] = 0
                }),
            (Name: "hard-bonus-medical-dlc", Difficulty: "Veteran", Dlc: true,
                Changes: new Dictionary<string, double>
                {
                    ["loot.stashMixMedical"] = .5,
                    ["loot.stashMedicalMedkitBias"] = 0,
                    ["loot.stashMedicalArmyMedkitBias"] = 2,
                    ["loot.cheapStashGrenadeChance"] = .8,
                    ["loot.stashMixGrenades"] = 3,
                    ["loot.expensiveStashMoneyChance"] = .6,
                    ["loot.stashMixCoupons"] = 1.5,
                    ["loot.stashMedkitMaxCount"] = -1
                })
        };
        foreach (var sample in cases)
            VerifyNativeEmittedFields(branches, sample.Name, sample.Difficulty,
                sample.Dlc, sample.Changes);
    }

    private static bool NativeSelectionIsZero(
        StashBranchPreview branch, StashEffectiveState snapshot,
        StashBranchPreviewRow row)
        => Math.Abs(StashEffectiveState.Number(
            branch.EffectiveSelection(Baseline, snapshot, row))) < 1e-9;

    private static void VerifyNativeEmittedFields(
        IReadOnlyList<StashBranchPreview> branches, string name,
        string difficulty, bool hasDlc, IReadOnlyDictionary<string, double> changes)
    {
        var dir = Path.Combine(Keep ?? Path.GetTempPath(), "TCD_NativeBranchParity", name);
        try
        {
            var vm = new MainViewModel(Catalog)
            {
                SelectedReferenceDifficulty = difficulty
            };
            foreach (var (id, target) in changes)
            {
                var parameter = vm.AllParameters.Single(p => p.Id == id);
                parameter.SnapCurrentNumberToMarker(target);
                Check(parameter.IsValueModifiedFromReference(),
                    name + ": specified source did not change: " + id);
            }
            AssertOnlyExpectedNonLootDefault(vm);
            var modified = vm.AllParameters
                .Where(p => p.IsValueModifiedFromReference())
                .Select(p => p.Id).ToArray();
            var snapshot = new StashEffectiveState(vm.AllParameters, modified,
                Catalog.DifficultyProfiles[difficulty].Sid);
            new CfgPatchWriterService().Write(vm, Catalog,
                hasDlc ? new GameInstallInfo { HasDlc1 = true } : null, dir);
            var generated = ReadOutput(dir);
            Check(generated.TryGetValue("GameData/ItemGeneratorPrototypes.cfg",
                    out var tiers), name + ": native tier clones missing");
            var checkedTierFields = 0;
            foreach (var group in branches.Where(b => !b.IsSmart))
            {
                var root = "TCD_" + group.Prototype + "/" +
                    group.GroupPath.Replace(".", "/");
                foreach (var row in group.Items)
                {
                    var prefix = root + "/[" + row.Index.ToString(
                        CultureInfo.InvariantCulture) + "]/";
                    foreach (var (field, predicted) in new[]
                    {
                        (group.Field, group.EffectiveSelection(Baseline, snapshot, row)),
                        ("MinCount", group.EffectiveCount(Baseline, snapshot, row, "MinCount")),
                        ("MaxCount", group.EffectiveCount(Baseline, snapshot, row, "MaxCount"))
                    })
                    {
                        if (predicted.Length == 0) continue;
                        var key = prefix + field;
                        Check(tiers!.TryGetValue(key, out var emitted)
                            && emitted == predicted,
                            name + ": native " + key + " expected " + predicted
                            + " emitted " + (emitted ?? "<missing>"));
                        checkedTierFields++;
                    }
                }
            }

            var checkedSmartFields = 0;
            var checkedSmartGroups = 0;
            Check(generated.TryGetValue("GameData/StashPrototypes.cfg", out var smart),
                name + ": affected SmartLoot clone missing");
            foreach (var group in branches.Where(b => b.IsSmart))
            {
                var root = "TCD_" + group.Prototype + "/" +
                    group.GroupPath.Replace(".", "/");
                var prefix = root[..root.LastIndexOf("/Items", StringComparison.Ordinal)];
                var affected = group.Items.Where(row =>
                    Math.Abs(snapshot.SmartFactor(row.ItemSid) - 1) > 1e-7
                    || (row.ItemSid is "Medkit" or "ArmyMedkit")
                    && (snapshot.Value($"loot.stash{row.ItemSid}MinCount", 0) > 0
                        || snapshot.Value($"loot.stash{row.ItemSid}MaxCount", 0) > 0))
                    .ToArray();
                if (affected.Length == 0) continue;
                checkedSmartGroups++;
                foreach (var row in affected)
                {
                    var itemPrefix = root + "/[" + row.Index.ToString(
                        CultureInfo.InvariantCulture) + "]/";
                    var predicted = group.EffectiveSelection(Baseline, snapshot, row);
                    var key = itemPrefix + group.Field;
                    Check(smart!.TryGetValue(key, out var actual)
                        && actual == predicted,
                        name + ": SmartLoot " + key + " expected " + predicted
                        + " emitted " + (actual ?? "<missing>"));
                    checkedSmartFields++;
                    foreach (var field in new[] { "MinCount", "MaxCount" })
                    {
                        var raw = field == "MinCount" ? row.MinCount : row.MaxCount;
                        if (raw.Length == 0) continue;
                        var count = group.EffectiveCount(Baseline, snapshot, row, field);
                        // Writer explicitly patches both endpoints at factor=0,
                        // and otherwise only changes native Medkit count bounds.
                        if (snapshot.SmartFactor(row.ItemSid) > 1e-7
                            && count == raw) continue;
                        key = itemPrefix + field;
                        Check(smart.TryGetValue(key, out actual) && actual == count,
                            name + ": SmartLoot count " + key
                            + " expected " + count + " emitted "
                            + (actual ?? "<missing>"));
                        checkedSmartFields++;
                    }
                }

                if (StashEffectiveState.UniformSmartFactor(
                    group.Items.Select(x => snapshot.SmartFactor(x.ItemSid)),
                    out var uniform))
                {
                    var spawn = group.EffectiveSpawn(snapshot);
                    foreach (var (field, original, predicted) in new[]
                    {
                        ("MinSpawnChance", group.MinSpawnChance, spawn.Min),
                        ("MaxSpawnChance", group.MaxSpawnChance, spawn.Max),
                        ("ItemSetCount", group.ItemSetCount, spawn.Sets)
                    })
                    {
                        if (original.Length == 0
                            || field == "ItemSetCount" && uniform > 0) continue;
                        var key = prefix + "/" + field;
                        Check(smart!.TryGetValue(key, out var actual)
                            && actual == predicted,
                            name + ": group spawn " + key
                            + " expected " + predicted
                            + " emitted " + (actual ?? "<missing>"));
                        checkedSmartFields++;
                    }
                }
            }
            Check(checkedTierFields >= 300 && checkedSmartFields > 0
                && checkedSmartGroups > 0,
                name + ": insufficient emitted CFG coverage");
            AssertProtectedBaseStashesUntouched(generated, name);
            if (!hasDlc)
                Check(!generated.ContainsKey("DLCGameData/DLC1/SpawnActorPrototypes.cfg"),
                    name + ": base-only patch includes DLC actors");
            else if (generated.TryGetValue("DLCGameData/DLC1/SpawnActorPrototypes.cfg",
                         out var dlcActors))
                Check(dlcActors.Keys.Select(k => k.Split('/')[0])
                        .ToHashSet(StringComparer.Ordinal)
                        .IsSubsetOf(DlcEligibleActorIds.Value),
                    name + ": generated DLC1 actor escaped allowlist");
            Console.WriteLine($"PASS {name}: {checkedTierFields} tier fields + "
                + $"{checkedSmartFields} SmartLoot emitted fields in "
                + $"{checkedSmartGroups} affected groups; safety gates");
        }
        finally
        {
            if (Keep is null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
