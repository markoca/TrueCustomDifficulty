using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;

namespace PresetSaveContract;

internal static class Program
{
    private static int _passed;

    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "TCD-preset-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            var presets = Path.Combine(root, "Presets");
            var target = Path.Combine(presets, "custom-preset.json");
            var service = new PresetService();

            var first = MakePreset("First", 1.25);
            service.Save(first, target);
            Check(File.Exists(target), "Preset is created in the requested Presets directory");
            Check(!Directory.EnumerateFiles(presets, "*.tmp").Any(), "No temp rename artifacts are created");
            Check(service.Load(target).Name == "First", "Created preset round-trips");

            var second = MakePreset("Second", 2.5);
            service.Save(second, target);
            var loaded = service.Load(target);
            Check(loaded.Name == "Second", "Existing preset is overwritten in place");
            Check(loaded.Values["combat.playerWeaponDamage"].Number == 2.5, "Overwrite persists the new value");
            Check(!Directory.EnumerateFiles(presets, "*.tmp").Any(), "Overwrite leaves no temp files");

            var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
                "src", "TrueCustomDifficulty", "Services", "PresetService.cs"));
            Check(!source.Contains("File.Move(tempPath, fullPath, overwrite: true)", StringComparison.Ordinal),
                "Windows-privileged temp-file replace path is absent");
            Check(source.Contains("File.WriteAllText(fullPath, json);", StringComparison.Ordinal),
                "Preset save writes completed JSON directly to the selected target");

            Console.WriteLine($"[PASS] {_passed} portable preset save assertions");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[FAIL] PresetSaveContract: " + ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static PresetFile MakePreset(string name, double value) => new()
    {
        PresetFormatVersion = PresetService.CurrentPresetVersion,
        Name = name,
        ToolVersion = "test",
        GameVersion = "test",
        ReferenceDifficulty = "Veteran",
        Values = new Dictionary<string, PresetValue>(StringComparer.Ordinal)
        {
            ["combat.playerWeaponDamage"] = new PresetValue { Kind = "number", Number = value }
        }
    };

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "publish.sh")) &&
                Directory.Exists(Path.Combine(current.FullName, "src", "TrueCustomDifficulty")))
                return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Run from the TrueCustomDifficulty repository.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _passed++;
    }
}
