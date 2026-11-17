using System;
using System.Collections.Generic;
using System.Linq;
using TrueCustomDifficulty.Infrastructure;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.ViewModels;

/// <summary>
/// TRADER STOCK partial odds sidecar (not catalog parameters): per trader
/// role × category, "keep X% of the vanilla odds" for that category's offers
/// (0.05 steps). 100% = vanilla. 0% is NOT stored here: it is the existing
/// catalog SELL toggle unchecked (category removed). Keys are the SELL
/// parameter ids, e.g. "economy.traderSells.general.weapons".
/// </summary>
public sealed class TraderSellOddsViewModel : ObservableObject
{
    public const double Step = 0.05;
    private readonly Dictionary<string, double> _values = new(StringComparer.Ordinal);

    public event EventHandler? Changed;

    public static IReadOnlyCollection<string> Ids { get; } =
        TraderSellsPolicy.ParameterIds.ToArray();

    public IReadOnlyDictionary<string, double> Values => _values;

    public static double Snap(double value)
        => Math.Round(Math.Clamp(double.IsFinite(value) ? value : 1.0, 0.0, 1.0) / Step,
            MidpointRounding.AwayFromZero) * Step;

    public double Get(string id) => _values.TryGetValue(id, out var value) ? value : 1.0;

    /// <summary>Stores 5–95%; 100% (and 0%, owned by the toggle) clear the entry.</summary>
    public void Set(string id, double value)
    {
        if (!Ids.Contains(id))
            throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown trader stock category.");
        var snapped = Snap(value);
        var keep = snapped > 0.0000001 && snapped < 1.0 - 0.0000001;
        if (keep && _values.TryGetValue(id, out var old) && Math.Abs(old - snapped) < 0.0000001)
            return;
        if (!keep && !_values.ContainsKey(id))
            return;
        if (keep) _values[id] = snapped; else _values.Remove(id);
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(IsModified));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsModified => _values.Count > 0;

    public void Reset()
    {
        if (_values.Count == 0) return;
        _values.Clear();
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(IsModified));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Dictionary<string, double>? ToPreset()
        => _values.Count == 0 ? null : new Dictionary<string, double>(_values, StringComparer.Ordinal);

    public void FromPreset(IReadOnlyDictionary<string, double>? preset)
    {
        _values.Clear();
        if (preset is not null)
            foreach (var (id, value) in preset)
                if (Ids.Contains(id))
                {
                    var snapped = Snap(value);
                    if (snapped > 0.0000001 && snapped < 1.0 - 0.0000001)
                        _values[id] = snapped;
                }
        OnPropertyChanged(nameof(Values));
        OnPropertyChanged(nameof(IsModified));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
