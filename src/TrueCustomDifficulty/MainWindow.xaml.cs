using System.IO;
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
    private const double AspectRatio = 16.0 / 9.0;
    private const double StartupWorkAreaFraction = 0.94;
    private const int WmSizing = 0x0214;
    private const int WmSysCommand = 0x0112;
    private const int ScMaximize = 0xF030;

    private bool _isAspectMaximized;
    private Rect _normalAspectBounds;

    public MainWindow()
    {
        InitializeComponent();

        // The worker owns temporary staging and an atomic install. Do not let
        // closing the window interrupt a live external repak/retoc process.
        Closing += (_, e) =>
        {
            if (DataContext is MainViewModel { IsBuilding: true })
                e.Cancel = true;
        };

        SourceInitialized += (_, _) =>
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
                source.AddHook(WindowProc);

            // The XAML default is 1600x900. Fit it to smaller work areas while
            // keeping 16:9 intact rather than letting the taskbar clip the UI.
            FitInitialWindowToWorkArea();
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

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel { IsBuilding: true })
            return;

        try
        {
            var settings = new ModDestinationSettingsService();
            var dialog = new SettingsWindow(settings.LoadPath()) { Owner = this };
            dialog.ShowDialog();
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(
                $"Settings could not be opened.\n\n{ex.Message}",
                "True Custom Difficulty",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void FitInitialWindowToWorkArea()
    {
        var work = SystemParameters.WorkArea;
        if (work.Width <= 0.0 || work.Height <= 0.0)
            return;

        // Width, Height and work-area coordinates are WPF DIPs. The same
        // ratio lock is applied to physical pixels during WM_SIZING.
        var usableWidth = Math.Max(MinWidth, work.Width * StartupWorkAreaFraction);
        var usableHeight = Math.Max(MinHeight, work.Height * StartupWorkAreaFraction);
        var width = Math.Clamp(
            Math.Min(Width, Math.Min(usableWidth, usableHeight * AspectRatio)),
            MinWidth,
            Width);
        var height = width / AspectRatio;

        Width = width;
        Height = height;
        Left = work.Left + (work.Width - width) / 2.0;
        Top = work.Top + (work.Height - height) / 2.0;
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

        // Manual resizing leaves the custom 16:9 maximized state and becomes
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

    private void VisualControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => SelectParameterFromElement(sender);

    // An aggregate is represented by its anchor in the list, but interacting
    // with a mixer strip should explain that specific parameter at the right.
    private void AggregateVisualControl_ParameterSelected(
        object sender, AggregateParameterSelectedEventArgs e)
    {
        if (DataContext is MainViewModel main && main.SelectedTab is not null
            && main.SelectedTab.Parameters.Contains(e.Parameter))
        {
            main.SelectedTab.SelectedParameter = e.Parameter;
        }
    }

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
