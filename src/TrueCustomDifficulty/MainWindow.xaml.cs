using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TrueCustomDifficulty.Controls;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty;

public partial class MainWindow : Window
{
    private const double AspectRatio = 16.0 / 10.0;
    private const int WmSizing = 0x0214;
    private const int WmSysCommand = 0x0112;
    private const int ScMaximize = 0xF030;

    private bool _isAspectMaximized;
    private Rect _normalAspectBounds;

    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += (_, _) =>
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
                source.AddHook(WindowProc);
        };

        try
        {
            var catalog = new ParameterCatalogService().Load();
            DataContext = new MainViewModel(catalog);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"The application could not load its parameter catalog.\n\n{ex.Message}",
                "True Custom Difficulty",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Close();
        }
    }

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeRestoreWindow_Click(object sender, RoutedEventArgs e)
        => ToggleAspectMaximize();

    private void ToggleAspectMaximize()
    {
        if (_isAspectMaximized)
        {
            Left = _normalAspectBounds.Left;
            Top = _normalAspectBounds.Top;
            Width = _normalAspectBounds.Width;
            Height = _normalAspectBounds.Height;
            _isAspectMaximized = false;
            return;
        }

        _normalAspectBounds = new Rect(Left, Top, Width, Height);

        var work = SystemParameters.WorkArea;
        var targetWidth = work.Width;
        var targetHeight = targetWidth / AspectRatio;

        if (targetHeight > work.Height)
        {
            targetHeight = work.Height;
            targetWidth = targetHeight * AspectRatio;
        }

        Left = work.Left + (work.Width - targetWidth) / 2.0;
        Top = work.Top + (work.Height - targetHeight) / 2.0;
        Width = targetWidth;
        Height = targetHeight;
        _isAspectMaximized = true;
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();



    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmSysCommand && ((int)wParam.ToInt64() & 0xFFF0) == ScMaximize)
        {
            Dispatcher.BeginInvoke(new Action(ToggleAspectMaximize));
            handled = true;
            return IntPtr.Zero;
        }

        if (msg != WmSizing || lParam == IntPtr.Zero)
            return IntPtr.Zero;

        // Manual resizing leaves the custom 16:10 maximized state and becomes
        // the new normal window geometry, while keeping the aspect lock active.
        _isAspectMaximized = false;

        var rect = Marshal.PtrToStructure<NativeRect>(lParam);
        var edge = wParam.ToInt32();

        var width = Math.Max(MinWidth, rect.Right - rect.Left);
        var height = Math.Max(MinHeight, rect.Bottom - rect.Top);

        var proposedRatio = width / height;
        var adjustHeight = edge is 1 or 2 || (edge is 4 or 5 or 7 or 8 && proposedRatio >= AspectRatio);

        if (adjustHeight)
        {
            height = width / AspectRatio;

            if (edge is 4 or 5)
                rect.Top = rect.Bottom - (int)Math.Round(height);
            else
                rect.Bottom = rect.Top + (int)Math.Round(height);
        }
        else
        {
            width = height * AspectRatio;

            if (edge is 1 or 4 or 7)
                rect.Left = rect.Right - (int)Math.Round(width);
            else
                rect.Right = rect.Left + (int)Math.Round(width);
        }

        Marshal.StructureToPtr(rect, lParam, fDeleteOld: false);
        handled = true;
        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }


    private void ParameterCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => SelectParameterFromElement(sender);

    private void Radar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => SelectParameterFromElement(sender);

    private void SelectParameterFromElement(object sender)
    {
        if (sender is not FrameworkElement { DataContext: ParameterViewModel parameter })
            return;

        if (DataContext is MainViewModel main && main.SelectedTab is not null)
            main.SelectedTab.SelectedParameter = parameter;
    }

    private void CurrentValueBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ParameterViewModel parameter } || !parameter.IsSlider)
            return;

        var direction = e.Delta > 0 ? 1.0 : -1.0;
        parameter.CurrentNumber = Math.Clamp(
            parameter.CurrentNumber + direction * parameter.TickFrequency,
            parameter.Minimum,
            parameter.Maximum);

        // The value box owns the wheel gesture; do not bubble it to the tab ScrollViewer.
        e.Handled = true;
    }

    private void SliderControl_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ParameterViewModel parameter })
        {
            parameter.ResetToReference();
            e.Handled = true;
        }
    }

    private void VanillaMarkerControl_MarkerClicked(object sender, VanillaMarkerClickedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ParameterViewModel parameter } markerBar)
            return;

        // Assign through the same view-model properties used by the sliders themselves.
        // This means normal slider constraints/cascades still run for click-to-snap changes.
        switch (markerBar.Tag as string)
        {
            case "choice":
                parameter.ChoiceIndex = e.Value;
                break;
            case "rangeMin":
                parameter.SnapCurrentMinNumberToMarker(e.Value);
                break;
            case "rangeMax":
                parameter.SnapCurrentMaxNumberToMarker(e.Value);
                break;
            default:
                parameter.SnapCurrentNumberToMarker(e.Value);
                break;
        }
    }
}
