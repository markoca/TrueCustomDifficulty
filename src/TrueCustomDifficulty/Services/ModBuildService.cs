using System.Diagnostics;
using System.IO;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// End-to-end player build pipeline:
/// current UI state -> temporary minimal BPatch CFG tree -> repak .pak ->
/// retoc companion IoStore container -> validated .pak/.utoc/.ucas triplet
/// installed directly into the configured game ~mods folder.
///
/// Third-party executables are never bundled or downloaded by the application.
/// repak and retoc must be supplied by the user in the tools directory beside the app.
/// </summary>
public sealed class ModBuildService
{
    private const string OutputPrefix = "zzzzz_TrueCustomDifficulty_";
    private const string RepakVersion = "V11";
    private const string RetocEngineVersion = "UE5_5";
    private const string PakMountPoint = "../../../";

    private readonly CfgPatchWriterService _cfgWriter = new();
    private readonly ExternalToolsService _tools = new();
    private readonly ModDeploymentService _deployment = new();

    public ModBuildResult Build(
        MainViewModel vm,
        ParameterCatalog catalog,
        GameInstallInfo? gameInstall,
        string configuredModsFolder,
        IProgress<BuildProgressUpdate>? progress = null)
    {
        var modsFolder = ModDestinationSettingsService.ValidatePath(configuredModsFolder);
        var workRoot = Path.Combine(
            Path.GetTempPath(),
            "TrueCustomDifficulty",
            Guid.NewGuid().ToString("N"));

        var stagingRoot = Path.Combine(workRoot, "staging");
        var packedRoot = Path.Combine(workRoot, "packed");

        try
        {
            Directory.CreateDirectory(stagingRoot);
            Directory.CreateDirectory(packedRoot);

            var cfgResult = _cfgWriter.Write(vm, catalog, gameInstall, stagingRoot);

            if (cfgResult.FileCount == 0)
            {
                // No persistent build-output directory exists. An unchanged
                // build means uninstall only our previous root-level triplet.
                progress?.Report(new BuildProgressUpdate(4, "INSTALLING"));
                var installedFiles = _deployment.Install(modsFolder, Array.Empty<string>());
                progress?.Report(new BuildProgressUpdate(5, "VERIFYING INSTALL"));
                return new ModBuildResult
                {
                    OutputDirectory = modsFolder,
                    HasMod = false,
                    PatchFileCount = 0,
                    ParameterCount = 0,
                    EditCount = 0,
                    Files = installedFiles
                };
            }

            var repakPath = _tools.Require("repak");
            var retocPath = _tools.Require("retoc");

            var modBaseName = BuildOutputBaseName(vm.BuildPresetName);
            var pakPath = Path.Combine(packedRoot, modBaseName + ".pak");
            var utocPath = Path.Combine(packedRoot, modBaseName + ".utoc");
            var ucasPath = Path.Combine(packedRoot, modBaseName + ".ucas");

            // CFG patches are ordinary loose files and belong in the .pak container.
            // Explicit V11 + ../../../ produces paths such as
            // ../../../Stalker2/Content/GameLite/GameData/... inside the archive.
            progress?.Report(new BuildProgressUpdate(1, "PACKING PAK"));
            RunRepak(repakPath, stagingRoot, pakPath);

            // retoc's to-zen path creates the IoStore companion container. For this
            // CFG-only staging tree there are no cooked assets to convert, and retoc
            // intentionally leaves the already-existing .pak untouched.
            progress?.Report(new BuildProgressUpdate(2, "BUILDING IOSTORE"));
            RunRetoc(retocPath, stagingRoot, utocPath);

            var expected = new[] { pakPath, utocPath, ucasPath };

            progress?.Report(new BuildProgressUpdate(3, "VERIFYING PACKAGE"));
            ValidateTriplet(expected);

            // The complete triplet exists only in this temporary work root
            // until deployment atomically replaces our owned files in ~mods.
            progress?.Report(new BuildProgressUpdate(4, "INSTALLING"));
            var finalFiles = _deployment.Install(modsFolder, expected);
            progress?.Report(new BuildProgressUpdate(5, "VERIFYING INSTALL"));
            ValidateTriplet(finalFiles);

            return new ModBuildResult
            {
                OutputDirectory = modsFolder,
                HasMod = true,
                PatchFileCount = cfgResult.FileCount,
                ParameterCount = cfgResult.ParameterCount,
                EditCount = cfgResult.EditCount,
                Files = finalFiles
            };
        }
        finally
        {
            TryDeleteDirectory(workRoot);
            TryDeleteEmptyParent(Path.GetDirectoryName(workRoot));
        }
    }

