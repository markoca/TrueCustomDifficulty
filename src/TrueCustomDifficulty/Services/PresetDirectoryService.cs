using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Owns the portable Presets directory beside the executable.
/// Owns the twelve shipped factory presets and migrates away the retired vanilla-named factory files.
/// Unrelated user-created preset files are never deleted.
/// </summary>
public sealed class PresetDirectoryService
{
    private const string ReadmeResourceName = "TrueCustomDifficulty.Presets.README.md";
    private static readonly string[] FactoryPresetNames =
    {
        "Anomaly Runner",
        "Field Medic",
        "Hardcore",
        "Hardcore+",
        "Immersive",
        "Mercenary",
        "Mutant Hunter",
        "Outcast",
        "Scavenger",
        "Smuggler",
        "Tactical",
        "Wanderer"
    };

    private static readonly string[] LegacyFactoryPresetNames = { "Novice", "Stalker", "Veteran", "Master" };

    // SHA-1 Git blob identities of the previously shipped, unmodified factory JSONs.
    // Upgrade only exact stock files; locally edited presets remain the user's own.
    // Prior twelve factory files without the A-Life Director settings.
    // Used only for exact-byte factory upgrades; modified user copies stay untouched.
    private static readonly Dictionary<string, string> PreAlifeFactoryBlobs = new(StringComparer.Ordinal)
    {
        ["Anomaly Runner"] = "932ee5d991e1dc812f36f5cc9c81e6547d4d8750",
        ["Field Medic"] = "5f25e615f7523dd45a8cf9ad5c6f5c26a83e07f1",
        ["Hardcore"] = "8cbd3d71c8069f5460910082f1538f0d4e32c17f",
        ["Hardcore+"] = "432885a199e0078422469a2399d6620f80f91cc5",
        ["Immersive"] = "9574e6ea453a418d3e89d7aa137dafbf7624a76a",
        ["Mercenary"] = "960eb5e1ab1cb114e0a6f1010c677b2469ddcbaa",
        ["Mutant Hunter"] = "ae146ab2b6aa5954b77196baa9fddbcef3cbc6bb",
        ["Outcast"] = "82b33ce9a513769765edd68e6d1bc7393f2660ec",
        ["Scavenger"] = "f321e7f76bd49c871200633a5ca63ddc9c079815",
        ["Smuggler"] = "4fcbbbea93e033e88a37cdf9e93dfddec5c91d23",
        ["Tactical"] = "e29c8b7e940a013cfc6dc7120d4057f2ed0817e9",
        ["Wanderer"] = "5c14b7f49f909271a7faf30780ef775380992706"
    };

    private static readonly Dictionary<string, string> PreviousFactoryBlobs = new(StringComparer.Ordinal)
    {
        ["Immersive"] = "2886bc8983cec7cf893b5584cdd95b903fee62b8",
        ["Tactical"] = "917da30873fc9558a94b45ba90bc3b6bdc808788",
        ["Scavenger"] = "87b9e5a7ebcedc67c659abdb2911e46fe7325cef",
        ["Mutant Hunter"] = "4aa35decba71fcc321f7102647d3a2610323a46d",
        ["Anomaly Runner"] = "2fdf56972b7bb118e87cdd7a75895bce489e7bfc",
        ["Hardcore"] = "fbc49bb05db2fa3e05acc7c13c04470e86fb44c5",
        ["Hardcore+"] = "af5987719650099df0565027bb2e33bbcbe14bff"
    };



    // Exact identities of the twelve stock files before retiring mutant pacing controls.
    // Upgrade untouched installed copies; never overwrite custom player changes.
    private static readonly Dictionary<string, string> PreCooldownFactoryBlobs = new(StringComparer.Ordinal)
    {
        ["Anomaly Runner"] = "a9c0ce094946cf17693c330640b6782bb1e38279",
        ["Field Medic"] = "cae10e7151cc5f341a140ff8a4e729aead549cb5",
        ["Hardcore"] = "c2d7f350e6dc885b94485eaa23c7b315e64ad0a9",
        ["Hardcore+"] = "3eb6d3985a35891413ba97ab3165fda7cb48027b",
        ["Immersive"] = "7d9cfefdae3d01779e82d36686ad262d8cac3ff4",
        ["Mercenary"] = "90c92757c52b084a0b4b5e10b49759206758bb65",
        ["Mutant Hunter"] = "2fa7b55667f99eb6b7ccbc03c74878aaaa7f634d",
        ["Outcast"] = "365aec3e6446d5630364140ca20496421be19391",
        ["Scavenger"] = "51fc63275f8d695c4693b2db0603823b231b66fa",
        ["Smuggler"] = "e96a212b63144c2efeb76aab4fb9ec88abd22f15",
        ["Tactical"] = "d1386e5ad082d09e003aef998cca0a1d21a0803f",
        ["Wanderer"] = "eaedd7b9f14a274386f6dfbae3dcbd55f7a4ef23"
    };

