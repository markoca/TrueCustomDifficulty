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
                var generated = Regex.Matches(itemText, generatorPattern);
                Check(generated.Count >= baseMatched.Count + dlcMatched.Count,
                    "Missing a distinct rank-gated gear generator for a selected physical stash");
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
                    var gearCount = Regex.Matches(body,
                        @"(?m)^\s*ItemPrototypeSID = (Gun\w+|\w+_Neutral_Armor)$").Count;
                    Check(gearCount is 1 or 2,
                        "Extra stash generator must contain exactly one weapon and zero or one armor");
                    Check(Regex.Matches(body, @"(?m)^\s*MinCount = 1$").Count == gearCount
                        && Regex.Matches(body, @"(?m)^\s*MaxCount = 1$").Count == gearCount,
                        "Each added gear category must generate exactly one item");
                    Check(!body.Contains("Gauss", StringComparison.OrdinalIgnoreCase)
                        && !body.Contains("Trophy", StringComparison.OrdinalIgnoreCase)
                        && !body.Contains("RPG", StringComparison.OrdinalIgnoreCase),
                        "Extra stash gear contains disallowed item");
                }
                Console.WriteLine($"PASS Extra Gear {percent}%: {baseMatched.Count} base + {dlcMatched.Count} DLC actors; {generated.Count} rank-specific actor generators");
            }
        }
        finally
        {
            if (Keep is null && Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
