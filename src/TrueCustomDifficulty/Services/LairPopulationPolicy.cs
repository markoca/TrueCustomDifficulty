using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// LAIR POPULATION (docs/ALIFE-LAIR-POPULATION.md).
///
/// How many inhabitants a world lair can hold: every eligible faction / rank
/// <c>MaxSpawnQuantity</c> in <c>LairPrototypes.cfg</c> (base + DLC1),
/// scaled half-up. A vanilla 0 stays 0 and a non-zero count never drops
/// below 1, so no lair loses a faction. Separate from GROUP SIZE (the
/// A-Life Director encounter caps). Story / special prototypes, settlement
/// guard posts, friendly scientists and the empty test prototype are not in
/// the manifest (tools/build_lair_population_manifest.py).
/// </summary>
public static class LairPopulationPolicy
{
    public const string HumanId = "alife.lairPopulation.human";
    public const string MutantId = "alife.lairPopulation.mutant";
    public const string Resource = "TrueCustomDifficulty.Data.lair-population-manifest.csv";

    public const int ExpectedRows = 1116;
    public const int ExpectedBaseRows = 640;
    public const int ExpectedDlcRows = 476;

    public static IReadOnlyList<string> ParameterIds { get; } = new[] { HumanId, MutantId };

    public sealed record Row(
        string Scope, string File, string Prototype, string FactionNode,
        string Faction, string Rank, string Group, int SourceMax);

    private static readonly Lazy<IReadOnlyList<Row>> Manifest = new(Load);

    public static IReadOnlyList<Row> Rows => Manifest.Value;

    public static string GroupOf(string parameterId) => parameterId switch
    {
        HumanId => "human",
        MutantId => "mutant",
        _ => throw new ArgumentException("Not a lair population control: " + parameterId)
    };

    /// <summary>Half-up; 0 stays 0, anything else stays at least 1.</summary>
    public static int Scale(int source, double multiplier)
        => source <= 0 ? 0 : Math.Max(1, (int)Math.Floor(source * multiplier + 0.5));

    /// <summary>Vanilla → scaled count for one example lair (board readout).</summary>
    public static (int Vanilla, int Current) Example(string prototype, string faction, string rank, double multiplier)
    {
        var row = Rows.FirstOrDefault(r => r.Scope == "baseGame" && r.Prototype == prototype
            && r.Faction == faction && r.Rank == rank)
            ?? throw new InvalidDataException($"Missing lair example {prototype}/{faction}/{rank}");
        return (row.SourceMax, Scale(row.SourceMax, multiplier));
    }

    private static IReadOnlyList<Row> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource)
            ?? throw new InvalidDataException("Missing embedded lair population manifest.");
        using var reader = new StreamReader(stream);
        if (reader.ReadLine() != "Scope,File,Prototype,FactionNode,Faction,Rank,Group,SourceMax")
            throw new InvalidDataException("Lair population manifest header drifted.");
        var rows = new List<Row>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            var v = line.Split(',');
            if (v.Length != 8)
                throw new InvalidDataException("Lair population manifest row: " + line);
            var expectedFile = v[0] switch
            {
                "baseGame" => "GameData/LairPrototypes.cfg",
                "dlc1" => "DLCGameData/DLC1/LairPrototypes.cfg",
                _ => throw new InvalidDataException("Unknown lair scope: " + v[0])
            };
            if (v[1] != expectedFile || v[6] is not ("human" or "mutant")
                || v[5] is not ("Newbie" or "Experienced" or "Veteran" or "Master")
                || !int.TryParse(v[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var max)
                || max < 0)
                throw new InvalidDataException("Unsafe lair population row: " + line);
            rows.Add(new Row(v[0], v[1], v[2], v[3], v[4], v[5], v[6], max));
        }
        if (rows.Count != ExpectedRows
            || rows.Count(r => r.Scope == "baseGame") != ExpectedBaseRows
            || rows.Count(r => r.Scope == "dlc1") != ExpectedDlcRows)
            throw new InvalidDataException("Lair population manifest size drifted.");
        return rows;
    }
}
