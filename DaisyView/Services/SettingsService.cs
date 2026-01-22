using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Manages application settings persistence in JSON format
/// Settings are stored in AppData\Local\DaisyView\settings.json
/// </summary>
public class SettingsService
{
    private readonly string _settingsFolder;
    private readonly string _settingsFilePath;
    private AppSettings _currentSettings;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Creates a new SettingsService using the default AppData location
    /// </summary>
    public SettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DaisyView"))
    {
    }

    /// <summary>
    /// Creates a new SettingsService with a custom settings folder path (for testing)
    /// </summary>
    /// <param name="settingsFolder">The folder path where settings.json will be stored</param>
    public SettingsService(string settingsFolder)
    {
        _settingsFolder = settingsFolder;
        _settingsFilePath = Path.Combine(_settingsFolder, "settings.json");
        
        _currentSettings = LoadSettings();
    }

    /// <summary>
    /// Gets the current application settings
    /// </summary>
    public AppSettings GetSettings()
    {
        return _currentSettings;
    }

    /// <summary>
    /// Saves the current settings to disk
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            // Ensure settings folder exists
            Directory.CreateDirectory(_settingsFolder);

            var json = JsonSerializer.Serialize(_currentSettings, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to save settings to {_settingsFilePath}", ex);
        }
    }

    /// <summary>
    /// Loads settings from disk, or creates default settings if file doesn't exist
    /// </summary>
    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            // Log error but continue with defaults
            Console.WriteLine($"Warning: Failed to load settings from {_settingsFilePath}: {ex.Message}");
        }

        // Return default settings
        return new AppSettings();
    }

    /// <summary>
    /// Updates a specific setting
    /// </summary>
    public void UpdateSetting(Action<AppSettings> updateAction)
    {
        updateAction(_currentSettings);
        SaveSettings();
    }

    /// <summary>
    /// Sets the last active folder path
    /// </summary>
    public void SetLastActiveFolderPath(string? path)
    {
        _currentSettings.LastActiveFolderPath = path;
        SaveSettings();
    }

    /// <summary>
    /// Gets the last active folder path
    /// </summary>
    public string? GetLastActiveFolderPath()
    {
        return _currentSettings.LastActiveFolderPath;
    }

    /// <summary>
    /// Adds a folder to favorites
    /// </summary>
    public void AddFavoritFolder(string folderPath)
    {
        if (!_currentSettings.FavoriteFolders.Contains(folderPath))
        {
            _currentSettings.FavoriteFolders.Add(folderPath);
            SaveSettings();
        }
    }

    /// <summary>
    /// Removes a folder from favorites
    /// </summary>
    public void RemoveFavoriteFolder(string folderPath)
    {
        if (_currentSettings.FavoriteFolders.Remove(folderPath))
        {
            SaveSettings();
        }
    }

    /// <summary>
    /// Gets all favorite folders
    /// </summary>
    public List<string> GetFavoriteFolders()
    {
        return _currentSettings.FavoriteFolders;
    }

    /// <summary>
    /// Removes any favorite folders that no longer exist on the filesystem
    /// </summary>
    /// <returns>Number of favorites removed</returns>
    public int CleanupInvalidFavorites()
    {
        var invalidFavorites = _currentSettings.FavoriteFolders
            .Where(path => !Directory.Exists(path))
            .ToList();

        if (invalidFavorites.Count > 0)
        {
            foreach (var path in invalidFavorites)
            {
                _currentSettings.FavoriteFolders.Remove(path);
            }
            SaveSettings();
        }

        return invalidFavorites.Count;
    }

    /// <summary>
    /// Checks if a folder is marked as favorite
    /// </summary>
    public bool IsFavorite(string folderPath)
    {
        return _currentSettings.FavoriteFolders.Contains(folderPath);
    }

    /// <summary>
    /// Saves random order for a folder
    /// </summary>
    public void SaveRandomOrder(string folderPath, List<string> fileOrder)
    {
        _currentSettings.RandomOrderCache[folderPath] = fileOrder;
        SaveSettings();
    }

    /// <summary>
    /// Gets the saved random order for a folder, if any
    /// </summary>
    public List<string>? GetRandomOrder(string folderPath)
    {
        _currentSettings.RandomOrderCache.TryGetValue(folderPath, out var order);
        return order;
    }

    /// <summary>
    /// Clears the random order for a folder
    /// </summary>
    public void ClearRandomOrder(string folderPath)
    {
        _currentSettings.RandomOrderCache.Remove(folderPath);
        _currentSettings.RandomEnabledFolders.Remove(folderPath);
        SaveSettings();
    }

    /// <summary>
    /// Gets the settings file path (useful for manual editing)
    /// </summary>
    public string GetSettingsFilePath()
    {
        return _settingsFilePath;
    }

    /// <summary>
    /// Saves the window size and position
    /// </summary>
    public void SaveWindowPlacement(double width, double height, double left, double top, string windowState)
    {
        _currentSettings.WindowWidth = width;
        _currentSettings.WindowHeight = height;
        _currentSettings.WindowLeft = left;
        _currentSettings.WindowTop = top;
        _currentSettings.WindowState = windowState;
        SaveSettings();
    }

    /// <summary>
    /// Gets the saved window width
    /// </summary>
    public double GetWindowWidth()
    {
        return _currentSettings.WindowWidth;
    }

    /// <summary>
    /// Gets the saved window height
    /// </summary>
    public double GetWindowHeight()
    {
        return _currentSettings.WindowHeight;
    }

    /// <summary>
    /// Gets the saved window left position
    /// </summary>
    public double GetWindowLeft()
    {
        return _currentSettings.WindowLeft;
    }

    /// <summary>
    /// Gets the saved window top position
    /// </summary>
    public double GetWindowTop()
    {
        return _currentSettings.WindowTop;
    }

    /// <summary>
    /// Gets the saved window state
    /// </summary>
    public string GetWindowState()
    {
        return _currentSettings.WindowState;
    }
}
