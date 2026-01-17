using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DaisyView.Services;
using DaisyView.Views;

namespace DaisyView;

/// <summary>
/// Application entry point and initialization
/// Handles theme setup, update checking, and service initialization
/// </summary>
public partial class App : Application
{
    private SettingsService? _settingsService;
    private LoggingService? _loggingService;
    private UpdateCheckService? _updateCheckService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Initialize services
            _settingsService = new SettingsService();
            _loggingService = new LoggingService(_settingsService);
            _updateCheckService = new UpdateCheckService(_loggingService);

            _loggingService.LogInfo("=== DaisyView Application Started ===");

            // Apply theme
            ApplyTheme();

            // Check for updates on startup
            _ = CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize application: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _loggingService?.LogInfo("=== DaisyView Application Shutting Down ===");
        _loggingService?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Applies the theme based on settings
    /// </summary>
    private void ApplyTheme()
    {
        var settings = _settingsService?.GetSettings();
        if (settings == null)
            return;

        bool isDarkMode = settings.Theme switch
        {
            "Dark" => true,
            "Light" => false,
            "System" => SystemParameters.HighContrast || IsSystemDarkMode(),
            _ => false
        };

        ApplyThemeColors(isDarkMode);
    }

    /// <summary>
    /// Determines if the system is in dark mode
    /// </summary>
    private static bool IsSystemDarkMode()
    {
        try
        {
            // Check Windows Registry for dark mode setting
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value != null && (int)value == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Updates the application theme colors
    /// </summary>
    private void ApplyThemeColors(bool isDarkMode)
    {
        var resources = Resources;

        if (isDarkMode)
        {
            resources["BackgroundBrush"] = new SolidColorBrush((Color)resources["DarkBackground"]!);
            resources["ForegroundBrush"] = new SolidColorBrush((Color)resources["DarkForeground"]!);
            resources["BorderBrush"] = new SolidColorBrush((Color)resources["DarkBorder"]!);
            resources["PanelBrush"] = new SolidColorBrush((Color)resources["DarkPanel"]!);
            resources["AccentBrush"] = new SolidColorBrush((Color)resources["DarkAccent"]!);
        }
        else
        {
            resources["BackgroundBrush"] = new SolidColorBrush((Color)resources["LightBackground"]!);
            resources["ForegroundBrush"] = new SolidColorBrush((Color)resources["LightForeground"]!);
            resources["BorderBrush"] = new SolidColorBrush((Color)resources["LightBorder"]!);
            resources["PanelBrush"] = new SolidColorBrush((Color)resources["LightPanel"]!);
            resources["AccentBrush"] = new SolidColorBrush((Color)resources["LightAccent"]!);
        }

        _loggingService?.LogInfo("Theme applied: {Theme}", isDarkMode ? "Dark" : "Light");
    }

    /// <summary>
    /// Checks for updates asynchronously
    /// </summary>
    private async Task CheckForUpdatesAsync()
    {
        if (_updateCheckService == null)
            return;

        var latestRelease = await _updateCheckService.CheckForUpdatesAsync();
        
        if (latestRelease != null)
        {
            var currentVersion = "1.0.0"; // TODO: Read from assembly version
            
            if (UpdateCheckService.IsNewerVersion(currentVersion, latestRelease.TagName ?? ""))
            {
                MainWindow?.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(
                        $"A new version of DaisyView is available: {latestRelease.TagName}\n\n{latestRelease.Body}\n\nWould you like to update?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var downloadUrl = _updateCheckService.GetDownloadUrl(latestRelease.TagName ?? "");
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(downloadUrl) 
                        { 
                            UseShellExecute = true 
                        });
                    }
                });
            }
        }
    }
}
