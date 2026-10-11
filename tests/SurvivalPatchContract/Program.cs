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
            Check(cohort.Length == 56, "Survival catalog no longer contains exactly 56 parameters");

            MasterPenaltyKnobSourceContract();

            var vm = new MainViewModel(Catalog);
            var loaded = vm.AllParameters.Select(p => p.Id)
                .ToHashSet(StringComparer.Ordinal);
            var unmounted = cohort.Where(p => !loaded.Contains(p.Id))
                .Select(p => p.Id).ToArray();
            Check(unmounted.SequenceEqual(new[] { "survival.drunknessThresholdScale" }),
                "Survival catalog-to-live-VM exclusions changed: "
                + string.Join(", ", unmounted));
            var live = cohort.Where(p => loaded.Contains(p.Id)).ToArray();
            Check(live.Length == 55, "Expected 55 live Survival source-backed parameters");

            var points = Baseline.EditPoints
                .Where(p => p.ParameterId.StartsWith("survival.", StringComparison.Ordinal))
                .ToArray();
            Check(points.Length == 135, "Survival manifest no longer contains 135 source edit points");
            Check(points.All(p => p.Scope == "baseGame"), "Survival source unexpectedly targets DLC");
            Check(points.Select(p => p.ParameterId).Distinct().Count() == 56,
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
                ("survival.instantStaminaRestore", 1.2),
                ("survival.energyDrinkEffectDuration", 1.3),
                ("survival.energyDrinkStaminaRecoveryStrength", 1.25),
                ("survival.energyDrinkFatigueRelief", 1.4)
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
                ("survival.starvationDamageInterval", 11.0),
                ("survival.severeFatigueRecoil", 0.65),
                ("survival.radiationStaminaDrainPenalty", 0.6)
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
    private static void TestDrinkDurationOverride()
    {
        const string name = "All-Drink-Duration-Override";
        var folder = Path.Combine(_keep ?? Path.GetTempPath(), "TCD_Survival_Patch", name);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
            ParameterViewModel Source(string id) => vm.AllParameters.Single(p => p.Id == id);
            var flag = Source("survival.instantStaminaRestore");
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

            Check(!flag.DrinkDurationOverrideEnabled, name + ": default mode must be OFF");
            foreach (var (id, _) in primary)
                Check(Math.Abs(Source(id).Maximum - 3.0) < 0.000001,
                    name + ": OFF failed to preserve per-drink native 3x maximum");

            var before = primary.Select(p => Source(p.Id).CurrentNumber)
                .Concat(children.Select(id => Source(id).CurrentNumber)).ToArray();
            flag.DrinkDurationOverrideEnabled = true;
            foreach (var (id, vanilla) in primary)
                Check(Math.Abs(Source(id).Maximum * vanilla - 270.0) < 0.000001,
                    name + ": ON missing common 270-second maximum for " + id);
            foreach (var childId in children)
                Check(Math.Abs(Source(childId).Maximum - 270.0) < 0.000001,
                    name + ": ON still caps secondary effect to its parent");

            var after = primary.Select(p => Source(p.Id).CurrentNumber)
                .Concat(children.Select(id => Source(id).CurrentNumber)).ToArray();
            Check(before.SequenceEqual(after),
                name + ": enabling the checkbox must not alter any duration");

            // Every independent CFG source reaches exactly 180 seconds even
            // though Water/Beer/Vodka have unequal original native maxima.
            foreach (var (id, vanilla) in primary)
                Source(id).SnapCurrentNumberToMarker(180.0 / vanilla);
            foreach (var childId in children)
                Source(childId).SnapCurrentNumberToMarker(180.0);
            foreach (var (id, vanilla) in primary)
                Check(Math.Abs(Source(id).CurrentNumber * vanilla - 180.0) < 0.000001,
                    name + ": equality to 180s failed for " + id);
            foreach (var childId in children)
                Check(Math.Abs(Source(childId).CurrentNumber - 180.0) < 0.000001,
                    name + ": equality to 180s failed for " + childId);
            var allIds = primary.Select(p => p.Id).Concat(children)
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
                Math.Abs(reloaded.Values["survival.beerEffectDuration"].Number!.Value - 9.0) < 0.000001,
                name + ": preset did not round-trip override and formerly restricted duration");

            flag.DrinkDurationOverrideEnabled = false;
            foreach (var (id, vanilla) in primary)
                Check(Math.Abs(Source(id).Maximum - 3.0) < 0.000001
                    && Source(id).CurrentNumber <= 3.0 + 0.000001,
                    name + ": OFF did not restore native maximum for " + id);
            Check(Math.Abs(Source("survival.waterRegenDuration").CurrentNumber -
                Source("survival.waterEffectDuration").CurrentNumber * 30.0) < 0.000001,
                name + ": child Water duration not minimally capped to parent");
            foreach (var childId in children.Where(x => x.StartsWith("survival.energy", StringComparison.Ordinal)))
                Check(Math.Abs(Source(childId).CurrentNumber -
                    Source("survival.energyDrinkEffectDuration").CurrentNumber * 45.0) < 0.000001,
                    name + ": child Energy duration not minimally capped to parent");
            CheckWriter(vm, allIds, Path.Combine(folder, "override-off"), name + "-OFF");
            _passed++;
            Console.WriteLine("PASS " + name + " (shared ceiling, independent values, both writer modes, preset)");
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
            Check(!Directory.EnumerateFiles(offFolder, "*.cfg", SearchOption.AllDirectories)
                    .Any(f => f.Contains("EffectPrototypes", StringComparison.OrdinalIgnoreCase)),
                name + ": inactive staged child leaked its EffectPrototypes BPatch");
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
            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            Check(result.FileCount == 0 && result.EditCount == 0 && result.ParameterCount == 0,
                name + ": vanilla preset emitted unexpected CFG");
            Check(!Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any(),
                name + ": vanilla output directory contains generated files");
            _passed++;
            Console.WriteLine("PASS " + name + " (no CFG)");
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
                name + ": inactive child leaked into CFG writer output");
            Check(!Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any(),
                name + ": inactive child unexpectedly wrote a CFG file");
            _passed++;
            Console.WriteLine("PASS " + name + " (gated value not emitted)");
        }
        finally
        {
            if (_keep is null && Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
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
            Check(!par.IsValueModifiedFromReference(), label + ": nonvanilla starting value");
            var step = Math.Max(0.01, par.Definition.Slider?.Step ?? 0.1);
            var candidate = par.ReferenceNumber + step;
            if (candidate > par.Maximum)
                candidate = par.ReferenceNumber - step;
            par.SnapCurrentNumberToMarker(candidate);
            Check(par.IsValueModifiedFromReference() && par.IsControlEnabled,
                label + ": slider not active/modified");

            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var points = Baseline.EditPoints.Where(p => p.ParameterId == id
                && (string.IsNullOrWhiteSpace(p.DifficultySid) || p.DifficultySid == sid))
                .ToArray();
            Check(points.Length > 0, label + ": no matching CFG source mapping");
            Check(points.All(p => p.Scope == "baseGame"), label + ": unexpected DLC1 point");

            var allChanged = vm.AllParameters.Where(p =>
                p.Id.StartsWith("survival.", StringComparison.Ordinal)
                && p.IsValueModifiedFromReference()).Select(p => p.Id).ToArray();
            Check(allChanged.SequenceEqual(new[] { id }),
                label + ": slider constraint moved another actual parameter: "
                + string.Join(", ", allChanged));

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, path);
            var files = points.Select(p => ExpectedFile(path, p.File))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            Check(result.FileCount == files.Length
                  && result.ParameterCount == 1 && result.EditCount == points.Length,
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
                    var expected = Expected(par, point);
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
