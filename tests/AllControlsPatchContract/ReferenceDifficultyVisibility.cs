using System.IO;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace AllControlsPatchContract;

// Covers source-backed difficulty availability without adding fake CFG
// parameters or rewriting old presets. Only the Master mechanics are
// source-authored as Master-exclusive; ordinary difficulty-specific CFG
// layers continue to support all four references.
internal static partial class Program
{
    private static void VerifyReferenceDifficultyVisibility()
    {
        var vm = new MainViewModel(Catalog);
        var master = vm.Tabs.Single(tab => tab.Id == "master");
        var masterRoots = new[]
        {
            "advanced.lowHealthMechanics",
            "advanced.masterEffectModifierMechanics",
            "advanced.starvationMechanics",
            "advanced.radiationMechanics",
            "advanced.fatigueMechanics"
        };

        foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
        {
            vm.SelectedReferenceDifficulty = difficulty;
            var available = difficulty == "Master";
            Check(master.IsVisible == available,
                difficulty + ": Master tab visibility differs from reference");
            foreach (var source in master.Parameters)
            {
                Check(source.IsAvailableForReferenceDifficulty == available,
                    difficulty + ": Master member availability leak: " + source.Id);
                if (!available)
                    Check(!source.IsControlVisible && !source.IsControlEnabled,
                        difficulty + ": Master member incorrectly shown or writable: " + source.Id);
            }

            foreach (var id in masterRoots)
            {
                var root = vm.AllParameters.Single(p => p.Id == id);
                Check(root.IsControlVisible == available,
                    difficulty + ": Master root visibility mismatch: " + id);
            }
            var normalCorpse = vm.AllParameters.Single(p => p.Id == "loot.npcCorpseBonus");
            var normalMedical = vm.AllParameters.Single(p => p.Id == "loot.npcMedicalChance");
            Check(normalCorpse.IsControlVisible && normalMedical.IsControlEnabled,
                difficulty + ": normal human NPC loot must stay supported");

            if (available)
            {
                vm.SelectedTab = master;
                Check(ReferenceEquals(vm.SelectedTab, master),
                    "Master reference must retain Master tab selection");
            }
            else
                Check(vm.SelectedTab?.Id != "master",
                    "Hidden Master tab still selected on " + difficulty);
        }

        // Switching away from Master must also dismiss the formerly selected
        // tab and keep the editor on a real visible tab.
        vm.SelectedReferenceDifficulty = "Master";
        vm.SelectedTab = master;
        vm.SelectedReferenceDifficulty = "Veteran";
        Check(!master.IsVisible && vm.SelectedTab?.IsVisible == true,
            "Switching Master -> Veteran left the UI on an unavailable tab");

        // Old presets can contain modified Master-only toggles on a different
        // difficulty. They may stay stored, but cannot silently alter CFG.
        var hiddenBundle = vm.AllParameters.Single(p =>
            p.Id == "advanced.starvationMechanics");
        hiddenBundle.CurrentBoolean = true;
        Check(!hiddenBundle.IsControlEnabled && !hiddenBundle.IsControlVisible,
            "Hidden Master mechanic became active on Veteran");

        var dir = Path.Combine(Path.GetTempPath(),
            "TCD_MasterReferenceVisibility_" + Guid.NewGuid().ToString("N"));
        try
        {
            var written = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
            Check(written.EditCount == 0 && written.ParameterCount == 0,
                "Unavailable Master toggle emitted a Veteran CFG patch");
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        Console.WriteLine("PASS reference-gated Master controls: all four difficulties, tab selection and dormant writer");
    }
}
