using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

public sealed class PresetService
{
    public const int CurrentPresetVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Save(PresetFile preset, string path)
    {
        Validate(preset);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        // Serialize completely before touching the target file. On Windows,
        // replacing a temp file with File.Move(..., overwrite: true) can fail
        // with ERROR_PRIVILEGE_NOT_HELD on otherwise writable user folders.
        // Presets are small text files, so write the completed JSON directly
        // to the requested portable Presets path instead.
        var json = JsonSerializer.Serialize(preset, Options);
        File.WriteAllText(fullPath, json);
    }

    public PresetFile Load(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var preset = JsonSerializer.Deserialize<PresetFile>(json, Options)
                          ?? throw new InvalidDataException("Preset file could not be parsed.");
            Validate(preset);
            return preset;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Preset file is not valid JSON or uses an invalid preset value.", ex);
        }
    }

    private static void Validate(PresetFile preset)
    {
        if (preset.PresetFormatVersion <= 0)
            throw new InvalidDataException("Preset format version must be a positive number.");

        if (preset.PresetFormatVersion > CurrentPresetVersion)
            throw new InvalidDataException(
                $"Preset format version {preset.PresetFormatVersion} is newer than this tool supports ({CurrentPresetVersion}).");

        if (string.IsNullOrWhiteSpace(preset.ReferenceDifficulty))
            throw new InvalidDataException("Preset does not contain a Base Preset.");

        preset.Values ??= new Dictionary<string, PresetValue>(StringComparer.Ordinal);
        preset.TraderItemOverrides ??= string.Empty;
        _ = TraderOffersPolicy.Decode(preset.TraderItemOverrides);
        if (!double.IsFinite(preset.GenericNpcRareGearChance)
            || preset.GenericNpcRareGearChance < 0.01
            || preset.GenericNpcRareGearChance > 1.0)
            throw new InvalidDataException(
                "Generic NPC rare GEAR chance must be between 1% and 100%.");

        foreach (var (label, fraction) in new[]
        {
            ("World mod item removal", preset.WorldModItemsRemoval),
            ("Corpse mod item removal", preset.CorpseModItemsRemoval)
        })
        {
            if (fraction is double value && (!double.IsFinite(value) || value < 0.0 || value > 1.0))
                throw new InvalidDataException($"{label} must be between 0% and 100%.");
        }
        if (preset.CorpseSmartLootNeedBoost is double needBoost
            && (!double.IsFinite(needBoost) || needBoost < 0.0 || needBoost > 2.0
                || Math.Abs(needBoost * 20.0 - Math.Round(needBoost * 20.0)) > 0.0000001))
            throw new InvalidDataException("Corpse SmartLoot NEED BOOST must use 0–2x in 0.05x steps.");

        // TRADER STOCK partial odds: known SELL ids only, 5–95% in 5% steps.
        if (preset.TraderSellOdds is { } sellOdds)
        {
            foreach (var (id, value) in sellOdds)
            {
                if (!TraderSellOddsViewModel.Ids.Contains(id))
                    throw new InvalidDataException($"Unknown trader stock category '{id}'.");
                if (!double.IsFinite(value) || value <= 0.0 || value >= 1.0
                    || Math.Abs(value * 20.0 - Math.Round(value * 20.0)) > 0.0000001)
                    throw new InvalidDataException($"Trader stock odds '{id}' must use 5–95% in 5% steps.");
            }
        }

        // STASH LOOT MIX presence sidecars: known ids only, 0–100% in 5% steps.
        if (preset.StashPresence is { } stashPresence)
        {
            foreach (var (id, value) in stashPresence)
            {
                if (!StashPresenceViewModel.Ids.Contains(id)
                    && id != StashPresenceViewModel.LegacyCheapDrinksId)
                    throw new InvalidDataException($"Unknown stash presence entry '{id}'.");
                if (!double.IsFinite(value) || value < 0.0 || value > 1.0
                    || Math.Abs(value * 20.0 - Math.Round(value * 20.0)) > 0.0000001)
                    throw new InvalidDataException($"Stash presence '{id}' must use 0–100% in 5% steps.");
            }
        }

        // Optional per-SID Chance sidecars: current 0–3x multiplier (0.05x
        // steps) or legacy absolute 0–100% (1% steps), never both.
        preset.NpcConsumableChances ??=
            new Dictionary<string, NpcConsumableChancePreset>(StringComparer.Ordinal);
        var npcSids = NpcConsumableItemViewModel.SourceItems
            .Select(item => item.Sid).ToHashSet(StringComparer.Ordinal);
        foreach (var (sid, settings) in preset.NpcConsumableChances)
        {
            if (!npcSids.Contains(sid) || settings is null)
                throw new InvalidDataException(
                    $"NPC consumable Chance preset contains an unknown or empty source SID: {sid}");
            if (settings.ChancePercent is double percent)
            {
                if (settings.Multiplier is not null || !double.IsFinite(percent)
                    || percent < 0.0 || percent > 100.0
                    || Math.Abs(percent - Math.Round(percent)) > 0.0000001)
                    throw new InvalidDataException(
                        $"NPC consumable Chance for {sid} must use 0–100% in 1% steps.");
            }
            else if (settings.Multiplier is double factor)
            {
                if (!double.IsFinite(factor) || factor < 0.0
                    || factor > NpcConsumableItemViewModel.MaximumMultiplier
                    || Math.Abs(factor * 20.0 - Math.Round(factor * 20.0)) > 0.0000001)
                    throw new InvalidDataException(
                        $"NPC consumable Chance multiplier for {sid} must use 0–3x in 0.05x steps.");
            }
            else if (settings.Enabled)
            {
                throw new InvalidDataException(
                    $"Enabled NPC consumable Chance for {sid} has no probability value.");
            }
        }

        foreach (var (id, value) in preset.Values)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidDataException("Preset contains an empty parameter ID.");

            if (value is null)
                throw new InvalidDataException($"Preset parameter '{id}' has no value object.");

            if (value.Number is double number && !double.IsFinite(number))
                throw new InvalidDataException($"Preset parameter '{id}' contains a non-finite number.");

            if (value.MinNumber is double min && !double.IsFinite(min))
                throw new InvalidDataException($"Preset parameter '{id}' contains a non-finite minimum.");

            if (value.MaxNumber is double max && !double.IsFinite(max))
                throw new InvalidDataException($"Preset parameter '{id}' contains a non-finite maximum.");
        }
    }
}
