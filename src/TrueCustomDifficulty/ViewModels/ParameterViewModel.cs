using System.Globalization;
using System.Text.Json;
using TrueCustomDifficulty.Infrastructure;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.ViewModels;

public sealed class ParameterViewModel : ObservableObject
{
    private const double Epsilon = 0.000001;
    private const double PercentageStep = 0.05; // 5 percentage-point UI steps.
    private const double NpcWeaponConditionVanillaMin = 0.25;
    private const double NpcWeaponConditionVanillaMax = 0.50;

    private double _currentNumber;
    private double _currentMinNumber;
    private double _currentMaxNumber;
    private bool _currentBoolean;
    private string _currentText = string.Empty;
    private string _referenceDifficulty;
    private Func<double>? _effectiveDamageMultiplierProvider;
    private Func<bool>? _dependencyEnabledProvider;
    private Func<bool>? _aggregateModifiedProvider;

    public ParameterDefinition Definition { get; }
    public string Id => Definition.Id;
    public string Label => Definition.Label;
    public string Description => Definition.Description;
    public string Rationale => string.IsNullOrWhiteSpace(Definition.Rationale) ? Definition.Description : Definition.Rationale;
    public string RationaleDisplay
    {
        get
        {
            var description = Description.Trim();
            var rationale = Rationale.Trim();

            string body;
            if (string.IsNullOrWhiteSpace(description))
                body = rationale;
            else if (string.IsNullOrWhiteSpace(rationale) || string.Equals(description, rationale, StringComparison.Ordinal))
                body = description;
            else
            {
                // The old hover tooltip exposed Description while the side panel exposed Rationale.
                // Keep both pieces visible in one persistent side-panel explanation instead of hiding
                // useful information behind hover-only UI.
                body = $"{description}{Environment.NewLine}{Environment.NewLine}{rationale}";
            }

            return $"{body}{Environment.NewLine}{Environment.NewLine}{ControlHelp}";
        }
    }

    public string ControlHelp
    {
        get
        {
            if (IsAggregateRadarAnchor)
            {
                return "CONTROLS" + Environment.NewLine +
                       "Mouse: drag a radar point to make that setting higher or lower. Hover a legend name to highlight the same point, scroll over the name to fine-tune it, and right-click the point or name to return only that setting to your Base Preset." + Environment.NewLine +
                       "Keyboard: choose a radar point, then use Arrow keys or +/- to fine-tune it. Press R to return the whole radar to your Base Preset.";
            }

            if (IsRange)
            {
                return "CONTROLS" + Environment.NewLine +
                       "Mouse: drag MIN and MAX to choose the range. Click a vanilla marker to copy that vanilla setting. Right-click either slider to return the whole range to your Base Preset." + Environment.NewLine +
                       "Keyboard: choose MIN or MAX, use Arrow keys for small changes, Page Up/Down for larger changes, and Home/End for the lowest/highest allowed setting.";
            }

            if (IsChoiceSlider)
            {
                return "CONTROLS" + Environment.NewLine +
                       "Mouse: drag or click the slider to choose an option. Click a vanilla marker to copy that vanilla choice. Right-click the slider to return to your Base Preset." + Environment.NewLine +
                       "Keyboard: use Arrow keys to move one choice at a time, Page Up/Down to move faster, and Home/End to jump to the first/last choice.";
            }

            if (IsToggle)
            {
                return "CONTROLS" + Environment.NewLine +
                       "Mouse: click the switch to turn this gameplay rule on or off." + Environment.NewLine +
                       "Keyboard: focus the switch and press Space to toggle it.";
            }

            return "CONTROLS" + Environment.NewLine +
                   "Mouse: drag or click the slider to change the setting. Click a vanilla marker to copy that vanilla setting, scroll over the number on the right to fine-tune it, and right-click the slider or number to return to your Base Preset." + Environment.NewLine +
                   "Keyboard: use Arrow keys for small changes, Page Up/Down for larger changes, and Home/End for the lowest/highest allowed setting.";
        }
    }
    public string Logic => Definition.Logic;
    public bool HasLogic => !string.IsNullOrWhiteSpace(Logic);
    public string Interaction => Definition.Interaction;
    public bool HasInteraction => !string.IsNullOrWhiteSpace(Interaction);
    public string Group => Definition.Group;
    public string Control => Definition.Control;
    public string SupportStatus => Definition.Support.Status;
    public IReadOnlyList<ChoiceOptionViewModel> ChoiceOptions { get; }
    public IReadOnlyList<VanillaMarkerViewModel> ChoiceMarkers { get; }
    public IReadOnlyList<VanillaMarkerViewModel> VanillaMarkers { get; }
    public bool IsSlider => Control == "slider";
    public bool IsRange => Control == "range";
    public bool IsToggle => Control == "toggle";
    // The only string-valued gameplay control is rendered as a discrete slider.
    public bool IsChoiceSlider => Control == "choiceSlider";

    private static readonly HashSet<string> AmmoDistributionParameterIds = new(StringComparer.Ordinal)
    {
        "loot.ammoBoxWeight918",
        "loot.ammoBoxWeight045",
        "loot.ammoBoxWeight545",
        "loot.ammoBoxWeight012"
    };

    public bool IsAmmoDistributionMember => AmmoDistributionParameterIds.Contains(Id);
    public bool IsAmmoDistributionAnchor => Id == "loot.ammoBoxWeight918";

    private static readonly HashSet<string> StashAmmoParameterIds = new(StringComparer.Ordinal)
    {
        "loot.cheapStashAmmoQuantity",
        "loot.mediumStashAmmoQuantity",
        "loot.expensiveStashAmmoQuantity"
    };

    public bool IsStashAmmoMember => StashAmmoParameterIds.Contains(Id);
    public bool IsStashAmmoAnchor => Id == "loot.cheapStashAmmoQuantity";

    private static readonly HashSet<string> MutantDropParameterIds = new(StringComparer.Ordinal)
    {
        "loot.mutant.blindDogChance",
        "loot.mutant.bloodsuckerChance",
        "loot.mutant.boarChance",
        "loot.mutant.burerChance",
        "loot.mutant.catChance",
        "loot.mutant.chimeraChance",
        "loot.mutant.controllerChance",
        "loot.mutant.deerChance",
        "loot.mutant.fleshChance",
        "loot.mutant.poltergeistChance",
        "loot.mutant.pseudodogChance",
        "loot.mutant.pseudogiantChance",
        "loot.mutant.snorkChance",
        "loot.mutant.tushkanChance"
    };

    public bool IsMutantDropMember => MutantDropParameterIds.Contains(Id);
    public bool IsMutantDropAnchor => Id == "loot.mutant.blindDogChance";

    private static readonly HashSet<string> IndividualAnomalyDamageParameterIds = new(StringComparer.Ordinal)
    {
        "environment.electroAnomalyDamage",
        "environment.chemicalAnomalyDamage",
        "environment.fireAnomalyDamage",
        "environment.carouselAnomalyDamage",
        "environment.toxicCloudDamage",
        "environment.steamAnomalyDamage",
        "environment.clickerHealthDamage",
        "environment.razorHealthDamage",
        "environment.soapBubbleHealthDamage",
        "environment.fireballHealthDamage",
        "environment.diamondHealthDamage"
    };

    public bool IsIndividualAnomalyDamageMember => IndividualAnomalyDamageParameterIds.Contains(Id);
    public bool IsIndividualAnomalyDamageAnchor => Id == "environment.electroAnomalyDamage";

    private static readonly HashSet<string> WorldLootRemovalParameterIds = new(StringComparer.Ordinal)
    {
        "loot.worldAmmoCratesRemoved",
        "loot.worldF1GrenadeBoxes",
        "loot.worldFoodDrinkCratesRemoved",
        "loot.worldMedicalCratesRemoved",
        "loot.looseWorldLootRemoved"
    };

    public bool IsWorldLootRemovalMember => WorldLootRemovalParameterIds.Contains(Id);
    public bool IsWorldLootRemovalAnchor => Id == "loot.worldAmmoCratesRemoved";

    private static readonly HashSet<string> MedicalHealingParameterIds = new(StringComparer.Ordinal)
    {
        "health.bandageHealing",
        "health.medkitHealing",
        "health.armyMedkitHealing",
        "health.ecoMedkitHealing"
    };

