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
        "combat" => "Use these controls to shape how dangerous fights feel: enemy toughness, damage, attack frequency, accuracy and explosions. Master-only special rules live in the Master tab. Yellow shows your current setting; click a vanilla marker to snap to it, or right-click any slider to restore vanilla.",
        "survival" => "These controls shape hunger, fatigue, sleep, radiation and bleeding. Rate controls decide how quickly survival states build up, while effect controls decide what those states do to you. Vanilla keeps the status-onset thresholds fixed; Master-only punishment bundles live in the Master tab. Click any vanilla marker under a slider to snap directly to that value.",
        "health" => "Health covers your HP pool, recovery and direct medical treatment. Master-only low-health and medical penalties live in the Master tab. Bleeding and radiation buildup remain in Survival; this tab controls how treatment removes them. Click any vanilla marker under a slider to snap directly to that value.",
        "stamina" => "Stamina covers the size of your stamina bar, base recovery, clear player action costs, water penalties, vault height and carry limits. Internal movement-state coefficients and emergency-recovery helper fields are documented but intentionally kept out of the normal UI. Click a vanilla marker to snap to it; right-click restores the selected baseline.",
        "master" => "These are extra punishment bundles that vanilla Master adds on top of the normal difficulty settings. Switch a bundle ON to reveal and tune its settings. Switch it OFF to disable the bundle and hide its settings.",
        "environment" => "Anomalies are divided into Global Damage, Individual Damage, Flycatcher Slowdown and Special Effects. Global Damage and individual sources remain independent. Flycatcher handles edit retained movement and turning speed (higher means less slowdown). The center ring edits both axes. Native ranges, reference ticks and per-source resets stay intact.",
        "economy" => "Economy controls starting coupons, trader pricing behavior, item/service costs, repair-by-reputation effects and quest coupon payouts. Some trader pricing rules are only partly exposed by the game files. Vanilla markers remain exact source reference points and can be clicked to snap a slider directly to them.",
        "equipment" => "Equipment Wear uses three Combat-style rings: bottom adjusts armor toughness, left adjusts weapon wear per shot and center adjusts both independently. Item Weight has separate compact ammo and food/medicine sliders. Source values retain their native scales; bonus caps remain audit-only.",
        _ => "Yellow shows your current setting. Vanilla markers show the original Novice, Stalker, Veteran and Master values; click a marker to snap directly to it. Right-click a slider to restore the selected vanilla baseline."
    };

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
        "survival" => "Hunger, fatigue, sleeping, radiation and bleeding behavior.",
        "health" => "Player health, recovery and direct medical treatment.",
        "stamina" => "Stamina pool and recovery, action costs, movement penalties, vaulting and carry capacity.",
        "loot" => "Corpse, stash, ammo, weapon-condition and mutant drop amounts while preserving Difficulty and Progression Rank differences.",
        "economy" => "Starting coupons, trader pricing, item and service costs, repair by reputation and quest coupon income.",
        "equipment" => "Armor and weapon wear plus item-weight tuning.",
        "environment" => "Global anomaly damage, 11 individual damage sources, Flycatcher slowdown and three special effects; no weather/emission timing.",
        "master" => "Optional Master-only punishment bundles for starvation, severe fatigue, radiation, low health and medical effectiveness.",
        _ => "Independent controls built from the vanilla configuration."
    };
}
