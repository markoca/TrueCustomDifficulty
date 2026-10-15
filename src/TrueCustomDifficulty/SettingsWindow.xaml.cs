using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty;

public partial class SettingsWindow : Window
{
    private readonly ModDestinationSettingsService _settings = new();
    private readonly string _savedModsPath;

    public SettingsWindow(string initialPath)
    {
        _savedModsPath = initialPath;
        InitializeComponent();
        ModsPathBox.Text = initialPath;
        UpdateOpenFolderButton();
    }

    private void ModsPathBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => UpdateOpenFolderButton();

    private void UpdateOpenFolderButton()
    {
        // Only a valid *saved* folder is available for opening. A newly browsed
        // or typed path must be saved first; no unsaved path is treated as installed.
        if (OpenModsFolderButton is null || ModsPathBox is null)
            return;

        var canOpen = false;
        if (!string.IsNullOrWhiteSpace(_savedModsPath) &&
            string.Equals(ModsPathBox.Text, _savedModsPath, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var configured = _settings.RequireConfiguredPath();
                canOpen = string.Equals(
                    configured,
                    ModDestinationSettingsService.ValidatePath(_savedModsPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                // Missing/invalid saved folders must not expose the Open action.
            }
        }

        OpenModsFolderButton.Visibility = canOpen ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenModsFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.Equals(ModsPathBox.Text, _savedModsPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Save the selected ~mods folder first.");

            var savedFolder = _settings.RequireConfiguredPath();
            if (!string.Equals(
                savedFolder,
                ModDestinationSettingsService.ValidatePath(_savedModsPath),
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The saved ~mods folder changed. Reopen Settings.");

            // Let the OS (or Wine's Windows shell) handle the directory.
            // ShellExecute may reuse an existing Explorer process and return null
            // even when opening the folder succeeds. Exceptions report failures.
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = savedFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or InvalidDataException or Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(this, ex.Message, "Open ~mods folder",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateOpenFolderButton();
        }
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
