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

    public string SliderGuide => Id switch
    {
        "loot" => "Difficulty chooses the loot rules; Progression Rank chooses the tier inside those rules. Drop Chance controls affect how often something appears, while MIN/MAX controls affect how much can appear at once. Yellow shows your current setting; vanilla markers show the original game values. Click a marker to snap directly to it; right-click a slider to restore the selected vanilla baseline.",
        "combat" => "Use these controls to shape how dangerous fights feel: enemy toughness, damage, attack frequency, accuracy, repeated-hit protection and explosives. Master-only special rules live in the Master tab. Yellow shows your current setting; click a vanilla marker to snap to it, or right-click any slider to restore vanilla.",
        "survival" => "These controls shape hunger, fatigue, sleep, radiation and bleeding. Rate controls decide how quickly survival states build up, while effect controls decide what those states do to you. Vanilla keeps the status-onset thresholds fixed; Master-only punishment bundles live in the Master tab. Click any vanilla marker under a slider to snap directly to that value.",
        "health" => "Health covers your HP pool, recovery and direct medical treatment. Master-only low-health and medical penalties live in the Master tab. Bleeding and radiation buildup remain in Survival; this tab controls how treatment removes them. Click any vanilla marker under a slider to snap directly to that value.",
        "stamina" => "Stamina covers the size of your stamina bar, base recovery, clear player action costs, water penalties, vault height and carry limits. Internal movement-state coefficients and emergency-recovery helper fields are documented but intentionally kept out of the normal UI. Click a vanilla marker to snap to it; right-click restores the selected baseline.",
        "master" => "These are extra punishment bundles that vanilla Master adds on top of the normal difficulty settings. Switch a bundle ON to reveal and tune its settings. Switch it OFF to disable the bundle and hide its settings.",
        "economy" => "Economy controls starting coupons, trader pricing behavior, item/service costs, repair-by-reputation effects and quest coupon payouts. Some trader pricing rules are only partly exposed by the game files. Vanilla markers remain exact source reference points and can be clicked to snap a slider directly to them.",
        "equipment" => "Equipment controls armor and player-weapon wear plus item-weight tuning. Wear controls change how quickly condition is lost, while weight controls preserve the vanilla differences between individual items. Click a vanilla marker to snap directly to that value.",
        "environment" => "Global Anomaly Damage is the broad damage layer. Individual damage sliders fine-tune specific hazards. Flycatcher Movement/Turn Speed show how much normal speed remains inside the slowdown: higher means less slowdown. Click a vanilla marker to snap to it; right-click restores the selected baseline.",
        _ => "Yellow shows your current setting. Vanilla markers show the original Novice, Stalker, Veteran and Master values; click a marker to snap directly to it. Right-click a slider to restore the selected vanilla baseline."
    };

    public TabViewModel(string id, string label, IEnumerable<ParameterViewModel> parameters)
    {
        Id = id;
        Label = label;
        Parameters = new ObservableCollection<ParameterViewModel>(parameters);
        ParametersView = CollectionViewSource.GetDefaultView(Parameters);

        // Tabs with multiple distinct CFG domains are grouped visibly so unrelated mechanics are not presented as one undifferentiated list.
        if (Id is "loot" or "combat" or "survival" or "health" or "stamina" or "economy" or "equipment" or "alife" or "master")
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
        "combat" => "Enemy toughness and damage, attack pressure, player lethality, repeated-hit protection, mutant danger and explosives.",
        "survival" => "Hunger, fatigue, sleeping, radiation and bleeding behavior.",
        "health" => "Player health, recovery and direct medical treatment.",
        "stamina" => "Stamina pool and recovery, action costs, movement penalties, vaulting and carry capacity.",
        "loot" => "Corpse, stash, ammo, weapon-condition and mutant drop amounts while preserving Difficulty and Progression Rank differences.",
        "economy" => "Starting coupons, trader pricing, item and service costs, repair by reputation and quest coupon income.",
        "equipment" => "Armor and weapon wear plus item-weight tuning.",
        "environment" => "Hazard effect strength only; weather/emission frequency and scheduling remain untouched.",
        "master" => "Optional Master-only punishment bundles for starvation, severe fatigue, radiation, low health and medical effectiveness.",
        _ => "Independent controls built from the vanilla configuration."
    };
}
