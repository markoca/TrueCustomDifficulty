using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace SurvivalPatchContract;

// Real production WPF ViewModel -> CfgPatchWriterService -> parsed BPatch.
// This DOES NOT load UE, prove original source ZIPs or test gameplay.
internal static class Program
{
    // All four normal food effects have one exact signed native source pair.
    private static void FoodSatietySourceContract()
    {
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        foreach (var (id, sid, native) in new[]
        {
            ("survival.breadSatiety", "BreadSatiety2", 20.0),
            ("survival.sausageSatiety", "SausageSatiety2", 30.0),
            ("survival.cannedFoodSatiety", "CannedSatiety3", 50.0),
            ("survival.milkSatiety", "MilkSatiety4", 100.0)
        })
        {
            var source = vm.AllParameters.Single(p => p.Id == id);
            var points = Baseline.EditPoints.Where(p => p.ParameterId == id).ToArray();
            Check(source.Group == "Hunger" && !source.IsControlVisible
                  && source.IsVisualAggregateMember && source.VisualAggregateFamily == "HungerLadder"
                  && source.Minimum == 0 && source.Maximum == Math.Min(native * 2, 100.0)
                  && source.TickFrequency == 5 && source.ReferenceNumber == native,
                "Food Hunger Relief is not an independent native HungerLadder child: " + id);
            Check(points.Length == 2
                  && points.Select(p => p.Key).ToHashSet(StringComparer.Ordinal)
                      .SetEquals(new[] { "ValueMin", "ValueMax" })
                  && points.All(p => p.Prototype == sid
                       && p.File == "GameData/EffectPrototypes.cfg"
                       && p.Operation == "negateSet"
                       && p.SourceRaw == "-" + native.ToString("0", CultureInfo.InvariantCulture)),
                "Food Hunger Relief writer pair drifted: " + id);
            source.CurrentNumber = Math.Min(native + 5, source.Maximum);
            Check(source.CurrentDisplay == source.CurrentNumber.ToString("0", CultureInfo.InvariantCulture) + " pts",
                "Food player-facing relief value or units drifted: " + id);
            source.CurrentNumber = 900;
            Check(Math.Abs(source.CurrentNumber - source.Maximum) < 0.000001,
                "Food upper clamp allowed an unreachable hunger meter value: " + id);
            source.CurrentNumber = -900;
            Check(source.CurrentNumber == 0,
                "Food lower clamp allowed negative player-facing relief: " + id);
            source.ResetToReference();
            Check(Math.Abs(source.CurrentNumber - native) < 0.000001,
                "Food reset lost original satiety: " + id);
        }
        var foodOrder = vm.Tabs.Single(t => t.Id == "survival").Parameters
            .Where(p => p.Id is "survival.breadSatiety" or "survival.sausageSatiety"
                or "survival.cannedFoodSatiety" or "survival.milkSatiety")
            .Select(p => p.Id).ToArray();
        Check(foodOrder.SequenceEqual(new[]
            { "survival.breadSatiety", "survival.sausageSatiety",
              "survival.cannedFoodSatiety", "survival.milkSatiety" }),
            "HungerLadder food sources lost their locked Bread/Sausage/Canned/Milk order");
        _passed++;
    }

    // Zero, max-relevant and mixed per-food edits must produce exact isolated
    // signed ValueMin/ValueMax leaves through the real Windows WPF writer.
    private static void FoodSatietyBoundaryWriterContract()
    {
        foreach (var (id, native, max) in new[]
        {
            ("survival.breadSatiety", 20.0, 40.0),
            ("survival.sausageSatiety", 30.0, 60.0),
            ("survival.cannedFoodSatiety", 50.0, 100.0),
            ("survival.milkSatiety", 100.0, 100.0)
        })
        {
            foreach (var value in new[] { 0.0, max })
            {
                if (Math.Abs(value - native) < 0.000001)
                    continue; // milk at 100 is vanilla, so produces no food patch.
                var name = "FoodSatiety-" + id.Replace('.', '-') + "-" + value.ToString("0", CultureInfo.InvariantCulture);
                var directory = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
                try
                {
                    var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
                    var parameter = vm.AllParameters.Single(p => p.Id == id);
                    parameter.CurrentNumber = value;
                    Check(parameter.IsValueModifiedFromReference()
                          && Math.Abs(parameter.CurrentNumber - value) < 0.000001,
                        name + ": native boundary did not remain editable");
                    CheckWriter(vm, new HashSet<string>(StringComparer.Ordinal) { id }, directory, name);
                    _passed++;
                }
                finally
                {
                    if (_keep is null && Directory.Exists(directory))
                        Directory.Delete(directory, recursive: true);
                }
            }
        }
    }

    // Verify both independent Master conditional knobs remain real source-
    // backed parameters and retain their pre-redesign writer/dependency data.
    private static void MasterPenaltyKnobSourceContract()
    {
        foreach (var (id, group, toggle, family, reference) in new[]
        {
            ("survival.radiationStaminaDrainPenalty", "Radiation",
                "advanced.radiationMechanics", "MasterRadiationPenalty", 0.5),
            ("health.masterLowHealthPenalty", "Low Health",
                "advanced.lowHealthMechanics", "MasterLowHealth", 0.7)
        })
        {
            var definition = Catalog.Parameters.Single(p => p.Id == id);
            Check(definition.Group == group && definition.Tab == "master",
                $"Master knob source moved to wrong section: {id}");
            Check(definition.RequiresToggle == toggle,
                $"Master knob source lost its own mechanics gate: {id}");
            Check(definition.Slider is { Min: 0.0, Max: 1.0, Step: 0.01 },
                $"Master knob's native 0..1 / 0.01 stepping changed: {id}");
            Check(definition.Targets.Count > 0,
                $"Master knob source has no BPatch mapping: {id}");
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Master" };
            var source = vm.AllParameters.Single(p => p.Id == id);
            Check(source.VisualAggregateFamily == family
                  && !source.IsControlVisible,
                $"Master knob must remain a child of its original visual family: {id}");
            Check(Math.Abs(source.ReferenceNumber - reference) < 0.000001,
                $"Master knob baseline changed: {id}");
        }
    }

