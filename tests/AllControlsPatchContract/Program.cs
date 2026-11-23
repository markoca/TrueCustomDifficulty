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

    private static void VerifyMedicalHealTimeRoutes()
    {
        var routes = new[]
        {
            ("health.bandageHealTime", "BandageHealing2"),
            ("health.medkitHealTime", "MedkitHealing3"),
            ("health.armyMedkitHealTime", "ArmyMedkitHealing4"),
            ("health.ecoMedkitHealTime", "EcoMedkitHealing4")
        };
        foreach (var (id, prototype) in routes)
        {
            var source = Catalog.Parameters.Single(p => p.Id == id);
            Check(source.ValueKind == "raw"
                && source.Slider is { Min: 1, Max: 10, Step: 1, StepMode: "absolute" }
                && source.Vanilla["All"].GetDouble() == 1
                && source.Targets.Count == 1
                && source.Targets[0].Prototype == prototype
                && source.Targets[0].Path == "Duration"
                && source.Targets[0].Operation == "set",
                $"Medical HEAL TIME source mapping: {id}");

            var points = Baseline.EditPoints.Where(p => p.ParameterId == id).ToArray();
            Check(points.Length == 1
                && points[0].Prototype == prototype
                && points[0].Key == "Duration"
                && points[0].Operation == "set"
                && points[0].SourceRaw == "1.0", // exact 2.1 source spelling
                $"Medical HEAL TIME writer mapping: {id}");
        }
        Check(!Catalog.Parameters.Any(p => p.Id == "health.healTime"),
            "Retired shared HEAL TIME is absent");
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 0)
                throw new ArgumentException("No arguments; the writer uses temporary output directories.");

            var ids = Catalog.Parameters.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            var tested = ids.Except(NonUi, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Check(ids.Count == 456 && NonUi.IsSubsetOf(ids) && tested.Length == 433,
                "Expected 456 catalog sources and 433 mounted editable writer probes");

            // The ordinary Master/Stalker corpse package contains no bonus
            // NPC_Ammo_Smart, so that source must be explicitly probed where
            // the real native ammo package is active (Novice).
            VerifyMedicalHealTimeRoutes();
            VerifyHumanAwarenessWeatherKnobs();
            VerifyHumanAwarenessWeatherLayout();
            VerifyWeatherNoiseMaskingSemantics();
            VerifyReferenceDifficultyVisibility();
            VerifyDormantNpcAmmoNotCounted();
            VerifyNpcSmartLootMasterToggle();
            VerifySingletonCfgPatchAndFileAccounting();
            VerifyPresetRoundTrip();
            VerifyFactoryFineStepPresetValues();
            VerifyFactoryPresetsLoadUnchanged();
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
            Check(executed == tested.Length, $"Not every mounted editable source was executed ({executed}/{tested.Length})");
            Console.WriteLine($"PASS: {executed}/{tested.Length} mounted editable source IDs produced production staged CFG output.");
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

        // A source-knob number must fit inside its own dial, while each
        // NEXT caption stays outside the previous knob's outer tick perimeter.
        var weatherType = typeof(TrueCustomDifficulty.Controls.AggregateVisualControl);
        static double WeatherMetric(Type t, string fieldName)
            => (double)(t.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)?.GetRawConstantValue()
                ?? throw new InvalidOperationException("Missing weather layout metric " + fieldName));
        var rowHeight = WeatherMetric(weatherType, "AwarenessWeatherRowHeight");
        var centerOffset = WeatherMetric(weatherType, "AwarenessWeatherKnobCenterOffset");
        var knobRadius = WeatherMetric(weatherType, "AwarenessWeatherKnobRadius");
        var labelOffset = WeatherMetric(weatherType, "AwarenessWeatherLabelTopOffset");
        const double outerTickExtension = 9.0;
        const double conservativeLabelHeight = 15.0;
        Check(centerOffset - (knobRadius + outerTickExtension)
                  - (labelOffset + conservativeLabelHeight) >= 8.0
              && rowHeight + labelOffset
                  - (centerOffset + knobRadius + outerTickExtension) >= 24.0,
            "A weather caption touches its own dial or the previous weather knob.");

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

    private static void VerifyNpcSmartLootMasterToggle()
    {
        var toggleSource = Catalog.Parameters.Single(p => p.Id == "loot.npcSmartLootEnabled");
        Check(toggleSource.Control == "toggle" && toggleSource.Vanilla["All"].GetBoolean()
            && toggleSource.Targets.Count == 1
            && toggleSource.Targets[0].Path == "EconomyDifficulty.CorpseSmartLoot.GeneralNPC"
            && toggleSource.Targets[0].Operation == "setListEmptyWhenOff",
            "NPC Smart Loot master toggle has no native list destination");
        var dir = Path.Combine(Path.GetTempPath(),
            "TCD_NpcSmartLootMaster_" + Guid.NewGuid().ToString("N"));
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Novice" };
            var master = vm.AllParameters.Single(p => p.Id == toggleSource.Id);
            var packages = vm.AllParameters.Single(p => p.Id == "loot.npcCorpseBonus");
            var bonusAmmo = vm.AllParameters.Single(p => p.Id == "loot.npcWeaponAmmoSupply");
            var stockStashToggle = vm.AllParameters.Single(p => p.Id == "loot.stashSmartLootEnabled");
            Check(packages.CurrentText.Contains("NPC_Ammo_Smart", StringComparison.Ordinal)
                && stockStashToggle.CurrentBoolean,
                "Novice native optional packages or independent stash switch missing");

            bonusAmmo.SnapCurrentNumberToMarker(2.0);
            vm.CorpseSmartLootNeedBoost = 1.5;
            Check(Math.Abs(vm.EffectiveCorpseSmartLootNeedBoost - 1.5) < 0.000001,
                "NEED BOOST must follow its knob without a second enable flag");
            Check(typeof(PresetFile).GetProperty("CorpseSmartLootNeedBoostEnabled") is null,
                "Retired NEED BOOST checkbox must not persist a hidden preset toggle");
            master.CurrentBoolean = false;
            Check(Math.Abs(vm.EffectiveCorpseSmartLootNeedBoost - 1.0) < 0.000001
                && Math.Abs(vm.CorpseSmartLootNeedBoost - 1.5) < 0.000001,
                "SMART LOOT master OFF must suspend boost without losing the knob value");
            Check(!bonusAmmo.IsControlEnabled
                && packages.CurrentText.Contains("NPC_Ammo_Smart", StringComparison.Ordinal),
                "Smart Loot OFF must preserve options while disabling the ammo editor");
            var off = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
            var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).ToArray();
            var difficulty = files.SingleOrDefault(p => p.Contains(
                "DifficultyPrototypes_patch_TrueCustomDifficulty.cfg", StringComparison.Ordinal));
            Check(off.FileCount > 0 && difficulty is not null,
                "Smart Loot OFF did not emit a difficulty CFG");
            var offText = File.ReadAllText(difficulty!);
            Check(offText.Contains("CorpseSmartLoot =", StringComparison.Ordinal)
                && !offText.Contains("GeneralNPC = NPC_Ammo_Smart", StringComparison.Ordinal)
                && !files.Any(p => p.Contains("StashPrototypes_patch_TrueCustomDifficulty.cfg", StringComparison.Ordinal)),
                "Smart Loot OFF failed to remove all corpse bonus packages");
            Check(stockStashToggle.CurrentBoolean,
                "NPC Smart Loot toggle unexpectedly altered stash Smart Loot");

            master.CurrentBoolean = true;
            Check(Math.Abs(vm.EffectiveCorpseSmartLootNeedBoost - 1.5) < 0.000001,
                "SMART LOOT master ON must restore the saved NEED BOOST multiplier");
            Check(bonusAmmo.IsControlEnabled
                && packages.CurrentText.Contains("NPC_Ammo_Smart", StringComparison.Ordinal),
                "Smart Loot ON did not restore the saved bonus package state");
            var on = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
            var staged = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).ToArray();
            Check(on.FileCount > 0
                && staged.Any(p => p.Contains("StashPrototypes_patch_TrueCustomDifficulty.cfg", StringComparison.Ordinal)),
                "Re-enabled bonus ammo did not generate its saved private clone");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
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

    // SAVE -> LOAD must restore every editor control exactly (new controls
    // included), at three difficulties, with every control moved off vanilla.
    private static void VerifyPresetRoundTrip()
    {
        var retired = (IReadOnlySet<string>)typeof(MainViewModel).GetField("RetiredHiddenPresetIds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
        var dir = Path.Combine(Path.GetTempPath(), "TCD_PresetRoundTrip", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var service = new PresetService();
            var mustContain = new[] {
                "combat.mutantSpecialAbilityRate", "combat.mutantMoveRate.bloodsucker.leap",
                "combat.mutantMoveRate.pseudogiant.shockwave", "health.fallDamage",
                "alife.lairPopulation.human", "alife.lairPopulation.mutant", "alife.lairRespawnTime",
                "environment.emissionFrequency", "environment.stormFrequency",
                "environment.emissionWarning", "economy.traderRestockHours",
                "loot.looseArtifactsRemoved" };
            var checkedValues = 0;
            foreach (var (difficulty, high) in new[] { ("Veteran", true), ("Master", false), ("Novice", true) })
            {
                var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
                foreach (var p in vm.AllParameters.Where(p => !retired.Contains(p.Id)))
                    SetEnd(p, high);
                var saved = vm.CreatePresetFile("round-trip");
                var moveKnobs = vm.AllParameters.Count(p => p.Id.StartsWith("combat.mutantMoveRate.", StringComparison.Ordinal));
                Check(moveKnobs == 53 && mustContain.All(saved.Values.ContainsKey)
                      && saved.Values.Count == vm.AllParameters.Count,
                    "Preset save does not contain every editor control (" + saved.Values.Count + "/" + vm.AllParameters.Count + ")");
                var path = Path.Combine(dir, difficulty + ".json");
                service.Save(saved, path);
                var fresh = new MainViewModel(Catalog);
                fresh.ApplyPresetFromPath(path);
                Check(fresh.SelectedReferenceDifficulty == difficulty, "Preset base difficulty did not round-trip: " + difficulty);
                var loaded = fresh.CreatePresetFile("round-trip");
                var diffs = new List<string>();
                foreach (var (id, value) in saved.Values)
                {
                    if (retired.Contains(id)) continue;
                    if (!loaded.Values.TryGetValue(id, out var back) || !SamePresetValue(value, back))
                        diffs.Add(id + ": saved " + Describe(value) + " loaded " + (back is null ? "<missing>" : Describe(back)));
                    checkedValues++;
                }
                Check(diffs.Count == 0, difficulty + " preset round-trip changed controls:\n" + string.Join("\n", diffs.Take(40)));
            }
            Console.WriteLine($"PASS preset SAVE -> LOAD: {checkedValues} control values at 3 difficulties restored exactly (53 mutant move knobs, SPECIAL MOVES, FALL DAMAGE, LAIR, ZONE WEATHER, TRADER RESTOCK, LOOSE ARTIFACTS included)");
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch (IOException) { }
        }
    }

    // Source-native factory role tuning uses 0.05x values on these two sliders.
    // Exercise the actual preset import and serializer, not a JSON-only check;
    // a 0.10x editor grid silently changed 0.85x to 0.8x on load.
    private static void VerifyFactoryFineStepPresetValues()
    {
        var ids = new[] { "combat.npcHealth", "combat.psySpawnCycleDelay" };
        var fineStops = new[] { 0.55, 0.65, 0.75, 0.85, 0.95 };
        foreach (var id in ids)
        {
            var source = Catalog.Parameters.Single(p => p.Id == id);
            Check(source.Slider is { Min: 0.25, Max: 2, Step: 0.05, StepMode: "absolute" },
                id + ": factory fractions must be reachable on the source editor grid");
        }
        var tested = 0;
        foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            foreach (var id in ids)
            {
                var parameter = vm.AllParameters.Single(p => p.Id == id);
                foreach (var value in fineStops)
                {
                    parameter.ApplyPresetValue(new PresetValue { Kind = "slider", Number = value });
                    Check(Math.Abs(parameter.CurrentNumber - value) < 1e-9,
                        $"{difficulty} {id}: importing {value:0.00}x rounded to {parameter.CurrentNumber:0.00}x");
                    var snapshot = vm.CreatePresetFile("factory-step-check");
                    Check(snapshot.Values.TryGetValue(id, out var saved)
                        && saved.Number.HasValue && Math.Abs(saved.Number.Value - value) < 1e-9,
                        $"{difficulty} {id}: save silently changed {value:0.00}x");
                    tested++;
                }
            }
        }
        Console.WriteLine($"PASS factory 0.05x steps: {tested} native slider/preset import-save checks");
    }

    // Every embedded factory preset loads without a silent value change.
    private static void VerifyFactoryPresetsLoadUnchanged()
    {
        var assembly = typeof(MainViewModel).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("TrueCustomDifficulty.Presets.", StringComparison.Ordinal)
                && n.EndsWith(".json", StringComparison.Ordinal)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Check(names.Length == 12, "Expected 12 embedded factory presets, found " + names.Length);
        var dir = Path.Combine(Path.GetTempPath(), "TCD_FactoryPresetLoad", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var problems = new List<string>();
            var checkedValues = 0;
            var healTimeByRole = new Dictionary<string, (int Bandage, int Medkit, int Army, int Eco)>(StringComparer.Ordinal)
            {
                ["Anomaly Runner"] = (6, 7, 8, 8),
                ["Field Medic"] = (5, 6, 7, 7),
                ["Hardcore+"] = (8, 9, 10, 10),
                ["Hardcore"] = (7, 8, 9, 9),
                ["Immersive"] = (6, 7, 8, 8),
                ["Mercenary"] = (5, 7, 7, 8),
                ["Mutant Hunter"] = (6, 7, 8, 8),
                ["Outcast"] = (8, 9, 10, 10),
                ["Scavenger"] = (7, 8, 9, 9),
                ["Smuggler"] = (5, 7, 8, 8),
                ["Tactical"] = (6, 8, 8, 8),
                ["Wanderer"] = (6, 7, 8, 8),
            };
            foreach (var name in names)
            {
                var path = Path.Combine(dir, name);
                using (var resource = assembly.GetManifestResourceStream(name)!)
                using (var file = File.Create(path))
                    resource.CopyTo(file);
                var file0 = new PresetService().Load(path);
                Check(file0.Values.Count == Catalog.Parameters.Count, name + ": factory preset is missing controls");
                // One source-specific signature per role ensures these twelve
                // profiles are genuinely differentiated, not 53 uniform 1x
                // mutant knobs copied from the generic source baseline.
                var signatures = new Dictionary<string, (string Id, double Value)>(StringComparer.Ordinal)
                {
                    ["Anomaly Runner"] = ("combat.mutantMoveRate.poltergeist.throw", 1.75),
                    ["Field Medic"] = ("combat.mutantMoveRate.blinddog.bite", 0.75),
                    ["Hardcore+"] = ("combat.mutantMoveRate.bloodsucker.leap", 3.0),
                    ["Hardcore"] = ("combat.mutantMoveRate.chimera.longLeap", 2.75),
                    ["Immersive"] = ("combat.mutantMoveRate.bloodsucker.leap", 1.5),
                    ["Mercenary"] = ("combat.mutantMoveRate.burer.disarm", 1.75),
                    ["Mutant Hunter"] = ("combat.mutantMoveRate.pseudodog.phantoms", 2.25),
                    ["Outcast"] = ("combat.mutantMoveRate.controller.psy", 1.75),
                    ["Scavenger"] = ("combat.mutantMoveRate.blinddog.runBite", 0.5),
                    ["Smuggler"] = ("combat.mutantMoveRate.controller.psy", 1.5),
                    ["Tactical"] = ("combat.npcFlankRate", 2.25),
                    ["Wanderer"] = ("combat.mutantMoveRate.poltergeist.throw", 1.75)
                };
                Check(signatures.TryGetValue(file0.Name, out var signature)
                    && file0.Values.TryGetValue(signature.Id, out var sourceValue)
                    && sourceValue.Number.HasValue
                    && Math.Abs(sourceValue.Number.Value - signature.Value) < 1e-9,
                    name + ": failed role-specific 2026-11-18 vibe signature");
                Check(file0.Values.Keys.Count(id => id.StartsWith("combat.mutantMoveRate.", StringComparison.Ordinal)) == 53
                    && file0.Values["environment.artifactCount"].Number == 1,
                    name + ": 53 native mutant controls and one-artifact-spawn invariant");
                // Role-accurate scarcity signatures on the same actual shipped files.
                // Checking two independent source endpoints prevents an off-by-one
                // or disabled smart-loot knob from masquerading as successful tuning.
                // Source-derived 2026-11-19 scarcity. Not an estimated total
                // drop percentage; item/tier sources reduce supplies independently.
                var scarcity = new Dictionary<string, (double Corpse, double Stash)>(StringComparer.Ordinal)
                {
                    ["Anomaly Runner"] = (0.10, 0.20),
                    ["Field Medic"] = (0.05, 0.15),
                    ["Hardcore+"] = (0.20, 0.25),
                    ["Hardcore"] = (0.15, 0.20),
                    ["Immersive"] = (0.10, 0.20),
                    ["Mercenary"] = (0.20, 0.20),
                    ["Mutant Hunter"] = (0.10, 0.20),
                    ["Outcast"] = (0.20, 0.25),
                    ["Scavenger"] = (0.05, 0.20),
                    ["Smuggler"] = (0.20, 0.20),
                    ["Tactical"] = (0.15, 0.20),
                    ["Wanderer"] = (0.10, 0.15),
                };
                Check(scarcity.TryGetValue(file0.Name, out var expectedSupply)
                    && Math.Abs(file0.Values["loot.genericCorpseReduction"].Number!.Value - expectedSupply.Corpse) < 1e-9
                    && Math.Abs(file0.Values["loot.genericStashReduction"].Number!.Value - expectedSupply.Stash) < 1e-9,
                    name + ": generic corpse/stash scarcity not aligned with role");
                Check(healTimeByRole.TryGetValue(file0.Name, out var expectedHeal)
                    && file0.Values["health.bandageHealTime"].Number == expectedHeal.Bandage
                    && file0.Values["health.medkitHealTime"].Number == expectedHeal.Medkit
                    && file0.Values["health.armyMedkitHealTime"].Number == expectedHeal.Army
                    && file0.Values["health.ecoMedkitHealTime"].Number == expectedHeal.Eco,
                    name + ": HEAL TIME differs from 5–10 s factory balance");
                // Radiation lethality is deliberately a separate role axis:
                // the 0–25× UI source is usable without touching the CFG stage
                // thresholds, build-up, recovery, or individual cures.
                var radiationByRole = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["Anomaly Runner"] = 16.0,
                    ["Field Medic"] = 5.5,
                    ["Hardcore+"] = 25.0,
                    ["Hardcore"] = 22.5,
                    ["Immersive"] = 7.0,
                    ["Mercenary"] = 10.0,
                    ["Mutant Hunter"] = 12.5,
                    ["Outcast"] = 24.0,
                    ["Scavenger"] = 9.0,
                    ["Smuggler"] = 14.0,
                    ["Tactical"] = 18.0,
                    ["Wanderer"] = 4.5,
                };
                Check(radiationByRole.TryGetValue(file0.Name, out var radiationMultiplier)
                    && file0.Values["survival.radiationDamage"].Number == radiationMultiplier
                    && radiationMultiplier >= 4.5 && radiationMultiplier <= 25,
                    name + ": role-specific radiation HP damage source drift");
                // Bandages treat bleeding for every role. They restore HP only
                // for Field Medic; Scavenger's former 10 HP bonus is retired.
                Check(file0.Values["health.bandageHealing"].Number == (file0.Name == "Field Medic" ? 10 : 0)
                    && file0.Values["health.bandageBleeding"].Number > 0
                    && file0.Values["health.foodHealing"].Boolean == false,
                    name + ": bandage must not heal HP outside Field Medic, without disabling bleeding treatment");
                var roleSourceSignatures = new Dictionary<string, (double WeaponWeight, double MedkitBleed, double StandardAmmo)>(StringComparer.Ordinal)
                {
                    ["Anomaly Runner"] = (0.95, 10, 0.50),
                    ["Field Medic"] = (1.05, 30, 0.50),
                    ["Hardcore+"] = (1.20, 5, 0.25),
                    ["Hardcore"] = (1.15, 5, 0.25),
                    ["Immersive"] = (1.00, 10, 0.50),
                    ["Mercenary"] = (1.05, 10, 0.75),
                    ["Mutant Hunter"] = (1.10, 15, 0.75),
                    ["Outcast"] = (1.20, 5, 0.25),
                    ["Scavenger"] = (1.10, 10, 0.50),
                    ["Smuggler"] = (0.85, 10, 0.50),
                    ["Tactical"] = (1.05, 10, 0.75),
                    ["Wanderer"] = (0.85, 10, 0.50),
                };
                Check(roleSourceSignatures.TryGetValue(file0.Name, out var roleExpected)
                      && file0.Values["equipment.weaponWeight"].Number == roleExpected.WeaponWeight
                      && file0.Values["health.medkitBleeding"].Number == roleExpected.MedkitBleed
                      && file0.Values["loot.mediumStashAmmoQuantity"].Number == roleExpected.StandardAmmo,
                    name + ": role-specific 2026-11-19 equipment/medicine/ammo signature changed");
                // Native loose-loot removals are role policy, never a global
                // removal. Keep artifact fields, quest and vendor sources.
                var worldRemovalByRole = new Dictionary<string, (bool Detectors, bool Armor, bool FixedArtifacts)>(StringComparer.Ordinal)
                {
                    ["Anomaly Runner"] = (false, true, false),
                    ["Field Medic"] = (true, true, true),
                    ["Hardcore+"] = (true, true, true),
                    ["Hardcore"] = (true, true, true),
                    ["Immersive"] = (true, true, false),
                    ["Mercenary"] = (true, true, true),
                    ["Mutant Hunter"] = (true, true, true),
                    ["Outcast"] = (true, true, true),
                    ["Scavenger"] = (true, false, false),
                    ["Smuggler"] = (true, true, false),
                    ["Tactical"] = (true, true, true),
                    ["Wanderer"] = (false, true, false),
                };
                Check(worldRemovalByRole.TryGetValue(file0.Name, out var worldRemoval)
                    && file0.Values["loot.worldDetectors"].Boolean == worldRemoval.Detectors
                    && file0.Values["loot.worldArmorRemoved"].Boolean == worldRemoval.Armor
                    && file0.Values["loot.looseArtifactsRemoved"].Boolean == worldRemoval.FixedArtifacts,
                    name + ": loose detector/armor/fixed artifact source toggles no longer match role");
                // Exact source-native Human Tactics + Awareness values for
                // every factory role. Fog = retained sight, rain/thunder = retained
                // hearing (lower fractions give the player better concealment).
                string[] humanIds =
                {
                    "combat.npcGrenadeRate", "combat.npcSuppressionRate", "combat.npcFlankRate",
                    "combat.npcPushRate", "combat.npcCamperPunishRate", "combat.npcAccuracy",
                    "combat.npcSightRange", "combat.npcHearingRange", "combat.npcAlertMemory",
                    "combat.npcFogVisibility", "combat.npcRainNoise", "combat.npcStormNoise"
                };
                var humanRoleValues = new Dictionary<string, double[]>(StringComparer.Ordinal)
                {
                    ["Anomaly Runner"] = new double[] { 0.75, 1, 1.25, 0.75, 1, 0.95, 1, 1.15, 1.4, 0.55, 0.55, 0.45 },
                    ["Field Medic"] = new double[] { 1, 1.5, 1, 1.5, 1, 0.95, 1.05, 1.05, 1.25, 0.65, 0.6, 0.5 },
                    ["Hardcore+"] = new double[] { 2.5, 2.75, 2.75, 2.5, 2.5, 1.35, 1.45, 1.45, 2.4, 0.85, 0.85, 0.75 },
                    ["Hardcore"] = new double[] { 2, 2.25, 2.25, 2, 2, 1.2, 1.3, 1.3, 1.9, 0.8, 0.8, 0.7 },
                    ["Immersive"] = new double[] { 1, 1.25, 1.25, 1, 1, 1, 1.05, 1.1, 1.5, 0.55, 0.55, 0.45 },
                    ["Mercenary"] = new double[] { 1.25, 2, 2, 1.25, 1.75, 1.1, 1.15, 1.2, 1.65, 0.7, 0.65, 0.55 },
                    ["Mutant Hunter"] = new double[] { 1, 1, 1.25, 1.25, 1, 1, 1.05, 1.3, 1.4, 0.6, 0.6, 0.5 },
                    ["Outcast"] = new double[] { 1.25, 1.25, 1.75, 1.75, 1.5, 1.1, 1.15, 1.3, 2, 0.7, 0.75, 0.65 },
                    ["Scavenger"] = new double[] { 0.75, 0.75, 1.25, 1, 1, 0.9, 0.85, 1, 1.1, 0.55, 0.55, 0.45 },
                    ["Smuggler"] = new double[] { 0.75, 1, 1.5, 1, 1.25, 1, 1.15, 1.35, 1.85, 0.6, 0.55, 0.45 },
                    ["Tactical"] = new double[] { 2, 2.25, 2.25, 1.75, 2, 1.25, 1.3, 1.2, 2.1, 0.8, 0.8, 0.7 },
                    ["Wanderer"] = new double[] { 0.75, 1, 1, 1, 0.75, 0.95, 1, 1, 1.25, 0.55, 0.55, 0.45 },
                };
                Check(humanRoleValues.TryGetValue(file0.Name, out var humanExpected)
                      && humanIds.Length == 12 && humanExpected.Length == humanIds.Length
                      && humanIds.Select((id, index) => Math.Abs(file0.Values[id].Number!.Value - humanExpected[index]) < 1e-9).All(v => v)
                      && file0.Values["combat.npcStormNoise"].Number < file0.Values["combat.npcRainNoise"].Number,
                    name + ": native human tactics / awareness or weather masking drift");
                var stashSmartEnabled = file0.Values["loot.stashSmartLootEnabled"].Boolean == true;
                var stockedChance = file0.Values["loot.stashSmartStockedChance"].Number!.Value;
                var lowChance = file0.Values["loot.stashSmartShortChance"].Number!.Value;
                Check(!stashSmartEnabled || (stockedChance <= 0.30 + 1e-9
                      && lowChance <= 0.65 + 1e-9 && stockedChance <= lowChance
                      && lowChance > 0),
                    name + ": stash Smart Loot remains too generous or inverted");
                Check(file0.Values["loot.npcCorpseBonus"].Text == string.Empty
                      && file0.Values["environment.artifactCount"].Number == 1,
                    name + ": a phantom NPC bonus pack or multi-artifact payout was introduced");
                var vm = new MainViewModel(Catalog);
                vm.ApplyPresetFromPath(path);
                var loaded = vm.CreatePresetFile("factory");
                // Trader Stock odds are preset sidecars rather than catalog
                // parameters. Ensure LOAD / SAVE also carries all role-specific
                // category probabilities; checking the 456 sliders is not enough.
                var expectedSellOdds = file0.TraderSellOdds
                    ?? new Dictionary<string, double>(StringComparer.Ordinal);
                var loadedSellOdds = loaded.TraderSellOdds
                    ?? new Dictionary<string, double>(StringComparer.Ordinal);
                Check(expectedSellOdds.Count >= 20
                      && loadedSellOdds.Count == expectedSellOdds.Count
                      && expectedSellOdds.All(pair =>
                          loadedSellOdds.TryGetValue(pair.Key, out var current)
                          && Math.Abs(pair.Value - current) < 1e-9),
                    name + ": trader stock partial offer odds did not survive load/save");

                foreach (var (id, back) in loaded.Values)
                {
                    if (!file0.Values.TryGetValue(id, out var value)) { problems.Add(name + " " + id + ": missing in file"); continue; }
                    if (!SamePresetValue(value, back))
                        problems.Add(name + " " + id + ": file " + Describe(value) + " loaded " + Describe(back));
                    checkedValues++;
                }
            }
            Check(problems.Count == 0, "Factory presets change on load:\n" + string.Join("\n", problems.Take(40)));
            Console.WriteLine($"PASS 12 factory presets: {checkedValues} control values load unchanged");
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch (IOException) { }
        }
    }

    private static bool SamePresetValue(PresetValue a, PresetValue b)
    {
        static bool Num(double? x, double? y)
            => (x is null && y is null)
            || (x is not null && y is not null && Math.Abs(x.Value - y.Value) < 1e-6);
        return string.Equals(a.Kind, b.Kind, StringComparison.Ordinal)
            && Num(a.Number, b.Number) && Num(a.MinNumber, b.MinNumber) && Num(a.MaxNumber, b.MaxNumber)
            && a.Boolean == b.Boolean
            && string.Equals(a.Text ?? string.Empty, b.Text ?? string.Empty, StringComparison.Ordinal);
    }

    private static string Describe(PresetValue v)
        => v.Kind + "(" + (v.Number?.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
            ?? v.Boolean?.ToString() ?? (v.MinNumber is not null ? v.MinNumber + "–" + v.MaxNumber : v.Text ?? "")) + ")";

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
