using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Immutable numeric control snapshot and common authored CFG field transform.
/// BOTH production stash writers and read-only Stashes previews use this path.
/// Native group eligibility/actor redirects are separate (phase-2 graph).
/// </summary>
public sealed class StashEffectiveState
{
    private readonly IReadOnlyDictionary<string, double> _values;
    private readonly HashSet<string> _modified;
    public string DifficultySid { get; }

    public StashEffectiveState(
        IEnumerable<ParameterViewModel> parameters,
        IEnumerable<string> modifiedIds,
        string difficultySid)
        : this(parameters.GroupBy(p => p.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().CurrentNumber,
                StringComparer.Ordinal),
            modifiedIds, difficultySid)
    {
    }

    public StashEffectiveState(
        IReadOnlyDictionary<string, double> values,
        IEnumerable<string> modifiedIds,
        string difficultySid)
    {
        _values = new Dictionary<string, double>(values, StringComparer.Ordinal);
        _modified = new HashSet<string>(modifiedIds, StringComparer.Ordinal);
        DifficultySid = difficultySid;
    }

    public double Value(string id, double fallback = 1.0)
        => _values.TryGetValue(id, out var value) ? value : fallback;

    public bool Changed(string id) => _modified.Contains(id);

    public static double Number(string raw)
    {
        var s = raw.Trim();
        var percentage = s.EndsWith("%", StringComparison.Ordinal);
        if (percentage) s = s[..^1].Trim();
        if (s.EndsWith("f", StringComparison.OrdinalIgnoreCase)) s = s[..^1].Trim();
        if (s.EndsWith(".", StringComparison.Ordinal)) s += "0";
        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture,
                out var result))
            throw new InvalidDataException($"Invalid stash source number '{raw}'.");
        return percentage ? result / 100.0 : result;
    }

    public static string FormatLikeSource(double number, string raw)
    {
        var source = raw.Trim();
        var percentage = source.EndsWith("%", StringComparison.Ordinal);
        var suffix = source.EndsWith("f", StringComparison.OrdinalIgnoreCase);
        var dotFloat = suffix && source.Length >= 2 && source[^2] == '.';
        var positive = source.StartsWith("+", StringComparison.Ordinal);
        if (Math.Abs(number) < 0.0000000001) number = 0;
        var represented = percentage ? number * 100.0 : number;
        var text = represented.ToString("0.######", CultureInfo.InvariantCulture);
        if (suffix)
        {
            if (dotFloat && Math.Abs(number - Math.Round(number)) < 0.0000001)
                text = Math.Round(number).ToString(CultureInfo.InvariantCulture) + ".";
            return text + "f";
        }
        if (positive && represented > 0) text = "+" + text;
        return percentage ? text + "%" : text;
    }

    private static string Decimal(double number)
        => (Math.Abs(number) < 0.0000000001 ? 0 : number)
            .ToString("0.######", CultureInfo.InvariantCulture);

    private static int PositiveCount(int count, double multiplier)
        => Math.Max(1, (int)Math.Floor(count * multiplier + 0.5));

    public static string? ItemBiasId(string sid) => sid switch
    {
        "Bandage" or "Medkit" or "ArmyMedkit" or "EcoMedkit" or "AntiRad" or "Antirad"
            => "loot.stashMixMedical",
        "Bread" or "Sausage" or "CannedFood" => "loot.stashMixFood",
        "Vodka" or "Water" or "Milk" or "Energetic" or "Beer" => "loot.stashMixDrinks",
        "GrenadeRGD5" or "GrenadeF1" => "loot.stashMixGrenades",
        "MoneyCommon" or "MoneyRare" or "MoneyEpic" or "MoneyLegendary"
            => "loot.stashMixCoupons",
        _ when sid.Length > 1 && sid[0] == 'A'
            && char.IsDigit(sid[1]) => "loot.stashMixAmmo",
        _ => null
    };

    private static string? MedicalDetailId(string sid) => sid switch
    {
        "Bandage" => "loot.stashMedicalBandageBias",
        "Medkit" => "loot.stashMedicalMedkitBias",
        "ArmyMedkit" => "loot.stashMedicalArmyMedkitBias",
        "AntiRad" or "Antirad" => "loot.stashMedicalAntiRadBias",
        _ => null
    };

    public bool MedicalDisabled(string sid)
    {
        if (sid is not ("Bandage" or "Medkit" or "ArmyMedkit"
            or "AntiRad" or "EcoMedkit")) return false;
        if (sid is "Medkit" or "ArmyMedkit"
            && Value($"loot.stash{sid}MaxCount", 0) < -.5) return true;
        // Global stash knobs tune Weight ONLY. Chance-based medical entries
        // are disabled only by an actual medical source-specific control.
        return MedicalDetailId(sid) is { } detail && Value(detail) <= 0;
    }

    // Weight-only global factor. This is NOT a multiplier of authored Chance.
    public double TierFactor(string sid)
    {
        if (MedicalDisabled(sid)) return 0.0;
        var id = ItemBiasId(sid);
        if (id is null) return 1.0;
        var factor = Value(id);
        if (MedicalDetailId(sid) is { } detail) factor *= Value(detail);
        return factor;
    }

    // Existing Medical Composition is already a source-exposed per-item
    // selector. Keep its original Chance behavior, without applying the six
    // global Weight knobs to independently-authored native Chance entries.
    public double ChanceFactor(string sid)
    {
        if (MedicalDisabled(sid)) return 0.0;
        return MedicalDetailId(sid) is { } detail ? Value(detail) : 1.0;
    }

    public double SelectionFactor(string sid, string field)
        => field switch
        {
            "Weight" => TierFactor(sid),
            "Chance" => ChanceFactor(sid),
            _ => throw new InvalidDataException("Unknown stash selection field: " + field)
        };

    // Only standard/rare regular ItemGenerator Chance entries; no SmartLoot,
    // non-matching rank gate, shared clue stash or quest/direct rewards.
    // Each source row keeps its own authored native probability: one compact
    // per-category slider scales those probabilities, never averaging them.
    public static string? TierChanceParameterId(string tier, string sid)
    {
        var suffix = ItemBiasId(sid) switch
        {
            "loot.stashMixAmmo" => "Ammo",
            "loot.stashMixMedical" => "Medical",
            "loot.stashMixFood" => "Food",
            "loot.stashMixDrinks" => "Drinks",
            "loot.stashMixGrenades" => "Grenades",
            "loot.stashMixCoupons" => "Coupons",
            _ => null
        };
        if (suffix is null) return null;
        return tier switch
        {
            "Medium" when suffix is "Medical" or "Drinks" or "Coupons"
                => "loot.mediumStash" + suffix + "Chance",
            "Expensive" when suffix is "Ammo" or "Medical" or "Food"
                or "Drinks" or "Grenades"
                => "loot.expensiveStash" + suffix + "Chance",
            _ => null
        };
    }

    public string TuneSelection(string sid, string field, string raw)
    {
        if (field is not ("Chance" or "Weight")) return raw;
        if (ItemBiasId(sid) is null) return raw;
        var bias = SelectionFactor(sid, field);
        if (Math.Abs(bias - 1) < 0.0000001) return raw;
        var original = Number(raw);
        var result = field == "Chance"
            ? Math.Clamp(original * bias, 0, 1)
            : Math.Max(0, original * bias);
        return FormatLikeSource(result, raw);
    }

    public string TuneCount(IReadOnlyDictionary<string, string> item, string field, string raw)
    {
        if (field is not ("MinCount" or "MaxCount")
            || !item.TryGetValue("ItemPrototypeSID", out var sid))
            return raw;
        if (MedicalDisabled(sid)) return "0";
        if (sid is not ("Medkit" or "ArmyMedkit")) return raw;
        var floor = (int)Math.Round(Value($"loot.stash{sid}MinCount", 0));
        var cap = (int)Math.Round(Value($"loot.stash{sid}MaxCount", 0));
        if (floor == 0 && cap == 0) return raw;
        if (!item.TryGetValue("MinCount", out var oldMin)
            || !item.TryGetValue("MaxCount", out var oldMax)
            || !int.TryParse(oldMin, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var sourceMin)
            || !int.TryParse(oldMax, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out var sourceMax))
            throw new InvalidDataException($"Invalid source stash counts for {sid}.");
        if (sourceMin < 0 || sourceMax < sourceMin)
            throw new InvalidDataException($"Invalid source stash range for {sid}.");
        if (cap == -1) return "0";
        var min = floor > 0 ? Math.Max(sourceMin, floor) : sourceMin;
        var max = cap > 0 ? Math.Min(sourceMax, cap) : sourceMax;
        if (min > max)
        {
            if (cap > 0) min = max;
            else max = min;
        }
        return (field == "MinCount" ? min : max).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The complete 3-tier writer field pipeline: amount/source bonus FIRST,
    /// then item Chance/Weight bias, then medical count rules. Preview calls
    /// this exact method for its native examples, not a parallel formula.
    /// </summary>
    public string TierField(
        CfgWriterBaseline baseline, string tier,
        CfgWriterCheapStashCategory category, CfgWriterCheapStashItem item,
        string field)
    {
        var sid = item.Fields["ItemPrototypeSID"];
        var raw = item.Fields[field];
        string updated = raw;
        if (tier == "Cheap")
        {
            var config = baseline.Special.CheapStashProduction;
            var ammo = config.AmmoEntries.FirstOrDefault(x =>
                category.Node == "[3]" && x.EntryNode == item.Node);
            if (ammo is not null && (field is "MinCount" or "MaxCount"))
                updated = PositiveCount(field == "MinCount"
                    ? ammo.SourceMinCount : ammo.SourceMaxCount,
                    Changed(config.AmmoQuantityParameterId)
                        ? Value(config.AmmoQuantityParameterId) : 1.0)
                    .ToString(CultureInfo.InvariantCulture);
            else if (category.Node == config.GrenadeEntry.CategoryNode
                && item.Node == config.GrenadeEntry.EntryNode)
            {
                if (field == "Chance")
                    updated = FormatLikeSource(
                        Changed(config.GrenadeChanceParameterId)
                            ? Value(config.GrenadeChanceParameterId)
                            : Number(config.GrenadeEntry.SourceChanceRaw),
                        config.GrenadeEntry.SourceChanceRaw);
                else if (field is "MinCount" or "MaxCount")
                    updated = (Changed(config.GrenadeQuantityParameterId)
                        ? (int)Math.Round(Value(config.GrenadeQuantityParameterId),
                            MidpointRounding.AwayFromZero)
                        : config.GrenadeEntry.SourceMinCount)
                        .ToString(CultureInfo.InvariantCulture);
            }
        }
        else if (tier == "Medium" || tier == "Expensive")
        {
            var ammo = tier == "Medium"
                ? baseline.Special.MediumStashAmmoProduction.AmmoEntries
                    .FirstOrDefault(x => x.CategoryNode == category.Node
                        && x.EntryNode == item.Node)
                : baseline.Special.ExpensiveStashProduction.AmmoEntries
                    .FirstOrDefault(x => x.CategoryNode == category.Node
                        && x.EntryNode == item.Node);
            if (ammo is not null && (field is "MinCount" or "MaxCount"))
            {
                var id = tier == "Medium"
                    ? baseline.Special.MediumStashAmmoProduction.ParameterId
                    : baseline.Special.ExpensiveStashProduction.AmmoQuantityParameterId;
                updated = PositiveCount(field == "MinCount"
                    ? ammo.SourceMinCount : ammo.SourceMaxCount,
                    Changed(id) ? Value(id) : 1.0)
                    .ToString(CultureInfo.InvariantCulture);
            }
            else if (tier == "Expensive" && field == "Chance")
            {
                var config = baseline.Special.ExpensiveStashProduction;
                var coupon = config.CouponEntries.FirstOrDefault(x =>
                    x.CategoryNode == category.Node && x.EntryNode == item.Node);
                if (coupon is not null
                    && Changed(config.CouponChanceParameterId)
                    && coupon.DifficultySids.Contains(
                        DifficultySid, StringComparer.OrdinalIgnoreCase))
                    updated = Decimal(Value(config.CouponChanceParameterId));
            }
        }
        else throw new InvalidDataException($"Unsupported stash tier {tier}.");
        updated = TuneSelection(sid, field, updated);
        if (field == "Chance" && TierChanceParameterId(tier, sid) is { } scopedId)
        {
            var multiplier = Value(scopedId);
            if (Math.Abs(multiplier - 1.0) > 0.0000001)
                updated = FormatLikeSource(
                    Math.Clamp(Number(updated) * multiplier, 0.0, 1.0),
                    updated);
        }
        return TuneCount(item.Fields, field, updated);
    }

    // One item-type mapping shared by tier and SmartLoot source families.
    public static string? SmartItemBiasId(string sid) => ItemBiasId(sid);

    public double SmartFactor(string sid)
    {
        if (SmartItemBiasId(sid) is null)
            throw new InvalidDataException("Unclassified SmartLoot item: " + sid);
        return TierFactor(sid);
    }

    public double SmartSelectionFactor(string sid, string selectionField)
    {
        if (SmartItemBiasId(sid) is null)
            throw new InvalidDataException("Unclassified SmartLoot item: " + sid);
        return SelectionFactor(sid, selectionField);
    }

    public string SmartSelection(string sid, string field, string raw)
    {
        if (field is not ("Weight" or "Chance") || raw.Length == 0) return raw;
        var factor = SmartSelectionFactor(sid, field);
        var output = factor <= .0000001 ? 0.0 : field == "Chance"
            ? Math.Clamp(Number(raw) * factor, 0, 1)
            : Math.Max(0, Number(raw) * factor);
        return FormatLikeSource(output, raw);
    }

    /// <summary>
    /// Shared SmartLoot count output for the world writer AND native group preview.
    /// Count suppression follows that exact item's native selection FIELD.
    /// Global Weight=0 does not silently suppress a Chance-only entry.
    /// </summary>
    public string SmartCount(string sid, string field, string raw,
        string originalMin, string originalMax, string selectionField)
    {
        if (field is not ("MinCount" or "MaxCount") || raw.Length == 0)
            return raw;
        if (SmartSelectionFactor(sid, selectionField) <= 0.0000001) return "0";
        if (sid is not ("Medkit" or "ArmyMedkit")) return raw;
        var original = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ItemPrototypeSID"] = sid,
            ["MinCount"] = originalMin,
            ["MaxCount"] = originalMax
        };
        return TuneCount(original, field, raw);
    }

    public static bool UniformSmartFactor(
        IEnumerable<double> values, out double factor)
    {
        var list = values.ToArray();
        // C# forbids capturing an out parameter inside a lambda (CS1628).
        // Capture an ordinary local while preserving the native group rule.
        var reference = list.Length > 0 ? list[0] : 1.0;
        factor = reference;
        return list.Length > 0
            && list.All(x => Math.Abs(x - reference) < .0000001)
            && Math.Abs(reference - 1) > .0000001;
    }

    public static string SmartSpawnEndpoint(string raw, double factor)
        => FormatLikeSource(Math.Clamp(Number(raw) * factor, 0, 1), raw);
}
