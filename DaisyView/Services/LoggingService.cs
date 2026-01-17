using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Manages application logging with configurable levels
/// Logs are written to AppData\Local\DaisyView\logs folder
/// File system operations taking longer than 5 seconds trigger warning-level logs
/// </summary>
public class LoggingService
{
    private readonly string _logsFolder;
    private readonly SettingsService _settingsService;
    private Logger? _logger;

    public LoggingService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _logsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DaisyView",
            "logs");
        
        InitializeLogger();
    }

    /// <summary>
    /// Initializes the Serilog logger with the configured level from settings
    /// </summary>
    private void InitializeLogger()
    {
        Directory.CreateDirectory(_logsFolder);

        var settings = _settingsService.GetSettings();
        var logLevel = ParseLogLevel(settings.LoggingLevel);

        _logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.File(
                Path.Combine(_logsFolder, "daisyview-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Converts string log level to Serilog LogEventLevel
    /// </summary>
    private static LogEventLevel ParseLogLevel(string levelString)
    {
        return levelString.ToLower() switch
        {
            "trace" => LogEventLevel.Verbose,
            "information" => LogEventLevel.Information,
            "info" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "warn" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            _ => LogEventLevel.Information
        };
    }

    /// <summary>
    /// Logs an info-level message for user actions
    /// </summary>
    public void LogUserAction(string action, string? details = null)
    {
        _logger?.Information("User action: {Action}. {Details}", action, details ?? "");
    }

    /// <summary>
    /// Logs a file system operation
    /// </summary>
    public void LogFileSystemOperation(string operation, string filePath, long durationMs)
    {
        if (durationMs > 5000)
        {
            _logger?.Warning("File system operation took longer than 5 seconds: {Operation} on {FilePath}. Duration: {DurationMs}ms",
                operation, filePath, durationMs);
        }
        else
        {
            _logger?.Information("File system operation: {Operation} on {FilePath}. Duration: {DurationMs}ms",
                operation, filePath, durationMs);
        }
    }

    /// <summary>
    /// Logs a trace-level message for less important events
    /// </summary>
    public void LogTrace(string message, params object?[] args)
    {
        _logger?.Verbose(message, args);
    }

    /// <summary>
    /// Logs an informational message
    /// </summary>
    public void LogInfo(string message, params object?[] args)
    {
        _logger?.Information(message, args);
    }

    /// <summary>
    /// Logs a warning message
    /// </summary>
    public void LogWarning(string message, params object?[] args)
    {
        _logger?.Warning(message, args);
    }

    /// <summary>
    /// Logs an error message
    /// </summary>
    public void LogError(string message, Exception? ex = null, params object?[] args)
    {
        if (ex != null)
        {
            _logger?.Error(ex, message, args);
        }
        else
        {
            _logger?.Error(message, args);
        }
    }

    /// <summary>
    /// Disposes the logger and flushes pending logs
    /// </summary>
    public void Dispose()
    {
        _logger?.Dispose();
    }
}