    public bool IsMedicalHealingMember => MedicalHealingParameterIds.Contains(Id);
    public bool IsMedicalHealingAnchor => Id == "health.bandageHealing";

    private static readonly HashSet<string> ActionCostParameterIds = new(StringComparer.Ordinal)
    {
        "stamina.sprintCost",
        "stamina.jumpCost",
        "stamina.vaultCost",
        "stamina.meleeNormalCost",
        "stamina.meleeStrongCost",
        "stamina.butstockCost",
        "stamina.aimRegenCoef"
    };

    public bool IsActionCostMember => ActionCostParameterIds.Contains(Id);
    public bool IsActionCostAnchor => Id == "stamina.sprintCost";

    private static readonly HashSet<string> ItemPriceScalerParameterIds = new(StringComparer.Ordinal)
    {
        "economy.consumableCost",
        "economy.ammoCost",
        "economy.armorCost",
        "economy.weaponCost",
        "economy.artifactCost",
        "economy.mutantPartValue"
    };

    public bool IsItemPriceScalerMember => ItemPriceScalerParameterIds.Contains(Id);
    public bool IsItemPriceScalerAnchor => Id == "economy.consumableCost";

    private static readonly HashSet<string> MutantHealthParameterIds = new(StringComparer.Ordinal)
    {
        "combat.mutantHealth.bloodsucker",
        "combat.mutantHealth.chimera",
        "combat.mutantHealth.deer",
        "combat.mutantHealth.pseudogiant",
        "combat.mutantHealth.pseudodog",
        "combat.mutantHealth.burer",
        "combat.mutantHealth.bayun",
        "combat.mutantHealth.controller"
    };

    public bool IsMutantHealthMember => MutantHealthParameterIds.Contains(Id);
    public bool IsMutantHealthAnchor => Id == "combat.mutantHealth.bloodsucker";

    private static readonly HashSet<string> SpecialAbilityCooldownParameterIds = new(StringComparer.Ordinal)
    {
        "combat.mutantCooldown.bloodsucker",
        "combat.mutantCooldown.chimera",
        "combat.mutantCooldown.deer",
        "combat.mutantCooldown.pseudogiant",
        "combat.mutantCooldown.pseudodog",
        "combat.mutantCooldown.burer",
        "combat.mutantCooldown.bayun",
        "combat.mutantCooldown.controller"
    };

    public bool IsSpecialAbilityCooldownMember => SpecialAbilityCooldownParameterIds.Contains(Id);
    public bool IsSpecialAbilityCooldownAnchor => Id == "combat.mutantCooldown.bloodsucker";

    private static readonly HashSet<string> HumanGroupSizeParameterIds = new(StringComparer.Ordinal)
    {
        "alife.humanGroupSize.newbie",
        "alife.humanGroupSize.experienced",
        "alife.humanGroupSize.veteran",
        "alife.humanGroupSize.master"
    };

    public bool IsHumanGroupSizeMember => HumanGroupSizeParameterIds.Contains(Id);
    public bool IsHumanGroupSizeAnchor => Id == "alife.humanGroupSize.newbie";

    private static readonly HashSet<string> MutantGroupSizeParameterIds = new(StringComparer.Ordinal)
    {
        "alife.mutantSpawn.bloodsucker",
        "alife.mutantSpawn.blindDog",
        "alife.mutantSpawn.flesh",
        "alife.mutantSpawn.controller",
        "alife.mutantSpawn.poltergeist",
        "alife.mutantSpawn.snork",
        "alife.mutantSpawn.boar",
        "alife.mutantSpawn.chimera",
        "alife.mutantSpawn.bayun",
        "alife.mutantSpawn.tushkan",
        "alife.mutantSpawn.ratSwarm",
        "alife.mutantSpawn.burer",
        "alife.mutantSpawn.pseudodog",
        "alife.mutantSpawn.deer"
    };

    public bool IsMutantGroupSizeMember => MutantGroupSizeParameterIds.Contains(Id);
    public bool IsMutantGroupSizeAnchor => Id == "alife.mutantSpawn.bloodsucker";

    public bool IsAggregateRadarAnchor =>
        IsAmmoDistributionAnchor ||
        IsStashAmmoAnchor ||
        IsMutantDropAnchor ||
        IsIndividualAnomalyDamageAnchor ||
        IsWorldLootRemovalAnchor ||
        IsMedicalHealingAnchor ||
        IsActionCostAnchor ||
        IsItemPriceScalerAnchor ||
        IsMutantHealthAnchor ||
        IsSpecialAbilityCooldownAnchor ||
        IsHumanGroupSizeAnchor ||
        IsMutantGroupSizeAnchor;

    public bool HasEffectiveDamage => _effectiveDamageMultiplierProvider is not null;
    public bool IsControlEnabled => _dependencyEnabledProvider?.Invoke() ?? true;
    // Radar backing parameters remain in the live VM/preset/writer graph but are
    // collapsed as individual rows. Each radar family has one visible aggregate anchor.
    public bool IsControlVisible =>
        !Id.Equals("alife.mutantSpawn.generic", StringComparison.Ordinal)
        && (!IsAmmoDistributionMember || IsAmmoDistributionAnchor)
        && (!IsStashAmmoMember || IsStashAmmoAnchor)
        && (!IsMutantDropMember || IsMutantDropAnchor)
        && (!IsIndividualAnomalyDamageMember || IsIndividualAnomalyDamageAnchor)
        && (!IsWorldLootRemovalMember || IsWorldLootRemovalAnchor)
        && (!IsMedicalHealingMember || IsMedicalHealingAnchor)
        && (!IsActionCostMember || IsActionCostAnchor)
        && (!IsItemPriceScalerMember || IsItemPriceScalerAnchor)
        && (!IsMutantHealthMember || IsMutantHealthAnchor)
        && (!IsSpecialAbilityCooldownMember || IsSpecialAbilityCooldownAnchor)
        && (!IsHumanGroupSizeMember || IsHumanGroupSizeAnchor)
        && (!IsMutantGroupSizeMember || IsMutantGroupSizeAnchor)
        && (_dependencyEnabledProvider?.Invoke() ?? true);
    public string ToggleStateLabel => CurrentBoolean ? "ON" : "OFF";
    public double Minimum => Definition.Slider?.Min ?? 0;
    public double Maximum => Definition.Slider?.Max ?? 1;
    public double ChoiceMinimum => 0;
    public double ChoiceMaximum => Math.Max(0, ChoiceOptions.Count - 1);
    public double ChoiceTickFrequency => 1.0;

    // Percentage controls move in 5 percentage-point increments. Point-based
    // controls can opt into an absolute catalog step (for example 0.25 SP or
    // 5 bleeding points) so the slider never produces awkward tiny decimals.
    public double TickFrequency
    {
        get
        {
            if (UsesAbsoluteCatalogStep())
                return Definition.Slider?.Step ?? 0.01;

            if (Definition.Display?.Primary == "percent")
                return PercentageStep;

            if (Definition.Display?.Primary is "percentOfVanilla" or "multiplierOfVanilla")
            {
                var reference = Math.Abs(GetReferenceNumber());
                if (reference > Epsilon)
                {
                    if (Definition.Display?.Primary == "multiplierOfVanilla")
                        return reference * 0.01; // 0.01x steps.

                    return reference * PercentageStep;
                }
            }

            return Definition.Slider?.Step ?? 0.01;
        }
    }


    public event EventHandler? StateChanged;

    public ParameterViewModel(ParameterDefinition definition, string referenceDifficulty)
    {
        Definition = definition;
        _referenceDifficulty = referenceDifficulty;
        ChoiceOptions = BuildChoiceOptions(definition);
        ChoiceMarkers = definition.Control == "choiceSlider"
            ? BuildChoiceVanillaMarkers(definition, ChoiceOptions)
            : Array.Empty<VanillaMarkerViewModel>();
        VanillaMarkers = definition.Id == "loot.droppedWeaponCondition"
            ? Array.Empty<VanillaMarkerViewModel>()
            : BuildVanillaMarkers(definition);
        ResetToReference(false);
    }

    public double CurrentNumber
    {
        get => _currentNumber;
        set
        {
            var normalized = NormalizeSliderValue(value);
            if (SetProperty(ref _currentNumber, normalized))
                NotifyDerived();
        }
    }

