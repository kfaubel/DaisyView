using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DaisyView.Models;

/// <summary>
/// Represents an image file in the current folder
/// </summary>
public class ImageFile : INotifyPropertyChanged
{
    private bool _isMarked;
    private bool _isActive;
    private byte[]? _thumbnailData;
    private bool _thumbnailGenerated;
    private bool _isVideo;
    private string? _convertedVideoPath;
    private bool _isFromShortcut;
    private string? _shortcutName;

    /// <summary>
    /// File name without path
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Display name shown in UI (includes shortcut prefix if from a shortcut)
    /// </summary>
    public string DisplayName => _isFromShortcut && !string.IsNullOrEmpty(_shortcutName) 
        ? $"[{_shortcutName}] {FileName}" 
        : FileName;

    /// <summary>
    /// Whether this file is from a shortcut folder (not directly in the current folder)
    /// </summary>
    public bool IsFromShortcut
    {
        get => _isFromShortcut;
        set { if (_isFromShortcut != value) { _isFromShortcut = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } }
    }

    /// <summary>
    /// Name of the shortcut folder this file came from (without .lnk extension)
    /// </summary>
    public string? ShortcutName
    {
        get => _shortcutName;
        set { if (_shortcutName != value) { _shortcutName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); } }
    }

    /// <summary>
    /// Full path to the file
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Whether this file is a video file (WebM, MP4, AVI, MPEG, etc.)
    /// </summary>
    public bool IsVideo
    {
        get => _isVideo;
        set { if (_isVideo != value) { _isVideo = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Path to the converted video file (MP4) if available
    /// Used when original video format is not supported by MediaElement
    /// </summary>
    public string? ConvertedVideoPath
    {
        get => _convertedVideoPath;
        set { if (_convertedVideoPath != value) { _convertedVideoPath = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Whether this file is marked (has a checkmark)
    /// </summary>
    public bool IsMarked
    {
        get => _isMarked;
        set { if (_isMarked != value) { _isMarked = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Whether this file is the active file (shown with red border)
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Cached thumbnail image data
    /// </summary>
    public byte[]? ThumbnailData
    {
        get => _thumbnailData;
        set { if (_thumbnailData != value) { _thumbnailData = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Whether the thumbnail has been generated
    /// </summary>
    public bool ThumbnailGenerated
    {
        get => _thumbnailGenerated;
        set { if (_thumbnailGenerated != value) { _thumbnailGenerated = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
