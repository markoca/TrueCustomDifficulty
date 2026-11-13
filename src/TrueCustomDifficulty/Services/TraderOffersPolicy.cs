using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Verified native stock-item index for the compact merchant inventory editor.
/// A selection can remove only an item already present for that seller role.
/// Item identifiers are source SIDs (not invented English localization names).
/// </summary>
public static class TraderOffersPolicy
{
    private const string Resource = "TrueCustomDifficulty.Data.trader-offers-source-rows.csv";
    private const string SourceSha256 = "6B06E7F3B6B4844E4043F4F620F7B8A364ED7623728027315AD90E7D74C2E05F";

    public sealed record Offer(string RoleId, string Category, string ItemSid,
        bool Dlc1Only, bool ZeroChanceOnly);

    private static readonly Lazy<IReadOnlyList<Offer>> Source = new(LoadOffers);
    public static IReadOnlyList<Offer> Offers => Source.Value;

    public static string Key(string roleId, string sid) => roleId + "|" + sid;

    public static IReadOnlySet<string> Decode(string? value)
    {
        var known = Offers.Select(x => Key(x.RoleId, x.ItemSid))
            .ToHashSet(StringComparer.Ordinal);
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(value)) return result;

        if (value.Length > 24000)
            throw new InvalidDataException("Trader stock selection exceeds its size limit.");
        foreach (var key in value.Split(';'))
        {
            if (string.IsNullOrWhiteSpace(key) || !known.Contains(key) || !result.Add(key))
                throw new InvalidDataException("Unknown or duplicate trader stock item: " + key);
        }
        return result;
    }

    public static string Encode(IEnumerable<string> keys)
    {
        var ordered = keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var text = string.Join(";", ordered);
        _ = Decode(text);
        return text;
    }

    public static bool OnlyDlc1(IEnumerable<string> keys)
    {
        var lookup = Offers.ToDictionary(x => Key(x.RoleId, x.ItemSid), StringComparer.Ordinal);
        var selected = keys.ToArray();
        return selected.Length > 0 && selected.All(k => lookup.TryGetValue(k, out var offer) && offer.Dlc1Only);
    }

    private static IReadOnlyList<Offer> LoadOffers()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource)
            ?? throw new InvalidDataException("Missing embedded trader item offer manifest.");
        using var reader = new StreamReader(stream);
        var raw = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        if (!string.Equals(sha, SourceSha256, StringComparison.Ordinal))
            throw new InvalidDataException("Trader stock source hash changed; re-extract the CFG before editing.");

        var rows = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (rows.Length != 2940 || !rows[0].StartsWith(
                "trade_scope,trade_sid,trader_type,root_stock_sid,item_sid,item_category,",
                StringComparison.Ordinal))
            throw new InvalidDataException("Unexpected original trader item source structure.");

        var roles = TraderSellsPolicy.Roles
            .ToDictionary(x => x.SourceRole, x => x.Id, StringComparer.Ordinal);
        var records = new Dictionary<string, (string Role, string Category, string Sid,
            bool Base, bool Dlc, bool Candidate)>(StringComparer.Ordinal);

        foreach (var line in rows.Skip(1))
        {
            var fields = line.Split(',');
            if (fields.Length != 20 || !roles.TryGetValue(fields[2], out var role)
                || fields[4].Length == 0 || fields[5].Length == 0
                || fields[0] is not ("base" or "dlc1")
                || fields[17] is not ("CANDIDATE" or "ZERO_CHANCE"))
                throw new InvalidDataException("Unexpected original merchant stock row.");

            var key = Key(role, fields[4]);
            records.TryGetValue(key, out var old);
            if (old.Sid is not null && old.Category != fields[5])
                throw new InvalidDataException("Ambiguous item category for trader: " + key);
            records[key] = (role, fields[5], fields[4],
                old.Base || fields[0] == "base",
                old.Dlc || fields[0] == "dlc1",
                old.Candidate || fields[17] == "CANDIDATE");
        }
        if (records.Count != 260 || records.Values.Select(v => v.Sid).Distinct(StringComparer.Ordinal).Count() != 164)
            throw new InvalidDataException("Original 164-item / 260-role-item offer census drifted.");

        return records.Values
            .Select(v => new Offer(v.Role, v.Category, v.Sid,
                Dlc1Only: !v.Base && v.Dlc,
                ZeroChanceOnly: !v.Candidate))
            .OrderBy(x => x.RoleId, StringComparer.Ordinal)
            .ThenBy(x => x.Category, StringComparer.Ordinal)
            .ThenBy(x => x.ItemSid, StringComparer.Ordinal)
            .ToArray();
    }
}
