using System.Windows;
using System.Windows.Input;
using TrueCustomDifficulty.Controls;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

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
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();



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