    // Source-native Master knob limits, including old presets beyond the
    // newly approved upper bounds. A real HP source must step in 0.5 HP,
    // not 0.01x relative-to-vanilla increments.
    private static void StarvationKnobRangeContract()
    {
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Master" };
        foreach (var (id, minimum, maximum, step, vanilla) in new[]
        {
            ("survival.starvationDamage", 0.0, 10.0, 0.5, 4.0),
            ("survival.starvationDamageInterval", 1.0, 10.0, 1.0, 10.0)
        })
        {
            var definition = Catalog.Parameters.Single(p => p.Id == id);
            var source = vm.AllParameters.Single(p => p.Id == id);
            Check(definition.Slider is not null
                  && definition.Slider.StepMode == "absolute"
                  && Math.Abs(source.Minimum - minimum) < 0.000001
                  && Math.Abs(source.Maximum - maximum) < 0.000001
                  && Math.Abs(source.TickFrequency - step) < 0.000001
                  && Math.Abs(source.ReferenceNumber - vanilla) < 0.000001
                  && source.VisualAggregateFamily == "StarvationTimeline"
                  && source.IsControlEnabled,
                "Starvation source-native knob limits, steps or ownership drifted: " + id);

            source.CurrentNumber = maximum + 100.0;
            Check(Math.Abs(source.CurrentNumber - maximum) < 0.000001,
                "Starvation knob cannot clamp above the upper bound: " + id);
            source.ApplyPresetValue(new PresetValue
                { Kind = "slider", Number = maximum + 100.0 });
            Check(Math.Abs(source.CurrentNumber - maximum) < 0.000001,
                "Old starvation preset did not clamp to the new maximum: " + id);
            source.CurrentNumber = minimum - 100.0;
            Check(Math.Abs(source.CurrentNumber - minimum) < 0.000001,
                "Starvation knob cannot reach the lower bound: " + id);
            source.CurrentNumber = minimum + step * 0.6;
            Check(Math.Abs(source.CurrentNumber - (minimum + step)) < 0.000001,
                "Starvation knob did not snap to the real HP/second step: " + id);
            source.ResetToReference();
            Check(Math.Abs(source.CurrentNumber - vanilla) < 0.000001,
                "Starvation knob reset lost its vanilla baseline: " + id);
        }
        _passed++;
    }

    // UI source-value regression for the shared Core Health/Core Stamina
    // primary ruler look. Styling must not change gameplay source semantics.
    private static void NativeHungerFatiguePrimarySourceContract()
    {
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        foreach (var (id, family, original, min, max, step) in new[]
        {
            ("survival.hungerRate", "HungerLadder", 0.015, 0.0, 0.030, 0.001),
            ("survival.wellFedStaminaRegen", "HungerLadder", 0.20, 0.0, 0.40, 0.01),
            ("survival.sleepinessRate", "FatigueCurve", 0.010, 0.0, 0.020, 0.001),
            ("survival.energyDrinkFatigueRelief", "FatigueCurve", 1.0, 0.0, 2.0, 0.01)
        })
        {
            var definition = Catalog.Parameters.Single(p => p.Id == id);
            var source = vm.AllParameters.Single(p => p.Id == id);
            Check(source.VisualAggregateFamily == family
                && Math.Abs(source.ReferenceNumber - original) < 0.000001
                && Math.Abs(source.Minimum - min) < 0.000001
                && Math.Abs(source.Maximum - max) < 0.000001
                && Math.Abs(source.TickFrequency - step) < 0.000001
                && definition.Targets.Count > 0,
                "Hunger/Fatigue primary CFG source or native ruler range drifted: " + id);
        }
        _passed++;
    }

