using System;
using System.Collections.Generic;
using System.Linq;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// ARTIFACT HUNTING source map (2.1 base dump, docs/environment/ARTIFACT-HUNTING.md).
///
/// The world's anomaly-field artifacts come from
/// <c>ArtifactSpawnerPrototypes.cfg → UniversalArtifactSpawner</c> (73 world
/// spawn actors) and its short-range child
/// <c>LesserZoneMagneticShortDistance</c> (1). Each has four zone ranks
/// (Newbie / Experienced / Veteran / Master = the area's rank, not the game
/// difficulty) with Count, MinCooldown/MaxCooldown (5400, never edited: the
/// world refill is driven by the game, e.g. emissions), SpawnChanceBase (25)
/// + SpawnChanceBonus (15) and a RarityChance split (Common / Uncommon / Rare /
/// Epic, summing to 100). Single quest / unique spawners (Weird*, Quest*,
/// WaterDrip) are never touched.
///
/// SPAWN CHANCE and ARTIFACTS PER SPAWN are direct edit points
/// in cfg-writer-baseline.json. RARE LUCK multiplies Rare and Epic by k and
/// renormalises the four shares back to 100 (written by the special writer).
/// </summary>
public static class ArtifactSpawnPolicy
{
    public const string SpawnChanceId = "environment.artifactSpawnChance";
    public const string RareLuckId = "environment.artifactRareLuck";
    public const string CountId = "environment.artifactCount";
    public const string File = "GameData/ArtifactSpawnerPrototypes.cfg";

    public static IReadOnlyList<string> ParameterIds { get; } =
        new[] { SpawnChanceId, RareLuckId, CountId };

    public static IReadOnlyList<string> Spawners { get; } =
        new[] { "UniversalArtifactSpawner", "LesserZoneMagneticShortDistance" };

    public static IReadOnlyList<string> Ranks { get; } =
        new[] { "Newbie", "Experienced", "Veteran", "Master" };

    public static IReadOnlyList<string> Rarities { get; } =
        new[] { "Common", "Uncommon", "Rare", "Epic" };

    // Vanilla RarityChance per zone rank (identical in both spawners).
    private static readonly double[][] VanillaRarity =
    {
        new[] { 80.0, 20.0, 0.0, 0.0 },
        new[] { 50.0, 48.0, 1.9, 0.1 },
        new[] { 30.0, 59.0, 10.0, 1.0 },
        new[] { 10.0, 65.0, 20.0, 5.0 }
    };

    // Source spelling of each RarityChance value ("80.f", "1.9f"), same order.
    public static IReadOnlyList<IReadOnlyList<string>> VanillaRarityRaw { get; } = new[]
    {
        new[] { "80.f", "20.f", "0.f", "0.f" },
        new[] { "50.f", "48.f", "1.9f", "0.1f" },
        new[] { "30.f", "59.f", "10.f", "1.f" },
        new[] { "10.f", "65.f", "20.f", "5.f" }
    };

    public const double VanillaSpawnChanceBase = 25.0;
    public const double VanillaSpawnChanceBonus = 15.0;

    public static IReadOnlyList<double> Vanilla(int rank) => VanillaRarity[rank];

    /// <summary>Common, Uncommon, Rare, Epic shares (sum 100) for a zone rank.
    /// Rare and Epic are multiplied by <paramref name="luck"/>; a rank without
    /// rare artifacts (Newbie) is unchanged.</summary>
    public static double[] Rarity(int rank, double luck)
    {
        var source = VanillaRarity[rank];
        var scaled = new[] { source[0], source[1], source[2] * luck, source[3] * luck };
        var total = scaled.Sum();
        if (total <= 0.0)
            return source.ToArray();
        return scaled.Select(v => Math.Round(v * 100.0 / total, 2)).ToArray();
    }

    /// <summary>Rare + Epic share of a zone rank, 0–100.</summary>
    public static double RarePlusShare(int rank, double luck)
    {
        var shares = Rarity(rank, luck);
        return shares[2] + shares[3];
    }

    /// <summary>
    /// Card text in player terms (no bare factor):
    /// "artifact chance 25% (+15% bonus)", "25% rare finds in Master zones",
    /// "1 artifact per spawn". Null for other ids.
    /// </summary>
    public static string? CardText(string id, double value) => id switch
    {
        SpawnChanceId => SpawnChanceText(value),
        RareLuckId => string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{RarePlusShare(3, value):0.#}% rare finds in Master zones"),
        CountId => value >= 1.5
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{value:0} artifacts per spawn")
            : "1 artifact per spawn",
        _ => null
    };

    /// <summary>
    /// Derived on-dial percentages, not user-facing multipliers. SPAWN CHANCE
    /// is the base chance per attempt (the 15% bonus scales separately).
    /// RARE+ is the conditional Rare+Epic share of Master-zone artifact finds;
    /// other zone ranks remain visible in the rarity breakdown below.
    /// Source factor values and their native steps remain unchanged.
    /// </summary>
    public static string KnobPercentage(string id, double value) => id switch
    {
        SpawnChanceId => string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{SpawnChance(value).Base:0.#}%"),
        RareLuckId => string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"{RarePlusShare(3, value):0.#}%"),
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    private static string SpawnChanceText(double factor)
    {
        var (chance, bonus) = SpawnChance(factor);
        return chance <= 0.0
            ? "0% · fields stay empty"
            : string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"artifact chance {chance:0.#}% (+{bonus:0.#}% bonus)");
    }

    /// <summary>Base and bonus spawn chance (percent, capped at 100).</summary>
    public static (double Base, double Bonus) SpawnChance(double factor)
        => (Math.Min(100.0, VanillaSpawnChanceBase * factor),
            Math.Min(100.0, VanillaSpawnChanceBonus * factor));
}
