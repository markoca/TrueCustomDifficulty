using System;
using System.Collections.Generic;
using System.Linq;
using TrueCustomDifficulty.Infrastructure;

namespace TrueCustomDifficulty.ViewModels;

/// <summary>
/// STASH LOOT MIX presence sidecars (not catalog parameters). Some stash
/// source categories are a single-category Weight pick (Basic ammo, Basic
/// water/energy drinks, Standard ammo), so the game always yields one item
/// and no native Chance exists to tune. A value below 100% adds a native
/// <c>Empty</c> entry to that Weight list so the pick sometimes yields
/// nothing: 100% = vanilla (no entry), 0% = never. 5% steps.
/// Rare ammo uses the existing catalog Rare Ammo Chance for the same job.
/// </summary>
public sealed class StashPresenceViewModel : ObservableObject
{
    public const string CheapAmmoId = "stashPresence.cheapAmmo";
    public const string CheapDrinksId = "stashPresence.cheapDrinks";
    public const string MediumAmmoId = "stashPresence.mediumAmmo";
    public const double Step = 0.05;

    public static IReadOnlyList<string> Ids { get; } =
        new[] { CheapAmmoId, CheapDrinksId, MediumAmmoId };

    private readonly Dictionary<string, double> _values =
        Ids.ToDictionary(id => id, _ => 1.0, StringComparer.Ordinal);

    public event EventHandler? Changed;

    public IReadOnlyDictionary<string, double> Values => _values;

    public static double Snap(double value)
        => Math.Round(Math.Clamp(double.IsFinite(value) ? value : 1.0, 0.0, 1.0) / Step,
            MidpointRounding.AwayFromZero) * Step;

    public double Get(string id) => _values.TryGetValue(id, out var value) ? value : 1.0;

    public void Set(string id, double value)
    {
        if (!_values.ContainsKey(id))
            throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown stash presence sidecar.");
        var snapped = Snap(value);
        if (Math.Abs(_values[id] - snapped) < 0.0000001)
            return;
        _values[id] = snapped;
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(IsModified));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsModified => _values.Values.Any(v => Math.Abs(v - 1.0) > 0.0000001);

    public void Reset()
    {
        foreach (var id in Ids)
            Set(id, 1.0);
    }

    /// <summary>Only changed entries are saved; older presets omit the block.</summary>
    public Dictionary<string, double>? ToPreset()
    {
        var changed = _values.Where(pair => Math.Abs(pair.Value - 1.0) > 0.0000001)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        return changed.Count == 0 ? null : changed;
    }

    public void FromPreset(IReadOnlyDictionary<string, double>? preset)
    {
        foreach (var id in Ids)
            Set(id, preset is not null && preset.TryGetValue(id, out var value) ? value : 1.0);
    }
}
