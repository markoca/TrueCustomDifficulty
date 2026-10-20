using System.Globalization;
using System.IO;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

// Run real production CFG emission for isolated GENERIC sources only.
// These are inspectable writer fixtures, NOT installable PAKs and NOT
// measurements of Unreal spawn rates or already generated inventories.
internal static partial class Program
{
    static void VerifyGenericLootPreGame()
    {
        // Numeric-only reference/0x/high and one deliberate cross-control
        // experiment. No change to persistent source controls or UI schema.
        var cases = new (string Name, string Difficulty, string? Control,
                         double Value, string? CorpseChoice)[]
        {
            ("npc-med-veteran-zero", "Veteran", "loot.npcMedicalChance", 0, null),
            ("npc-med-veteran-high", "Veteran", "loot.npcMedicalChance", 2, null),
            ("npc-med-novice-zero-bonus-active", "Novice", "loot.npcMedicalChance", 0, null),
            ("npc-med-novice-zero-corpse-none", "Novice", "loot.npcMedicalChance", 0, ""),
            ("npc-ammo-novice-zero", "Novice", "loot.npcWeaponAmmoSupply", 0, null),
            ("npc-ammo-novice-high", "Novice", "loot.npcWeaponAmmoSupply", 3, null),
            ("npc-ammo-stalker-water-inactive", "Stalker", "loot.npcWeaponAmmoSupply", 3, null),
            ("stash-medical-zero", "Veteran", "loot.stashMixMedical", 0, null),
            ("stash-ammo-zero", "Veteran", "loot.stashMixAmmo", 0, null),
            ("stash-food-zero", "Veteran", "loot.stashMixFood", 0, null),
            ("stash-drinks-zero", "Veteran", "loot.stashMixDrinks", 0, null),
            ("stash-grenades-zero", "Veteran", "loot.stashMixGrenades", 0, null),
            ("stash-coupons-zero", "Veteran", "loot.stashMixCoupons", 0, null)
        };
        foreach (var scenario in cases)
        {
            var dir = Path.Combine(Keep ?? Path.GetTempPath(),
                "TCD_GenericLoot_PreGame", scenario.Name);
            try
            {
                var vm = new MainViewModel(Catalog)
                {
                    SelectedReferenceDifficulty = scenario.Difficulty
                };
                var parameter = vm.AllParameters.Single(p => p.Id == scenario.Control);
                parameter.SnapCurrentNumberToMarker(scenario.Value);
                Check(parameter.IsValueModifiedFromReference(),
                    scenario.Name + ": expected numerical change is a vanilla value");
                if (scenario.CorpseChoice is not null)
                {
                    var corpse = vm.AllParameters.Single(p => p.Id == "loot.npcCorpseBonus");
                    Check(corpse.IsChoiceSlider
                        && corpse.ChoiceOptions.Any(x => x.Value == scenario.CorpseChoice),
                        scenario.Name + ": invalid corpse bonus choice");
                    corpse.CurrentText = scenario.CorpseChoice;
                    Check(corpse.IsValueModifiedFromReference(),
                        scenario.Name + ": corpse option must change its source value");
                }
                AssertOnlyExpectedNonLootDefault(vm);
                var changedLoot = vm.AllParameters
                    .Where(p => p.Id.StartsWith("loot.", StringComparison.Ordinal)
                        && p.IsValueModifiedFromReference())
                    .Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
                var expectedChanged = new HashSet<string>(StringComparer.Ordinal)
                    { scenario.Control! };
                if (scenario.CorpseChoice is not null)
                    expectedChanged.Add("loot.npcCorpseBonus");
                Check(changedLoot.SetEquals(expectedChanged),
                    scenario.Name + ": unexpected modified Loot source controls");
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
                var result = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
                if (scenario.Name == "npc-ammo-stalker-water-inactive")
                {
                    // An edited ammo multiplier remains saved, but the Stalker
                    // Water package does not include NPC_Ammo_Smart. Since
                    // Sleep HP now starts at native 300, there must be no
                    // automatic fallback CFG edit masking dormant NPC ammo.
                    Check(result.ParameterCount == 0 && result.EditCount == 0
                        && result.FileCount == 0 && parameter.CurrentNumber == 3
                        && parameter.IsValueModifiedFromReference(),
                        "Water package incorrectly counted dormant NPC ammo tuning as applied");
                }
                var output = ReadOutput(dir);
                AssertProtectedBaseStashesUntouched(output, scenario.Name);
                Check(!output.ContainsKey("DLCGameData/DLC1/SpawnActorPrototypes.cfg"),
                    scenario.Name + ": base-only diagnostic wrote DLC1 actors");

                // Never conflate human corpses with world ItemContainers.
                var isNpc = scenario.Name.StartsWith("npc-", StringComparison.Ordinal);
                if (isNpc)
                {
                    Check(!output.ContainsKey("GameData/SpawnActorPrototypes.cfg"),
                        scenario.Name + ": ordinary NPC altered physical world stash actors");
                    if (scenario.Control == "loot.npcMedicalChance")
                        CheckNpcMedicalSourceFields(output, scenario.Name, scenario.Value);
                    else
                        CheckNpcAmmoSourceFields(output, scenario.Name, scenario.Difficulty,
                            scenario.Value);
                    CheckNpcDifficultyIsolation(output, scenario.Name,
                        scenario.Difficulty, scenario.CorpseChoice);
                }
                else
                {
                    Check(!output.ContainsKey("GameData/DifficultyPrototypes.cfg"),
                        scenario.Name + ": stash knob rewrote NPC corpse bonus selection");
                    CheckWorldStashSourceFields(output, scenario.Name,
                        scenario.Difficulty, scenario.Control!, scenario.Value);
                }
                Console.WriteLine("PASS generic CFG fixture: " + scenario.Name);
            }
            finally
            {
                if (Keep is null && Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
        }
        Console.WriteLine("PASS 13 generic NPC/stash writer fixtures; "
            + "source-isolated CFG fields, no game probabilities or PAK claims");
    }

    private static void CheckNpcMedicalSourceFields(
        IReadOnlyDictionary<string, Dictionary<string, string>> output,
        string name, double factor)
    {
        Check(output.TryGetValue("GameData/ItemGeneratorPrototypes.cfg",
                out var emitted), name + ": no NPC consumable clone output");
        Check(!output.ContainsKey("GameData/StashPrototypes.cfg"),
            name + ": standard NPC medical changed unrelated corpse SmartLoot");

        var config = Baseline.Special.NpcMedicalLootProduction;
        var medical = new HashSet<string>(StringComparer.Ordinal)
            { "Bandage", "Medkit", "ArmyMedkit", "AntiRad" };
        var medRows = 0;
        var otherRows = 0;
        foreach (var pool in config.Pools)
        {
            Check(pool.CustomSid == "TCD_" + pool.VanillaSid,
                name + ": NPC consumable clone did not retain original root");
            var root = pool.CustomSid + "/ItemGenerator/";
            Check(emitted!.TryGetValue(pool.CustomSid + "/SID", out var sid)
                && sid == pool.CustomSid, name + ": NPC clone SID missing");
            foreach (var category in pool.TemplateCategories)
            {
                foreach (var item in category.Items)
                {
                    var path = root + category.Node + "/PossibleItems/" + item.Node + "/";
                    var itemSid = item.Fields["ItemPrototypeSID"];
                    var rawChance = item.Fields["Chance"];
                    var expected = medical.Contains(itemSid)
                        ? Math.Clamp(Num(rawChance) * factor, 0, 1)
                        : Num(rawChance);
                    Check(emitted.TryGetValue(path + "Chance", out var chance)
                        && Math.Abs(Num(chance!) - expected) < 1e-7,
                        name + ": NPC item Chance wrong at " + path);
                    Check(emitted.TryGetValue(path + "ItemPrototypeSID", out var actualSid)
                        && actualSid == itemSid,
                        name + ": NPC item identity changed at " + path);
                    foreach (var count in new[] { "MinCount", "MaxCount" })
                        Check(emitted.TryGetValue(path + count, out var value)
                            && value == item.Fields[count],
                            name + ": NPC item quantity changed at " + path + count);
                    if (medical.Contains(itemSid)) medRows++;
                    else otherRows++;
                }
            }
        }
        Check(config.Pools.Count == 6 && medRows == 58 && otherRows == 46,
            name + ": six human consumable pools / 104 rows drifted");

        // Check exactly the 33 audited production-only sub-generator edges,
        // not a broad edit of vanilla shared pool roots.
        var dataDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dataDir is not null && !File.Exists(Path.Combine(dataDir.FullName,
                   "src", "TrueCustomDifficulty", "Data",
                   "npc-medical-production-redirect-manifest.csv")))
            dataDir = dataDir.Parent;
        Check(dataDir is not null, name + ": cannot find original NPC manifest");
        var manifest = File.ReadAllLines(Path.Combine(dataDir!.FullName,
            "src", "TrueCustomDifficulty", "Data",
            "npc-medical-production-redirect-manifest.csv"))
            .Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Check(manifest.Length == 33,
            name + ": named NPC or missing standard-human source redirect");
        foreach (var raw in manifest)
        {
            var fields = raw.Split(',');
            Check(fields.Length == 9 && fields[1] == "GameData/ItemGeneratorPrototypes.cfg"
                && fields[4] == "ItemGeneratorPrototypeSID",
                name + ": invalid original NPC redirect schema");
            var leaf = fields[2] + "/" + fields[3].Replace(".", "/") + "/" + fields[4];
            Check(emitted!.TryGetValue(leaf, out var value) && value == fields[6]
                && fields[6] == "TCD_" + fields[5],
                name + ": missing scoped human NPC redirect " + leaf);
        }
        Check(!emitted!.Keys.Any(k => k.StartsWith("GeneralNPC_Consumables_",
                StringComparison.Ordinal)),
            name + ": original shared human consumable parent was overwritten");
    }

    private static void CheckNpcAmmoSourceFields(
        IReadOnlyDictionary<string, Dictionary<string, string>> output,
        string name, string difficulty, double factor)
    {
        var config = Baseline.Special.NpcCorpseAmmoProduction;
        Check(config.Entries.Count == 43 && config.SourceSid == "NPC_Ammo_Smart"
            && config.TargetSid == "TCD_NPC_Ammo_Smart",
            name + ": authored NPC ammo bonus package drifted");
        Check(!output.ContainsKey("GameData/ItemGeneratorPrototypes.cfg"),
            name + ": corpse bonus ammo touched NPC ItemGenerator weapons/consumables");
        if (difficulty == "Stalker")
        {
            Check(!output.ContainsKey("GameData/StashPrototypes.cfg")
                && !output.ContainsKey("GameData/DifficultyPrototypes.cfg"),
                name + ": inactive Water package still emitted bonus-ammo CFG");
            return;
        }
        // Evaluate both lookups unconditionally: a short-circuited && does
        // not definitely assign the second out variable (CS0165).
        var hasSmart = output.TryGetValue("GameData/StashPrototypes.cfg", out var emitted);
        var hasDifficulty = output.TryGetValue(
            "GameData/DifficultyPrototypes.cfg", out var difficultyCfg);
        Check(hasSmart && hasDifficulty,
            name + ": selected NPC ammo bonus does not emit both source and list");
        Check(difficultyCfg!.TryGetValue(
                "Easy/EconomyDifficulty/CorpseSmartLoot/GeneralNPC", out var choice)
            && choice == "NPC_Medicine_Smart, TCD_NPC_Ammo_Smart",
            name + ": difficulty list lost medical package or ammo clone");
        var root = config.TargetSid + "/";
        Check(emitted!.TryGetValue(root + "SID", out var sid) && sid == config.TargetSid,
            name + ": missing referenced NPC ammo clone SID");
        foreach (var entry in config.Entries)
        {
            var path = root + "ItemGenerators/[" + entry.RankIndex
                + "]/SmartLootParams/PrimaryWeaponParams/[" + entry.GroupIndex
                + "]/Items/[" + entry.ItemIndex + "]/";
            var min = Math.Max(0, Math.Floor(entry.MinCount * factor + .5));
            var max = Math.Max(0, Math.Floor(entry.MaxCount * factor + .5));
            Check(emitted.TryGetValue(path + "MinCount", out var minRaw)
                && Math.Abs(Num(minRaw!) - min) < 1e-7,
                name + ": incorrect original ammo minimum " + path);
            Check(emitted.TryGetValue(path + "MaxCount", out var maxRaw)
                && Math.Abs(Num(maxRaw!) - max) < 1e-7,
                name + ": incorrect original ammo maximum " + path);
        }
        Check(!emitted.Keys.Any(k => k.StartsWith(config.SourceSid + "/", 
                StringComparison.Ordinal)),
            name + ": NPC ammo original SmartLoot was overwritten");
    }

    private static void CheckNpcDifficultyIsolation(
        IReadOnlyDictionary<string, Dictionary<string, string>> output,
        string name, string difficulty, string? corpseChoice)
    {
        if (corpseChoice is null)
        {
            // The ammo variants redirect their package list separately.
            // Other NPC tests must not select a new corpse bonus package.
            if (name.StartsWith("npc-med-", StringComparison.Ordinal)
                || difficulty == "Stalker")
                Check(!output.ContainsKey("GameData/DifficultyPrototypes.cfg"),
                    name + ": medical / inactive ammo edited corpse package list");
            return;
        }
        Check(difficulty == "Novice" && corpseChoice == "",
            name + ": unapproved corpse bonus choice scenario");
        Check(output.TryGetValue("GameData/DifficultyPrototypes.cfg", out var diff)
            && diff!.TryGetValue("Easy/EconomyDifficulty/CorpseSmartLoot",
                out var cleared) && cleared == "",
            name + ": explicit NONE did not clear the original bonus package");
        Check(!diff!.Keys.Any(k => k.StartsWith(
                "Easy/EconomyDifficulty/CorpseSmartLoot/GeneralNPC",
                StringComparison.Ordinal)),
            name + ": stale original corpse bonus list after NONE");
    }

    private static void CheckWorldStashSourceFields(
        IReadOnlyDictionary<string, Dictionary<string, string>> output,
        string name, string difficulty, string control, double value)
    {
        Check(output.TryGetValue("GameData/ItemGeneratorPrototypes.cfg", out var tier),
            name + ": stash category failed to produce ItemGenerator source clone");
        Check(!tier!.Keys.Any(k => k.StartsWith("TCD_GeneralNPC_Consumables_",
                StringComparison.Ordinal)),
            name + ": generic stash knob touched NPC consumables");
        var branches = StashBranchPreviewCatalog.Load(Baseline);
        var expectedSource = control switch
        {
            "loot.stashMixAmmo" => "Ammo",
            "loot.stashMixMedical" => "Medical",
            "loot.stashMixFood" => "Food",
            "loot.stashMixDrinks" => "Drinks",
            "loot.stashMixGrenades" => "Grenades",
            "loot.stashMixCoupons" => "Coupons",
            _ => throw new InvalidDataException(name + ": unsupported generic category")
        };
        var source = expectedSource == "Coupons"
            ? "GamePass_Stash_ItemGenerator_Rare"
            : "GamePass_Stash_ItemGenerator_Cheap";
        var snapshot = new StashEffectiveState(
            new Dictionary<string, double> { [control] = value },
            new[] { control }, Catalog.DifficultyProfiles[difficulty].Sid);
        var items = branches.Where(x => !x.IsSmart && x.Prototype == source)
            .SelectMany(g => g.Items
                .Where(x => StashEffectiveState.ItemBiasId(x.ItemSid) == control)
                .Select(x => (Group: g, Item: x))).ToArray();
        Check(items.Length > 0, name + ": no native source rows for category " + expectedSource);
        foreach (var (group, item) in items)
        {
            var path = "TCD_" + group.Prototype + "/"
                + group.GroupPath.Replace(".", "/") + "/[" + item.Index + "]/";
            var expected = group.EffectiveSelection(Baseline, snapshot, item);
            Check(tier.TryGetValue(path + group.Field, out var actual)
                && Math.Abs(Num(actual!) - Num(expected)) < 1e-7,
                name + ": native tier Chance/Weight mismatch at " + path);
            if (value == 0)
                Check(Num(actual!) == 0,
                    name + ": zero source factor retained positive selection " + path);
        }

        if (control == "loot.stashMixCoupons")
        {
            Check(!output.ContainsKey("GameData/StashPrototypes.cfg"),
                name + ": no world SmartLoot coupon rows are indexed");
            return;
        }
        Check(output.TryGetValue("GameData/StashPrototypes.cfg", out var smart),
            name + ": parallel generic SmartLoot layer not emitted");
        var sourceSmart = branches.Where(g => g.IsSmart)
            .SelectMany(g => g.Items
                .Where(item => StashEffectiveState.SmartItemBiasId(item.ItemSid) == control)
                .Select(item => (Group: g, Item: item)))
            .FirstOrDefault();
        Check(sourceSmart.Group is not null,
            name + ": SmartLoot category has no native source row");
        var key = "TCD_" + sourceSmart.Group!.Prototype + "/"
            + sourceSmart.Group.GroupPath.Replace(".", "/") + "/["
            + sourceSmart.Item!.Index + "]/";
        Check(smart!.TryGetValue(key + sourceSmart.Group.Field, out var smartSelection)
            && Num(smartSelection!) == 0,
            name + ": parallel SmartLoot selection not disabled at " + key);
        foreach (var field in new[] { "MinCount", "MaxCount" })
            Check(smart.TryGetValue(key + field, out var count) && Num(count!) == 0,
                name + ": parallel SmartLoot " + field + " not disabled at " + key);
    }
}
