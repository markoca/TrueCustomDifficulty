using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// NEED BOOST: one compact 270-degree knob beside BONUS PACKS. It
/// stretches the need-driven span of the selected corpse SmartLoot packages
/// (MinSpawnChance 10% → MaxSpawnChance): 0x = flat 10% (no need scaling),
/// 1x = vanilla, 2x = doubled span. The checkbox switches the whole boost
/// ON/OFF: OFF = vanilla span, the knob value stays saved and dimmed.
/// Preset sidecars, not catalog parameters.
/// </summary>
public sealed partial class AggregateVisualControl
{
    private const double NeedBoostMaximum = 2.0;
    private const double NeedBoostStep = 0.05;

    private Rect _npcBoostKnobHit = Rect.Empty;
    private bool _npcBoostDragging;
    private bool _npcBoostFocused;
    private bool _npcBoostHover;

    public static readonly DependencyProperty NeedBoostProperty =
        DependencyProperty.Register(nameof(NeedBoost), typeof(double),
            typeof(AggregateVisualControl),
            new FrameworkPropertyMetadata(1.0,
                FrameworkPropertyMetadataOptions.AffectsRender
                | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public double NeedBoost
    {
        get => (double)GetValue(NeedBoostProperty);
        set => SetCurrentValue(NeedBoostProperty, SnapNeedBoost(value));
    }

    public static readonly DependencyProperty NeedBoostEnabledProperty =
        DependencyProperty.Register(nameof(NeedBoostEnabled), typeof(bool),
            typeof(AggregateVisualControl),
            new FrameworkPropertyMetadata(true,
                FrameworkPropertyMetadataOptions.AffectsRender
                | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool NeedBoostEnabled
    {
        get => (bool)GetValue(NeedBoostEnabledProperty);
        set => SetCurrentValue(NeedBoostEnabledProperty, value);
    }

    private Rect _npcBoostCheckbox = Rect.Empty;
    private bool _npcBoostHoverCheckbox;

    // The knob edits only while the boost is ON (checkbox) and editable.
    private bool NpcNeedBoostKnobActive() => NpcNeedBoostEditable() && NeedBoostEnabled;

    private static double SnapNeedBoost(double value)
        => Math.Round(Math.Clamp(double.IsFinite(value) ? value : 1.0, 0.0, NeedBoostMaximum)
            / NeedBoostStep, MidpointRounding.AwayFromZero) * NeedBoostStep;

    // Active only while at least one package is on: with no package the
    // value stays saved but has nothing to shape.
    private bool NpcNeedBoostEditable()
        => Family == "NpcLootLoadout" && IsEnabled && ModItemsEditorAvailable
            && NpcCorpsePackages.Parse(P("loot.npcCorpseBonus")?.CurrentText).Count > 0;

    private void DrawNpcNeedBoostCard(
        DrawingContext dc, Rect card, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        var editable = NpcNeedBoostEditable();
        var enabled = editable && NeedBoostEnabled;
        var boost = SnapNeedBoost(NeedBoost);
        var value = boost.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "×";
        DrawNpcCompactDial(dc, card, "NEED BOOST", value,
            "FLAT", "2×", 0.0, NeedBoostMaximum, boost, 1.0, NeedBoostStep,
            enabled, enabled && Math.Abs(boost - 1.0) > Epsilon,
            enabled && _npcBoostHover,
            text, muted, border, accent, panel,
            captionInset: 29.0);
        // Same checkbox chrome and position as MOD LOOT SCARCITY.
        _npcBoostCheckbox = new Rect(card.Left + 9.0, card.Top + 10.0, 16.0, 16.0);
        BooleanCheckboxChrome.Draw(dc, _npcBoostCheckbox, NeedBoostEnabled,
            editable, editable && _npcBoostHoverCheckbox, panel, border, accent);
        _npcBoostKnobHit = NpcCompactDialHit(card);
    }

    private void SelectNpcNeedBoost()
    {
        _npcBoostFocused = true;
        _npcModFocused = false;
        _npcConsumableFocused = null;
        _selectedParameter = null;
        _selectedHitKind = null;
        if (P("loot.npcCorpseBonus") is { } anchor)
            RaiseParameterSelected(anchor);
    }

    private void UpdateNpcNeedBoost(Point point)
    {
        // Same 270-degree mapping as the other NPC knobs; a click in the
        // dial centre selects without changing the value.
        var center = _npcBoostKnobHit.Location
            + new Vector(_npcBoostKnobHit.Width / 2.0, _npcBoostKnobHit.Height / 2.0);
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        if (dx * dx + dy * dy < 20.0 * 20.0)
            return;
        var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        var clockwise = (angle - WeaponKnobStartAngle + 360.0) % 360.0;
        var ratio = Math.Clamp(clockwise > WeaponKnobSweepAngle
            ? clockwise < 315.0 ? 1.0 : 0.0
            : clockwise / WeaponKnobSweepAngle, 0.0, 1.0);
        NeedBoost = ratio * NeedBoostMaximum;
    }

    private bool NpcNeedBoostMouseDown(Point point)
    {
        if (NpcNeedBoostEditable() && _npcBoostCheckbox.Contains(point))
        {
            SelectNpcNeedBoost();
            NeedBoostEnabled = !NeedBoostEnabled;
            return true;
        }
        if (!NpcNeedBoostEditable() || !_npcBoostKnobHit.Contains(point))
        {
            _npcBoostFocused = false;
            return false;
        }
        SelectNpcNeedBoost();
        if (!NeedBoostEnabled)
            return true;
        _npcBoostDragging = true;
        CaptureMouse();
        UpdateNpcNeedBoost(point);
        return true;
    }

    private bool NpcNeedBoostMouseMove(Point point, MouseButtonState left)
    {
        if (Family != "NpcLootLoadout")
            return false;
        if (_npcBoostDragging && left == MouseButtonState.Pressed)
        {
            UpdateNpcNeedBoost(point);
            return true;
        }
        var hoverBox = NpcNeedBoostEditable() && _npcBoostCheckbox.Contains(point);
        var hover = NpcNeedBoostKnobActive() && _npcBoostKnobHit.Contains(point);
        if (hover != _npcBoostHover || hoverBox != _npcBoostHoverCheckbox)
        {
            _npcBoostHover = hover;
            _npcBoostHoverCheckbox = hoverBox;
            InvalidateVisual();
        }
        if (!hover && !hoverBox)
            return false;
        SetHoverTarget(null);
        Cursor = Cursors.Hand;
        return true;
    }

    private bool NpcNeedBoostMouseUp()
    {
        if (!_npcBoostDragging)
            return false;
        _npcBoostDragging = false;
        ReleaseMouseCapture();
        return true;
    }

    private void NpcNeedBoostLostCapture() => _npcBoostDragging = false;

    private void NpcNeedBoostMouseLeave()
    {
        if (!_npcBoostHover && !_npcBoostHoverCheckbox)
            return;
        _npcBoostHover = false;
        _npcBoostHoverCheckbox = false;
        InvalidateVisual();
    }

    private bool NpcNeedBoostWheel(Point point, int delta)
    {
        if (!NpcNeedBoostKnobActive() || !_npcBoostKnobHit.Contains(point))
            return false;
        var direction = Math.Sign(delta);
        if (direction == 0)
            return true;
        SelectNpcNeedBoost();
        Focus();
        NeedBoost = NeedBoost + direction * NeedBoostStep;
        return true;
    }

    // Right-click restores vanilla 1x (the reference), not OFF.
    private bool NpcNeedBoostRightClick(Point point)
    {
        if (!NpcNeedBoostKnobActive() || !_npcBoostKnobHit.Contains(point))
            return false;
        SelectNpcNeedBoost();
        NeedBoost = 1.0;
        return true;
    }

    private bool NpcNeedBoostKeyDown(Key key)
    {
        if (!_npcBoostFocused || !NpcNeedBoostEditable())
            return false;
        if (key == Key.Space)
        {
            NeedBoostEnabled = !NeedBoostEnabled;
            return true;
        }
        if (key == Key.R)
        {
            NeedBoost = 1.0;
            NeedBoostEnabled = true;
            return true;
        }
        if (!NeedBoostEnabled)
            return false;
        var step = key switch
        {
            Key.Right or Key.Up or Key.Add or Key.OemPlus => 1,
            Key.Left or Key.Down or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };
        if (step == 0 && key is not (Key.Home or Key.End))
            return false;
        NeedBoost = key == Key.Home ? 0.0
            : key == Key.End ? NeedBoostMaximum
            : NeedBoost + step * NeedBoostStep;
        return true;
    }
}