    // Git blob SHA-1 identities of the unmodified stock presets shipped just
    // before the three independently editable Water/Energy short durations.
    // Preserves user edits; only exact original factory files are upgraded.
    private static readonly Dictionary<string, string> PreShortDurationFactoryBlobs = new(StringComparer.Ordinal)
    {
        ["Anomaly Runner"] = "7c38b41302187ded7c67339e53d43272b90991a0",
        ["Field Medic"] = "deb68004efcca7f4b01169c6d6144f3fe9a60c37",
        ["Hardcore"] = "64252be28859660919a991908d8447a221dc222b",
        ["Hardcore+"] = "73f77a7554a7bdda86d4f0b7296eac3319db56cf",
        ["Immersive"] = "ca60b566f4c2789fe3556844b6f008bf610ef48c",
        ["Mercenary"] = "88562b8b313f80bea384e3bca21a5d6343e7d9b2",
        ["Mutant Hunter"] = "c3ec7bca085e2990b0606f3ea221836562139282",
        ["Outcast"] = "0c92c045d3a53af7fd765f0b86a541d277201071",
        ["Scavenger"] = "5394ebe40a42c89bf7f393a0487b50064df7491c",
        ["Smuggler"] = "4bb34763340782ecf7b580074a8145af41bdf271",
        ["Tactical"] = "5a27b549d7db01e3fc279c2d01e9ab54202cec4b",
        ["Wanderer"] = "8626f6371f1a5bc793d3e50165876720a46b82d5"
    };

    // Exact stock identities of the 213-key, pre-complete-coverage factory
    // presets. Upgrade only untouched copies to all 247 source-backed controls.
    // Factory-named user edits must not be overwritten.
    private static readonly Dictionary<string, string> PreFullCoverageFactoryBlobs = new(StringComparer.Ordinal)
    {
        ["Anomaly Runner"] = "9ee7c1b681021f29059ef8f77f0921251f64ac3c",
        ["Field Medic"] = "ac6937227bd4a6b693c504a2e4e9eb2b8b57d8e7",
        ["Hardcore"] = "0b772daf862db333d6534cfb99082c870ef1b2e9",
        ["Hardcore+"] = "316c358603a8372b8b622ec56da747f7d598afff",
        ["Immersive"] = "67024e7275640be1c52eef992974cee8195cf9da",
        ["Mercenary"] = "6ddcc30a4ee2c9e660249a095fb33a252efd29ba",
        ["Mutant Hunter"] = "d00d40dbc52b2dc4a72a0c64a702e6a68b2a1971",
        ["Outcast"] = "7adf57f7a4abb84800874c725a418cf5c12e9652",
        ["Scavenger"] = "fda1f0268cafa1afe41991c666aa94bbb61f44ab",
        ["Smuggler"] = "5573751a2aea8b58365255ff7ce183027d28258f",
        ["Tactical"] = "ad690289c77c92bdd97b0ea5f3cbe4ac6b7bcee8",
        ["Wanderer"] = "4a166781ebe98967454d83521c7f07bc6e09edd5",
    };

