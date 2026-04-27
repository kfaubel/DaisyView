using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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
    private const int MaxConcurrentThumbnailWorkers = 8;
    private readonly LoggingService _loggingService;
    private readonly FitsImageService _fitsImageService;
    private CancellationTokenSource? _backgroundTaskCancellation;
    private static readonly object LockObject = new();
    private volatile int _currentThumbnailSize = 200; // Default size - volatile for thread safety
    private bool _disposed = false;
    
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
    /// Generates thumbnails for all images in the background
    /// Visible images are prioritized but all generation happens on background threads
    /// </summary>
    public void GenerateThumbnailsAsync(List<ImageFile> images, int visibleCount)
    {
        GenerateThumbnailsAsync(images, 0, Math.Max(0, visibleCount - 1));
    }

    /// <summary>
    /// Generates thumbnails for all images in the background.
    /// Images in the visible index range are prioritized first.
    /// </summary>
    public void GenerateThumbnailsAsync(List<ImageFile> images, int visibleStartIndex, int visibleEndIndex)
    {
        if (images.Count == 0)
            return;

        // Cancel and dispose any existing background task
        _backgroundTaskCancellation?.Cancel();
        _backgroundTaskCancellation?.Dispose();
        _backgroundTaskCancellation = new CancellationTokenSource();

        var token = _backgroundTaskCancellation.Token;

        var safeStart = Math.Clamp(visibleStartIndex, 0, images.Count - 1);
        var safeEnd = Math.Clamp(visibleEndIndex, safeStart, images.Count - 1);

        // Process all thumbnails in the background - viewport images first, then the rest
        var visibleImages = images
            .Skip(safeStart)
            .Take((safeEnd - safeStart) + 1)
            .ToList();

        var backgroundImages = images
            .Take(safeStart)
            .Concat(images.Skip(safeEnd + 1))
            .ToList();

        _ = Task.Run(async () =>
        {
            try
            {
                // Generate visible thumbnails first using the worker pool.
                await ProcessThumbnailBatchAsync(visibleImages, token);

                if (token.IsCancellationRequested)
                {
                    _loggingService.LogTrace("Thumbnail background generation cancelled");
                    return;
                }

                // Generate remaining thumbnails with the same worker pool.
                await ProcessThumbnailBatchAsync(backgroundImages, token);

                _loggingService.LogTrace("Thumbnail background generation completed using {WorkerCount} workers", MaxConcurrentThumbnailWorkers);
            }
            catch (OperationCanceledException)
            {
                _loggingService.LogTrace("Thumbnail background generation cancelled");
            }
        }, token);
    }

    /// <summary>
    /// Processes a batch of thumbnails using a bounded worker pool.
    /// </summary>
    private async Task ProcessThumbnailBatchAsync(List<ImageFile> images, CancellationToken token)
    {
        if (images.Count == 0)
            return;

        await Parallel.ForEachAsync(
            images,
            new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = MaxConcurrentThumbnailWorkers
            },
            async (image, cancellationToken) =>
            {
                await GenerateThumbnailAsync(image, cancellationToken);
            });
    }

    /// <summary>
    /// Asynchronously generates a thumbnail for a single image
    /// All I/O and image processing happens on background threads
    /// </summary>
    private async Task GenerateThumbnailAsync(ImageFile imageFile, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        try
        {
            if (imageFile.ThumbnailGenerated)
                return;

            if (token.IsCancellationRequested)
                return;

            // Generate thumbnail on background thread
            var thumbnailData = await Task.Run(() => GenerateThumbnailData(imageFile), token);
            
            if (thumbnailData != null)
            {
                lock (LockObject)
                {
                    // Set data first, then generated flag to trigger single notification
                    imageFile.ThumbnailData = thumbnailData;
                    imageFile.ThumbnailGenerated = true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled, don't log
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to generate thumbnail for {FileName}: {Message}", 
                imageFile.FileName, ex.Message);
        }
    }

    /// <summary>
    /// Generates thumbnail data for an image file (runs on background thread)
    /// </summary>
    private byte[]? GenerateThumbnailData(ImageFile imageFile)
    {
        try
        {
            int thumbnailSize = AppConstants.ThumbnailSizes.LargePixels;
            BitmapSource? bitmapSource = null;

            if (imageFile.IsVideo)
            {
                bitmapSource = CreateVideoPlaceholder(thumbnailSize);
            }
            else if (MediaTypeHelper.IsFitsFile(imageFile.FilePath))
            {
                bitmapSource = _fitsImageService.LoadFitsImage(imageFile.FilePath, thumbnailSize);
            }
            else
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageFile.FilePath);
                bitmap.DecodePixelWidth = thumbnailSize;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                bitmapSource = bitmap;
            }

            if (bitmapSource == null)
                return null;

            // Encode to JPEG
            var encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 85;
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to generate thumbnail data for {FileName}: {Message}", 
                imageFile.FileName, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Creates a placeholder image for videos when extraction fails
    /// </summary>
    private BitmapImage? CreateVideoPlaceholder(int thumbnailSize)
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
                    new System.Windows.Rect(0, 0, thumbnailSize, thumbnailSize));
                
                // Draw a play symbol in the center
                var penBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
                var pen = new System.Windows.Media.Pen(penBrush, 2);
                int size = thumbnailSize / 3;
                int x = (thumbnailSize - size) / 2;
                int y = (thumbnailSize - size) / 2;
                
                drawingContext.DrawEllipse(null, pen, new System.Windows.Point(x + size / 2, y + size / 2), size / 2, size / 2);
            }
            
            var renderTargetBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                thumbnailSize, thumbnailSize, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
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
