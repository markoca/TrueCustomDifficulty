using System.IO;
using System.Reflection;
using System.Text.Json;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

public sealed class CfgWriterBaselineService
{
    private const string ResourceName = "TrueCustomDifficulty.Data.cfg-writer-baseline.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CfgWriterBaseline Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"Embedded writer baseline resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<CfgWriterBaseline>(json, Options)
            ?? throw new InvalidDataException("Embedded CFG writer baseline is invalid.");
    }
}
