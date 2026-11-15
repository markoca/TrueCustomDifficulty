using TrueCustomDifficulty.Infrastructure;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.ViewModels;

/// <summary>
/// Optional source-SID-specific NPC consumable Chance override. Native game
/// source values remain authoritative; 100% leaves their exact raw Chance intact.
/// No extra catalog parameters or difficulty profiles are manufactured.
/// </summary>
public sealed class NpcConsumableItemViewModel : ObservableObject
{
    public sealed record SourceItem(string Sid, string Label, string Group);

    // Closed allowlist: all ten SIDs present in the six source-backed 2.1
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
                OnPropertyChanged(nameof(IsEffectiveChange));
        }
    }

    public double Multiplier
    {
        get => _multiplier;
        set
        {
            if (!double.IsFinite(value) || value < 0.0 || value > 2.0)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "NPC consumable Chance multipliers must be finite from 0x to 2x.");
            var snapped = Math.Round(value * 20.0, MidpointRounding.AwayFromZero) / 20.0;
            if (SetProperty(ref _multiplier, snapped))
            {
                OnPropertyChanged(nameof(PercentText));
                OnPropertyChanged(nameof(IsEffectiveChange));
            }
        }
    }

    public string PercentText => $"{Multiplier * 100.0:0}%";
    public bool IsEffectiveChange => Enabled && Math.Abs(Multiplier - 1.0) > 0.000001;

    public void Reset()
    {
        Enabled = false;
        Multiplier = 1.0;
    }

    public NpcConsumableChancePreset ToPreset()
        => new() { Enabled = Enabled, Multiplier = Multiplier };

    public void ApplyPreset(NpcConsumableChancePreset? stored)
    {
        if (stored is null)
        {
            Reset();
            return;
        }
        Multiplier = stored.Multiplier;
        Enabled = stored.Enabled;
    }
}
