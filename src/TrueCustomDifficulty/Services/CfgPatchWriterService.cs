using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Turns the current UI state into minimal STALKER 2 config patches.
///
/// Prototype CFGs are emitted as BPatch files inside the matching prototype
/// folder. Singular/global CFGs are emitted as cfg_patch files beside the
/// original logical CFG path. No full vanilla CFG is copied into the final mod output.
/// </summary>
public sealed partial class CfgPatchWriterService
{
    private const string PatchSuffix = "TrueCustomDifficulty";
    private readonly CfgWriterBaselineService _baselineService = new();

    public CfgWriteResult Write(
        MainViewModel vm,
        ParameterCatalog catalog,
        GameInstallInfo? gameInstall,
        string outputRoot)
    {
        var baseline = _baselineService.Load();
        ValidateBaseline(baseline, catalog);

        if (!catalog.DifficultyProfiles.TryGetValue(vm.SelectedReferenceDifficulty, out var difficulty))
            throw new InvalidDataException($"Unknown Base Preset: {vm.SelectedReferenceDifficulty}");

        // Only active controls are allowed to contribute production output.
        // A dependent Master child can retain a custom value in memory/presets while
        // its parent bundle is OFF, but that inactive value must not leak into CFG output.
        // IsModified is intentionally an aggregate *visual* state for radar
        // anchors. A child source can highlight its parent even when the
        // parent's own CFG input remains vanilla. Never treat that highlight
        // as a writer change: doing so can trigger unrelated special
        // production fanouts (notably the Rare Stash coupon source).
        // Source-backed children remain independent live parameters.
        var modified = vm.AllParameters
            .Where(p => p.IsValueModifiedFromReference() && p.IsControlEnabled)
            .ToList();
        var modifiedById = modified.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var allById = vm.AllParameters.ToDictionary(p => p.Id, StringComparer.Ordinal);
        // Reject incompatible overlapping weapon-slot edits BEFORE touching the
        // existing staging output directory or writing any mod files.
        if (allById.TryGetValue("loot.stashGearEnabled", out var gearOn)
            && gearOn.CurrentBoolean
            && allById.TryGetValue("loot.worldWeaponShuffle", out var worldShuffle)
            && worldShuffle.CurrentBoolean)
            throw new InvalidDataException(
                "Extra Stash Gear and World Weapon Shuffle cannot both be enabled. " +
                "They edit the same authored weapon stash slots. Turn off one to build safely.");

        // At zero coverage no private gear generators exist, so modified bonus
        // Weight/Chance knobs cannot affect produced CFG. The separate master
        // opt-in at 0% remains valid for its 13 approved authored removals.
        // Validate BEFORE touching staging to avoid destroying previous output.
        if (allById.TryGetValue("loot.stashGearEnabled", out var gearMaster)
            && gearMaster.CurrentBoolean
            && allById.TryGetValue("loot.stashGearCoveragePercent", out var gearCoverage)
            && Math.Clamp((int)Math.Round(gearCoverage.CurrentNumber), 0, 100) == 0)
        {
            var inactiveGearTunings = StashGearParameterIds
                .Where(id => id is not ("loot.stashGearEnabled"
                    or "loot.stashGearCoveragePercent") && modifiedById.ContainsKey(id))
                .ToArray();
            if (inactiveGearTunings.Length > 0)
                throw new InvalidDataException(
                    "Extra Stash Gear Coverage is 0%: Bonus Mix and Bonus Chance changes " +
                    "cannot affect added gear. Increase Coverage or reset these settings:" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine,
                        inactiveGearTunings.Select(id => "  - " + allById[id].Label)));
        }

        // A 1-100% actor coverage with all four class weights zero is a
        // legitimate "remove approved original gear, add none" configuration.
        // However Bonus Chance cannot affect any loot there: no private item
        // generator candidate will be emitted. Do not count its edited value
        // as a successful CFG change merely because removals still occur.
        if (allById.TryGetValue("loot.stashGearEnabled", out var emptyMixMaster)
            && emptyMixMaster.CurrentBoolean
            && allById.TryGetValue("loot.stashGearCoveragePercent", out var emptyMixCoverage)
            && Math.Clamp((int)Math.Round(emptyMixCoverage.CurrentNumber), 0, 100) > 0
            && modifiedById.ContainsKey("loot.stashGearBonusChance")
            && StashGearParameterIds
                .Where(id => id.EndsWith("Weight", StringComparison.Ordinal))
                .All(id => allById.TryGetValue(id, out var weight)
                    && Math.Clamp((int)Math.Round(weight.CurrentNumber * 10.0), 0, 30) == 0))
        {
            throw new InvalidDataException(
                "Extra Stash Gear Bonus Mix has no enabled categories: Bonus Chance " +
                "cannot affect any added gear. Raise at least one category weight " +
                "or reset Bonus Chance.");
        }

        var deferred = baseline.DeferredParameterIds.ToHashSet(StringComparer.Ordinal);

