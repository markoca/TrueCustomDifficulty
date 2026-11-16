using System.Globalization;

namespace TrueCustomDifficulty.Models;

/// <summary>
/// Native 2.1 per-player-rank Chance of the ten NPC consumable SIDs, taken
/// from the six GeneralNPC_Consumables_* source pools in the writer baseline.
/// Each pool branch is gated by game difficulty (Easy/Medium/Hard vs Stalker)
/// and player rank (Newbie/Experienced vs Veteran/Master). For one SID the
/// preview averages over the role pools that can carry it; a pool without
/// that SID in a branch contributes 0. This is a per-entry generator Chance,
/// not a guarantee that a given corpse holds the item.
/// </summary>
public sealed class NpcConsumableRankProfile
{
    public static readonly string[] Ranks = { "Newbie", "Experienced", "Veteran", "Master" };
    public static readonly string[] RankInitials = { "N", "E", "V", "M" };
    private static readonly string[] DifficultySids = { "Easy", "Medium", "Hard", "Stalker" };

    // sid -> difficulty SID -> rank index -> per carrying pool raw Chance
    // (null = the pool has no row for that SID in that branch).
    private readonly Dictionary<string, Dictionary<string, double?[][]>> _rows =
        new(StringComparer.Ordinal);

    private static readonly Lazy<NpcConsumableRankProfile?> SharedProfile = new(() =>
    {
        try
        {
            return FromBaseline(new Services.CfgWriterBaselineService().Load());
        }
        catch (Exception)
        {
            // Preview only: never block the editor if the resource is absent.
            return null;
        }
    });

    public static NpcConsumableRankProfile? Shared => SharedProfile.Value;

    /// <summary>UI reference difficulty name → native EGameDifficulty SID.</summary>
    public static string DifficultySid(string? referenceDifficulty) => referenceDifficulty switch
    {
        "Novice" => "Easy",
        "Stalker" => "Medium",
        "Veteran" => "Hard",
        "Master" => "Stalker",
        _ => "Hard"
    };

    public static NpcConsumableRankProfile FromBaseline(CfgWriterBaseline baseline)
    {
        var profile = new NpcConsumableRankProfile();
        var pools = baseline.Special.NpcMedicalLootProduction.Pools;
        var sids = pools
            .SelectMany(pool => pool.TemplateCategories)
            .SelectMany(category => category.Items)
            .Select(item => item.Fields.GetValueOrDefault("ItemPrototypeSID") ?? "")
            .Where(sid => sid.Length > 0)
            .Distinct(StringComparer.Ordinal);

        foreach (var sid in sids)
        {
            var carrying = pools.Where(pool => pool.TemplateCategories
                .SelectMany(category => category.Items)
                .Any(item => item.Fields.GetValueOrDefault("ItemPrototypeSID") == sid))
                .ToArray();
            var byDifficulty = new Dictionary<string, double?[][]>(StringComparer.Ordinal);
            foreach (var difficulty in DifficultySids)
            {
                var byRank = new double?[Ranks.Length][];
                for (var rank = 0; rank < Ranks.Length; rank++)
                {
                    byRank[rank] = carrying.Select(pool =>
                    {
                        var category = pool.TemplateCategories.FirstOrDefault(c =>
                            Gate(c.Fields, "Diff", "EGameDifficulty::" + difficulty)
                            && Gate(c.Fields, "PlayerRank", "ERank::" + Ranks[rank]));
                        var item = category?.Items.FirstOrDefault(i =>
                            i.Fields.GetValueOrDefault("ItemPrototypeSID") == sid);
                        return item is not null
                            && item.Fields.TryGetValue("Chance", out var raw)
                            && double.TryParse(raw, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out var chance)
                            ? chance : (double?)null;
                    }).ToArray();
                }
                byDifficulty[difficulty] = byRank;
            }
            profile._rows[sid] = byDifficulty;
        }
        return profile;
    }

    /// <summary>
    /// Mean Chance per player rank for <paramref name="sid"/> at the given
    /// native difficulty, after applying <paramref name="transform"/> to each
    /// existing source row (identity = vanilla). Absent rows stay 0.
    /// </summary>
    public double[]? Average(string sid, string difficultySid, Func<double, double> transform)
    {
        if (!_rows.TryGetValue(sid, out var byDifficulty)
            || !byDifficulty.TryGetValue(difficultySid, out var byRank))
            return null;
        return byRank.Select(pools => pools.Length == 0 ? 0.0
            : pools.Select(raw => raw is double chance
                    ? Math.Clamp(transform(chance), 0.0, 1.0) : 0.0)
                .Average()).ToArray();
    }

    private static bool Gate(IReadOnlyDictionary<string, string> fields, string key, string token)
        => fields.TryGetValue(key, out var raw)
            && raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(token, StringComparer.Ordinal);
}