    // Immediate predecessor: fully source-complete 247-entry factory files.
    // Match exact Git blob identities: NEVER overwrite customized factory names.
    private static readonly Dictionary<string, string> PreScarcityBalanceFactoryBlobs = new(StringComparer.Ordinal)
    {
        ["Anomaly Runner"] = "b2eb8030864f3588aefcd00103dd6a620dc15007",
        ["Field Medic"] = "827d491b84a00c05da656ca6b2da6a46fe57e219",
        ["Hardcore"] = "a210b7247cfa7de0e534de2e81b3f4b9d84fd01f",
        ["Hardcore+"] = "2b357713e3ad46dddd70b0a1abfc80bd396eeaeb",
        ["Immersive"] = "a45e761d9ba40c30cd1802efe487aa8e5a6a38b6",
        ["Mercenary"] = "b1cc2175ae464dcfc54ef35f7537aad88aba4f1f",
        ["Mutant Hunter"] = "98e6596f7a4eb8f20ada15abbd20097363277ef2",
        ["Outcast"] = "d9e73ed6a8123862a784eb5a3efa14066ba73353",
        ["Scavenger"] = "03d415702d72e2fd09580d214244770efe365f82",
        ["Smuggler"] = "0f4c402a4131987bdb227331e7b2d3ccee2cba3f",
        ["Tactical"] = "5f5ad32a1deb8685e5c57d5ba9b5a1b77ea056db",
        ["Wanderer"] = "0d3be7a1e48a937369c4bd04e7179a155b5e2cb5",
    };

    public string Ensure()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Presets");
        Directory.CreateDirectory(directory);

        var readmePath = Path.Combine(directory, "README.md");
        WriteEmbedded(ReadmeResourceName, readmePath);

        // v0.9.26.51 factory-preset migration:
        // the four old shipped presets are intentionally retired from the application.
        // Remove only those exact legacy factory filenames; unrelated user presets are untouched.
        foreach (var legacyName in LegacyFactoryPresetNames)
        {
            var legacyPath = Path.Combine(directory, $"{legacyName}.json");
            if (File.Exists(legacyPath))
                File.Delete(legacyPath);
        }

        foreach (var name in FactoryPresetNames)
        {
            var destination = Path.Combine(directory, $"{name}.json");
            if (!File.Exists(destination) || IsUnmodifiedPreviousFactory(name, destination))
                WriteEmbedded($"TrueCustomDifficulty.Presets.{name}.json", destination);
        }

        return directory;
    }

    public IReadOnlyList<PresetListItem> List()
    {
        var directory = Ensure();
        return Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => FactoryOrder(Path.GetFileNameWithoutExtension(path)))
            .ThenBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .Select(path => new PresetListItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path
            })
            .ToList();
    }

    private static int FactoryOrder(string name)
    {
        var index = Array.FindIndex(FactoryPresetNames, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : FactoryPresetNames.Length;
    }

    private static bool IsUnmodifiedPreviousFactory(string name, string path)
    {
        if (!PreviousFactoryBlobs.TryGetValue(name, out var knownHash)
            && !PreAlifeFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreCooldownFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreShortDurationFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreFullCoverageFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreScarcityBalanceFactoryBlobs.TryGetValue(name, out knownHash))
            return false;

        try
        {
            // Git object identity = SHA1("blob " + byte length + NUL + exact bytes).
            // This deliberately rejects every user-edited version, including edits
            // to metadata, rather than guessing whether it is still a factory file.
            var bytes = File.ReadAllBytes(path);
            var header = Encoding.ASCII.GetBytes($"blob {bytes.Length}\0");
            var input = new byte[header.Length + bytes.Length];
            Buffer.BlockCopy(header, 0, input, 0, header.Length);
            Buffer.BlockCopy(bytes, 0, input, header.Length, bytes.Length);
            var actual = Convert.ToHexString(SHA1.HashData(input));
            return actual.Equals(knownHash, StringComparison.OrdinalIgnoreCase)
                || (PreAlifeFactoryBlobs.TryGetValue(name, out var preAlifeHash)
                    && actual.Equals(preAlifeHash, StringComparison.OrdinalIgnoreCase))
                || (PreCooldownFactoryBlobs.TryGetValue(name, out var preCooldownHash)
                    && actual.Equals(preCooldownHash, StringComparison.OrdinalIgnoreCase))
                || (PreShortDurationFactoryBlobs.TryGetValue(name, out var preDurationHash)
                    && actual.Equals(preDurationHash, StringComparison.OrdinalIgnoreCase))
                || (PreFullCoverageFactoryBlobs.TryGetValue(name, out var preFullCoverageHash)
                    && actual.Equals(preFullCoverageHash, StringComparison.OrdinalIgnoreCase))
                || (PreScarcityBalanceFactoryBlobs.TryGetValue(name, out var preScarcityHash)
                    && actual.Equals(preScarcityHash, StringComparison.OrdinalIgnoreCase));
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void WriteEmbedded(string resourceName, string destinationPath)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded preset resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        File.WriteAllText(destinationPath, reader.ReadToEnd());
    }
}
