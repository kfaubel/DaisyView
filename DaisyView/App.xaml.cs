using System;
using System.Diagnostics;
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
    private Stopwatch? _startupTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        _startupTimer = Stopwatch.StartNew();
        try
        {
            base.OnStartup(e);
            LogStartupTime("Application OnStartup started");

            // Initialize services
            _settingsService = new SettingsService();
            LogStartupTime("SettingsService initialized");
            
            _loggingService = new LoggingService(_settingsService);
            LogStartupTime("LoggingService initialized");
            
            _updateCheckService = new UpdateCheckService(_loggingService);
            LogStartupTime("UpdateCheckService initialized");

            _loggingService.LogInfo("=== DaisyView Application Started ===");

            // Apply theme
            ApplyTheme();
            LogStartupTime("Theme applied");

            // Create and show the main window explicitly so startup does not depend on StartupUri.
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
            LogStartupTime("Main window shown");

            // Check for updates on startup
            _ = CheckForUpdatesAsync();
            LogStartupTime("Update check initiated");
        }
        catch (Exception ex)
        {
            _loggingService?.LogError("Failed to initialize application during startup", ex);
            var errorMessage = ex.InnerException != null
                ? $"Failed to initialize application: {ex.Message}\n\nInner exception: {ex.InnerException.Message}"
                : $"Failed to initialize application: {ex.Message}";
            MessageBox.Show(errorMessage, "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void LogStartupTime(string message)
    {
        if (_startupTimer != null)
        {
            _loggingService?.LogInfo("[STARTUP] {Message} - {ElapsedMs}ms", message, _startupTimer.ElapsedMilliseconds);
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
    /// Checks for updates asynchronously without blocking UI startup
    /// </summary>
    private async Task CheckForUpdatesAsync()
    {
        if (_updateCheckService == null)
            return;

        try
        {
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
        catch (Exception ex)
        {
            _loggingService?.LogWarning("Error during update check: {Message}", ex.Message);
        }
    }
}
