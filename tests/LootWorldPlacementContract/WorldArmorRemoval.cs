using System;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootWorldPlacementContract;

internal static partial class Program
{
    // Exercise the real production writer; never accept synthetic fragments.
    private static void VerifyWorldArmorRemoval()
    {
        var direct = Csv("world-armor-direct-manifest.csv", 6);
        var stash = Csv("world-armor-stash-manifest.csv", 5);
        Check(direct.Count == 94
            && direct.Count(r => r[0] == "baseGame") == 76
            && direct.Count(r => r[0] == "dlc1") == 18
            && stash.Count == 6, "Armor source inventory drifted");
        Check(direct.All(r => r[2] == "WorldMap_WP" &&
            (r[5] == "Armor" || r[5] == "Helmet")
            && !r[4].Contains("X7", StringComparison.OrdinalIgnoreCase)),
            "Quest armor leaked into direct-removal allowlist");
        Check(stash.All(r => r[3] == "Head")
            && !stash.Any(r => r[1].Contains("Rostok_D0D974B342233E90E494C1BE1CB21240")
                || r[1].Contains("Promzona_StorageontheHill_StashGenerator6")),
            "Quest-linked armor leaked into stash-removal allowlist");

        foreach (var hasDlc1 in new[] { false, true })
        {
            var root = Path.Combine(Keep ?? Path.GetTempPath(),
                "TCD_World_Armor_" + (hasDlc1 ? "dlc" : "base"));
            try
            {
                var off = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
                Check(!off.AllParameters.Single(p => p.Id == "loot.worldArmorRemoved").CurrentBoolean,
                    "Armor-removal checkbox should default OFF");
                new CfgPatchWriterService().Write(off, Catalog,
                    hasDlc1 ? new GameInstallInfo { HasDlc1 = true } : null, root);
                Check(!Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Any(),
                    "OFF must not generate armor removal files");

                var on = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
                var checkbox = on.AllParameters.Single(p => p.Id == "loot.worldArmorRemoved");
                Check(checkbox.IsToggle && checkbox.IsControlEnabled,
                    "World armor removal must be an active checkbox");
                checkbox.CurrentBoolean = true;
                var result = new CfgPatchWriterService().Write(on, Catalog,
                    hasDlc1 ? new GameInstallInfo { HasDlc1 = true } : null, root);
                var documents = ReadOutput(root);
                var expectedDirect = direct.Where(r => r[0] == "baseGame" || hasDlc1).ToArray();
                Check(result.EditCount == expectedDirect.Length + stash.Count
                    && result.FileCount == expectedDirect.Length + 1
                    && result.ParameterCount == 1 && documents.Count == result.FileCount,
                    "Unexpected world armor BPatch accounting");

                var expectedFiles = expectedDirect.Select(r => ActorFile(r[1], r[2], r[3]))
                    .ToHashSet(StringComparer.Ordinal);
                foreach (var row in expectedDirect)
                {
                    var key = ActorFile(row[1], row[2], row[3]);
                    Check(documents.TryGetValue(key, out var actor)
                        && actor!.Removed.SetEquals(new[] { row[3] })
                        && actor.Fields.Count == 0,
                        "Direct armor patch touched more than its approved actor: " + key);
                }
                const string itemGen = "GameData/ItemGeneratorPrototypes.cfg";
                var expectedSlots = stash.Select(r => r[1] + "/" + r[4])
                    .ToHashSet(StringComparer.Ordinal);
                Check(documents.TryGetValue(itemGen, out var generator)
                    && generator!.Removed.SetEquals(expectedSlots)
                    && generator.Fields.Count == 0,
                    "Stash armor patch touched more than its six approved items");
                expectedFiles.Add(itemGen);
                Check(documents.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expectedFiles),
                    "Unrelated quest or world loot sources were modified");
            }
            finally
            {
                if (Keep is null && Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }
        Console.WriteLine("PASS world armor removal: OFF untouched; base 76 direct+6 stash, DLC1 +18 direct; quest exclusions.");
    }
}
