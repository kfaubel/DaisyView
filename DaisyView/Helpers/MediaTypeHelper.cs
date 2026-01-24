using System;
using System.Collections.Generic;
using System.IO;

namespace DaisyView.Helpers;

/// <summary>
/// Helper class for determining media file types
/// Centralizes all extension checking to avoid duplication
/// </summary>
public static class MediaTypeHelper
{
    /// <summary>
    /// Image file extensions (static images)
    /// </summary>
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp", ".fits", ".fit"
    };

    /// <summary>
    /// Animated/video extensions that play via MediaElement
    /// </summary>
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".webm", ".mp4", ".avi", ".mpeg", ".mpg", ".gif"
    };

    /// <summary>
    /// Extensions that require conversion to MP4 for playback
    /// </summary>
    private static readonly HashSet<string> ConvertibleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".webm", ".avi", ".mpeg", ".mpg"
    };

    /// <summary>
    /// All supported media extensions
    /// </summary>
    public static readonly string[] AllSupportedExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp", ".fits", ".fit",
        ".webm", ".mp4", ".avi", ".mpeg", ".mpg"
    };

    /// <summary>
    /// Checks if a file is a supported image or video
    /// </summary>
    public static bool IsSupportedMedia(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ImageExtensions.Contains(ext) || VideoExtensions.Contains(ext);
    }

    /// <summary>
    /// Checks if a file is a static image (not video/animated)
    /// </summary>
    public static bool IsStaticImage(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ImageExtensions.Contains(ext);
    }

    /// <summary>
    /// Checks if a file is a video or animated format
    /// </summary>
    public static bool IsVideoFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return VideoExtensions.Contains(ext);
    }

    /// <summary>
    /// Checks if a file is a GIF (animated or static)
    /// </summary>
    public static bool IsGif(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a file is an MP4 (plays natively in MediaElement)
    /// </summary>
    public static bool IsMp4(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".mp4", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a video format needs conversion to MP4 for playback
    /// </summary>
    public static bool NeedsConversion(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ConvertibleExtensions.Contains(ext);
    }

    /// <summary>
    /// Checks if a file is a WebM video
    /// </summary>
    public static bool IsWebM(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".webm", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a file is an AVI video
    /// </summary>
    public static bool IsAvi(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".avi", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a file is an MPEG/MPG video
    /// </summary>
    public static bool IsMpeg(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ext.Equals(".mpeg", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".mpg", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a file is a FITS astronomical image
    /// </summary>
    public static bool IsFitsFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ext.Equals(".fits", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".fit", StringComparison.OrdinalIgnoreCase);
    }
}
