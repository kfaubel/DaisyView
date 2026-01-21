using System;

namespace DaisyView.Constants;

/// <summary>
/// Application-wide constants for consistency and maintainability
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Thumbnail size constants
    /// </summary>
    public static class ThumbnailSizes
    {
        public const string Small = "Small";
        public const string Medium = "Medium";
        public const string Large = "Large";

        public const int SmallPixels = 100;
        public const int MediumPixels = 200;
        public const int LargePixels = 400;

        /// <summary>
        /// Default number of visible thumbnails (used for priority loading)
        /// </summary>
        public const int DefaultVisibleCount = 10;

        /// <summary>
        /// Gets pixel size from size name
        /// </summary>
        public static int GetPixelSize(string sizeName) => sizeName.ToLower() switch
        {
            "small" => SmallPixels,
            "medium" => MediumPixels,
            "large" => LargePixels,
            _ => MediumPixels
        };
    }

    /// <summary>
    /// Timing constants for various operations
    /// </summary>
    public static class Timing
    {
        /// <summary>
        /// Interval for checking video loop position
        /// </summary>
        public static readonly TimeSpan VideoLoopCheckInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Time before cursor is hidden in slideshow mode
        /// </summary>
        public static readonly TimeSpan CursorHideDelay = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Timeout for drive readiness check
        /// </summary>
        public static readonly TimeSpan DriveCheckTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Default maximum age for cache files (30 days)
        /// </summary>
        public const long DefaultCacheMaxAgeHours = 720;

        /// <summary>
        /// Default maximum cache size (20 GB)
        /// </summary>
        public const long DefaultCacheMaxSizeBytes = 21_474_836_480;
    }

    /// <summary>
    /// Application paths and folder names
    /// </summary>
    public static class Paths
    {
        public const string AppDataFolderName = "DaisyView";
        public const string VideoCacheFolderName = "VideoCache";
        public const string SettingsFileName = "settings.json";
        public const string LogsFolderName = "Logs";
    }

    /// <summary>
    /// UI colors used throughout the application
    /// </summary>
    public static class Colors
    {
        public const string ActiveGold = "#FFD700";
        public const string InactiveGrey = "#808080";
        public const string ErrorYellow = "#FFFF00";
    }

    /// <summary>
    /// Default window dimensions
    /// </summary>
    public static class WindowDefaults
    {
        public const double Width = 1200;
        public const double Height = 800;
        public const string State = "Normal";
    }
}
