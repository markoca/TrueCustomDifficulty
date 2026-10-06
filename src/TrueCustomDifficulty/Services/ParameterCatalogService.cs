using System.IO;
using System.Reflection;
using System.Text.Json;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

public sealed class ParameterCatalogService
{
    private const string EmbeddedCatalogName = "TrueCustomDifficulty.Data.parameters.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public ParameterCatalog Load()
    {
        // The catalog embedded into the built EXE is authoritative. A stale Data/parameters.json
        // left beside the executable used to override the embedded catalog silently, which could
        // disable newly added controls/constraints after an otherwise correct publish.
        //
        // Developers can still opt into an external catalog deliberately by setting
        // STALKER2_TCD_PARAMETERS_PATH to an explicit JSON file path.
        var explicitOverridePath = Environment.GetEnvironmentVariable("STALKER2_TCD_PARAMETERS_PATH");
        string json;

        if (!string.IsNullOrWhiteSpace(explicitOverridePath))
        {
            var resolvedOverridePath = Path.GetFullPath(explicitOverridePath);
            if (!File.Exists(resolvedOverridePath))
                throw new FileNotFoundException(
                    $"STALKER2_TCD_PARAMETERS_PATH points to a missing file: {resolvedOverridePath}",
                    resolvedOverridePath);

            json = File.ReadAllText(resolvedOverridePath);
        }
        else
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedCatalogName)
                               ?? throw new FileNotFoundException(
                                   "The embedded parameter catalog was not found.");
            using var reader = new StreamReader(stream);
            json = reader.ReadToEnd();
        }

        var catalog = JsonSerializer.Deserialize<ParameterCatalog>(json, Options)
                      ?? throw new InvalidDataException("parameters.json could not be parsed.");

        Validate(catalog);
        return catalog;
    }

    private static void Validate(ParameterCatalog catalog)
    {
        if (catalog.Parameters.Count == 0)
            throw new InvalidDataException("The parameter catalog contains no parameters.");

        if (catalog.DifficultyProfiles.Count == 0)
            throw new InvalidDataException("The parameter catalog contains no difficulty profiles.");

        if (!catalog.DifficultyProfiles.ContainsKey(catalog.App.DefaultReferenceDifficulty))
        {
            throw new InvalidDataException(
                $"Default Base Preset '{catalog.App.DefaultReferenceDifficulty}' has no difficulty profile.");
        }

        var tabIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tab in catalog.Tabs)
        {
            if (string.IsNullOrWhiteSpace(tab.Id) || !tabIds.Add(tab.Id))
                throw new InvalidDataException($"Duplicate or empty tab id: {tab.Id}");
            if (string.IsNullOrWhiteSpace(tab.Label))
                throw new InvalidDataException($"Tab {tab.Id} has no label.");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parametersById = new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in catalog.Parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Id) || !ids.Add(parameter.Id))
                throw new InvalidDataException($"Duplicate or empty parameter id: {parameter.Id}");

            if (string.IsNullOrWhiteSpace(parameter.Tab) || !tabIds.Contains(parameter.Tab))
                throw new InvalidDataException($"Parameter {parameter.Id} references unknown tab '{parameter.Tab}'.");

            ValidateParameterShape(parameter);
            parametersById[parameter.Id] = parameter;
        }

        foreach (var parameter in catalog.Parameters.Where(p => !string.IsNullOrWhiteSpace(p.RequiresToggle)))
        {
            if (string.Equals(parameter.Id, parameter.RequiresToggle, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Parameter {parameter.Id} cannot depend on itself.");

            if (!parametersById.TryGetValue(parameter.RequiresToggle, out var parent))
            {
                throw new InvalidDataException(
                    $"Parameter {parameter.Id} requires unknown toggle {parameter.RequiresToggle}.");
            }

            if (!string.Equals(parent.Control, "toggle", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Parameter {parameter.Id} requires {parameter.RequiresToggle}, but that parameter is not a toggle.");
            }

            if (parameter.ExposeInUi && !parent.ExposeInUi)
            {
                throw new InvalidDataException(
                    $"Visible parameter {parameter.Id} depends on hidden toggle {parameter.RequiresToggle}.");
            }
        }

        var constraintIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var constrainedParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var constraint in catalog.SliderConstraints)
        {
            if (string.IsNullOrWhiteSpace(constraint.Id) || !constraintIds.Add(constraint.Id))
                throw new InvalidDataException($"Duplicate or empty slider constraint id: {constraint.Id}");

            if (constraint.Order is not ("nondecreasing" or "nonincreasing"))
                throw new InvalidDataException($"Slider constraint {constraint.Id} has unsupported order '{constraint.Order}'.");

            if (constraint.ParameterIds.Count < 2)
                throw new InvalidDataException($"Slider constraint {constraint.Id} must contain at least two parameters.");

            var members = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameterId in constraint.ParameterIds)
            {
                if (!members.Add(parameterId))
                    throw new InvalidDataException($"Slider constraint {constraint.Id} contains duplicate parameter {parameterId}.");

                if (!parametersById.TryGetValue(parameterId, out var parameter))
                    throw new InvalidDataException($"Slider constraint {constraint.Id} references unknown parameter {parameterId}.");

                if (!string.Equals(parameter.Control, "slider", StringComparison.Ordinal))
                    throw new InvalidDataException($"Slider constraint {constraint.Id} references non-slider parameter {parameterId}.");

                if (!constrainedParameters.Add(parameterId))
                    throw new InvalidDataException($"Parameter {parameterId} belongs to more than one slider constraint.");
            }

            ValidateVanillaConstraint(constraint, parametersById, catalog.DifficultyProfiles.Keys);
        }
    }

    private static void ValidateParameterShape(ParameterDefinition parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter.Label))
            throw new InvalidDataException($"Parameter {parameter.Id} has no label.");

        if (parameter.Vanilla.Count == 0)
            throw new InvalidDataException($"Parameter {parameter.Id} has no vanilla values.");

        switch (parameter.Control)
        {
            case "slider":
                if (parameter.Slider is null)
                    throw new InvalidDataException($"Slider parameter {parameter.Id} has no slider definition.");
                if (parameter.Slider.Max < parameter.Slider.Min)
                    throw new InvalidDataException($"Slider parameter {parameter.Id} has max below min.");
                if (parameter.Slider.Step <= 0)
                    throw new InvalidDataException($"Slider parameter {parameter.Id} has a non-positive step.");

                foreach (var pair in parameter.Vanilla)
                {
                    if (pair.Value.ValueKind != JsonValueKind.Number)
                        throw new InvalidDataException($"Slider parameter {parameter.Id} has non-numeric vanilla value for {pair.Key}.");

                    var value = pair.Value.GetDouble();
                    if (value < parameter.Slider.Min - 0.000001 || value > parameter.Slider.Max + 0.000001)
                    {
                        throw new InvalidDataException(
                            $"Slider parameter {parameter.Id} vanilla {pair.Key}={value} lies outside " +
                            $"[{parameter.Slider.Min}, {parameter.Slider.Max}].");
                    }
                }
                break;

            case "range":
                if (parameter.Slider is null)
                    throw new InvalidDataException($"Range parameter {parameter.Id} has no slider definition.");
                if (parameter.Slider.Max < parameter.Slider.Min)
                    throw new InvalidDataException($"Range parameter {parameter.Id} has max below min.");
                if (parameter.Slider.Step <= 0)
                    throw new InvalidDataException($"Range parameter {parameter.Id} has a non-positive step.");

                foreach (var pair in parameter.Vanilla)
                {
                    if (pair.Value.ValueKind != JsonValueKind.Number)
                        throw new InvalidDataException($"Range parameter {parameter.Id} has non-numeric vanilla value for {pair.Key}.");

                    var value = pair.Value.GetDouble();
                    if (value < parameter.Slider.Min - 0.000001 || value > parameter.Slider.Max + 0.000001)
                    {
                        throw new InvalidDataException(
                            $"Range parameter {parameter.Id} vanilla {pair.Key}={value} lies outside " +
                            $"[{parameter.Slider.Min}, {parameter.Slider.Max}].");
                    }
                }
                break;

            case "toggle":
                foreach (var pair in parameter.Vanilla)
                {
                    if (pair.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                        throw new InvalidDataException($"Toggle parameter {parameter.Id} has non-boolean vanilla value for {pair.Key}.");
                }
                break;

            case "choiceSlider":
                if (parameter.Vanilla.Values.Any(v => v.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined))
                    throw new InvalidDataException($"Choice parameter {parameter.Id} contains an empty vanilla choice.");
                break;

            default:
                throw new InvalidDataException($"Parameter {parameter.Id} uses unsupported control type '{parameter.Control}'.");
        }

        if (parameter.Support.Status is not ("confirmed" or "runtime_test_required"))
        {
            throw new InvalidDataException(
                $"Parameter {parameter.Id} has unsupported production status '{parameter.Support.Status}'.");
        }
    }

    private static void ValidateVanillaConstraint(
        SliderConstraintDefinition constraint,
        IReadOnlyDictionary<string, ParameterDefinition> parametersById,
        IEnumerable<string> difficulties)
    {
        const double epsilon = 0.000001;

        foreach (var difficulty in difficulties)
        {
            var values = constraint.ParameterIds
                .Select(id => ResolveVanillaNumber(parametersById[id], difficulty))
                .ToList();

            for (var i = 1; i < values.Count; i++)
            {
                var valid = constraint.Order == "nondecreasing"
                    ? values[i - 1] <= values[i] + epsilon
                    : values[i - 1] + epsilon >= values[i];

                if (!valid)
                {
                    throw new InvalidDataException(
                        $"Slider constraint {constraint.Id} is invalid in vanilla {difficulty}: " +
                        $"{constraint.ParameterIds[i - 1]}={values[i - 1]} and {constraint.ParameterIds[i]}={values[i]}.");
                }
            }
        }
    }

    private static double ResolveVanillaNumber(ParameterDefinition parameter, string difficulty)
    {
        if (!parameter.Vanilla.TryGetValue(difficulty, out var value)
            && !parameter.Vanilla.TryGetValue("All", out value))
        {
            value = parameter.Vanilla.Values.FirstOrDefault();
        }

        if (value.ValueKind != JsonValueKind.Number)
            throw new InvalidDataException($"Slider parameter {parameter.Id} does not have a numeric vanilla value.");

        return value.GetDouble();
    }

}