    /// <summary>
    /// Snaps a numeric slider to an exact visible reference-marker value. Unlike normal
    /// drag input this deliberately bypasses the current UI tick rounding, because a marker
    /// represents an exact source/vanilla value that may not be an integer percentage of the
    /// currently selected Base Preset. PropertyChanged still fires as CurrentNumber, so linked
    /// slider constraints continue to cascade normally.
    /// </summary>
    public void SnapCurrentNumberToMarker(double value)
    {
        var clamped = Math.Clamp(value, Minimum, Maximum);
        if (SetProperty(ref _currentNumber, clamped, nameof(CurrentNumber)))
            NotifyDerived();
    }

    public double CurrentMinNumber
    {
        get => _currentMinNumber;
        set
        {
            var normalized = NormalizeRangeValue(value);
            if (normalized > _currentMaxNumber)
                _currentMaxNumber = normalized;

            if (SetProperty(ref _currentMinNumber, normalized))
            {
                OnPropertyChanged(nameof(CurrentMaxNumber));
                NotifyDerived();
            }
        }
    }

    public double CurrentMaxNumber
    {
        get => _currentMaxNumber;
        set
        {
            var normalized = NormalizeRangeValue(value);
            if (normalized < _currentMinNumber)
                _currentMinNumber = normalized;

            if (SetProperty(ref _currentMaxNumber, normalized))
            {
                OnPropertyChanged(nameof(CurrentMinNumber));
                NotifyDerived();
            }
        }
    }

    public void SnapCurrentMinNumberToMarker(double value)
    {
        var clamped = Math.Clamp(value, Minimum, Maximum);
        if (clamped > _currentMaxNumber)
            _currentMaxNumber = clamped;

        if (SetProperty(ref _currentMinNumber, clamped, nameof(CurrentMinNumber)))
        {
            OnPropertyChanged(nameof(CurrentMaxNumber));
            NotifyDerived();
        }
    }

    public void SnapCurrentMaxNumberToMarker(double value)
    {
        var clamped = Math.Clamp(value, Minimum, Maximum);
        if (clamped < _currentMinNumber)
            _currentMinNumber = clamped;

        if (SetProperty(ref _currentMaxNumber, clamped, nameof(CurrentMaxNumber)))
        {
            OnPropertyChanged(nameof(CurrentMinNumber));
            NotifyDerived();
        }
    }

    public string RangeMinDisplay => FormatPercentOfReference(CurrentMinNumber);
    public string RangeMaxDisplay => FormatPercentOfReference(CurrentMaxNumber);

    public bool CurrentBoolean
    {
        get => _currentBoolean;
        set
        {
            if (SetProperty(ref _currentBoolean, value))
                NotifyDerived();
        }
    }

    public string CurrentText
    {
        get => _currentText;
        set
        {
            if (SetProperty(ref _currentText, value))
            {
                OnPropertyChanged(nameof(ChoiceIndex));
                NotifyDerived();
            }
        }
    }

    public double ChoiceIndex
    {
        get
        {
            if (!IsChoiceSlider || ChoiceOptions.Count == 0)
                return 0;

            for (var i = 0; i < ChoiceOptions.Count; i++)
            {
                if (ChoiceOptions[i].Value == CurrentText)
                    return i;
            }

            return 0;
        }
        set
        {
            if (!IsChoiceSlider || ChoiceOptions.Count == 0)
                return;

            var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            var index = Math.Clamp(rounded, 0, ChoiceOptions.Count - 1);
            CurrentText = ChoiceOptions[index].Value;
        }
    }

    public bool IsModified
    {
        get
        {
            if (_aggregateModifiedProvider is not null)
                return _aggregateModifiedProvider();

            var vanilla = GetReferenceElement();
            if (IsSlider)
                return vanilla.ValueKind == JsonValueKind.Number && Math.Abs(CurrentNumber - vanilla.GetDouble()) > Epsilon;
            if (IsRange)
            {
                if (Id == "loot.droppedWeaponCondition")
                    return Math.Abs(CurrentMinNumber - NpcWeaponConditionVanillaMin) > Epsilon
                        || Math.Abs(CurrentMaxNumber - NpcWeaponConditionVanillaMax) > Epsilon;

                var reference = GetReferenceNumber();
                return Math.Abs(CurrentMinNumber - reference) > Epsilon || Math.Abs(CurrentMaxNumber - reference) > Epsilon;
            }
            if (IsToggle)
                return vanilla.ValueKind is JsonValueKind.True or JsonValueKind.False && CurrentBoolean != vanilla.GetBoolean();
            if (IsChoiceSlider)
                return CurrentText != ElementToText(vanilla);
            return false;
        }
    }

    public string CurrentDisplay
    {
        get
        {
            if (IsToggle)
                return CurrentBoolean ? "ON" : "OFF";

            if (IsChoiceSlider)
                return ChoiceOptions.FirstOrDefault(o => o.Value == CurrentText)?.Label
                    ?? (string.IsNullOrWhiteSpace(CurrentText) ? "None" : CurrentText);

            if (IsRange)
            {
                if (Id == "loot.droppedWeaponCondition")
                    return $"{CurrentMinNumber * 100:0}-{CurrentMaxNumber * 100:0}% condition";

                return $"MIN {RangeMinDisplay} / MAX {RangeMaxDisplay}";
            }

            var playerFacing = FormatPlayerFacingCurrentValue();
            if (!string.IsNullOrWhiteSpace(playerFacing))
                return playerFacing;

            if (Definition.Display?.Primary == "percent")
                return $"{CurrentNumber * 100:0.#}%";

            if (Definition.Display?.Primary == "multiplier")
                return $"{CurrentNumber:0.##}x";

            var vanilla = GetReferenceNumber();
            if (Definition.Display?.Primary == "percentOfVanilla" && Math.Abs(vanilla) > Epsilon)
                return $"{CurrentNumber / vanilla * 100:0}%";
            if (Definition.Display?.Primary == "multiplierOfVanilla" && Math.Abs(vanilla) > Epsilon)
                return $"{CurrentNumber / vanilla:0.##}x";

            if (Definition.Display?.Primary == "raw" && !string.IsNullOrWhiteSpace(Definition.Display.Unit))
                return $"{FormatCompact(CurrentNumber)} {Definition.Display.Unit}";

            return FormatCompact(CurrentNumber);
        }
    }