        var blocked = modified
            .Where(p => deferred.Contains(p.Id))
            .OrderBy(p => p.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (blocked.Count > 0)
        {
            var details = string.Join(
                Environment.NewLine,
                blocked.Select(p => $"  - {p.Label} ({p.Id})"));

            throw new InvalidDataException(
                "These controls still require aggregate CFG writer design and cannot be built safely yet:" +
                Environment.NewLine + Environment.NewLine + details + Environment.NewLine + Environment.NewLine +
                "Reset those controls to their selected vanilla baseline and build again.");
        }

        if (string.IsNullOrWhiteSpace(outputRoot))
            throw new ArgumentException("CFG staging output path is required.", nameof(outputRoot));

        RecreateDirectory(outputRoot);

        var documents = new Dictionary<string, PatchDocument>(StringComparer.OrdinalIgnoreCase);
        var writtenParameters = new HashSet<string>(StringComparer.Ordinal);
        var editCount = 0;
        var hasDlc1 = gameInstall?.HasDlc1 == true;

        foreach (var parameter in modified)
        {
            var points = baseline.EditPoints
                .Where(p => p.ParameterId.Equals(parameter.Id, StringComparison.Ordinal))
                .Where(p => string.IsNullOrWhiteSpace(p.DifficultySid)
                    || p.DifficultySid.Equals(difficulty.Sid, StringComparison.OrdinalIgnoreCase))
                .Where(p => IsScopeApplicable(p.Scope, hasDlc1))
                .ToList();

            foreach (var point in points)
            {
                var document = GetDocument(documents, point.File);
                var value = TransformValue(parameter, point);
                if (ValuesEquivalent(value, point.SourceRaw))
                    continue;

                document.AddAssignment(point.Prototype, point.NodePath, point.Key, value);
                writtenParameters.Add(parameter.Id);
                editCount++;
            }
        }

        editCount += WriteAdditionalMechanics(
            baseline,
            modifiedById,
            allById,
            difficulty.Sid,
            documents,
            writtenParameters);

        editCount += WriteCorpseSmartLoot(
            baseline,
            modifiedById,
            allById,
            difficulty.Sid,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteNpcCorpseAmmoProduction(
            baseline,
            modifiedById,
            allById,
            difficulty.Sid,
            documents,
            writtenParameters);

        editCount += WriteNpcMedicalLootProduction(
            baseline,
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteCheapStashProduction(
            baseline,
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteMediumStashAmmoProduction(
            baseline,
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteExpensiveStashProduction(
            baseline,
            modifiedById,
            allById,
            difficulty.Sid,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteWorldStashSmartLoot(
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteCabinDirectMedkits(
            modifiedById, allById, documents, writtenParameters);

        editCount += WriteRegularStashDirectMedical(
            modifiedById, allById, documents, writtenParameters);

        editCount += WriteDlcCheapNoFreshStashes(
            baseline,
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteDestructibleAmmoProduction(
            baseline,
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteDestructibleGrenadeProduction(
            baseline,
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WritePsyOverrides(
            baseline,
            modifiedById,
            allById,
            difficulty.Sid,
            documents,
            writtenParameters);

        var worldWeaponDistribution = WriteWorldWeaponDistribution(
            modifiedById,
            allById,
            hasDlc1,
            outputRoot,
            documents,
            writtenParameters);
        editCount += worldWeaponDistribution.EditCount;

        // Strictly allowlisted original regular weapon reward nodes disappear
        // only while the Extra Stash Gear master checkbox is enabled.
        editCount += WriteStashAuthoredWeaponRemovals(allById, documents);
        editCount += WriteStashGearCoverage(allById, hasDlc1, documents);

        editCount += WriteWorldAttachmentShuffle(
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteWorldDetectorSpawns(
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        editCount += WriteBlueprintShuffle(
            modifiedById,
            allById,
            documents,
            writtenParameters);

        editCount += WriteTraderMinBuyCondition(
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters);

        var f1BoxRemoval = WriteWorldF1GrenadeBoxes(
            modifiedById,
            hasDlc1,
            outputRoot,
            writtenParameters);
        editCount += f1BoxRemoval.EditCount;

        var crateRemoval = WriteCrateSpawnRemovals(
            baseline,
            modifiedById,
            hasDlc1,
            outputRoot,
            writtenParameters);
        editCount += crateRemoval.EditCount;

        var looseRemoval = WriteLooseWorldLootRemovals(modifiedById, hasDlc1, outputRoot, writtenParameters);
        editCount += looseRemoval.EditCount;

        var uniqueWeaponShuffle = WriteLooseUniqueWeaponShuffle(baseline.Special.LooseUniqueWeaponShuffle,
            modifiedById, outputRoot, writtenParameters);
        editCount += uniqueWeaponShuffle.EditCount;


        // One whole-percent control and the opt-in switch are handled by the
        // safe, source-audited physical actor writer rather than generic edit points.
        foreach (var id in StashGearParameterIds)
            if (modifiedById.ContainsKey(id))
                writtenParameters.Add(id);

        // The writer must not demand concrete output for an unchanged aggregate
        // anchor flagged IsModified only because one of its child controls moved.
        var modifiedSupported = modified
            .Where(p => p.IsValueModifiedFromReference() && !deferred.Contains(p.Id))
            .Select(p => p.Id)
            .ToHashSet(StringComparer.Ordinal);
        // NPC Bonus Ammo Drops remains editable and serializable when the
        // current corpse package is Water or None. Those stored values are
        // dormant, not writer failures or applied CFG changes.
        var dormantSavedOnly = new HashSet<string>(StringComparer.Ordinal);
        var corpseAmmo = baseline.Special.NpcCorpseAmmoProduction;
        if (modifiedSupported.Contains(corpseAmmo.ParameterId)
            && allById.TryGetValue(corpseAmmo.CorpseChoiceParameterId, out var corpseChoice)
            && !corpseChoice.CurrentText.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(corpseAmmo.SourceSid, StringComparer.Ordinal))
            dormantSavedOnly.Add(corpseAmmo.ParameterId);

        var missing = modifiedSupported
            .Where(id => !writtenParameters.Contains(id) && !dormantSavedOnly.Contains(id))
            .OrderBy(id => id).ToList();
        if (missing.Count > 0)
        {
            // Integer source-backed A-Life limits are quantized per rank. A 0.95x
            // editor value applied to a source count of 1 still writes 1, so it
            // cannot produce a BPatch. Do not pretend the change had an effect.
            var inertIntegerIds = missing.Where(id => baseline.EditPoints.Any(point =>
                point.ParameterId == id && point.Operation is
                    "scaleIntegerHalfUp" or "scaleIntegerHalfUpPreserveZero")).ToList();
            if (inertIntegerIds.Count > 0)
            {
                throw new InvalidDataException(
                    "These A-Life group-size multipliers do not change any actual integer CFG limits " +
                    "after rounding and therefore cannot produce BPatch edits. " +
                    "Use a stronger step that changes at least one group count, or reset to 1x:" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, inertIntegerIds.Select(id => $"  - {id}")));
            }

            throw new InvalidDataException(
                "CFG writer has no concrete output for these modified controls:" + Environment.NewLine +
                string.Join(Environment.NewLine, missing.Select(id => $"  - {id}")));
        }

        var fileCount = f1BoxRemoval.FileCount + crateRemoval.FileCount + looseRemoval.FileCount + uniqueWeaponShuffle.FileCount + worldWeaponDistribution.FileCount;
        foreach (var pair in documents.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (pair.Value.IsEmpty)
                continue;

            var outputPath = GetOutputPath(outputRoot, pair.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, pair.Value.Render(pair.Key), new UTF8Encoding(false));
            fileCount++;
        }

        // Logical writer route counts must agree with the physical staged
        // output. Cross-domain overwrites or an omitted file otherwise make
        // Build Mod report a plausible but false number of CFG patches.
        // Prototype BPatch files end in .cfg, but global/singleton patches
        // intentionally end in .cfg_patch_TrueCustomDifficulty (no trailing
        // .cfg). Enumerate all files in our freshly recreated staging tree,
        // not only files matched by the "*.cfg" filter.
        var stagedFileCount = Directory.EnumerateFiles(
            outputRoot, "*", SearchOption.AllDirectories).Count();
        if (stagedFileCount != fileCount)
            throw new InvalidDataException(
                $"CFG output accounting mismatch: {fileCount} writer-counted files " +
                $"but {stagedFileCount} physical staged CFG files. " +
                "Inspect colliding source actor paths or missing BPatch output.");

        return new CfgWriteResult
        {
            OutputRoot = outputRoot,
            FileCount = stagedFileCount,
            ParameterCount = writtenParameters.Count,
            EditCount = editCount
        };
    }

    private static void ValidateBaseline(CfgWriterBaseline baseline, ParameterCatalog catalog)
    {
        if (baseline.WriterBaselineVersion != 1)
            throw new InvalidDataException($"Unsupported CFG writer baseline version: {baseline.WriterBaselineVersion}");

        if (!baseline.GameVersion.Equals(catalog.Source.GameVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"CFG writer baseline targets game version {baseline.GameVersion}, " +
                $"but the parameter catalog targets {catalog.Source.GameVersion}.");
        }

        var catalogIds = catalog.Parameters.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        var deferred = baseline.DeferredParameterIds.ToHashSet(StringComparer.Ordinal);
        var supported = baseline.EditPoints.Select(p => p.ParameterId).ToHashSet(StringComparer.Ordinal);
        supported.Add("loot.worldWeaponShuffle");
        supported.Add("loot.worldWeaponDensity");
        supported.Add("loot.worldAttachmentShuffle");
        supported.Add("loot.worldDetectors");
        supported.Add("loot.worldF1GrenadeBoxes");
        supported.Add("loot.blueprintShuffle");
        supported.Add("loot.looseWorldLootRemoved");
        supported.UnionWith(StashMixParameterIds);
        supported.UnionWith(StashMedicalParameterIds);
        supported.UnionWith(StashGearParameterIds);
        // An exact manifest-backed special route, not a fictional direct CFG leaf.
        var uniqueShuffleRoute = baseline.Special.LooseUniqueWeaponShuffle;
        if (uniqueShuffleRoute.ParameterId != "loot.looseUniqueWeaponShuffle"
            || uniqueShuffleRoute.ManifestResource !=
                "TrueCustomDifficulty.Data.loose-unique-weapon-shuffle-manifest.csv"
            || uniqueShuffleRoute.ExpectedActorCount != 9)
        {
            throw new InvalidDataException(
                "Unique world weapon shuffle baseline route / manifest contract drifted.");
        }
        supported.Add(uniqueShuffleRoute.ParameterId);
        supported.Add("economy.traderMinBuyCondition");
        supported.UnionWith(StashTierChanceParameterIds);
        supported.UnionWith(baseline.Special.ListMembership.Select(p => p.ParameterId));
        supported.UnionWith(baseline.Special.SetList.Select(p => p.ParameterId));
        supported.UnionWith(baseline.Special.PsyParameterKeys.Keys);
        if (!string.IsNullOrWhiteSpace(baseline.Special.CheapStashProduction.AmmoQuantityParameterId))
            supported.Add(baseline.Special.CheapStashProduction.AmmoQuantityParameterId);
        if (!string.IsNullOrWhiteSpace(baseline.Special.CheapStashProduction.GrenadeChanceParameterId))
            supported.Add(baseline.Special.CheapStashProduction.GrenadeChanceParameterId);
        if (!string.IsNullOrWhiteSpace(baseline.Special.CheapStashProduction.GrenadeQuantityParameterId))
            supported.Add(baseline.Special.CheapStashProduction.GrenadeQuantityParameterId);
        if (!string.IsNullOrWhiteSpace(baseline.Special.MediumStashAmmoProduction.ParameterId))
            supported.Add(baseline.Special.MediumStashAmmoProduction.ParameterId);
        if (!string.IsNullOrWhiteSpace(baseline.Special.ExpensiveStashProduction.AmmoQuantityParameterId))
            supported.Add(baseline.Special.ExpensiveStashProduction.AmmoQuantityParameterId);
        if (!string.IsNullOrWhiteSpace(baseline.Special.ExpensiveStashProduction.CouponChanceParameterId))
            supported.Add(baseline.Special.ExpensiveStashProduction.CouponChanceParameterId);
        if (!string.IsNullOrWhiteSpace(baseline.Special.NpcMedicalLootProduction.ParameterId))
            supported.Add(baseline.Special.NpcMedicalLootProduction.ParameterId);
        if (!string.IsNullOrWhiteSpace(baseline.Special.NpcCorpseAmmoProduction.ParameterId))
            supported.Add(baseline.Special.NpcCorpseAmmoProduction.ParameterId);
        if (!string.IsNullOrWhiteSpace(baseline.Special.DestructibleAmmoProduction.ParameterId))
            supported.Add(baseline.Special.DestructibleAmmoProduction.ParameterId);
        supported.UnionWith(
            baseline.Special.DestructibleAmmoProduction.WeightParameterIds.Values
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(baseline.Special.DestructibleGrenadeProduction.ParameterId))
            supported.Add(baseline.Special.DestructibleGrenadeProduction.ParameterId);
        supported.UnionWith(baseline.Special.CrateSpawnProduction.Categories.Select(x => x.ParameterId));

        var unknown = supported.Concat(deferred).Where(id => !catalogIds.Contains(id)).Distinct(StringComparer.Ordinal).OrderBy(id => id).ToList();
        if (unknown.Count > 0)
            throw new InvalidDataException("CFG writer baseline contains unknown parameter IDs: " + string.Join(", ", unknown));

        var overlap = supported.Intersect(deferred, StringComparer.Ordinal).OrderBy(id => id).ToList();
        if (overlap.Count > 0)
            throw new InvalidDataException("CFG writer baseline marks parameters as both supported and deferred: " + string.Join(", ", overlap));

        var uncovered = catalogIds.Except(supported, StringComparer.Ordinal).Except(deferred, StringComparer.Ordinal).OrderBy(id => id).ToList();
        if (uncovered.Count > 0)
            throw new InvalidDataException("CFG writer baseline has no status for parameter IDs: " + string.Join(", ", uncovered));

        if (supported.Count != baseline.SupportedParameterCount)
        {
            throw new InvalidDataException(
                $"CFG writer baseline supported-parameter count is {baseline.SupportedParameterCount}, but {supported.Count} unique writer mappings were loaded.");
        }
    }

    private static bool IsScopeApplicable(string scope, bool hasDlc1) =>
        !scope.Equals("dlc1", StringComparison.OrdinalIgnoreCase) || hasDlc1;

    private static PatchDocument GetDocument(IDictionary<string, PatchDocument> documents, string file)
    {
        if (!documents.TryGetValue(file, out var document))
        {
            document = new PatchDocument();
            documents[file] = document;
        }
        return document;
    }

    private static string TransformValue(ParameterViewModel parameter, CfgWriterEditPoint point)
    {
        return point.Operation switch
        {
            "set" => FormatSetValue(parameter, point.SourceRaw),
            "toggleSourceOrZero" => ToggleSourceOrZero(parameter, point.SourceRaw),
            "negateSet" => FormatLikeSource(-parameter.CurrentNumber, point.SourceRaw),
            "inverseScale" => InverseScale(parameter, point.SourceRaw),
            "scale" => FormatLikeSource(ParseNumber(point.SourceRaw) * parameter.CurrentNumber, point.SourceRaw),
            "scaleNonZero" => ScaleNonZero(parameter, point.SourceRaw),
            "scaleClamp01" => ScaleClamp01(parameter, point.SourceRaw),
            "setRangeBound" => SetRangeBound(parameter, point),
            "scaleIntegerHalfUp" => ScaleIntegerHalfUp(parameter, point.SourceRaw, preserveZero: false),
            "scaleIntegerHalfUpPreserveZero" => ScaleIntegerHalfUp(parameter, point.SourceRaw, preserveZero: true),
            "setDeltaFromRetainedFraction" => FormatLikeSource(parameter.CurrentNumber - 1.0, point.SourceRaw),
            _ => throw new InvalidDataException(
                $"Unsupported concrete CFG writer operation '{point.Operation}' for {parameter.Id}.")
        };
    }

    private static string SetRangeBound(ParameterViewModel parameter, CfgWriterEditPoint point)
    {
        if (!parameter.IsRange)
            throw new InvalidDataException($"Range-bound writer operation requires a range control: {parameter.Id}.");

        var value = point.Key switch
        {
            "MinDurability" => parameter.CurrentMinNumber,
            "MaxDurability" => parameter.CurrentMaxNumber,
            _ => throw new InvalidDataException(
                $"Unsupported range-bound key '{point.Key}' for {parameter.Id}.")
        };

        return FormatLikeSource(Math.Clamp(value, 0.0, 1.0), point.SourceRaw);
    }

    private static string FormatSetValue(ParameterViewModel parameter, string sourceRaw)
    {
        if (parameter.IsToggle)
            return parameter.CurrentBoolean ? "true" : "false";

        if (parameter.IsSlider)
            return FormatLikeSource(parameter.CurrentNumber, sourceRaw);

        if (parameter.IsChoiceSlider)
            return parameter.CurrentText;

        throw new InvalidDataException($"Unsupported control type '{parameter.Control}' for direct CFG set: {parameter.Id}");
    }

    private static string ToggleSourceOrZero(ParameterViewModel parameter, string sourceRaw)
    {
        if (!parameter.IsToggle)
            throw new InvalidDataException($"toggleSourceOrZero requires a toggle control: {parameter.Id}.");

        return parameter.CurrentBoolean ? sourceRaw : FormatLikeSource(0.0, sourceRaw);
    }

    private static string InverseScale(ParameterViewModel parameter, string sourceRaw)
    {
        if (parameter.CurrentNumber <= 0.0000001)
            throw new InvalidDataException($"Inverse-scale parameter {parameter.Id} must be greater than zero.");

        var source = ParseNumber(sourceRaw);
        return FormatLikeSource(source / parameter.CurrentNumber, sourceRaw);
    }

    private static string ScaleNonZero(ParameterViewModel parameter, string sourceRaw)
    {
        var source = ParseNumber(sourceRaw);
        var value = Math.Abs(source) < 0.0000001 ? source : source * parameter.CurrentNumber;
        return FormatLikeSource(value, sourceRaw);
    }

    private static string ScaleClamp01(ParameterViewModel parameter, string sourceRaw)
    {
        var source = ParseNumber(sourceRaw);
        var value = Math.Clamp(source * parameter.CurrentNumber, 0.0, 1.0);
        return FormatLikeSource(value, sourceRaw);
    }

    private static string ScaleIntegerHalfUp(ParameterViewModel parameter, string sourceRaw, bool preserveZero)
    {
        var source = ParseNumber(sourceRaw);
        if (preserveZero && Math.Abs(source) < 0.0000001)
            return FormatLikeSource(0.0, sourceRaw);

        var scaled = Math.Floor(source * parameter.CurrentNumber + 0.5);
        if (!preserveZero)
            scaled = Math.Max(1.0, scaled);
        else
            scaled = Math.Max(0.0, scaled);
        return FormatLikeSource(scaled, sourceRaw);
    }

    private static double ParseNumber(string sourceRaw)
    {
        var text = sourceRaw.Trim();
        var percent = text.EndsWith("%", StringComparison.Ordinal);
        if (percent)
            text = text[..^1].Trim();
        if (text.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            text = text[..^1].Trim();

        if (text.EndsWith(".", StringComparison.Ordinal))
            text += "0";

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new InvalidDataException($"CFG writer could not parse numeric source value '{sourceRaw}'.");

        return percent ? value / 100.0 : value;
    }

    private static string FormatLikeSource(double value, string sourceRaw)
    {
        var source = sourceRaw.Trim();
        var percent = source.EndsWith("%", StringComparison.Ordinal);
        var floatSuffix = source.EndsWith("f", StringComparison.OrdinalIgnoreCase);
        var dotFloatStyle = floatSuffix && source.Length >= 2 && source[^2] == '.';
        var explicitPositive = source.StartsWith("+", StringComparison.Ordinal);

        if (percent)
        {
            var scaled = value * 100.0;
            var body = FormatNumber(scaled);
            if (explicitPositive && scaled > 0)
                body = "+" + body;
            return body + "%";
        }

        var result = FormatNumber(value);
        if (floatSuffix)
        {
            if (dotFloatStyle && IsWhole(value))
                result = Math.Round(value).ToString(CultureInfo.InvariantCulture) + ".";
            return result + "f";
        }

        if (explicitPositive && value > 0)
            result = "+" + result;

        return result;
    }

    private static bool IsWhole(double value) => Math.Abs(value - Math.Round(value)) < 0.0000001;

    private static bool ValuesEquivalent(string generated, string sourceRaw)
    {
        if (generated.Trim().Equals(sourceRaw.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        if (bool.TryParse(generated.Trim(), out var generatedBool)
            && bool.TryParse(sourceRaw.Trim(), out var sourceBool))
        {
            return generatedBool == sourceBool;
        }

        try
        {
            return Math.Abs(ParseNumber(generated) - ParseNumber(sourceRaw)) < 0.0000001;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static string FormatNumber(double value)
    {
        if (Math.Abs(value) < 0.0000000001)
            value = 0;
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static int WriteAdditionalMechanics(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        string difficultySid,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var members = baseline.Special.ListMembership;
        // An AdditionalMechanics parent toggle can report IsModified because
        // a grouped Master child value changed, even if the parent's own
        // boolean stayed at its vanilla state. Rebuild the full list only
        // when an actual parent checkbox value differs from its baseline.
        var changed = members.Where(m =>
            modifiedById.TryGetValue(m.ParameterId, out var parameter)
            && parameter.IsValueModifiedFromReference()).ToList();
        if (changed.Count == 0)
            return 0;

        const string file = "GameData/DifficultyPrototypes.cfg";
        var document = GetDocument(documents, file);
        var environment = document.GetOrAddNode(difficultySid, new[] { "EnvironmentDifficulty" });

        var desired = members
            .Where(m => allById.TryGetValue(m.ParameterId, out var parameter) && parameter.CurrentBoolean)
            .OrderBy(m => AdditionalMechanicOrder(m.Member))
            .ThenBy(m => m.Member, StringComparer.Ordinal)
            .ToList();

        environment.RemoveChild("AdditionalMechanicsEffects");
        environment.RemoveAssignment("AdditionalMechanicsEffects");

        var edits = 1;
        if (desired.Count == 0)
        {
            environment.SetAssignment("AdditionalMechanicsEffects", string.Empty);
        }
        else
        {
            var list = environment.GetOrAddChild("AdditionalMechanicsEffects", useBpatch: false);
            list.Clear();
            for (var i = 0; i < desired.Count; i++)
            {
                list.SetAssignment($"[{i}]", desired[i].Member);
                edits++;
            }
        }

        foreach (var item in changed)
            writtenParameters.Add(item.ParameterId);

        return edits;
    }

    private static int AdditionalMechanicOrder(string member) => member switch
    {
        "HungerNegativeMechanics" => 0,
        "RadiationNegativeMechanics" => 1,
        "SleepinessNegativeMechanics" => 2,
        "RegenHealthNegativeMechanics" => 3,
        "MasterEffectModifierNegativeMechanics" => 4,
        _ => 100
    };

    private static int WriteCorpseSmartLoot(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        string difficultySid,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var edits = 0;
        foreach (var item in baseline.Special.SetList)
        {
            if (!modifiedById.ContainsKey(item.ParameterId) || !allById.TryGetValue(item.ParameterId, out var parameter))
                continue;
            if (!IsScopeApplicable(item.Scope, hasDlc1))
                continue;

            var document = GetDocument(documents, item.File);
            var parts = item.Path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                throw new InvalidDataException($"Invalid setList writer path: {item.Path}");

            // CorpseSmartLoot is a small optional subtree. Replace only that subtree so
            // an empty selection can reliably remove inherited smart-loot entries.
            var parentPath = parts.Take(parts.Length - 2).ToArray();
            var structName = parts[^2];
            var leaf = parts[^1];
            var parent = document.GetOrAddNode(difficultySid, parentPath);
            parent.RemoveChild(structName);
            parent.RemoveAssignment(structName);

            if (string.IsNullOrWhiteSpace(parameter.CurrentText))
            {
                parent.SetAssignment(structName, string.Empty);
                edits++;
            }
            else
            {
                var subtree = parent.GetOrAddChild(structName, useBpatch: false);
                subtree.Clear();
                subtree.SetAssignment(leaf, parameter.CurrentText);
                edits += 2;
            }

            writtenParameters.Add(item.ParameterId);
        }
        return edits;
    }

    private static int WriteNpcCorpseAmmoProduction(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        string difficultySid,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var config = baseline.Special.NpcCorpseAmmoProduction;
        if (string.IsNullOrWhiteSpace(config.ParameterId)
            || !modifiedById.ContainsKey(config.ParameterId))
            return 0;

        if (!allById.TryGetValue(config.ParameterId, out var ammo)
            || !allById.TryGetValue(config.CorpseChoiceParameterId, out var corpse))
            throw new InvalidDataException("NPC corpse ammo or corpse choice source is missing.");

        // The parameter remains editable/saved even when the extra package
        // is currently Water or None. No unused clone or global CFG override.
        var packages = corpse.CurrentText.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!packages.Contains(config.SourceSid, StringComparer.Ordinal))
        {
            // This multiplier stays saved for a future Medicine + Ammo
            // selection, but has no current CFG destination. Do not count
            // an ignored player setting as an emitted writer parameter.
            return 0;
        }

        if (config.Entries.Count != 43
            || config.Entries.Select(e => (e.RankIndex, e.GroupIndex)).Distinct().Count() != 24
            || config.Entries.Select(e => (e.RankIndex, e.GroupIndex, e.ItemIndex)).Distinct().Count() != 43
            || config.Entries.Any(e => e.RankIndex < 0 || e.RankIndex > 3
                || e.MinCount < 0 || e.MaxCount < e.MinCount || string.IsNullOrWhiteSpace(e.SourceSid)))
            throw new InvalidDataException("NPC_Ammo_Smart count catalog drift: expected 43 source items in 24 groups/4 ranks.");

        if (string.IsNullOrWhiteSpace(config.TargetSid)
            || config.TargetSid.Equals(config.SourceSid, StringComparison.Ordinal)
            || config.BaseTemplateRefKey != config.SourceSid)
            throw new InvalidDataException("NPC corpse ammo clone cannot reference itself or replace vanilla source.");

        var document = GetDocument(documents, config.File);
        var root = document.GetOrAddReferencedRoot(
            config.TargetSid, config.BaseTemplateRefUrl, config.BaseTemplateRefKey);
        root.SetAssignment("SID", config.TargetSid);
        var rankList = root.GetOrAddChild("ItemGenerators", useBpatch: true);
        var edits = 1;
        foreach (var source in config.Entries)
        {
            var entry = rankList.GetOrAddChild($"[{source.RankIndex}]", useBpatch: true)
                .GetOrAddChild("SmartLootParams", useBpatch: true)
                .GetOrAddChild("PrimaryWeaponParams", useBpatch: true)
                .GetOrAddChild($"[{source.GroupIndex}]", useBpatch: true)
                .GetOrAddChild("Items", useBpatch: true)
                .GetOrAddChild($"[{source.ItemIndex}]", useBpatch: true);
            // Retain source selection and all group mechanics by reference.
            // Change only MinCount / MaxCount with true integer-half-up.
            entry.SetAssignment("MinCount", FormatNumber(
                Math.Max(0.0, Math.Floor(source.MinCount * ammo.CurrentNumber + 0.5))));
            entry.SetAssignment("MaxCount", FormatNumber(
                Math.Max(0.0, Math.Floor(source.MaxCount * ammo.CurrentNumber + 0.5))));
            edits += 2;
        }

        var difficultyDocument = GetDocument(documents, "GameData/DifficultyPrototypes.cfg");
        var economy = difficultyDocument.GetOrAddNode(difficultySid, new[] { "EconomyDifficulty" });
        economy.RemoveChild("CorpseSmartLoot");
        economy.RemoveAssignment("CorpseSmartLoot");
        var package = economy.GetOrAddChild("CorpseSmartLoot", useBpatch: false);
        package.Clear();
        package.SetAssignment("GeneralNPC",
            string.Join(", ", packages.Select(sid =>
                sid == config.SourceSid ? config.TargetSid : sid)));
        writtenParameters.Add(config.ParameterId);
        return edits + 2;
    }

    private static int WriteNpcMedicalLootProduction(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var config = baseline.Special.NpcMedicalLootProduction;
        if (string.IsNullOrWhiteSpace(config.ParameterId))
            return 0;

        if (!modifiedById.ContainsKey(config.ParameterId))
            return 0;

        if (!allById.TryGetValue(config.ParameterId, out var parameter))
            throw new InvalidDataException($"NPC medical-loot writer parameter '{config.ParameterId}' was not loaded.");
        if (parameter.CurrentNumber < 0)
            throw new InvalidDataException("Human NPC Medical Loot Frequency cannot be negative.");

        var document = GetDocument(documents, config.File);
        var edits = 0;
        foreach (var pool in config.Pools)
        {
            var root = document.GetOrAddReferencedRoot(
                pool.CustomSid,
                config.BaseTemplateRefUrl,
                config.BaseTemplateRefKey);
            root.SetAssignment("SID", pool.CustomSid);
            edits++;
            edits += WriteNpcMedicalPoolTemplate(root, pool, parameter.CurrentNumber);
        }

        foreach (var redirect in LoadNpcMedicalRedirects(config.RedirectManifestResource))
        {
            if (!IsScopeApplicable(redirect.Scope, hasDlc1))
                continue;

            var redirectDocument = GetDocument(documents, redirect.File);
            redirectDocument.AddAssignment(
                redirect.Prototype,
                redirect.NodePath,
                redirect.Key,
                redirect.TargetValue);
            edits++;
        }

        writtenParameters.Add(config.ParameterId);
        return edits;
    }

    private static int WriteNpcMedicalPoolTemplate(
        PatchNode root,
        CfgWriterNpcMedicalPool pool,
        double frequencyMultiplier)
    {
        var medicalByNode = pool.MedicalEntries.ToDictionary(
            x => (x.CategoryNode, x.EntryNode));
        var itemGenerator = root.GetOrAddChild("ItemGenerator", useBpatch: false);
        var edits = 0;

        foreach (var categoryDefinition in pool.TemplateCategories)
        {
            var category = itemGenerator.GetOrAddChild(categoryDefinition.Node, useBpatch: false);
            foreach (var field in categoryDefinition.Fields)
            {
                category.SetAssignment(field.Key, field.Value);
                edits++;
            }

            var possibleItems = category.GetOrAddChild("PossibleItems", useBpatch: false);
            foreach (var itemDefinition in categoryDefinition.Items)
            {
                var item = possibleItems.GetOrAddChild(itemDefinition.Node, useBpatch: false);
                medicalByNode.TryGetValue((categoryDefinition.Node, itemDefinition.Node), out var medicalEntry);
                foreach (var field in itemDefinition.Fields)
                {
                    var value = field.Value;
                    if (medicalEntry is not null && field.Key == "Chance")
                    {
                        var sourceChance = ParseNumber(medicalEntry.SourceChanceRaw);
                        var scaledChance = Math.Clamp(sourceChance * frequencyMultiplier, 0.0, 1.0);
                        value = FormatLikeSource(scaledChance, medicalEntry.SourceChanceRaw);
                    }

                    item.SetAssignment(field.Key, value);
                    edits++;
                }
            }
        }

        return edits;
    }

    private static IReadOnlyList<NpcMedicalRedirect> LoadNpcMedicalRedirects(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new InvalidDataException("NPC medical-loot redirect manifest resource name is empty.");

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded NPC medical-loot redirect manifest '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);

        var header = reader.ReadLine();
        if (header is null)
            throw new InvalidDataException("NPC medical-loot redirect manifest is empty.");

        var redirects = new List<NpcMedicalRedirect>();
        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 9)
                throw new InvalidDataException($"NPC medical-loot redirect manifest line {lineNumber} has {columns.Length} columns instead of 9.");

            var nodePath = columns[3]
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nodePath.Length == 0)
                throw new InvalidDataException($"NPC medical-loot redirect manifest line {lineNumber} has an empty node path.");

            redirects.Add(new NpcMedicalRedirect(
                columns[0].Trim(),
                columns[1].Trim(),
                columns[2].Trim(),
                nodePath,
                columns[4].Trim(),
                columns[6].Trim()));
        }

        if (redirects.Count != 33)
        {
            throw new InvalidDataException(
                $"NPC medical-loot redirect manifest contains {redirects.Count} assignments; expected audited standard-human scope of 33.");
        }

        return redirects;
    }

    private sealed record NpcMedicalRedirect(
        string Scope,
        string File,
        string Prototype,
        IReadOnlyList<string> NodePath,
        string Key,
        string TargetValue);

    private static int WriteCheapStashProduction(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var config = baseline.Special.CheapStashProduction;
        if (string.IsNullOrWhiteSpace(config.AmmoQuantityParameterId)
            || string.IsNullOrWhiteSpace(config.GrenadeChanceParameterId)
            || string.IsNullOrWhiteSpace(config.GrenadeQuantityParameterId))
        {
            return 0;
        }

        var ammoChanged = modifiedById.ContainsKey(config.AmmoQuantityParameterId);
        var grenadeChanceChanged = modifiedById.ContainsKey(config.GrenadeChanceParameterId);
        var grenadeQuantityChanged = modifiedById.ContainsKey(config.GrenadeQuantityParameterId);
        if (!ammoChanged && !grenadeChanceChanged && !grenadeQuantityChanged
            && !HasStashTuningChange(modifiedById, "Cheap"))
            return 0;

        if (!allById.TryGetValue(config.AmmoQuantityParameterId, out var ammoParameter))
            throw new InvalidDataException($"Cheap-stash writer parameter '{config.AmmoQuantityParameterId}' was not loaded.");
        if (!allById.TryGetValue(config.GrenadeChanceParameterId, out var grenadeChanceParameter))
            throw new InvalidDataException($"Cheap-stash writer parameter '{config.GrenadeChanceParameterId}' was not loaded.");
        if (!allById.TryGetValue(config.GrenadeQuantityParameterId, out var grenadeQuantityParameter))
            throw new InvalidDataException($"Cheap-stash writer parameter '{config.GrenadeQuantityParameterId}' was not loaded.");

        var document = GetDocument(documents, config.File);
        var customSids = new List<string> { config.CustomGeneratorSid };
        customSids.AddRange(config.RankWrappers.Select(x => x.TargetSid));

        var edits = 0;
        foreach (var sid in customSids)
        {
            var root = document.GetOrAddReferencedRoot(
                sid,
                config.BaseTemplateRefUrl,
                config.BaseTemplateRefKey);
            root.SetAssignment("SID", sid);
            edits++;
            edits += WriteCheapStashTemplate(
                root,
                config,
                ammoChanged ? ammoParameter.CurrentNumber : 1.0,
                grenadeChanceChanged ? grenadeChanceParameter.CurrentNumber : ParseNumber(config.GrenadeEntry.SourceChanceRaw),
                grenadeQuantityChanged ? (int)Math.Round(grenadeQuantityParameter.CurrentNumber, MidpointRounding.AwayFromZero) : config.GrenadeEntry.SourceMinCount,
                allById, baseline, modifiedById);
        }

        foreach (var redirect in LoadCheapStashRedirects(config.RedirectManifestResource))
        {
            if (!IsScopeApplicable(redirect.Scope, hasDlc1)
                || !IsApprovedStashTierRedirect(redirect.File, redirect.Prototype, redirect.PhysicalContainerSid))
                continue;

            var redirectDocument = GetDocument(documents, redirect.File);
            redirectDocument.AddAssignment(
                redirect.Prototype,
                redirect.NodePath,
                redirect.Key,
                redirect.TargetValue);
            edits++;
        }

        if (ammoChanged)
            writtenParameters.Add(config.AmmoQuantityParameterId);
        if (grenadeChanceChanged)
            writtenParameters.Add(config.GrenadeChanceParameterId);
        if (grenadeQuantityChanged)
            writtenParameters.Add(config.GrenadeQuantityParameterId);

        MarkModifiedStashTuning(modifiedById, "Cheap", writtenParameters);
        return edits;
    }

    private static int WriteCheapStashTemplate(
        PatchNode root,
        CfgWriterCheapStashProduction config,
        double ammoMultiplier,
        double grenadeChance,
        int grenadeQuantity,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById)
    {

        // Immutable snapshot shared by the actual CFG writer and Stashes UI.
        var effective = new StashEffectiveState(
            allById.Values, modifiedById.Keys,
            "Easy");
        if (ammoMultiplier <= 0)
            throw new InvalidDataException("Cheap Stash Ammo Quantity must be greater than zero.");
        if (grenadeQuantity < 0)
            throw new InvalidDataException("Cheap Stash RGD-5 Quantity cannot be negative.");

        var itemGenerator = root.GetOrAddChild("ItemGenerator", useBpatch: false);
        var edits = 0;

        foreach (var categoryDefinition in config.TemplateCategories)
        {
            var category = itemGenerator.GetOrAddChild(categoryDefinition.Node, useBpatch: false);
            foreach (var field in categoryDefinition.Fields)
            {
                category.SetAssignment(field.Key, field.Value);
                edits++;
            }

            var possibleItems = category.GetOrAddChild("PossibleItems", useBpatch: false);
            foreach (var itemDefinition in categoryDefinition.Items)
            {
                var item = possibleItems.GetOrAddChild(itemDefinition.Node, useBpatch: false);
                foreach (var field in itemDefinition.Fields)
                {
                    var value = effective.TierField(
                        baseline, "Cheap", categoryDefinition, itemDefinition, field.Key);
                    item.SetAssignment(field.Key, value);
                    edits++;
                }
            }
        }

        return edits;
    }

    private static int ScalePositiveInteger(int source, double multiplier)
    {
        var scaled = Math.Floor(source * multiplier + 0.5);
        return Math.Max(1, (int)scaled);
    }

    private static IReadOnlyList<CheapStashRedirect> LoadCheapStashRedirects(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new InvalidDataException("Cheap-stash redirect manifest resource name is empty.");

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded cheap-stash redirect manifest '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);

        var header = reader.ReadLine();
        if (header is null)
            throw new InvalidDataException("Cheap-stash redirect manifest is empty.");

        var redirects = new List<CheapStashRedirect>();
        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 10)
                throw new InvalidDataException($"Cheap-stash redirect manifest line {lineNumber} has {columns.Length} columns instead of 10.");

            var file = columns[1].Trim();
            var nodePath = columns[3]
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nodePath.Length == 0)
                throw new InvalidDataException($"Cheap-stash redirect manifest line {lineNumber} has an empty node path.");

            redirects.Add(new CheapStashRedirect(
                file.StartsWith("DLCGameData/DLC1/", StringComparison.OrdinalIgnoreCase) ? "dlc1" : "baseGame",
                file,
                columns[2].Trim(),
                nodePath,
                columns[4].Trim(),
                columns[6].Trim(),
                columns[7].Trim()));
        }

        if (redirects.Count != 1271)
        {
            throw new InvalidDataException(
                $"Cheap-stash redirect manifest contains {redirects.Count} assignments; expected audited production scope of 1271.");
        }

        return redirects;
    }

    private sealed record CheapStashRedirect(
        string Scope,
        string File,
        string Prototype,
        IReadOnlyList<string> NodePath,
        string Key,
        string TargetValue,
        string PhysicalContainerSid);

    private static int WriteMediumStashAmmoProduction(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var config = baseline.Special.MediumStashAmmoProduction;
        if (string.IsNullOrWhiteSpace(config.ParameterId))
            return 0;

        if (!modifiedById.ContainsKey(config.ParameterId)
            && !HasStashTuningChange(modifiedById, "Medium"))
            return 0;

        if (!allById.TryGetValue(config.ParameterId, out var parameter))
            throw new InvalidDataException($"Medium-stash writer parameter '{config.ParameterId}' was not loaded.");
        if (parameter.CurrentNumber <= 0)
            throw new InvalidDataException("Medium Stash Ammo Quantity must be greater than zero.");

        var document = GetDocument(documents, config.File);
        var customSids = new List<string> { config.CustomGeneratorSid };
        customSids.AddRange(config.RankWrappers.Select(x => x.TargetSid));

        var edits = 0;
        foreach (var sid in customSids)
        {
            var root = document.GetOrAddReferencedRoot(
                sid,
                config.BaseTemplateRefUrl,
                config.BaseTemplateRefKey);
            root.SetAssignment("SID", sid);
            edits++;
            edits += WriteMediumStashAmmoTemplate(root, config,
                modifiedById.ContainsKey(config.ParameterId) ? parameter.CurrentNumber : 1.0,
                allById, baseline, modifiedById);
        }

        foreach (var redirect in LoadMediumStashRedirects(config.RedirectManifestResource))
        {
            if (!IsScopeApplicable(redirect.Scope, hasDlc1)
                || !IsApprovedStashTierRedirect(redirect.File, redirect.Prototype, redirect.PhysicalContainerSid))
                continue;

            var redirectDocument = GetDocument(documents, redirect.File);
            redirectDocument.AddAssignment(
                redirect.Prototype,
                redirect.NodePath,
                redirect.Key,
                redirect.TargetValue);
            edits++;
        }

        if (modifiedById.ContainsKey(config.ParameterId))
            writtenParameters.Add(config.ParameterId);
        MarkModifiedStashTuning(modifiedById, "Medium", writtenParameters);
        return edits;
    }

    private static int WriteMediumStashAmmoTemplate(
        PatchNode root,
        CfgWriterMediumStashAmmoProduction config,
        double ammoMultiplier,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById)
    {

        // Immutable snapshot shared by the actual CFG writer and Stashes UI.
        var effective = new StashEffectiveState(
            allById.Values, modifiedById.Keys,
            "Easy");
        var itemGenerator = root.GetOrAddChild("ItemGenerator", useBpatch: false);
        var edits = 0;

        foreach (var categoryDefinition in config.TemplateCategories)
        {
            var category = itemGenerator.GetOrAddChild(categoryDefinition.Node, useBpatch: false);
            foreach (var field in categoryDefinition.Fields)
            {
                category.SetAssignment(field.Key, field.Value);
                edits++;
            }

            var possibleItems = category.GetOrAddChild("PossibleItems", useBpatch: false);
            foreach (var itemDefinition in categoryDefinition.Items)
            {
                var item = possibleItems.GetOrAddChild(itemDefinition.Node, useBpatch: false);

                foreach (var field in itemDefinition.Fields)
                {
                    var value = effective.TierField(
                        baseline, "Medium", categoryDefinition, itemDefinition, field.Key);
                    item.SetAssignment(field.Key, value);
                    edits++;
                }
            }
        }

        return edits;
    }

    private static IReadOnlyList<MediumStashRedirect> LoadMediumStashRedirects(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new InvalidDataException("Medium-stash redirect manifest resource name is empty.");

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded medium-stash redirect manifest '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);

        var header = reader.ReadLine();
        if (header is null)
            throw new InvalidDataException("Medium-stash redirect manifest is empty.");

        var redirects = new List<MediumStashRedirect>();
        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 10)
                throw new InvalidDataException($"Medium-stash redirect manifest line {lineNumber} has {columns.Length} columns instead of 10.");

            var file = columns[1].Trim();
            var nodePath = columns[3]
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nodePath.Length == 0)
                throw new InvalidDataException($"Medium-stash redirect manifest line {lineNumber} has an empty node path.");

            redirects.Add(new MediumStashRedirect(
                file.StartsWith("DLCGameData/DLC1/", StringComparison.OrdinalIgnoreCase) ? "dlc1" : "baseGame",
                file,
                columns[2].Trim(),
                nodePath,
                columns[4].Trim(),
                columns[6].Trim(),
                columns[7].Trim()));
        }

        if (redirects.Count != 450)
        {
            throw new InvalidDataException(
                $"Medium-stash redirect manifest contains {redirects.Count} assignments; expected audited production scope of 450.");
        }

        return redirects;
    }

    private sealed record MediumStashRedirect(
        string Scope,
        string File,
        string Prototype,
        IReadOnlyList<string> NodePath,
        string Key,
        string TargetValue,
        string PhysicalContainerSid);

    private static int WriteExpensiveStashProduction(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        string difficultySid,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var config = baseline.Special.ExpensiveStashProduction;
        if (string.IsNullOrWhiteSpace(config.AmmoQuantityParameterId))
            return 0;

        // Existing coupon-only builds keep using the audited vanilla-wrapper edit points.
        // TCD-owned Expensive roots are needed only when ammo quantity redirects production stash paths.
        if (!modifiedById.ContainsKey(config.AmmoQuantityParameterId)
            && !HasStashTuningChange(modifiedById, "Expensive"))
            return 0;

        if (!allById.TryGetValue(config.AmmoQuantityParameterId, out var ammoParameter))
            throw new InvalidDataException($"Expensive-stash writer parameter '{config.AmmoQuantityParameterId}' was not loaded.");
        if (ammoParameter.CurrentNumber <= 0)
            throw new InvalidDataException("Expensive Stash Ammo Quantity must be greater than zero.");

        ParameterViewModel? couponParameter = null;
        var couponChanged = !string.IsNullOrWhiteSpace(config.CouponChanceParameterId)
            && modifiedById.ContainsKey(config.CouponChanceParameterId);
        if (couponChanged && !allById.TryGetValue(config.CouponChanceParameterId, out couponParameter))
            throw new InvalidDataException($"Expensive-stash coupon writer parameter '{config.CouponChanceParameterId}' was not loaded.");

        var document = GetDocument(documents, config.File);
        var customSids = new List<string> { config.CustomGeneratorSid };
        customSids.AddRange(config.RankWrappers.Select(x => x.TargetSid));

        var edits = 0;
        foreach (var sid in customSids)
        {
            var root = document.GetOrAddReferencedRoot(
                sid,
                config.BaseTemplateRefUrl,
                config.BaseTemplateRefKey);
            root.SetAssignment("SID", sid);
            edits++;
            edits += WriteExpensiveStashTemplate(
                root,
                config,
                modifiedById.ContainsKey(config.AmmoQuantityParameterId)
                    ? ammoParameter.CurrentNumber : 1.0,
                couponChanged ? couponParameter?.CurrentNumber : null,
                difficultySid,
                allById, baseline, modifiedById);
        }

        foreach (var redirect in LoadExpensiveStashRedirects(config.RedirectManifestResource))
        {
            if (!IsScopeApplicable(redirect.Scope, hasDlc1)
                || !IsApprovedStashTierRedirect(redirect.File, redirect.Prototype, redirect.PhysicalContainerSid))
                continue;

            var redirectDocument = GetDocument(documents, redirect.File);
            redirectDocument.AddAssignment(
                redirect.Prototype,
                redirect.NodePath,
                redirect.Key,
                redirect.TargetValue);
            edits++;
        }

        if (modifiedById.ContainsKey(config.AmmoQuantityParameterId))
            writtenParameters.Add(config.AmmoQuantityParameterId);
        MarkModifiedStashTuning(modifiedById, "Expensive", writtenParameters);
        return edits;
    }

    private static int WriteExpensiveStashTemplate(
        PatchNode root,
        CfgWriterExpensiveStashProduction config,
        double ammoMultiplier,
        double? couponChance,
        string difficultySid,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById)
    {

        // Immutable snapshot shared by the actual CFG writer and Stashes UI.
        var effective = new StashEffectiveState(
            allById.Values, modifiedById.Keys,
            difficultySid);
        var itemGenerator = root.GetOrAddChild("ItemGenerator", useBpatch: false);
        var edits = 0;

        foreach (var categoryDefinition in config.TemplateCategories)
        {
            var category = itemGenerator.GetOrAddChild(categoryDefinition.Node, useBpatch: false);
            foreach (var field in categoryDefinition.Fields)
            {
                category.SetAssignment(field.Key, field.Value);
                edits++;
            }

            var possibleItems = category.GetOrAddChild("PossibleItems", useBpatch: false);
            foreach (var itemDefinition in categoryDefinition.Items)
            {
                var item = possibleItems.GetOrAddChild(itemDefinition.Node, useBpatch: false);

                foreach (var field in itemDefinition.Fields)
                {
                    var value = effective.TierField(
                        baseline, "Expensive", categoryDefinition, itemDefinition, field.Key);
                    item.SetAssignment(field.Key, value);
                    edits++;
                }
            }
        }

        return edits;
    }

    private static IReadOnlyList<ExpensiveStashRedirect> LoadExpensiveStashRedirects(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new InvalidDataException("Expensive-stash redirect manifest resource name is empty.");

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded expensive-stash redirect manifest '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);

        var header = reader.ReadLine();
        if (header is null)
            throw new InvalidDataException("Expensive-stash redirect manifest is empty.");

        var redirects = new List<ExpensiveStashRedirect>();
        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 10)
                throw new InvalidDataException($"Expensive-stash redirect manifest line {lineNumber} has {columns.Length} columns instead of 10.");

            var file = columns[1].Trim();
            var nodePath = columns[3]
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nodePath.Length == 0)
                throw new InvalidDataException($"Expensive-stash redirect manifest line {lineNumber} has an empty node path.");

            redirects.Add(new ExpensiveStashRedirect(
                file.StartsWith("DLCGameData/DLC1/", StringComparison.OrdinalIgnoreCase) ? "dlc1" : "baseGame",
                file,
                columns[2].Trim(),
                nodePath,
                columns[4].Trim(),
                columns[6].Trim(),
                columns[7].Trim()));
        }

        if (redirects.Count != 305)
        {
            throw new InvalidDataException(
                $"Expensive-stash redirect manifest contains {redirects.Count} assignments; expected audited production scope of 305.");
        }

        return redirects;
    }

    private sealed record ExpensiveStashRedirect(
        string Scope,
        string File,
        string Prototype,
        IReadOnlyList<string> NodePath,
        string Key,
        string TargetValue,
        string PhysicalContainerSid);

    private static int WriteDestructibleAmmoProduction(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var config = baseline.Special.DestructibleAmmoProduction;
        if (string.IsNullOrWhiteSpace(config.ParameterId))
            return 0;

        var quantityChanged = modifiedById.ContainsKey(config.ParameterId);
        var weightIds = config.WeightParameterIds.Values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var weightsChanged = weightIds.Any(modifiedById.ContainsKey);

        if (!quantityChanged && !weightsChanged)
            return 0;

        if (!allById.TryGetValue(config.ParameterId, out var quantityParameter))
            throw new InvalidDataException($"Ammo Box Amount writer parameter '{config.ParameterId}' was not loaded.");
        if (quantityParameter.CurrentNumber <= 0)
            throw new InvalidDataException("Ammo from Breakable Boxes must be greater than zero.");

        var weightParameters = new Dictionary<string, ParameterViewModel>(StringComparer.Ordinal);
        foreach (var pair in config.WeightParameterIds)
        {
            if (!allById.TryGetValue(pair.Value, out var parameter) || !parameter.IsSlider)
                throw new InvalidDataException($"Ammo Box Distribution writer parameter '{pair.Value}' was not loaded.");
            if (parameter.CurrentNumber < 0)
                throw new InvalidDataException($"Ammo Box Distribution weight '{pair.Value}' cannot be negative.");
            weightParameters[pair.Key] = parameter;
        }

        static double ParseWeight(string raw)
        {
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException($"Invalid authored ammo Weight '{raw}'.");
            return value;
        }

        foreach (var pool in config.Pools)
        {
            var effectiveWeights = pool.Items.Select(item =>
            {
                if (!item.Fields.TryGetValue("ItemPrototypeSID", out var itemSid)
                    || !item.Fields.TryGetValue("Weight", out var sourceWeightRaw)
                    || !weightParameters.TryGetValue(itemSid, out var parameter))
                {
                    throw new InvalidDataException(
                        $"Ammo Box Distribution mapping is incomplete for pool '{pool.SourceSid}'.");
                }

                return ParseWeight(sourceWeightRaw) * parameter.CurrentNumber;
            }).ToArray();

            if (effectiveWeights.All(x => x <= 0))
            {
                throw new InvalidDataException(
                    "Ammo Box Distribution cannot set every caliber in the same ammo box pool to 0x.");
            }
        }

        var document = GetDocument(documents, config.File);
        var edits = 0;

        foreach (var pool in config.Pools)
        {
            var root = document.GetOrAddReferencedRoot(
                pool.TargetSid,
                config.BaseTemplateRefUrl,
                config.BaseTemplateRefKey);
            root.SetAssignment("SID", pool.TargetSid);
            edits++;

            var itemGenerator = root.GetOrAddChild("ItemGenerator", useBpatch: false);
            var category = itemGenerator.GetOrAddChild(pool.CategoryNode, useBpatch: false);
            foreach (var field in pool.CategoryFields)
            {
                category.SetAssignment(field.Key, field.Value);
                edits++;
            }

            var possibleItems = category.GetOrAddChild("PossibleItems", useBpatch: false);
            foreach (var itemDefinition in pool.Items)
            {
                var item = possibleItems.GetOrAddChild(itemDefinition.Node, useBpatch: false);
                if (!itemDefinition.Fields.TryGetValue("ItemPrototypeSID", out var itemSid))
                    throw new InvalidDataException($"Ammo pool '{pool.SourceSid}' has an item without ItemPrototypeSID.");
                if (!weightParameters.TryGetValue(itemSid, out var weightParameter))
                    throw new InvalidDataException($"Ammo pool '{pool.SourceSid}' has no weight mapping for '{itemSid}'.");

                foreach (var field in itemDefinition.Fields)
                {
                    var value = field.Value;
                    if (field.Key == "MinCount")
                    {
                        value = ScalePositiveInteger(
                                itemDefinition.SourceMinCount,
                                quantityParameter.CurrentNumber)
                            .ToString(CultureInfo.InvariantCulture);
                    }
                    else if (field.Key == "MaxCount")
                    {
                        value = ScalePositiveInteger(
                                itemDefinition.SourceMaxCount,
                                quantityParameter.CurrentNumber)
                            .ToString(CultureInfo.InvariantCulture);
                    }
                    else if (field.Key == "Weight")
                    {
                        var effectiveWeight = ParseWeight(field.Value) * weightParameter.CurrentNumber;
                        value = effectiveWeight.ToString("0.###", CultureInfo.InvariantCulture);
                    }

                    item.SetAssignment(field.Key, value);
                    edits++;
                }
            }
        }

        foreach (var redirect in LoadDestructibleQuantityRedirects(
                     config.RedirectManifestResource,
                     expectedRedirectCount: 1235,
                     manifestLabel: "destructible-ammo"))
        {
            if (!IsScopeApplicable(redirect.Scope, hasDlc1))
                continue;

            var redirectDocument = GetDocument(documents, redirect.File);
            redirectDocument.AddAssignment(
                redirect.Prototype,
                redirect.NodePath,
                redirect.Key,
                redirect.TargetValue);
            edits++;
        }

        if (quantityChanged)
            writtenParameters.Add(config.ParameterId);
        foreach (var parameterId in weightIds.Where(modifiedById.ContainsKey))
            writtenParameters.Add(parameterId);

        return edits;
    }

    private static int WriteDestructibleGrenadeProduction(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        return WriteDestructibleQuantityProduction(
            baseline.Special.DestructibleGrenadeProduction,
            modifiedById,
            allById,
            hasDlc1,
            documents,
            writtenParameters,
            expectedRedirectCount: 182,
            controlLabel: "Grenade Box Quantity",
            manifestLabel: "destructible-grenade");
    }

    private static int WriteDestructibleQuantityProduction(
        CfgWriterDestructibleAmmoProduction config,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters,
        int expectedRedirectCount,
        string controlLabel,
        string manifestLabel)
    {
        if (string.IsNullOrWhiteSpace(config.ParameterId)
            || !modifiedById.ContainsKey(config.ParameterId))
        {
            return 0;
        }

        if (!allById.TryGetValue(config.ParameterId, out var parameter))
            throw new InvalidDataException($"{controlLabel} writer parameter '{config.ParameterId}' was not loaded.");

        if (parameter.CurrentNumber <= 0)
            throw new InvalidDataException($"{controlLabel} must be greater than zero.");

        var document = GetDocument(documents, config.File);
        var edits = 0;

        foreach (var pool in config.Pools)
        {
            var root = document.GetOrAddReferencedRoot(
                pool.TargetSid,
                config.BaseTemplateRefUrl,
                config.BaseTemplateRefKey);
            root.SetAssignment("SID", pool.TargetSid);
            edits++;

            var itemGenerator = root.GetOrAddChild("ItemGenerator", useBpatch: false);
            var category = itemGenerator.GetOrAddChild(pool.CategoryNode, useBpatch: false);
            foreach (var field in pool.CategoryFields)
            {
                category.SetAssignment(field.Key, field.Value);
                edits++;
            }

            var possibleItems = category.GetOrAddChild("PossibleItems", useBpatch: false);
            foreach (var itemDefinition in pool.Items)
            {
                var item = possibleItems.GetOrAddChild(itemDefinition.Node, useBpatch: false);
                foreach (var field in itemDefinition.Fields)
                {
                    var value = field.Value;
                    if (field.Key == "MinCount")
                    {
                        value = ScalePositiveInteger(
                                itemDefinition.SourceMinCount,
                                parameter.CurrentNumber)
                            .ToString(CultureInfo.InvariantCulture);
                    }
                    else if (field.Key == "MaxCount")
                    {
                        value = ScalePositiveInteger(
                                itemDefinition.SourceMaxCount,
                                parameter.CurrentNumber)
                            .ToString(CultureInfo.InvariantCulture);
                    }

                    item.SetAssignment(field.Key, value);
                    edits++;
                }
            }
        }

        foreach (var redirect in LoadDestructibleQuantityRedirects(
                     config.RedirectManifestResource,
                     expectedRedirectCount,
                     manifestLabel))
        {
            if (!IsScopeApplicable(redirect.Scope, hasDlc1))
                continue;

            var redirectDocument = GetDocument(documents, redirect.File);
            redirectDocument.AddAssignment(
                redirect.Prototype,
                redirect.NodePath,
                redirect.Key,
                redirect.TargetValue);
            edits++;
        }

        writtenParameters.Add(config.ParameterId);
        return edits;
    }

    private static IReadOnlyList<DestructibleAmmoRedirect> LoadDestructibleQuantityRedirects(
        string resourceName,
        int expectedRedirectCount,
        string manifestLabel)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new InvalidDataException($"{manifestLabel} redirect manifest resource name is empty.");

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded {manifestLabel} redirect manifest '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);

        var header = reader.ReadLine();
        if (header is null)
            throw new InvalidDataException($"{manifestLabel} redirect manifest is empty.");

        var redirects = new List<DestructibleAmmoRedirect>();
        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 13)
            {
                throw new InvalidDataException(
                    $"{manifestLabel} redirect manifest line {lineNumber} has {columns.Length} columns instead of 13.");
            }

            var file = columns[1].Trim();
            var nodePath = columns[3]
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nodePath.Length == 0)
                throw new InvalidDataException($"{manifestLabel} redirect manifest line {lineNumber} has an empty node path.");

            redirects.Add(new DestructibleAmmoRedirect(
                file.StartsWith("DLCGameData/DLC1/", StringComparison.OrdinalIgnoreCase) ? "dlc1" : "baseGame",
                file,
                columns[2].Trim(),
                nodePath,
                columns[4].Trim(),
                columns[6].Trim()));
        }

