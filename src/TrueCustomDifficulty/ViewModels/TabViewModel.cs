using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using TrueCustomDifficulty.Infrastructure;

namespace TrueCustomDifficulty.ViewModels;

public sealed class TabViewModel : ObservableObject
{
    private ParameterViewModel? _selectedParameter;

    public string Id { get; }
    public string Label { get; }
    public ObservableCollection<ParameterViewModel> Parameters { get; }
    public ICollectionView ParametersView { get; }

    public string TabSummary => GetTabSummary(Id);

    public ParameterViewModel? SelectedParameter
    {
        get => _selectedParameter;
        set
        {
            if (ReferenceEquals(_selectedParameter, value))
                return;

            if (_selectedParameter is not null)
                _selectedParameter.IsSelected = false;

            if (SetProperty(ref _selectedParameter, value) && _selectedParameter is not null)
                _selectedParameter.IsSelected = true;
        }
    }

    public TabViewModel(string id, string label, IEnumerable<ParameterViewModel> parameters)
    {
        Id = id;
        Label = label;
        // Explicit order of the four visible Combat sections.
        var ordered = Id == "combat"
            ? parameters.OrderBy(p => p.Group switch
            {
                "Your Weapons" => 0,
                "Human Enemies" => 1,
                "Mutant Combat" => 2,
                "Mutant Health" => 3,
                _ => 4
            }).ToList()
            : parameters.ToList();
        Parameters = new ObservableCollection<ParameterViewModel>(ordered);
        ParametersView = CollectionViewSource.GetDefaultView(Parameters);
        if (Id == "combat")
            ParametersView.Filter = item => item is ParameterViewModel p
                && !p.Id.StartsWith("combat.psy", StringComparison.Ordinal)
                && !p.Id.StartsWith("combat.accumulatedDamage", StringComparison.Ordinal);

        // Tabs with multiple distinct CFG domains are grouped visibly so unrelated mechanics are not presented as one undifferentiated list.
        if (Id is "loot" or "combat" or "survival" or "health" or "stamina" or "economy" or "equipment" or "environment" or "alife" or "master")
            ParametersView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ParameterViewModel.Group)));

        SelectedParameter = Parameters.FirstOrDefault(p => p.IsControlVisible);
    }

    public void EnsureVisibleSelection()
    {
        if (SelectedParameter?.IsControlVisible == true)
            return;

        SelectedParameter = Parameters.FirstOrDefault(p => p.IsControlVisible);
    }

    private static string GetTabSummary(string id) => id switch
    {
        "combat" => "Enemy toughness and damage, attack pressure, player lethality, mutant danger and explosions.",
        "survival" => "Hunger, fatigue, sleeping, radiation and bleeding.",
        "equipment" => "Armor durability, weapon wear and the weight of ammunition and consumables.",
        "health" => "Player health, recovery and direct medical treatment.",
        "stamina" => "Stamina pool and recovery, action costs, movement penalties, vaulting and carry capacity.",
        "loot" => "Corpse, stash, ammo, weapon-condition and mutant drop amounts while preserving Difficulty and Progression Rank differences.",
        "economy" => "Starting coupons, trader pricing, item and service costs, repair by reputation and quest coupon income.",
        "environment" => "Global anomaly damage, 11 individual damage sources, Flycatcher slowdown and three special effects; no weather/emission timing.",
        "alife" => "Human and mutant encounter group-size caps by player rank; world population and spawn timing are unchanged.",
        "master" => "Optional Master-only punishment bundles for starvation, severe fatigue, radiation, low health and medical effectiveness.",
        _ => "Independent controls built from the vanilla configuration."
    };
}