    private string FormatPlayerFacingCurrentValue()
    {
        return Id switch
        {
            // Combat: keep true scalers as scalers, but say what is being scaled.
            "combat.npcHealth" => Times(CurrentNumber, "HP"),
            "combat.npcWeaponDamage" => Times(CurrentNumber, "gun dmg"),
            "combat.npcAttackCooldown" => Times(CurrentNumber, "attack rate"),
            "combat.npcHipAccuracy" => Times(CurrentNumber, "accuracy"),
            "combat.mutantDamage" => Times(CurrentNumber, "hit dmg"),
            "combat.mutantAttackCooldown" => Times(CurrentNumber, "attack rate"),
            "combat.playerWeaponDamage" => Times(CurrentNumber, "weapon dmg"),
            "combat.playerHeadshotMultiplier" => Times(CurrentNumber, "headshot dmg"),
            "combat.weaponJamming" => Times(CurrentNumber, "jam rate"),
            "combat.explosionDamage" => Times(CurrentNumber, "blast dmg"),
            "combat.accumulatedDamageWeightMax" or "combat.accumulatedDamageWeightMin"
                => $"{CurrentNumber * 100:0}% strength",
            "combat.psySpawnCycleDelay" => Times(CurrentNumber, "wait"),
            "combat.psyPhantomCount" => Times(CurrentNumber, "phantoms"),
            "combat.psyDurabilityDamage" => Times(CurrentNumber, "wear"),

            // Survival: show actual rates, thresholds, durations and player-facing penalties.
            "survival.hungerRate" => $"{CurrentNumber:0.000} pts/s",
            "survival.hungerMediumStaminaRegen" or "survival.hungerStrongStaminaRegen"
                => $"{CurrentNumber * 100:0.#}% regen loss",
            "survival.sleepinessRate" => $"{CurrentNumber:0.000} pts/s",
            "survival.energyDrinkFatigueRelief" => Times(CurrentNumber, "relief"),
            "survival.sleepEligibilityThreshold" => $"{CurrentNumber:0} pts",
            "survival.minimumSleepHours" => $"{CurrentNumber:0} h",
            "survival.radiationAccumulation" => Times(CurrentNumber, "buildup"),
            "survival.radiationRecovery" => $"{CurrentNumber:0.##} pts/s",
            "survival.radiationDamage" => Times(CurrentNumber, "rad dmg"),
            "survival.bleedingSeverity" => Times(CurrentNumber, "bleed"),
            "survival.bleedingRecovery" => $"{CurrentNumber:0.##} pts/s",
            "survival.wellFedStaminaRegen" => SignedPercent(CurrentNumber, "regen"),
            "survival.fatigueLowAimPenalty" or
            "survival.fatigueMediumAimPenalty" or
            "survival.fatigueStrongAimPenalty" => SignedPercent(CurrentNumber, "aim time"),
            "survival.fatigueMediumReloadPenalty" or
            "survival.fatigueStrongReloadPenalty" => SignedPercent(CurrentNumber, "reload"),
            "survival.sleepHealthRestore" => $"{CurrentNumber:0} HP",
            "survival.sleepStaminaRestore" => SignedPercent(CurrentNumber, "stamina"),
            "survival.bleedingLowDamage" or
            "survival.bleedingMediumDamage" or
            "survival.bleedingStrongDamage" => $"{CurrentNumber:0.#} HP/tick",

            // Health: prefer actual HP/radiation/bleeding amounts over percent-of-vanilla abstractions.
            "health.maxHP" => $"{CurrentNumber:0} HP",
            "health.baseRegen" => $"{CurrentNumber:0.#} HP/s",
            "health.regenDelay" => $"{CurrentNumber:0.#} s",
            "health.regenerationScalar" => $"{CurrentNumber * 100:0.#}% regen",
            "health.lowHealthRegen" => $"{CurrentNumber:0.#} HP/s",
            "health.masterLowHealthPenalty" => $"{CurrentNumber * 100:0.#}% regen loss",
            "health.masterBandageModifier" or
            "health.masterMedkitModifier" or
            "health.masterArmyMedkitModifier" => $"{CurrentNumber * 100:0.#}% healing",
            "health.bandageBleeding" or
            "health.medkitBleeding" or
            "health.armyMedkitBleeding" or
            "health.ecoMedkitBleeding" => $"{CurrentNumber:0} bleed pts",
            "health.ecoMedkitAntirad" or
            "health.antiRadRadiation" => $"{CurrentNumber:0} rad pts",

            // Stamina: expose actual SP/time/weight, and translate opaque state coefficients.
            "stamina.maxSP" => $"{CurrentNumber:0} SP",
            "stamina.sprintDisableThreshold" => $"{CurrentNumber:0.#} SP",
            "stamina.sprintRecoveryDelay" => $"{CurrentNumber:0.#} s",
            "stamina.lowRecoveryThreshold" => $"{CurrentNumber:0} SP",
            "stamina.lowRecoveryStrength" => $"{CurrentNumber:0.##} SP/s",
            "stamina.lowRecoveryDuration" => $"{CurrentNumber:0.#} s",
            "stamina.sprintUnderRunSpeedRegenCoef" or
            "stamina.joggingRegenCoef" or
            "stamina.slowRunRegenCoef" or
            "stamina.sprintRegenCoef" or
            "stamina.runRegenCoef" => RecoveryModifier(CurrentNumber),
            "stamina.vaultMaxHeight" => VaultHeight(CurrentNumber),
            "stamina.waterMediumDrain" or
            "stamina.waterStrongDrain" => Times(CurrentNumber, "drain"),
            "stamina.regeneration" => $"{CurrentNumber:0.#} SP/s",
            "stamina.maxCarryWeight" => $"{CurrentNumber:0} kg",

            // Loot: distinguish literal chance from "times vanilla" amount scalers.
            "loot.npcMedicalChance" => Times(CurrentNumber, "drop chance"),
            "loot.npcWeaponAmmoSupply" => Times(CurrentNumber, "ammo"),
            "loot.cheapStashGrenadeChance" => $"{CurrentNumber * 100:0.#}% chance",
            "loot.cheapStashGrenadeQuantity" => Count(CurrentNumber, "grenade"),
            "loot.destructibleAmmoQuantity" => Times(CurrentNumber, "ammo"),
            "loot.destructibleGrenadeQuantity" => Times(CurrentNumber, "grenades"),
            "loot.expensiveStashMoneyChance" => $"{CurrentNumber * 100:0.#}% chance",
            "loot.worldWeaponDensity" => $"{CurrentNumber * 100:0.#}% density",

            // Economy: say whether the scalar changes payout, prices, stock or rewards.
            "economy.startingMoney" => Count(CurrentNumber, "coupon"),
            "economy.buyCondition" => Times(CurrentNumber, "payout"),
            "economy.sellCondition" => Times(CurrentNumber, "shop price"),
            "economy.traderMinBuyCondition" => $"{CurrentNumber * 100:0.#}% condition",
            "economy.itemValueFloor" => $"{CurrentNumber * 100:0.#}% value",
            "economy.traderAmmoStock" or "economy.medicStock" => Times(CurrentNumber, "stock"),
            "economy.fastTravelCost" or
            "economy.upgradeCost" or
            "economy.repairDifficultyCost" => Times(CurrentNumber, "price"),
            "economy.repairCostBase" => $"{CurrentNumber * 100:0.#}% base price",
            "economy.repairEnemy" or
            "economy.repairDisaffection" or
            "economy.repairNeutral" or
            "economy.repairFriend" => Times(CurrentNumber, "repair"),
            "economy.mainQuestReward" or
            "economy.sideQuestReward" => Times(CurrentNumber, "reward"),

            // Equipment.
            "equipment.armorDurability" => Times(CurrentNumber, "durability"),
            "equipment.ammoWeight" or "equipment.consumableWeight" => Times(CurrentNumber, "weight"),
            "equipment.weaponWearPerShot" => Times(CurrentNumber, "wear"),

            // Anomalies.
            "environment.anomalyDamage" => Times(CurrentNumber, "anomaly dmg"),
            "environment.flycatcherMovementSpeed" => $"{CurrentNumber * 100:0.#}% move speed",
            "environment.flycatcherTurnSpeed" => $"{CurrentNumber * 100:0.#}% turn speed",
            "environment.razorBleeding" => Times(CurrentNumber, "bleeding"),
            "environment.diamondBleeding" => $"{CurrentNumber:0.#} bleed pts",
            "environment.psyAnomalyIntensity" => Times(CurrentNumber, "intensity"),

            // Master-only survival penalties.
            "survival.starvationStaminaDrain" => SignedPercent(CurrentNumber, "stamina cost"),
            "survival.starvationDamage" => $"{CurrentNumber:0.#} HP/tick",
            "survival.starvationDamageInterval" => $"{CurrentNumber:0.#} s",
            "survival.starvationPenaltyDelay" => $"{CurrentNumber:0.#} s",
            "survival.starvationHealthRegenPenalty" => $"{CurrentNumber * 100:0.#}% regen loss",
            "survival.severeFatigueMaxStamina" => NegativePercent(CurrentNumber, "max SP"),
            "survival.severeFatigueCarryWeight" => NegativePercent(CurrentNumber, "carry"),
            "survival.severeFatigueRecoil" => SignedPercent(CurrentNumber, "recoil"),
            "survival.severeFatigueAimSway" => SignedPercent(CurrentNumber, "sway"),
            "survival.severeFatigueDispersion" => SignedPercent(CurrentNumber, "spread"),
            "survival.radiationStaminaDrainPenalty" => SignedPercent(CurrentNumber, "stamina cost"),

            _ => string.Empty
        };
    }

    private static string Times(double value, string meaning)
        => $"{value:0.##}x {meaning}";

    private static string SignedPercent(double value, string meaning)
    {
        var percent = value * 100.0;
        return $"{percent:+0.##;-0.##;0}% {meaning}";
    }

    private static string NegativePercent(double value, string meaning)
    {
        if (Math.Abs(value) <= Epsilon)
            return $"0% {meaning}";

        return $"-{Math.Abs(value) * 100:0.##}% {meaning}";
    }

    private static string RecoveryModifier(double value)
    {
        if (Math.Abs(value) <= Epsilon)
            return "Neutral";

        return SignedPercent(value, "regen");
    }

