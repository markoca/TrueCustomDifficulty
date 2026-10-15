using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty;

public partial class SettingsWindow : Window
{
    private readonly ModDestinationSettingsService _settings = new();

    public SettingsWindow(string initialPath)
    {
        InitializeComponent();
        ModsPathBox.Text = initialPath;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select the game's ~mods folder" };
        if (Directory.Exists(ModsPathBox.Text))
            dialog.InitialDirectory = ModsPathBox.Text;

        if (dialog.ShowDialog(this) == true)
            ModsPathBox.Text = dialog.FolderName;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.SavePath(ModsPathBox.Text);
            DialogResult = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show(this, ex.Message, "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
