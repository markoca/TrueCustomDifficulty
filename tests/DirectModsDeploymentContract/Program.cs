using System.IO;
using TrueCustomDifficulty.Services;

namespace DirectModsDeploymentContract;

internal static class Program
{
    private static int _passed;

    private static int Main()
    {
        var root = Path.Combine(Path.GetTempPath(), "TCD-direct-mods-" + Guid.NewGuid().ToString("N"));
        try
        {
            var mods = Path.Combine(root, "Game", "Content", "Paks", "~mods");
            var packed = Path.Combine(root, "packed");
            Directory.CreateDirectory(mods);
            Directory.CreateDirectory(packed);

            const string oldStem = "zzzzz_TrueCustomDifficulty_Old";
            foreach (var ext in new[] { ".pak", ".utoc", ".ucas" })
                File.WriteAllText(Path.Combine(mods, oldStem + ext), "old-" + ext);
            File.WriteAllText(Path.Combine(mods, "zzzz_UnrelatedMod.pak"), "keep");

            const string newStem = "zzzzz_TrueCustomDifficulty_Hardcore";
            var built = new[] { ".pak", ".utoc", ".ucas" }.Select(ext =>
            {
                var path = Path.Combine(packed, newStem + ext);
                File.WriteAllText(path, "new-" + ext);
                return path;
            }).ToArray();

            var deployment = new ModDeploymentService();
            var installed = deployment.Install(mods, built);
            Check(installed.Count == 3, "Complete triplet installed");
            Check(installed.All(path => Path.GetDirectoryName(path) == Path.GetFullPath(mods)), "Persistent results are directly in ~mods");
            Check(installed.All(File.Exists), "Installed triplet exists");
            Check(new[] { ".pak", ".utoc", ".ucas" }.All(ext => !File.Exists(Path.Combine(mods, oldStem + ext))), "Previous owned triplet replaced");
            Check(File.Exists(Path.Combine(mods, "zzzz_UnrelatedMod.pak")), "Unrelated mod preserved");
            Check(!Directory.EnumerateDirectories(mods, ".tcd-install-*").Any(), "Install staging cleaned");

            deployment.Install(mods, Array.Empty<string>());
            Check(new[] { ".pak", ".utoc", ".ucas" }.All(ext => !File.Exists(Path.Combine(mods, newStem + ext))), "No-difference build uninstalls owned triplet");
            Check(File.Exists(Path.Combine(mods, "zzzz_UnrelatedMod.pak")), "Unrelated mod survives uninstall");

            var repo = FindRepositoryRoot();
            var builder = File.ReadAllText(Path.Combine(repo, "src", "TrueCustomDifficulty", "Services", "ModBuildService.cs"));
            var vm = File.ReadAllText(Path.Combine(repo, "src", "TrueCustomDifficulty", "ViewModels", "MainViewModel.cs"));
            var publish = File.ReadAllText(Path.Combine(repo, "publish.sh"));
            Check(!builder.Contains("Path.Combine(AppContext.BaseDirectory, \"Output\")", StringComparison.Ordinal), "Builder has no persistent app Output path");
            Check(!builder.Contains("CommitTriplet(", StringComparison.Ordinal) && !builder.Contains("ReplaceOutputWithEmptyDirectory(", StringComparison.Ordinal), "Legacy Output transaction helpers removed");
            Check(builder.Contains("_deployment.Install(modsFolder, expected)", StringComparison.Ordinal) && builder.Contains("_deployment.Install(modsFolder, Array.Empty<string>())", StringComparison.Ordinal), "Build worker owns direct ~mods deployment");
            Check(vm.Contains("_modBuildService.Build(this, _catalog, _gameInstall, modsFolder!, progress)", StringComparison.Ordinal) && !vm.Contains("_modDeploymentService", StringComparison.Ordinal), "UI passes selected ~mods into build worker");
            Check(!publish.Contains("mkdir -p -- \"$PUBLISH_DIR/tools\" \"$PUBLISH_DIR/Presets\" \"$PUBLISH_DIR/Output\"", StringComparison.Ordinal), "Fresh publish does not create Output");

            Console.WriteLine($"[PASS] {_passed} direct ~mods deployment assertions");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[FAIL] DirectModsDeploymentContract: " + ex);
            return 1;
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

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
