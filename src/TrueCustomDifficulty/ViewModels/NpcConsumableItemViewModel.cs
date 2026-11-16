using TrueCustomDifficulty.Infrastructure;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.ViewModels;

/// <summary>
/// Optional per-SID NPC consumable generator Chance. A checked row multiplies
/// every native source Chance of that SID (0–3x, 0.05x steps, clamped to 1),
/// so the vanilla difficulty / player-rank progression is preserved.
/// Unchecked leaves source Chance untouched (medical still follows MED DROPS).
/// Old absolute-percent presets are kept exactly until the user edits them.
/// </summary>
public sealed class NpcConsumableItemViewModel : ObservableObject
{
    public sealed record SourceItem(string Sid, string Label, string Group);

    public const double MinimumMultiplier = 0.0;
    public const double MaximumMultiplier = 3.0;
    public const double MultiplierStep = 0.05;

    // Unchecked rows show a muted dash: no override, source Chance untouched.
    public const string UntouchedText = "—";

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
    private double _multiplier = 1.0;
    private int? _legacyChancePercent;

    public NpcConsumableItemViewModel(SourceItem source)
    {
        Sid = source.Sid;
        Label = source.Label;
        Group = source.Group;
    }

    public string Sid { get; }
    public string Label { get; }
    public string Group { get; }
    public bool IsMedical => Group == "MEDICAL";

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (SetProperty(ref _enabled, value))
                NotifyDisplay();
        }
    }

    /// <summary>Source-relative Chance multiplier, 0–3x in 0.05x steps.</summary>
    public double Multiplier
    {
        get => _multiplier;
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value),
                    "NPC consumable Chance multiplier must be finite.");
            var snapped = Snap(value);
            if (Math.Abs(_multiplier - snapped) < 0.0000001 && _legacyChancePercent is null)
                return;
            _multiplier = snapped;
            _legacyChancePercent = null;
            OnPropertyChanged(nameof(Multiplier));
            OnPropertyChanged(nameof(LegacyChancePercent));
            NotifyDisplay();
        }
    }

    /// <summary>
    /// Read-only compatibility mode for presets saved by the short-lived
    /// absolute-percent editor: literal Chance on every matching row. Never
    /// authored by the current slider; cleared on the first edit.
    /// </summary>
    public int? LegacyChancePercent => _legacyChancePercent;

    public string PercentText => !Enabled ? UntouchedText
        : _legacyChancePercent is int legacy ? legacy + "%"
        : Multiplier.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "×";

    public bool IsEffectiveChange => Enabled
        && (_legacyChancePercent is not null || Math.Abs(Multiplier - 1.0) > 0.000001);

    /// <summary>
    /// Shared numeric Chance transform used by the UI rank preview and the
    /// CFG writer: <paramref name="inheritedMultiplier"/> is MED DROPS for
    /// medical rows and 1 otherwise; the result is clamped to 0..1. The
    /// FEWER SUPPLIES master is applied afterwards by the caller.
    /// </summary>
    public double ScaleSourceChance(double sourceChance, double inheritedMultiplier)
    {
        if (Enabled && _legacyChancePercent is int legacy)
            return legacy / 100.0;
        var factor = Enabled ? Multiplier : inheritedMultiplier;
        return Math.Clamp(sourceChance * factor, 0.0, 1.0);
    }

    public void Reset()
    {
        Enabled = false;
        Multiplier = 1.0;
    }

    public NpcConsumableChancePreset ToPreset() => new()
    {
        Enabled = Enabled,
        Multiplier = _legacyChancePercent is null ? Multiplier : null,
        ChancePercent = _legacyChancePercent
    };

    public void ApplyPreset(NpcConsumableChancePreset? stored)
    {
        if (stored is null)
        {
            Reset();
            return;
        }
        if (stored.Multiplier is double factor)
        {
            if (!double.IsFinite(factor) || factor < MinimumMultiplier
                || factor > MaximumMultiplier)
                throw new ArgumentOutOfRangeException(nameof(stored));
            Multiplier = factor;
        }
        else if (stored.ChancePercent is double percent)
        {
            if (!double.IsFinite(percent) || percent < 0.0 || percent > 100.0)
                throw new ArgumentOutOfRangeException(nameof(stored));
            _multiplier = 1.0;
            _legacyChancePercent = checked((int)Math.Round(percent, MidpointRounding.AwayFromZero));
            OnPropertyChanged(nameof(Multiplier));
            OnPropertyChanged(nameof(LegacyChancePercent));
            NotifyDisplay();
        }
        else
        {
            Reset();
            return;
        }
        Enabled = stored.Enabled;
    }

    private static double Snap(double value)
        => Math.Round(Math.Clamp(value, MinimumMultiplier, MaximumMultiplier) / MultiplierStep,
            MidpointRounding.AwayFromZero) * MultiplierStep;

    private void NotifyDisplay()
    {
        OnPropertyChanged(nameof(PercentText));
        OnPropertyChanged(nameof(IsEffectiveChange));
    }
}
