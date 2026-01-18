using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Converts WebM video files to MP4 format using FFmpeg
/// Cached conversions are reused to avoid redundant processing
/// </summary>
public class VideoConversionService
{
    private readonly LoggingService _loggingService;
    private readonly SettingsService _settingsService;
    private readonly string _cacheDirectory;

    public VideoConversionService(LoggingService loggingService, SettingsService? settingsService = null)
    {
        _loggingService = loggingService;
        _settingsService = settingsService ?? new SettingsService();
        
        // Create cache directory in AppData
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheDirectory = Path.Combine(appDataPath, "DaisyView", "VideoCache");
        
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            // Clean up cache on startup (old files and enforce size limits)
            ManageCacheSize();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to create video cache directory", ex);
        }
    }

    /// <summary>
    /// Gets the path to an MP4 file for the given WebM input.
    /// If a cached conversion exists, returns that path.
    /// Otherwise, starts an async conversion and returns null initially,
    /// then returns the path when conversion completes.
    /// </summary>
    public string? GetConvertedFilePath(string webmFilePath)
    {
        if (!File.Exists(webmFilePath))
            return null;

        try
        {
            var fileInfo = new FileInfo(webmFilePath);
            var cacheFileName = $"{Path.GetFileNameWithoutExtension(webmFilePath)}_{fileInfo.Length}_{fileInfo.LastWriteTimeUtc:yyyyMMddHHmmss}.daicache";
            var cachedMp4Path = Path.Combine(_cacheDirectory, cacheFileName);

            // Return cached file if it exists
            if (File.Exists(cachedMp4Path))
            {
                _loggingService.LogInfo("Using cached MP4 for WebM: {WebmFile}", webmFilePath);
                return cachedMp4Path;
            }

            // Clean up old cache files for this WebM (keep only the latest)
            CleanupOldCacheFiles(Path.GetFileNameWithoutExtension(webmFilePath));

            return cachedMp4Path;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error getting converted file path", ex);
            return null;
        }
    }

    /// <summary>
    /// Converts a WebM file to MP4 asynchronously using FFmpeg
    /// Returns the path to the converted MP4 file on success
    /// </summary>
    public async Task<string?> ConvertWebmToMp4Async(string webmFilePath)
    {
        if (!File.Exists(webmFilePath))
        {
            _loggingService.LogWarning("WebM file not found: {WebmFile}", webmFilePath);
            return null;
        }

        var mp4Path = GetConvertedFilePath(webmFilePath);
        if (mp4Path == null)
            return null;

        // If already converted, return the path
        if (File.Exists(mp4Path))
            return mp4Path;

        return await Task.Run(() => ConvertWebmToMp4Internal(webmFilePath, mp4Path));
    }

    /// <summary>
    /// Internal method to perform the actual FFmpeg conversion
    /// </summary>
    private string? ConvertWebmToMp4Internal(string webmFilePath, string mp4Path)
    {
        try
        {
            _loggingService.LogInfo("Starting WebM to MP4 conversion: {WebmFile} -> {Mp4File}", webmFilePath, mp4Path);

            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{webmFilePath}\" -c:v libx264 -preset fast -c:a aac -q:a 5 \"{mp4Path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    _loggingService.LogError("Failed to start FFmpeg process for: {WebmFile}", null, webmFilePath);
                    return null;
                }

                // Read output to prevent deadlock
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    _loggingService.LogError("FFmpeg conversion failed for {WebmFile}. Exit code: {ExitCode}. Error: {Error}", null,
                        webmFilePath, process.ExitCode, error);
                    
                    // Clean up partial file
                    try
                    {
                        if (File.Exists(mp4Path))
                            File.Delete(mp4Path);
                    }
                    catch { }

                    return null;
                }

                // Verify the output file was created
                if (!File.Exists(mp4Path))
                {
                    _loggingService.LogError("FFmpeg did not create output file: {Mp4File}", null, mp4Path);
                    return null;
                }

                _loggingService.LogInfo("Successfully converted WebM to MP4: {Mp4File}", mp4Path);
                return mp4Path;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Exception during WebM to MP4 conversion", ex);
            
            // Clean up partial file
            try
            {
                if (File.Exists(mp4Path))
                    File.Delete(mp4Path);
            }
            catch { }

            return null;
        }
    }

    /// <summary>
    /// Removes old cached files for a given WebM file
    /// (keeps only the latest conversion)
    /// </summary>
    private void CleanupOldCacheFiles(string baseFileName)
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
                return;

            var oldFiles = Directory.GetFiles(_cacheDirectory, $"{baseFileName}_*.daicache");
            foreach (var file in oldFiles)
            {
                try
                {
                    File.Delete(file);
                    _loggingService.LogInfo("Cleaned up old cache file: {CacheFile}", file);
                }
                catch
                {
                    _loggingService.LogWarning("Failed to delete old cache file: {CacheFile}", file);
                }
            }
        }
        catch
        {
            _loggingService.LogWarning("Error cleaning up old cache files");
        }
    }

    /// <summary>
    /// Manages cache size by removing old files if cache exceeds size limit
    /// Also removes files older than the configured age limit
    /// </summary>
    private void ManageCacheSize()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
                return;

            var settings = _settingsService.GetSettings();
            var maxCacheSizeBytes = settings.VideoCacheMaxSizeBytes;
            var maxCacheAgeHours = settings.VideoCacheMaxAgeHours;

            var cacheFiles = Directory.GetFiles(_cacheDirectory, "*.daicache")
                .Select(f => new FileInfo(f))
                .ToList();

            // Remove files older than configured age
            var now = DateTime.UtcNow;
            var oldFiles = cacheFiles.Where(f => 
                (now - f.LastAccessTimeUtc).TotalHours > maxCacheAgeHours)
                .ToList();

            foreach (var file in oldFiles)
            {
                try
                {
                    file.Delete();
                    _loggingService.LogInfo("Removed old cache file: {CacheFile}", file.Name);
                }
                catch { }
            }

            // Calculate total cache size and remove oldest files if exceeds limit
            var currentSize = cacheFiles.Sum(f => f.Length);
            if (currentSize > maxCacheSizeBytes)
            {
                var filesToRemove = cacheFiles
                    .OrderBy(f => f.LastAccessTimeUtc)
                    .ToList();

                long bytesToFree = currentSize - maxCacheSizeBytes;
                foreach (var file in filesToRemove)
                {
                    if (bytesToFree <= 0)
                        break;

                    try
                    {
                        bytesToFree -= file.Length;
                        file.Delete();
                        _loggingService.LogInfo("Removed cache file to manage size: {CacheFile}", file.Name);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Error managing cache size", ex);
        }
    }

    /// <summary>
    /// Clears the entire video cache
    /// </summary>
    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, true);
                Directory.CreateDirectory(_cacheDirectory);
                _loggingService.LogInfo("Video cache cleared");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to clear video cache", ex);
        }
    }
}
