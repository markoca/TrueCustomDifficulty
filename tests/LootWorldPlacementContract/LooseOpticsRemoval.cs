using System;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootWorldPlacementContract;

internal static partial class Program
{
    // Real production writer, exact ActorSID manifest, separate base/DLC1 paths.
    private static void VerifyLooseOpticsRemoval()
    {
        var rows = Csv("loose-optics-direct-manifest.csv", 5);
        Check(rows.Count == 17 && rows.Count(r => r[0] == "baseGame") == 15
            && rows.Count(r => r[0] == "dlc1") == 2,
            "Loose optics direct actor inventory drift");
        Check(rows.Count(r => r[4].StartsWith("Binoculars_", StringComparison.Ordinal)) == 13
            && rows.Count(r => r[4].StartsWith("NVG_Gen", StringComparison.Ordinal)) == 4,
            "Loose optics binocular / night-vision identity counts drift");
        Check(rows.All(r => r[2] == "WorldMap_WP"
            && r[1] == (r[0] == "dlc1"
                ? "DLCGameData/DLC1/SpawnActorPrototypes.cfg"
                : "GameData/SpawnActorPrototypes.cfg")
            && r[3].Length == 32
            && r[3].All(Uri.IsHexDigit)),
            "Loose optics must include only exact world actor nodes");
        Check(rows.Select(r => r[3]).Distinct(StringComparer.Ordinal).Count() == 17,
            "Repeated direct loose-optics ActorSID");

        foreach (var dlc in new[] { false, true })
        {
            var root = Path.Combine(Keep ?? Path.GetTempPath(),
                "TCD_Loose_Optics_" + (dlc ? "dlc" : "base"));
            try
            {
                var off = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
                var p = off.AllParameters.Single(x => x.Id == "loot.looseOpticsRemoved");
                Check(p.IsToggle && p.IsControlEnabled && !p.CurrentBoolean,
                    "Loose optics checkbox should be live and Vanilla OFF");
                new CfgPatchWriterService().Write(off, Catalog,
                    dlc ? new GameInstallInfo { HasDlc1 = true } : null, root);
                Check(!Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Any(),
                    "OFF must not remove any world pickup");

                var on = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
                on.AllParameters.Single(x => x.Id == "loot.looseOpticsRemoved").CurrentBoolean = true;
                var result = new CfgPatchWriterService().Write(on, Catalog,
                    dlc ? new GameInstallInfo { HasDlc1 = true } : null, root);
                var output = ReadOutput(root);
                var expected = rows.Where(r => r[0] == "baseGame" || dlc).ToArray();
                Check(result.EditCount == expected.Length && result.FileCount == expected.Length
                    && result.ParameterCount == 1 && output.Count == expected.Length,
                    "Direct binocular/NVG removal edit + file accounting drift");
                var expectedFiles = expected.Select(r => ActorFile(r[1], r[2], r[3]))
                    .ToHashSet(StringComparer.Ordinal);
                Check(output.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expectedFiles),
                    "Writer changed non-allowlisted world actors");
                foreach (var r in expected)
                {
                    var file = ActorFile(r[1], r[2], r[3]);
                    Check(output.TryGetValue(file, out var patch)
                        && patch!.Removed.SetEquals(new[] { r[3] })
                        && patch.Fields.Count == 0,
                        "Unsafe binocular/NVG patch: " + file);
                }
            }
            finally
            {
                if (Keep is null && Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }
        Console.WriteLine("PASS loose optics: OFF unchanged, 15 base and 17 DLC1 direct-removal files; no shared generator changes");
    }
}
