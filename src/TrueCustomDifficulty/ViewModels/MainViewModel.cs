using System.IO;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using TrueCustomDifficulty.Infrastructure;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ParameterCatalog _catalog;
    private readonly PresetService _presetService = new();
    private readonly PresetDirectoryService _presetDirectoryService = new();
    private readonly ModBuildService _modBuildService = new();
    private readonly ModDestinationSettingsService _modDestinationSettings = new();
    private readonly GameInstallInfo? _gameInstall;
    private string _selectedReferenceDifficulty;
    private TabViewModel? _selectedTab;
    private bool _isApplyingReference;
    private bool _suspendSliderConstraints;
    private readonly SliderConstraintCoordinator _sliderConstraintCoordinator;
    private IReadOnlyList<ParameterViewModel> _anomalyDerivedParameters = Array.Empty<ParameterViewModel>();
    private PresetListItem? _selectedPreset;
    private string? _loadedPresetName;
    private bool _suppressPresetSelection;
    private bool _isBuilding;
    private int _buildProgressStep;
    private string _buildProgressLabel = string.Empty;

    // All mutable editor inputs are locked during the off-thread CFG/package
    // build. Only completed pipeline stages advance the progress bar.
    public bool IsBuilding
    {
        get => _isBuilding;
        private set
        {
            if (!SetProperty(ref _isBuilding, value))
                return;
            OnPropertyChanged(nameof(IsBuildIdle));
            BuildCommand?.RaiseCanExecuteChanged();
        }
    }

    public bool IsBuildIdle => !IsBuilding;

    public int BuildProgressStep
    {
        get => _buildProgressStep;
        private set => SetProperty(ref _buildProgressStep, value);
    }

    public string BuildProgressLabel
    {
        get => _buildProgressLabel;
        private set => SetProperty(ref _buildProgressLabel, value);
    }

    public string AppName => _catalog.App.Name;
    public string AppVersion => _catalog.App.Version;
    public string GameVersion => _catalog.Source.GameVersion;

    public ObservableCollection<string> ReferenceDifficulties { get; } = new(new[]
    {
        "Novice", "Stalker", "Veteran", "Master"
    });

    public ObservableCollection<TabViewModel> Tabs { get; } = new();
    public ObservableCollection<PresetListItem> AvailablePresets { get; } = new();
    public IReadOnlyList<ParameterViewModel> AllParameters { get; }

    public RelayCommand ResetCurrentTabCommand { get; }
    public RelayCommand ResetAllCommand { get; }
    public RelayCommand SavePresetCommand { get; }
    public RelayCommand LoadPresetCommand { get; }
    public RelayCommand BuildCommand { get; }

    public MainViewModel(ParameterCatalog catalog)
    {
        _catalog = catalog;
        _presetDirectoryService.Ensure();
        RefreshPresetList();
        _gameInstall = new GameInstallLocatorService().Detect();
        _selectedReferenceDifficulty = catalog.App.DefaultReferenceDifficulty;

        var ammoDistributionParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "loot.ammoBoxWeight918",
            "loot.ammoBoxWeight045",
            "loot.ammoBoxWeight545",
            "loot.ammoBoxWeight012"
        };

        var stashAmmoParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "loot.cheapStashAmmoQuantity",
            "loot.mediumStashAmmoQuantity",
            "loot.expensiveStashAmmoQuantity"
        };

        var mutantDropParameterIds = new HashSet<string>(StringComparer.Ordinal)
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

        var individualAnomalyDamageParameterIds = new HashSet<string>(StringComparer.Ordinal)
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
            "environment.fireballHealthDamage"
        };

        var worldLootRemovalParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "loot.worldAmmoCratesRemoved",
            "loot.worldF1GrenadeBoxes",
            "loot.worldFoodDrinkCratesRemoved",
            "loot.worldMedicalCratesRemoved",
            "loot.looseWorldLootRemoved"
        };

        // Hidden aggregate-backed controls still require a live ParameterViewModel.
        // Without this membership the World Placement visual has an absent source
        // and drops the entire board into "data unavailable".
        var worldPlacementParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "loot.worldWeaponShuffle",
            "loot.worldWeaponDensity",
            "loot.looseUniqueWeaponShuffle",
            "loot.worldAttachmentShuffle",
            "loot.worldDetectors",
            "loot.blueprintShuffle"
        };

        var medicalHealingParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "health.bandageHealing",
            "health.medkitHealing",
            "health.armyMedkitHealing",
            "health.ecoMedkitHealing",
            "health.foodHealing",
            "health.bandageBleeding",
            "health.medkitBleeding",
            "health.armyMedkitBleeding",
            "health.ecoMedkitBleeding",
            "health.ecoMedkitAntirad",
            "health.antiRadRadiation",
            "health.beerRadiationRemoval",
            "health.vodkaRadiationRemoval"
        };

        var actionCostParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "stamina.sprintCost", "stamina.jumpCost", "stamina.vaultCost",
            "stamina.meleeNormalCost", "stamina.meleeStrongCost",
            "stamina.butstockCost"
        };
        var exhaustionRecoveryParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "stamina.aimRegenCoef", "stamina.sprintDisableThreshold",
            "stamina.sprintRecoveryDelay", "stamina.lowRecoveryThreshold",
            "stamina.lowRecoveryStrength", "stamina.lowRecoveryDuration"
        };

        var consumableAggregateParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "survival.energyDrinkInstantStaminaRestore",
            "survival.waterStaminaEffectStrength",
            "survival.waterStaminaCostReduction",
            "survival.energyDrinkStaminaRecoveryStrength",
            "survival.waterRegenDuration",
            "survival.energyFullCostDuration",
            "survival.energyPartialCostDuration",
            // Source-backed medical buffs are hidden aggregate members; all six
            // must be instantiated or the medicine knob/timeline lanes vanish.
            "survival.vincaBleedRecovery",
            "survival.vincaDuration",
            "survival.herculesCarryBonus",
            "survival.herculesDuration",
            "survival.psiBlockRecovery",
            "survival.psiBlockDuration"
        };

        var itemPriceScalerParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "economy.consumableCost",
            "economy.ammoCost",
            "economy.armorCost",
            "economy.weaponCost",
            "economy.artifactCost",
            "economy.mutantPartValue"
        };

        var mutantHealthParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "combat.mutantHealth.bloodsucker",
            "combat.mutantHealth.chimera",
            "combat.mutantHealth.deer",
            "combat.mutantHealth.pseudogiant",
            "combat.mutantHealth.pseudodog",
            "combat.mutantHealth.burer",
            "combat.mutantHealth.bayun",
            "combat.mutantHealth.controller",
        "combat.mutantHealth.blinddog",
        "combat.mutantHealth.boar",
        "combat.mutantHealth.flesh",
        "combat.mutantHealth.poltergeist",
        "combat.mutantHealth.snork",
        "combat.mutantHealth.tushkan"
        };

        var humanGroupSizeParameterIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "alife.humanGroupSize.newbie",
            "alife.humanGroupSize.experienced",
            "alife.humanGroupSize.veteran",
            "alife.humanGroupSize.master"
        };

        var mutantGroupSizeParameterIds = new HashSet<string>(StringComparer.Ordinal)
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

        // Keep deprecated stash Weight IDs instantiated for old presets and
        // source patch compatibility, without showing inert Basic mixer knobs.
        var legacyStashWeightIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "loot.stashMixAmmo", "loot.stashMixGrenades", "loot.stashMixCoupons",
            "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
            "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
            "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
            "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance",
            "loot.expensiveStashMoneyChance"
        };

        // Every slider constraint member needs a live VM, even when its
        // value is edited only through a composite visual's handles.
        // Otherwise SliderConstraintCoordinator indexes a missing ID at startup.
        var constrainedParameterIds = catalog.SliderConstraints
            .SelectMany(constraint => constraint.ParameterIds)
            .ToHashSet(StringComparer.Ordinal);

        var parameterVMs = catalog.Parameters
            .Where(p => p.ExposeInUi
                || constrainedParameterIds.Contains(p.Id)
                || legacyStashWeightIds.Contains(p.Id)
                || ammoDistributionParameterIds.Contains(p.Id)
                || stashAmmoParameterIds.Contains(p.Id)
                || mutantDropParameterIds.Contains(p.Id)
                || individualAnomalyDamageParameterIds.Contains(p.Id)
                || worldLootRemovalParameterIds.Contains(p.Id)
                || worldPlacementParameterIds.Contains(p.Id)
                || medicalHealingParameterIds.Contains(p.Id)
                || actionCostParameterIds.Contains(p.Id)
                || exhaustionRecoveryParameterIds.Contains(p.Id)
                || consumableAggregateParameterIds.Contains(p.Id)
                || itemPriceScalerParameterIds.Contains(p.Id)
                || mutantHealthParameterIds.Contains(p.Id)
                || humanGroupSizeParameterIds.Contains(p.Id)
                || mutantGroupSizeParameterIds.Contains(p.Id))
            .Select(p => new ParameterViewModel(p, _selectedReferenceDifficulty))
            .ToList();
        AllParameters = parameterVMs;
        _sliderConstraintCoordinator = new SliderConstraintCoordinator(catalog.SliderConstraints, parameterVMs);

        var globalAnomalyDamage = parameterVMs.FirstOrDefault(p => p.Id == "environment.anomalyDamage");
        if (globalAnomalyDamage is not null)
        {
            _anomalyDerivedParameters = parameterVMs.Where(p => individualAnomalyDamageParameterIds.Contains(p.Id)).ToList();
            foreach (var p in _anomalyDerivedParameters)
                p.SetEffectiveDamageMultiplierProvider(() => globalAnomalyDamage.CurrentNumber);
        }

        var parametersById = parameterVMs.ToDictionary(p => p.Id, StringComparer.Ordinal);

        void WireVisualAggregate(string anchorId, params string[] memberIds)
        {
            if (!parametersById.TryGetValue(anchorId, out var visualAnchor))
                return;

            var members = memberIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            visualAnchor.SetAggregateModifiedProvider(
                () => members.Any(parameter => parameter.IsValueModifiedFromReference()));

            foreach (var visualMember in members)
            {
                visualMember.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName is nameof(ParameterViewModel.CurrentNumber)
                        or nameof(ParameterViewModel.CurrentBoolean)
                        or nameof(ParameterViewModel.CurrentText)
                        or nameof(ParameterViewModel.CurrentMinNumber)
                        or nameof(ParameterViewModel.CurrentMaxNumber))
                    {
                        visualAnchor.RefreshAggregateModifiedState();
                    }
                };
            }
        }

        WireVisualAggregate(
            "survival.sleepinessRate",
            "survival.sleepinessRate",
            "survival.sleepLowThreshold",
            "survival.sleepMediumThreshold",
            "survival.sleepStrongThreshold",
            "survival.energyDrinkFatigueRelief",
            "survival.fatigueLowAimPenalty",
            "survival.fatigueMediumAimPenalty",
            "survival.fatigueStrongAimPenalty",
            "survival.fatigueMediumReloadPenalty",
            "survival.fatigueStrongReloadPenalty");

        WireVisualAggregate(
            "advanced.lowHealthMechanics",
            "advanced.lowHealthMechanics",
            "health.masterLowHealthPenalty");

        WireVisualAggregate(
            "advanced.radiationMechanics",
            "advanced.radiationMechanics",
            "survival.radiationStaminaDrainPenalty");

        WireVisualAggregate(
            "advanced.masterEffectModifierMechanics",
            "advanced.masterEffectModifierMechanics",
            "health.masterBandageModifier",
            "health.masterMedkitModifier",
            "health.masterArmyMedkitModifier");

        WireVisualAggregate(
            "advanced.fatigueMechanics",
            "advanced.fatigueMechanics",
            "survival.severeFatigueMaxStamina",
            "survival.severeFatigueCarryWeight",
            "survival.severeFatigueRecoil",
            "survival.severeFatigueAimSway",
            "survival.severeFatigueDispersion");

        WireVisualAggregate(
            "health.maxHP",
            "health.maxHP",
            "health.baseRegen",
            "health.regenDelay",
            "health.regenerationScalar",
            "health.lowHealthRegen");

        WireVisualAggregate(
            "combat.playerWeaponDamage",
            "combat.playerWeaponDamage",
            "combat.playerHeadshotMultiplier",
            "combat.weaponJamming");

        WireVisualAggregate(
            "combat.npcHealth",
            "combat.npcHealth",
            "combat.npcWeaponDamage",
            "combat.explosionDamage");

        WireVisualAggregate(
            "survival.waterInstantStaminaRestore",
            "survival.waterInstantStaminaRestore",
            "survival.energyDrinkInstantStaminaRestore",
            "survival.waterStaminaEffectStrength",
            "survival.waterStaminaCostReduction",
            "survival.energyDrinkStaminaRecoveryStrength",
            "survival.waterEffectDuration",
            "survival.waterRegenDuration",
            "survival.energyDrinkEffectDuration",
            "survival.energyFullCostDuration",
            "survival.energyPartialCostDuration",
            "survival.vodkaEffectDuration",
            "survival.beerEffectDuration",
            "survival.magicVodkaEffectDuration",
            "survival.vincaBleedRecovery",
            "survival.vincaDuration",
            "survival.herculesCarryBonus",
            "survival.herculesDuration",
            "survival.psiBlockRecovery",
            "survival.psiBlockDuration");

        WireVisualAggregate(
            "survival.sleepEligibilityThreshold",
            "survival.sleepEligibilityThreshold",
            "survival.minimumSleepHours",
            "survival.sleepHealthRestore",
            "survival.sleepStaminaRestore");

        WireVisualAggregate(
            "survival.hungerRate",
            "survival.hungerRate",
            "survival.hungerLowThreshold",
            "survival.hungerMediumThreshold",
            "survival.hungerStrongThreshold",
            "survival.hungerMediumStaminaRegen",
            "survival.hungerStrongStaminaRegen",
            "survival.wellFedStaminaRegen",
            "survival.breadSatiety",
            "survival.sausageSatiety",
            "survival.cannedFoodSatiety",
            "survival.milkSatiety");

        WireVisualAggregate(
            "survival.starvationStaminaDrain",
            "advanced.starvationMechanics",
            "survival.starvationStaminaDrain",
            "survival.starvationDamage",
            "survival.starvationDamageInterval",
            "survival.starvationPenaltyDelay",
            "survival.starvationHealthRegenPenalty");

        WireVisualAggregate(
            "survival.radiationAccumulation",
            "survival.radiationAccumulation",
            "survival.radiationRecovery",
            "survival.radiationDamage");

        // One compact Coupon Flow board owns all three original coupon sources;
        // the reward parameters remain live for presets and CFG generation.
        WireVisualAggregate(
            "economy.startingMoney",
            "economy.startingMoney",
            "economy.mainQuestReward",
            "economy.sideQuestReward");

        WireVisualAggregate(
            "economy.buyCondition",
            "economy.buyCondition",
            "economy.sellCondition",
            "economy.traderMinBuyCondition",
            "economy.itemValueFloor");

        WireVisualAggregate(
            "economy.repairEnemy",
            "economy.repairEnemy",
            "economy.repairDisaffection",
            "economy.repairNeutral",
            "economy.repairFriend");

        WireVisualAggregate(
            "economy.traderAmmoStock",
            "economy.traderAmmoStock",
            "economy.medicStock");

        WireVisualAggregate(
            "economy.fastTravelCost",
            "economy.fastTravelCost",
            "economy.upgradeCost",
            "economy.repairCostBase");

        // Equipment Wear has two independent sources behind one crosshair
        // anchor; both must update the group's modified state.
        WireVisualAggregate(
            "equipment.armorDurability",
            "equipment.armorDurability",
            "equipment.weaponWearPerShot");
        WireVisualAggregate(
            "equipment.ammoWeight",
            "equipment.ammoWeight",
            "equipment.consumableWeight");

        // Global damage is edited inside the Individual Anomaly Damage radar
        // alongside ten confirmed, player-facing source controls. Keep this legacy
        // aggregate source registered for state/provenance, but its standalone
        // visual anchor is hidden in ParameterViewModel.IsControlVisible.
        // Flycatcher and Anomaly Special Effects remain separate.
        WireVisualAggregate("environment.anomalyDamage", "environment.anomalyDamage");
        WireVisualAggregate(
            "environment.flycatcherMovementSpeed",
            "environment.flycatcherMovementSpeed",
            "environment.flycatcherTurnSpeed");
        WireVisualAggregate(
            "environment.razorBleeding",
            "environment.razorBleeding",
            "environment.psyAnomalyIntensity");

        // The NPC card combines distinct native shapes: discrete smart-loot
        // choice, medical and attached-ammo multipliers, and MIN/MAX durability.
        WireVisualAggregate(
            "loot.npcCorpseBonus",
            "loot.npcCorpseBonus",
            "loot.npcMedicalChance",
            "loot.npcWeaponAmmoSupply",
            "loot.droppedWeaponCondition");

        // Existing World Loot toggles stay independent. Weapon density still
        // observes its catalog requiresToggle dependency on weapon shuffle.
        WireVisualAggregate(
            "loot.worldWeaponShuffle",
            "loot.worldWeaponShuffle",
            "loot.worldWeaponDensity",
            "loot.looseUniqueWeaponShuffle",
            "loot.worldAttachmentShuffle",
            "loot.worldDetectors",
            "loot.blueprintShuffle");

        // Mutually exclusive overlapping stash weapon transformations:
        // the last toggle switched ON wins, regardless of which tab is open.
        var weaponShuffle = parametersById["loot.worldWeaponShuffle"];
        var extraGear = parametersById["loot.stashGearEnabled"];
        weaponShuffle.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ParameterViewModel.CurrentBoolean)
                && !_suspendSliderConstraints && !_isApplyingReference
                && weaponShuffle.CurrentBoolean && extraGear.CurrentBoolean)
                extraGear.CurrentBoolean = false;
        };
        extraGear.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ParameterViewModel.CurrentBoolean)
                && !_suspendSliderConstraints && !_isApplyingReference
                && extraGear.CurrentBoolean && weaponShuffle.CurrentBoolean)
                weaponShuffle.CurrentBoolean = false;
        };

        // Tier ammo quantity and independent native Chance editors share one
        // visual family; legacy tier-wide multipliers remain preset-compatible.
        WireVisualAggregate(
            "loot.stashMixMedical",
            "loot.stashMixAmmo",
            "loot.stashMixMedical",
            "loot.stashMixFood",
            "loot.stashMixDrinks",
            "loot.stashMixGrenades",
            "loot.stashMixCoupons",
            "loot.cheapStashAmmoQuantity",
            "loot.mediumStashAmmoQuantity",
            "loot.expensiveStashAmmoQuantity",
            "loot.standardStashChanceScale",
            "loot.rareStashChanceScale",
            "loot.mediumStashMedicalChance",
            "loot.mediumStashDrinksChance",
            "loot.mediumStashCouponsChance",
            "loot.expensiveStashAmmoChance",
            "loot.expensiveStashMedicalChance",
            "loot.expensiveStashFoodChance",
            "loot.expensiveStashDrinksChance",
            "loot.expensiveStashGrenadesChance",
            "loot.cheapStashGrenadeChance",
            "loot.cheapStashGrenadeQuantity",
            "loot.expensiveStashMoneyChance");

        // One visible global Ammo Quantity is authoritative. The existing
        // three persisted writer IDs remain the separate actual CFG routes.
        // Mirror changes to all three, including presets and reset paths.
        var stashAmmoSources = new[]
        {
            "loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity",
            "loot.expensiveStashAmmoQuantity"
        }.Select(id => parametersById[id]).ToArray();
        bool syncingStashAmmo = false;
        void SyncStashAmmoQuantity()
        {
            if (syncingStashAmmo) return;
            syncingStashAmmo = true;
            try
            {
                var value = stashAmmoSources[0].CurrentNumber;
                foreach (var dependent in stashAmmoSources.Skip(1))
                    if (Math.Abs(dependent.CurrentNumber - value) > 0.0000001)
                        dependent.SnapCurrentNumberToMarker(value);
            }
            finally { syncingStashAmmo = false; }
        }
        stashAmmoSources[0].PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                SyncStashAmmoQuantity();
        };
        SyncStashAmmoQuantity();

        // One native relative-bias chart for actual medical ItemPrototypeSIDs
        // and one separate Medkit/Army stack floor/cap editor (0 = AUTO).
        WireVisualAggregate(
            "loot.stashMedicalBandageBias",
            "loot.stashMedicalBandageBias",
            "loot.stashMedicalMedkitBias",
            "loot.stashMedicalArmyMedkitBias",
            "loot.stashMedicalAntiRadBias",
            "loot.stashMedicalBandageChanceScale",
            "loot.stashMedicalMedkitChanceScale",
            "loot.stashMedicalArmyMedkitChanceScale",
            "loot.stashMedicalAntiRadChanceScale",
            "loot.stashMedkitMinCount",
            "loot.stashMedkitMaxCount",
            "loot.stashArmyMedkitMinCount",
            "loot.stashArmyMedkitMaxCount");

        WireVisualAggregate(
            "loot.stashGearEnabled",
            "loot.stashGearEnabled",
            "loot.stashGearCoveragePercent",
            "loot.stashGearWeaponWeight",
            "loot.stashGearArmorWeight",
            "loot.stashGearArtifactWeight",
            "loot.stashGearAttachmentWeight",
            "loot.stashGearBonusChance");

        // Breakable box quantities and relative SNG/NATO ammo weights are
        // independent sources in one compact control, not duplicate editors.
        WireVisualAggregate(
            "loot.ammoBoxWeight918",
            "loot.destructibleAmmoQuantity",
            "loot.destructibleGrenadeQuantity",
            "loot.ammoBoxWeight918",
            "loot.ammoBoxWeight045",
            "loot.ammoBoxWeight545",
            "loot.ammoBoxWeight012");

        if (parametersById.TryGetValue("survival.beerDrunknessStrength", out var alcoholAnchor))
        {
            parametersById.TryGetValue("survival.vodkaDrunknessStrength", out var vodkaStrength);

            alcoholAnchor.SetAggregateModifiedProvider(
                () => Math.Abs(alcoholAnchor.CurrentNumber - 15.0) > 0.0000001
                    || (vodkaStrength is not null && Math.Abs(vodkaStrength.CurrentNumber - 30.0) > 0.0000001));

            alcoholAnchor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                    alcoholAnchor.RefreshAggregateModifiedState();
            };

            if (vodkaStrength is not null)
            {
                vodkaStrength.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        alcoholAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        if (parametersById.TryGetValue("survival.bleedingSeverity", out var bleedingCurveAnchor))
        {
            var bleedingCurveIds = new[]
            {
                "survival.bleedingSeverity",
                "survival.bleedingRecovery",
                "survival.bleedingLowDamage",
                "survival.bleedingMediumDamage",
                "survival.bleedingStrongDamage",
                "survival.bleedingMediumThreshold",
                "survival.bleedingStrongThreshold"
            };

            var bleedingCurveMembers = bleedingCurveIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            bleedingCurveAnchor.SetAggregateModifiedProvider(
                () => bleedingCurveMembers.Any(parameter =>
                {
                    if (parameter.Definition.Vanilla.TryGetValue(_selectedReferenceDifficulty, out var difficultyVanilla)
                        && difficultyVanilla.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        return Math.Abs(parameter.CurrentNumber - difficultyVanilla.GetDouble()) > 0.0000001;
                    }

                    return parameter.Definition.Vanilla.TryGetValue("All", out var sharedVanilla)
                        && sharedVanilla.ValueKind == System.Text.Json.JsonValueKind.Number
                        && Math.Abs(parameter.CurrentNumber - sharedVanilla.GetDouble()) > 0.0000001;
                }));

            foreach (var member in bleedingCurveMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        bleedingCurveAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        // Stash Ammo is no longer a separate special editor. The native
        // Stash Rewards aggregates only three tier ammo stack amounts. The
        // existing RGD-5 Chance/Amount pair and Rare Coupons Chance are
        // members of StashContentMix; production/preset IDs do not change.

        if (parametersById.TryGetValue("loot.mutant.blindDogChance", out var mutantDropAnchor))
        {
            var mutantDropMembers = mutantDropParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            mutantDropAnchor.SetAggregateModifiedProvider(
                () => mutantDropMembers.Any(parameter =>
                    parameter.Definition.Vanilla.TryGetValue("All", out var vanilla)
                    && vanilla.ValueKind == System.Text.Json.JsonValueKind.Number
                    && Math.Abs(parameter.CurrentNumber - vanilla.GetDouble()) > 0.0000001));

            foreach (var member in mutantDropMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        mutantDropAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        if (parametersById.TryGetValue("environment.electroAnomalyDamage", out var individualAnomalyDamageAnchor))
        {
            var individualAnomalyDamageMembers = individualAnomalyDamageParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            individualAnomalyDamageAnchor.SetAggregateModifiedProvider(
                () => individualAnomalyDamageMembers.Any(parameter =>
                    parameter.Definition.Vanilla.TryGetValue("All", out var vanilla)
                    && vanilla.ValueKind == System.Text.Json.JsonValueKind.Number
                    && Math.Abs(parameter.CurrentNumber - vanilla.GetDouble()) > 0.0000001));

            foreach (var member in individualAnomalyDamageMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        individualAnomalyDamageAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        if (parametersById.TryGetValue("loot.worldAmmoCratesRemoved", out var worldLootRemovalAnchor))
        {
            var worldLootRemovalMembers = worldLootRemovalParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            worldLootRemovalAnchor.SetAggregateModifiedProvider(
                () => worldLootRemovalMembers.Any(parameter => Math.Abs(parameter.CurrentNumber) > 0.0000001));

            foreach (var member in worldLootRemovalMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        worldLootRemovalAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        if (parametersById.TryGetValue("health.bandageHealing", out var medicalHealingAnchor))
        {
            var medicalHealingMembers = medicalHealingParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            medicalHealingAnchor.SetAggregateModifiedProvider(
                () => medicalHealingMembers.Any(parameter =>
                    parameter.IsValueModifiedFromReference()));

            foreach (var member in medicalHealingMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName is nameof(ParameterViewModel.CurrentNumber)
                        or nameof(ParameterViewModel.CurrentBoolean))
                        medicalHealingAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        if (parametersById.TryGetValue("stamina.sprintCost", out var actionCostAnchor))
        {
            var actionCostMembers = actionCostParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            actionCostAnchor.SetAggregateModifiedProvider(
                () => actionCostMembers.Any(parameter =>
                    parameter.Definition.Vanilla.TryGetValue("All", out var vanilla)
                    && vanilla.ValueKind == System.Text.Json.JsonValueKind.Number
                    && Math.Abs(parameter.CurrentNumber - vanilla.GetDouble()) > 0.0000001));

            foreach (var member in actionCostMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        actionCostAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        if (parametersById.TryGetValue("stamina.sprintDisableThreshold", out var exhaustionRecoveryAnchor))
        {
            var exhaustionRecoveryMembers = exhaustionRecoveryParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            exhaustionRecoveryAnchor.SetAggregateModifiedProvider(
                () => exhaustionRecoveryMembers.Any(parameter =>
                    parameter.Definition.Vanilla.TryGetValue("All", out var vanilla)
                    && vanilla.ValueKind == System.Text.Json.JsonValueKind.Number
                    && Math.Abs(parameter.CurrentNumber - vanilla.GetDouble()) > 0.0000001));

            foreach (var member in exhaustionRecoveryMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        exhaustionRecoveryAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        if (parametersById.TryGetValue("economy.consumableCost", out var itemPriceScalerAnchor))
        {
            var itemPriceScalerMembers = itemPriceScalerParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            itemPriceScalerAnchor.SetAggregateModifiedProvider(
                () => itemPriceScalerMembers.Any(parameter => Math.Abs(parameter.CurrentNumber - 1.0) > 0.0000001));

            foreach (var member in itemPriceScalerMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        itemPriceScalerAnchor.RefreshAggregateModifiedState();
                };
            }
        }

        if (parametersById.TryGetValue("combat.mutantHealth.bloodsucker", out var mutantHealthAnchor))
        {
            var mutantHealthMembers = mutantHealthParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();

            mutantHealthAnchor.SetAggregateModifiedProvider(
                () => mutantHealthMembers.Any(parameter => Math.Abs(parameter.CurrentNumber - 1.0) > 0.0000001));

            foreach (var member in mutantHealthMembers)
            {
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        mutantHealthAnchor.RefreshAggregateModifiedState();
                };
            }
        }


        if (parametersById.TryGetValue("alife.humanGroupSize.newbie", out var humanGroupSizeAnchor))
        {
            var members = humanGroupSizeParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();
            humanGroupSizeAnchor.SetAggregateModifiedProvider(
                () => members.Any(parameter => Math.Abs(parameter.CurrentNumber - 1.0) > 0.0000001));
            foreach (var member in members)
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        humanGroupSizeAnchor.RefreshAggregateModifiedState();
                };
        }

        if (parametersById.TryGetValue("alife.mutantSpawn.bloodsucker", out var mutantGroupSizeAnchor))
        {
            var members = mutantGroupSizeParameterIds
                .Select(id => parametersById.TryGetValue(id, out var parameter) ? parameter : null)
                .Where(parameter => parameter is not null)
                .Cast<ParameterViewModel>()
                .ToArray();
            mutantGroupSizeAnchor.SetAggregateModifiedProvider(
                () => members.Any(parameter => Math.Abs(parameter.CurrentNumber - 1.0) > 0.0000001));
            foreach (var member in members)
                member.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                        mutantGroupSizeAnchor.RefreshAggregateModifiedState();
                };
        }

        // Medical Items: editor-only common physical ruler ceilings. Every
        // source retains its own value and CFG writer; no synthetic CFG field.
        // OFF restores native catalog maxima, clamping only excess values.
        var medicalMode = parametersById["health.bandageHealing"];
        var medicalHealingIds = new[]
        {
            "health.bandageHealing", "health.medkitHealing",
            "health.armyMedkitHealing", "health.ecoMedkitHealing"
        };
        var medicalBleedingIds = new[]
        {
            "health.bandageBleeding", "health.medkitBleeding",
            "health.armyMedkitBleeding", "health.ecoMedkitBleeding"
        };
        var medicalRadiationIds = new[]
        {
            "health.ecoMedkitAntirad", "health.antiRadRadiation",
            "health.beerRadiationRemoval", "health.vodkaRadiationRemoval"
        };
        var medicalSources = new List<ParameterViewModel>();
        foreach (var (ids, expandedMaximum) in new[]
        {
            (medicalHealingIds, 200.0),
            (medicalBleedingIds, 100.0),
            (medicalRadiationIds, 200.0)
        })
        {
            foreach (var id in ids)
            {
                var source = parametersById[id];
                medicalSources.Add(source);
                source.SetDynamicMaximumProvider(() => medicalMode.MedicalOverrideEnabled
                    ? expandedMaximum : source.Definition.Slider!.Max);
            }
        }
        medicalMode.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ParameterViewModel.MedicalOverrideEnabled))
                foreach (var source in medicalSources)
                    source.RefreshDynamicMaximum();
        };

        // One UI-only, preset-backed duration override covers all consumables.
        // Drinks and their children retain the 270s ruler; medicines share
        // a separate 900s ruler, with native limits restored when OFF.
        // OFF restores source-native caps, with no fictional CFG boolean.
        var drinkDurationMode = parametersById["survival.waterInstantStaminaRestore"];
        const double sharedDrinkDurationMaximumSeconds = 270.0;
        var drinkMainSeconds = new (string Id, double VanillaSeconds)[]
        {
            ("survival.waterEffectDuration", 30.0),
            ("survival.energyDrinkEffectDuration", 45.0),
            ("survival.vodkaEffectDuration", 20.0),
            ("survival.beerEffectDuration", 20.0),
            ("survival.magicVodkaEffectDuration", 90.0)
        };
        var drinkMainParameters = drinkMainSeconds
            .Select(source => parametersById[source.Id]).ToArray();
        // Source-native values are multipliers for the five parents but real
        // seconds for their children. Normalize at the ViewModel boundary,
        // not only at the mouse control, so no 0.5s values leak through.
        foreach (var (id, baseSeconds) in drinkMainSeconds)
            parametersById[id].SetDrinkDurationStepProvider(
                () => (drinkDurationMode.DrinkDurationOverrideEnabled ? 5.0 : 1.0) / baseSeconds);
        foreach (var id in new[] { "survival.waterRegenDuration",
                     "survival.energyFullCostDuration", "survival.energyPartialCostDuration" })
            parametersById[id].SetDrinkDurationStepProvider(
                () => drinkDurationMode.DrinkDurationOverrideEnabled ? 5.0 : 1.0);
        foreach (var (id, vanillaSeconds) in drinkMainSeconds)
        {
            parametersById[id].SetDynamicMaximumProvider(() =>
                drinkDurationMode.DrinkDurationOverrideEnabled
                    ? sharedDrinkDurationMaximumSeconds / vanillaSeconds
                    : parametersById[id].Definition.Slider!.Max);
        }

        // Three independent direct-seconds medicine sources share the existing
        // override toggle, retaining their different native caps when OFF.
        const double sharedMedicineDurationMaximumSeconds = 900.0;
        var medicineDurationParameters = new[]
        {
            parametersById["survival.vincaDuration"],
            parametersById["survival.herculesDuration"],
            parametersById["survival.psiBlockDuration"]
        };
        foreach (var medicineDuration in medicineDurationParameters)
            medicineDuration.SetDynamicMaximumProvider(() =>
                drinkDurationMode.DrinkDurationOverrideEnabled
                    ? sharedMedicineDurationMaximumSeconds
                    : medicineDuration.Definition.Slider!.Max);

        // Real-seconds source caps for the three independently mapped short
        // effects. If the main duration shrinks, children are clamped to its
        // new duration; native minima are always reachable because both main
        // sliders have a catalog floor matching their longest vanilla child.
        void LinkShortDrinkDuration(string childId, string parentId, double mainVanillaSeconds)
        {
            var parent = parametersById[parentId];
            var child = parametersById[childId];
            // The secondary effect may never outlast its own drink, even
            // when Override Limits expands the main drink's available range.
            child.SetDynamicMaximumProvider(() => parent.CurrentNumber * mainVanillaSeconds);
            parent.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                    child.RefreshDynamicMaximum();
            };
        }
        LinkShortDrinkDuration("survival.waterRegenDuration", "survival.waterEffectDuration", 30.0);
        LinkShortDrinkDuration("survival.energyFullCostDuration", "survival.energyDrinkEffectDuration", 45.0);
        LinkShortDrinkDuration("survival.energyPartialCostDuration", "survival.energyDrinkEffectDuration", 45.0);
        var shortDrinkParameters = new[]
        {
            parametersById["survival.waterRegenDuration"],
            parametersById["survival.energyFullCostDuration"],
            parametersById["survival.energyPartialCostDuration"]
        };
        drinkDurationMode.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(ParameterViewModel.DrinkDurationOverrideEnabled))
                return;
            // Restrict primary maxima before child maxima when turning OFF.
            // A child whose value now exceeds its parent is clamped minimally;
            // unaffected values are never rewritten.
            foreach (var main in drinkMainParameters)
                main.RefreshDynamicMaximum();
            foreach (var child in shortDrinkParameters)
                child.RefreshDynamicMaximum();
            foreach (var medicineDuration in medicineDurationParameters)
                medicineDuration.RefreshDynamicMaximum();
        };

        var dependencyChildren = new Dictionary<string, List<ParameterViewModel>>(StringComparer.Ordinal);
        foreach (var child in parameterVMs.Where(p => !string.IsNullOrWhiteSpace(p.Definition.RequiresToggle)))
        {
            if (!parametersById.TryGetValue(child.Definition.RequiresToggle, out var parent) || !parent.IsToggle)
                continue;

            child.SetDependencyEnabledProvider(() => parent.CurrentBoolean);
            if (!dependencyChildren.TryGetValue(parent.Id, out var children))
            {
                children = new List<ParameterViewModel>();
                dependencyChildren[parent.Id] = children;
            }
            children.Add(child);
        }

        foreach (var p in parameterVMs)
        {
            // Constraints are driven directly by the slider's CurrentNumber property change.
            // This keeps the cascade tied to the actual TwoWay WPF binding update instead of
            // relying on the broader derived-state notification path.
            p.PropertyChanged += (_, args) =>
            {
                if (!_suspendSliderConstraints
                    && args.PropertyName == nameof(ParameterViewModel.CurrentNumber))
                {
                    _sliderConstraintCoordinator.ApplyFrom(p);
                    EnforceStashMedicalQuantityBounds(p);
                }
            };

            p.StateChanged += (_, _) =>
            {
                if (dependencyChildren.TryGetValue(p.Id, out var children))
                {
                    foreach (var child in children)
                        child.RefreshDependencyState();

                    // If the currently selected child was just hidden by its parent toggle,
                    // do not leave the rationale panel pointing at a control that no longer
                    // exists in the visible list.
                    SelectedTab?.EnsureVisibleSelection();
                }
                RefreshState();
            };
        }

        foreach (var tab in catalog.Tabs.OrderBy(t => t.Order))
        {
            Tabs.Add(new TabViewModel(
                tab.Id,
                tab.Label,
                parameterVMs.Where(p => p.Definition.Tab == tab.Id)
                            .OrderBy(ParameterSortOrder)
                            .ThenBy(p => p.Group)
                            .ThenBy(p => p.Label)));
        }

        SelectedTab = Tabs.FirstOrDefault();

        ResetCurrentTabCommand = new RelayCommand(_ => ResetCurrentTab(), _ => SelectedTab is not null);
        ResetAllCommand = new RelayCommand(_ => ApplyReferenceDifficulty(resetAll: true));
        SavePresetCommand = new RelayCommand(_ => SavePreset());
        LoadPresetCommand = new RelayCommand(_ => LoadPreset());
        BuildCommand = new RelayCommand(_ => _ = BuildAsync(), _ => !IsBuilding);
    }

    public string BuildPresetName
        => string.IsNullOrWhiteSpace(_loadedPresetName)
            ? "Custom"
            : _loadedPresetName;

    public PresetListItem? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (!SetProperty(ref _selectedPreset, value) || value is null || _suppressPresetSelection)
                return;

            LoadPresetFromPath(value.Path);
        }
    }

    public string SelectedReferenceDifficulty
    {
        get => _selectedReferenceDifficulty;
        set
        {
            if (!ReferenceDifficulties.Contains(value))
                return;

            if (SetProperty(ref _selectedReferenceDifficulty, value))
            {
                OnPropertyChanged(nameof(BasePresetIndex));
                if (!_isApplyingReference)
                    ApplyReferenceDifficulty(resetAll: true);
            }
        }
    }

    // UI-only numeric position for the four-step Base Preset slider.
    // Presets continue to store the stable difficulty name, not this index.
    public double BasePresetIndex
    {
        get
        {
            var index = ReferenceDifficulties.IndexOf(_selectedReferenceDifficulty);
            return index < 0 ? 0 : index;
        }
        set
        {
            var index = (int)Math.Clamp(
                Math.Round(value, MidpointRounding.AwayFromZero),
                0,
                ReferenceDifficulties.Count - 1);
            SelectedReferenceDifficulty = ReferenceDifficulties[index];
        }
    }

    public TabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }



    // One-way coupling: MIN is authoritative; a MAX below MIN is raised,
    // never the other way around. Applied to live edits and bulk/preset states.
    private void EnforceStashMedicalQuantityBounds(ParameterViewModel? edited = null)
    {
        var pairs = new (string Min, string Max)[]
        {
            ("loot.stashMedkitMinCount", "loot.stashMedkitMaxCount"),
            ("loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount")
        };
        foreach (var pair in pairs)
        {
            if (edited is not null && edited.Id != pair.Min && edited.Id != pair.Max)
                continue;

            var min = AllParameters.First(p => p.Id == pair.Min);
            var max = AllParameters.First(p => p.Id == pair.Max);
            // UI MAX=0 is stored -1, UI MAX=5 uses stored 0.
            // When MAX is dragged below MIN, lower MIN first; when MIN is
            // raised above MAX, raise MAX instead. Old presets remain intact.
            var cap = max.CurrentNumber < 0.0 ? 0.0 :
                max.CurrentNumber == 0.0 ? 5.0 : max.CurrentNumber;
            if (min.CurrentNumber > cap)
            {
                if (edited?.Id == pair.Max)
                    min.CurrentNumber = Math.Max(0.0, cap);
                else
                    max.CurrentNumber = min.CurrentNumber;
            }
        }
    }

    private void NormalizeEditorConstraints()
    {
        // Legacy presets with both ON: deterministically prefer Extra Gear.
        var gear = AllParameters.First(p => p.Id == "loot.stashGearEnabled");
        var shuffle = AllParameters.First(p => p.Id == "loot.worldWeaponShuffle");
        if (gear.CurrentBoolean && shuffle.CurrentBoolean)
            shuffle.CurrentBoolean = false;
        _sliderConstraintCoordinator.NormalizeAll();
        EnforceStashMedicalQuantityBounds();
        // Called after preset load, reference changes and before CFG builds:
        // the single visible Basic anchor controls all three native routes.
        var ammoIds = new[] { "loot.cheapStashAmmoQuantity",
            "loot.mediumStashAmmoQuantity", "loot.expensiveStashAmmoQuantity" };
        var ammo = AllParameters.FirstOrDefault(p => p.Id == ammoIds[0]);
        if (ammo is not null)
            foreach (var id in ammoIds.Skip(1))
            {
                var target = AllParameters.FirstOrDefault(p => p.Id == id);
                if (target is not null && Math.Abs(target.CurrentNumber - ammo.CurrentNumber) > 0.0000001)
                    target.SnapCurrentNumberToMarker(ammo.CurrentNumber);
            }
    }

    private void ApplyReferenceDifficulty(bool resetAll)
    {
        _isApplyingReference = true;
        _suspendSliderConstraints = true;
        try
        {
            foreach (var p in AllParameters)
                p.ChangeReferenceDifficulty(_selectedReferenceDifficulty, resetAll);
        }
        finally
        {
            _suspendSliderConstraints = false;
            _isApplyingReference = false;
        }

        if (resetAll)
        {
            AllParameters.First(p => p.Id == "survival.waterInstantStaminaRestore")
                .DrinkDurationOverrideEnabled = false;
            AllParameters.First(p => p.Id == "health.bandageHealing")
                .MedicalOverrideEnabled = false;
        }
        NormalizeEditorConstraints();
        RefreshState();
    }

    private void ResetCurrentTab()
    {
        if (SelectedTab is null)
            return;

        _suspendSliderConstraints = true;
        try
        {
            if (SelectedTab.Id == "health")
                AllParameters.First(p => p.Id == "health.bandageHealing")
                    .MedicalOverrideEnabled = false;
            if (SelectedTab.Id == "survival")
                AllParameters.First(p => p.Id == "survival.waterInstantStaminaRestore")
                    .DrinkDurationOverrideEnabled = false;
            foreach (var p in SelectedTab.Parameters)
                p.ResetToReference(false);
        }
        finally
        {
            _suspendSliderConstraints = false;
        }

        NormalizeEditorConstraints();
        RefreshState();
    }

    private void RefreshPresetList(string? selectPath = null)
    {
        var items = _presetDirectoryService.List();
        AvailablePresets.Clear();
        foreach (var item in items)
            AvailablePresets.Add(item);

        if (!string.IsNullOrWhiteSpace(selectPath))
            SelectPresetPath(selectPath);
    }

    private void SelectPresetPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var match = AvailablePresets.FirstOrDefault(p =>
            string.Equals(Path.GetFullPath(p.Path), fullPath, StringComparison.OrdinalIgnoreCase));

        _suppressPresetSelection = true;
        try
        {
            SelectedPreset = match;
        }
        finally
        {
            _suppressPresetSelection = false;
        }
    }

    private static bool RequiresPositiveTreatmentMagnitudePresetMigration(string? toolVersion)
    {
        if (string.IsNullOrWhiteSpace(toolVersion))
            return true;

        return !Version.TryParse(toolVersion, out var parsed)
            || parsed < new Version(0, 2, 45);
    }

    private static bool IsPositiveTreatmentMagnitudeParameter(string parameterId) => parameterId is
        "health.bandageBleeding" or
        "health.medkitBleeding" or
        "health.armyMedkitBleeding" or
        "health.ecoMedkitBleeding" or
        "health.ecoMedkitAntirad" or
        "health.antiRadRadiation" or
        "health.beerRadiationRemoval" or
        "health.vodkaRadiationRemoval";

    private static bool RequiresPositiveAimingPenaltyMagnitudePresetMigration(string? toolVersion)
    {
        if (string.IsNullOrWhiteSpace(toolVersion))
            return true;

        return !Version.TryParse(toolVersion, out var parsed)
            || parsed < new Version(0, 2, 46);
    }

    private static bool RequiresPositiveHungerPenaltyMagnitudePresetMigration(string? toolVersion)
    {
        if (string.IsNullOrWhiteSpace(toolVersion))
            return true;

        return !Version.TryParse(toolVersion, out var parsed)
            || parsed < new Version(0, 2, 47);
    }

    private static bool IsPositiveHungerPenaltyMagnitudeParameter(string parameterId) => parameterId is
        "survival.hungerMediumStaminaRegen" or
        "survival.hungerStrongStaminaRegen";

    private static bool RequiresPositiveMasterPenaltyMagnitudePresetMigration(string? toolVersion)
    {
        if (string.IsNullOrWhiteSpace(toolVersion))
            return true;

        return !Version.TryParse(toolVersion, out var parsed)
            || parsed < new Version(0, 2, 50);
    }

    private static bool IsPositiveMasterPenaltyMagnitudeParameter(string parameterId) => parameterId is
        "health.masterLowHealthPenalty" or
        "survival.starvationHealthRegenPenalty" or
        "survival.severeFatigueMaxStamina" or
        "survival.severeFatigueCarryWeight";

    private static bool RequiresCrateReductionPresetMigration(string? toolVersion)
    {
        if (string.IsNullOrWhiteSpace(toolVersion))
            return true;

        return !Version.TryParse(toolVersion, out var parsed)
            || parsed < new Version(0, 3, 5);
    }

    private static bool RequiresWorldLootSliderPresetMigration(string? toolVersion)
    {
        if (string.IsNullOrWhiteSpace(toolVersion))
            return true;
        return !Version.TryParse(toolVersion, out var parsed) || parsed < new Version(0, 4, 7);
    }

    private void SavePreset()
    {
        NormalizeEditorConstraints();

        var dialog = new SaveFileDialog
        {
            Title = "Save True Custom Difficulty preset",
            Filter = "JSON preset (*.json)|*.json",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = "custom-preset.json",
            // Save requires create-file rights, not merely the ability to
            // overwrite already-existing factory JSONs.
            InitialDirectory = _presetDirectoryService.EnsureWritable()
        };
        if (dialog.ShowDialog() != true)
            return;

        var preset = new PresetFile
        {
            PresetFormatVersion = PresetService.CurrentPresetVersion,
            Name = Path.GetFileNameWithoutExtension(dialog.FileName),
            ToolVersion = AppVersion,
            GameVersion = GameVersion,
            ReferenceDifficulty = SelectedReferenceDifficulty,
            DrinkDurationOverride = AllParameters.First(p =>
                p.Id == "survival.waterInstantStaminaRestore").DrinkDurationOverrideEnabled,
            MedicalOverride = AllParameters.First(p =>
                p.Id == "health.bandageHealing").MedicalOverrideEnabled,
            SavedAtUtc = DateTimeOffset.UtcNow,
            // Save the complete control state, not only modified values.
            // This makes a preset deterministic when loaded into a fresh session.
            Values = AllParameters.ToDictionary(
                p => p.Id,
                p => p.ToPresetValue(),
                StringComparer.Ordinal)
        };

        try
        {
            _presetService.Save(preset, dialog.FileName);
            RefreshPresetList(Path.GetFullPath(dialog.FileName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(
                $"Preset could not be saved.\n\n{ex.Message}",
                AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void LoadPreset()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load True Custom Difficulty preset",
            Filter = "JSON preset (*.json)|*.json",
            DefaultExt = ".json",
            InitialDirectory = _presetDirectoryService.Ensure()
        };
        if (dialog.ShowDialog() != true)
            return;

        LoadPresetFromPath(dialog.FileName);
    }

    private void LoadPresetFromPath(string path)
    {
        try
        {
            var preset = _presetService.Load(path);

            if (!ReferenceDifficulties.Contains(preset.ReferenceDifficulty))
            {
                throw new InvalidDataException(
                    $"Preset base preset '{preset.ReferenceDifficulty}' is not supported.");
            }

            // Always reset every current control to the preset's baseline first.
            // Older presets may not contain parameters added by newer tool versions;
            // those controls therefore land on vanilla instead of keeping stale session state.
            // Constraint propagation is suspended for the bulk import so file ordering cannot
            // influence the result; one deterministic normalization pass runs afterwards.
            _isApplyingReference = true;
            _suspendSliderConstraints = true;
            try
            {
                _selectedReferenceDifficulty = preset.ReferenceDifficulty;
                OnPropertyChanged(nameof(SelectedReferenceDifficulty));
                OnPropertyChanged(nameof(BasePresetIndex));

                foreach (var parameter in AllParameters)
                    parameter.ChangeReferenceDifficulty(_selectedReferenceDifficulty, true);

                var parametersById = AllParameters.ToDictionary(p => p.Id, StringComparer.Ordinal);
                // Restore the editor-range mode BEFORE importing seconds/multipliers
                // so values up to 270s are not truncated at old per-drink caps.
                parametersById["survival.waterInstantStaminaRestore"]
                    .DrinkDurationOverrideEnabled = preset.DrinkDurationOverride;
                parametersById["health.bandageHealing"]
                    .MedicalOverrideEnabled = preset.MedicalOverride;
                var migratePositiveTreatmentMagnitude = RequiresPositiveTreatmentMagnitudePresetMigration(preset.ToolVersion);
                var migratePositiveAimingPenaltyMagnitude = RequiresPositiveAimingPenaltyMagnitudePresetMigration(preset.ToolVersion);
                var migratePositiveHungerPenaltyMagnitude = RequiresPositiveHungerPenaltyMagnitudePresetMigration(preset.ToolVersion);
                var migratePositiveMasterPenaltyMagnitude = RequiresPositiveMasterPenaltyMagnitudePresetMigration(preset.ToolVersion);
                var migrateCrateReduction = RequiresCrateReductionPresetMigration(preset.ToolVersion);
                var migrateWorldLootSliders = RequiresWorldLootSliderPresetMigration(preset.ToolVersion);

                if (migrateCrateReduction)
                {
                    // v0.3.4 briefly exposed binary Spawn X Crates toggles where false meant
                    // remove 100% of the corresponding audited category. Preserve that exact
                    // gameplay when loading such a preset into the v0.3.5+ category+percentage model.
                    var migratedAnyCrateRemoval = false;
                    if (preset.Values.TryGetValue("loot.spawnAmmoCrates", out var oldAmmo)
                        && oldAmmo.Boolean == false
                        && parametersById.TryGetValue("loot.reduceAmmoCrates", out var ammoReduction))
                    {
                        ammoReduction.CurrentBoolean = true;
                        migratedAnyCrateRemoval = true;
                    }

                    if (preset.Values.TryGetValue("loot.spawnFoodDrinkCrates", out var oldFood)
                        && oldFood.Boolean == false
                        && parametersById.TryGetValue("loot.reduceFoodDrinkCrates", out var foodReduction))
                    {
                        foodReduction.CurrentBoolean = true;
                        migratedAnyCrateRemoval = true;
                    }

                    if (preset.Values.TryGetValue("loot.spawnMedicalCrates", out var oldMedical)
                        && oldMedical.Boolean == false
                        && parametersById.TryGetValue("loot.reduceMedicalCrates", out var medicalReduction))
                    {
                        medicalReduction.CurrentBoolean = true;
                        migratedAnyCrateRemoval = true;
                    }

                    if (migratedAnyCrateRemoval
                        && parametersById.TryGetValue("loot.crateRemovalPercent", out var cratePercent))
                    {
                        cratePercent.CurrentNumber = 1.0;
                    }
                }

                if (migrateWorldLootSliders)
                {
                    // v0.4.8 removes the World Loot enable checkboxes and the shared crate percentage.
                    // Preserve old gameplay by folding enable-state + amount into each standalone slider.
                    var sharedCrateFraction = preset.Values.TryGetValue("loot.crateRemovalPercent", out var oldCratePercent)
                        && oldCratePercent.Number.HasValue
                        ? Math.Clamp(oldCratePercent.Number.Value, 0.0, 1.0)
                        : 0.0;

                    void MigrateCrate(string newId, string oldToggleId, string legacySpawnId)
                    {
                        var enabled = preset.Values.TryGetValue(oldToggleId, out var oldToggle) && oldToggle.Boolean == true;
                        if (!enabled && preset.Values.TryGetValue(legacySpawnId, out var oldSpawn) && oldSpawn.Boolean == false)
                        {
                            enabled = true;
                            if (sharedCrateFraction <= 0.0) sharedCrateFraction = 1.0;
                        }
                        if (parametersById.TryGetValue(newId, out var target))
                            target.CurrentNumber = enabled ? sharedCrateFraction : 0.0;
                    }

                    MigrateCrate("loot.worldAmmoCratesRemoved", "loot.reduceAmmoCrates", "loot.spawnAmmoCrates");
                    MigrateCrate("loot.worldFoodDrinkCratesRemoved", "loot.reduceFoodDrinkCrates", "loot.spawnFoodDrinkCrates");
                    MigrateCrate("loot.worldMedicalCratesRemoved", "loot.reduceMedicalCrates", "loot.spawnMedicalCrates");

                    var weaponEnabled = preset.Values.TryGetValue("loot.worldWeaponShuffle", out var oldWeaponToggle)
                                        && oldWeaponToggle.Boolean == true;
                    if (!weaponEnabled
                        && preset.Values.TryGetValue("loot.worldWeaponShuffleAmount", out var oldWeaponAmount)
                        && oldWeaponAmount.Number.GetValueOrDefault() > 0.0)
                        weaponEnabled = true;
                    if (parametersById.TryGetValue("loot.worldWeaponShuffle", out var weaponToggle))
                        weaponToggle.CurrentBoolean = weaponEnabled;
                }

                foreach (var pair in preset.Values)
                {
                    if (!parametersById.TryGetValue(pair.Key, out var parameter))
                        continue;

                    if (migratePositiveTreatmentMagnitude
                        && IsPositiveTreatmentMagnitudeParameter(pair.Key)
                        && pair.Value.Number is < 0.0)
                    {
                        // Before v0.2.45 direct bleeding/radiation treatment controls exposed
                        // the engine's negative delta. The UI now stores an intuitive positive
                        // removal magnitude while the writer negates it back to the CFG value.
                        var migrated = new PresetValue
                        {
                            Kind = pair.Value.Kind,
                            Number = Math.Abs(pair.Value.Number.Value),
                            MinNumber = pair.Value.MinNumber,
                            MaxNumber = pair.Value.MaxNumber,
                            Boolean = pair.Value.Boolean,
                            Text = pair.Value.Text
                        };
                        parameter.ApplyPresetValue(migrated);
                    }
                    else if (migratePositiveAimingPenaltyMagnitude
                        && pair.Key == "stamina.aimRegenCoef"
                        && pair.Value.Number is < 0.0)
                    {
                        // Before v0.2.46 Aiming Stamina Cost exposed the engine's negative
                        // regeneration coefficient. The UI now stores a positive recovery-penalty
                        // magnitude and the writer negates it back to the original CFG convention.
                        var migrated = new PresetValue
                        {
                            Kind = pair.Value.Kind,
                            Number = Math.Abs(pair.Value.Number.Value),
                            MinNumber = pair.Value.MinNumber,
                            MaxNumber = pair.Value.MaxNumber,
                            Boolean = pair.Value.Boolean,
                            Text = pair.Value.Text
                        };
                        parameter.ApplyPresetValue(migrated);
                    }
                    else if (migratePositiveHungerPenaltyMagnitude
                        && IsPositiveHungerPenaltyMagnitudeParameter(pair.Key)
                        && pair.Value.Number is < 0.0)
                    {
                        // Before v0.2.47 the two standard hunger stamina-recovery controls exposed
                        // the engine's negative regeneration modifier. The UI now stores a positive
                        // penalty magnitude while the writer negates it back to the CFG convention.
                        var migrated = new PresetValue
                        {
                            Kind = pair.Value.Kind,
                            Number = Math.Abs(pair.Value.Number.Value),
                            MinNumber = pair.Value.MinNumber,
                            MaxNumber = pair.Value.MaxNumber,
                            Boolean = pair.Value.Boolean,
                            Text = pair.Value.Text
                        };
                        parameter.ApplyPresetValue(migrated);
                    }
                    else if (migratePositiveMasterPenaltyMagnitude
                        && IsPositiveMasterPenaltyMagnitudeParameter(pair.Key)
                        && pair.Value.Number is < 0.0)
                    {
                        // Before v0.2.50 these Master penalty/loss controls exposed the engine's
                        // negative CFG delta. The UI now stores a positive penalty magnitude while
                        // negateSet preserves the exact original runtime value.
                        var migrated = new PresetValue
                        {
                            Kind = pair.Value.Kind,
                            Number = Math.Abs(pair.Value.Number.Value),
                            MinNumber = pair.Value.MinNumber,
                            MaxNumber = pair.Value.MaxNumber,
                            Boolean = pair.Value.Boolean,
                            Text = pair.Value.Text
                        };
                        parameter.ApplyPresetValue(migrated);
                    }
                    else
                    {
                        parameter.ApplyPresetValue(pair.Value);
                    }
                }

                // Migrate the retired shared instant-stamina scalar (1x)
                // into exact Water (38%) and regular Energy Drink (50%) values.
                // Explicit new per-drink preset values always take precedence.
                // Limited Energy Drink is no longer implicitly scaled.
                if (preset.Values.TryGetValue("survival.instantStaminaRestore", out var oldRefill)
                    && oldRefill.Number is double refillScale && double.IsFinite(refillScale))
                {
                    foreach (var (id, vanilla) in new[]
                    {
                        ("survival.waterInstantStaminaRestore", 0.38),
                        ("survival.energyDrinkInstantStaminaRestore", 0.50)
                    })
                    {
                        if (!preset.Values.ContainsKey(id)
                            && parametersById.TryGetValue(id, out var source))
                            source.SnapCurrentNumberToMarker(Math.Clamp(
                                refillScale * vanilla, source.Minimum, source.Maximum));
                    }
                }

                // Backward-compatible split of Water's former shared +25% Stamina
                // and -10% SPDrain multiplier. Old presets keep both strengths
                // unless the new cost-cut value is explicitly present.
                if (!preset.Values.ContainsKey("survival.waterStaminaCostReduction")
                    && preset.Values.TryGetValue("survival.waterStaminaEffectStrength",
                        out var legacyWaterStrength)
                    && legacyWaterStrength.Number is double legacyWaterMultiplier
                    && double.IsFinite(legacyWaterMultiplier)
                    && parametersById.TryGetValue("survival.waterStaminaCostReduction",
                        out var waterCostCut))
                    waterCostCut.SnapCurrentNumberToMarker(Math.Clamp(
                        legacyWaterMultiplier, waterCostCut.Minimum, waterCostCut.Maximum));

                // Earlier releases persisted one 1x multiplier for BOTH
                // BeerAntirad1 (-10) and VodkaAntirad3 (-50). Convert that
                // historical setting into the two new independent positive
                // RAD quantities only when a new per-drink key is absent.
                // SnapCurrentNumberToMarker preserves exact historical amounts
                // such as 12.5 RAD even when modern mouse edits use 5-PTS steps.
                if (preset.Values.TryGetValue("survival.alcoholRadiationRemoval",
                        out var legacyAlcohol)
                    && legacyAlcohol.Number is double legacyScale)
                {
                    foreach (var (id, vanilla) in new[]
                    {
                        ("health.beerRadiationRemoval", 10.0),
                        ("health.vodkaRadiationRemoval", 50.0)
                    })
                    {
                        if (!preset.Values.ContainsKey(id)
                            && parametersById.TryGetValue(id, out var source))
                            source.SnapCurrentNumberToMarker(
                                Math.Clamp(legacyScale * vanilla,
                                    source.Minimum, source.Maximum));
                    }
                }

                // Old presets used the parent multiplier for EVERY Water/Energy
                // timed effect. Preserve the old resulting duration on import
                // when the new independent duration field is absent.
                void MigrateLegacyShortDuration(string parentId, string shortId, double vanillaSeconds)
                {
                    if (preset.Values.ContainsKey(shortId)
                        || !preset.Values.ContainsKey(parentId)
                        || !parametersById.TryGetValue(parentId, out var mainDuration)
                        || !parametersById.TryGetValue(shortId, out var shortDuration))
                        return;
                    shortDuration.SnapCurrentNumberToMarker(Math.Clamp(
                        vanillaSeconds * mainDuration.CurrentNumber,
                        shortDuration.Minimum, shortDuration.Maximum));
                }

                MigrateLegacyShortDuration("survival.waterEffectDuration",
                    "survival.waterRegenDuration", 5.0);
                MigrateLegacyShortDuration("survival.energyDrinkEffectDuration",
                    "survival.energyFullCostDuration", 5.0);
                MigrateLegacyShortDuration("survival.energyDrinkEffectDuration",
                    "survival.energyPartialCostDuration", 15.0);
            }
            finally
            {
                _suspendSliderConstraints = false;
                _isApplyingReference = false;
            }

            NormalizeEditorConstraints();
            RefreshState();
            _loadedPresetName = string.IsNullOrWhiteSpace(preset.Name)
                ? Path.GetFileNameWithoutExtension(path)
                : preset.Name;
            SelectPresetPath(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(
                $"Preset could not be loaded.\n\n{ex.Message}",
                AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task BuildAsync()
    {
        if (IsBuilding)
            return;

        ModBuildResult? result = null;
        string? modsFolder = null;
        Exception? buildError = null;

        try
        {
            // Validate before showing the busy rail: no build was started if
            // the mods path is not configured.
            modsFolder = _modDestinationSettings.RequireConfiguredPath();
            NormalizeEditorConstraints();

            BuildProgressStep = 0;
            BuildProgressLabel = "WRITING CFG";
            IsBuilding = true;

            // Progress<T> marshals the worker's completed-stage notifications
            // back to the WPF dispatcher without blocking the UI thread.
            var progress = new Progress<BuildProgressUpdate>(update =>
            {
                if (!IsBuilding || update.CompletedStages < BuildProgressStep)
                    return;

                BuildProgressStep = update.CompletedStages;
                BuildProgressLabel = update.Phase;
            });

            // Reading the VM is safe while all editors and presets are locked.
            // No expensive CFG writing, repak, retoc or file deployment work
            // may block the WPF dispatcher during a build.
            result = await Task.Run(() =>
                _modBuildService.Build(this, _catalog, _gameInstall, modsFolder!, progress));

            BuildProgressStep = 6;
            BuildProgressLabel = "COMPLETE";
        }
        catch (Exception ex)
        {
            // This command is dispatched without an await by ICommand.
            // Unexpected worker failures must be surfaced rather than lost
            // as unobserved Task exceptions, and the footer must still reset.
            buildError = ex;
        }
        finally
        {
            // Clear the rail before the result dialog appears, on success,
            // failure and the no-mod/uninstall path alike.
            IsBuilding = false;
        }

        if (buildError is not null)
        {
            MessageBox.Show(
                $"Mod could not be built.\n\n{buildError.Message}",
                AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        if (result is null || modsFolder is null)
            return;

        if (!result.HasMod)
        {
            MessageBox.Show(
                "No gameplay values differ from the selected vanilla baseline.\n\n" +
                "Previous True Custom Difficulty files were removed from:\n" + modsFolder,
                AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(
            $"Built and installed True Custom Difficulty successfully.\n\n" +
            $"{result.PatchFileCount} BPatch CFG file(s)\n" +
            $"{result.ParameterCount} modified control(s)\n" +
            $"{result.EditCount} concrete CFG edit(s)\n\n" +
            $"Installed to:\n{modsFolder}\n\n" +
            string.Join("\n", result.Files.Select(Path.GetFileName)),
            AppName,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static int ParameterSortOrder(ParameterViewModel parameter)
    {
        if (parameter.Definition.Tab == "environment")
        {
            // Explicit four-section Anomalies order: global, individual
            // damage, Flycatcher slowdown, then special effects.
            return parameter.Group switch
            {
                "Global Anomaly Damage" => 0,
                "Damage by Anomaly" => 100,
                "Flycatcher Slowdown" => 200,
                "Special Anomaly Effects" => 300,
                _ => 400
            };
        }

        if (parameter.Definition.Tab == "combat")
        {
            return parameter.Id switch
            {
                // Human Enemies
                "combat.npcHealth" => 0,
                "combat.npcWeaponDamage" => 10,

                // Your Weapons
                "combat.playerWeaponDamage" => 60,
                "combat.playerHeadshotMultiplier" => 70,
                "combat.weaponJamming" => 80,

                // Only visible Combat anchors need an explicit sort position.
                "combat.mutantHealth.bloodsucker" => 130,
                _ => 199
            };
        }

        if (parameter.Definition.Tab == "survival")
        {
            return parameter.Id switch
            {
                // Hunger
                "survival.hungerRate" => 0,
                // Normal per-food hunger relief: two compact rows immediately
                // after the Hunger Ladder, never default/alphabetic fallback.
                "survival.breadSatiety" => 10,
                "survival.sausageSatiety" => 20,
                "survival.cannedFoodSatiety" => 30,
                "survival.milkSatiety" => 35,
                "survival.wellFedStaminaRegen" => 40,
                "survival.hungerMediumStaminaRegen" => 50,
                "survival.hungerStrongStaminaRegen" => 60,
                "survival.satietyScalar" => 70,

                // Extra starvation bundle
                "advanced.starvationMechanics" => 100,
                "survival.starvationStaminaDrain" => 120,
                "survival.starvationDamage" => 130,
                "survival.starvationDamageInterval" => 140,
                "survival.starvationPenaltyDelay" => 150,
                "survival.starvationHealthRegenPenalty" => 160,

                // Standard fatigue
                "survival.sleepinessRate" => 200,
                "survival.energyDrinkFatigueRelief" => 210,
                "survival.fatigueLowAimPenalty" => 240,
                "survival.fatigueMediumAimPenalty" => 250,
                "survival.fatigueStrongAimPenalty" => 260,
                "survival.fatigueMediumReloadPenalty" => 270,
                "survival.fatigueStrongReloadPenalty" => 280,

                // Consumable Effects
                "survival.waterInstantStaminaRestore" => 290,
                "survival.energyDrinkInstantStaminaRestore" => 291,
                "survival.waterEffectDuration" => 292,
                "survival.waterRegenDuration" => 292,
                "survival.energyDrinkEffectDuration" => 293,
                "survival.energyFullCostDuration" => 293,
                "survival.energyPartialCostDuration" => 293,
                "survival.vodkaEffectDuration" => 294,
                "survival.beerEffectDuration" => 295,
                "survival.magicVodkaEffectDuration" => 296,

                // Alcohol Tolerance
                "survival.beerDrunknessStrength" => 297,
                "survival.vodkaDrunknessStrength" => 298,

                // Sleeping
                "survival.sleepEligibilityThreshold" => 300,
                "survival.minimumSleepHours" => 310,
                "survival.sleepHoursMultiplier" => 320,
                "survival.sleepHealthRestore" => 340,
                "survival.sleepStaminaRestore" => 350,

                // Extra Master fatigue bundle
                "advanced.fatigueMechanics" => 400,
                "survival.severeFatigueMaxStamina" => 420,
                "survival.severeFatigueCarryWeight" => 430,
                "survival.severeFatigueRecoil" => 440,
                "survival.severeFatigueAimSway" => 450,
                "survival.severeFatigueDispersion" => 460,

                // Radiation
                "survival.radiationAccumulation" => 500,
                "survival.radiationRecovery" => 510,
                "advanced.radiationMechanics" => 520,
                "survival.radiationStaminaDrainPenalty" => 540,

                // Bleeding
                "survival.bleedingSeverity" => 600,
                "survival.bleedingRecovery" => 610,
                "survival.bleedingLowDamage" => 650,
                "survival.bleedingMediumDamage" => 660,
                "survival.bleedingStrongDamage" => 670,

                _ => 799
            };
        }

        if (parameter.Definition.Tab == "equipment")
        {
            // Independent, compact groups: wear first, item weight second.
            return parameter.Id switch
            {
                "equipment.armorDurability" => 0,
                "equipment.weaponWearPerShot" => 10,
                "equipment.ammoWeight" => 100,
                "equipment.consumableWeight" => 110,
                _ => 199
            };
        }

        if (parameter.Definition.Tab == "health")
        {
            return parameter.Id switch
            {
                // Core Health
                "health.maxHP" => 0,
                "health.baseRegen" => 10,
                "health.regenDelay" => 20,
                "health.regenerationScalar" => 30,

                // Low Health regeneration belongs to the active Core Health editor.
                "health.lowHealthRegen" => 110,

                // Direct healing
                "health.bandageHealing" => 200,
                "health.medkitHealing" => 210,
                "health.armyMedkitHealing" => 220,
                "health.ecoMedkitHealing" => 230,
                "health.foodHealing" => 240,

                // Bleeding treatment
                "health.bandageBleeding" => 400,
                "health.medkitBleeding" => 410,
                "health.armyMedkitBleeding" => 420,
                "health.ecoMedkitBleeding" => 430,

                // Radiation treatment
                "health.ecoMedkitAntirad" => 500,
                "health.antiRadRadiation" => 510,
                "health.beerRadiationRemoval" => 520,
                "health.vodkaRadiationRemoval" => 530,

                _ => 699
            };
        }


        if (parameter.Definition.Tab == "stamina")
        {
            return parameter.Id switch
            {
                // Core stamina
                "stamina.maxSP" => 0,
                "stamina.regeneration" => 10,

                // Exhaustion and recovery
                "stamina.sprintDisableThreshold" => 200,
                "stamina.sprintRecoveryDelay" => 110,
                "stamina.lowRecoveryThreshold" => 120,
                "stamina.lowRecoveryStrength" => 130,
                "stamina.lowRecoveryDuration" => 140,

                // Action costs
                "stamina.sprintCost" => 100,
                "stamina.aimRegenCoef" => 205,
                "stamina.jumpCost" => 210,
                "stamina.vaultCost" => 220,
                "stamina.meleeNormalCost" => 230,
                "stamina.meleeStrongCost" => 240,
                "stamina.butstockCost" => 250,

                // Traversal
                "stamina.vaultMaxHeight" => 400,

                // Water
                "stamina.waterMediumDrain" => 500,
                "stamina.waterStrongDrain" => 510,

                // Encumbrance
                "stamina.maxCarryWeight" => 600,
                _ => 699
            };
        }

        if (parameter.Definition.Tab == "economy")
        {
            return parameter.Id switch
            {
                // Starting Coupons
                "economy.startingMoney" => 0,

                // Coupon Rewards
                "economy.mainQuestReward" => 10,
                "economy.sideQuestReward" => 20,

                // Trading
                "economy.buyCondition" => 100,
                "economy.sellCondition" => 110,
                "economy.traderMinBuyCondition" => 120,
                "economy.itemValueFloor" => 130,
                "economy.medicStock" => 150,

                // Item Price Scalers
                "economy.consumableCost" => 200,
                "economy.ammoCost" => 210,
                "economy.armorCost" => 220,
                "economy.weaponCost" => 230,
                "economy.artifactCost" => 240,

                // Services
                "economy.upgradeCost" => 300,
                "economy.repairDifficultyCost" => 310,
                "economy.repairCostBase" => 320,

                // Repair per Reputation
                "economy.repairEnemy" => 400,
                "economy.repairDisaffection" => 410,
                "economy.repairNeutral" => 420,
                "economy.repairFriend" => 430,

                _ => 599
            };
        }

        if (parameter.Definition.Tab == "master")
        {
            return parameter.Id switch
            {
                // Starvation bundle
                "advanced.starvationMechanics" => 300,
                "survival.starvationStaminaDrain" => 320,
                "survival.starvationDamage" => 330,
                "survival.starvationDamageInterval" => 340,
                "survival.starvationPenaltyDelay" => 350,
                "survival.starvationHealthRegenPenalty" => 360,

                // Severe fatigue bundle
                "advanced.fatigueMechanics" => 400,
                "survival.severeFatigueMaxStamina" => 420,
                "survival.severeFatigueCarryWeight" => 430,
                "survival.severeFatigueRecoil" => 440,
                "survival.severeFatigueAimSway" => 450,
                "survival.severeFatigueDispersion" => 460,

                // Radiation bundle
                "advanced.radiationMechanics" => 500,
                "survival.radiationStaminaDrainPenalty" => 520,

                // Low-health bundle
                "advanced.lowHealthMechanics" => 600,
                "health.masterLowHealthPenalty" => 620,

                // Master EffectModifier bundle
                "advanced.masterEffectModifierMechanics" => 700,
                "health.masterBandageModifier" => 710,
                "health.masterMedkitModifier" => 720,
                "health.masterArmyMedkitModifier" => 730,
                _ => 799
            };
        }

        if (parameter.Definition.Tab == "loot")
        {
            // World pickup, container, NPC and mutant controls stay in Loot.
            // Stash families are owned by the adjacent Stashes tab.
            return parameter.Id switch
            {
                // World depletion
                "loot.worldAmmoCratesRemoved" => 0,
                "loot.worldF1GrenadeBoxes" => 1,
                "loot.worldFoodDrinkCratesRemoved" => 2,
                "loot.worldMedicalCratesRemoved" => 3,
                "loot.looseWorldLootRemoved" => 4,

                // World placement
                "loot.worldWeaponShuffle" => 100,
                "loot.worldWeaponDensity" => 101,
                "loot.looseUniqueWeaponShuffle" => 102,
                "loot.worldAttachmentShuffle" => 102,
                "loot.worldDetectors" => 103,
                "loot.blueprintShuffle" => 104,

                // NPC loot and equipment
                "loot.npcCorpseBonus" => 200,
                "loot.npcMedicalChance" => 201,
                "loot.npcWeaponAmmoSupply" => 202,
                "loot.droppedWeaponCondition" => 203,

                // Two box yields and four ammo family weight sources
                "loot.ammoBoxWeight918" => 400,
                "loot.destructibleAmmoQuantity" => 401,
                "loot.destructibleGrenadeQuantity" => 402,
                "loot.ammoBoxWeight045" => 403,
                "loot.ammoBoxWeight545" => 404,
                "loot.ammoBoxWeight012" => 405,

                // The 14 species remain an independently edited sixth card
                _ when parameter.Id.StartsWith("loot.mutant.", StringComparison.Ordinal)
                    => 500,
                _ => 600
            };
        }

        if (parameter.Definition.Tab == "stashes")
        {
            // Preserve stable loot.* IDs: moving UI ownership does not rename
            // preset entries or change the production CFG writer.
            return parameter.Id switch
            {
                // Six compact stash composition controls and six optional gear tiers.
                "loot.stashMixAmmo" => 310,
                "loot.stashMixMedical" => 311,
                "loot.stashMixFood" => 312,
                "loot.stashMixDrinks" => 313,
                "loot.stashMixGrenades" => 314,
                "loot.stashMixCoupons" => 315,
                "loot.stashMedicalBandageBias" => 316,
                "loot.stashMedicalMedkitBias" => 317,
                "loot.stashMedicalArmyMedkitBias" => 318,
                "loot.stashMedicalAntiRadBias" => 319,
                // Medical selection and stack size form one readable flow.
                "loot.stashMedkitMinCount" => 320,
                "loot.stashMedkitMaxCount" => 321,
                "loot.stashArmyMedkitMinCount" => 322,
                "loot.stashArmyMedkitMaxCount" => 323,
                // Additional weapons/armor are a separate final section.
                "loot.stashGearEnabled" => 330,
                "loot.stashGearCoveragePercent" => 331,
                "loot.stashGearWeaponWeight" => 332,
                "loot.stashGearArmorWeight" => 333,
                "loot.stashGearArtifactWeight" => 334,
                "loot.stashGearAttachmentWeight" => 335,
                "loot.stashGearBonusChance" => 336,

                // Three native stash tier ammo quantities; RGD-5 Chance/Amount and
                // Rare Coupon Chance are members of StashContentMix.
                "loot.cheapStashAmmoQuantity" => 300,
                "loot.mediumStashAmmoQuantity" => 301,
                "loot.expensiveStashAmmoQuantity" => 302,
                "loot.cheapStashGrenadeChance" => 303,
                "loot.cheapStashGrenadeQuantity" => 304,
                "loot.expensiveStashMoneyChance" => 305,

                _ => 600
            };
        }

        return 100;
    }

    private void RefreshState()
    {
        foreach (var p in _anomalyDerivedParameters)
            p.RefreshComputedValues();

    }
}
