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

    /// <summary>
    /// Same state plus non-catalog sidecar values (STASH LOOT MIX presence).
    /// A sidecar away from 1.0 counts as changed.
    /// </summary>
    public StashEffectiveState WithValues(IReadOnlyDictionary<string, double>? extra)
    {
        if (extra is null || extra.Count == 0)
            return this;
        var merged = new Dictionary<string, double>(_values, StringComparer.Ordinal);
        foreach (var (id, value) in extra)
            merged[id] = value;
        return new StashEffectiveState(merged,
            _modified.Concat(extra.Where(pair => Math.Abs(pair.Value - 1.0) > 0.0000001)
                .Select(pair => pair.Key)),
            DifficultySid);
    }

    // ---- Presence of single-kind ammo Weight picks ------------------------
    // Ammo lists hold only ammo, so a per-tier presence scales the whole list
    // (see WeightListEdit). Drinks/food/meds/coupons use their stashMix factor.

    /// <summary>The value id that sets this category's presence, or null.</summary>
    public static string? PresenceId(string tier, CfgWriterCheapStashCategory category)
    {
        if (category.Items.Count == 0
            || category.Items.Any(item => !item.Fields.ContainsKey("Weight")
                || !item.Fields.ContainsKey("ItemPrototypeSID")))
            return null;
        var kinds = category.Items
            .Select(item => ItemBiasId(item.Fields["ItemPrototypeSID"]))
            .Distinct().ToArray();
        if (kinds.Length != 1)
            return null;
        return (tier, kinds[0]) switch
        {
            ("Cheap", "loot.stashMixAmmo") => StashPresenceViewModel.CheapAmmoId,
            ("Medium", "loot.stashMixAmmo") => StashPresenceViewModel.MediumAmmoId,
            // Rare ammo: the existing catalog Rare Ammo Chance also thins
            // the Rare ammo Weight picks, so one control means "Rare ammo".
            ("Expensive", "loot.stashMixAmmo") => "loot.expensiveStashAmmoChance",
            _ => null
        };
    }

    public double Presence(string tier, CfgWriterCheapStashCategory category)
        => PresenceId(tier, category) is { } id ? Math.Clamp(Value(id), 0.0, 1.0) : 1.0;

    /// <summary>
    /// The one rule for a native Weight list (verified: a Weight list always
    /// yields exactly ONE entry, picked by Weight; Chance and Weight never mix
    /// in one list). To make a control mean "keep X% of the game's odds" for
    /// its own items WITHOUT handing that share to the other items, the
    /// removed Weight goes to an appended native <c>Empty</c> entry, so the
    /// list total stays the source total. Presence scales the whole list.
    /// Returns null for a Chance list. Empty is null when nothing was removed;
    /// a legacy factor above 1x leaves no Empty (relative boost only).
    /// </summary>
    public (IReadOnlyList<string> Weights, IReadOnlyList<double> Values, string? Empty, double EmptyValue)?
        WeightListEdit(CfgWriterBaseline baseline, string tier, CfgWriterCheapStashCategory category)
    {
        if (category.Items.Count == 0
            || category.Items.Any(item => !item.Fields.ContainsKey("Weight")))
            return null;
        // STASH SUPPLY REDUCTION keeps (1 - r) of every Weight list's odds the
        // same way: the reduced share also goes to Empty (one rule for Chance
        // rows, Weight lists and SmartLoot spawn endpoints).
        var presence = Presence(tier, category) * SupplyRemaining();
        var source = category.Items.Sum(item => Math.Max(0.0, Number(item.Fields["Weight"])));
        var values = new List<double>(category.Items.Count);
        var weights = new List<string>(category.Items.Count);
        foreach (var item in category.Items)
        {
            var tuned = TierField(baseline, tier, category, item, "Weight");
            var value = Math.Max(0.0, Number(tuned)) * presence;
            values.Add(value);
            weights.Add(Math.Abs(presence - 1.0) < 0.0000001
                ? tuned
                : FormatLikeSource(value, item.Fields["Weight"]));
        }
        var empty = source - values.Sum();
        return empty > 0.0000001
            ? (weights, values, empty.ToString("0.####", CultureInfo.InvariantCulture), empty)
            : (weights, values, null, 0.0);
    }

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

    // Dedicated 0–100% native Chance scale. These four IDs are independent
    // from medical Weight biases; 100% preserves EACH authored Chance value.
    private static string? MedicalChanceScaleId(string sid) => sid switch
    {
        "Bandage" => "loot.stashMedicalBandageChanceScale",
        "Medkit" => "loot.stashMedicalMedkitChanceScale",
        "ArmyMedkit" => "loot.stashMedicalArmyMedkitChanceScale",
        "AntiRad" or "Antirad" => "loot.stashMedicalAntiRadChanceScale",
        _ => null
    };

    public bool MedicalDisabled(string sid)
    {
        if (sid is not ("Bandage" or "Medkit" or "ArmyMedkit"
            or "AntiRad" or "EcoMedkit")) return false;
        // A genuine medical quantity exclusion suppresses both fields.
        // A zero Weight dial must NOT switch off a Chance-only native row.
        // A zero Chance scale must NOT switch off a Weight-only native row.
        return (sid is "Medkit" or "ArmyMedkit")
            && Value($"loot.stash{sid}MaxCount", 0) < -.5;
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

    // Chance-only percent-of-source: 100% = authored native Chance.
    // An absolute 100% per-stash probability would be fabricated because
    // the same item SID can have distinct authored Chance rows per tier.
    public double ChanceFactor(string sid)
    {
        if (MedicalDisabled(sid)) return 0.0;
        return MedicalChanceScaleId(sid) is { } scale ? Value(scale) : 1.0;
    }

    public double SelectionFactor(string sid, string field)
        => field switch
        {
            "Weight" => TierFactor(sid),
            "Chance" => ChanceFactor(sid),
            _ => throw new InvalidDataException("Unknown stash selection field: " + field)
        };

    // Only regular Basic/Standard/Rare ItemGenerator Chance entries; no SmartLoot,
    // non-matching rank gate, shared clue stash or quest/direct rewards.
    // Each source row keeps its own authored native probability: one compact
    // per-category slider scales those probabilities, never averaging them.
    // Basic MEDS / FOOD / DRINKS cells (loot.stashMix*) also own the Basic
    // Chance rows (Bandage/Medkit/Vodka on Easy/Medium/Hard), so one cell
    // keeps X% of the game's odds in BOTH Basic list types. Basic RGD-5 is an
    // absolute chance with its own control and is not scaled here.
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
            "Cheap" when suffix is "Medical" or "Food" or "Drinks"
                => "loot.stashMix" + suffix,
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
    // Source quantity, not whole-stash presence. A positive nonzero native
    // one-item stack stays one item; zero means an explicit native zero count.
    // Keep this transform last so it composes with the existing AMMO and
    // Medical quantity controls without mutating the original source range.

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
        if (field == "Chance")
        {
            // Retain hidden legacy category Chance factors from older presets.
            // The universal tier control scales the actual native row Chance
            // AFTER the category factor, without creating synthetic drop odds.
            var scoped = TierChanceParameterId(tier, sid);
            var factor = (scoped is null ? 1.0 : Value(scoped))
                * (tier == "Medium" ? Value("loot.standardStashChanceScale")
                    : tier == "Expensive" ? Value("loot.rareStashChanceScale") : 1.0);
            if (Math.Abs(factor - 1.0) > 0.0000001)
                updated = FormatLikeSource(
                    Math.Clamp(Number(updated) * factor, 0.0, 1.0), updated);
        }
        updated = TuneCount(item.Fields, field, updated);
        // Supply reduction: Chance rows here; Weight lists in WeightListEdit
        // (Empty share). Stack sizes are never changed by it.
        var remaining = SupplyRemaining();
        if (remaining < 1.0 && field == "Chance")
            updated = FormatLikeSource(
                Math.Clamp(Number(updated) * remaining, 0, 1), updated);
        return updated;
    }

    /// <summary>1 - STASH SUPPLY REDUCTION (validated 0..1).</summary>
    public double SupplyRemaining()
    {
        var remaining = 1.0 - Value("loot.genericStashReduction", 0.0);
        if (!double.IsFinite(remaining) || remaining < 0.0 || remaining > 1.0)
            throw new InvalidDataException("Invalid generic stash supply reduction");
        return remaining;
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
        var factor = SelectionFactor(sid, selectionField);
        // Smart Chance groups (e.g. StashFood_Smart food/drink rolls): the
        // Basic MEDS / FOOD / DRINKS / COUPONS cells keep X% of these odds
        // too, exactly like their Weight groups. Hidden legacy ammo/grenade
        // factors stay Weight-only.
        if (selectionField == "Chance" && ItemBiasId(sid) is
                "loot.stashMixMedical" or "loot.stashMixFood"
                or "loot.stashMixDrinks" or "loot.stashMixCoupons")
            factor *= Math.Max(0.0, Value(ItemBiasId(sid)!));
        return factor;
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

    /// <summary>
    /// SmartLoot Weight group: share of the group's source Weight that is
    /// kept (Σ w·k / Σ w, capped at 1). Scaling the group's native spawn
    /// endpoints by this share while each Weight is scaled by its own k makes
    /// every item keep exactly k of its game odds — the same "keep X% of game
    /// odds" rule as the tier lists, using only native fields (no Empty).
    /// </summary>
    public static double SmartKeptShare(IEnumerable<(double Weight, double Factor)> items)
    {
        var list = items.ToArray();
        var source = list.Sum(item => Math.Max(0.0, item.Weight));
        if (source <= 0.0000001) return 1.0;
        var kept = list.Sum(item => Math.Max(0.0, item.Weight) * Math.Max(0.0, item.Factor));
        return Math.Min(1.0, kept / source);
    }

    /// <summary>
    /// Spawn-endpoint factor of one SmartLoot group (writer and preview):
    /// Weight group = kept share of source Weight; Chance group = 1 (items
    /// carry their own scaled Chance), or 0 when every item is switched off.
    /// </summary>
    public static double SmartGroupEndpointFactor(string field,
        IReadOnlyList<string> weights, IReadOnlyList<double> factors)
        => field == "Weight"
            ? SmartKeptShare(weights.Select((w, i) => (Number(w), factors[i])))
            : factors.All(f => f <= 0.0000001) ? 0.0 : 1.0;

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
