using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    static void VerifyStashGearCoverage()
    {
        const string actorPattern = @"PrototypeSID = TCD_ExtraGear_([0-9A-F]{32})_[NEVM]";
        const string generatorPattern = @"(?m)^TCD_ExtraGear_([0-9A-F]{32})_[NEVM] : struct\.begin";
        var root = Path.Combine(Keep ?? Path.GetTempPath(), "TCD_Stash_Gear_Coverage");
        using var stream = typeof(CfgPatchWriterService).Assembly.GetManifestResourceStream(
            "TrueCustomDifficulty.Data.gear-progression-ranks.csv")
            ?? throw new InvalidDataException("Embedded gear progression catalog absent");
        using var reader = new StreamReader(stream);
        var progression = reader.ReadToEnd().Split('\n',
                StringSplitOptions.RemoveEmptyEntries).Skip(1)
            .Select(line => line.TrimEnd('\r').Split(','))
            .ToDictionary(row => row[0],
                row => (ItemClass: row[1], Tier: int.Parse(row[3][1..])),
                StringComparer.Ordinal);
        Check(progression.Count == 142
            && progression.Values.Count(x => x.ItemClass == "Weapon") == 34
            && progression.Values.Count(x => x.ItemClass == "Armor") == 39
            && progression.Values.Count(x => x.ItemClass == "Artifact") == 69,
            "Gear progression catalog items/counts changed");
        using var attachmentStream = typeof(CfgPatchWriterService).Assembly.GetManifestResourceStream(
            "TrueCustomDifficulty.Data.stash-bonus-attachment-progressions.csv")
            ?? throw new InvalidDataException("Embedded stash attachment pool absent");
        using var attachmentReader = new StreamReader(attachmentStream);
        var attachments = attachmentReader.ReadToEnd().Split('\n',
                StringSplitOptions.RemoveEmptyEntries).Skip(1)
            .Select(row => row.TrimEnd('\r').Split(','))
            .ToDictionary(row => row[3],
                row => (Sid: row[0], Tier: int.Parse(row[2][1..])),
                StringComparer.Ordinal);
        Check(attachments.Count == 19
            && attachments.Values.Count(x => x.Tier == 2) == 8
            && attachments.Values.Count(x => x.Tier == 3) == 7
            && attachments.Values.Count(x => x.Tier == 4) == 4,
            "Attachment progression catalog size/tier mismatch");
        try
        {
            foreach (var percent in new[] { 0, 1, 50, 100 })
            foreach (var dlc in new[] { false, true })
            {
                var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
                vm.AllParameters.Single(x => x.Id == "loot.stashGearEnabled").CurrentBoolean = true;
                var control = vm.AllParameters.Single(
                    x => x.Id == "loot.stashGearCoveragePercent");
                Check(control.IsSlider && control.IsControlEnabled
                    && control.Minimum == 0 && control.Maximum == 100
                    && control.TickFrequency == 1, "Coverage must expose exactly 0-100 by 1");
                control.SnapCurrentNumberToMarker(percent);
                var dir = Path.Combine(root, $"{percent}pct-" + (dlc ? "dlc" : "base"));
                new CfgPatchWriterService().Write(vm, Catalog,
                    dlc ? new GameInstallInfo { HasDlc1 = true } : null, dir);
                var baseActors = OutputPath(dir, "GameData/SpawnActorPrototypes.cfg");
                var dlcActors = OutputPath(dir, "DLCGameData/DLC1/SpawnActorPrototypes.cfg");
                var baseText = File.Exists(baseActors) ? File.ReadAllText(baseActors) : "";
                var dlcText = File.Exists(dlcActors) ? File.ReadAllText(dlcActors) : "";
                var expectedBase = (800 * percent + 50) / 100;
                var expectedDlc = dlc ? (108 * percent + 50) / 100 : 0;
                static HashSet<string> ActorsIn(string source) =>
                    Regex.Matches(source, actorPattern).Select(m => m.Groups[1].Value)
                        .ToHashSet(StringComparer.Ordinal);
                var baseMatched = ActorsIn(baseText);
                var dlcMatched = ActorsIn(dlcText);
                Check(baseMatched.Count == expectedBase && dlcMatched.Count == expectedDlc,
                    $"Physical stash coverage mismatch at {percent}%: base {baseMatched.Count}/{expectedBase}, DLC {dlcMatched.Count}/{expectedDlc}");
                Check(!baseMatched.Contains("1CBBBE27428A1766B7F911863C044833")
                    && !baseMatched.Overlaps(BaseProtectedActorIds.Value),
                    "Reserved Trophy / quest stash was touched by gear coverage");
                var itemConfig = OutputPath(dir, "GameData/ItemGeneratorPrototypes.cfg");
                var itemText = File.ReadAllText(itemConfig);
                Check(!itemText.Contains("TCD_GamePass_Stash_ItemGenerator_CheapNoFresh",
                    StringComparison.Ordinal),
                    "Extra Stash Gear alone must not clone the DLC CheapNoFresh shared pool");
                var generated = Regex.Matches(itemText, generatorPattern);
                // Exact ActorSID, native rank/slot and emitted prototype parity.
                // Unlike a >= count check, this also detects orphan generators.
                VerifyExactGearCfgLinks(percent, dlc, baseText, dlcText, itemText);
                Check(generated.Count == 0 == (percent == 0),
                    "Zero coverage must emit no extra equipment generators");
                var prototypes = Regex.Matches(itemText,
                    @"(?m)^TCD_ExtraGear_[0-9A-F]{32}_[NEVM] : struct\.begin");
                var lines = itemText.Split('\n');
                var generatorBeginnings = lines.Select((value, index) => (value, index))
                    .Where(x => Regex.IsMatch(x.value,
                        @"^TCD_ExtraGear_[0-9A-F]{32}_[NEVM] : struct\.begin"))
                    .Select(x => x.index).ToArray();
                for (var i = 0; i < generatorBeginnings.Length; i++)
                {
                    var from = generatorBeginnings[i];
                    var to = from + 1;
                    while (to < lines.Length && !Regex.IsMatch(lines[to],
                        @"^[^\s].* : struct\.begin"))
                        to++;
                    var body = string.Join("\n", lines[from..to]);
                    var itemSids = Regex.Matches(body,
                        @"(?m)^\s*ItemPrototypeSID = (\S+)$")
                        .Select(m => m.Groups[1].Value).ToArray();
                    var code = Regex.Match(lines[from], @"_([NEVM]) : struct\.begin")
                        .Groups[1].Value;
                    var maxTier = code switch
                    {
                        "N" => 1, "E" => 2, "V" => 3, "M" => 4,
                        _ => throw new InvalidDataException("Unknown native rank")
                    };
                    var attachmentRefs = Regex.Matches(body,
                        @"(?m)^\s*ItemGeneratorPrototypeSID = (\S+)$")
                        .Select(m => m.Groups[1].Value).ToArray();
                    // By default Weapon, Armor and Artifact are each enabled,
                    // so the writer must select TWO distinct categories, one
                    // candidate each. No added item can exceed player rank.
                    var categoryNames = Regex.Matches(body,
                        @"(?m)^\s*Category = EItemGenerationCategory::(\w+)$")
                        .Select(m => m.Groups[1].Value).ToArray();
                    Check(itemSids.Length + attachmentRefs.Length == 2
                        && categoryNames.Length == 2
                        && categoryNames.Distinct(StringComparer.Ordinal).Count() == 2,
                        "Default bonus generator must offer two different item categories");
                    Check(itemSids.All(id => progression.TryGetValue(id, out var item)
                            && item.Tier <= maxTier)
                        && attachmentRefs.All(id => attachments.TryGetValue(id, out var entry)
                            && entry.Tier <= maxTier && maxTier >= 2),
                        "Bonus pool may not select gear above native player rank");
                    Check(Regex.Matches(body, @"(?m)^\s*MinCount = 1$").Count == itemSids.Length
                        && Regex.Matches(body, @"(?m)^\s*MaxCount = 1$").Count == itemSids.Length
                        && Regex.Matches(body, @"(?m)^\s*Chance = 0\.5$").Count == 2,
                        "Both bonus candidates must have native Chance 0.5 and one-item counts");
                    Check(!new[] { "Gauss", "Trophy", "RPG", "Weird", "Quest",
                            "Fake", "Prolog", "CNPP", "Heart" }.Any(word =>
                            body.Contains(word, StringComparison.OrdinalIgnoreCase)),
                        "Forbidden item entered stash bonus generator");
                    if (attachmentRefs.Length > 0)
                        Check(categoryNames.Contains("SubItemGenerator", StringComparer.Ordinal),
                            "Nested attachment generator category missing");
                    foreach (var candidate in itemSids)
                    {
                        var category = progression[candidate].ItemClass;
                        Check(category switch
                        {
                            "Artifact" => categoryNames.Contains("Artifact", StringComparer.Ordinal),
                            "Armor" => categoryNames.Contains("BodyArmor", StringComparer.Ordinal),
                            "Weapon" => categoryNames.Contains("WeaponPrimary", StringComparer.Ordinal)
                                || categoryNames.Contains("WeaponPistol", StringComparer.Ordinal),
                            _ => false
                        }, "Bonus category does not match its audited item type");
                    }
                }
                Console.WriteLine($"PASS Extra Gear {percent}%: {baseMatched.Count} base + {dlcMatched.Count} DLC actors; {generated.Count} rank-specific actor generators");
            }
            // 100%-coverage forced type mixes: every produced candidate
            // must belong to the sole enabled category, independent of DLC.
            foreach (var kind in new[] { "Weapon", "Armor", "Artifact", "Attachment" })
            {
                var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
                vm.AllParameters.Single(x => x.Id == "loot.stashGearEnabled").CurrentBoolean = true;
                vm.AllParameters.Single(x => x.Id == "loot.stashGearCoveragePercent")
                    .SnapCurrentNumberToMarker(100);
                foreach (var other in new[] { "Weapon", "Armor", "Artifact", "Attachment" })
                    vm.AllParameters.Single(x => x.Id == "loot.stashGear" + other + "Weight")
                        .SnapCurrentNumberToMarker(other == kind ? 3 : 0);
                // Verify shared native Chance also at exact 0%, 37%, 100%.
                var desiredChance = kind switch
                {
                    "Weapon" => 0,
                    "Armor" => 37,
                    "Artifact" => 100,
                    "Attachment" => 50,
                    _ => throw new InvalidDataException("Unknown forced gear class")
                };
                var chance = vm.AllParameters.Single(
                    x => x.Id == "loot.stashGearBonusChance");
                Check(chance.IsControlEnabled && chance.IsSlider
                    && chance.Minimum == 0 && chance.Maximum == 100
                    && chance.TickFrequency == 1 && chance.ReferenceNumber == 50,
                    "Shared Extra Gear Chance control has wrong default or slider range");
                chance.SnapCurrentNumberToMarker(desiredChance);
                var dir = Path.Combine(root, "forced-" + kind);
                new CfgPatchWriterService().Write(vm, Catalog, null, dir);
                var output = File.ReadAllText(OutputPath(dir, "GameData/ItemGeneratorPrototypes.cfg"));
                var ids = Regex.Matches(output, @"(?m)^\s*ItemPrototypeSID = (\S+)$")
                    .Select(m => m.Groups[1].Value)
                    .Where(progression.ContainsKey).ToArray();
                var refs = Regex.Matches(output,
                    @"(?m)^\s*ItemGeneratorPrototypeSID = (\S+)$")
                    .Select(m => m.Groups[1].Value).ToArray();
                Check(kind == "Attachment"
                    ? ids.Length == 0 && refs.Length > 100
                        && refs.All(attachments.ContainsKey)
                    : ids.Length >= 800 && refs.Length == 0
                        && ids.All(id => progression[id].ItemClass == kind),
                    "Forced bonus category did not exclude other classes: " + kind);
                var forcedCategories = Regex.Matches(output,
                    @"(?m)^\s*Category = EItemGenerationCategory::(\w+)$").Count;
                Check(forcedCategories == ids.Length + refs.Length,
                    "One enabled bonus class must produce one candidate per generator");
                var actualChances = Regex.Matches(output,
                    @"(?m)^\s*Chance = ([^\r\n]+)$")
                    .Select(m => m.Groups[1].Value).ToArray();
                var expectedChance = (desiredChance / 100.0)
                    .ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                Check(actualChances.Length == ids.Length + refs.Length
                    && actualChances.All(value => value == expectedChance),
                    "Global Chance was not written to every " + kind +
                    " candidate: expected " + expectedChance);
            }
            VerifyEmptyStashGearMix();
        }
        finally
        {
            if (Keep is null && Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