    private static void SleepHealthDefaultContract()
    {
        var definition = Catalog.Parameters.Single(p => p.Id == "survival.sleepHealthRestore");
        Check(definition.Default.Mode == "fixedVanilla"
              && definition.Slider is { Min: 0.0, Max: 600.0, Step: 5.0 },
            "Sleep HP must default to native 300 HP on a 0..600 HP slider");
        Check(Math.Abs(definition.Vanilla["All"].GetDouble() - 300.0) < 0.000001,
            "Sleep HP source reference changed");
        foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var source = vm.AllParameters.Single(p => p.Id == definition.Id);
            Check(Math.Abs(source.CurrentNumber - 300.0) < 0.000001
                  && Math.Abs(source.ReferenceNumber - 300.0) < 0.000001
                  && !source.IsValueModifiedFromReference(),
                "Sleep editor must start source-neutral at 300 HP");
            source.ApplyPresetValue(new PresetValue { Kind = "slider", Number = 300.0 });
            Check(Math.Abs(source.CurrentNumber - 300.0) < 0.000001,
                "Existing 300-HP preset value must remain exactly 300 HP");
            source.ApplyPresetValue(new PresetValue { Kind = "slider", Number = 500.0 });
            Check(Math.Abs(source.CurrentNumber - 500.0) < 0.000001
                  && source.IsValueModifiedFromReference(),
                "Custom 500-HP preset must survive the expanded range");
            source.ApplyPresetValue(new PresetValue { Kind = "slider", Number = 700.0 });
            Check(Math.Abs(source.CurrentNumber - 600.0) < 0.000001,
                "Preset values beyond 600 HP must clamp");
            source.ResetToReference();
            Check(Math.Abs(source.CurrentNumber - 300.0) < 0.000001
                  && !source.IsValueModifiedFromReference(),
                "Sleep reset must restore unchanged 300 HP");
            source.SnapCurrentNumberToMarker(0.0);
            Check(Math.Abs(source.CurrentNumber) < 0.000001,
                "Sleep editor cannot select zero HP");
            source.SnapCurrentNumberToMarker(600.0);
            Check(Math.Abs(source.CurrentNumber - 600.0) < 0.000001,
                "Sleep editor cannot select 600 HP");
        }
        _passed++;
    }

    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    private static string? _keep;
    private static int _passed;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0)
            {
                if (args.Length != 2 || args[0] != "--out")
                    throw new ArgumentException("Usage: SurvivalPatchContract [--out PATH]");
                _keep = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(_keep);
            }

            var cohort = Catalog.Parameters
                .Where(p => p.Id.StartsWith("survival.", StringComparison.Ordinal))
                .OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
            Check(cohort.Length == 68, "Survival catalog no longer contains exactly 68 parameters");

            FoodSatietySourceContract();
            FoodSatietyBoundaryWriterContract();
            MasterPenaltyKnobSourceContract();
            StarvationKnobRangeContract();
            NativeHungerFatiguePrimarySourceContract();
            SleepHealthDefaultContract();

            var vm = new MainViewModel(Catalog);
            var loaded = vm.AllParameters.Select(p => p.Id)
                .ToHashSet(StringComparer.Ordinal);
            var unmounted = cohort.Where(p => !loaded.Contains(p.Id))
                .Select(p => p.Id).ToArray();
            Check(unmounted.SequenceEqual(new[] { "survival.drunknessThresholdScale" }),
                "Survival catalog-to-live-VM exclusions changed: "
                + string.Join(", ", unmounted));
            var live = cohort.Where(p => loaded.Contains(p.Id)).ToArray();
            Check(live.Length == 67, "Expected 67 live Survival source-backed parameters");

            var points = Baseline.EditPoints
                .Where(p => p.ParameterId.StartsWith("survival.", StringComparison.Ordinal))
                .ToArray();
            Check(points.Length == 153, "Survival manifest no longer contains 153 source edit points");
            Check(points.All(p => p.Scope == "baseGame"), "Survival source unexpectedly targets DLC");
            Check(points.Select(p => p.ParameterId).Distinct().Count() == 68,
                "Survival manifest-to-catalog parameter inventory differs");
            Check(points.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal)
                      .SetEquals(cohort.Select(p => p.Id)),
                "Survival catalog-to-manifest parameter mapping differs");

            foreach (var def in live)
            {
                var difficulty = string.IsNullOrEmpty(def.RequiresToggle) ? "Veteran" : "Master";
                Test(def.Id, difficulty);
            }
            // Difficulty-sensitive fields: Easy/Medium/Hard/Stalker SID mapping.
            foreach (var difficulty in new[] { "Novice", "Stalker", "Master" })
            {
                Test("survival.radiationAccumulation", difficulty);
                Test("survival.bleedingSeverity", difficulty);
            }
            foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
                TestVanilla(difficulty);

            foreach (var id in new[] { "survival.starvationStaminaDrain",
                "survival.starvationDamage", "survival.severeFatigueRecoil",
                "survival.radiationStaminaDrainPenalty" })
                TestDisabled(id);

            TestCombined("Veteran-Consumable-Overlap", "Veteran", new[]
            {
                ("survival.waterInstantStaminaRestore", 0.44),
                ("survival.energyDrinkInstantStaminaRestore", 0.61),
                ("survival.energyDrinkEffectDuration", 1.3),
                ("survival.energyDrinkStaminaRecoveryStrength", 1.25),
                ("survival.energyDrinkFatigueRelief", 1.4)
            });
            TestIndependentInstantRefill();
            TestIndependentWaterStrengths();
            TestCombined("Veteran-Independent-Water-Strengths", "Veteran", new[]
            {
                ("survival.waterInstantStaminaRestore", 0.44),
                ("survival.waterStaminaEffectStrength", 1.40),
                ("survival.waterStaminaCostReduction", 0.75)
            });
            TestCombined("Veteran-Drink-Independent-Durations", "Veteran", new[]
            {
                ("survival.waterEffectDuration", 1.5),
                ("survival.waterRegenDuration", 20.0),
                ("survival.energyDrinkEffectDuration", 1.5),
                ("survival.energyFullCostDuration", 30.0),
                ("survival.energyPartialCostDuration", 40.0)
            });
            TestShortDurationBounds("survival.waterEffectDuration",
                new[] { "survival.waterRegenDuration" }, 30.0, 0.8, 24.0);
            TestShortDurationBounds("survival.energyDrinkEffectDuration",
                new[] { "survival.energyFullCostDuration", "survival.energyPartialCostDuration" },
                45.0, 0.75, 33.75);
            TestDrinkDurationOverride();
            TestMedicineTargets();
            // 0x is a real gameplay setting: validate emitted ValueMin and ValueMax = 0.
            TestCombined("Veteran-Sleep-Zero-HP", "Veteran", new[]
            {
                ("survival.sleepHealthRestore", 0.0)
            });
            TestCombined("Veteran-Medicine-Independent-Effects", "Veteran", new[]
            {
                ("survival.vincaBleedRecovery", 18.0),
                ("survival.vincaDuration", 265.0),
                ("survival.herculesCarryBonus", 35.0),
                ("survival.herculesDuration", 470.0),
                ("survival.psiBlockRecovery", 16.0),
                ("survival.psiBlockDuration", 135.0)
            });

            TestCombined("Veteran-Four-Normal-Food-Effects", "Veteran", new[]
            {
                ("survival.breadSatiety", 35.0),
                ("survival.sausageSatiety", 10.0),
                ("survival.cannedFoodSatiety", 85.0),
                ("survival.milkSatiety", 75.0)
            });
            TestCombined("Veteran-Hunger-Fatigue", "Veteran", new[]
            {
                ("survival.hungerMediumStaminaRegen", 0.43),
                ("survival.hungerStrongStaminaRegen", 0.8),
                ("survival.fatigueLowAimPenalty", 0.2),
                ("survival.fatigueMediumAimPenalty", 0.3),
                ("survival.fatigueStrongAimPenalty", 0.45)
            });
            TestCombined("Master-Mechanics-Shared-Prototypes", "Master", new[]
            {
                ("survival.starvationDamage", 4.5),
                ("survival.starvationDamageInterval", 9.0),
                ("survival.severeFatigueRecoil", 0.65),
                ("survival.radiationStaminaDrainPenalty", 0.6)
            });
            // Writer uses the exact 0 HP / 10 HP and 1 s / 9 s CFG bounds
            // without leaking changes into unrelated Master effect prototypes.
            TestCombined("Master-Starvation-Minimums", "Master", new[]
            {
                ("survival.starvationDamage", 0.0),
                ("survival.starvationDamageInterval", 1.0)
            });
            TestCombined("Master-Starvation-Max-Damage", "Master", new[]
            {
                ("survival.starvationDamage", 10.0),
                ("survival.starvationDamageInterval", 9.0)
            });


            // Paired/triad native-slider constraints must emit only actually
            // cascaded CFG leaves. The actively moved slider takes priority.
            var linked = new[]
            {
                new LinkedCase("bleeding-up", "survival.bleedingLowDamage", 1.8,
                    new[] { E("survival.bleedingLowDamage",1.8), E("survival.bleedingMediumDamage",1.8) }),
                new LinkedCase("bleeding-down", "survival.bleedingStrongDamage", 0.5,
                    new[] { E("survival.bleedingLowDamage",0.5), E("survival.bleedingMediumDamage",0.5), E("survival.bleedingStrongDamage",0.5) }),
                new LinkedCase("fatigue-aim-up", "survival.fatigueLowAimPenalty", 0.3,
                    new[] { E("survival.fatigueLowAimPenalty",0.3), E("survival.fatigueMediumAimPenalty",0.3) }),
                new LinkedCase("fatigue-aim-down", "survival.fatigueStrongAimPenalty", 0.1,
                    new[] { E("survival.fatigueLowAimPenalty",0.1), E("survival.fatigueMediumAimPenalty",0.1), E("survival.fatigueStrongAimPenalty",0.1) }),
                new LinkedCase("fatigue-reload-up", "survival.fatigueMediumReloadPenalty", 0.3,
                    new[] { E("survival.fatigueMediumReloadPenalty",0.3), E("survival.fatigueStrongReloadPenalty",0.3) }),
                new LinkedCase("fatigue-reload-down", "survival.fatigueStrongReloadPenalty", 0.1,
                    new[] { E("survival.fatigueMediumReloadPenalty",0.1), E("survival.fatigueStrongReloadPenalty",0.1) }),
                new LinkedCase("sleep-stages-up", "survival.sleepLowThreshold", 90,
                    new[] { E("survival.sleepLowThreshold",90), E("survival.sleepMediumThreshold",90), E("survival.sleepStrongThreshold",90) }),
                new LinkedCase("sleep-stages-down", "survival.sleepStrongThreshold", 10,
                    new[] { E("survival.sleepLowThreshold",10), E("survival.sleepMediumThreshold",10), E("survival.sleepStrongThreshold",10) }),
                new LinkedCase("hunger-stages-up", "survival.hungerLowThreshold", 90,
                    new[] { E("survival.hungerLowThreshold",90), E("survival.hungerMediumThreshold",90), E("survival.hungerStrongThreshold",90) }),
                new LinkedCase("hunger-stages-down", "survival.hungerStrongThreshold", 5,
                    new[] { E("survival.hungerLowThreshold",5), E("survival.hungerMediumThreshold",5), E("survival.hungerStrongThreshold",5) }),
                new LinkedCase("bleeding-stages-up", "survival.bleedingMediumThreshold", 80,
                    new[] { E("survival.bleedingMediumThreshold",80), E("survival.bleedingStrongThreshold",80) }),
                new LinkedCase("bleeding-stages-down", "survival.bleedingStrongThreshold", 20,
                    new[] { E("survival.bleedingMediumThreshold",20), E("survival.bleedingStrongThreshold",20) }),
                new LinkedCase("hunger-recovery-down", "survival.hungerStrongStaminaRegen", 0.2,
                    new[] { E("survival.hungerMediumStaminaRegen",0.2), E("survival.hungerStrongStaminaRegen",0.2) })
            };
            Check(Catalog.SliderConstraints.Count(c =>
                c.ParameterIds.Any(id => id.StartsWith("survival.", StringComparison.Ordinal))) == 7,
                "Unexpected Survival linked-slider catalog count");
            foreach (var scenario in linked)
                TestLinked(scenario);

            // A disabled Master child retains its edited value in memory but
            // emits nothing until the parent is re-enabled. Existing 3
            // Veteran-disabled cases cover the inverse transition.
            foreach (var (parent, child) in new[]
            {
                ("advanced.starvationMechanics", "survival.starvationStaminaDrain"),
                ("advanced.starvationMechanics", "survival.starvationDamage"),
                ("advanced.radiationMechanics", "survival.radiationStaminaDrainPenalty"),
                ("advanced.fatigueMechanics", "survival.severeFatigueRecoil")
            })
                TestReenabled(parent, child);
            Console.WriteLine("SOURCE ONLY (no production VM): survival.drunknessThresholdScale");
            Console.WriteLine($"PASS: {_passed} Survival production BPatch writer scenarios.");
            Console.WriteLine("NOT VERIFIED: original archived CFG values, UE runtime, effects in game.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    // One editor-only flag grants every real effect the same 270-second
    // ceiling without synchronizing source values or inventing a CFG boolean.
    // Every medicine magnitude edits both deterministic Value bounds, and
    // Hercules keeps its two separate effect SIDs synchronized.
    private static void TestMedicineTargets()
    {
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        var loaded = vm.AllParameters.ToDictionary(p => p.Id, StringComparer.Ordinal);
        foreach (var (id, count, vanilla, playerValue) in new[]
        {
            ("survival.vincaBleedRecovery", 2, 10.0, "+10 bleed pts"),
            ("survival.vincaDuration", 1, 180.0, "180 seconds"),
            ("survival.herculesCarryBonus", 4, 20.0, "+20 kg carry"),
            ("survival.herculesDuration", 2, 300.0, "300 seconds"),
            ("survival.psiBlockRecovery", 2, 10.0, "+10 psi pts"),
            ("survival.psiBlockDuration", 1, 60.0, "60 seconds")
        })
        {
            var source = Catalog.Parameters.Single(p => p.Id == id);
            var points = Baseline.EditPoints.Where(p => p.ParameterId == id).ToArray();
            // Regression for the empty Medicine Effects screenshot: hidden
            // aggregate parameters MUST exist in the live VM, not only JSON.
            Check(loaded.TryGetValue(id, out var live)
                && live.VisualAggregateFamily == "ConsumableMixer"
                && !live.IsControlVisible
                && Math.Abs(live.ReferenceNumber - vanilla) < 0.000001,
                "Medicine source not mounted in live ConsumableMixer: " + id);
            if (id.EndsWith("Recovery", StringComparison.Ordinal)
                || id.EndsWith("CarryBonus", StringComparison.Ordinal))
                Check(live.CurrentDisplay == playerValue,
                    "Medicine player-facing units are not source-native: " + id);
            Check(source.Tab == "survival" && source.Group == "Consumable Effects"
                && !source.ExposeInUi && points.Length == count
                && points.All(p => p.Operation == "set"
                    && p.File == "GameData/EffectPrototypes.cfg"
                    && Math.Abs(ParseNumber(p.SourceRaw) - vanilla) < 0.000001),
                "Medicine source mapping drifted: " + id);
            Check(points.Select(p => p.Prototype + "." + p.Key).Distinct().Count() == count,
                "Duplicate medicine CFG target for " + id);
            Check(source.Targets.Select(t => t.Prototype).Distinct().Count()
                == (id.StartsWith("survival.hercules", StringComparison.Ordinal) ? 2 : 1),
                "Medicine effect pairing drifted: " + id);
        }
        _passed++;
    }

    private static void TestDrinkDurationOverride()
    {
        const string name = "All-Consumable-Duration-Override";
        var folder = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            ParameterViewModel Source(string id) => vm.AllParameters.Single(p => p.Id == id);
            var flag = Source("survival.waterInstantStaminaRestore");
            var primary = new (string Id, double VanillaSeconds)[]
            {
                ("survival.waterEffectDuration", 30.0),
                ("survival.energyDrinkEffectDuration", 45.0),
                ("survival.vodkaEffectDuration", 20.0),
                ("survival.beerEffectDuration", 20.0),
                ("survival.magicVodkaEffectDuration", 90.0)
            };
            var children = new[]
            {
                "survival.waterRegenDuration",
                "survival.energyFullCostDuration",
                "survival.energyPartialCostDuration"
            };

            var medicines = new (string Id, double NativeMax)[]
            {
                ("survival.vincaDuration", 540.0),
                ("survival.herculesDuration", 900.0),
                ("survival.psiBlockDuration", 300.0)
            };
            Check(!flag.DrinkDurationOverrideEnabled, name + ": default mode must be OFF");
            foreach (var (id, nativeMax) in medicines)
                Check(Math.Abs(Source(id).Maximum - nativeMax) < 0.000001,
                    name + ": OFF medicine maximum drifted: " + id);
            foreach (var (id, _) in primary)
                Check(Math.Abs(Source(id).Maximum - 3.0) < 0.000001,
                    name + ": OFF failed to preserve per-drink native 3x maximum");

            var before = primary.Select(p => Source(p.Id).CurrentNumber)
                .Concat(children.Select(id => Source(id).CurrentNumber))
                .Concat(medicines.Select(p => Source(p.Id).CurrentNumber)).ToArray();
            flag.DrinkDurationOverrideEnabled = true;
            foreach (var (id, vanilla) in primary)
                Check(Math.Abs(Source(id).Maximum * vanilla - 270.0) < 0.000001,
                    name + ": ON missing common 270-second maximum for " + id);
            // Each secondary effect is independent in value, but it can never
            // outlast its actual parent drink. The Override checkbox expands
            // the parent ceiling; it does NOT uncouple the child from the
            // parent's current seconds (both UI design locks).
            Check(Math.Abs(Source("survival.waterRegenDuration").Maximum
                - Source("survival.waterEffectDuration").CurrentNumber * 30.0) < 0.000001,
                name + ": ON lost Water child/parent live bound");
            foreach (var childId in children.Where(id =>
                         id.StartsWith("survival.energy", StringComparison.Ordinal)))
                Check(Math.Abs(Source(childId).Maximum
                    - Source("survival.energyDrinkEffectDuration").CurrentNumber * 45.0) < 0.000001,
                    name + ": ON lost Energy child/parent live bound");
            foreach (var (id, _) in medicines)
                Check(Math.Abs(Source(id).Maximum - 900.0) < 0.000001,
                    name + ": ON does not set " + id + " to a 900s maximum");

            var after = primary.Select(p => Source(p.Id).CurrentNumber)
                .Concat(children.Select(id => Source(id).CurrentNumber))
                .Concat(medicines.Select(p => Source(p.Id).CurrentNumber)).ToArray();
            Check(before.SequenceEqual(after),
                name + ": enabling the checkbox must not alter any duration");

            // Every independent CFG source reaches exactly 180 seconds even
            // though Water/Beer/Vodka have unequal original native maxima.
            foreach (var (id, vanilla) in primary)
                Source(id).SnapCurrentNumberToMarker(180.0 / vanilla);
            foreach (var childId in children)
            {
                Check(Math.Abs(Source(childId).Maximum - 180.0) < 0.000001,
                    name + ": expanded parent did not raise child's live maximum");
                Source(childId).SnapCurrentNumberToMarker(180.0);
            }
            foreach (var (id, _) in medicines)
            {
                Source(id).SnapCurrentNumberToMarker(900.0);
                Check(Math.Abs(Source(id).CurrentNumber - 900.0) < 0.000001,
                    name + ": medicine failed to reach 900s: " + id);
            }
            foreach (var (id, vanilla) in primary)
                Check(Math.Abs(Source(id).CurrentNumber * vanilla - 180.0) < 0.000001,
                    name + ": equality to 180s failed for " + id);
            foreach (var childId in children)
                Check(Math.Abs(Source(childId).CurrentNumber - 180.0) < 0.000001,
                    name + ": equality to 180s failed for " + childId);
            var allIds = primary.Select(p => p.Id).Concat(children)
                .Concat(medicines.Select(p => p.Id))
                .ToHashSet(StringComparer.Ordinal);
            CheckWriter(vm, allIds, Path.Combine(folder, "override-on"), name + "-ON");

            var preset = new PresetFile
            {
                Name = "Drink Override Test",
                ToolVersion = "0.9.26.82",
                GameVersion = Catalog.Source.GameVersion,
                ReferenceDifficulty = "Veteran",
                DrinkDurationOverride = flag.DrinkDurationOverrideEnabled,
                Values = vm.AllParameters.ToDictionary(
                    p => p.Id, p => p.ToPresetValue(), StringComparer.Ordinal)
            };
            var presetPath = Path.Combine(folder, "saved-drink-override.json");
            var service = new PresetService();
            service.Save(preset, presetPath);
            var reloaded = service.Load(presetPath);
            Check(reloaded.DrinkDurationOverride &&
                Math.Abs(reloaded.Values["survival.beerEffectDuration"].Number!.Value - 9.0) < 0.000001 &&
                medicines.All(p =>
                    Math.Abs(reloaded.Values[p.Id].Number!.Value - 900.0) < 0.000001),
                name + ": preset did not round-trip extended drink and medicine durations");

            flag.DrinkDurationOverrideEnabled = false;
            foreach (var (id, vanilla) in primary)
                Check(Math.Abs(Source(id).Maximum - 3.0) < 0.000001
                    && Source(id).CurrentNumber <= 3.0 + 0.000001,
                    name + ": OFF did not restore native maximum for " + id);
            foreach (var (id, nativeMax) in medicines)
                Check(Math.Abs(Source(id).Maximum - nativeMax) < 0.000001
                    && Math.Abs(Source(id).CurrentNumber - nativeMax) < 0.000001,
                    name + ": OFF did not clamp medicine to its native limit: " + id);
            Check(Math.Abs(Source("survival.waterRegenDuration").CurrentNumber -
                Source("survival.waterEffectDuration").CurrentNumber * 30.0) < 0.000001,
                name + ": child Water duration not minimally capped to parent");
            foreach (var childId in children.Where(x => x.StartsWith("survival.energy", StringComparison.Ordinal)))
                Check(Math.Abs(Source(childId).CurrentNumber -
                    Source("survival.energyDrinkEffectDuration").CurrentNumber * 45.0) < 0.000001,
                    name + ": child Energy duration not minimally capped to parent");
            CheckWriter(vm, allIds, Path.Combine(folder, "override-off"), name + "-OFF");
            _passed++;
            Console.WriteLine("PASS " + name + " (drinks 270s, medicines 900s, native caps, CFG writer, preset)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    private static void TestShortDurationBounds(
        string mainId, string[] shortIds, double mainVanillaSeconds,
        double newMainMultiplier, double expectedCapSeconds)
    {
        var name = "Short-Duration-Bounds-" + mainId;
        var output = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var main = vm.AllParameters.Single(p => p.Id == mainId);
            var children = shortIds.Select(id => vm.AllParameters.Single(p => p.Id == id)).ToArray();
            Check(main.IsSlider && children.All(p => p.IsSlider),
                name + ": missing live native slider");
            foreach (var child in children)
            {
                Check(Math.Abs(child.Minimum - child.ReferenceNumber) < 0.000001,
                    name + ": child minimum must be authored vanilla");
                Check(Math.Abs(child.Maximum - mainVanillaSeconds) < 0.000001,
                    name + ": child's dynamic maximum must match current main seconds");
                child.SnapCurrentNumberToMarker(mainVanillaSeconds);
                Check(Math.Abs(child.CurrentNumber - mainVanillaSeconds) < 0.000001,
                    name + ": cannot reach main effect's actual duration");
            }

            main.SnapCurrentNumberToMarker(newMainMultiplier);
            Check(Math.Abs(main.CurrentNumber * mainVanillaSeconds -
                expectedCapSeconds) < 0.000001,
                name + ": main duration not respected");
            foreach (var child in children)
            {
                Check(Math.Abs(child.Maximum - expectedCapSeconds) < 0.000001,
                    name + ": dynamic maximum did not move with main");
                Check(Math.Abs(child.CurrentNumber - expectedCapSeconds) < 0.000001,
                    name + ": child did not minimally clamp when main shortened");
                child.SnapCurrentNumberToMarker(child.Maximum + 20);
                Check(Math.Abs(child.CurrentNumber - expectedCapSeconds) < 0.000001,
                    name + ": out-of-range edited duration bypassed dynamic cap");
            }

            // Parent expansion is not a command to change the independently
            // adjusted children; only shrinking their actual upper bound cascades.
            main.SnapCurrentNumberToMarker(1.0);
            foreach (var child in children)
                Check(Math.Abs(child.CurrentNumber - expectedCapSeconds) < 0.000001
                      && Math.Abs(child.Maximum - mainVanillaSeconds) < 0.000001,
                    name + ": expanding main illegally moved a shorter duration");
            main.SnapCurrentNumberToMarker(newMainMultiplier);

            var expectedIds = new HashSet<string>(shortIds, StringComparer.Ordinal) { mainId };
            CheckWriter(vm, expectedIds, output, name);
            _passed++;
            Console.WriteLine("PASS " + name + " (real-second caps, native BPatch)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private sealed record LinkedCase(string Name, string ActiveId, double MovedTo,
        (string Id, double Value)[] Expected);

    private static (string Id, double Value) E(string id, double value) => (id, value);

    private static void TestLinked(LinkedCase test)
    {
        var name = "Linked-" + test.Name;
        var folder = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var active = vm.AllParameters.Single(p => p.Id == test.ActiveId);
            active.SnapCurrentNumberToMarker(test.MovedTo);
            Check(Math.Abs(active.CurrentNumber - test.MovedTo) < 0.000001,
                name + ": dragged slider lost priority");
            var expected = test.Expected.ToDictionary(p => p.Id, p => p.Value,
                StringComparer.Ordinal);
            foreach (var pair in expected)
            {
                var p = vm.AllParameters.Single(x => x.Id == pair.Key);
                Check(Math.Abs(p.CurrentNumber - pair.Value) < 0.000001,
                    name + ": linked value " + pair.Key + " expected " + pair.Value
                    + ", got " + p.CurrentNumber);
            }
            var actual = vm.AllParameters.Where(p =>
                p.Id.StartsWith("survival.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference())
                .Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(actual.SetEquals(expected.Keys), name + ": unexpected modified sliders: "
                + string.Join(", ", actual));
            CheckWriter(vm, actual, folder, name);
            _passed++;
            Console.WriteLine("PASS " + name + " (only " + actual.Count + " cascaded CFG controls)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    private static void TestReenabled(string parentId, string childId)
    {
        var name = "Master-Reenabled-" + childId;
        var folder = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch",
            name.Replace(".", "-", StringComparison.Ordinal));
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Master" };
            var parent = vm.AllParameters.Single(p => p.Id == parentId);
            var child = vm.AllParameters.Single(p => p.Id == childId);
            Check(parent.IsToggle && parent.ReferenceBoolean && child.IsControlEnabled,
                name + ": Master dependency should begin enabled");
            if (childId == "survival.starvationStaminaDrain")
                Check(child.IsVisualAggregateAnchor && child.IsControlVisible,
                    name + ": visual aggregate anchor must remain visible when toggled OFF");
            child.SnapCurrentNumberToMarker(child.ReferenceNumber
                + (child.Definition.Slider?.Step ?? 0.1));
            Check(child.IsValueModifiedFromReference(), name + ": child never changed");
            parent.CurrentBoolean = false;
            Check(!child.IsControlEnabled && child.IsValueModifiedFromReference(),
                name + ": parent OFF did not retain an inactive edited child");
            if (childId == "survival.starvationStaminaDrain")
            {
                Check(child.IsVisualAggregateAnchor && child.IsControlVisible,
                    name + ": parent OFF must not hide the whole Starvation panel");
                Check(vm.AllParameters.Where(p => p.Definition.RequiresToggle == parentId)
                          .All(p => !p.IsControlEnabled),
                    name + ": all five Starvation penalty tracks must be inactive while OFF");
            }

            // The parent OFF change legitimately patches DifficultyPrototypes,
            // but the custom retained child must NOT leak EffectPrototypes while
            // inactive. Test the real production writer before switching ON.
            var offFolder = Path.Combine(folder, "bundle-off");
            var offResult = new CfgPatchWriterService().Write(vm, Catalog, null, offFolder);
            Check(offResult.FileCount > 0, name + ": parent OFF did not write its bundle change");
            // The inactive child must not emit an unrelated EffectPrototypes CFG.
            Check(!Directory.EnumerateFiles(offFolder, "*.cfg", SearchOption.AllDirectories)
                    .Any(f => f.Contains("EffectPrototypes", StringComparison.OrdinalIgnoreCase)),
                name + ": inactive child leaked into EffectPrototypes BPatch");
            parent.CurrentBoolean = true;
            Check(child.IsControlEnabled && child.IsValueModifiedFromReference()
                && !parent.IsValueModifiedFromReference(),
                name + ": re-enabling parent failed to retain child-only change");
            if (childId == "survival.starvationStaminaDrain")
                Check(vm.AllParameters.Where(p => p.Definition.RequiresToggle == parentId)
                          .All(p => p.IsControlEnabled),
                    name + ": all five Starvation controls must re-enable with their parent");
            CheckWriter(vm, new HashSet<string>(StringComparer.Ordinal) {childId},
                folder, name);
            _passed++;
            Console.WriteLine("PASS " + name + " (parent restored, child-only CFG)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    private static void CheckWriter(MainViewModel vm, HashSet<string> changed, string folder,
        string name)
    {
        var sid = Catalog.DifficultyProfiles[vm.SelectedReferenceDifficulty].Sid;
        var mapped = Baseline.EditPoints.Where(p => changed.Contains(p.ParameterId)
            && (string.IsNullOrEmpty(p.DifficultySid) || p.DifficultySid == sid)).ToArray();
        Check(mapped.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal)
              .SetEquals(changed), name + ": missing concrete direct source mappings");
        var result = new CfgPatchWriterService().Write(vm, Catalog, null, folder);
        var expectedFiles = mapped.Select(p => ExpectedFile(folder,p.File))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Check(result.FileCount == expectedFiles.Length && result.ParameterCount == changed.Count
              && result.EditCount == mapped.Length,
              name + ": writer totals include unrelated/missing parameters");
        var observedFiles = Directory.GetFiles(folder, "*TrueCustomDifficulty*",
            SearchOption.AllDirectories);
        Check(observedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(expectedFiles),
            name + ": extra or missing CFG output files");
        foreach (var file in expectedFiles)
        {
            var sameFilePoints = mapped.Where(p =>
                string.Equals(ExpectedFile(folder,p.File),file,
                    StringComparison.OrdinalIgnoreCase)).ToArray();
            var patch = Parse(File.ReadAllText(file));
            var expectedPaths = sameFilePoints.Select(p => p.Prototype + "/" +
                (p.NodePath.Count == 0 ? "" : string.Join("/",p.NodePath) + "/") + p.Key)
                .ToHashSet(StringComparer.Ordinal);
            var observedPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in patch.Children.Values)
                Collect(root,root.Name,observedPaths);
            Check(observedPaths.SetEquals(expectedPaths),
                name + ": exact BPatch leaf set mismatch: expected "
                + string.Join(",",expectedPaths) + " observed " + string.Join(",",observedPaths));
            foreach (var point in sameFilePoints)
            {
                var node = patch.Get(point.Prototype);
                Check(node.Bpatch, name + ": root not bpatch");
                foreach (var part in point.NodePath)
                {
                    node = node.Get(part);
                    Check(node.Bpatch, name + ": nested path not bpatch: " + part);
                }
                var vmParameter = vm.AllParameters.Single(p => p.Id == point.ParameterId);
                Approx(node.Value(point.Key),Expected(vmParameter,point),
                    name + ": writer value drift " + point.ParameterId);
            }
        }
    }

    private static void TestVanilla(string difficulty)
    {
        var name = "Vanilla-" + difficulty;
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var sleep = vm.AllParameters.Single(p => p.Id == "survival.sleepHealthRestore");
            Check(Math.Abs(sleep.CurrentNumber - 300.0) < 0.000001
                  && !sleep.IsValueModifiedFromReference(),
                name + ": sleep HP must be source-neutral");
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            Check(result.FileCount == 0 && result.EditCount == 0 && result.ParameterCount == 0,
                name + ": untouched project wrote CFG changes");
            Check(!Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any(),
                name + ": untouched project wrote files");
            _passed++;
            Console.WriteLine("PASS " + name + " (zero CFG edits)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    private static void TestDisabled(string id)
    {
        var name = "Veteran-Gated-" + id.Replace(".", "-", StringComparison.Ordinal);
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            var child = vm.AllParameters.Single(p => p.Id == id);
            Check(!child.IsControlEnabled, name + ": Master parent unexpectedly ON");
            child.SnapCurrentNumberToMarker(child.ReferenceNumber + 0.1);
            Check(child.IsValueModifiedFromReference() && !child.IsControlEnabled,
                name + ": edited child should remain inactive");
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            Check(result.FileCount == 0 && result.EditCount == 0 && result.ParameterCount == 0,
                name + ": disabled Master child emitted CFG changes");
            Check(!Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any(),
                name + ": inactive child unexpectedly wrote its own CFG prototype");
            _passed++;
            Console.WriteLine("PASS " + name + " (gated value not emitted)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    // The +25% Stamina effect is distinct from the native -10% SPDrain:
    // both have their own full CFG ValueMin/Max ownership and source UI bar.
    private static void TestIndependentWaterStrengths()
    {
        const string test = "Independent-Water-Strengths";
        var bonusId = "survival.waterStaminaEffectStrength";
        var costId = "survival.waterStaminaCostReduction";
        var all = Baseline.EditPoints.Where(p =>
            p.ParameterId == bonusId || p.ParameterId == costId).ToArray();
        Check(all.Length == 4 && all.Count(p => p.ParameterId == bonusId) == 2
            && all.Count(p => p.ParameterId == costId) == 2,
            test + ": each source must own exactly ValueMin and ValueMax");
        Check(all.Where(p => p.ParameterId == bonusId).All(p =>
            p.Prototype == "WaterStamina2" && p.Operation == "scale" && p.SourceRaw == "25%")
            && all.Where(p => p.ParameterId == costId).All(p =>
            p.Prototype == "WaterStaminaPerAction1" && p.Operation == "scale"
            && p.SourceRaw == "-10%"),
            test + ": native bonus and action SPDrain mappings are not independent");
        var vm = new MainViewModel(Catalog);
        foreach (var id in new[] { bonusId, costId })
        {
            var source = vm.AllParameters.Single(p => p.Id == id);
            Check(source.VisualAggregateFamily == "ConsumableMixer"
                && !source.IsControlVisible && Math.Abs(source.ReferenceNumber - 1.0) < 0.000001
                && Math.Abs(source.Maximum - 2.0) < 0.000001
                && Math.Abs(source.TickFrequency - 0.05) < 0.000001,
                test + ": source is not independently mounted in native Water bar: " + id);
        }
        _passed++;
    }

    private static void TestIndependentInstantRefill()
    {
        const string test = "Independent-Instant-Stamina";
        var waterId = "survival.waterInstantStaminaRestore";
        var energyId = "survival.energyDrinkInstantStaminaRestore";
        var relevant = Baseline.EditPoints.Where(p =>
            p.ParameterId == waterId || p.ParameterId == energyId).ToArray();
        Check(relevant.Length == 4, test + ": expected exactly two direct fields per drink");
        Check(relevant.Count(p => p.ParameterId == waterId) == 2
            && relevant.Where(p => p.ParameterId == waterId).All(p =>
                p.Prototype == "WaterStaminaInstant" && p.Operation == "set"
                && p.SourceRaw == "38%"),
            test + ": Water must own only its vanilla 38% prototype");
        Check(relevant.Count(p => p.ParameterId == energyId) == 2
            && relevant.Where(p => p.ParameterId == energyId).All(p =>
                p.Prototype == "EnergeticStaminaInstant3" && p.Operation == "set"
                && p.SourceRaw == "50%"),
            test + ": Energy Drink must own only its vanilla 50% prototype");
        Check(!relevant.Any(p => p.Prototype == "EnergeticLimitedStaminaInstant4"),
            test + ": Limited Energy Drink must stay untouched");
        var vm = new MainViewModel(Catalog);
        var water = vm.AllParameters.Single(p => p.Id == waterId);
        var energy = vm.AllParameters.Single(p => p.Id == energyId);
        Check(Math.Abs(water.CurrentNumber - 0.38) < 0.000001
            && Math.Abs(energy.CurrentNumber - 0.50) < 0.000001,
            test + ": wrong actual instant stamina percentage");
        Check(Math.Abs(water.TickFrequency - 0.01) < 0.000001
            && Math.Abs(energy.TickFrequency - 0.01) < 0.000001,
            test + ": both controls need exactly 1 percentage point per step");
        _passed++;
    }

    private static void TestCombined(string label, string difficulty,
        (string Id, double Value)[] changes)
    {
        var output = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var ids = changes.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            Check(ids.Count == changes.Length, label + ": duplicate parameter ID");
            foreach (var (id, value) in changes)
            {
                var p = vm.AllParameters.Single(x => x.Id == id);
                Check(p.IsControlEnabled, label + ": inactive child " + id);
                p.SnapCurrentNumberToMarker(value);
                Check(p.IsValueModifiedFromReference(), label + ": unchanged value " + id);
            }
            var changed = vm.AllParameters.Where(p =>
                p.Id.StartsWith("survival.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference())
                .Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(changed.SetEquals(ids), label + ": linked slider/anchor changed "
                  + string.Join(", ", changed) + ", expected " + string.Join(", ", ids));
            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var points = Baseline.EditPoints.Where(p => ids.Contains(p.ParameterId)
                && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid == sid))
                .ToArray();
            Check(points.Length >= ids.Count, label + ": insufficient CFG source fields");

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, output);
            var files = points.Select(p => ExpectedFile(output, p.File))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var actualFiles = Directory.GetFiles(output, "*TrueCustomDifficulty*",
                SearchOption.AllDirectories);
            Check(result.ParameterCount == ids.Count && result.EditCount == points.Length
                  && result.FileCount == files.Length
                  && actualFiles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(files),
                label + ": extra or missing CFG files, edits, or parameter IDs");

            foreach (var file in files)
            {
                var doc = Parse(File.ReadAllText(file));
                var relevant = points.Where(p => string.Equals(
                    ExpectedFile(output, p.File), file, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var expected = relevant.Select(p => p.Prototype + "/"
                    + (p.NodePath.Count > 0 ? string.Join("/", p.NodePath) + "/" : "")
                    + p.Key).ToHashSet(StringComparer.Ordinal);
                var actual = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in doc.Children.Values)
                    Collect(root, root.Name, actual);
                Check(actual.SetEquals(expected), label + ": merged patch lost or added fields:"
                      + " wanted " + string.Join(", ", expected)
                      + ", got " + string.Join(", ", actual));
                foreach (var point in relevant)
                {
                    var node = doc.Get(point.Prototype);
                    Check(node.Bpatch, label + ": prototype missing bpatch flag");
                    foreach (var segment in point.NodePath)
                    {
                        node = node.Get(segment);
                        Check(node.Bpatch, label + ": nested flag missing at " + segment);
                    }
                    var parameter = vm.AllParameters.Single(p => p.Id == point.ParameterId);
                    Approx(node.Value(point.Key), Expected(parameter, point),
                        label + ": " + point.ParameterId + "/" + point.Key);
                }
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (" + points.Length
                              + " merged source fields)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private static void Test(string id, string difficulty)
    {
        var label = difficulty + "-" + id.Replace(".", "-", StringComparison.Ordinal);
        var path = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var par = vm.AllParameters.Single(p => p.Id == id);
            Check(par.IsSlider && par.IsControlEnabled,
                label + ": expected enabled real numeric editor");
            Check(!par.IsValueModifiedFromReference(), label + ": unexpected startup edit");
            var step = Math.Max(0.01, par.Definition.Slider?.Step ?? 0.1);
            var candidate = par.ReferenceNumber + step;
            if (candidate > par.Maximum)
                candidate = par.ReferenceNumber - step;
            par.SnapCurrentNumberToMarker(candidate);
            Check(par.IsValueModifiedFromReference() && par.IsControlEnabled,
                label + ": slider not active/modified");

            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var points = Baseline.EditPoints.Where(p =>
                p.ParameterId == id
                && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid == sid))
                .ToArray();
            Check(points.Length > 0, label + ": no matching CFG source mapping");
            Check(points.All(p => p.Scope == "baseGame"), label + ": unexpected DLC1 point");

            var allChanged = vm.AllParameters.Where(p =>
                p.Id.StartsWith("survival.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p => p.Id).ToArray();
            Check(allChanged.ToHashSet(StringComparer.Ordinal).SetEquals(
                      new[] { id }),
                label + ": slider constraint moved another actual parameter: "
                + string.Join(", ", allChanged));

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            var files = points.Select(p => ExpectedFile(path, p.File))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            Check(result.FileCount == files.Length
                  && result.ParameterCount == 1
                  && result.EditCount == points.Length,
                label + ": incorrect file/parameter/edit counts ("
                + result.FileCount + "/" + result.ParameterCount + "/"
                + result.EditCount + ", wanted " + files.Length + "/1/" + points.Length + ")");
            var actualFiles = Directory.GetFiles(path, "*TrueCustomDifficulty*",
                SearchOption.AllDirectories);
            Check(actualFiles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(files),
                label + ": unrelated/missing output files: " + string.Join(",", actualFiles));

            foreach (var file in files)
            {
                var sourcePoints = points.Where(p =>
                    string.Equals(ExpectedFile(path, p.File), file,
                                  StringComparison.OrdinalIgnoreCase)).ToArray();
                var patch = Parse(File.ReadAllText(file));
                var expectedPaths = sourcePoints.Select(p =>
                    p.Prototype + "/" + (p.NodePath.Count > 0
                        ? string.Join("/", p.NodePath) + "/" : "") + p.Key)
                    .ToHashSet(StringComparer.Ordinal);
                var observedPaths = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in patch.Children.Values)
                    Collect(root, root.Name, observedPaths);
                Check(expectedPaths.SetEquals(observedPaths),
                    label + ": unexpected BPatch paths expected={"
                    + string.Join(",", expectedPaths) + "} actual={"
                    + string.Join(",", observedPaths) + "}");

                foreach (var point in sourcePoints)
                {
                    var node = patch.Get(point.Prototype);
                    Check(node.Bpatch, label + ": prototype missing bpatch flag");
                    foreach (var segment in point.NodePath)
                    {
                        node = node.Get(segment);
                        Check(node.Bpatch, label + ": nested segment missing bpatch: " + segment);
                    }
                    var actualParameter = vm.AllParameters.Single(p => p.Id == point.ParameterId);
                    var expected = Expected(actualParameter, point);
                    Approx(node.Value(point.Key), expected, label + ": " + point.Prototype + "." + point.Key);
                }
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (" + points.Length + " source fields)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    private static string ExpectedFile(string outRoot, string source)
    {
        var file = Path.GetFileName(source);
        var stem = Path.GetFileNameWithoutExtension(file);
        var parent = Path.GetDirectoryName(source.Replace('/', Path.DirectorySeparatorChar))
                     ?? string.Empty;
        var folder = Path.Combine(outRoot, "Stalker2", "Content", "GameLite", parent);
        return file.EndsWith("Prototypes.cfg", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(folder, stem, stem + "_patch_TrueCustomDifficulty.cfg")
            : Path.Combine(folder, file + "_patch_TrueCustomDifficulty");
    }

    private static double Expected(ParameterViewModel par, CfgWriterEditPoint point)
    {
        return point.Operation switch
        {
            "set" => par.CurrentNumber,
            "negateSet" => -par.CurrentNumber,
            "scale" => ParseNumber(point.SourceRaw) * par.CurrentNumber,
            _ => throw new InvalidDataException(
                point.ParameterId + ": unsupported Survival operation " + point.Operation),
        };
    }

    private static double ParseNumber(string raw)
    {
        var text = raw.Trim();
        var percent = text.EndsWith("%", StringComparison.Ordinal);
        if (percent) text = text[..^1];
        if (text.EndsWith("f", StringComparison.OrdinalIgnoreCase)) text = text[..^1];
        return double.Parse(text, CultureInfo.InvariantCulture) / (percent ? 100 : 1);
    }

    private static void Approx(string raw, double expected, string description)
    {
        var actual = ParseNumber(raw);
        Check(Math.Abs(actual - expected) < 0.0000011,
            description + ": wrong value " + actual + ", expected " + expected);
    }

    private static void Collect(Node node, string prefix, HashSet<string> result)
    {
        foreach (var key in node.Assignments.Keys)
            Check(result.Add(prefix + "/" + key), "Duplicate CFG field " + prefix + "/" + key);
        foreach (var child in node.Children.Values)
            Collect(child, prefix + "/" + child.Name, result);
    }

    private static Node Parse(string cfg)
    {
        var root = new Node("document", "");
        var nodes = new Stack<Node>();
        nodes.Push(root);
        foreach (var raw in cfg.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (line == "struct.end")
            {
                Check(nodes.Count > 1, "Unmatched CFG struct.end");
                nodes.Pop();
                continue;
            }
            var open = Regex.Match(line, @"^(?<name>\S+)\s*:\s*struct\.begin(?<flags>.*)$");
            if (open.Success)
            {
                var n = new Node(open.Groups["name"].Value, open.Groups["flags"].Value);
                Check(nodes.Peek().Children.TryAdd(n.Name, n), "Duplicate CFG node " + n.Name);
                nodes.Push(n);
                continue;
            }
            var value = Regex.Match(line, @"^(?<key>\S+)\s*=\s*(?<value>.*)$");
            Check(value.Success, "Unknown CFG line: " + line);
            Check(nodes.Peek().Assignments.TryAdd(value.Groups["key"].Value,
                                                   value.Groups["value"].Value),
                "Duplicate CFG assignment " + line);
        }
        Check(nodes.Count == 1, "CFG missing closing struct.end");
        return root;
    }

    private static void Check(bool ok, string description)
    {
        if (!ok) throw new InvalidDataException(description);
    }

    private sealed class Node(string name, string flags)
    {
        public string Name { get; } = name;
        public bool Bpatch { get; } = flags.Contains("{bpatch}", StringComparison.Ordinal);
        public Dictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Assignments { get; } = new(StringComparer.Ordinal);
        public Node Get(string name) =>
            Children.TryGetValue(name, out var node) ? node :
            throw new InvalidDataException($"CFG {Name} missing child {name}");
        public string Value(string key) =>
            Assignments.TryGetValue(key, out var value) ? value :
            throw new InvalidDataException($"CFG {Name} missing assignment {key}");
    }
}
