using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
            
            // Wire up global exception handlers BEFORE initialization
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Application_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            
            LogStartupTime("Application OnStartup started");

            // Initialize services
            _settingsService = new SettingsService();
            LogStartupTime("SettingsService initialized");
            
            _loggingService = new LoggingService(_settingsService);
            LogStartupTime("LoggingService initialized");

            var appSettings = _settingsService.GetSettings();
            LogSystemInfo();
            
            _updateCheckService = new UpdateCheckService(
                _loggingService,
                appSettings.UpdateGitHubOwner,
                appSettings.UpdateGitHubRepo);
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
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
                
                if (UpdateCheckService.IsNewerVersion(currentVersion, latestRelease.TagName ?? ""))
                {
                    MainWindow?.Dispatcher.Invoke(() =>
                    {
                        var releaseLabel = latestRelease.TagName ?? latestRelease.Name ?? "latest";
                        var body = latestRelease.Body ?? string.Empty;
                        var preview = body.Length > 700 ? body[..700] + "\n\n..." : body;

                        var result = MessageBox.Show(
                            $"A new version of DaisyView is available: {releaseLabel}\n\n" +
                            "Yes = Download and update now\n" +
                            "No = View release notes\n" +
                            "Cancel = Remind me later\n\n" +
                            preview,
                            "Update Available",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            var downloadUrl = _updateCheckService.GetPreferredDownloadUrl(latestRelease);
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(downloadUrl) 
                            { 
                                UseShellExecute = true 
                            });

                            var closeForUpdate = MessageBox.Show(
                                "The update download has started.\n\nClose DaisyView now so the installer can replace files?",
                                "Close For Update",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (closeForUpdate == MessageBoxResult.Yes)
                            {
                                Shutdown();
                            }
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            var releasePageUrl = _updateCheckService.GetReleasePageUrl(latestRelease.TagName ?? "");
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(releasePageUrl)
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

    /// <summary>
    /// Logs system information at startup for diagnostics
    /// </summary>
    private void LogSystemInfo()
    {
        try
        {
            var osVersion = Environment.OSVersion;
            var processorCount = Environment.ProcessorCount;
            var totalMemory = GC.GetTotalMemory(false) / (1024 * 1024); // MB
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

            _loggingService?.LogInfo(
                "=== System Information === OS: {OSVersion} | Processors: {ProcessorCount} | " +
                "Available Memory: {TotalMemoryMb}MB | DaisyView Version: {AppVersion}",
                osVersion, processorCount, totalMemory, appVersion);
        }
        catch (Exception ex)
        {
            _loggingService?.LogWarning("Failed to log system information: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Handles unhandled exceptions on the UI thread
    /// </summary>
    private void Application_DispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _loggingService?.LogError(
            "=== CRITICAL: UNHANDLED DISPATCHER EXCEPTION - Application will terminate ===",
            e.Exception);

        MessageBox.Show(
            $"An unexpected error occurred and DaisyView must close.\n\n" +
            $"Error: {e.Exception.Message}\n\n" +
            $"Technical details have been logged to:\n" +
            $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DaisyView", "logs")}",
            "Critical Error - DaisyView Closing",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = false; // Allow application to crash (we've logged it)
        Shutdown(1); // Exit with error code 1
    }

    /// <summary>
    /// Handles unhandled exceptions in background tasks
    /// </summary>
    private void TaskScheduler_UnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        _loggingService?.LogError(
            "=== Unhandled Task/Async Exception ===",
            e.Exception);

        // Don't show dialog for cancellation exceptions
        if (!e.Exception.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase))
        {
            MainWindow?.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"A background operation failed: {e.Exception.Message}\n\n" +
                    "The application will continue running, but some features may not work correctly.",
                    "Background Operation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
        }

        e.SetObserved(); // Prevent application crash for task exceptions
    }

    /// <summary>
    /// Handles unhandled exceptions from the CLR runtime
    /// </summary>
    private void CurrentDomain_UnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        _loggingService?.LogError(
            "=== CRITICAL: Unhandled CLR Exception - IsTerminating: {IsTerminating} ===",
            ex,
            e.IsTerminating);

        if (e.IsTerminating)
        {
            MessageBox.Show(
                "A critical system error occurred. DaisyView must close immediately.\n\n" +
                "Please check the log file for details.",
                "Critical System Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
