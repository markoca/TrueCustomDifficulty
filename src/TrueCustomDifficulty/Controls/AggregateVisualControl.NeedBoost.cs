using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// NEED BOOST: one compact 270-degree knob beside CORPSE SMART LOOT. It
/// stretches the need-driven span of the selected corpse SmartLoot packages
/// (MinSpawnChance 10% → MaxSpawnChance): 0x = off (flat 10%), 1x = vanilla,
/// 2x = doubled span. A preset sidecar, not a catalog parameter.
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
        var enabled = NpcNeedBoostEditable();
        var boost = SnapNeedBoost(NeedBoost);
        var value = boost.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "×";
        DrawNpcCompactDial(dc, card, "NEED BOOST", value,
            "OFF", "2×", 0.0, NeedBoostMaximum, boost, 1.0, NeedBoostStep,
            enabled, enabled && Math.Abs(boost - 1.0) > Epsilon,
            enabled && _npcBoostHover,
            text, muted, border, accent, panel);
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
        if (!NpcNeedBoostEditable() || !_npcBoostKnobHit.Contains(point))
        {
            _npcBoostFocused = false;
            return false;
        }
        SelectNpcNeedBoost();
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
        var hover = NpcNeedBoostEditable() && _npcBoostKnobHit.Contains(point);
        if (hover != _npcBoostHover)
        {
            _npcBoostHover = hover;
            InvalidateVisual();
        }
        if (!hover)
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
        if (!_npcBoostHover)
            return;
        _npcBoostHover = false;
        InvalidateVisual();
    }

    private bool NpcNeedBoostWheel(Point point, int delta)
    {
        if (!NpcNeedBoostEditable() || !_npcBoostKnobHit.Contains(point))
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
        if (!NpcNeedBoostEditable() || !_npcBoostKnobHit.Contains(point))
            return false;
        SelectNpcNeedBoost();
        NeedBoost = 1.0;
        return true;
    }

    private bool NpcNeedBoostKeyDown(Key key)
    {
        if (!_npcBoostFocused || !NpcNeedBoostEditable())
            return false;
        if (key == Key.R)
        {
            NeedBoost = 1.0;
            return true;
        }
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
