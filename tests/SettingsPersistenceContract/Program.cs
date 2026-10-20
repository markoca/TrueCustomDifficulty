using System.IO;
using System.Text.Json;
using TrueCustomDifficulty.Services;

namespace SettingsPersistenceContract;

internal static class Program
{
    private static int _passed;

    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "TCD-settings-" + Guid.NewGuid().ToString("N"));
        try
        {
            var portable = Path.Combine(root, "executable");
            var localData = Path.Combine(root, "local-appdata");
            var mods = Path.Combine(root, "game", "Content", "Paks", "~mods");
            Directory.CreateDirectory(portable);
            Directory.CreateDirectory(mods);

            var portableFile = Path.Combine(portable, "user-settings.json");
            var userFile = Path.Combine(localData, "TrueCustomDifficulty", "user-settings.json");

            var settings = new ModDestinationSettingsService(portable, localData);
            Check(settings.LoadPath() == "", "Fresh install has no mod path");
            settings.SavePath(mods);
            Check(File.Exists(portableFile), "Writable install saves next to EXE");
            Check(!File.Exists(userFile), "Writable install does not create profile fallback");
            Check(settings.LoadPath() == Path.GetFullPath(mods), "Portable path round-trips");
            Check(settings.RequireConfiguredPath() == Path.GetFullPath(mods), "Build resolves the portable path");
            Check(Directory.GetFiles(portable, "*.tmp").Length == 0, "Atomic portable save leaves no tmp files");

            // Simulate the Windows AccessDenied error at the exact atomic write
            // boundary, without depending on the test runner's ACLs.
            var deniedPortableWrites = 0;
            void DeniedPortable(string destination, string json)
            {
                if (destination == portableFile)
                {
                    deniedPortableWrites++;
                    throw new UnauthorizedAccessException("Access to the portable settings path is denied.");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllText(destination, json);
            }
            var fallback = new ModDestinationSettingsService(portable, localData, DeniedPortable);
            fallback.SavePath(mods);
            Check(deniedPortableWrites == 1, "Access denied on portable settings activates fallback");
            Check(File.Exists(userFile), "Fallback writes LocalAppData settings");
            Check(File.ReadAllText(portableFile).Contains("ModsFolder", StringComparison.Ordinal),
                  "Fallback does not destroy existing portable settings");
            Check(new ModDestinationSettingsService(portable, localData).LoadPath() == Path.GetFullPath(mods),
                  "Fresh service instance loads the user-profile fallback");

            var newerMods = Path.Combine(root, "second-game", "~mods");
            Directory.CreateDirectory(newerMods);
            fallback.SavePath(newerMods);
            Check(deniedPortableWrites == 1, "Existing fallback remains authoritative on subsequent saves");
            Check(new ModDestinationSettingsService(portable, localData).RequireConfiguredPath() == Path.GetFullPath(newerMods),
                  "Subsequent save and build resolve the updated fallback path");
            using (var json = JsonDocument.Parse(File.ReadAllText(userFile)))
                Check(json.RootElement.GetProperty("ModsFolder").GetString() == Path.GetFullPath(newerMods),
                      "Fallback stores the canonical selected folder");

            // The program directory must still be readable on a new machine
            // without a user-profile fallback.
            File.Delete(userFile);
            Check(settings.LoadPath() == Path.GetFullPath(mods),
                  "Legacy portable file is read when fallback is absent");
            File.WriteAllText(portableFile, "{ invalid json");
            MustThrow<InvalidDataException>(() => settings.LoadPath(), "Corrupt portable JSON is reported");

            File.WriteAllText(userFile, "{ invalid json");
            MustThrow<InvalidDataException>(() => settings.LoadPath(),
                "Corrupt authoritative fallback JSON is reported (not masked by portable)");

            var noUserRoot = new ModDestinationSettingsService(portable, "",
                (_, _) => throw new UnauthorizedAccessException("Denied"));
            MustThrow<IOException>(() => noUserRoot.SavePath(mods),
                "Missing LocalAppData produces an actionable write error");
            MustThrow<InvalidDataException>(() => fallback.SavePath(Path.GetDirectoryName(mods)!),
                "A Paks parent cannot be persisted as ~mods");
            MustThrow<InvalidDataException>(() => fallback.SavePath(Path.Combine(root, "missing", "~mods")),
                "A nonexistent ~mods folder cannot be persisted");

            Console.WriteLine($"[PASS] {_passed} settings portability assertions");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[FAIL] SettingsPersistenceContract: " + ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        _passed++;
    }

    private static void MustThrow<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T)
        {
            _passed++;
            return;
        }
        throw new Exception(message);
    }
}