    private static string Count(double value, string singular)
    {
        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        var word = Math.Abs(rounded - 1.0) <= Epsilon ? singular : singular + "s";
        return $"{rounded:0} {word}";
    }

    private static string VaultHeight(double value)
    {
        var height = Math.Round(value, MidpointRounding.AwayFromZero);
        var marker = height switch
        {
            <= 129 => "below waist",
            130 => "WAIST",
            <= 224 => "waist-eye",
            225 => "EYES",
            <= 299 => "above eyes",
            _ => "ABOVE EYES"
        };

        return $"{height:0} ({marker})";
    }

    private static string FormatCompact(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    public string TechnicalDisplay
    {
        get
        {
            if (Id == "loot.worldWeaponShuffle")
                return "Redistribute regular world weapons across compatible locations while preserving progression.";
            if (Id == "loot.worldWeaponDensity")
            {
                var baseCount = 36 + (int)Math.Round(260.0 * CurrentNumber, MidpointRounding.AwayFromZero);
                var dlcCount = 36 + (int)Math.Round(340.0 * CurrentNumber, MidpointRounding.AwayFromZero);
                var baseMinimum = Math.Clamp(baseCount / 36, 1, 6);
                var dlcMinimum = Math.Clamp(dlcCount / 36, 1, 8);

                static string Copies(int value) => value switch
                {
                    1 => "once",
                    2 => "twice",
                    _ => $"{value} times"
                };

                return $"{baseCount}/296 base locations ({dlcCount}/376 with DLC1) | every weapon at least {Copies(baseMinimum)} ({Copies(dlcMinimum)} with DLC1).";
            }
            if (Id == "loot.worldF1GrenadeBoxes")
            {
                var baseCount = (int)Math.Round(61.0 * CurrentNumber, MidpointRounding.AwayFromZero);
                var dlcCount = (int)Math.Round(101.0 * CurrentNumber, MidpointRounding.AwayFromZero);
                return $"Removes {baseCount}/61 base F1 boxes ({dlcCount}/101 with DLC).";
            }
            if (Id == "loot.worldAttachmentShuffle")
                return "Redistribute optics, suppressors, magazines and underbarrel attachments across compatible world locations.";
            if (Id == "loot.worldDetectors")
                return "ON removes the audited loose and stash detectors. OFF leaves vanilla detector spawns untouched.";
            if (Id == "loot.blueprintShuffle")
                return "Randomize blueprint locations.";
            if (Id == "combat.accumulatedDamageIncludesHealedHealth")
                return "ON: Healing restores HP, but healed damage stays in the repeated-hit protection tracker.";
            if (IsToggle || IsChoiceSlider)
                return string.Empty;

            var descriptor = string.IsNullOrWhiteSpace(Definition.Display?.ValueLabel)
                ? "Current setting"
                : Definition.Display!.ValueLabel;

            if (IsRange)
            {
                if (Id == "loot.droppedWeaponCondition")
                    return $"{descriptor} · MIN {CurrentMinNumber * 100:0.#}% / MAX {CurrentMaxNumber * 100:0.#}%";

                return $"{descriptor} · MIN {CurrentMinNumber * 100:0.#}% / MAX {CurrentMaxNumber * 100:0.#}% of vanilla";
            }

            if (Definition.Display?.Primary == "percent")
                return $"{descriptor} · {CurrentNumber * 100:0.#}%";

            if (Definition.Display?.Primary == "multiplierOfVanilla")
            {
                var reference = GetReferenceNumber();
                var multiplier = Math.Abs(reference) > Epsilon ? CurrentNumber / reference : 0.0;
                return $"{descriptor} · {multiplier:0.##}x";
            }

            if (Definition.ValueKind == "probability")
                return $"{descriptor} · {CurrentNumber * 100:0.#}% chance";

            if (IsBleedingRemovalControl())
                return $"{descriptor} · {Math.Abs(CurrentNumber):0.##} points";

            if (IsRadiationRemovalControl())
                return $"{descriptor} · {Math.Abs(CurrentNumber):0.##} points";

            if (IsMovementStaminaEffect())
            {
                var state = CurrentNumber < -Epsilon
                    ? "drains stamina"
                    : CurrentNumber > Epsilon
                        ? "helps recovery"
                        : "neutral";
                return $"{descriptor} · {CurrentNumber:+0.##;-0.##;0} ({state})";
            }

            if (Id is "combat.accumulatedDamageWeightMax" or "combat.accumulatedDamageWeightMin")
                return $"{descriptor} · {CurrentNumber:0.##}x of full scale";

            if (Definition.ValueKind == "multiplier")
            {
                var basis = Definition.Display?.Primary == "percentOfVanilla" ? "of vanilla" : "of normal";
                return $"{descriptor} · {CurrentNumber:0.##}x {basis}";
            }

            if (Definition.Tab == "loot" && Definition.Display?.Primary == "percentOfVanilla")
            {
                var reference = GetReferenceNumber();
                var scale = Math.Abs(reference) > Epsilon ? CurrentNumber / reference : CurrentNumber;
                return $"{descriptor} · {scale:0.##}x of vanilla";
            }

            var value = FormatTechnicalValue(CurrentNumber);
            return $"{descriptor} · {value}";
        }
    }

    private string FormatTechnicalValue(double value)
    {
        var unit = Definition.Display?.Unit ?? string.Empty;

        if (Definition.Id == "economy.itemValueFloor")
            return $"{value * 100:0.#}% of base value";

        if (Definition.Display?.Primary == "percent" && Definition.ValueKind == "raw")
            return $"{value * 100:+0.#;-0.#;0}%";

        if (unit == "fraction")
        {
            if (value < -Epsilon)
                return $"{Math.Abs(value) * 100:0.#}% penalty";
            if (value > Epsilon)
                return $"{value * 100:0.#}% bonus";
            return "neutral";
        }

        var number = value.ToString("0.#####", CultureInfo.InvariantCulture);
        return unit switch
        {
            "" => number,
            "coefficient" => number,
            "regen rate" => $"{number} HP recovery rate",
            "HP-rate" => $"{number} HP recovery",
            "uu" => $"{number} height units",
            _ => $"{number} {unit}"
        };
    }

    private bool IsMovementStaminaEffect() => Id is
        "stamina.sprintUnderRunSpeedRegenCoef" or
        "stamina.sprintRegenCoef" or
        "stamina.runRegenCoef" or
        "stamina.joggingRegenCoef" or
        "stamina.slowRunRegenCoef";

    private bool IsBleedingRemovalControl() => Id is
        "health.bandageBleeding" or
        "health.medkitBleeding" or
        "health.armyMedkitBleeding" or
        "health.ecoMedkitBleeding";

    private bool IsRadiationRemovalControl() => Id is
        "health.ecoMedkitAntirad" or
        "health.antiRadRadiation";

    public string EffectiveDisplay
    {
        get
        {
            if (!HasEffectiveDamage)
                return string.Empty;

            var multiplier = _effectiveDamageMultiplierProvider?.Invoke() ?? 1.0;
            if (Id == "environment.razorHealthDamage")
            {
                var lowMin = 20.0 * CurrentNumber * multiplier;
                var lowMax = 25.0 * CurrentNumber * multiplier;
                var high = 100.0 * CurrentNumber * multiplier;
                return $"Effective {lowMin:0.##}-{lowMax:0.##} / {high:0.##}";
            }

            var effective = CurrentNumber * multiplier;
            return $"Effective {effective:0.##}";
        }
    }

    public string EffectiveDetails
    {
        get
        {
            if (!HasEffectiveDamage)
                return string.Empty;

            var multiplier = _effectiveDamageMultiplierProvider?.Invoke() ?? 1.0;
            if (Id == "environment.razorHealthDamage")
            {
                var lowMinBase = 20.0 * CurrentNumber;
                var lowMaxBase = 25.0 * CurrentNumber;
                var highBase = 100.0 * CurrentNumber;
                return $"Base anomaly damage: low {lowMinBase:0.##}-{lowMaxBase:0.##}, high {highBase:0.##}\n" +
                       $"Global anomaly damage: {multiplier:0.##}x\n" +
                       $"Before protection: low {lowMinBase * multiplier:0.##}-{lowMaxBase * multiplier:0.##}, high {highBase * multiplier:0.##} damage";
            }

            var effective = CurrentNumber * multiplier;
            return $"Base anomaly damage: {CurrentNumber:0.#####}\nGlobal anomaly damage: {multiplier:0.##}x\nBefore protection: {effective:0.##} damage";
        }
    }

    public bool HasPracticalQuantity => Definition.Tab == "loot" && Definition.LootHint is not null;

    public string PracticalQuantityDisplay
    {
        get
        {
            if (Definition.Tab != "loot" || Definition.LootHint is null)
                return string.Empty;

            var hint = Definition.LootHint;
            var label = string.IsNullOrWhiteSpace(hint.Label) ? "Practical" : hint.Label;

            if (hint.Kind == "absoluteChancePer100" && IsSlider)
            {
                var successes = CurrentNumber * 100.0;
                if (Id.StartsWith("loot.mutant.", StringComparison.Ordinal))
                    return $"Expected · {label}: ≈{successes:0.#} part drops / 100 {hint.Unit} · 1 part per successful drop";

                if (Id == "loot.expensiveStashMoneyChance")
                    return $"Expected · {label}: ≈{successes:0.#} stashes with coupons / 100 {hint.Unit}";

                return $"Expected · {label}: ≈{successes:0.#} successes / 100 {hint.Unit}";
            }

            if (hint.Kind == "scaledChanceExample" && IsSlider && hint.Chance.HasValue)
            {
                var reference = GetReferenceNumber();
                var scale = Math.Abs(reference) > Epsilon ? CurrentNumber / reference : CurrentNumber;
                var result = Math.Clamp(hint.Chance.Value * scale, 0.0, 1.0);
                var outcome = hint.Unit.Contains("stash", StringComparison.OrdinalIgnoreCase)
                    ? $"≈{result * 100:0.#} stashes with this entry / 100 {hint.Unit}"
                    : $"≈{result * 100:0.#} drops / 100 {hint.Unit}";
                return $"Example · {label}: {hint.Chance.Value * 100:0.#}% vanilla -> {result * 100:0.#}% current · {outcome}";
            }

            if (hint.Kind == "rangeCountExample" && IsRange && hint.Min.HasValue && hint.Max.HasValue)
            {
                var min = hint.Min.Value * CurrentMinNumber;
                var max = hint.Max.Value * CurrentMaxNumber;
                return $"Example · {label}: {hint.Min.Value:0.##}–{hint.Max.Value:0.##} {hint.Unit} vanilla -> ≈{min:0.##}–{max:0.##} {hint.Unit} current · final counts use whole numbers";
            }

            if (hint.Kind == "rangePercentExample" && IsRange && hint.Min.HasValue && hint.Max.HasValue)
            {
                var min = Math.Clamp(hint.Min.Value * CurrentMinNumber, 0.0, 1.0);
                var max = Math.Clamp(hint.Max.Value * CurrentMaxNumber, 0.0, 1.0);
                return $"Example · {label}: {hint.Min.Value * 100:0.#}–{hint.Max.Value * 100:0.#}% vanilla -> {min * 100:0.#}–{max * 100:0.#}% current";
            }

            if (hint.Kind == "scaledCountEntries" && IsSlider && hint.Entries.Count > 0)
            {
                static int ScaleCount(double source, double multiplier)
                    => Math.Max(1, (int)Math.Floor(source * multiplier + 0.5));

                var lines = hint.Entries.Select(entry =>
                {
                    var currentMin = ScaleCount(entry.Min, CurrentNumber);
                    var currentMax = ScaleCount(entry.Max, CurrentNumber);
                    var qualifier = string.IsNullOrWhiteSpace(entry.Qualifier) ? string.Empty : $" [{entry.Qualifier}]";
                    var selection = string.IsNullOrWhiteSpace(entry.Selection) ? string.Empty : $" · {entry.Selection}";
                    return $"{entry.Label}{qualifier}: {entry.Min:0}-{entry.Max:0} -> {currentMin}-{currentMax}{selection}";
                });
                return $"{label} · vanilla -> current{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
            }

            if (hint.Kind == "exactCount" && IsSlider)
            {
                var unit = string.IsNullOrWhiteSpace(hint.Unit) ? "items" : hint.Unit;
                return $"{label} · vanilla {(hint.Min ?? GetReferenceNumber()):0} {unit} -> current {CurrentNumber:0} {unit}";
            }

            if (hint.Kind == "text" && !string.IsNullOrWhiteSpace(hint.Text))
                return $"{label} · {hint.Text}";

            return string.Empty;
        }
    }

    public bool HasValueMeaning => !string.IsNullOrWhiteSpace(ValueMeaning);

    public string ValueMeaning
    {
        get
        {
            if (Definition.Tab != "loot")
                return string.Empty;

            if (IsChoiceSlider)
                return "Chooses which extra loot preset ordinary human corpses can use. This is a named choice, not a percentage.";

            if (IsToggle)
                return "Turns this extra vanilla loot rule on or off. It does not change stack size or drop chance by itself.";

            if (IsRange)
            {
                if (Definition.ValueKind == "durabilityAbsoluteRange")
                    return $"MIN {RangeMinDisplay} and MAX {RangeMaxDisplay} are the actual condition limits for dropped NPC weapons. " +
                           "Vanilla is kept exactly until you move either bound; after that, ordinary human NPC drops use the selected range.";

                if (Definition.ValueKind == "durabilityRangeMultiplier")
                    return $"MIN {RangeMinDisplay} changes the low end of dropped weapon condition; MAX {RangeMaxDisplay} changes the high end. " +
                           "100% / 100% keeps every vanilla condition range unchanged.";

                return $"MIN {RangeMinDisplay} changes the smallest vanilla stack; MAX {RangeMaxDisplay} changes the largest. " +
                       "100% / 100% keeps the original ranges, including differences between difficulty and Progression Rank.";
            }

            if (Definition.ValueKind == "probability")
                return $"{CurrentDisplay} is the actual drop chance. Roughly speaking, that means about {CurrentNumber * 100:0.#} successful drops out of 100 eligible chances.";

            if (Definition.Display?.Primary == "percentOfVanilla")
            {
                var reference = GetReferenceNumber();
                var scale = Math.Abs(reference) > Epsilon ? CurrentNumber / reference : 0.0;
                return $"This keeps the original vanilla differences and scales them to {scale * 100:0.#}%. " +
                       "It is not a flat drop chance: for example, a vanilla 20% chance becomes 10% when this control is set to 50%.";
            }

            return string.Empty;
        }
    }

    public string VanillaReferenceText
    {
        get
        {
            if (Definition.Vanilla.TryGetValue("All", out var all))
                return $"Vanilla on all difficulties: {FormatVanillaValue(all, isSharedValue: true)}";

            var order = new[] { "Novice", "Stalker", "Veteran", "Master" };
            var values = order
                .Where(Definition.Vanilla.ContainsKey)
                .Select(k => $"{k}: {FormatVanillaValue(Definition.Vanilla[k], isSharedValue: false)}");

            return "Vanilla · " + string.Join("   ·   ", values);
        }
    }

    public string ReferenceLabel => Definition.Vanilla.ContainsKey("All")
        ? "Same on all difficulties"
        : $"Base Preset: {_referenceDifficulty}";

    public double ReferenceNumber => GetReferenceNumber();

    public void SetEffectiveDamageMultiplierProvider(Func<double> provider)
    {
        _effectiveDamageMultiplierProvider = provider;
        RefreshComputedValues();
        OnPropertyChanged(nameof(HasEffectiveDamage));
    }

    public void SetAggregateModifiedProvider(Func<bool> provider)
    {
        _aggregateModifiedProvider = provider;
        OnPropertyChanged(nameof(IsModified));
    }

    public void RefreshAggregateModifiedState()
        => OnPropertyChanged(nameof(IsModified));

    public void SetDependencyEnabledProvider(Func<bool> provider)
    {
        _dependencyEnabledProvider = provider;
        OnPropertyChanged(nameof(IsControlEnabled));
        OnPropertyChanged(nameof(IsControlVisible));
    }

    public void RefreshDependencyState()
    {
        OnPropertyChanged(nameof(IsControlEnabled));
        OnPropertyChanged(nameof(IsControlVisible));
    }

    public void RefreshComputedValues()
    {
        OnPropertyChanged(nameof(TechnicalDisplay));
        OnPropertyChanged(nameof(EffectiveDisplay));
        OnPropertyChanged(nameof(EffectiveDetails));
        OnPropertyChanged(nameof(ValueMeaning));
        OnPropertyChanged(nameof(PracticalQuantityDisplay));
    }

    public void ChangeReferenceDifficulty(string difficulty, bool resetValue)
    {
        _referenceDifficulty = difficulty;
        OnPropertyChanged(nameof(ReferenceLabel));
        OnPropertyChanged(nameof(ReferenceNumber));
        OnPropertyChanged(nameof(VanillaReferenceText));
        OnPropertyChanged(nameof(TickFrequency));

        if (resetValue)
            ResetToReference();
        else
        {
            OnPropertyChanged(nameof(CurrentDisplay));
            OnPropertyChanged(nameof(ChoiceIndex));
            OnPropertyChanged(nameof(ToggleStateLabel));
            OnPropertyChanged(nameof(TechnicalDisplay));
            OnPropertyChanged(nameof(RangeMinDisplay));
            OnPropertyChanged(nameof(RangeMaxDisplay));
            OnPropertyChanged(nameof(ValueMeaning));
            OnPropertyChanged(nameof(PracticalQuantityDisplay));
            OnPropertyChanged(nameof(IsModified));
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ResetToReference(bool notify = true)
    {
        var element = GetReferenceElement();
        if (IsSlider && element.ValueKind == JsonValueKind.Number)
            _currentNumber = element.GetDouble();
        else if (IsRange && element.ValueKind == JsonValueKind.Number)
        {
            if (Id == "loot.droppedWeaponCondition")
            {
                _currentMinNumber = NpcWeaponConditionVanillaMin;
                _currentMaxNumber = NpcWeaponConditionVanillaMax;
            }
            else
            {
                _currentMinNumber = element.GetDouble();
                _currentMaxNumber = element.GetDouble();
            }
        }
        else if (IsToggle && element.ValueKind is JsonValueKind.True or JsonValueKind.False)
            _currentBoolean = element.GetBoolean();
        else if (IsChoiceSlider)
            _currentText = ElementToText(element);

        OnPropertyChanged(nameof(CurrentNumber));
        OnPropertyChanged(nameof(CurrentMinNumber));
        OnPropertyChanged(nameof(CurrentMaxNumber));
        OnPropertyChanged(nameof(RangeMinDisplay));
        OnPropertyChanged(nameof(RangeMaxDisplay));
        OnPropertyChanged(nameof(CurrentBoolean));
        // CurrentBoolean may be reset by assigning the backing field directly during a
        // baseline switch. Notify the derived ON/OFF label explicitly as well, otherwise
        // the checkbox can visually keep the previous text (most noticeably OFF when
        // switching to Master, whose Master-only toggles default to ON).
        OnPropertyChanged(nameof(ToggleStateLabel));
        OnPropertyChanged(nameof(CurrentText));
        OnPropertyChanged(nameof(ChoiceIndex));
        OnPropertyChanged(nameof(CurrentDisplay));
        OnPropertyChanged(nameof(TechnicalDisplay));
        OnPropertyChanged(nameof(RangeMinDisplay));
        OnPropertyChanged(nameof(RangeMaxDisplay));
        OnPropertyChanged(nameof(EffectiveDisplay));
        OnPropertyChanged(nameof(EffectiveDetails));
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ReferenceLabel));
        OnPropertyChanged(nameof(TickFrequency));
        OnPropertyChanged(nameof(ValueMeaning));
        OnPropertyChanged(nameof(PracticalQuantityDisplay));

        if (notify)
            StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public PresetValue ToPresetValue() => new()
    {
        Kind = Control,
        Number = IsSlider ? CurrentNumber : null,
        MinNumber = IsRange ? CurrentMinNumber : null,
        MaxNumber = IsRange ? CurrentMaxNumber : null,
        Boolean = IsToggle ? CurrentBoolean : null,
        Text = IsChoiceSlider ? CurrentText : null
    };

    public void ApplyPresetValue(PresetValue value)
    {
        // v0.9.26.44 changed NPC Weapon Condition from a multiplier slider to an
        // absolute MIN/MAX condition range. Preserve old user presets by converting
        // their multiplier into the old vanilla 25%-50% range before normal handling.
        if (Id == "loot.droppedWeaponCondition"
            && IsRange
            && string.Equals(value.Kind, "slider", StringComparison.Ordinal)
            && value.Number.HasValue)
        {
            CurrentMinNumber = Math.Clamp(NpcWeaponConditionVanillaMin * value.Number.Value, Minimum, Maximum);
            CurrentMaxNumber = Math.Clamp(NpcWeaponConditionVanillaMax * value.Number.Value, Minimum, Maximum);
            return;
        }

        // Older presets may not have Kind populated. When present, require it to
        // match the current catalog control so a changed parameter type cannot
        // silently consume the wrong JSON value shape.
        if (!string.IsNullOrWhiteSpace(value.Kind)
            && !string.Equals(value.Kind, Control, StringComparison.Ordinal))
        {
            return;
        }

        if (IsSlider && value.Number.HasValue)
            CurrentNumber = value.Number.Value;
        else if (IsRange)
        {
            if (value.MinNumber.HasValue || value.MaxNumber.HasValue)
            {
                if (value.MinNumber.HasValue)
                    CurrentMinNumber = value.MinNumber.Value;
                if (value.MaxNumber.HasValue)
                    CurrentMaxNumber = value.MaxNumber.Value;
            }
            else if (value.Number.HasValue)
            {
                // Backward compatibility with early range presets that used
                // a single Number field.
                CurrentMinNumber = value.Number.Value;
                CurrentMaxNumber = value.Number.Value;
            }
        }
        else if (IsToggle && value.Boolean.HasValue)
            CurrentBoolean = value.Boolean.Value;
        else if (IsChoiceSlider && value.Text is not null
                 && ChoiceOptions.Any(option => option.Value == value.Text))
            CurrentText = value.Text;
    }

    private double NormalizeSliderValue(double value)
    {
        var clamped = Math.Clamp(value, Minimum, Maximum);
        if (!IsSlider)
            return clamped;

        if (UsesAbsoluteCatalogStep())
            return SnapToCatalogStep(clamped);

        if (Definition.Display?.Primary == "percent")
        {
            var snappedValue = Math.Round(clamped / PercentageStep, MidpointRounding.AwayFromZero) * PercentageStep;
            return Math.Clamp(snappedValue, Minimum, Maximum);
        }

        if (Definition.Display?.Primary is "percentOfVanilla" or "multiplierOfVanilla")
        {
            var reference = GetReferenceNumber();
            if (Math.Abs(reference) <= Epsilon)
                return clamped;

            var ratio = clamped / reference;
            var ratioStep = Definition.Display?.Primary == "multiplierOfVanilla" ? 0.01 : PercentageStep;
            var snappedRatio = Math.Round(ratio / ratioStep, MidpointRounding.AwayFromZero) * ratioStep;
            var snappedValue = snappedRatio * reference;
            return Math.Clamp(snappedValue, Minimum, Maximum);
        }

        // Raw sliders must honor the catalog step as well. Previously the WPF slider
        // advertised TickFrequency/SmallChange but raw values were accepted continuously,
        // so controls such as Starting Coupons (step 10) could land on 3-decimal values.
        return SnapToCatalogStep(clamped);
    }

    private bool UsesAbsoluteCatalogStep() =>
        string.Equals(Definition.Slider?.StepMode, "absolute", StringComparison.OrdinalIgnoreCase);

    private double SnapToCatalogStep(double clamped)
    {
        var step = Definition.Slider?.Step ?? 0.0;
        if (step <= Epsilon)
            return clamped;

        var offset = clamped - Minimum;
        var snappedSteps = Math.Round(offset / step, MidpointRounding.AwayFromZero);
        var snappedValue = Minimum + snappedSteps * step;
        return Math.Clamp(snappedValue, Minimum, Maximum);
    }

    private double NormalizeRangeValue(double value)
    {
        var clamped = Math.Clamp(value, Minimum, Maximum);
        if (UsesAbsoluteCatalogStep())
            return SnapToCatalogStep(clamped);

        var reference = GetReferenceNumber();
        if (Math.Abs(reference) <= Epsilon)
            return clamped;

        var ratio = clamped / reference;
        var snappedRatio = Math.Round(ratio / PercentageStep, MidpointRounding.AwayFromZero) * PercentageStep;
        return Math.Clamp(snappedRatio * reference, Minimum, Maximum);
    }

    private string FormatPercentOfReference(double value)
    {
        var reference = GetReferenceNumber();
        return Math.Abs(reference) > Epsilon
            ? $"{value / reference * 100:0}%"
            : value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private JsonElement GetReferenceElement()
    {
        if (Definition.Vanilla.TryGetValue(_referenceDifficulty, out var specific))
            return specific;
        if (Definition.Vanilla.TryGetValue("All", out var all))
            return all;
        return Definition.Vanilla.Values.FirstOrDefault();
    }

    private double GetReferenceNumber()
    {
        var element = GetReferenceElement();
        return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : 0;
    }

    private string FormatVanillaValue(JsonElement element, bool isSharedValue)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            var value = element.GetDouble();
            var unit = string.IsNullOrWhiteSpace(Definition.Display?.Unit) ? string.Empty : $" {Definition.Display!.Unit}";

            // Difficulty multipliers are easiest to understand as their actual vanilla percentages.
            if (Definition.Display?.Primary == "percent")
            {
                if (Id == "survival.radiationStaminaDrainPenalty")
                    return $"{value * 100:+0.#;-0.#;0}%";

                return $"{value * 100:0.#}%";
            }

            if (Definition.Display?.Primary == "multiplier")
                return $"{value:0.##}x";

            // Shared direct-value parameters are displayed as vanilla 1x plus their real CFG value.
            if (Definition.Display?.Primary == "percentOfVanilla" && isSharedValue)
                return $"1x  ({value:0.#####}{unit})";

            return $"{value:0.#####}{unit}";
        }

        if (element.ValueKind == JsonValueKind.True)
            return "On";
        if (element.ValueKind == JsonValueKind.False)
            return "Off";
        if (element.ValueKind == JsonValueKind.Array)
        {
            var items = element.EnumerateArray().Select(FriendlyElementText).ToList();
            return items.Count == 0 ? "None" : string.Join(" + ", items);
        }

        return FriendlyElementText(element);
    }

    private void NotifyDerived()
    {
        OnPropertyChanged(nameof(CurrentDisplay));
        OnPropertyChanged(nameof(ChoiceIndex));
        OnPropertyChanged(nameof(ToggleStateLabel));
        OnPropertyChanged(nameof(TechnicalDisplay));
        OnPropertyChanged(nameof(RangeMinDisplay));
        OnPropertyChanged(nameof(RangeMaxDisplay));
        OnPropertyChanged(nameof(EffectiveDisplay));
        OnPropertyChanged(nameof(EffectiveDetails));
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(ValueMeaning));
        OnPropertyChanged(nameof(PracticalQuantityDisplay));
        StateChanged?.Invoke(this, EventArgs.Empty);
    }


