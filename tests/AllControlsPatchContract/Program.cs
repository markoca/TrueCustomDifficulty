using System.IO;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace AllControlsPatchContract;

// Black-box acceptance: exercise the actual WPF view models and production
// CfgPatchWriterService for every player-editable native source ID.
// A route name in JSON is not sufficient: Write() must produce a real staged CFG.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();

    // These are catalogued/serializable source audits, not independent UI controls.
    private static readonly HashSet<string> NonUi = new(StringComparer.Ordinal)
    {
        "economy.repairDifficultyCost", "alife.mutantSpawn.generic",
        "survival.drunknessThresholdScale",
        "stamina.sprintDisableThreshold", "stamina.sprintRecoveryDelay",
        "stamina.lowRecoveryThreshold", "stamina.lowRecoveryStrength",
        "stamina.lowRecoveryDuration", "stamina.sprintUnderRunSpeedRegenCoef",
        "stamina.joggingRegenCoef", "stamina.slowRunRegenCoef",
        "stamina.sprintRegenCoef", "stamina.runRegenCoef"
    };

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 0)
                throw new ArgumentException("No arguments; the writer uses temporary output directories.");

            var ids = Catalog.Parameters.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            var tested = ids.Except(NonUi, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Check(ids.Count == 268 && NonUi.IsSubsetOf(ids) && tested.Length == 255,
                "Expected complete 268-source / 255-editable audit scope");

            var executed = 0;
            var failed = new List<string>();
            foreach (var id in tested)
            {
                var success = false;
                var issues = new List<string>();
                // Master enables the Master-mechanics dependencies; Stalker
                // also exercises the alternative difficulty-section layout.
                foreach (var difficulty in new[] { "Master", "Stalker" })
                {
                    foreach (var end in new[] { "maximum", "minimum" })
                    {
                        var path = Path.Combine(Path.GetTempPath(), "TCD_FinalCfgMapping",
                            Guid.NewGuid().ToString("N"));
                        try
                        {
                            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
                            var target = vm.AllParameters.Single(p => p.Id == id);
                            if (!string.IsNullOrWhiteSpace(target.Definition.RequiresToggle))
                            {
                                var parent = vm.AllParameters.Single(p => p.Id == target.Definition.RequiresToggle);
                                parent.CurrentBoolean = true;
                            }
                            if (id == "loot.worldWeaponDensity")
                                vm.AllParameters.Single(p => p.Id == "loot.worldWeaponShuffle")
                                    .CurrentBoolean = true;

                            SetEnd(target, end == "maximum");
                            if (!target.IsControlEnabled || !target.IsValueModifiedFromReference())
                            {
                                issues.Add($"{difficulty}/{end}: no live source modification");
                                continue;
                            }

                            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
                            var cfg = Directory.Exists(path)
                                ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray()
                                : Array.Empty<string>();

                            Check(result.FileCount > 0 && result.EditCount > 0 && cfg.Length > 0,
                                "No staged BPatch/cfg_patch files were generated");

                            foreach (var file in cfg)
                                Check(file.Contains("_patch_", StringComparison.OrdinalIgnoreCase)
                                      || file.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase),
                                    "Staged output is neither BPatch nor an actor removenode CFG: " + file);

                            // Direct source: at least one actual current-difficulty
                            // CFG leaf must be in the emitted text, not just a
                            // patch for a linked/dependency setting.
                            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
                            var sourcePoints = Baseline.EditPoints
                                .Where(p => p.ParameterId == id &&
                                    p.Scope == "baseGame" &&
                                    (string.IsNullOrEmpty(p.DifficultySid) || p.DifficultySid == sid))
                                .ToArray();
                            if (sourcePoints.Length > 0)
                            {
                                var output = string.Join("\n", cfg.Select(File.ReadAllText));
                                Check(sourcePoints.Any(p => output.Contains(
                                    p.Key + " =", StringComparison.Ordinal)),
                                    "Changed direct source has no corresponding CFG key");
                            }
                            executed++;
                            success = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            issues.Add($"{difficulty}/{end}: {ex.Message}");
                        }
                        finally
                        {
                            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
                            catch (IOException) { /* retain temp fixture for debugging */ }
                        }
                    }
                    if (success) break;
                }
                if (!success)
                    failed.Add(id + ": " + string.Join(" | ", issues));
            }
            Check(failed.Count == 0, "Writer probe failures:\n" + string.Join("\n", failed));
            Check(executed == 255, "Not every effective source was executed");
            Console.WriteLine($"PASS: {executed}/255 editable native source IDs produced actual production staged CFG output.");
            Console.WriteLine("NOT VERIFIED: in-game STALKER 2 CFG merge, UI interaction via Wine, or gameplay effects.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static void SetEnd(ParameterViewModel p, bool high)
    {
        if (p.IsToggle)
            p.CurrentBoolean = !p.ReferenceBoolean;
        else if (p.IsChoiceSlider)
        {
            var other = p.ChoiceOptions.FirstOrDefault(o =>
                !string.Equals(o.Value, p.CurrentText, StringComparison.Ordinal));
            if (other is not null)
                p.CurrentText = other.Value;
        }
        else if (p.IsRange)
        {
            // The NPC weapon-condition field is an absolute 0..1 range.
            p.CurrentMinNumber = high ? 0.75 : 0;
            p.CurrentMaxNumber = high ? 0.95 : 0.20;
        }
        else if (p.IsSlider)
            p.CurrentNumber = high ? p.Maximum : p.Minimum;
        else
            throw new InvalidOperationException("Unknown editor source type " + p.Control);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidDataException(message);
    }
}
