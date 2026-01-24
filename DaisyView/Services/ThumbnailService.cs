using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FFMpegCore;
using DaisyView.Constants;
using DaisyView.Helpers;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Generates thumbnails for image files
/// Visible thumbnails are generated first, then background generation processes the rest
/// Background generation is abandoned if user changes folders
/// </summary>
public class ThumbnailService : IDisposable
{
    private readonly LoggingService _loggingService;
    private readonly FitsImageService _fitsImageService;
    private CancellationTokenSource? _backgroundTaskCancellation;
    private static readonly object LockObject = new();
    private volatile int _currentThumbnailSize = 200; // Default size - volatile for thread safety
    private bool _disposed = false;

    public event EventHandler<ImageFile>? ThumbnailGenerated;

    public ThumbnailService(LoggingService loggingService)
    {
        _loggingService = loggingService;
        _fitsImageService = new FitsImageService(loggingService);
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
        // Cancel and dispose any existing background task
        _backgroundTaskCancellation?.Cancel();
        _backgroundTaskCancellation?.Dispose();
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
    /// Generates a thumbnail for a single image or video file
    /// </summary>
    private void GenerateThumbnail(ImageFile imageFile)
    {
        try
        {
            if (imageFile.ThumbnailGenerated)
                return;

            BitmapSource? bitmapSource = null;

            if (imageFile.IsVideo)
            {
                // For video files, try to extract first frame using FFmpeg
                bitmapSource = ExtractVideoFrameThumbnail(imageFile.FilePath);
                
                // If extraction failed, create a placeholder
                if (bitmapSource == null)
                {
                    bitmapSource = CreateVideoPlaceholder();
                }
            }
            else if (MediaTypeHelper.IsFitsFile(imageFile.FilePath))
            {
                // For FITS files, use the FitsImageService
                bitmapSource = _fitsImageService.LoadFitsImage(imageFile.FilePath, _currentThumbnailSize);
            }
            else
            {
                // For standard image files, load directly
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageFile.FilePath);
                bitmap.DecodePixelWidth = _currentThumbnailSize;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                bitmapSource = bitmap;
            }

            if (bitmapSource == null)
            {
                _loggingService.LogWarning("Failed to generate thumbnail for {FileName}: bitmap is null", imageFile.FileName);
                return;
            }

            lock (LockObject)
            {
                imageFile.ThumbnailGenerated = true;
                // Store encoded bitmap data
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                
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
    /// Creates a placeholder image for videos when extraction fails
    /// </summary>
    private BitmapImage? CreateVideoPlaceholder()
    {
        try
        {
            // Create a simple dark gray placeholder
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64)),
                    null,
                    new System.Windows.Rect(0, 0, _currentThumbnailSize, _currentThumbnailSize));
                
                // Draw a play symbol in the center
                var penBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
                var pen = new System.Windows.Media.Pen(penBrush, 2);
                int size = _currentThumbnailSize / 3;
                int x = (_currentThumbnailSize - size) / 2;
                int y = (_currentThumbnailSize - size) / 2;
                
                drawingContext.DrawEllipse(null, pen, new System.Windows.Point(x + size / 2, y + size / 2), size / 2, size / 2);
            }
            
            var renderTargetBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                _currentThumbnailSize, _currentThumbnailSize, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            
            return bitmapImage;
        }
        catch
        {
            // If even placeholder creation fails, return null
            return null;
        }
    }

    /// <summary>
    /// Extracts the first frame of a video file as a thumbnail
    /// </summary>
    private BitmapImage? ExtractVideoFrameThumbnail(string videoPath)
    {
        try
        {
            var tempImagePath = Path.Combine(Path.GetTempPath(), $"frame_{Path.GetRandomFileName()}.png");

            try
            {
                // Use FFmpeg to extract first frame
                FFMpegArguments
                    .FromFileInput(videoPath)
                    .OutputToFile(tempImagePath, true, options => options
                        .WithFrameOutputCount(1)
                        .Seek(TimeSpan.FromSeconds(0)))
                    .ProcessSynchronously();

                if (File.Exists(tempImagePath))
                {
                    // Read file into memory before deleting it
                    byte[] imageData = File.ReadAllBytes(tempImagePath);
                    
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(imageData);
                    bitmap.DecodePixelWidth = _currentThumbnailSize;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                }
                else
                {
                    _loggingService.LogWarning("FFmpeg failed to extract frame from {VideoPath}: output file not created", videoPath);
                }
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempImagePath))
                {
                    try
                    {
                        File.Delete(tempImagePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to extract video frame thumbnail from {VideoPath}: {Message}", videoPath, ex.Message);
            return null;
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
        return AppConstants.ThumbnailSizes.GetPixelSize(sizeString);
    }

    /// <summary>
    /// Cleans up resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method for proper disposal pattern
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _backgroundTaskCancellation?.Cancel();
            _backgroundTaskCancellation?.Dispose();
            _backgroundTaskCancellation = null;
        }

        _disposed = true;
    }
}