        if (redirects.Count != expectedRedirectCount)
        {
            throw new InvalidDataException(
                $"{manifestLabel} redirect manifest contains {redirects.Count} assignments; expected audited production scope of {expectedRedirectCount}.");
        }

        return redirects;
    }

    private sealed record DestructibleAmmoRedirect(
        string Scope,
        string File,
        string Prototype,
        IReadOnlyList<string> NodePath,
        string Key,
        string TargetValue);

    private static int WritePsyOverrides(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        string difficultySid,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        var psyIds = baseline.Special.PsyParameterKeys.Keys.ToHashSet(StringComparer.Ordinal);
        // PsyPhantomPressure uses the Spawn-Cycle Delay parameter as a visual
        // aggregate anchor. A changed Phantom Count makes that anchor's
        // IsModified true for the UI, even if its own delay remains vanilla.
        // Select only actual source-value changes for emitted PSY fields.
        var changed = psyIds
            .Where(id => modifiedById.TryGetValue(id, out var parameter)
                && parameter.IsValueModifiedFromReference())
            .ToList();
        if (changed.Count == 0)
            return 0;

        var document = GetDocument(documents, "GameData/DifficultyPrototypes.cfg");
        var npc = document.GetOrAddNode(difficultySid, new[] { "NPCCombatDifficulty" });
        var edits = 0;

        if (difficultySid.Equals("Stalker", StringComparison.OrdinalIgnoreCase))
        {
            // Master already owns the complete override array. Patch only the fields
            // whose player-facing controls changed.
            var container = npc.GetOrAddChild("PsyPhantomNPCOverrides", useBpatch: true);
            foreach (var entry in baseline.Special.PsyOverrides.Values.OrderBy(x => CfgNaturalComparer.IndexValue(x.Index)))
            {
                var node = container.GetOrAddChild(entry.Index, useBpatch: true);
                node.SetAssignment("OverrideEffectPrototypeSID", entry.EffectSid);
                foreach (var id in changed)
                {
                    var key = baseline.Special.PsyParameterKeys[id];
                    node.SetAssignment(key, FormatParameterScalar(allById[id]));
                    edits++;
                }
            }
        }
        else
        {
            // Lower vanilla difficulties do not own a complete array. Create a full
            // localized subtree using the current values of all four controls.
            npc.RemoveChild("PsyPhantomNPCOverrides");
            npc.RemoveAssignment("PsyPhantomNPCOverrides");
            var container = npc.GetOrAddChild("PsyPhantomNPCOverrides", useBpatch: false);
            container.Clear();

            foreach (var entry in baseline.Special.PsyOverrides.Values.OrderBy(x => CfgNaturalComparer.IndexValue(x.Index)))
            {
                var node = container.GetOrAddChild(entry.Index, useBpatch: false);
                node.SetAssignment("OverrideEffectPrototypeSID", entry.EffectSid);
                foreach (var pair in baseline.Special.PsyParameterKeys.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    node.SetAssignment(pair.Value, FormatParameterScalar(allById[pair.Key]));
                    edits++;
                }
            }
        }

        foreach (var id in changed)
            writtenParameters.Add(id);

        return edits;
    }

    private static (int EditCount, int FileCount) WriteWorldWeaponDistribution(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        string outputRoot,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        const string toggleId = "loot.worldWeaponShuffle";
        const string densityId = "loot.worldWeaponDensity";

        var toggleTouched = modifiedById.ContainsKey(toggleId);
        var densityTouched = modifiedById.ContainsKey(densityId);
        if (!toggleTouched && !densityTouched)
            return (0, 0);

        if (toggleTouched) writtenParameters.Add(toggleId);
        if (densityTouched) writtenParameters.Add(densityId);

        var enabled = allById.TryGetValue(toggleId, out var toggleParameter)
                      && toggleParameter.IsToggle
                      && toggleParameter.CurrentBoolean;
        if (!enabled)
            return (0, 0);

        var density = allById.TryGetValue(densityId, out var densityParameter)
            ? Math.Clamp(densityParameter.CurrentNumber, 0.0, 1.0)
            : 1.0;

        var pool = LoadWorldWeaponPool()
            .OrderBy(x => x.ItemSid, StringComparer.Ordinal)
            .ToList();
        if (pool.Count != 36 || pool.Select(x => x.ItemSid).Distinct(StringComparer.Ordinal).Count() != 36)
            throw new InvalidDataException($"World weapon regular pool drifted: expected 36 unique models, found {pool.Count}.");

        var direct = LoadWorldWeaponDirectSlots()
            .Where(x => IsScopeApplicable(x.Scope, hasDlc1))
            .ToList();
        var expectedDirect = hasDlc1 ? 362 : 282;
        if (direct.Count != expectedDirect)
            throw new InvalidDataException(
                $"World weapon direct-location surface drifted: expected {expectedDirect}, found {direct.Count}.");

        var generator = LoadWorldWeaponSlots().ToList();
        if (generator.Count != 14)
            throw new InvalidDataException(
                $"World weapon generator-location surface drifted: expected 14, found {generator.Count}.");

        var candidates = new List<WorldWeaponLocation>();
        candidates.AddRange(direct.Select(x => new WorldWeaponLocation(
            x.ItemSid, x.Family, x.Tier, true, x.Scope, x.SourceFile, x.LevelName, x.ActorSid,
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty, string.Empty)));
        candidates.AddRange(generator.Select(x => new WorldWeaponLocation(
            x.ItemSid, x.Family, x.Tier, false, "baseGame", "GameData/ItemGeneratorPrototypes.cfg",
            string.Empty, string.Empty, x.GeneratorSid, x.NodePath, x.Chance, x.Weight,
            x.MinDurability, x.MaxDurability, x.AmmoMinCount, x.AmmoMaxCount, x.MinCount, x.MaxCount)));

        if (candidates.Count != (hasDlc1 ? 376 : 296))
            throw new InvalidDataException(
                $"World weapon safe-location count drifted: {candidates.Count}; expected {(hasDlc1 ? 376 : 296)}.");

        var minimumCount = pool.Count;
        var targetCount = Math.Clamp(
            minimumCount + (int)Math.Round(
                (candidates.Count - minimumCount) * density,
                MidpointRounding.AwayFromZero),
            minimumCount,
            candidates.Count);

        // Compatibility contract:
        // - pistols only use pistol slots;
        // - every other regular weapon uses a primary slot;
        // - assigned weapon tier may differ from the authored slot tier by at most one step.
        static string SlotType(string family) =>
            string.Equals(family, "Pistol", StringComparison.Ordinal) ? "Pistol" : "Primary";

        static int TierNumber(string tier) => tier switch
        {
            "T1" => 1,
            "T2" => 2,
            "T3" => 3,
            "T4" => 4,
            _ => throw new InvalidDataException($"Unknown world weapon progression tier: {tier}")
        };

        static bool Compatible(WorldWeaponPoolItem weapon, WorldWeaponLocation location) =>
            string.Equals(SlotType(weapon.Family), SlotType(location.Family), StringComparison.Ordinal)
            && Math.Abs(TierNumber(weapon.Tier) - TierNumber(location.Tier)) <= 1;

        // The audited topology proves these global guarantees for same-slot-type / +/-1-tier assignment.
        // Base can guarantee up to 6 copies of every model; DLC1 can guarantee up to 8.
        var maximumGuaranteedCopies = hasDlc1 ? 8 : 6;
        var guaranteedCopies = Math.Clamp(targetCount / pool.Count, 1, maximumGuaranteedCopies);

        // Group concrete locations into compatibility buckets.
        var bucketLocations = candidates
            .GroupBy(x => (Type: SlotType(x.Family), Tier: TierNumber(x.Tier)))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.IsDirect ? x.ActorSid : x.GeneratorSid + "|" + x.NodePath, StringComparer.Ordinal).ToList());

        foreach (var list in bucketLocations.Values)
            SelectRandomPrefix(list, list.Count);

        // Exact max-flow over weapon demand -> compatible slot buckets.
        // This guarantees `guaranteedCopies` for all 36 models whenever the audit contract says it is feasible.
        var weaponCount = pool.Count;
        var buckets = bucketLocations.Keys
            .OrderBy(x => x.Type, StringComparer.Ordinal)
            .ThenBy(x => x.Tier)
            .ToList();

        var source = 0;
        var weaponStart = 1;
        var bucketStart = weaponStart + weaponCount;
        var sink = bucketStart + buckets.Count;
        var nodeCount = sink + 1;

        var capacity = new int[nodeCount, nodeCount];
        var originalCapacity = new int[nodeCount, nodeCount];

        void AddEdge(int from, int to, int cap)
        {
            capacity[from, to] += cap;
            originalCapacity[from, to] += cap;
        }

        for (var wi = 0; wi < weaponCount; wi++)
        {
            AddEdge(source, weaponStart + wi, guaranteedCopies);
            for (var bi = 0; bi < buckets.Count; bi++)
            {
                var bucket = buckets[bi];
                var probe = bucketLocations[bucket][0];
                if (Compatible(pool[wi], probe))
                    AddEdge(weaponStart + wi, bucketStart + bi, guaranteedCopies);
            }
        }

        for (var bi = 0; bi < buckets.Count; bi++)
            AddEdge(bucketStart + bi, sink, bucketLocations[buckets[bi]].Count);

        var requiredFlow = weaponCount * guaranteedCopies;
        var flow = 0;
        while (true)
        {
            var parent = Enumerable.Repeat(-1, nodeCount).ToArray();
            parent[source] = source;
            var queue = new Queue<int>();
            queue.Enqueue(source);

            while (queue.Count > 0 && parent[sink] < 0)
            {
                var u = queue.Dequeue();
                for (var v = 0; v < nodeCount; v++)
                {
                    if (parent[v] >= 0 || capacity[u, v] <= 0)
                        continue;
                    parent[v] = u;
                    queue.Enqueue(v);
                }
            }

            if (parent[sink] < 0)
                break;

            var aug = int.MaxValue;
            for (var v = sink; v != source; v = parent[v])
                aug = Math.Min(aug, capacity[parent[v], v]);

            for (var v = sink; v != source; v = parent[v])
            {
                var u = parent[v];
                capacity[u, v] -= aug;
                capacity[v, u] += aug;
            }

            flow += aug;
        }

        if (flow != requiredFlow)
            throw new InvalidDataException(
                $"World weapon compatibility allocation failed: assigned {flow}/{requiredFlow} mandatory copies.");

        var assignments = new Dictionary<WorldWeaponLocation, WorldWeaponPoolItem>();
        var bucketOffsets = buckets.ToDictionary(x => x, _ => 0);

        for (var wi = 0; wi < weaponCount; wi++)
        {
            for (var bi = 0; bi < buckets.Count; bi++)
            {
                var from = weaponStart + wi;
                var to = bucketStart + bi;
                var used = originalCapacity[from, to] - capacity[from, to];
                if (used <= 0)
                    continue;

                var bucket = buckets[bi];
                var locations = bucketLocations[bucket];
                for (var n = 0; n < used; n++)
                {
                    var index = bucketOffsets[bucket]++;
                    if (index >= locations.Count)
                        throw new InvalidDataException("World weapon bucket allocation overflow.");
                    assignments[locations[index]] = pool[wi];
                }
            }
        }

        if (assignments.Count != requiredFlow)
            throw new InvalidDataException(
                $"World weapon mandatory assignment count drifted: {assignments.Count}; expected {requiredFlow}.");

        // Density fill: retain additional locations and give each one a random compatible model.
        var remaining = candidates.Where(x => !assignments.ContainsKey(x)).ToList();
        var extraCount = targetCount - assignments.Count;
        SelectRandomPrefix(remaining, extraCount);

        foreach (var location in remaining.Take(extraCount))
        {
            var compatibleWeapons = pool.Where(x => Compatible(x, location)).ToList();
            if (compatibleWeapons.Count == 0)
                throw new InvalidDataException(
                    $"No compatible regular weapon exists for {location.Family}/{location.Tier} world slot.");
            assignments[location] = compatibleWeapons[RandomNumberGenerator.GetInt32(compatibleWeapons.Count)];
        }

        if (assignments.Count != targetCount)
            throw new InvalidDataException(
                $"World weapon density assigned {assignments.Count} locations; expected {targetCount}.");

        var perWeapon = assignments.Values
            .GroupBy(x => x.ItemSid, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var underGuaranteed = pool
            .Where(x => !perWeapon.TryGetValue(x.ItemSid, out var count) || count < guaranteedCopies)
            .Select(x => x.ItemSid)
            .ToList();
        if (underGuaranteed.Count > 0)
            throw new InvalidDataException(
                $"World weapon minimum-copy guarantee failed ({guaranteedCopies}x): {string.Join(", ", underGuaranteed)}");

        var edits = 0;
        var files = 0;

        foreach (var row in direct)
        {
            var location = candidates.Single(x => x.IsDirect
                                                  && x.Scope == row.Scope
                                                  && x.ActorSid == row.ActorSid);
            if (!assignments.TryGetValue(location, out var assigned))
            {
                var outputPath = GetSpawnActorRemovalOutputPath(outputRoot, row.SourceFile, row.LevelName, row.ActorSid);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllText(
                    outputPath,
                    $"// Generated by True Custom Difficulty.{Environment.NewLine}" +
                    $"// World Weapon Density: removed eligible regular weapon location.{Environment.NewLine}" +
                    $"{row.ActorSid} : removenode{Environment.NewLine}",
                    new UTF8Encoding(false));
                edits++;
                files++;
                continue;
            }

            if (string.Equals(assigned.ItemSid, row.ItemSid, StringComparison.Ordinal))
                continue; // The selected location already contains the assigned weapon.

            var replacementPath = GetSpawnActorRemovalOutputPath(outputRoot, row.SourceFile, row.LevelName, row.ActorSid);
            Directory.CreateDirectory(Path.GetDirectoryName(replacementPath)!);
            File.WriteAllText(
                replacementPath,
                $"// Generated by True Custom Difficulty.{Environment.NewLine}" +
                $"// Compatible World Weapon Shuffle: {row.ItemSid} -> {assigned.ItemSid}.{Environment.NewLine}" +
                $"{row.ActorSid} : struct.begin {{bpatch}}{Environment.NewLine}" +
                $"   SpawnedPrototypeSID = {assigned.ItemSid}{Environment.NewLine}" +
                $"   ItemSID = {assigned.ItemSid}{Environment.NewLine}" +
                $"struct.end{Environment.NewLine}",
                new UTF8Encoding(false));
            edits += 2;
            files++;
        }

        var itemGeneratorDocument = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
        foreach (var slot in generator)
        {
            var location = candidates.Single(x => !x.IsDirect
                                                  && x.GeneratorSid == slot.GeneratorSid
                                                  && x.NodePath == slot.NodePath);
            var path = slot.NodePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (path.Length < 2)
                throw new InvalidDataException(
                    $"Invalid world weapon generator node path for {slot.GeneratorSid}: {slot.NodePath}");

            if (!assignments.TryGetValue(location, out var assigned))
            {
                itemGeneratorDocument.AddRemovalNode(slot.GeneratorSid, path[..^1], path[^1]);
                edits++;
                continue;
            }

            if (string.Equals(assigned.ItemSid, slot.ItemSid, StringComparison.Ordinal))
                continue;

            // Preserve the physical original item node and ALL of its
            // authored nested Upgrades / Attaches structures. These nested
            // blocks may themselves contain Chance, MinCount and MaxCount;
            // copying those values as if they were direct item properties
            // corrupts the vanilla reward shape. A minimal indexed {bpatch}
            // changes only the selected weapon SID, leaving other fields
            // (and nested upgrade/attachment rewards) untouched.
            itemGeneratorDocument.AddAssignment(
                slot.GeneratorSid, path, "ItemPrototypeSID", assigned.ItemSid);
            edits++;
        }

        return (edits, files);
    }


    private static int WriteWorldAttachmentShuffle(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        const string parameterId = "loot.worldAttachmentShuffle";
        if (!modifiedById.ContainsKey(parameterId))
            return 0;

        if (!allById.TryGetValue(parameterId, out var parameter) || !parameter.IsToggle)
            throw new InvalidDataException($"Missing toggle parameter {parameterId}.");

        writtenParameters.Add(parameterId);
        if (!parameter.CurrentBoolean)
            return 0; // Vanilla: no attachment placement patch.

        var pool = LoadWorldAttachmentPool().ToDictionary(x => x.ItemSid, StringComparer.Ordinal);
        if (pool.Count != 39)
            throw new InvalidDataException($"World attachment pool drifted: expected 39 unique models, found {pool.Count}.");

        var direct = LoadWorldAttachmentDirectSlots()
            .Where(x => IsScopeApplicable(x.Scope, hasDlc1))
            .ToList();
        var generator = LoadWorldAttachmentGeneratorSlots()
            .Where(x => IsScopeApplicable(x.ActorScope, hasDlc1))
            .ToList();

        var expectedDirect = hasDlc1 ? 7 : 5;
        var expectedGenerator = hasDlc1 ? 60 : 57;
        var expectedTotal = hasDlc1 ? 67 : 62;
        if (direct.Count != expectedDirect || generator.Count != expectedGenerator
            || direct.Count + generator.Count != expectedTotal)
        {
            throw new InvalidDataException(
                $"World attachment safe surface drifted: direct/generator/total={direct.Count}/{generator.Count}/{direct.Count + generator.Count}; " +
                $"expected {expectedDirect}/{expectedGenerator}/{expectedTotal}.");
        }

        var slots = new List<WorldAttachmentSlot>();
        slots.AddRange(direct.Select(x => new WorldAttachmentSlot(
            "direct|" + x.Scope + "|" + x.ActorSid,
            x.ItemSid, x.Category, x.Tier,
            true, x.Scope, x.SourceFile, x.ActorSid,
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)));

        slots.AddRange(generator.Select(x => new WorldAttachmentSlot(
            "generator|" + x.ActorScope + "|" + x.ActorSid + "|" + x.CloneSid + "|" + x.EntryNodePath,
            x.ItemSid, x.Category, x.Tier,
            false, x.ActorScope, x.ActorFile, x.ActorSid,
            x.GeneratorSid, x.CloneSid, x.RedirectNodePaths, x.EntryNodePath, string.Empty)));

        if (slots.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count() != slots.Count)
            throw new InvalidDataException("World attachment safe surface contains duplicate physical slot keys.");

        var sourceCounts = slots
            .GroupBy(x => x.ItemSid, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var sourceItems = sourceCounts.Keys.ToList();
        var missingPool = sourceItems.Where(x => !pool.ContainsKey(x)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (missingPool.Count > 0)
            throw new InvalidDataException(
                "World attachment surface references items outside the audited pool: " + string.Join(", ", missingPool));

        var expectedUnique = hasDlc1 ? 39 : 38;
        if (sourceItems.Count != expectedUnique)
            throw new InvalidDataException(
                $"World attachment source multiset unique-model count drifted: {sourceItems.Count}; expected {expectedUnique}.");

        static int TierNumber(string tier) => tier switch
        {
            "T1" => 1,
            "T2" => 2,
            "T3" => 3,
            "T4" => 4,
            _ => throw new InvalidDataException($"Unknown attachment progression tier: {tier}")
        };

        static bool Compatible(WorldAttachmentPoolItem item, string slotCategory, string slotTier) =>
            string.Equals(item.Category, slotCategory, StringComparison.Ordinal)
            && Math.Abs(TierNumber(item.Tier) - TierNumber(slotTier)) <= 1;

        var buckets = slots
            .GroupBy(x => (x.Category, x.Tier))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var bucket in buckets.Values)
            SelectRandomPrefix(bucket, bucket.Count);

        var itemIds = sourceItems.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var bucketKeys = buckets.Keys
            .OrderBy(x => x.Category, StringComparer.Ordinal)
            .ThenBy(x => TierNumber(x.Tier))
            .ToList();

        // Preserve the exact source-item multiset while assigning it to compatible category/tier buckets.
        var source = 0;
        var itemStart = 1;
        var bucketStart = itemStart + itemIds.Count;
        var sink = bucketStart + bucketKeys.Count;
        var nodeCount = sink + 1;

        var capacity = new int[nodeCount, nodeCount];
        var originalCapacity = new int[nodeCount, nodeCount];

        void AddEdge(int from, int to, int cap)
        {
            capacity[from, to] += cap;
            originalCapacity[from, to] += cap;
        }

        for (var ii = 0; ii < itemIds.Count; ii++)
        {
            var sid = itemIds[ii];
            var item = pool[sid];
            var count = sourceCounts[sid];
            AddEdge(source, itemStart + ii, count);

            for (var bi = 0; bi < bucketKeys.Count; bi++)
            {
                var bucket = bucketKeys[bi];
                if (Compatible(item, bucket.Category, bucket.Tier))
                    AddEdge(itemStart + ii, bucketStart + bi, count);
            }
        }

        for (var bi = 0; bi < bucketKeys.Count; bi++)
            AddEdge(bucketStart + bi, sink, buckets[bucketKeys[bi]].Count);

        var requiredFlow = slots.Count;
        var flow = 0;
        while (true)
        {
            var parent = Enumerable.Repeat(-1, nodeCount).ToArray();
            parent[source] = source;
            var queue = new Queue<int>();
            queue.Enqueue(source);

            while (queue.Count > 0 && parent[sink] < 0)
            {
                var u = queue.Dequeue();
                for (var v = 0; v < nodeCount; v++)
                {
                    if (parent[v] >= 0 || capacity[u, v] <= 0)
                        continue;
                    parent[v] = u;
                    queue.Enqueue(v);
                }
            }

            if (parent[sink] < 0)
                break;

            var aug = int.MaxValue;
            for (var v = sink; v != source; v = parent[v])
                aug = Math.Min(aug, capacity[parent[v], v]);

            for (var v = sink; v != source; v = parent[v])
            {
                var u = parent[v];
                capacity[u, v] -= aug;
                capacity[v, u] += aug;
            }

            flow += aug;
        }

        if (flow != requiredFlow)
            throw new InvalidDataException(
                $"World attachment compatible shuffle allocation failed: assigned {flow}/{requiredFlow} slots.");

        var assignments = new Dictionary<string, string>(StringComparer.Ordinal);
        var bucketOffsets = bucketKeys.ToDictionary(x => x, _ => 0);

        for (var ii = 0; ii < itemIds.Count; ii++)
        {
            for (var bi = 0; bi < bucketKeys.Count; bi++)
            {
                var used = originalCapacity[itemStart + ii, bucketStart + bi]
                           - capacity[itemStart + ii, bucketStart + bi];
                if (used <= 0)
                    continue;

                var bucket = bucketKeys[bi];
                var bucketSlots = buckets[bucket];
                for (var n = 0; n < used; n++)
                {
                    var index = bucketOffsets[bucket]++;
                    if (index >= bucketSlots.Count)
                        throw new InvalidDataException("World attachment bucket allocation overflow.");
                    assignments[bucketSlots[index].Key] = itemIds[ii];
                }
            }
        }

        if (assignments.Count != slots.Count)
            throw new InvalidDataException(
                $"World attachment shuffle assigned {assignments.Count}/{slots.Count} physical slots.");

        var assignedCounts = assignments.Values
            .GroupBy(x => x, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        if (sourceCounts.Count != assignedCounts.Count
            || sourceCounts.Any(x => !assignedCounts.TryGetValue(x.Key, out var count) || count != x.Value))
        {
            throw new InvalidDataException("World attachment shuffle did not preserve the exact source-item multiset.");
        }

        var edits = 0;

        // Direct pickups: patch both runtime item-identity fields together.
        foreach (var row in direct)
        {
            var key = "direct|" + row.Scope + "|" + row.ActorSid;
            var assigned = assignments[key];
            if (string.Equals(assigned, row.ItemSid, StringComparison.Ordinal))
                continue;

            var document = GetDocument(documents, row.SourceFile);
            document.AddAssignment(row.ActorSid, Array.Empty<string>(), "SpawnedPrototypeSID", assigned);
            document.AddAssignment(row.ActorSid, Array.Empty<string>(), "ItemSID", assigned);
            edits += 2;
        }

        // Every physical generator usage gets a TCD-owned clone, even when the vanilla
        // generator currently has one consumer. This isolates the shuffle from traders,
        // quests, benches, other world actors, and future reuse of the vanilla definition.
        var generatorDocument = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
        foreach (var cloneGroup in generator.GroupBy(x => x.CloneSid, StringComparer.Ordinal))
        {
            var first = cloneGroup.First();
            if (cloneGroup.Any(x => !string.Equals(x.GeneratorSid, first.GeneratorSid, StringComparison.Ordinal)
                                    || !string.Equals(x.ActorSid, first.ActorSid, StringComparison.Ordinal)
                                    || !string.Equals(x.ActorFile, first.ActorFile, StringComparison.Ordinal)))
            {
                throw new InvalidDataException($"World attachment clone group '{first.CloneSid}' mixes physical consumers.");
            }

            var cloneRoot = generatorDocument.GetOrAddReferencedRoot(
                first.CloneSid,
                "../ItemGeneratorPrototypes.cfg",
                first.GeneratorSid);
            cloneRoot.SetAssignment("SID", first.CloneSid);
            edits++;

            foreach (var row in cloneGroup)
            {
                var key = "generator|" + row.ActorScope + "|" + row.ActorSid + "|" + row.CloneSid + "|" + row.EntryNodePath;
                var assigned = assignments[key];
                var path = row.EntryNodePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                generatorDocument.AddAssignment(row.CloneSid, path, "ItemPrototypeSID", assigned);
                edits++;
            }

            var actorDocument = GetDocument(documents, first.ActorFile);
            foreach (var redirectPath in first.RedirectNodePaths.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                actorDocument.AddAssignment(
                    first.ActorSid,
                    redirectPath.Split('/', StringSplitOptions.RemoveEmptyEntries),
                    "PrototypeSID",
                    first.CloneSid);
                edits++;
            }
        }

        return edits;
    }

    private static IReadOnlyList<WorldAttachmentPoolItem> LoadWorldAttachmentPool()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "TrueCustomDifficulty.Data.world-attachment-shuffle-pool.csv")
            ?? throw new InvalidDataException("Embedded world attachment shuffle pool was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();

        var rows = new List<WorldAttachmentPoolItem>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 3)
                throw new InvalidDataException("Invalid world attachment shuffle pool row.");
            rows.Add(new WorldAttachmentPoolItem(c[0], c[1], c[2]));
        }

        if (rows.Count != 39 || rows.Select(x => x.ItemSid).Distinct(StringComparer.Ordinal).Count() != 39)
            throw new InvalidDataException(
                $"World attachment shuffle pool must contain 39 unique models; found {rows.Count} rows.");

        return rows;
    }

    private static IReadOnlyList<WorldAttachmentDirectSlot> LoadWorldAttachmentDirectSlots()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "TrueCustomDifficulty.Data.world-attachment-direct-manifest.csv")
            ?? throw new InvalidDataException("Embedded world attachment direct manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();

        var rows = new List<WorldAttachmentDirectSlot>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 7)
                throw new InvalidDataException("Invalid world attachment direct manifest row.");
            rows.Add(new WorldAttachmentDirectSlot(c[0], c[1], c[2], c[3], c[4], c[5], c[6]));
        }

        if (rows.Count != 7)
            throw new InvalidDataException($"World attachment direct manifest must contain 7 audited slots; found {rows.Count}.");

        return rows;
    }

    private static IReadOnlyList<WorldAttachmentGeneratorSlot> LoadWorldAttachmentGeneratorSlots()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "TrueCustomDifficulty.Data.world-attachment-generator-manifest.csv")
            ?? throw new InvalidDataException("Embedded world attachment generator manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();

        var rows = new List<WorldAttachmentGeneratorSlot>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 10)
                throw new InvalidDataException("Invalid world attachment generator manifest row.");
            rows.Add(new WorldAttachmentGeneratorSlot(
                c[0], c[1], c[2], c[3], c[4], c[5], c[6], c[7], c[8], c[9]));
        }

        if (rows.Count != 60)
            throw new InvalidDataException(
                $"World attachment generator manifest must contain 60 audited physical entry slots; found {rows.Count}.");
        if (rows.Select(x => x.CloneSid).Distinct(StringComparer.Ordinal).Count() != 55)
            throw new InvalidDataException("World attachment generator manifest must resolve to exactly 55 physical clone groups.");

        return rows;
    }

    private sealed record WorldAttachmentPoolItem(string ItemSid, string Category, string Tier);
    private sealed record WorldAttachmentDirectSlot(
        string Scope, string SourceFile, string LevelName, string ActorSid, string ItemSid, string Category, string Tier);
    private sealed record WorldAttachmentGeneratorSlot(
        string ActorScope, string ActorFile, string ActorSid, string RedirectNodePaths,
        string GeneratorSid, string CloneSid, string EntryNodePath, string ItemSid, string Category, string Tier);
    private sealed record WorldAttachmentSlot(
        string Key, string ItemSid, string Category, string Tier,
        bool IsDirect, string Scope, string File, string ActorSid,
        string GeneratorSid, string CloneSid, string RedirectNodePaths, string EntryNodePath, string Reserved);

    private static int WriteWorldDetectorSpawns(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        const string parameterId = "loot.worldDetectors";
        if (!modifiedById.ContainsKey(parameterId))
            return 0;

        if (!allById.TryGetValue(parameterId, out var parameter) || !parameter.IsToggle)
            throw new InvalidDataException($"Missing toggle parameter {parameterId}.");

        writtenParameters.Add(parameterId);
        if (!parameter.CurrentBoolean)
            return 0; // Vanilla OFF: preserve authored detector spawns.

        var direct = LoadWorldDetectorDirectRows()
            .Where(x => IsScopeApplicable(x.Scope, hasDlc1))
            .ToList();
        var stash = LoadWorldDetectorStashRows()
            .Where(x => IsScopeApplicable(x.Scope, hasDlc1))
            .ToList();

        var expectedDirect = hasDlc1 ? 7 : 4;
        var expectedStash = hasDlc1 ? 1 : 0;
        if (direct.Count != expectedDirect || stash.Count != expectedStash)
            throw new InvalidDataException(
                $"World detector surface drifted: direct/stash={direct.Count}/{stash.Count}; expected {expectedDirect}/{expectedStash}.");

        var edits = 0;
        foreach (var row in direct)
        {
            var document = GetDocument(documents, row.SourceFile);
            document.AddRemovalRoot(row.ActorSid);
            edits++;
        }

        foreach (var row in stash)
        {
            var document = GetDocument(documents, row.File);
            var path = row.NodePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (path.Length < 2)
                throw new InvalidDataException($"Invalid world detector stash node path: {row.NodePath}");
            document.AddRemovalNode(row.GeneratorSid, path[..^1], path[^1]);
            edits++;
        }

        return edits;
    }

    private static IReadOnlyList<WorldDetectorDirectRow> LoadWorldDetectorDirectRows()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "TrueCustomDifficulty.Data.world-detector-direct-manifest.csv")
            ?? throw new InvalidDataException("Embedded world detector direct manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var rows = new List<WorldDetectorDirectRow>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 4) throw new InvalidDataException("Invalid world detector direct manifest row.");
            rows.Add(new WorldDetectorDirectRow(c[0], c[1], c[2], c[3]));
        }
        if (rows.Count != 7)
            throw new InvalidDataException($"World detector direct manifest must contain 7 active safe actors; found {rows.Count}.");
        return rows;
    }

    private static IReadOnlyList<WorldDetectorStashRow> LoadWorldDetectorStashRows()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "TrueCustomDifficulty.Data.world-detector-stash-manifest.csv")
            ?? throw new InvalidDataException("Embedded world detector stash manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var rows = new List<WorldDetectorStashRow>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 6) throw new InvalidDataException("Invalid world detector stash manifest row.");
            rows.Add(new WorldDetectorStashRow(c[0], c[1], c[2], c[3], c[4], c[5]));
        }
        if (rows.Count != 1)
            throw new InvalidDataException($"World detector stash manifest must contain exactly one proven active non-quest stash detector; found {rows.Count}.");
        return rows;
    }

    private sealed record WorldDetectorDirectRow(string Scope, string SourceFile, string ActorSid, string ItemSid);
    private sealed record WorldDetectorStashRow(
        string Scope, string File, string GeneratorSid, string NodePath, string ItemSid, string ActorSid);

    private static int WriteBlueprintShuffle(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        const string toggleId = "loot.blueprintShuffle";
        if (!modifiedById.ContainsKey(toggleId))
            return 0;

        writtenParameters.Add(toggleId);
        var enabled = allById.TryGetValue(toggleId, out var parameter)
                      && parameter.IsToggle
                      && parameter.CurrentBoolean;
        if (!enabled)
            return 0; // Vanilla: emit no blueprint-placement patch.

        var slots = LoadBlueprintShuffleSlots()
            .OrderBy(x => x.GeneratorSid, StringComparer.Ordinal)
            .ToList();
        if (slots.Count < 2)
            return 0;

        // Fresh non-deterministic build-time derangement:
        // every included blueprint remains present exactly once, and none stays
        // in its vanilla slot when the shuffle is enabled.
        var rng = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
        var shuffled = slots.Select(x => x.ItemSid).ToList();
        var success = false;
        for (var attempt = 0; attempt < 128 && !success; attempt++)
        {
            for (var i = shuffled.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                if (j != i) (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            success = true;
            for (var i = 0; i < slots.Count; i++)
            {
                if (StringComparer.Ordinal.Equals(slots[i].ItemSid, shuffled[i]))
                {
                    success = false;
                    break;
                }
            }
        }

        if (!success)
        {
            // Deterministic fallback derangement: rotate by one slot.
            shuffled = slots.Skip(1).Select(x => x.ItemSid).Append(slots[0].ItemSid).ToList();
        }

        var document = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
        var edits = 0;
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var path = slot.NodePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (path.Length < 2)
                throw new InvalidDataException($"Invalid blueprint shuffle node path for {slot.GeneratorSid}: {slot.NodePath}");

            var node = document.AddReplacementNode(slot.GeneratorSid, path[..^1], path[^1]);
            node.SetAssignment("ItemPrototypeSID", shuffled[i]);
            node.SetAssignment("Chance", "1");
            edits++;
        }

        return edits;
    }

    private static IReadOnlyList<BlueprintShuffleSlot> LoadBlueprintShuffleSlots()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TrueCustomDifficulty.Data.blueprint-shuffle-manifest.csv")
            ?? throw new InvalidDataException("Embedded blueprint shuffle manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var rows = new List<BlueprintShuffleSlot>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 8) throw new InvalidDataException("Invalid blueprint shuffle manifest row.");
            rows.Add(new BlueprintShuffleSlot(c[1], c[2], c[3]));
        }

        if (rows.Count != 77)
            throw new InvalidDataException($"Blueprint shuffle manifest must contain exactly 77 proven world slots; found {rows.Count}.");
        if (rows.Select(x => x.GeneratorSid).Distinct(StringComparer.Ordinal).Count() != rows.Count)
            throw new InvalidDataException("Blueprint shuffle manifest contains duplicate generator slots.");
        if (rows.Select(x => x.ItemSid).Distinct(StringComparer.Ordinal).Count() != rows.Count)
            throw new InvalidDataException("Blueprint shuffle manifest contains duplicate blueprint items.");

        return rows;
    }

    private sealed record BlueprintShuffleSlot(string GeneratorSid, string ItemSid, string NodePath);

    private static int WriteTraderMinBuyCondition(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        IReadOnlyDictionary<string, ParameterViewModel> allById,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> writtenParameters)
    {
        const string parameterId = "economy.traderMinBuyCondition";
        if (!modifiedById.ContainsKey(parameterId))
            return 0;

        if (!allById.TryGetValue(parameterId, out var parameter) || !parameter.IsSlider)
            throw new InvalidDataException($"Missing slider parameter {parameterId}.");

        writtenParameters.Add(parameterId);
        var threshold = Math.Clamp(parameter.CurrentNumber, 0.0, 1.0);
        var rows = LoadTraderMinBuyConditionRows()
            .Where(row => IsScopeApplicable(row.Scope, hasDlc1))
            .OrderBy(row => row.File, StringComparer.Ordinal)
            .ThenBy(row => row.Prototype, StringComparer.Ordinal)
            .ToList();

        var edits = 0;
        foreach (var row in rows)
        {
            var document = GetDocument(documents, row.File);
            var path = row.NodePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var value = FormatLikeSource(threshold, row.VanillaRaw);

            document.AddAssignment(row.Prototype, path, row.WeaponKey, value);
            document.AddAssignment(row.Prototype, path, row.ArmorKey, value);
            edits += 2;
        }

        return edits;
    }

    private static IReadOnlyList<TraderMinBuyConditionRow> LoadTraderMinBuyConditionRows()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "TrueCustomDifficulty.Data.trader-min-buy-condition-manifest.csv")
            ?? throw new InvalidDataException("Embedded trader minimum buy condition manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();

        var rows = new List<TraderMinBuyConditionRow>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 7)
                throw new InvalidDataException("Invalid trader minimum buy condition manifest row.");

            rows.Add(new TraderMinBuyConditionRow(c[0], c[1], c[2], c[3], c[4], c[5], c[6]));
        }

        if (rows.Count != 86)
            throw new InvalidDataException($"Trader minimum buy condition manifest must contain exactly 86 production profiles; found {rows.Count}.");
        if (rows.Select(x => (x.File, x.Prototype)).Distinct().Count() != rows.Count)
            throw new InvalidDataException("Trader minimum buy condition manifest contains duplicate profile targets.");

        return rows;
    }

    private sealed record TraderMinBuyConditionRow(
        string Scope,
        string File,
        string Prototype,
        string NodePath,
        string WeaponKey,
        string ArmorKey,
        string VanillaRaw);

    private static void CopyIfPresent(PatchNode node, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(value)) node.SetAssignment(key, value);
    }

    private static void SelectDeterministicPrefix<T>(IList<T> items, int count, Random rng)
    {
        for (var i = 0; i < count; i++)
        {
            var j = rng.Next(i, items.Count);
            if (j != i) (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private static IReadOnlyList<WorldWeaponSlot> LoadWorldWeaponSlots()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TrueCustomDifficulty.Data.world-weapon-shuffle-manifest.csv")
            ?? throw new InvalidDataException("Embedded world weapon shuffle manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var rows = new List<WorldWeaponSlot>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 15) throw new InvalidDataException("Invalid world weapon shuffle manifest row.");
            rows.Add(new WorldWeaponSlot(c[1], c[3], c[4], c[5], c[6], c[7], c[8], c[9], c[10], c[11], c[12], c[13], c[14]));
        }
        return rows;
    }

    private static IReadOnlyList<WorldWeaponPoolItem> LoadWorldWeaponPool()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TrueCustomDifficulty.Data.world-weapon-shuffle-pool.csv")
            ?? throw new InvalidDataException("Embedded world weapon shuffle pool was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var rows = new List<WorldWeaponPoolItem>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 3) throw new InvalidDataException("Invalid world weapon shuffle pool row.");
            rows.Add(new WorldWeaponPoolItem(c[0], c[1], c[2]));
        }
        return rows;
    }

    private static IReadOnlyList<WorldWeaponDirectSlot> LoadWorldWeaponDirectSlots()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TrueCustomDifficulty.Data.world-weapon-direct-manifest.csv")
            ?? throw new InvalidDataException("Embedded world weapon direct-location manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var rows = new List<WorldWeaponDirectSlot>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 7) throw new InvalidDataException("Invalid world weapon direct-location manifest row.");
            rows.Add(new WorldWeaponDirectSlot(c[0], c[1], c[2], c[3], c[4], c[5], c[6]));
        }

        if (rows.Count != 362)
            throw new InvalidDataException($"World weapon direct-location manifest count drifted: {rows.Count}; expected 362.");
        if (rows.Select(x => (x.Scope, x.ActorSid)).Distinct().Count() != rows.Count)
            throw new InvalidDataException("World weapon direct-location manifest contains duplicate actor targets.");

        return rows;
    }

    private sealed record WorldWeaponSlot(string GeneratorSid, string ItemSid, string Family, string Tier, string NodePath,
        string Chance, string Weight, string MinDurability, string MaxDurability, string AmmoMinCount, string AmmoMaxCount,
        string MinCount, string MaxCount);
    private sealed record WorldWeaponPoolItem(string ItemSid, string Family, string Tier);
    private sealed record WorldWeaponDirectSlot(
        string Scope, string SourceFile, string LevelName, string ActorSid, string ItemSid, string Family, string Tier);
    private sealed record WorldWeaponLocation(
        string ItemSid, string Family, string Tier,
        bool IsDirect, string Scope, string SourceFile, string LevelName, string ActorSid,
        string GeneratorSid, string NodePath,
        string Chance, string Weight, string MinDurability, string MaxDurability,
        string AmmoMinCount, string AmmoMaxCount, string MinCount, string MaxCount);

    // Derange only the nine verified non-deferred unique world pickup actor
    // identities; never remove a pickup or edit a globally shared ItemSID.
    // Mesh and authored item condition travel with the weapon, while physical
    // actor identity, coordinates and rotation stay attached to the location.
    private static (int EditCount, int FileCount) WriteLooseUniqueWeaponShuffle(
        CfgWriterLooseUniqueWeaponShuffle route,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        string outputRoot, ISet<string> writtenParameters)
    {
        var parameterId = route.ParameterId;
        if (!modifiedById.TryGetValue(parameterId, out var parameter) || !parameter.IsToggle)
            return (0, 0);
        writtenParameters.Add(parameterId);
        if (!parameter.CurrentBoolean) return (0, 0);

        var slots = LoadLooseUniqueWeaponSlots(route)
            .OrderBy(x => x.ActorSid, StringComparer.Ordinal).ToList();
        // Sattolo shuffle produces one random cycle, hence every slot changes
        // to a DIFFERENT weapon with exactly one of each weapon remaining.
        var shuffled = slots.ToList();
        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (var i = 0; i < slots.Count; i++)
        {
            var location = slots[i];
            var weapon = shuffled[i];
            if (string.Equals(location.ItemSid, weapon.ItemSid, StringComparison.Ordinal))
                throw new InvalidDataException("Unique weapon shuffle failed to move every weapon.");
            var outputPath = GetSpawnActorRemovalOutputPath(
                outputRoot, location.SourceFile, location.LevelName, location.ActorSid);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath,
                $"// Generated by True Custom Difficulty.{Environment.NewLine}" +
                $"// Loose unique weapon location shuffle: {location.ItemSid} -> {weapon.ItemSid}.{Environment.NewLine}" +
                $"{location.ActorSid} : struct.begin {{bpatch}}{Environment.NewLine}" +
                $"   SpawnedPrototypeSID = {weapon.ItemSid}{Environment.NewLine}" +
                $"   ItemSID = {weapon.ItemSid}{Environment.NewLine}" +
                $"   MeshPath = {weapon.MeshPath}{Environment.NewLine}" +
                $"   Durability = {weapon.Durability}{Environment.NewLine}" +
                $"   DisablePhysicsAndCollision = {weapon.DisablePhysicsAndCollision}{Environment.NewLine}" +
                $"struct.end{Environment.NewLine}",
                new UTF8Encoding(false));
        }
        return (slots.Count * 5, slots.Count);
    }

    private static IReadOnlyList<LooseUniqueWeaponSlot> LoadLooseUniqueWeaponSlots(
        CfgWriterLooseUniqueWeaponShuffle route)
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(route.ManifestResource)
            ?? throw new InvalidDataException("Embedded unique-weapon shuffle manifest missing.");
        using var reader = new StreamReader(stream);
        var header = reader.ReadLine();
        if (header != "scope,sourceFile,levelName,actorSid,itemSid,meshPath,durability,disablePhysicsAndCollision")
            throw new InvalidDataException("Invalid unique-weapon shuffle manifest header.");
        var rows = new List<LooseUniqueWeaponSlot>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 8 || c[0] != "baseGame" ||
                c[1] != "GameData/SpawnActorPrototypes.cfg" || c[2] != "WorldMap_WP" ||
                c[3].Length != 32 || !c[3].All(Uri.IsHexDigit) ||
                !c[4].StartsWith("Gun", StringComparison.Ordinal) ||
                !(c[5].StartsWith("SkeletalMesh'", StringComparison.Ordinal) ||
                  c[5].StartsWith("StaticMesh'", StringComparison.Ordinal)) ||
                !double.TryParse(c[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var durability) ||
                durability < 0.0 || durability > 1.0 ||
                (c[7] != "true" && c[7] != "false"))
                throw new InvalidDataException("Invalid unique-weapon shuffle manifest row.");
            rows.Add(new LooseUniqueWeaponSlot(c[1], c[2], c[3], c[4], c[5], c[6], c[7]));
        }
        if (rows.Count != route.ExpectedActorCount ||
            rows.Select(x => x.ActorSid).Distinct(StringComparer.Ordinal).Count() != 9 ||
            rows.Select(x => x.ItemSid).Distinct(StringComparer.Ordinal).Count() != 9)
            throw new InvalidDataException("Unique-weapon shuffle requires nine distinct actor/item pairs.");
        return rows;
    }

    private sealed record LooseUniqueWeaponSlot(
        string SourceFile, string LevelName, string ActorSid, string ItemSid,
        string MeshPath, string Durability, string DisablePhysicsAndCollision);

    private static (int EditCount, int FileCount) WriteLooseWorldLootRemovals(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        bool hasDlc1, string outputRoot, ISet<string> writtenParameters)
    {
        const string parameterId = "loot.looseWorldLootRemoved";
        if (!modifiedById.TryGetValue(parameterId, out var parameter) || !parameter.IsSlider) return (0, 0);
        writtenParameters.Add(parameterId);
        var fraction = Math.Clamp(parameter.CurrentNumber, 0.0, 1.0);
        if (fraction <= 0.0) return (0, 0);
        var rows = LoadLooseWorldLootRows().Where(r => IsScopeApplicable(r.Scope, hasDlc1)).ToList();
        var removeCount = Math.Clamp((int)Math.Round(rows.Count * fraction, MidpointRounding.AwayFromZero), 0, rows.Count);
        SelectRandomPrefix(rows, removeCount);
        foreach (var row in rows.Take(removeCount))
        {
            var outputPath = GetSpawnActorRemovalOutputPath(outputRoot, row.SourceFile, row.LevelName, row.ActorSid);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath,
                $"// Generated by True Custom Difficulty.{Environment.NewLine}" +
                $"// Loose world loot: {fraction * 100:0.#}% removed at build time.{Environment.NewLine}" +
                $"{row.ActorSid} : removenode{Environment.NewLine}", new UTF8Encoding(false));
        }
        return (removeCount, removeCount);
    }

    private static IReadOnlyList<LooseWorldLootRow> LoadLooseWorldLootRows()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TrueCustomDifficulty.Data.loose-world-loot-manifest.csv")
            ?? throw new InvalidDataException("Embedded loose-world-loot manifest was not found.");
        using var reader = new StreamReader(stream); reader.ReadLine();
        var rows = new List<LooseWorldLootRow>(); string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length != 7) throw new InvalidDataException("Invalid loose-world-loot manifest row.");
            rows.Add(new LooseWorldLootRow(c[2], c[3], c[4], c[5]));
        }
        if (rows.Count != 7233) throw new InvalidDataException($"Loose-world-loot manifest count drifted: {rows.Count}; expected 7233.");
        return rows;
    }

    private sealed record LooseWorldLootRow(string Scope, string SourceFile, string LevelName, string ActorSid);

    private static (int EditCount, int FileCount) WriteWorldF1GrenadeBoxes(
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        bool hasDlc1,
        string outputRoot,
        ISet<string> writtenParameters)
    {
        const string parameterId = "loot.worldF1GrenadeBoxes";
        if (!modifiedById.TryGetValue(parameterId, out var parameter) || !parameter.IsSlider)
            return (0, 0);

        writtenParameters.Add(parameterId);
        var removedFraction = Math.Clamp(parameter.CurrentNumber, 0.0, 1.0);

        var rows = LoadWorldF1GrenadeBoxRows()
            .Where(x => IsScopeApplicable(x.Scope, hasDlc1))
            .ToList();

        var expected = hasDlc1 ? 101 : 61;
        if (rows.Count != expected)
            throw new InvalidDataException(
                $"World F1 grenade box surface drifted: expected {expected}, found {rows.Count}.");

        var removeCount = Math.Clamp(
            (int)Math.Round(rows.Count * removedFraction, MidpointRounding.AwayFromZero),
            0,
            rows.Count);
        var keepCount = rows.Count - removeCount;
        if (removeCount <= 0)
            return (0, 0); // 0% = vanilla/no-op.

        SelectRandomPrefix(rows, removeCount);

        foreach (var row in rows.Take(removeCount))
        {
            var outputPath = GetSpawnActorRemovalOutputPath(
                outputRoot,
                row.SourceFile,
                row.LevelName,
                row.ActorSid);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(
                outputPath,
                $"// Generated by True Custom Difficulty.{Environment.NewLine}" +
                $"// F1 Grenade Boxes: keeping {keepCount}/{rows.Count} audited physical boxes.{Environment.NewLine}" +
                $"{row.ActorSid} : removenode{Environment.NewLine}",
                new UTF8Encoding(false));
        }

        return (removeCount, removeCount);
    }

    private static IReadOnlyList<WorldF1GrenadeBoxRow> LoadWorldF1GrenadeBoxRows()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "TrueCustomDifficulty.Data.world-f1-grenade-box-manifest.csv")
            ?? throw new InvalidDataException("Embedded world F1 grenade box manifest was not found.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();

        var rows = new List<WorldF1GrenadeBoxRow>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var c = line.Split(',');
            if (c.Length != 6)
                throw new InvalidDataException("Invalid world F1 grenade box manifest row.");
            rows.Add(new WorldF1GrenadeBoxRow(c[0], c[1], c[2], c[3], c[4], c[5]));
        }

        if (rows.Count != 101)
            throw new InvalidDataException(
                $"World F1 grenade box manifest must contain 101 audited physical actors; found {rows.Count}.");
        if (rows.Count(x => x.Scope.Equals("base", StringComparison.OrdinalIgnoreCase)) != 61
            || rows.Count(x => x.Scope.Equals("dlc1", StringComparison.OrdinalIgnoreCase)) != 40)
        {
            throw new InvalidDataException("World F1 grenade box manifest scope split must be 61 base + 40 DLC1.");
        }
        if (rows.Any(x => !x.SpawnedPrototypeSid.Equals("D_WoodenAmmoCrate_01", StringComparison.Ordinal)
                          || !x.GeneratorSid.Equals("DestructibleStash_F1", StringComparison.Ordinal)))
        {
            throw new InvalidDataException("World F1 grenade box manifest contains a non-dedicated F1 box row.");
        }

        return rows;
    }

    private sealed record WorldF1GrenadeBoxRow(
        string Scope,
        string SourceFile,
        string LevelName,
        string ActorSid,
        string SpawnedPrototypeSid,
        string GeneratorSid);

    private static (int EditCount, int FileCount) WriteCrateSpawnRemovals(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modifiedById,
        bool hasDlc1,
        string outputRoot,
        ISet<string> writtenParameters)
    {
        var config = baseline.Special.CrateSpawnProduction;
        if (config.Categories.Count == 0)
            return (0, 0);

        var rows = LoadCrateSpawnRows(config.ManifestResource);
        var counts = rows.GroupBy(x => x.Category, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var category in config.Categories)
        {
            if (!counts.TryGetValue(category.Category, out var actual) || actual != category.ExpectedActorCount)
                throw new InvalidDataException(
                    $"Crate-spawn manifest count for category {category.Category} is {actual}; expected {category.ExpectedActorCount}.");
        }

        var edits = 0;
        var files = 0;
        foreach (var category in config.Categories)
        {
            if (!modifiedById.TryGetValue(category.ParameterId, out var parameter) || !parameter.IsSlider)
                continue;

            writtenParameters.Add(category.ParameterId);
            var removalFraction = Math.Clamp(parameter.CurrentNumber, 0.0, 1.0);
            if (removalFraction <= 0.0)
                continue; // Vanilla: this world-loot domain emits no patch.

            var candidates = rows
                .Where(r => string.Equals(r.Category, category.Category, StringComparison.Ordinal)
                            && IsScopeApplicable(r.Scope, hasDlc1))
                .ToList();
            var removeCount = Math.Clamp((int)Math.Round(
                candidates.Count * removalFraction, MidpointRounding.AwayFromZero), 0, candidates.Count);

            SelectRandomPrefix(candidates, removeCount);
            foreach (var row in candidates.Take(removeCount))
            {
                var outputPath = GetSpawnActorRemovalOutputPath(outputRoot, row.SourceFile, row.LevelName, row.ActorSid);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllText(
                    outputPath,
                    $"// Generated by True Custom Difficulty.{Environment.NewLine}" +
                    $"// World loot {category.Category}: {removalFraction * 100:0.#}% removed at build time.{Environment.NewLine}" +
                    $"{row.ActorSid} : removenode{Environment.NewLine}",
                    new UTF8Encoding(false));
                edits++; files++;
            }
        }
        return (edits, files);
    }

    private static void SelectRandomPrefix<T>(IList<T> items, int count)
    {
        // Partial Fisher-Yates: after count iterations, items[0..count) is an unbiased
        // sample without replacement. RandomNumberGenerator makes every Build independent
        // without carrying a global pseudo-random seed in presets or output metadata.
        for (var i = 0; i < count; i++)
        {
            var j = RandomNumberGenerator.GetInt32(i, items.Count);
            if (j != i)
            {
                var temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }
    }

    private static string GetSpawnActorRemovalOutputPath(
        string outputRoot,
        string sourceFile,
        string levelName,
        string actorSid)
    {
        var normalized = sourceFile.Replace('\\', '/');
        var relativeDirectory = Path.GetDirectoryName(normalized.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var prototypeFolder = Path.GetFileNameWithoutExtension(normalized);
        return Path.Combine(
            outputRoot,
            "Stalker2",
            "Content",
            "GameLite",
            relativeDirectory,
            prototypeFolder,
            levelName,
            actorSid + ".cfg");
    }

    private static IReadOnlyList<CrateSpawnRow> LoadCrateSpawnRows(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new InvalidDataException("Crate-spawn manifest resource name is empty.");

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded crate-spawn manifest '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);

        var header = reader.ReadLine();
        if (header is null)
            throw new InvalidDataException("Crate-spawn manifest is empty.");

        var rows = new List<CrateSpawnRow>();
        string? line;
        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');
            if (columns.Length != 9)
                throw new InvalidDataException(
                    $"Crate-spawn manifest line {lineNumber} has {columns.Length} columns instead of 9.");

            rows.Add(new CrateSpawnRow(
                columns[0].Trim(),
                columns[1].Trim(),
                columns[2].Trim().Equals("dlc1", StringComparison.OrdinalIgnoreCase) ? "dlc1" : "baseGame",
                columns[3].Trim(),
                columns[4].Trim(),
                columns[5].Trim()));
        }

        return rows;
    }

    private sealed record CrateSpawnRow(
        string ParameterId,
        string Category,
        string Scope,
        string SourceFile,
        string LevelName,
        string ActorSid);

    private static string FormatParameterScalar(ParameterViewModel parameter)
    {
        if (parameter.IsToggle)
            return parameter.CurrentBoolean ? "true" : "false";
        if (parameter.IsSlider)
            return FormatNumber(parameter.CurrentNumber);
        if (parameter.IsChoiceSlider)
            return parameter.CurrentText;
        throw new InvalidDataException($"Unsupported special CFG scalar control: {parameter.Id}");
    }

    private static string GetOutputPath(string outputRoot, string sourceFile)
    {
        var normalized = sourceFile.Replace('\\', '/');
        var relativeDirectory = Path.GetDirectoryName(normalized.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var fileName = Path.GetFileName(normalized);
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var root = Path.Combine(outputRoot, "Stalker2", "Content", "GameLite", relativeDirectory);

        if (fileName.EndsWith("Prototypes.cfg", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(
                root,
                stem,
                $"{stem}_patch_{PatchSuffix}.cfg");
        }

        return Path.Combine(root, $"{fileName}_patch_{PatchSuffix}");
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
    }

    private sealed class PatchDocument
    {
        private readonly Dictionary<string, PatchNode> _roots = new(StringComparer.Ordinal);
        public bool IsEmpty => _roots.Count == 0;

        public PatchNode GetOrAddNode(string prototype, IEnumerable<string> nodePath)
        {
            var root = GetOrAddRoot(prototype);
            var current = root;
            foreach (var segment in nodePath)
                current = current.GetOrAddChild(segment, useBpatch: true);
            return current;
        }

        public PatchNode GetOrAddReferencedRoot(string prototype, string refUrl, string refKey)
        {
            if (!_roots.TryGetValue(prototype, out var node))
            {
                node = new PatchNode(prototype, useBpatch: false, refUrl, refKey);
                _roots[prototype] = node;
            }
            else
            {
                node.SetReference(refUrl, refKey);
            }
            return node;
        }

        public void AddAssignment(string prototype, IEnumerable<string> nodePath, string key, string value)
        {
            GetOrAddNode(prototype, nodePath).SetAssignment(key, value);
        }

        public PatchNode AddReplacementNode(string prototype, IEnumerable<string> parentPath, string childName)
        {
            var parent = GetOrAddNode(prototype, parentPath);
            parent.RemoveChild(childName);
            return parent.GetOrAddChild(childName, useBpatch: false);
        }

        public void AddRemovalNode(string prototype, IEnumerable<string> parentPath, string childName)
        {
            var parent = GetOrAddNode(prototype, parentPath);
            parent.RemoveChild(childName);
            parent.GetOrAddChild(childName, useBpatch: false).SetRemoval();
        }

        public void AddRemovalRoot(string prototype)
        {
            var root = GetOrAddRoot(prototype);
            root.SetRemoval();
        }

        public string Render(string sourceFile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Generated by True Custom Difficulty.");
            sb.AppendLine($"// BPatch source: {sourceFile.Replace('\\', '/')}");
            sb.AppendLine("// Only values changed by the active preset are emitted.");
            sb.AppendLine();

            foreach (var root in _roots.OrderBy(x => x.Key, CfgNaturalComparer.Instance))
            {
                root.Value.Render(sb, 0);
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        private PatchNode GetOrAddRoot(string name)
        {
            if (!_roots.TryGetValue(name, out var node))
            {
                node = new PatchNode(name, useBpatch: true);
                _roots[name] = node;
            }
            return node;
        }
    }

    private sealed class PatchNode
    {
        private readonly Dictionary<string, string> _assignments = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PatchNode> _children = new(StringComparer.Ordinal);

        public string Name { get; }
        public bool UseBpatch { get; private set; }
        public string ReferenceUrl { get; private set; } = string.Empty;
        public string ReferenceKey { get; private set; } = string.Empty;
        public bool IsRemoval { get; private set; }

        public PatchNode(string name, bool useBpatch, string referenceUrl = "", string referenceKey = "")
        {
            Name = name;
            UseBpatch = useBpatch;
            ReferenceUrl = referenceUrl;
            ReferenceKey = referenceKey;
        }

        public void SetReference(string referenceUrl, string referenceKey)
        {
            UseBpatch = false;
            ReferenceUrl = referenceUrl;
            ReferenceKey = referenceKey;
        }

        public void SetRemoval()
        {
            IsRemoval = true;
            UseBpatch = false;
            _assignments.Clear();
            _children.Clear();
        }

        public PatchNode GetOrAddChild(string name, bool useBpatch)
        {
            if (!_children.TryGetValue(name, out var node))
            {
                node = new PatchNode(name, useBpatch);
                _children[name] = node;
            }
            else if (!useBpatch)
            {
                node.UseBpatch = false;
            }
            return node;
        }

        public void SetAssignment(string key, string value)
        {
            _children.Remove(key);
            _assignments[key] = value;
        }

        public void RemoveAssignment(string key) => _assignments.Remove(key);
        public void RemoveChild(string name) => _children.Remove(name);

        public void Clear()
        {
            _assignments.Clear();
            _children.Clear();
        }

        public void Render(StringBuilder sb, int indent)
        {
            var prefix = new string(' ', indent * 4);
            if (IsRemoval)
            {
                sb.Append(prefix).Append(Name).Append(" : removenode").AppendLine();
                return;
            }

            sb.Append(prefix).Append(Name).Append(" : struct.begin");
            if (!string.IsNullOrWhiteSpace(ReferenceUrl) || !string.IsNullOrWhiteSpace(ReferenceKey))
                sb.Append(" {refurl=").Append(ReferenceUrl).Append(";refkey=").Append(ReferenceKey).Append('}');
            else if (UseBpatch)
                sb.Append(" {bpatch}");
            sb.AppendLine();

            foreach (var assignment in _assignments.OrderBy(x => x.Key, CfgNaturalComparer.Instance))
            {
                sb.Append(new string(' ', (indent + 1) * 4))
                    .Append(assignment.Key)
                    .Append(" =");
                if (!string.IsNullOrWhiteSpace(assignment.Value))
                    sb.Append(' ').Append(assignment.Value);
                sb.AppendLine();
            }

            foreach (var child in _children.OrderBy(x => x.Key, CfgNaturalComparer.Instance))
                child.Value.Render(sb, indent + 1);

            sb.Append(prefix).AppendLine("struct.end");
        }
    }

    private sealed class CfgNaturalComparer : IComparer<string>
    {
        public static CfgNaturalComparer Instance { get; } = new();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            var xi = IndexValue(x);
            var yi = IndexValue(y);
            if (xi >= 0 && yi >= 0)
                return xi.CompareTo(yi);
            if (xi >= 0)
                return -1;
            if (yi >= 0)
                return 1;
            return StringComparer.Ordinal.Compare(x, y);
        }

        public static int IndexValue(string value)
        {
            if (value.Length >= 3 && value[0] == '[' && value[^1] == ']'
                && int.TryParse(value[1..^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                return index;
            }
            return -1;
        }
    }
}
