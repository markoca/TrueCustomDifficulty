using TrueCustomDifficulty.Services;

namespace UserPresetStorageContract;

internal static class Program
{
    private static int _passed;

    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "TCD-user-presets-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new UserPresetStorageService(root);
            var expected = Path.Combine(Path.GetFullPath(root), "TrueCustomDifficulty", "Presets");
            var actual = service.EnsureDirectory();

            Check(actual == expected, "Custom preset directory is rooted in per-user application data");
            Check(Directory.Exists(actual), "Custom preset directory is created");

            File.WriteAllText(Path.Combine(actual, "Zulu.json"), "{}");
            File.WriteAllText(Path.Combine(actual, "alpha.json"), "{}");
            File.WriteAllText(Path.Combine(actual, "ignore.txt"), "{}");

            var items = service.List();
            Check(items.Count == 2, "Only JSON presets are listed");
            Check(items[0].Name == "alpha" && items[1].Name == "Zulu", "User presets are sorted by display name");
            Check(items.All(item => Path.GetDirectoryName(item.Path) == expected), "Listed presets stay in per-user storage");

            Directory.Delete(actual, recursive: true);
            Check(service.List().Count == 0, "Missing per-user preset directory lists as empty");

            Console.WriteLine($"[PASS] {_passed} user preset storage assertions");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[FAIL] UserPresetStorageContract: " + ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _passed++;
    }
}
