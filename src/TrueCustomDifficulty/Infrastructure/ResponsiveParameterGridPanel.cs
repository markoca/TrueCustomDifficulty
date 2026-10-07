using System.Windows;
using System.Windows.Controls;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Infrastructure;

public sealed class ResponsiveParameterGridPanel : Panel
{
    private const double Gap = 8.0;
    private const double TwoColumnThreshold = 700.0;

    // Explicit UX pairings. The catalog remains the gameplay/source-of-truth order,
    // while this map decides which related half-width controls should visually share a row.
    // A one-item row is intentional when the control has no sensible partner.
    private static readonly IReadOnlyDictionary<string, SemanticSlot> SemanticSlots =
        new Dictionary<string, SemanticSlot>(StringComparer.Ordinal)
        {
            // Combat
            ["combat.npcHealth"] = new("combat-human-1", 0),
            ["combat.npcWeaponDamage"] = new("combat-human-1", 1),
            ["combat.npcAttackCooldown"] = new("combat-human-2", 0),
            ["combat.npcHipAccuracy"] = new("combat-human-2", 1),
            ["combat.mutantDamage"] = new("combat-mutant-1", 0),
            ["combat.mutantAttackCooldown"] = new("combat-mutant-1", 1),
            ["combat.playerWeaponDamage"] = new("combat-player-1", 0),
            ["combat.playerHeadshotMultiplier"] = new("combat-player-1", 1),
            ["combat.accumulatedDamageWeightMax"] = new("combat-repeat-1", 0),
            ["combat.accumulatedDamageWeightMin"] = new("combat-repeat-1", 1),
            ["combat.psySpawnCycleDelay"] = new("combat-psy-1", 0),
            ["combat.psyPhantomCount"] = new("combat-psy-1", 1),
            ["combat.psyBleeding"] = new("combat-psy-2", 0),
            ["combat.psyDurabilityDamage"] = new("combat-psy-2", 1),

            // Survival
            ["survival.hungerRate"] = new("survival-hunger-1", 0),
            ["survival.wellFedStaminaRegen"] = new("survival-hunger-1", 1),
            ["survival.hungerMediumStaminaRegen"] = new("survival-hunger-2", 0),
            ["survival.hungerStrongStaminaRegen"] = new("survival-hunger-2", 1),

            ["survival.sleepinessRate"] = new("survival-fatigue-1", 0),
            ["survival.energyDrinkFatigueRelief"] = new("survival-fatigue-1", 1),
            ["survival.fatigueLowAimPenalty"] = new("survival-fatigue-low", 0),
            ["survival.fatigueMediumAimPenalty"] = new("survival-fatigue-medium", 0),
            ["survival.fatigueMediumReloadPenalty"] = new("survival-fatigue-medium", 1),
            ["survival.fatigueStrongAimPenalty"] = new("survival-fatigue-severe", 0),
            ["survival.fatigueStrongReloadPenalty"] = new("survival-fatigue-severe", 1),

            ["survival.sleepEligibilityThreshold"] = new("survival-sleep-1", 0),
            ["survival.minimumSleepHours"] = new("survival-sleep-1", 1),
            ["survival.sleepHealthRestore"] = new("survival-sleep-2", 0),
            ["survival.sleepStaminaRestore"] = new("survival-sleep-2", 1),

            ["survival.radiationAccumulation"] = new("survival-radiation-1", 0),
            ["survival.radiationRecovery"] = new("survival-radiation-1", 1),

            ["survival.bleedingSeverity"] = new("survival-bleeding-1", 0),
            ["survival.bleedingRecovery"] = new("survival-bleeding-1", 1),
            ["survival.bleedingLowDamage"] = new("survival-bleeding-2", 0),
            ["survival.bleedingMediumDamage"] = new("survival-bleeding-2", 1),
            ["survival.bleedingStrongDamage"] = new("survival-bleeding-3", 0),

            // Health
            ["health.maxHP"] = new("health-core-1", 0),
            ["health.baseRegen"] = new("health-core-1", 1),
            ["health.regenDelay"] = new("health-core-2", 0),
            ["health.regenerationScalar"] = new("health-core-2", 1),
            ["health.bandageBleeding"] = new("health-bleed-treatment-1", 0),
            ["health.medkitBleeding"] = new("health-bleed-treatment-1", 1),
            ["health.armyMedkitBleeding"] = new("health-bleed-treatment-2", 0),
            ["health.ecoMedkitBleeding"] = new("health-bleed-treatment-2", 1),
            ["health.ecoMedkitAntirad"] = new("health-rad-treatment-1", 0),
            ["health.antiRadRadiation"] = new("health-rad-treatment-1", 1),

            // Stamina
            ["stamina.maxSP"] = new("stamina-core-1", 0),
            ["stamina.regeneration"] = new("stamina-core-1", 1),
            ["stamina.sprintDisableThreshold"] = new("stamina-sprint-recovery-1", 0),
            ["stamina.sprintRecoveryDelay"] = new("stamina-sprint-recovery-1", 1),
            ["stamina.lowRecoveryThreshold"] = new("stamina-emergency-1", 0),
            ["stamina.lowRecoveryStrength"] = new("stamina-emergency-1", 1),
            ["stamina.lowRecoveryDuration"] = new("stamina-emergency-2", 0),
            ["stamina.sprintUnderRunSpeedRegenCoef"] = new("stamina-speed-state-1", 0),
            ["stamina.joggingRegenCoef"] = new("stamina-speed-state-1", 1),
            ["stamina.slowRunRegenCoef"] = new("stamina-speed-state-2", 0),
            ["stamina.runRegenCoef"] = new("stamina-speed-state-2", 1),
            ["stamina.waterMediumDrain"] = new("stamina-water-1", 0),
            ["stamina.waterStrongDrain"] = new("stamina-water-1", 1),

            // Loot
            ["loot.npcCorpseBonus"] = new("loot-npc-1", 0),
            ["loot.npcMedicalChance"] = new("loot-npc-1", 1),
            ["loot.npcWeaponAmmoSupply"] = new("loot-npc-2", 0),
            ["loot.droppedWeaponCondition"] = new("loot-npc-2", 1),
            ["loot.cheapStashGrenadeChance"] = new("loot-stash-grenade-1", 0),
            ["loot.cheapStashGrenadeQuantity"] = new("loot-stash-grenade-1", 1),
            ["loot.destructibleAmmoQuantity"] = new("loot-breakable-1", 0),
            ["loot.destructibleGrenadeQuantity"] = new("loot-breakable-1", 1),
            ["loot.worldWeaponShuffle"] = new("loot-world-weapon-1", 0),
            ["loot.worldWeaponDensity"] = new("loot-world-weapon-1", 1),
            ["loot.worldAttachmentShuffle"] = new("loot-world-other-1", 0),
            ["loot.worldDetectors"] = new("loot-world-other-1", 1),

            // Economy
            ["economy.buyCondition"] = new("economy-trading-1", 0),
            ["economy.sellCondition"] = new("economy-trading-1", 1),
            ["economy.traderMinBuyCondition"] = new("economy-trading-2", 0),
            ["economy.itemValueFloor"] = new("economy-trading-2", 1),
            ["economy.traderAmmoStock"] = new("economy-stock-1", 0),
            ["economy.medicStock"] = new("economy-stock-1", 1),
            ["economy.fastTravelCost"] = new("economy-services-1", 0),
            ["economy.upgradeCost"] = new("economy-services-1", 1),
            ["economy.repairDifficultyCost"] = new("economy-services-2", 0),
            ["economy.repairCostBase"] = new("economy-services-2", 1),
            ["economy.repairEnemy"] = new("economy-reputation-1", 0),
            ["economy.repairDisaffection"] = new("economy-reputation-1", 1),
            ["economy.repairNeutral"] = new("economy-reputation-2", 0),
            ["economy.repairFriend"] = new("economy-reputation-2", 1),
            ["economy.mainQuestReward"] = new("economy-rewards-1", 0),
            ["economy.sideQuestReward"] = new("economy-rewards-1", 1),

            // Equipment
            ["equipment.ammoWeight"] = new("equipment-weight-1", 0),
            ["equipment.consumableWeight"] = new("equipment-weight-1", 1),

            // Environment
            ["environment.flycatcherMovementSpeed"] = new("environment-flycatcher-1", 0),
            ["environment.flycatcherTurnSpeed"] = new("environment-flycatcher-1", 1),
            ["environment.razorBleeding"] = new("environment-bleeding-1", 0),
            ["environment.diamondBleeding"] = new("environment-bleeding-1", 1),

            // Master
            ["advanced.lowHealthMechanics"] = new("master-low-health-1", 0),
            ["health.masterLowHealthPenalty"] = new("master-low-health-1", 1),
            ["health.masterBandageModifier"] = new("master-medical-1", 0),
            ["health.masterMedkitModifier"] = new("master-medical-1", 1),
            ["health.masterArmyMedkitModifier"] = new("master-medical-2", 0),
            ["advanced.masterEffectModifierMechanics"] = new("master-medical-2", 1),

            ["advanced.starvationMechanics"] = new("master-starvation-1", 0),
            ["survival.starvationPenaltyDelay"] = new("master-starvation-1", 1),
            ["survival.starvationDamage"] = new("master-starvation-2", 0),
            ["survival.starvationDamageInterval"] = new("master-starvation-2", 1),
            ["survival.starvationStaminaDrain"] = new("master-starvation-3", 0),
            ["survival.starvationHealthRegenPenalty"] = new("master-starvation-3", 1),

            ["advanced.radiationMechanics"] = new("master-radiation-1", 0),
            ["survival.radiationStaminaDrainPenalty"] = new("master-radiation-1", 1),

            ["advanced.fatigueMechanics"] = new("master-fatigue-1", 0),
            ["survival.severeFatigueMaxStamina"] = new("master-fatigue-1", 1),
            ["survival.severeFatigueCarryWeight"] = new("master-fatigue-2", 0),
            ["survival.severeFatigueAimSway"] = new("master-fatigue-2", 1),
            ["survival.severeFatigueRecoil"] = new("master-fatigue-3", 0),
            ["survival.severeFatigueDispersion"] = new("master-fatigue-3", 1)
        };

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = ResolveWidth(availableSize.Width);
        var columns = GetColumnCount(width);
        var cardWidth = GetCardWidth(width, columns);
        var rows = BuildRows(columns);

