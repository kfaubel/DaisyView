using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Generates thumbnails for image files
/// Visible thumbnails are generated first, then background generation processes the rest
/// Background generation is abandoned if user changes folders
/// </summary>
public class ThumbnailService
{
    private readonly LoggingService _loggingService;
    private CancellationTokenSource? _backgroundTaskCancellation;
    private static readonly object LockObject = new();
    private int _currentThumbnailSize = 200; // Default size

    public event EventHandler<ImageFile>? ThumbnailGenerated;

    public ThumbnailService(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <summary>
    /// Sets the current thumbnail size
    /// </summary>
    public void SetThumbnailSize(string sizeString)
    {
        _currentThumbnailSize = GetThumbnailSizePixels(sizeString);
    }

    /// <summary>
    /// Generates thumbnails for visible images first, then background-generates the rest
    /// </summary>
    public void GenerateThumbnailsAsync(List<ImageFile> images, int visibleCount)
    {
        // Cancel any existing background task
        _backgroundTaskCancellation?.Cancel();
        _backgroundTaskCancellation = new CancellationTokenSource();

        var token = _backgroundTaskCancellation.Token;

        // Generate visible thumbnails immediately
        var visibleImages = images.Take(visibleCount).ToList();
        foreach (var image in visibleImages)
        {
            if (token.IsCancellationRequested)
                return;

            GenerateThumbnail(image);
        }

        // Generate remaining thumbnails in the background
        var backgroundImages = images.Skip(visibleCount).ToList();
        _ = Task.Run(async () =>
        {
            foreach (var image in backgroundImages)
            {
                if (token.IsCancellationRequested)
                {
                    _loggingService.LogTrace("Thumbnail background generation cancelled");
                    return;
                }

                GenerateThumbnail(image);
                await Task.Delay(10, token); // Small delay to avoid blocking UI
            }

            _loggingService.LogTrace("Thumbnail background generation completed");
        }, token);
    }

    /// <summary>
    /// Generates a thumbnail for a single image file
    /// </summary>
    private void GenerateThumbnail(ImageFile imageFile)
    {
        try
        {
            if (imageFile.ThumbnailGenerated)
                return;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imageFile.FilePath);
            bitmap.DecodePixelWidth = _currentThumbnailSize;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            lock (LockObject)
            {
                imageFile.ThumbnailGenerated = true;
                // Store encoded bitmap data
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                
                using var ms = new MemoryStream();
                encoder.Save(ms);
                imageFile.ThumbnailData = ms.ToArray();
            }

            _loggingService.LogTrace("Thumbnail generated for {FileName}", imageFile.FileName);
            ThumbnailGenerated?.Invoke(this, imageFile);
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to generate thumbnail for {FileName}: {Message}", 
                imageFile.FileName, ex.Message);
        }
    }

    /// <summary>
    /// Cancels background thumbnail generation
    /// </summary>
    public void CancelBackgroundGeneration()
    {
        _backgroundTaskCancellation?.Cancel();
    }

    /// <summary>
    /// Gets the thumbnail size in pixels based on the size setting
    /// </summary>
    public static int GetThumbnailSizePixels(string sizeString)
    {
        return sizeString.ToLower() switch
        {
            "small" => 100,
            "medium" => 200,
            "large" => 400,
            _ => 200
        };
    }
}
