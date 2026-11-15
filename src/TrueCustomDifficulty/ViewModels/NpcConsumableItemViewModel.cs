using TrueCustomDifficulty.Infrastructure;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.ViewModels;

/// <summary>
/// Optional, absolute per-SID NPC consumable generator Chance. A checked
/// percentage is the literal 0..1 CFG Chance across all matching source rows.
/// Unchecked leaves source Chance untouched. Old multiplier presets are
/// preserved exactly until the user edits that legacy control.
/// </summary>
public sealed class NpcConsumableItemViewModel : ObservableObject
{
    public sealed record SourceItem(string Sid, string Label, string Group);

    // Closed allowlist: ten SIDs in the six source-backed 2.1
    // GeneralNPC_Consumables_* clones (26 food, 20 drinks, 58 medical rows).
    public static IReadOnlyList<SourceItem> SourceItems { get; } = new[]
    {
        new SourceItem("Bread", "Bread", "FOOD"),
        new SourceItem("Sausage", "Sausage", "FOOD"),
        new SourceItem("CannedFood", "Canned Food", "FOOD"),
        new SourceItem("Energetic", "Energy Drink", "DRINKS"),
        new SourceItem("Beer", "Beer", "DRINKS"),
        new SourceItem("Vodka", "Vodka", "DRINKS"),
        new SourceItem("Bandage", "Bandage", "MEDICAL"),
        new SourceItem("Medkit", "Medkit", "MEDICAL"),
        new SourceItem("ArmyMedkit", "Army Medkit", "MEDICAL"),
        new SourceItem("AntiRad", "Anti-Rad", "MEDICAL")
    };

    private bool _enabled;
    private int _chancePercent = 100;
    private double? _legacyMultiplier;

    public NpcConsumableItemViewModel(SourceItem source)
    {
        Sid = source.Sid;
        Label = source.Label;
        Group = source.Group;
    }

    public string Sid { get; }
    public string Label { get; }
    public string Group { get; }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetProperty(ref _enabled, value))
            {
                OnPropertyChanged(nameof(PercentText));
                OnPropertyChanged(nameof(IsEffectiveChange));
            }
        }
    }

    // Absolute source CFG Chance, 0% -> Chance=0; 100% -> Chance=1.
    // A source SID has different vanilla Chances in different NPC templates:
    // there is no truthful single vanilla percentage for its unchecked row.
    public int ChancePercent
    {
        get => _chancePercent;
        set
        {
            if (value is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "NPC consumable generator Chance must be from 0% to 100%.");
            if (_chancePercent == value && _legacyMultiplier is null)
                return;
            _chancePercent = value;
            _legacyMultiplier = null;
            OnPropertyChanged(nameof(ChancePercent));
            OnPropertyChanged(nameof(LegacyMultiplier));
            OnPropertyChanged(nameof(PercentText));
            OnPropertyChanged(nameof(IsEffectiveChange));
        }
    }

    // Read-only compatibility mode, never authored by a new slider. Legacy
    // 0..2x presets still produce their former source-relative probabilities.
    public double? LegacyMultiplier => _legacyMultiplier;

    public string PercentText => !Enabled ? "CFG"
        : _legacyMultiplier is double legacy
            ? legacy.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "×"
        : $"{ChancePercent}%";

    public bool IsEffectiveChange => Enabled
        && (_legacyMultiplier is null || Math.Abs(_legacyMultiplier.Value - 1.0) > 0.000001);

    public void Reset()
    {
        Enabled = false;
        ChancePercent = 100;
    }

    public NpcConsumableChancePreset ToPreset() => new()
    {
        Enabled = Enabled,
        ChancePercent = _legacyMultiplier is null ? ChancePercent : null,
        Multiplier = _legacyMultiplier
    };

    public void ApplyPreset(NpcConsumableChancePreset? stored)
    {
        if (stored is null)
        {
            Reset();
            return;
        }
        if (stored.ChancePercent is double chance)
        {
            ChancePercent = checked((int)Math.Round(chance, MidpointRounding.AwayFromZero));
        }
        else if (stored.Multiplier is double factor)
        {
            if (!double.IsFinite(factor) || factor < 0.0 || factor > 2.0)
                throw new ArgumentOutOfRangeException(nameof(stored));
            _legacyMultiplier = factor;
            OnPropertyChanged(nameof(LegacyMultiplier));
            OnPropertyChanged(nameof(PercentText));
            OnPropertyChanged(nameof(IsEffectiveChange));
        }
        else
        {
            Reset();
            return;
        }
        Enabled = stored.Enabled;
    }
}