        double totalHeight = 0.0;

        foreach (var row in rows)
        {
            var rowHeight = 0.0;

            foreach (var item in row.Items)
            {
                var measureWidth = row.FullWidth ? width : cardWidth;
                item.Element.Measure(new Size(measureWidth, double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, item.Element.DesiredSize.Height);
            }

            row.Height = rowHeight;
            totalHeight += rowHeight + Gap;
        }

        if (totalHeight > 0.0)
            totalHeight -= Gap;

        return new Size(width, Math.Max(0.0, totalHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0.0, finalSize.Width);
        var columns = GetColumnCount(width);
        var cardWidth = GetCardWidth(width, columns);
        var rows = BuildRows(columns);

        double y = 0.0;

        foreach (var row in rows)
        {
            var rowHeight = row.Items.Count == 0
                ? 0.0
                : row.Items.Max(item => item.Element.DesiredSize.Height);

            foreach (var item in row.Items)
            {
                if (row.FullWidth)
                {
                    item.Element.Arrange(new Rect(0.0, y, width, rowHeight));
                    continue;
                }

                var column = columns == 1 ? 0 : Math.Clamp(item.Column, 0, 1);
                item.Element.Arrange(new Rect(
                    column * (cardWidth + Gap),
                    y,
                    cardWidth,
                    rowHeight));
            }

            y += rowHeight + Gap;
        }

        foreach (UIElement child in InternalChildren)
        {
            if (!IsLayoutVisible(child))
                child.Arrange(new Rect(0.0, 0.0, 0.0, 0.0));
        }

        if (y > 0.0)
            y -= Gap;

        return new Size(width, Math.Max(finalSize.Height, y));
    }

    private List<LayoutRow> BuildRows(int columns)
    {
        var stagedRows = new List<(int FirstIndex, LayoutRow Row)>();
        var semanticRows = new Dictionary<string, (int FirstIndex, LayoutRow Row)>(StringComparer.Ordinal);
        LayoutRow? autoRow = null;
        var autoFirstIndex = -1;

        void FlushAuto()
        {
            if (autoRow is null || autoRow.Items.Count == 0)
                return;

            stagedRows.Add((autoFirstIndex, autoRow));
            autoRow = null;
            autoFirstIndex = -1;
        }

        var visibleIndex = 0;

        foreach (UIElement child in InternalChildren)
        {
            if (!IsLayoutVisible(child))
                continue;

            if (IsFullWidth(child))
            {
                FlushAuto();
                stagedRows.Add((visibleIndex, new LayoutRow(fullWidth: true)
                {
                    Items = { new LayoutItem(child, 0) }
                }));
                visibleIndex++;
                continue;
            }

            var parameter = (child as FrameworkElement)?.DataContext as ParameterViewModel;

            if (columns > 1
                && parameter is not null
                && SemanticSlots.TryGetValue(parameter.Id, out var slot))
            {
                FlushAuto();

                if (!semanticRows.TryGetValue(slot.RowKey, out var semantic))
                {
                    semantic = (visibleIndex, new LayoutRow(fullWidth: false));
                    semanticRows[slot.RowKey] = semantic;
                    stagedRows.Add(semantic);
                }

                // Semantic mappings are authored explicitly. If a duplicate column slips in,
                // keep the later control visible in a normal row rather than overlapping it.
                if (semantic.Row.Items.Any(item => item.Column == slot.Column))
                {
                    autoRow ??= new LayoutRow(fullWidth: false);
                    if (autoFirstIndex < 0)
                        autoFirstIndex = visibleIndex;

                    autoRow.Items.Add(new LayoutItem(child, autoRow.Items.Count));
                    if (autoRow.Items.Count >= columns)
                        FlushAuto();
                }
                else
                {
                    semantic.Row.Items.Add(new LayoutItem(child, slot.Column));
                }

                visibleIndex++;
                continue;
            }

            autoRow ??= new LayoutRow(fullWidth: false);
            if (autoFirstIndex < 0)
                autoFirstIndex = visibleIndex;

            autoRow.Items.Add(new LayoutItem(child, autoRow.Items.Count));

            if (autoRow.Items.Count >= columns)
                FlushAuto();

            visibleIndex++;
        }

        FlushAuto();

        return stagedRows
            .OrderBy(entry => entry.FirstIndex)
            .Select(entry => entry.Row)
            .ToList();
    }

    private static double ResolveWidth(double width)
        => double.IsInfinity(width) || double.IsNaN(width)
            ? 900.0
            : Math.Max(280.0, width);

    private static int GetColumnCount(double width)
        => width >= TwoColumnThreshold ? 2 : 1;

    private static double GetCardWidth(double width, int columns)
        => Math.Max(260.0, (width - Gap * (columns - 1)) / columns);

    private static bool IsLayoutVisible(UIElement child)
    {
        if (child is FrameworkElement { DataContext: ParameterViewModel parameter })
            return parameter.IsControlVisible;

        return child.Visibility != Visibility.Collapsed;
    }

    private static bool IsFullWidth(UIElement child)
    {
        if (child is not FrameworkElement { DataContext: ParameterViewModel parameter })
            return false;

        return parameter.IsAggregateRadarAnchor;
    }

    private sealed record SemanticSlot(string RowKey, int Column);
    private sealed record LayoutItem(UIElement Element, int Column);

    private sealed class LayoutRow(bool fullWidth)
    {
        public bool FullWidth { get; } = fullWidth;
        public List<LayoutItem> Items { get; } = new();
        public double Height { get; set; }
    }
}
