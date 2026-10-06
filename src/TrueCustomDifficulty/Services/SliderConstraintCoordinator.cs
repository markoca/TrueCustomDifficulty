using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Keeps explicitly related sliders in a valid semantic order using their actual
/// parameter/CFG values. The slider the user moves is authoritative; neighboring
/// sliders move only as far as required to preserve the configured relation.
/// </summary>
public sealed class SliderConstraintCoordinator
{
    private const double Epsilon = 0.000001;

    private readonly List<RuntimeConstraint> _constraints = new();
    private readonly Dictionary<string, List<RuntimeConstraint>> _constraintsByParameterId = new(StringComparer.Ordinal);
    private bool _isApplying;

    public SliderConstraintCoordinator(
        IEnumerable<SliderConstraintDefinition> definitions,
        IEnumerable<ParameterViewModel> parameters)
    {
        var parametersById = parameters.ToDictionary(parameter => parameter.Id, StringComparer.Ordinal);

        foreach (var definition in definitions)
        {
            var members = definition.ParameterIds
                .Select(id => new RuntimeMember(parametersById[id]))
                .ToList();

            var runtime = new RuntimeConstraint(
                definition.Id,
                string.Equals(definition.Order, "nondecreasing", StringComparison.Ordinal),
                members);

            _constraints.Add(runtime);
            foreach (var member in members)
            {
                if (!_constraintsByParameterId.TryGetValue(member.Parameter.Id, out var groups))
                {
                    groups = new List<RuntimeConstraint>();
                    _constraintsByParameterId[member.Parameter.Id] = groups;
                }

                groups.Add(runtime);
            }
        }
    }

    public void ApplyFrom(ParameterViewModel changedParameter)
    {
        if (_isApplying || !_constraintsByParameterId.TryGetValue(changedParameter.Id, out var constraints))
            return;

        _isApplying = true;
        try
        {
            foreach (var constraint in constraints)
            {
                var activeIndex = constraint.Members.FindIndex(member => member.Parameter.Id == changedParameter.Id);
                if (activeIndex >= 0)
                    ApplyFrom(constraint, activeIndex);
            }
        }
        finally
        {
            _isApplying = false;
        }
    }

    /// <summary>
    /// Deterministically repairs an imported/bulk state. The first member is preserved and
    /// each following member is moved only when required to restore the configured order.
    /// </summary>
    public void NormalizeAll()
    {
        if (_isApplying)
            return;

        _isApplying = true;
        try
        {
            foreach (var constraint in _constraints)
                Normalize(constraint);
        }
        finally
        {
            _isApplying = false;
        }
    }

    private static void ApplyFrom(RuntimeConstraint constraint, int activeIndex)
    {
        ClampActiveToFeasibleRange(constraint, activeIndex);

        // Propagate toward lower-index neighbors first, then higher-index neighbors.
        // Only a violated relation moves; valid spacing is preserved exactly.
        for (var i = activeIndex - 1; i >= 0; i--)
        {
            var left = constraint.Members[i];
            var right = constraint.Members[i + 1];

            if (constraint.IsNondecreasing)
            {
                if (left.Value > right.Value + Epsilon)
                    SetAtMost(left, right.Value);
            }
            else if (left.Value + Epsilon < right.Value)
            {
                SetAtLeast(left, right.Value);
            }
        }

        for (var i = activeIndex + 1; i < constraint.Members.Count; i++)
        {
            var left = constraint.Members[i - 1];
            var right = constraint.Members[i];

            if (constraint.IsNondecreasing)
            {
                if (right.Value + Epsilon < left.Value)
                    SetAtLeast(right, left.Value);
            }
            else if (right.Value > left.Value + Epsilon)
            {
                SetAtMost(right, left.Value);
            }
        }
    }

    private static void ClampActiveToFeasibleRange(RuntimeConstraint constraint, int activeIndex)
    {
        var active = constraint.Members[activeIndex];
        var lowerBound = active.MinimumValue;
        var upperBound = active.MaximumValue;

        if (constraint.IsNondecreasing)
        {
            // Left-side members must be able to stay <= active.
            for (var i = 0; i <= activeIndex; i++)
                lowerBound = Math.Max(lowerBound, constraint.Members[i].MinimumValue);

            // Right-side members must be able to stay >= active.
            for (var i = activeIndex; i < constraint.Members.Count; i++)
                upperBound = Math.Min(upperBound, constraint.Members[i].MaximumValue);
        }
        else
        {
            // Left-side members must be able to stay >= active.
            for (var i = 0; i <= activeIndex; i++)
                upperBound = Math.Min(upperBound, constraint.Members[i].MaximumValue);

            // Right-side members must be able to stay <= active.
            for (var i = activeIndex; i < constraint.Members.Count; i++)
                lowerBound = Math.Max(lowerBound, constraint.Members[i].MinimumValue);
        }

        if (active.Value < lowerBound - Epsilon)
            SetAtLeast(active, lowerBound);
        else if (active.Value > upperBound + Epsilon)
            SetAtMost(active, upperBound);
    }

    private static void Normalize(RuntimeConstraint constraint)
    {
        ClampActiveToFeasibleRange(constraint, 0);

        for (var i = 1; i < constraint.Members.Count; i++)
        {
            var left = constraint.Members[i - 1];
            var right = constraint.Members[i];

            if (constraint.IsNondecreasing)
            {
                if (right.Value + Epsilon < left.Value)
                    SetAtLeast(right, left.Value);
            }
            else if (right.Value > left.Value + Epsilon)
            {
                SetAtMost(right, left.Value);
            }
        }
    }

    private static void SetAtMost(RuntimeMember member, double upperBound)
    {
        member.Value = upperBound;

        var guard = 0;
        while (member.Value > upperBound + Epsilon
               && member.CanNudgeValueDown
               && guard++ < 8)
        {
            member.NudgeValueDown();
        }
    }

    private static void SetAtLeast(RuntimeMember member, double lowerBound)
    {
        member.Value = lowerBound;

        var guard = 0;
        while (member.Value + Epsilon < lowerBound
               && member.CanNudgeValueUp
               && guard++ < 8)
        {
            member.NudgeValueUp();
        }
    }

    private sealed class RuntimeMember
    {
        public RuntimeMember(ParameterViewModel parameter)
        {
            Parameter = parameter;
        }

        public ParameterViewModel Parameter { get; }

        public double Value
        {
            get => Parameter.CurrentNumber;
            set => Parameter.CurrentNumber = value;
        }

        public double MinimumValue => Parameter.Minimum;
        public double MaximumValue => Parameter.Maximum;

        public bool CanNudgeValueDown => Parameter.CurrentNumber > Parameter.Minimum + Epsilon;
        public bool CanNudgeValueUp => Parameter.CurrentNumber < Parameter.Maximum - Epsilon;

        public void NudgeValueDown()
        {
            var tick = Math.Max(Parameter.TickFrequency, Epsilon);
            Parameter.CurrentNumber -= tick;
        }

        public void NudgeValueUp()
        {
            var tick = Math.Max(Parameter.TickFrequency, Epsilon);
            Parameter.CurrentNumber += tick;
        }
    }

    private sealed record RuntimeConstraint(
        string Id,
        bool IsNondecreasing,
        List<RuntimeMember> Members);
}
