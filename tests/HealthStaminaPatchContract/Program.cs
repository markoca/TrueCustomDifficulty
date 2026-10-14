using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.Controls;
using TrueCustomDifficulty.ViewModels;

namespace HealthStaminaPatchContract;

// This executes the actual WPF ViewModel and CfgPatchWriterService. No
// independent CFG serialization or game runtime is emulated. Source ZIP
// parity is independently checked by tools/audit_cfg_source_values.py.
internal static class Program
{
    private static readonly ParameterCatalog Catalog = new ParameterCatalogService().Load();
    private static readonly CfgWriterBaseline Baseline = new CfgWriterBaselineService().Load();
    private static string? _keepRoot;
    private static int _passed;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0)
            {
                if (args.Length != 2 || args[0] != "--out")
                    throw new ArgumentException("Usage: HealthStaminaPatchContract [--out PATH]");
                _keepRoot = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(_keepRoot);
            }

            // Verify 1x Bandage cap and legacy preset bounds with the live VM.
            BandageBleedingUpperBoundCase();
            BleedingTimelineProjectionCase();

            MedicalItemsPairingCase();
            MovementLimitsAndExhaustionNamingCase();
            StaminaStatesLayoutContractCase();

            // A unique concrete edit for every live Health/Stamina catalog
            // parameter. Master-only modifiers use Master because their
            // parent AdditionalMechanics switch is naturally enabled there.
            var cohort = Catalog.Parameters
                .Where(p => p.Id.StartsWith("health.", StringComparison.Ordinal)
                         || p.Id.StartsWith("stamina.", StringComparison.Ordinal))
                .OrderBy(p => p.Id, StringComparer.Ordinal)
                .ToArray();
            Check(cohort.Length == 45, "Health/Stamina catalog cohort is no longer 45 controls");

            // These five source-backed speed-state definitions are intentionally
            // excluded from MainViewModel. They are documented internal CFG
            // research, not exposed/active UI controls or valid writer inputs.
            // Pin the exclusion rather than inventing UI coverage for them.
            var internalOnly = new HashSet<string>(StringComparer.Ordinal)
            {
                "stamina.sprintUnderRunSpeedRegenCoef",
                "stamina.joggingRegenCoef",
                "stamina.slowRunRegenCoef",
                "stamina.sprintRegenCoef",
                "stamina.runRegenCoef"
            };
            var initialVm = new MainViewModel(Catalog);
            var liveIds = initialVm.AllParameters.Select(p => p.Id)
                .ToHashSet(StringComparer.Ordinal);
            var notLoaded = cohort.Where(p => !liveIds.Contains(p.Id))
                .Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(notLoaded.SetEquals(internalOnly),
                "Unexpected Health/Stamina app VM exclusions: "
                + string.Join(", ", notLoaded.OrderBy(s => s)));
            var liveCohort = cohort.Where(p => liveIds.Contains(p.Id)).ToArray();
            Check(liveCohort.Length == 40,
                "Health/Stamina live VM count is not 40");
            foreach (var def in liveCohort)
            {
                var diff = def.Id.StartsWith("health.master", StringComparison.Ordinal)
                    ? "Master" : "Veteran";
                Test(def.Id, diff);
            }
            Console.WriteLine("INTERNAL SOURCE ONLY: 5 intentionally non-VM stamina speed-state CFG definitions");

            // The 4 selected Base Presets must map scalar Regen_HP to the
            // correct Easy/Medium/Hard/Stalker DifficultyPrototypes row.
            foreach (var difficulty in new[] { "Novice", "Stalker", "Master" })
                Test("health.regenerationScalar", difficulty);

            // Parent toggles are intentionally full-list replacements.
            // Regression: the child-only modifier tests above must NOT
            // trigger this list; a genuinely changed parent must still do so.
            foreach (var (id, member) in new[]
            {
                ("advanced.starvationMechanics", "HungerNegativeMechanics"),
                ("advanced.radiationMechanics", "RadiationNegativeMechanics"),
                ("advanced.fatigueMechanics", "SleepinessNegativeMechanics"),
                ("advanced.lowHealthMechanics", "RegenHealthNegativeMechanics"),
                ("advanced.masterEffectModifierMechanics", "MasterEffectModifierNegativeMechanics")
            })
                MasterParentToggleCase(id, member);

            Console.WriteLine($"PASS: {_passed} Health/Stamina production BPatch writer scenarios.");
            Console.WriteLine("NOT VERIFIED: in-game runtime, UE nested inherited VaultingParams BPatch, original-source archive parity.");
            return 0;
        }
        catch (Exception exc)
        {
            Console.Error.WriteLine("FAIL: " + exc);
            return 1;
        }
    }

    private static void MovementLimitsAndExhaustionNamingCase()
    {
        // Pin the UI-only design changes without changing the writer contract:
        // the six exhaustion inputs retain their stable CFG source identities.
        var labels = new (string Id, string Label)[]
        {
            ("stamina.sprintDisableThreshold", "Sprint Locks"),
            ("stamina.lowRecoveryThreshold", "Recovery Kicks In"),
            ("stamina.aimRegenCoef", "Aiming Regen Penalty"),
            ("stamina.sprintRecoveryDelay", "Exhaustion Regen Delay"),
            ("stamina.lowRecoveryStrength", "Boost Strength"),
            ("stamina.lowRecoveryDuration", "Boost Duration")
        };
        foreach (var (id, label) in labels)
        {
            var source = Catalog.Parameters.Single(p => p.Id == id);
            Check(source.Label == label,
                $"Stamina gamer-facing label mismatch: {id}");
            Check(source.Targets.Count > 0,
                $"Stamina source lost its CFG writer route: {id}");
        }

        var vault = Catalog.Parameters.Single(p => p.Id == "stamina.vaultMaxHeight");
        var carry = Catalog.Parameters.Single(p => p.Id == "stamina.maxCarryWeight");
        Check(vault.Slider is { Min: 70.0, Max: 300.0, Step: 1.0 },
            "Horizontal vault control must keep the native 70..300 uu / 1-uu steps");
        Check(carry.Slider is { Min: 20.0, Max: 160.0, Step: 5.0 },
            "Horizontal carry control must keep the native 20..160 kg / 5-kg steps");
        foreach (var (label, value) in new[]
        {
            ("WAIST", 130.0), ("EYES", 225.0), ("ABOVE EYES", 300.0)
        })
            Check(vault.Markers.Any(m => m.Label == label && m.Value == value),
                $"The horizontal vault ruler is missing its exact marker: {label}");

        var vm = new MainViewModel(Catalog);
        var stamina = vm.Tabs.Single(t => t.Id == "stamina");
        Check(stamina.Parameters.Any(p => p.Id == vault.Id && p.VisualAggregateFamily == "StaminaCore")
            && stamina.Parameters.Any(p => p.Id == carry.Id && p.VisualAggregateFamily == "StaminaCore"),
            "Vault height and carry weight must remain in the Core Stamina visual family");
        Console.WriteLine("MOVEMENT/EXHAUSTION: native IDs, slider ranges, landmarks and gamer captions preserved.");
    }


    private static void StaminaStatesLayoutContractCase()
    {
        // Real WPF control contract: responsive board height and all six
        // native source IDs remain bound to the independently editable family.
        var board = new ActionCostsRadar { Mode = "Exhaustion" };
        board.Measure(new System.Windows.Size(920.0, 1000.0));
        Check(Math.Abs(board.DesiredSize.Height - 330.0) < 0.000001,
            "Stamina States wide panel measurement drifted");
        board.Measure(new System.Windows.Size(720.0, 1000.0));
        Check(Math.Abs(board.DesiredSize.Height - 450.0) < 0.000001,
            "Stamina States narrow panel measurement drifted");

        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        foreach (var (id, min, max) in new[]
        {
            ("stamina.sprintDisableThreshold", 0.0, 32.2),
            ("stamina.lowRecoveryThreshold", 1.0, 60.0),
            ("stamina.aimRegenCoef", 0.0, 0.5),
            ("stamina.sprintRecoveryDelay", 0.0, 2.0),
            ("stamina.lowRecoveryStrength", 0.0, 20.0),
            ("stamina.lowRecoveryDuration", 0.5, 8.0)
        })
        {
            var p = vm.AllParameters.Single(p => p.Id == id);
            Check(p.IsSlider && p.IsControlEnabled &&
                  Math.Abs(p.Minimum - min) < 0.000001 &&
                  Math.Abs(p.Maximum - max) < 0.000001,
                "Stamina States changed original CFG-backed native limits: " + id);
        }
        var sprint = vm.AllParameters.Single(p => p.Id == "stamina.sprintDisableThreshold");
        var boost = vm.AllParameters.Single(p => p.Id == "stamina.lowRecoveryThreshold");
        sprint.SnapCurrentNumberToMarker(20.0);
        boost.SnapCurrentNumberToMarker(20.0);
        Check(Math.Abs(sprint.CurrentNumber - 20.0) < 0.000001
              && Math.Abs(boost.CurrentNumber - 20.0) < 0.000001,
            "Coincident Stamina Zones must remain two independent source values");
        sprint.SnapCurrentNumberToMarker(16.1);
        Check(Math.Abs(boost.CurrentNumber - 20.0) < 0.000001,
            "Editing sprint lock illegally moved low-SP boost threshold");
        _passed++;
        Console.WriteLine("PASS stamina-states-shared-zone-layout-and-source-independence");
    }

    private static void MedicalItemsPairingCase()
    {
        // All eight actual healing/bleeding source values remain independent
        // while their UI routes through exactly one visible medical card.
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        var idPairs = new[]
        {
            ("health.bandageHealing", "health.bandageBleeding"),
            ("health.medkitHealing", "health.medkitBleeding"),
            ("health.armyMedkitHealing", "health.armyMedkitBleeding"),
            ("health.ecoMedkitHealing", "health.ecoMedkitBleeding")
        };
        var anchor = vm.AllParameters.Single(p => p.Id == "health.bandageHealing");
        Check(anchor.IsMedicalHealingAnchor && anchor.IsControlVisible,
            "Unified Medical Items editor must retain its bandage-healing anchor");
        foreach (var (healId, bleedId) in idPairs)
        {
            var healing = vm.AllParameters.Single(p => p.Id == healId);
            var bleeding = vm.AllParameters.Single(p => p.Id == bleedId);
            Check(healing.IsMedicalHealingMember && bleeding.IsMedicalHealingMember,
                "Paired medical treatment must use one source-backed visual family");
            Check(!bleeding.IsControlVisible && !bleeding.IsVisualAggregateAnchor,
                "Bleeding source must not create a separate top-level card");
            if (healing != anchor)
                Check(!healing.IsControlVisible,
                    "All paired healing values must be rendered only in the medical card");
            Check(healing.IsSlider && bleeding.IsSlider,
                "Paired healing and bleeding must remain independent numeric sliders");
        }
        var radiationIds = new[]
        {
            ("health.ecoMedkitAntirad", 60.0, 120.0),
            ("health.antiRadRadiation", 100.0, 200.0)
        };
        foreach (var (id, baseline, maximum) in radiationIds)
        {
            var rad = vm.AllParameters.Single(p => p.Id == id);
            Check(rad.IsMedicalHealingMember && !rad.IsControlVisible
                  && !rad.IsVisualAggregateAnchor && rad.IsSlider,
                "Radiation Removal source must be an independent hidden member of Medical Items: " + id);
            Check(rad.ReferenceNumber == baseline && rad.Maximum == maximum
                  && rad.TickFrequency == 5.0,
                "Radiation Removal native range, step or original CFG baseline drifted: " + id);
        }
        // Alcohol is a real source effect, not decorative RAD columns.
        // The original signed -10/-50 must round-trip as two separate
        // positive RAD controls with source-true native upper limits.
        var beer = vm.AllParameters.Single(p => p.Id == "health.beerRadiationRemoval");
        var vodka = vm.AllParameters.Single(p => p.Id == "health.vodkaRadiationRemoval");
        Check(beer.IsMedicalHealingMember && vodka.IsMedicalHealingMember
              && !beer.IsControlVisible && !vodka.IsControlVisible,
            "Beer and Vodka must be editable only through the Medical Items RAD bank");
        Check(beer.ReferenceNumber == 10.0 && beer.Maximum == 30.0
              && beer.TickFrequency == 5.0
              && vodka.ReferenceNumber == 50.0 && vodka.Maximum == 100.0
              && vodka.TickFrequency == 5.0,
            "Beer/Vodka source maxima, step or CFG-original radiation values drifted");
        var food = vm.AllParameters.Single(p => p.Id == "health.foodHealing");
        Check(food.IsMedicalHealingMember && !food.IsControlVisible && food.IsToggle,
            "Food Healing must remain a checkbox in the same medical card");

        var bleed = vm.AllParameters.Single(p => p.Id == "health.medkitBleeding");
        Check(!anchor.IsModified, "Unedited medical group should not be marked modified");
        bleed.CurrentNumber = 20.0;
        Check(anchor.IsModified && bleed.IsValueModifiedFromReference(),
            "Changing a hidden bleeding child must mark its visible medical group modified");
        bleed.ResetToReference();
        Check(!anchor.IsModified,
            "Resetting a bleeding child must clear the medical group modified state");

        var scientificRad = vm.AllParameters.Single(p => p.Id == "health.ecoMedkitAntirad");
        var antiRad = vm.AllParameters.Single(p => p.Id == "health.antiRadRadiation");
        scientificRad.CurrentNumber = 65.0;
        Check(anchor.IsModified && scientificRad.IsValueModifiedFromReference()
              && !antiRad.IsValueModifiedFromReference(),
            "Editing Scientific radiation must modify only its own source and shared card");
        scientificRad.ResetToReference();
        Check(!anchor.IsModified, "Radiation reset should clear composite modified state");
        antiRad.CurrentNumber = 105.0;
        Check(anchor.IsModified && antiRad.IsValueModifiedFromReference()
              && !scientificRad.IsValueModifiedFromReference(),
            "Editing Anti-Rad must modify only its own CFG source and shared card");
        antiRad.ResetToReference();
        Check(!anchor.IsModified, "Anti-Rad reset should clear modified medical card");
        beer.CurrentNumber = 30.0;
        Check(beer.CurrentNumber == 30.0,
            "Beer radiation removal must reach 3x / 30 RAD");
        Check(anchor.IsModified && beer.IsValueModifiedFromReference()
              && !vodka.IsValueModifiedFromReference(),
            "Beer RAD removal must independently edit the Medical Items group");
        beer.ResetToReference();
        vodka.CurrentNumber = 55.0;
        Check(anchor.IsModified && vodka.IsValueModifiedFromReference()
              && !beer.IsValueModifiedFromReference(),
            "Vodka RAD removal must independently edit the Medical Items group");
        vodka.ResetToReference();
        Check(!anchor.IsModified, "Resetting Beer/Vodka must clear group modification");
        _passed++;
        Console.WriteLine("PASS medical-items-4x2-plus-radiation-4-vertical-membership");
    }

    private static void BleedingTimelineProjectionCase()
    {
        static BleedTimelineResult Run(double hp = 100, double hpPct = 100,
            double bleed = 80, double regen = 0,
            double low = 1, double medium = 1.5, double severe = 2,
            double midAt = 50, double severeAt = 75)
            => BleedingTimelineProjection.Calculate(
                hp, hpPct, bleed, regen, low, medium, severe, midAt, severeAt);

        var severeOnly = Run();
        Check(severeOnly.DeathSecond == 50 && severeOnly.Hits == 50
              && severeOnly.Samples[^1].Hp == 0,
            "Severe 2 HP per 1s hypothetical tick must kill 100 HP in 50 ticks");

        var halfHp = Run(hpPct: 50);
        Check(halfHp.DeathSecond == 25 && halfHp.Hits == 25,
            "Start-HP preview percentage must modify tick/death projections");

        var doubleHp = Run(hp: 200);
        Check(doubleHp.DeathSecond == 100 && doubleHp.Hits == 100,
            "Changing live health.maxHP must update hypothetical death time");

        var recovery = Run(bleed: 10, regen: 0.6);
        Check(recovery.DeathSecond is null
              && recovery.BleedEndsSecond == 17 && recovery.Hits == 17
              && recovery.Samples[^1].Hp == 83,
            "Recovery should end low bleeding without inventing a death");

        var transitions = Run(regen: 0.3);
        Check(transitions.Samples.Any(t => t.Stage == "SEVERE")
              && transitions.Samples.Any(t => t.Stage == "MEDIUM")
              && transitions.DeathSecond is not null,
            "Natural recovery must cross live bleeding threshold stages");

        var harmless = Run(low: 0, medium: 0, severe: 0);
        Check(harmless.DeathSecond is null && harmless.Hits == 0
              && harmless.Samples[^1].Hp == 100,
            "Zero HP damage must never invent death or HP charges");

        var empty = Run(bleed: 0);
        Check(empty.DeathSecond is null && empty.BleedEndsSecond == 0
              && empty.Hits == 0 && empty.Duration == 0,
            "No starting bleed should show no damage and zero elapsed time");

        var milestones = BleedingTimelineProjection.TimelineMarkers(severeOnly);
        Check(milestones.Count == 3
              && milestones[0].Hits == 1 && milestones[0].Second == 1
              && milestones[1].Hits == 25 && milestones[1].Second == 25
              && milestones[2].Hits == 50 && milestones[2].Second == 50,
            "Starvation-style Bleeding rail must use real first/middle/last ticks");
        Check(BleedingTimelineProjection.TimelineMarkers(empty).Count == 0
              && BleedingTimelineProjection.TimelineMarkers(harmless).Count == 0,
            "No-damage cases must not invent tick landmarks");
        var single = BleedingTimelineProjection.TimelineMarkers(Run(hp: 1));
        Check(single.Count == 1 && single[0].Hits == 1 && single[0].Second == 1,
            "Single lethal HP hit must not be presented as three events");
        var phaseCase = Run(hp: 200, bleed: 80, regen: 1);
        var phase = BleedingTimelineProjection.StageSegments(phaseCase);
        Check(phase.Count == 3
              && phase[0].Stage == "SEVERE" && phase[0].FirstSecond == 1
              && phase[0].LastSecond == 6 && phase[0].DamageTicks == 6
              && phase[1].Stage == "MEDIUM" && phase[1].FirstSecond == 7
              && phase[1].LastSecond == 31 && phase[1].DamageTicks == 25
              && phase[2].Stage == "LOW" && phase[2].FirstSecond == 32
              && phase[2].LastSecond == 80 && phase[2].DamageTicks == 49,
            "Bleeding time bands must follow all live Low/Medium/Severe phases");
        var marks = BleedingTimelineProjection.TimelineMarkers(phaseCase);
        Check(marks.Count == 4 && marks[0].Second == 1
              && marks[1].Second == 7 && marks[2].Second == 32
              && marks[3].Second == 80,
            "Stage start hit markers and the final hit must be real, not invented");

        var shifted = BleedingTimelineProjection.StageSegments(
            Run(hp: 200, bleed: 80, regen: 1, severeAt: 70));
        Check(shifted[0].LastSecond == 11 && shifted[1].FirstSecond == 12,
            "Moving severe onset immediately moves timeline boundaries");

        var faster = BleedingTimelineProjection.StageSegments(
            Run(hp: 200, bleed: 80, regen: 2));
        Check(faster[0].LastSecond == 3 && faster[1].FirstSecond == 4,
            "Changing bleeding recovery resizes timeline stage durations");

        var noLowHp = BleedingTimelineProjection.StageSegments(
            Run(hp: 200, bleed: 80, regen: 1, low: 0));
        Check(noLowHp.Count == 3 && noLowHp[2].Stage == "LOW"
              && noLowHp[2].DamageTicks == 0 && noLowHp[2].HpLost == 0,
            "Zero damage Low phase remains visibly represented");

        Console.WriteLine("PASS BleedingTimelineProjection 15 scenarios");
    }

    private static void BandageBleedingUpperBoundCase()
    {
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        var bandage = vm.AllParameters.Single(p => p.Id == "health.bandageBleeding");
        Check(bandage.IsSlider && bandage.Minimum == 0.0
              && bandage.Maximum == 100.0 && bandage.ReferenceNumber == 100.0
              && bandage.TickFrequency == 5.0,
            "Bandage Bleeding maximum must equal vanilla 1x / 100 PTS");
        foreach (var (id, maximum) in new[]
        {
            ("health.medkitBleeding", 30.0),
            ("health.armyMedkitBleeding", 70.0),
            ("health.ecoMedkitBleeding", 50.0)
        })
            Check(vm.AllParameters.Single(p => p.Id == id).Maximum == maximum,
                id + ": the common 100-PTS ruler must not enlarge native maxima");

        bandage.ApplyPresetValue(new PresetValue { Kind = "slider", Number = 200.0 });
        Check(bandage.CurrentNumber == 100.0,
            "Old Bandage 200-PTS presets must clamp to 1x");
        bandage.SnapCurrentNumberToMarker(200.0);
        Check(bandage.CurrentNumber == 100.0,
            "Marker setter must not bypass Bandage ceiling");
        bandage.CurrentNumber = 95.0;
        Check(bandage.CurrentNumber == 95.0 && bandage.IsValueModifiedFromReference(),
            "Bandage must keep native 5-PTS editing below 1x");
        bandage.ResetToReference();
        Check(bandage.CurrentNumber == 100.0,
            "Bandage reset must restore original 1x");
        _passed++;
        Console.WriteLine("PASS bandage-bleeding-1x-cap");
    }

    private static void MasterParentToggleCase(string toggleId, string removedMember)
    {
        var label = "Master-mechanics-" + toggleId.Replace(".", "-", StringComparison.Ordinal);
        var output = Path.Combine(_keepRoot ?? Path.GetTempPath(),
            "TCD_Health_Stamina", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Master" };
            var toggle = vm.AllParameters.Single(p => p.Id == toggleId);
            Check(toggle.IsToggle && toggle.ReferenceBoolean && toggle.CurrentBoolean,
                label + ": Master mechanics parent no longer has its original ON baseline");
            toggle.CurrentBoolean = false;
            Check(toggle.IsValueModifiedFromReference(),
                label + ": deliberate parent change did not register");

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, output);
            Check(result.FileCount == 1 && result.ParameterCount == 1
                  && result.EditCount == 5,
                label + ": complete four-member list replacement expected (5 edits)");
            var file = ExpectedFile(output, "GameData/DifficultyPrototypes.cfg");
            var document = Parse(File.ReadAllText(file));
            Check(document.Children.Count == 1
                  && document.Children.ContainsKey("Stalker"),
                label + ": wrong Master CFG SID");
            var root = document.Get("Stalker");
            var environment = root.Get("EnvironmentDifficulty");
            Check(root.Bpatch && environment.Bpatch
                  && environment.Children.Count == 1
                  && environment.Assignments.Count == 0,
                label + ": parent replacement has unrelated assignments");
            var list = environment.Get("AdditionalMechanicsEffects");
            Check(!list.Bpatch && list.Children.Count == 0
                  && list.Assignments.Count == 4,
                label + ": mechanics array must be a four-entry full replacement");
            var vanillaMembers = new[]
            {
                "HungerNegativeMechanics",
                "RadiationNegativeMechanics",
                "SleepinessNegativeMechanics",
                "RegenHealthNegativeMechanics",
                "MasterEffectModifierNegativeMechanics"
            };
            var expected = vanillaMembers.Where(s => s != removedMember).ToArray();
            for (var index = 0; index < expected.Length; index++)
                Check(list.Value($"[{index}]") == expected[index],
                    label + ": original mechanics list order/member mismatch at " + index);

            _passed++;
            Console.WriteLine("PASS " + label + " (full 4-item mechanics list replacement)");
        }
        finally
        {
            if (_keepRoot is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private static void Test(string id, string difficulty)
    {
        var label = difficulty + "-" + id.Replace(".", "-", StringComparison.Ordinal);
        var output = Path.Combine(_keepRoot ?? Path.GetTempPath(), "TCD_Health_Stamina", label);
        try
        {
            var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = difficulty };
            var parameter = vm.AllParameters.Single(p => p.Id == id);
            Check(parameter.IsControlEnabled, label + ": backing UI control is disabled");
            Check(!parameter.IsValueModifiedFromReference(), label + ": baseline starts edited");

            if (parameter.IsToggle)
                parameter.CurrentBoolean = !parameter.ReferenceBoolean;
            else
            {
                Check(parameter.IsSlider, label + ": unsupported catalog editor");
                // Marker setter changes the real CurrentNumber and triggers
                // any WPF-linked value constraints without rounding drift.
                var step = Math.Max(0.01, parameter.Definition.Slider?.Step ?? 0.1);
                var candidate = id == "health.beerRadiationRemoval"
                    ? parameter.Maximum : parameter.ReferenceNumber + step;
                if (candidate > parameter.Maximum)
                    candidate = parameter.ReferenceNumber - step;
                parameter.SnapCurrentNumberToMarker(candidate);
            }
            Check(parameter.IsValueModifiedFromReference(), label + ": value did not change");
            Check(parameter.IsControlEnabled, label + ": value change disabled control");

            // The direct route inventory is evaluated by the actual selected
            // difficulty SID, not the human-facing text used in the UI.
            var sid = Catalog.DifficultyProfiles[difficulty].Sid;
            var points = Baseline.EditPoints
                .Where(p => p.ParameterId == id
                         && (string.IsNullOrWhiteSpace(p.DifficultySid)
                             || p.DifficultySid == sid))
                .ToArray();
            Check(points.Length > 0, label + ": no direct writer edit points");
            Check(points.All(p => p.Scope == "baseGame"),
                label + ": Health/Stamina moved out of baseGame");

            var modified = vm.AllParameters
                .Where(p => (p.Id.StartsWith("health.", StringComparison.Ordinal)
                          || p.Id.StartsWith("stamina.", StringComparison.Ordinal))
                         && p.IsValueModifiedFromReference())
                .Select(p => p.Id).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            Check(modified.SequenceEqual(new[] { id }),
                label + ": a slider change unexpectedly changed other Health/Stamina values");

            var result = new CfgPatchWriterService().Write(vm, Catalog, null, output);
            var expectedFiles = points.Select(p => ExpectedFile(output, p.File))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            Check(result.EditCount == points.Length
                  && result.ParameterCount == 1
                  && result.FileCount == expectedFiles.Length,
                label + ": wrong edit/parameter/file totals ("
                + result.EditCount + "/" + result.ParameterCount + "/" + result.FileCount
                + "; expected " + points.Length + "/1/" + expectedFiles.Length + ")");
            var actualFiles = Directory.GetFiles(output, "*TrueCustomDifficulty*",
                SearchOption.AllDirectories);
            Check(actualFiles.Length == expectedFiles.Length
                  && actualFiles.ToHashSet(StringComparer.OrdinalIgnoreCase)
                      .SetEquals(expectedFiles),
                label + ": wrong CFG output paths or additional files");

            foreach (var file in expectedFiles)
            {
                Check(File.Exists(file), label + ": expected CFG not written: " + file);
                var document = Parse(File.ReadAllText(file));
                var editPoints = points.Where(p => ExpectedFile(output, p.File) == file).ToArray();
                var expectedPaths = editPoints.Select(p =>
                    p.Prototype + "/" + (p.NodePath.Count > 0
                        ? string.Join("/", p.NodePath) + "/" : "") + p.Key)
                    .ToHashSet(StringComparer.Ordinal);
                var observedPaths = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in document.Children.Values)
                    Collect(root, root.Name, observedPaths);
                Check(expectedPaths.SetEquals(observedPaths),
                    label + ": unrelated or missing BPatch fields "
                    + "expected={" + string.Join(", ", expectedPaths) + "} observed={"
                    + string.Join(", ", observedPaths) + "}");

                foreach (var point in editPoints)
                {
                    var root = document.Get(point.Prototype);
                    Check(root.Bpatch, label + ": prototype must be {bpatch}");
                    var node = root;
                    foreach (var segment in point.NodePath)
                    {
                        node = node.Get(segment);
                        Check(node.Bpatch,
                            label + ": nested source path " + segment + " not {bpatch}");
                    }
                    var actual = node.Value(point.Key);
                    var expected = ExpectedValue(parameter, point);
                    if (parameter.IsToggle && point.Operation == "set")
                        Check(actual == (parameter.CurrentBoolean ? "true" : "false"),
                            label + ": boolean output mismatch");
                    else
                        Approx(actual, expected, label + ": " + point.Key);

                    // In particular, Player.VaultingParams may be inherited
                    // from the original [0] prototype. Test the generated
                    // structure; runtime semantics remain NOT VERIFIED.
                    if (id == "stamina.vaultMaxHeight")
                        Check(root.Name == "Player" && node.Name == "VaultingParams"
                              && point.Key == "MaxObstacleHeight",
                            label + ": inherited VaultingParams path changed");
                }
            }
            _passed++;
            Console.WriteLine("PASS " + label + " (" + points.Length + " CFG edits)");
        }
        finally
        {
            if (_keepRoot is null && Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    private static string ExpectedFile(string root, string source)
    {
        var file = Path.GetFileName(source);
        var stem = Path.GetFileNameWithoutExtension(file);
        var parent = Path.GetDirectoryName(source.Replace('/', Path.DirectorySeparatorChar))
                     ?? string.Empty;
        var location = Path.Combine(root, "Stalker2", "Content", "GameLite", parent);
        if (file.EndsWith("Prototypes.cfg", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(location, stem, stem + "_patch_TrueCustomDifficulty.cfg");
        return Path.Combine(location, file + "_patch_TrueCustomDifficulty");
    }

    private static double ExpectedValue(ParameterViewModel parameter, CfgWriterEditPoint point)
    {
        return point.Operation switch
        {
            "set" => parameter.CurrentNumber,
            "negateSet" => -parameter.CurrentNumber,
            "toggleSourceOrZero" => parameter.CurrentBoolean ? ParseRaw(point.SourceRaw) : 0.0,
            _ => throw new InvalidDataException(
                point.ParameterId + ": unexpected Health/Stamina operation " + point.Operation)
        };
    }

    private static double ParseRaw(string raw)
    {
        var txt = raw.Trim();
        var percent = txt.EndsWith("%", StringComparison.Ordinal);
        if (percent) txt = txt[..^1];
        if (txt.EndsWith("f", StringComparison.OrdinalIgnoreCase)) txt = txt[..^1];
        return double.Parse(txt, CultureInfo.InvariantCulture) / (percent ? 100.0 : 1.0);
    }

    private static void Approx(string raw, double expected, string context)
    {
        var actual = ParseRaw(raw);
        Check(Math.Abs(actual - expected) < 0.000001,
            context + ": output " + actual + ", expected " + expected);
    }

    private static void Collect(Node node, string prefix, HashSet<string> fields)
    {
        foreach (var key in node.Assignments.Keys)
            Check(fields.Add(prefix + "/" + key), "duplicate field " + prefix + "/" + key);
        foreach (var child in node.Children.Values)
            Collect(child, prefix + "/" + child.Name, fields);
    }

    private static Node Parse(string source)
    {
        var root = new Node("document", "");
        var stack = new Stack<Node>();
        stack.Push(root);
        foreach (var lineRaw in source.Replace("\r", "").Split('\n'))
        {
            var line = lineRaw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (line == "struct.end")
            {
                Check(stack.Count > 1, "Unmatched struct.end in CFG output");
                stack.Pop();
                continue;
            }
            var start = Regex.Match(line, @"^(?<name>\S+)\s*:\s*struct\.begin(?<flags>.*)$");
            if (start.Success)
            {
                var node = new Node(start.Groups["name"].Value, start.Groups["flags"].Value);
                Check(stack.Peek().Children.TryAdd(node.Name, node),
                    "Duplicate CFG section " + node.Name);
                stack.Push(node);
                continue;
            }
            var assignment = Regex.Match(line, @"^(?<key>\S+)\s*=\s*(?<value>.*)$");
            Check(assignment.Success, "Unparsed CFG statement: " + line);
            Check(stack.Peek().Assignments.TryAdd(
                assignment.Groups["key"].Value, assignment.Groups["value"].Value),
                "Duplicate CFG field " + line);
        }
        Check(stack.Count == 1, "Missing struct.end in CFG output");
        return root;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private sealed class Node(string name, string flags)
    {
        public string Name { get; } = name;
        public bool Bpatch { get; } = flags.Contains("{bpatch}", StringComparison.Ordinal);
        public Dictionary<string, Node> Children { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Assignments { get; } = new(StringComparer.Ordinal);
        public Node Get(string segment) =>
            Children.TryGetValue(segment, out var node) ? node :
            throw new InvalidDataException($"Missing CFG {Name}/{segment}");
        public string Value(string field) =>
            Assignments.TryGetValue(field, out var value) ? value :
            throw new InvalidDataException($"Missing CFG {Name}.{field}");
    }
}
