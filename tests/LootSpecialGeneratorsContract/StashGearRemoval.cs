using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    static void VerifyExtraStashGearWeaponRemoval()
    {
        var root = Path.Combine(Keep ?? Path.GetTempPath(), "TCD_Stash_Gear_Removal");
        var enabledPath = Path.Combine(root, "enabled");
        var offPath = Path.Combine(root, "off");
        var conflictPath = Path.Combine(root, "conflict");
        const string source = "GameData/ItemGeneratorPrototypes.cfg";
        try
        {
            var off = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            new CfgPatchWriterService().Write(off, Catalog, null, offPath);
            Check(!File.Exists(OutputPath(offPath, source)),
                "Extra Gear OFF must not patch existing authored weapon slots.");

            var on = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var toggle = on.AllParameters.Single(x => x.Id == "loot.stashGearEnabled");
            Check(toggle.IsToggle && !toggle.CurrentBoolean, "Extra Gear master must be OFF by default.");
            toggle.CurrentBoolean = true;
            // All six new gear selectors are still OFF, but existing ordinary
            // authored weapons must nevertheless disappear from eligible stashes.
            var coverage = on.AllParameters.Single(x => x.Id == "loot.stashGearCoveragePercent");
            Check(coverage.CurrentNumber == 0 && coverage.IsControlEnabled,
                "Enabled Extra Gear must activate a 0-percent coverage control.");

            var result = new CfgPatchWriterService().Write(on, Catalog, null, enabledPath);
            var file = OutputPath(enabledPath, source);
            Check(File.Exists(file) && result.EditCount >= 13,
                "Extra Gear ON must write the original weapon removal BPatch.");
            var text = File.ReadAllText(file);
            var removed = Regex.Matches(text,
                @"^\s*\[0\]\s*:\s*removenode\s*$", RegexOptions.Multiline);
            Check(removed.Count == 13,
                $"Expected 13 exact authored regular weapon removals, got {removed.Count}.");
            var regularStashes = new[]
            {
                "07727ED446A8EB9D0F56B3ABB9542CAD",
                "0CC8DAA64C554EF5C010439FE1CC448B",
                "393A3DB04B0C6906EBF05299C8E3DEEE",
                "4AB2B2CF48D0FC16B2E853BF27922273",
                "837831724DFAE2B9000E369F7D767BA3",
                "8E3157704F88075AAB9F519551CDA57D",
                "91D632284EAEDA16F6CD7098A676611F",
                "A3B04A9949CA27AA3E62FD9F3CD08ECD",
                "A4D6A1EA48DDDEE35883379DFFD93F81",
                "B3F81A794CB50D8DD300F794E6FDAF25",
                "BB1EF91D4CEC1424FF522FBD6DCA3148",
                "C59BE52D481BCA2E1EB1CE816B67EF71",
                "E67E4BD743B8868C0D9715826155DA1D"
            };
            Check(regularStashes.Length == 13 && regularStashes.All(id =>
                Regex.IsMatch(text,
                    @"(?m)^" + @"\s*[^\r\n]*_" + id + @"\s*:\s*struct\.begin\s*\{bpatch\}",
                    RegexOptions.CultureInvariant)),
                "An approved original weapon generator was not patched.");
            Check(!text.Contains("1CBBBE27428A1766B7F911863C044833", StringComparison.Ordinal)
                && !text.Contains("464E8FB14AEEC7B154602AB22BEA1EBC", StringComparison.Ordinal),
                "Reserved Trophy or protected STC Malachite generator must remain untouched.");

            var conflict = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            conflict.AllParameters.Single(x => x.Id == "loot.stashGearEnabled").CurrentBoolean = true;
            conflict.AllParameters.Single(x => x.Id == "loot.worldWeaponShuffle").CurrentBoolean = true;
            var blocked = false;
            try
            {
                new CfgPatchWriterService().Write(conflict, Catalog, null, conflictPath);
            }
            catch (InvalidDataException ex) when
                (ex.Message.Contains("cannot both be enabled", StringComparison.Ordinal))
            {
                blocked = true;
            }
            Check(blocked, "Overlapping World Weapon Shuffle and stash weapon removal must fail closed.");
            Console.WriteLine("PASS Extra Gear: 13 safe authored weapons removed only when enabled; protected entries excluded; conflicting shuffle blocked.");
        }
        finally
        {
            if (Keep is null && Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
