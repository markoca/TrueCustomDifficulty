using System.IO;
using System.Text.Json;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace RCSignalPatchContract;

// Uses the real embedded diagnostic preset and the production C# CFG writer.
// Staged BPatch success is NOT proof that STALKER 2 applied the patch in-game.
internal static class Program
{
    private const string ResourceName = "TrueCustomDifficulty.QA.RC-CFG-Signal.json";
    private const int ExpectedSources = 13;

    [STAThread]
    private static int Main(string[] args)
    {
        string? keep = null;
        try
        {
            if (args.Length > 0)
            {
                if (args.Length != 2 || args[0] != "--out")
                    throw new ArgumentException("Usage: RCSignalPatchContract [--out PATH]");
                keep = Path.GetFullPath(args[1]);
            }

            var catalog = new ParameterCatalogService().Load();
            var baseline = new CfgWriterBaselineService().Load();
            using var stream = typeof(MainViewModel).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidDataException("Missing embedded RC CFG Signal preset");
            var preset = JsonSerializer.Deserialize<PresetFile>(stream)
                ?? throw new InvalidDataException("Invalid embedded RC CFG Signal preset");

            Check(preset.Name == "RC - CFG Signal" &&
                  preset.ReferenceDifficulty == "Stalker" &&
                  preset.Values.Count == ExpectedSources,
                "Diagnostic preset identity/baseline/source count changed");
            Check(!preset.DrinkDurationOverride && !preset.MedicalOverride,
                "RC diagnostic must not mask native caps via override modes");

            var vm = new MainViewModel(catalog);
            vm.SelectedReferenceDifficulty = preset.ReferenceDifficulty;
            var byId = vm.AllParameters.ToDictionary(p => p.Id, StringComparer.Ordinal);

            foreach (var (id, entry) in preset.Values)
            {
                Check(byId.TryGetValue(id, out var p) &&
                      p.IsSlider && p.IsControlEnabled && entry.Kind == "slider" &&
                      entry.Number.HasValue, "RC source is not a live editable slider: " + id);
                p!.ApplyPresetValue(entry);
                Check(Math.Abs(p.CurrentNumber - entry.Number!.Value) < 0.000001,
                    "RC source silently snapped away from requested extreme value: " + id);
                Check(p.IsValueModifiedFromReference(),
                    "RC test setting has no real reference difference: " + id);
            }

            var modified = vm.AllParameters.Where(
                p => p.IsValueModifiedFromReference() && p.IsControlEnabled)
                .Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
            Check(modified.SetEquals(preset.Values.Keys) && modified.Count == ExpectedSources,
                "Sparse RC preset modified unrelated controls or omitted expected sources: " +
                string.Join(", ", modified.Except(preset.Values.Keys)));

            var sourceIds = baseline.EditPoints.Select(p => p.ParameterId)
                .ToHashSet(StringComparer.Ordinal);
            Check(preset.Values.Keys.All(sourceIds.Contains),
                "Every diagnostic setting must have a direct production CFG edit point");
            var output = keep ?? Path.Combine(Path.GetTempPath(), "TCD-RC-Signal",
                Guid.NewGuid().ToString("N"));

            try
            {
                var result = new CfgPatchWriterService().Write(vm, catalog, null, output);
                Check(result.FileCount > 0 && result.EditCount >= ExpectedSources &&
                      result.ParameterCount == ExpectedSources,
                    "Production writer did not emit all thirteen diagnostic source settings");
                var files = Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
                    .ToArray();
                Check(files.Length == result.FileCount && files.All(f => new FileInfo(f).Length > 0),
                    "RC staged output has unexpected or empty files");
                var allText = string.Join("\n", files.Select(File.ReadAllText));
                Check(allText.Contains("{bpatch}", StringComparison.Ordinal) &&
                      files.Any(f => f.Contains("cfg_patch", StringComparison.OrdinalIgnoreCase)),
                    "RC staged output lacks either prototype BPatch or global cfg_patch");
                foreach (var id in preset.Values.Keys)
                {
                    // Check the original source-backed key is actually emitted.
                    // Direct points with difficulty-specific SIDs are narrowed
                    // to the selected Stalker profile, just like the real writer.
                    var sourcePoints = baseline.EditPoints.Where(p =>
                        p.ParameterId == id && p.Scope == "baseGame" &&
                        (string.IsNullOrEmpty(p.DifficultySid)
                         || p.DifficultySid == catalog.DifficultyProfiles["Stalker"].Sid))
                        .ToArray();
                    Check(sourcePoints.Length > 0 &&
                          sourcePoints.Any(p => allText.Contains(
                              p.Key + " =", StringComparison.Ordinal)),
                        "Missing authored CFG leaf assignment for RC control: " + id);
                }

                var expectedDogCaps = new[] { 12, 18, 24, 36 };
                Check(ALifeIntegerCapSteps.EffectiveCaps("alife.mutantSpawn.blindDog", 3)
                      .SequenceEqual(expectedDogCaps),
                    "A-Life 3x diagnostic source caps differ from original-ranked CFG");

                Console.WriteLine("PASS: " + ExpectedSources +
                    " distinct RC settings emitted by the real production writer.");
                Console.WriteLine("Staged files: " + result.FileCount +
                    ", concrete CFG edits: " + result.EditCount);
                Console.WriteLine("NOT TESTED: repak/retoc container read-back and in-game gameplay.");
                if (keep is not null)
                    Console.WriteLine("Evidence: " + output);
                return 0;
            }
            finally
            {
                if (keep is null && Directory.Exists(output))
                    Directory.Delete(output, recursive: true);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("FAIL: " + e);
            return 1;
        }
    }

    private static void Check(bool ok, string description)
    {
        if (!ok)
            throw new InvalidDataException(description);
    }
}
