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

    /// <summary>
    /// File name without path
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Full path to the file
    /// </summary>
    public required string FilePath { get; set; }

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
