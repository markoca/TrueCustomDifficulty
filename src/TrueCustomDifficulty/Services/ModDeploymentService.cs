using System.IO;

namespace TrueCustomDifficulty.Services;

/// <summary>Stages a complete difficulty triplet, replacing only this application's prior triplets.</summary>
public sealed class ModDeploymentService
{
    private const string OwnedPrefix = "zzzzz_TrueCustomDifficulty_";
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pak", ".utoc", ".ucas" };

    public IReadOnlyList<string> Install(string configuredModsFolder, IReadOnlyList<string> builtFiles)
    {
        var mods = ModDestinationSettingsService.ValidatePath(configuredModsFolder);
        ValidateBuiltFiles(builtFiles);

        var working = Path.Combine(mods, ".tcd-install-" + Guid.NewGuid().ToString("N"));
        var incoming = Path.Combine(working, "incoming");
        var previous = Path.Combine(working, "previous");
        var installed = new List<string>();
        var displaced = new List<(string Original, string Backup)>();
        var canClean = true;

        try
        {
            Directory.CreateDirectory(incoming);
            Directory.CreateDirectory(previous);

            foreach (var file in builtFiles)
            {
                var staged = Path.Combine(incoming, Path.GetFileName(file));
                File.Copy(file, staged);
                if (new FileInfo(staged).Length != new FileInfo(file).Length)
                    throw new IOException("Incomplete staged archive: " + Path.GetFileName(file));
            }

            foreach (var existing in Directory.EnumerateFiles(mods, "*", SearchOption.TopDirectoryOnly))
            {
                if (!IsOwnedFile(existing))
                    continue;
                var backup = Path.Combine(previous, Path.GetFileName(existing));
                File.Move(existing, backup);
                displaced.Add((existing, backup));
            }

            foreach (var file in builtFiles)
            {
                var target = Path.Combine(mods, Path.GetFileName(file));
                File.Move(Path.Combine(incoming, Path.GetFileName(file)), target);
                installed.Add(target);
            }

            return installed;
        }
        catch
        {
            try
            {
                foreach (var target in installed)
                    if (File.Exists(target))
                        File.Delete(target);
                foreach (var (original, backup) in displaced)
                    if (File.Exists(backup))
                        File.Move(backup, original);
            }
            catch (Exception ex)
            {
                canClean = false; // Leave recoverable backups in place.
                throw new IOException("Installation/restore failed. Previous files remain in " + previous, ex);
            }
            throw;
        }
        finally
        {
            if (canClean)
            {
                try
                {
                    if (Directory.Exists(working))
                        Directory.Delete(working, recursive: true);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static void ValidateBuiltFiles(IReadOnlyList<string> files)
    {
        if (files.Count == 0)
            return; // A vanilla-only build uninstalls our previous output.
        if (files.Count != 3)
            throw new InvalidDataException("A complete .pak/.utoc/.ucas triplet is required.");

        var names = files.Select(Path.GetFileName).ToArray();
        if (names.Any(name => string.IsNullOrWhiteSpace(name) ||
            !name.StartsWith(OwnedPrefix, StringComparison.Ordinal) ||
            !Extensions.Contains(Path.GetExtension(name))) ||
            names.Select(Path.GetFileNameWithoutExtension).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1 ||
            names.Select(Path.GetExtension).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3 ||
            files.Any(file => !File.Exists(file) || new FileInfo(file).Length == 0))
            throw new InvalidDataException("Incomplete or invalid difficulty output.");
    }

    private static bool IsOwnedFile(string path)
    {
        var name = Path.GetFileName(path);
        return name.StartsWith(OwnedPrefix, StringComparison.Ordinal) &&
            Extensions.Contains(Path.GetExtension(name));
    }
}
