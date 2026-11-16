using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
    private const int UiBatchSize = 24;
    private readonly LoggingService _loggingService;
    private readonly FitsImageService _fitsImageService;
    private readonly string _thumbnailCacheFolder;
    private CancellationTokenSource? _backgroundTaskCancellation;
    private volatile int _currentThumbnailSize = 200; // Default size - volatile for thread safety
    private int _thumbnailRequestCounter;
    private bool _disposed = false;

    private sealed record ThumbnailWorkResult(ImageFile ImageFile, byte[] ThumbnailData);
    
    public ThumbnailService(LoggingService loggingService)
    {
        _loggingService = loggingService;
        _fitsImageService = new FitsImageService(loggingService);
        _thumbnailCacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.Paths.AppDataFolderName,
            "ThumbnailCache");
        Directory.CreateDirectory(_thumbnailCacheFolder);
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
        var requestId = Interlocked.Increment(ref _thumbnailRequestCounter);
        var generationStopwatch = Stopwatch.StartNew();

        _loggingService.LogInfo(
            "Thumbnail generation start #{RequestId}: total={TotalCount}, visibleRange={VisibleStart}-{VisibleEnd}, workers={WorkerCount}",
            requestId, images.Count, safeStart, safeEnd, MaxConcurrentThumbnailWorkers);

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
                var totalApplied = 0;
                var firstUiBatchLogged = false;

                // Generate visible thumbnails first using the worker pool.
                var visibleResults = await ProcessThumbnailBatchAsync(visibleImages, token);
                var visibleApply = await ApplyThumbnailBatchAsync(visibleResults, token, requestId, generationStopwatch, firstUiBatchLogged);
                totalApplied += visibleApply.AppliedCount;
                firstUiBatchLogged = visibleApply.FirstUiBatchLogged;

                if (token.IsCancellationRequested)
                {
                    _loggingService.LogTrace("Thumbnail background generation cancelled");
                    return;
                }

                // Generate remaining thumbnails with the same worker pool.
                var backgroundResults = await ProcessThumbnailBatchAsync(backgroundImages, token);
                var backgroundApply = await ApplyThumbnailBatchAsync(backgroundResults, token, requestId, generationStopwatch, firstUiBatchLogged);
                totalApplied += backgroundApply.AppliedCount;
                firstUiBatchLogged = backgroundApply.FirstUiBatchLogged;

                _loggingService.LogInfo(
                    "Thumbnail generation done #{RequestId}: applied={AppliedCount}, elapsedMs={ElapsedMs}",
                    requestId,
                    totalApplied,
                    generationStopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                _loggingService.LogInfo(
                    "Thumbnail generation cancelled #{RequestId}: elapsedMs={ElapsedMs}",
                    requestId,
                    generationStopwatch.ElapsedMilliseconds);
            }
        }, token);
    }

    /// <summary>
    /// Processes a batch of thumbnails using a bounded worker pool.
    /// </summary>
    private async Task<List<ThumbnailWorkResult>> ProcessThumbnailBatchAsync(List<ImageFile> images, CancellationToken token)
    {
        if (images.Count == 0)
            return [];

        var results = new ConcurrentQueue<ThumbnailWorkResult>();

        await Parallel.ForEachAsync(
            images,
            new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = MaxConcurrentThumbnailWorkers
            },
            async (image, cancellationToken) =>
            {
                var generated = await GenerateThumbnailAsync(image, cancellationToken);
                if (generated != null)
                {
                    results.Enqueue(generated);
                }
            });

        return [.. results];
    }

    /// <summary>
    /// Asynchronously generates a thumbnail for a single image
    /// All I/O and image processing happens on background threads
    /// </summary>
    private async Task<ThumbnailWorkResult?> GenerateThumbnailAsync(ImageFile imageFile, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return null;

        try
        {
            if (imageFile.ThumbnailGenerated)
                return null;

            if (token.IsCancellationRequested)
                return null;

            // Generate thumbnail on background thread
            var thumbnailData = await Task.Run(() => GenerateThumbnailData(imageFile), token);

            if (thumbnailData != null)
            {
                return new ThumbnailWorkResult(imageFile, thumbnailData);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled, don't log
            return null;
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to generate thumbnail for {FileName}: {Message}", 
                imageFile.FileName, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Applies generated thumbnail updates to the UI in batches.
    /// </summary>
    private async Task<(int AppliedCount, bool FirstUiBatchLogged)> ApplyThumbnailBatchAsync(
        List<ThumbnailWorkResult> results,
        CancellationToken token,
        int requestId,
        Stopwatch generationStopwatch,
        bool firstUiBatchLogged)
    {
        if (results.Count == 0 || token.IsCancellationRequested)
            return (0, firstUiBatchLogged);

        var appliedCount = 0;

        for (int i = 0; i < results.Count; i += UiBatchSize)
        {
            token.ThrowIfCancellationRequested();
            var batch = results.Skip(i).Take(UiBatchSize).ToList();

            // Set properties on the UI thread at Background priority so thumbnail updates
            // never block user input (Input priority = 5 > Background priority = 4).
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var result in batch)
                {
                    result.ImageFile.ThumbnailData = result.ThumbnailData;
                    result.ImageFile.ThumbnailGenerated = true;
                }
            }, DispatcherPriority.Background, token);

            appliedCount += batch.Count;

            if (!firstUiBatchLogged)
            {
                firstUiBatchLogged = true;
                _loggingService.LogInfo(
                    "Thumbnail generation first-batch #{RequestId}: batchCount={BatchCount}, elapsedMs={ElapsedMs}",
                    requestId,
                    batch.Count,
                    generationStopwatch.ElapsedMilliseconds);
            }
        }

        return (appliedCount, firstUiBatchLogged);
    }

    /// <summary>
    /// Generates thumbnail data for an image file (runs on background thread)
    /// </summary>
    private byte[]? GenerateThumbnailData(ImageFile imageFile)
    {
        try
        {
            int thumbnailSize = _currentThumbnailSize;

            if (TryGetFileSignature(imageFile.FilePath, out var lastWriteTicks, out var fileLength)
                && TryReadThumbnailFromDiskCache(imageFile.FilePath, thumbnailSize, lastWriteTicks, fileLength, out var cachedData))
            {
                return cachedData;
            }

            BitmapSource? bitmapSource = null;

            if (imageFile.IsVideo)
            {
                if (MediaTypeHelper.IsMpeg(imageFile.FilePath) || MediaTypeHelper.IsMp4(imageFile.FilePath))
                {
                    bitmapSource = TryExtractVideoThumbnail(imageFile.FilePath, thumbnailSize);
                }

                bitmapSource ??= CreateVideoPlaceholder(thumbnailSize);
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

            var encoded = ms.ToArray();

            if (TryGetFileSignature(imageFile.FilePath, out lastWriteTicks, out fileLength))
            {
                TryWriteThumbnailToDiskCache(imageFile.FilePath, thumbnailSize, lastWriteTicks, fileLength, encoded);
            }

            return encoded;
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to generate thumbnail data for {FileName}: {Message}", 
                imageFile.FileName, ex.Message);
            return null;
        }
    }

    private bool TryGetFileSignature(string filePath, out long lastWriteTicks, out long fileLength)
    {
        lastWriteTicks = 0;
        fileLength = 0;

        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return false;

            lastWriteTicks = fileInfo.LastWriteTimeUtc.Ticks;
            fileLength = fileInfo.Length;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryReadThumbnailFromDiskCache(string filePath, int thumbnailSize, long lastWriteTicks, long fileLength, out byte[] thumbnailData)
    {
        thumbnailData = Array.Empty<byte>();

        try
        {
            var cachePath = GetThumbnailCacheFilePath(filePath, thumbnailSize, lastWriteTicks, fileLength);
            if (!File.Exists(cachePath))
                return false;

            thumbnailData = File.ReadAllBytes(cachePath);
            return thumbnailData.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private void TryWriteThumbnailToDiskCache(string filePath, int thumbnailSize, long lastWriteTicks, long fileLength, byte[] thumbnailData)
    {
        try
        {
            var cachePath = GetThumbnailCacheFilePath(filePath, thumbnailSize, lastWriteTicks, fileLength);
            File.WriteAllBytes(cachePath, thumbnailData);
        }
        catch
        {
            // Cache write failures should never break thumbnail generation.
        }
    }

    private string GetThumbnailCacheFilePath(string filePath, int thumbnailSize, long lastWriteTicks, long fileLength)
    {
        var keyMaterial = $"{filePath}|{thumbnailSize}|{lastWriteTicks}|{fileLength}";
        var keyBytes = Encoding.UTF8.GetBytes(keyMaterial);
        var hashBytes = SHA256.HashData(keyBytes);
        var hash = Convert.ToHexString(hashBytes);
        return Path.Combine(_thumbnailCacheFolder, hash + ".jpg");
    }

    /// <summary>
    /// Attempts to extract a thumbnail frame from a video on an STA thread.
    /// Returns null when extraction fails so caller can use placeholder fallback.
    /// </summary>
    private BitmapSource? TryExtractVideoThumbnail(string videoPath, int thumbnailSize)
    {
        BitmapSource? result = null;
        Exception? extractionException = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = ExtractVideoThumbnailOnSta(videoPath, thumbnailSize);
            }
            catch (Exception ex)
            {
                extractionException = ex;
            }
        });

        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(3000))
        {
            _loggingService.LogTrace("Video thumbnail extraction timed out for {Path}", videoPath);
            return null;
        }

        if (extractionException != null)
        {
            _loggingService.LogTrace("Video thumbnail extraction failed for {Path}: {Message}", videoPath, extractionException.Message);
            return null;
        }

        return result;
    }

    /// <summary>
    /// Extracts a representative frame from the video using MediaPlayer.
    /// Must run on an STA thread.
    /// </summary>
    private static BitmapSource? ExtractVideoThumbnailOnSta(string videoPath, int thumbnailSize)
    {
        using var openedSignal = new AutoResetEvent(false);
        using var failedSignal = new AutoResetEvent(false);

        var player = new MediaPlayer
        {
            ScrubbingEnabled = true,
            Volume = 0,
            IsMuted = true
        };

        Exception? mediaError = null;
        player.MediaOpened += (_, _) => openedSignal.Set();
        player.MediaFailed += (_, e) =>
        {
            mediaError = e.ErrorException;
            failedSignal.Set();
        };

        try
        {
            player.Open(new Uri(videoPath, UriKind.Absolute));

            var waitStart = DateTime.UtcNow;
            while ((DateTime.UtcNow - waitStart).TotalMilliseconds < 1500)
            {
                if (failedSignal.WaitOne(0))
                    return null;

                if (openedSignal.WaitOne(0))
                    break;

                PumpDispatcher(TimeSpan.FromMilliseconds(20));
            }

            if (mediaError != null)
                return null;

            if (player.NaturalVideoWidth <= 0 || player.NaturalVideoHeight <= 0)
                return null;

            if (player.NaturalDuration.HasTimeSpan && player.NaturalDuration.TimeSpan > TimeSpan.FromMilliseconds(250))
            {
                var durationMs = player.NaturalDuration.TimeSpan.TotalMilliseconds;
                var captureMs = Math.Min(1000.0, Math.Max(100.0, durationMs * 0.10));
                player.Position = TimeSpan.FromMilliseconds(captureMs);
                PumpDispatcher(TimeSpan.FromMilliseconds(120));
            }
            else
            {
                PumpDispatcher(TimeSpan.FromMilliseconds(80));
            }

            var sourceWidth = player.NaturalVideoWidth;
            var sourceHeight = player.NaturalVideoHeight;
            var scale = Math.Min((double)thumbnailSize / sourceWidth, (double)thumbnailSize / sourceHeight);
            var renderWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
            var renderHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawVideo(player, new System.Windows.Rect(0, 0, renderWidth, renderHeight));
            }

            var target = new RenderTargetBitmap(renderWidth, renderHeight, 96, 96, PixelFormats.Pbgra32);
            target.Render(visual);
            target.Freeze();
            return target;
        }
        finally
        {
            player.Close();
        }
    }

    private static void PumpDispatcher(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = duration
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };

        timer.Start();
        Dispatcher.PushFrame(frame);
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
