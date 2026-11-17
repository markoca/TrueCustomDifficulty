using System.IO;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace AllControlsPatchContract;

// Black-box acceptance: exercise the actual WPF view models and production
// CfgPatchWriterService for every player-editable native source ID.
// A route name in JSON is not sufficient: Write() must produce a real staged CFG.
internal static partial class Program
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
        "stamina.sprintRegenCoef", "stamina.runRegenCoef",
        // Kept in the catalog for source/preset compatibility, but not
        // instantiated in the current ViewModel. Do not manufacture
        // invisible controls merely to satisfy the all-controls probe.
        "environment.diamondHealthDamage", "environment.diamondBleeding",
        "loot.standardStashChanceScale", "loot.rareStashChanceScale",
        // TECHNICIAN stock has no rail and is never written (2026-11-16).
        "economy.traderSells.technician.weapons", "economy.traderSells.technician.attachments",
        "economy.traderSells.technician.ammo", "economy.traderSells.technician.medical",
        "economy.traderSells.technician.food", "economy.traderSells.technician.drinks"
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
            Check(ids.Count == 402 && NonUi.IsSubsetOf(ids) && tested.Length == 379,
                "Expected 402 catalog sources and 379 mounted editable writer probes");

            // The ordinary Master/Stalker corpse package contains no bonus
            // NPC_Ammo_Smart, so that source must be explicitly probed where
            // the real native ammo package is active (Novice).
            VerifyHumanAwarenessWeatherKnobs();
            VerifyHumanAwarenessWeatherLayout();
            VerifyWeatherNoiseMaskingSemantics();
            VerifyReferenceDifficultyVisibility();
            VerifyDormantNpcAmmoNotCounted();
            VerifySingletonCfgPatchAndFileAccounting();
            var executed = 0;
            var failed = new List<string>();
            foreach (var id in tested)
            {
                var success = false;
                var issues = new List<string>();
                // Master enables the Master-mechanics dependencies; Stalker
                // also exercises the alternative difficulty-section layout.
                foreach (var difficulty in id == "loot.npcWeaponAmmoSupply"
                    ? new[] { "Novice" }
                    : new[] { "Master", "Stalker" })
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
                            // A changed gear priority or native bonus Chance has no
                            // physical actor to patch while default coverage is 0%.
                            // Exercise a *real* 1%-coverage source route, not only
                            // a fake writtenParameters marker in the C# writer.
                            if (id.StartsWith("loot.stashGear", StringComparison.Ordinal))
                            {
                                vm.AllParameters.Single(p => p.Id == "loot.stashGearEnabled")
                                    .CurrentBoolean = true;
                                vm.AllParameters.Single(p => p.Id == "loot.stashGearCoveragePercent")
                                    .SnapCurrentNumberToMarker(1.0);
                            }
                            if (!target.IsControlEnabled || !target.IsValueModifiedFromReference())
                            {
                                issues.Add($"{difficulty}/{end}: no live source modification");
                                continue;
                            }

                            var installation = id.StartsWith(TraderSellsPolicy.Prefix, StringComparison.Ordinal)
                                ? new GameInstallInfo { HasDlc1 = true } : null;
                            var result = new CfgPatchWriterService().Write(vm, Catalog, installation, path);
                            var cfg = Directory.Exists(path)
                                ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToArray()
                                : Array.Empty<string>();

                            Check(result.FileCount > 0 && result.EditCount > 0 && cfg.Length > 0,
                                "No staged BPatch/cfg_patch files were generated");
                            Check(result.FileCount == cfg.Length,
                                "Reported CFG FileCount differs from physical staged CFG files: " + id);
                            if (id == "loot.npcWeaponAmmoSupply")
                            {
                                // Require the actual ammo clone and its
                                // difficulty redirect, not just any staged CFG file.
                                var stashPath = Path.Combine(path, "Stalker2", "Content",
                                    "GameLite", "GameData", "StashPrototypes",
                                    "StashPrototypes_patch_TrueCustomDifficulty.cfg");
                                var difficultyPath = Path.Combine(path, "Stalker2", "Content",
                                    "GameLite", "GameData", "DifficultyPrototypes",
                                    "DifficultyPrototypes_patch_TrueCustomDifficulty.cfg");
                                Check(File.Exists(stashPath) && File.Exists(difficultyPath),
                                    "NPC ammo setting has no private stash clone and selected difficulty redirect");
                                var stashCfg = File.ReadAllText(stashPath);
                                var difficultyCfg = File.ReadAllText(difficultyPath);
                                Check(stashCfg.Contains("TCD_NPC_Ammo_Smart : struct.begin",
                                        StringComparison.Ordinal)
                                    && stashCfg.Contains("SID = TCD_NPC_Ammo_Smart",
                                        StringComparison.Ordinal)
                                    && stashCfg.Contains("MinCount =", StringComparison.Ordinal)
                                    && stashCfg.Contains("MaxCount =", StringComparison.Ordinal)
                                    && difficultyCfg.Contains(
                                        "GeneralNPC = NPC_Medicine_Smart, TCD_NPC_Ammo_Smart",
                                        StringComparison.Ordinal)
                                    && result.ParameterCount >= 1,
                                    "Bonus Ammo Drops counted unrelated source output instead of "
                                    + "its NPC_Ammo_Smart clone and active Novice redirect");
                            }

                            // Incremental Stashes controls must emit a physical
                            // generator BPatch, not merely claim a routed value.
                            // Prior 248-source domain tests remain authoritative.
                            if (id.StartsWith("loot.stashGear", StringComparison.Ordinal)
                                || id.StartsWith("loot.stashMix", StringComparison.Ordinal)
                                || id.StartsWith("loot.stashMedical", StringComparison.Ordinal)
                                || id.StartsWith("loot.stashMedkit", StringComparison.Ordinal)
                                || id.StartsWith("loot.stashArmyMedkit", StringComparison.Ordinal)
                                || id is "loot.standardStashChanceScale" or "loot.rareStashChanceScale"
                                || id.StartsWith("loot.mediumStash", StringComparison.Ordinal)
                                || id.StartsWith("loot.expensiveStash", StringComparison.Ordinal))
                            {
                                var generator = Path.Combine(path, "Stalker2", "Content",
                                    "GameLite", "GameData", "ItemGeneratorPrototypes",
                                    "ItemGeneratorPrototypes_patch_TrueCustomDifficulty.cfg");
                                Check(File.Exists(generator),
                                    "Stashes source produced no ItemGeneratorPrototypes BPatch: " + id);
                            }

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
            Check(executed == 344, "Not every mounted editable source was executed");
            Console.WriteLine($"PASS: {executed}/344 mounted editable source IDs produced production staged CFG output.");
            Console.WriteLine("NOT VERIFIED: in-game STALKER 2 CFG merge, UI interaction via Wine, or gameplay effects.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    // Master has no extra corpse package. An edited ammo multiplier stays in
    // the preset, but must NOT contribute to the writer's applied count or
    // produce an NPC_Ammo_Smart clone. Native sleep recovery is unchanged:
    // this probe should produce no files at all.
    private static void VerifyHumanAwarenessWeatherLayout()
    {
        var layoutMethod = typeof(TrueCustomDifficulty.Controls.AggregateVisualControl).GetMethod(
            "HumanAwarenessLayout", System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static);
        Check(layoutMethod is not null, "Human Awareness layout is missing.");

        foreach (var width in new[] { 700.0, 750.0, 850.0, 1024.0 })
        {
            var layout = layoutMethod!.Invoke(null, new object[] { width })!;
            var props = layout.GetType();
            var legend = (System.Windows.Rect)props.GetProperty("Legend")!.GetValue(layout)!;
            var weather = (System.Windows.Rect)props.GetProperty("Weather")!.GetValue(layout)!;
            var height = (double)props.GetProperty("Height")!.GetValue(layout)!;
            var radius = (double)props.GetProperty("Radius")!.GetValue(layout)!;
            var center = (System.Windows.Point)props.GetProperty("Center")!.GetValue(layout)!;
            Check(weather.Left >= legend.Right + 12.0
                  && weather.Right <= width - 12.0
                  && weather.Top >= 35.0
                  && weather.Bottom + 8.0 <= height
                  && weather.Height >= 3.0 * 116.0
                  && center.X + radius + 18.0 < legend.Left,
                $"Human Awareness vertical weather dials overlap radar/legend or clip at {width} DIP.");
        }

        // Narrow fallback retains the right-hand vertical column below the radar.
        var narrow = layoutMethod!.Invoke(null, new object[] { 620.0 })!;
        var type = narrow.GetType();
        var narrowLegend = (System.Windows.Rect)type.GetProperty("Legend")!.GetValue(narrow)!;
        var narrowWeather = (System.Windows.Rect)type.GetProperty("Weather")!.GetValue(narrow)!;
        var narrowHeight = (double)type.GetProperty("Height")!.GetValue(narrow)!;
        Check(narrowWeather.Left >= narrowLegend.Right + 12.0
              && narrowHeight >= narrowWeather.Bottom + 8.0,
            "Narrow Human Awareness weather column must not overlap the sensing legend.");
    }

    private static void VerifyHumanAwarenessWeatherKnobs()
    {
        var vm = new MainViewModel(Catalog);
        var anchor = vm.AllParameters.Single(p => p.Id == HumanCombatPolicy.SightRangeId);
        Check(anchor.IsVisualAggregateAnchor && anchor.IsControlVisible,
            "The Human Awareness radar must remain the only visible aggregate anchor.");

        foreach (var id in new[]
        {
            HumanCombatPolicy.FogVisibilityId,
            HumanCombatPolicy.RainNoiseId,
            HumanCombatPolicy.StormNoiseId
        })
        {
            var parameter = vm.AllParameters.Single(p => p.Id == id);
            Check(HumanCombatPolicy.AwarenessIds.Contains(id)
                  && parameter.VisualAggregateFamily == "HumanAwareness"
                  && parameter.IsVisualAggregateMember
                  && !parameter.IsVisualAggregateAnchor
                  && !parameter.IsControlVisible
                  && parameter.IsControlEnabled,
                $"{id} must stay editable on the Human Awareness board without a duplicate slider card.");
        }
    }

    private static void VerifyWeatherNoiseMaskingSemantics()
    {
        var vm = new MainViewModel(Catalog);
        var rain = vm.AllParameters.Single(p => p.Id == HumanCombatPolicy.RainNoiseId);
        var thunder = vm.AllParameters.Single(p => p.Id == HumanCombatPolicy.StormNoiseId);
        Check(rain.CurrentDisplay == "30%" && thunder.CurrentDisplay == "40%",
            "Rain/thunder noise must show masked sound rather than remaining hearing.");
        Check(rain.VanillaReferenceText.Contains("30%", StringComparison.Ordinal)
              && thunder.VanillaReferenceText.Contains("40%", StringComparison.Ordinal),
            "Weather noise reference readouts must use masking semantics.");
        Check(HumanCombatPolicy.CardText(HumanCombatPolicy.RainNoiseId, 0.7, 0.7) == "rain noise 30%"
              && HumanCombatPolicy.CardText(HumanCombatPolicy.StormNoiseId, 0.6, 0.6) == "thunder noise 40%",
            "Awareness readouts must use the complemented percentages.");
        Check(Math.Abs(rain.ToPresetValue().Number.GetValueOrDefault() - 0.7) < 0.000001
              && Math.Abs(thunder.ToPresetValue().Number.GetValueOrDefault() - 0.6) < 0.000001,
            "Existing presets must retain original source coefficients.");
        rain.SnapCurrentNumberToMarker(0.4);
        Check(rain.CurrentDisplay == "60%"
              && Math.Abs(rain.ToPresetValue().Number.GetValueOrDefault() - 0.4) < 0.000001,
            "Editing noise must only invert its display, not the preset.");
    }

    private static void VerifyDormantNpcAmmoNotCounted()
    {
        var dir = Path.Combine(Path.GetTempPath(),
            "TCD_DormantNpcAmmo_Count_" + Guid.NewGuid().ToString("N"));
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Master" };
            var ammo = vm.AllParameters.Single(p => p.Id == "loot.npcWeaponAmmoSupply");
            ammo.SnapCurrentNumberToMarker(2.0);
            Check(ammo.IsValueModifiedFromReference(),
                "Dormant ammo probe failed to change its real source control");
            Check(string.IsNullOrEmpty(vm.AllParameters.Single(
                p => p.Id == "loot.npcCorpseBonus").CurrentText),
                "Master extra corpse package was expected to be None");
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
            // The writer counts every staged file, including global
            // .cfg_patch_TrueCustomDifficulty outputs without a .cfg suffix.
            var output = Directory.EnumerateFiles(dir, "*",
                SearchOption.AllDirectories).ToArray();
            Check(result.ParameterCount == 0 && result.FileCount == 0
                  && result.EditCount == 0 && output.Length == 0,
                "Dormant Master ammo setting generated unexpected CFG output");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    // Prove the singleton/global output format separately from prototype
    // BPatch files. A test that enumerates only "*.cfg" silently misses
    // CoreVariables.cfg_patch_TrueCustomDifficulty and can miscount output.
    private static void VerifySingletonCfgPatchAndFileAccounting()
    {
        var dir = Path.Combine(Path.GetTempPath(),
            "TCD_SingletonCfgPatch_Count_" + Guid.NewGuid().ToString("N"));
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Master" };
            var money = vm.AllParameters.Single(p => p.Id == "economy.startingMoney");
            money.SnapCurrentNumberToMarker(100.0);
            Check(money.IsControlEnabled && money.IsValueModifiedFromReference(),
                "Starting Money singleton probe did not change a live source");

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
            var output = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).ToArray();
            var singleton = Path.Combine(dir, "Stalker2", "Content", "GameLite",
                "GameData", "CoreVariables.cfg_patch_TrueCustomDifficulty");

            Check(File.Exists(singleton),
                "Starting Money did not emit its native singleton cfg_patch destination");
            Check(File.ReadAllText(singleton).Contains(
                    "PlayerStartingMoney = 100", StringComparison.Ordinal),
                "Starting Money singleton cfg_patch did not contain its actual changed value");
            Check(output.Length == result.FileCount && result.EditCount > 0,
                "Global singleton output was omitted from physical CFG file accounting");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
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