    private static string BuildOutputBaseName(string? presetName)
    {
        var raw = string.IsNullOrWhiteSpace(presetName) ? "Custom" : presetName.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(raw
            .Select(ch => invalid.Contains(ch) || char.IsWhiteSpace(ch) ? '_' : ch)
            .ToArray());

        while (safe.Contains("__", StringComparison.Ordinal))
            safe = safe.Replace("__", "_", StringComparison.Ordinal);

        safe = safe.Trim('_', '.');
        if (string.IsNullOrWhiteSpace(safe))
            safe = "Custom";

        return OutputPrefix + safe;
    }

    private static void RunRepak(string repakPath, string stagingRoot, string pakPath)
    {
        var args = new[]
        {
            "pack",
            stagingRoot,
            pakPath,
            "--mount-point",
            PakMountPoint,
            "--version",
            RepakVersion
        };

        RunExternalTool(repakPath, args, Path.GetDirectoryName(pakPath), "repak");

        if (!File.Exists(pakPath) || new FileInfo(pakPath).Length == 0)
        {
            throw new InvalidDataException(
                "repak completed without producing a non-empty CFG .pak file." + Environment.NewLine +
                $"Expected: {pakPath}");
        }
    }

    private static void RunRetoc(string retocPath, string stagingRoot, string utocPath)
    {
        var args = new[]
        {
            "to-zen",
            stagingRoot,
            utocPath,
            "--version",
            RetocEngineVersion
        };

        RunExternalTool(retocPath, args, Path.GetDirectoryName(utocPath), "retoc");
    }

    private static void RunExternalTool(
        string executablePath,
        IEnumerable<string> arguments,
        string? workingDirectory,
        string displayName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidDataException($"{displayName} could not be started.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            var stdout = stdoutTask.GetAwaiter().GetResult().Trim();
            var stderr = stderrTask.GetAwaiter().GetResult().Trim();

            if (process.ExitCode != 0)
            {
                var details = BuildProcessDetails(stdout, stderr);
                throw new InvalidDataException(
                    $"{displayName} failed with exit code {process.ExitCode}." +
                    (string.IsNullOrWhiteSpace(details)
                        ? string.Empty
                        : Environment.NewLine + Environment.NewLine + details));
            }
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidDataException(
                $"{displayName} could not be executed from:{Environment.NewLine}  {executablePath}{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                ex);
        }
    }

    private static string BuildProcessDetails(string stdout, string stderr)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(stderr))
            parts.Add(stderr);
        if (!string.IsNullOrWhiteSpace(stdout))
            parts.Add(stdout);

        var text = string.Join(Environment.NewLine + Environment.NewLine, parts);
        const int maxLength = 6000;
        return text.Length <= maxLength ? text : text[^maxLength..];
    }

    private static void ValidateTriplet(IEnumerable<string> files)
    {
        var missing = new List<string>();
        var empty = new List<string>();

        foreach (var path in files)
        {
            if (!File.Exists(path))
            {
                missing.Add(Path.GetFileName(path));
                continue;
            }

            if (new FileInfo(path).Length == 0)
                empty.Add(Path.GetFileName(path));
        }

        if (missing.Count == 0 && empty.Count == 0)
            return;

        var messages = new List<string>();
        if (missing.Count > 0)
            messages.Add("Missing: " + string.Join(", ", missing));
        if (empty.Count > 0)
            messages.Add("Empty: " + string.Join(", ", empty));

        throw new InvalidDataException(
            "Packing did not produce a complete .pak/.utoc/.ucas triplet." + Environment.NewLine +
            string.Join(Environment.NewLine, messages));
    }

    private static void TryDeleteDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only. Build success/failure is determined before this point.
        }
    }

    private static void TryDeleteEmptyParent(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        try
        {
            if (!Directory.EnumerateFileSystemEntries(path).Any())
                Directory.Delete(path);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
