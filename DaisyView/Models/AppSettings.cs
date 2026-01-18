using System;
using System.Collections.Generic;

namespace DaisyView.Models;

/// <summary>
/// Represents application settings that persist between sessions.
/// Settings are stored in JSON format in AppData\Local\DaisyView
/// </summary>
public class AppSettings
{
    /// <summary>
    /// The theme to use: "Light", "Dark", or "System"
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// The last active folder path
    /// </summary>
    public string? LastActiveFolderPath { get; set; }

    /// <summary>
    /// Logging level: "Trace", "Information", "Warning", "Error"
    /// </summary>
    public string LoggingLevel { get; set; } = "Trace";

    /// <summary>
    /// List of favorite folder paths
    /// </summary>
    public List<string> FavoriteFolders { get; set; } = new();

    /// <summary>
    /// Thumbnail size in pixels: "Small" (100), "Medium" (200), "Large" (400)
    /// </summary>
    public string ThumbnailSize { get; set; } = "Medium";

    /// <summary>
    /// Stores random sort order for each folder (key: folder path, value: list of file names in random order)
    /// </summary>
    public Dictionary<string, List<string>> RandomOrderCache { get; set; } = new();

    /// <summary>
    /// Tracks which folders have random sorting enabled
    /// </summary>
    public List<string> RandomEnabledFolders { get; set; } = new();

    /// <summary>
    /// Maximum video cache size in bytes (default: 20 GB)
    /// </summary>
    public long VideoCacheMaxSizeBytes { get; set; } = 21_474_836_480; // 20 GB

    /// <summary>
    /// Maximum age of cache files in hours (default: 30 days = 720 hours)
    /// </summary>
    public long VideoCacheMaxAgeHours { get; set; } = 720; // 30 days
}
