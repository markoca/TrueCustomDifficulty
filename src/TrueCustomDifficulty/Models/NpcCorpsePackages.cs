namespace TrueCustomDifficulty.Models;

/// <summary>
/// The three authored corpse SmartLoot packages behind
/// <c>EconomyDifficulty.CorpseSmartLoot.GeneralNPC</c>. The native field is a
/// list, so any subset is a valid CFG value; vanilla only authors
/// Medicine+Ammo (Easy), Water (Medium) and none (Hard / Stalker). Values are
/// always written in this canonical order, comma + space separated, which
/// keeps every vanilla string and old preset text byte-identical.
/// </summary>
public static class NpcCorpsePackages
{
    public const string Medicine = "NPC_Medicine_Smart";
    public const string Ammo = "NPC_Ammo_Smart";
    public const string Water = "NPC_Water_Smart";

    public static IReadOnlyList<string> Sids { get; } = new[] { Medicine, Ammo, Water };

    /// <summary>Short chip captions, same order as <see cref="Sids"/>.</summary>
    public static IReadOnlyList<string> ChipLabels { get; } = new[] { "MED", "AMMO", "WATER" };

    public static IReadOnlyList<string> Parse(string? text) =>
        (text ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

    public static bool Contains(string? text, string sid)
        => Parse(text).Contains(sid, StringComparer.Ordinal);

    /// <summary>True when <paramref name="text"/> is a canonical subset.</summary>
    public static bool IsCanonical(string? text)
        => string.Equals(text ?? string.Empty, Compose(Parse(text)), StringComparison.Ordinal)
            && Parse(text).All(sid => Sids.Contains(sid, StringComparer.Ordinal))
            && Parse(text).Distinct(StringComparer.Ordinal).Count() == Parse(text).Count;

    public static string Compose(IEnumerable<string> selected)
    {
        var set = selected.ToHashSet(StringComparer.Ordinal);
        return string.Join(", ", Sids.Where(set.Contains));
    }

    public static string Toggle(string? text, string sid)
    {
        var set = Parse(text).ToHashSet(StringComparer.Ordinal);
        if (!set.Remove(sid))
            set.Add(sid);
        return Compose(set);
    }

    /// <summary>All 8 subsets, bit order Medicine=1, Ammo=2, Water=4.</summary>
    public static IReadOnlyList<string> AllCombinations { get; } =
        Enumerable.Range(0, 8)
            .Select(mask => Compose(Sids.Where((_, bit) => (mask & (1 << bit)) != 0)))
            .ToArray();

    public static string Label(string? text)
    {
        var parts = Parse(text);
        if (parts.Count == 0)
            return "None";
        return string.Join(" + ", parts.Select(sid => sid switch
        {
            Medicine => "Medicine",
            Ammo => "Ammo",
            Water => "Water",
            _ => sid
        }));
    }
}
