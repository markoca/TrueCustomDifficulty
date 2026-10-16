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

    // One opt-in, deliberately exaggerated diagnostic preset. Keep it OUT of
    // FactoryPresetNames: existing 12-role audit and exact-stock migrations
    // must remain unchanged. The QA copy is seeded only when absent.
    private const string RcSignalFileName = "RC - CFG Signal.json";
    private const string RcSignalResourceName = "TrueCustomDifficulty.QA.RC-CFG-Signal.json";
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

    // Exact 248-source stock identities shipped before role-first tuning.
    // These do not match player-edited files; never overwrite custom work.
    private static readonly Dictionary<string, string> PreRoleRebalanceFactoryBlobs =
        new(StringComparer.Ordinal)
    {
        ["Immersive"] = "3c4eea2fb6770f381b4513ea23686edbf3fc88fd",
        ["Tactical"] = "d5bff23a96938447c25320e1e628c2d1dea29ab1",
        ["Field Medic"] = "3ca87bc2a719507ebbc9980333cd08ea57deea87",
        ["Mutant Hunter"] = "43cf8f818707ecb2746331940bca44ecbf8f58f2",
        ["Anomaly Runner"] = "db3e54c7a022cdafa7a360409ba4d85fa06cc198",
        ["Wanderer"] = "1f537e4c768a58386f6c5c0cdb6e5b9921ead144",
        ["Mercenary"] = "22a862e8de46f66a99160d62b25834e0b56c4a62",
        ["Smuggler"] = "785f390e40c2014aaf756473903170b6e3f05ad4",
        ["Scavenger"] = "242734e642a08006753826b0c93c1a87d8ecdc33",
        ["Outcast"] = "4d43faf1ba8d9a7d4613cb78a0648a8435a5c06c",
        ["Hardcore"] = "aa900e79fd20ad26853333d6ee92864ade7e7715",
        ["Hardcore+"] = "4a849dff785a8c33864e14b651195ad206385aef",
    };

    // Exact pristine stock profiles before 2026-10-15 consumable cadence
    // and duration-override rebalance. Customized files remain untouched.
    private static readonly Dictionary<string, string> PreConsumableRebalanceFactoryBlobs =
        new(StringComparer.Ordinal)
    {
        ["Immersive"] = "76c40e5a7c364991d320d01be671cbf2a73a81b0",
        ["Tactical"] = "3017ab08b5f9933bce9e22603a02b2803e17c015",
        ["Field Medic"] = "68aaf6c89241a2fa066c73db838855faba41b924",
        ["Mutant Hunter"] = "cde631534a0ebf351512b0d976c774adf59c7c97",
        ["Anomaly Runner"] = "5f9e5f091ae94b6dbfbfd0c9d172aae74a69a38a",
        ["Wanderer"] = "5cdd7192768e06096f93d6a9c74941b294b07493",
        ["Mercenary"] = "33f3248392c502f4696137188fc0274490372f9a",
        ["Smuggler"] = "d0eedab8dba1d6a6a991606aa29954b722160266",
        ["Scavenger"] = "cd6d395e72fd0762a96e322770cfbb25ed5e684a",
        ["Outcast"] = "e7addc89ef6e160e7e6c81f22dbfe1d6a27694b4",
        ["Hardcore"] = "7e85bf6f1e892d4b106122beee45ce03db6dc3ea",
        ["Hardcore+"] = "1393b57ab6d7a8428d1728602396ea18c911b256",
    };

    // 2026-10-15 source-native full factory audit: only byte-identical
    // preceding stock JSONs migrate. Never overwrite user-modified profiles.
    private static readonly Dictionary<string, string> PreFullDeepAuditFactoryBlobs =
        new(StringComparer.Ordinal)
    {
        ["Immersive"] = "f33933af3b85f509ddcd58fe99fe0b9a00663e",
        ["Tactical"] = "b7f561645ae3ef7a285ed561efcbe7ae763336fc",
        ["Field Medic"] = "655555d3ce09f20c1d1549b90d2dc09899f81fb6",
        ["Mutant Hunter"] = "6879cde6a8728f0c9e86251495c64f171abf4206",
        ["Anomaly Runner"] = "e5dcd8d49dab42ec58fc445ccfaed5c7487d03da",
        ["Wanderer"] = "1d5de557850038c7173c97cb67dca445ec1149e0",
        ["Mercenary"] = "b49ebc3b91839c49c5d42c3a7391fe6616339062",
        ["Smuggler"] = "e8f17e36b5d8a03419d75a7f188f63b69ab7168e",
        ["Scavenger"] = "8b9419231fa0355ec5e12b1ad79a1572d94f6f77",
        ["Outcast"] = "ae64af6afa62ac67c2d33c282852d2b535804a44",
        ["Hardcore"] = "a91d781f85d2c6d4fd82704ee8d311fc7ab2488c",
        ["Hardcore+"] = "521b3902d1dbf9a57f3e21ed54a4d1e5ccaf0533",
    };

    // Final CFG route and integer-output audit: upgrade only pristine
    // pre-audit stock snapshots. User changes always win.
    private static readonly Dictionary<string, string> PreFinalCfgAuditFactoryBlobs =
        new(StringComparer.Ordinal)
    {
        ["Immersive"] = "db16beaf5af906a69742f6ae3c94349375b6a01c",
        ["Tactical"] = "8c3759d0823025bbf3a7d49b5a6ad823fae92927",
        ["Field Medic"] = "6de4f20ea23c7ea24266830451f79bd6c5ae9d5b",
        ["Mutant Hunter"] = "d74a716d89529386359ca8ee0a3be4d5038502ea",
        ["Anomaly Runner"] = "fc63d8cf0455854d80053a29b88488827cf3cc65",
        ["Wanderer"] = "e1a380844aec46dd4ac2e58290e45ea2f4eed1d1",
        ["Mercenary"] = "622580572e71fee53c4b2bd3d263b0bd438457ee",
        ["Smuggler"] = "47c8c67dd6469d8d04d56df9ef7548147aa1768b",
        ["Scavenger"] = "4a7ab4a244d923e932b46ae4c6be3d80a7b24c3f",
        ["Outcast"] = "7fb05b09cc86610e6c6d32754faeb5176cef1ba1",
        ["Hardcore"] = "29233b424c92e0d12faf80cdec1fc0d893154c61",
        ["Hardcore+"] = "f813e67c0aed365638c8e822a1bded45774b0769",
    };

    // Load the newly canonical A-Life stock values only when the previous
    // factory file is byte-identical. A user-edited file must not be replaced.
    private static readonly Dictionary<string, string> PreEffectiveALifeFactoryBlobs =
        new(StringComparer.Ordinal)
    {
        ["Immersive"] = "e21b9812866616a6bc9cc4dd337d56c3e82d7f75",
        ["Tactical"] = "560c735dcd1f4ba18e6942f727b11159d6e1428f",
        ["Field Medic"] = "a26c8021f42d62073e5a967be4444cd9a0332941",
        ["Mutant Hunter"] = "39ecdcc19e0144ec6ffea5977c785039c8a4fa10",
        ["Anomaly Runner"] = "8a93952d58c7ca5ba8a4b6feefe5a1e39817bfeb",
        ["Wanderer"] = "d671b9cf32f67932c7d518b2b0aa03f5931430f0",
        ["Mercenary"] = "b63ed1dd6fcd195d0a7c027d546825fed3493a86",
        ["Smuggler"] = "098a1159bb3e209ba4292e477dfa741b1f809381",
        ["Scavenger"] = "d2126bd33893994a400a80d1d3809d38e3f8a438",
        ["Outcast"] = "94797e80f8498383f20c73e8402f04915cb48e49",
        ["Hardcore"] = "c1a9b1ec2b87a59dd92b83e52bee302790b53e3e",
        ["Hardcore+"] = "6674aa193990081ed3e4166645873ac9e6e7b3ee",
    };

    // Exact pristine factory snapshots before STASH CONTENT / EXTRA STASH GEAR.
    // Only these byte-identical older stock files migrate; user-edited copies stay.
    private static readonly Dictionary<string, string> PreStashContentGearFactoryBlobs =
        new(StringComparer.Ordinal)
    {
        ["Anomaly Runner"] = "e71030009d9b56aa8c9e86611b9a27ddfed690f5",
        ["Field Medic"] = "a26c8021f42d62073e5a967be4444cd9a0332941",
        ["Hardcore"] = "bc732679c08c08121ebc9ee42e5731b09cd5ed9e",
        ["Hardcore+"] = "762a2aba5004a0d6474faae8ea497216f0e717f9",
        ["Immersive"] = "e21b9812866616a6bc9cc4dd337d56c3e82d7f75",
        ["Mercenary"] = "73d7d0219e4ad678a7c251de189c4b912bdd76ed",
        ["Mutant Hunter"] = "5b737bada7c573a69484e85a6737f87741053d4a",
        ["Outcast"] = "92813860b2ba0513b90dbc5516c55f42ca467193",
        ["Scavenger"] = "2261537e26b9d250bff313dabbbdae5cf9298f9a",
        ["Smuggler"] = "098a1159bb3e209ba4292e477dfa741b1f809381",
        ["Tactical"] = "d15b267d8c6a0ffc7f9f1f3005b66d4e0ca366fd",
        ["Wanderer"] = "8d123be710f4d625be5577c9efcae7d856bb0e7b",
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

        // QA profiles are NOT factory roles. Never overwrite a tester's edited
        // version or migrate it via the stock hash allowlists.
        var rcSignalPath = Path.Combine(directory, RcSignalFileName);
        if (!File.Exists(rcSignalPath))
            WriteEmbedded(RcSignalResourceName, rcSignalPath);

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

    // Exact stock 247-source files before Water's +25% Stamina and -10%
    // SPDrain split. A byte-identical factory preset should acquire its new
    // second Water strength; customized user copies are never overwritten.
    private static readonly Dictionary<string, string> PreWaterSplitFactoryBlobs =
        new(StringComparer.Ordinal)
    {
        ["Anomaly Runner"] = "d137e71e95c4a87d16a00d2f6bd7e5e4ae9dea87",
        ["Field Medic"] = "f82491c7488713854ad6e8b25afbf0462691d3ff",
        ["Hardcore"] = "b07148e1a40f203a9ed5480eb3f071aeea5eb322",
        ["Hardcore+"] = "e418f173fc950f3d9a6c249d60963cfce10e1a14",
        ["Immersive"] = "07ed02cd1db7bea301482a03f720198488ce9115",
        ["Mercenary"] = "fd8589e2540b8c908e947c50674c5a914978e300",
        ["Mutant Hunter"] = "a41dc89559f0f9394f08ad3c9b584c698d02be06",
        ["Outcast"] = "1c32c7ee583460dcafa0f3edbc278bb33261befa",
        ["Scavenger"] = "567382e0921d08f22d2390038fd28aee9b210281",
        ["Smuggler"] = "370c0b8303838fd663faf72f3f32de03bf0f2023",
        ["Tactical"] = "0eb421d60ca3d4ebc9ee359123fa4154a246ac26",
        ["Wanderer"] = "55677a66b97b0ef8faead62fb0da418514adf139",
    };

    private static bool IsUnmodifiedPreviousFactory(string name, string path)
    {
        if (!PreviousFactoryBlobs.TryGetValue(name, out var knownHash)
            && !PreAlifeFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreCooldownFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreShortDurationFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreFullCoverageFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreScarcityBalanceFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreWaterSplitFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreRoleRebalanceFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreConsumableRebalanceFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreFullDeepAuditFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreFinalCfgAuditFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreEffectiveALifeFactoryBlobs.TryGetValue(name, out knownHash)
            && !PreStashContentGearFactoryBlobs.TryGetValue(name, out knownHash))
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
                    && actual.Equals(preScarcityHash, StringComparison.OrdinalIgnoreCase))
                || (PreWaterSplitFactoryBlobs.TryGetValue(name, out var preWaterHash)
                    && actual.Equals(preWaterHash, StringComparison.OrdinalIgnoreCase))
                || (PreRoleRebalanceFactoryBlobs.TryGetValue(name, out var preRoleHash)
                    && actual.Equals(preRoleHash, StringComparison.OrdinalIgnoreCase))
                || (PreConsumableRebalanceFactoryBlobs.TryGetValue(name, out var preConsumableHash)
                    && actual.Equals(preConsumableHash, StringComparison.OrdinalIgnoreCase))
                || (PreFullDeepAuditFactoryBlobs.TryGetValue(name, out var preDeepHash)
                    && actual.Equals(preDeepHash, StringComparison.OrdinalIgnoreCase))
                || (PreFinalCfgAuditFactoryBlobs.TryGetValue(name, out var preFinalCfgHash)
                    && actual.Equals(preFinalCfgHash, StringComparison.OrdinalIgnoreCase))
                || (PreEffectiveALifeFactoryBlobs.TryGetValue(name, out var preEffectiveALifeHash)
                    && actual.Equals(preEffectiveALifeHash, StringComparison.OrdinalIgnoreCase))
                || (PreStashContentGearFactoryBlobs.TryGetValue(name, out var preStashHash)
                    && actual.Equals(preStashHash, StringComparison.OrdinalIgnoreCase));
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