    private static IReadOnlyList<VanillaMarkerViewModel> BuildVanillaMarkers(ParameterDefinition definition)
    {
        if (definition.Control != "slider" && definition.Control != "range")
            return Array.Empty<VanillaMarkerViewModel>();

        var result = new List<VanillaMarkerViewModel>();

        if (definition.Vanilla.TryGetValue("All", out var shared) && shared.ValueKind == JsonValueKind.Number)
        {
            result.Add(new VanillaMarkerViewModel("VANILLA", shared.GetDouble()));
        }
        else
        {
            var labels = new Dictionary<string, (string Long, string Short)>(StringComparer.Ordinal)
            {
                ["Novice"] = ("NOVICE", "NOVICE"),
                ["Stalker"] = ("STALKER", "STALKER"),
                ["Veteran"] = ("VETERAN", "VETERAN"),
                ["Master"] = ("MASTER", "MASTER")
            };

            var raw = new List<(string Difficulty, string Long, string Short, double Value)>();
            foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
            {
                if (definition.Vanilla.TryGetValue(difficulty, out var value) && value.ValueKind == JsonValueKind.Number)
                {
                    var label = labels[difficulty];
                    raw.Add((difficulty, label.Long, label.Short, value.GetDouble()));
                }
            }

            result.AddRange(raw
                .GroupBy(x => Math.Round(x.Value, 9))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var items = g.ToList();
                    var label = items.Count == 4
                        ? "VANILLA"
                        : items.Count > 1
                            ? string.Join(" / ", items.Select(x => x.Short))
                            : items[0].Long;
                    return new VanillaMarkerViewModel(label, items[0].Value);
                }));
        }

        // Optional explicit UX reference points can coexist with the vanilla difficulty markers.
        // They share the same click-to-snap interaction but do not affect automatic baseline/reset behavior.
        foreach (var marker in definition.Markers)
        {
            if (!string.IsNullOrWhiteSpace(marker.Label))
                result.Add(new VanillaMarkerViewModel(marker.Label, marker.Value));
        }

        // Merge labels that intentionally occupy the same slider position, e.g. VANILLA/WAIST.
        return result
            .GroupBy(x => Math.Round(x.Value, 9))
            .OrderBy(g => g.Key)
            .Select(g => new VanillaMarkerViewModel(
                string.Join(" / ", g.Select(x => x.Label).Distinct(StringComparer.Ordinal)),
                g.First().Value))
            .ToList();
    }


    private static IReadOnlyList<VanillaMarkerViewModel> BuildChoiceVanillaMarkers(
        ParameterDefinition definition,
        IReadOnlyList<ChoiceOptionViewModel> options)
    {
        if (definition.Control != "choiceSlider" || options.Count == 0)
            return Array.Empty<VanillaMarkerViewModel>();

        var difficultyLabels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Novice"] = "NOVICE",
            ["Stalker"] = "STALKER",
            ["Veteran"] = "VETERAN",
            ["Master"] = "MASTER"
        };

        var markers = new List<(string Label, double Value)>();
        foreach (var difficulty in new[] { "Novice", "Stalker", "Veteran", "Master" })
        {
            if (!definition.Vanilla.TryGetValue(difficulty, out var element))
                continue;

            var raw = ElementToText(element);
            var index = -1;
            for (var i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i].Value, raw, StringComparison.Ordinal))
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
                markers.Add((difficultyLabels[difficulty], index));
        }

        return markers
            .GroupBy(x => x.Value)
            .OrderBy(g => g.Key)
            .Select(g => new VanillaMarkerViewModel(
                string.Join(" / ", g.Select(x => x.Label)),
                g.Key))
            .ToList();
    }

    private static IReadOnlyList<ChoiceOptionViewModel> BuildChoiceOptions(ParameterDefinition definition)
    {
        if (definition.Control != "choiceSlider")
            return Array.Empty<ChoiceOptionViewModel>();

        var order = new[] { "Novice", "Stalker", "Veteran", "Master", "All" };
        var result = new List<ChoiceOptionViewModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in order.Concat(definition.Vanilla.Keys.Where(k => !order.Contains(k))))
        {
            if (!definition.Vanilla.TryGetValue(key, out var element))
                continue;

            var raw = ElementToText(element);
            if (!seen.Add(raw))
                continue;

            result.Add(new ChoiceOptionViewModel(raw, FriendlyChoiceLabel(element)));
        }

        return result;
    }

    private static string FriendlyChoiceLabel(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var items = element.EnumerateArray().Select(FriendlyElementText).ToList();
            return items.Count == 0 ? "None" : string.Join(" + ", items);
        }

        return FriendlyElementText(element);
    }

    private static string FriendlyElementText(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
            return ElementToText(element);

        return (element.GetString() ?? string.Empty) switch
        {
            "NPC_Medicine_Smart" => "Medicine",
            "NPC_Ammo_Smart" => "Ammo",
            "NPC_Water_Smart" => "Water",
            var value => value
        };
    }

    private static string ElementToText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.GetDouble().ToString("0.#####", CultureInfo.InvariantCulture),
        JsonValueKind.True => "ON",
        JsonValueKind.False => "OFF",
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Array => string.Join(", ", element.EnumerateArray().Select(ElementToText)),
        JsonValueKind.Null => "—",
        _ => element.ToString()
    };
}


public sealed record ChoiceOptionViewModel(string Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record VanillaMarkerViewModel(string Label, double Value);
