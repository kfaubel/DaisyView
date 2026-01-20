using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Represents the status of video conversion or playback
/// </summary>
public enum VideoConversionStatus
{
    Converting,
    Failed
}

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
    /// Gets the path to a cached .daicache file for the given WebM input.
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
            // Use .daicache extension to avoid leaving .mp4 files on disk
            var cacheFileName = $"{Path.GetFileNameWithoutExtension(webmFilePath)}_{fileInfo.Length}_{fileInfo.LastWriteTimeUtc:yyyyMMddHHmmss}.daicache";
            var cachedFilePath = Path.Combine(_cacheDirectory, cacheFileName);

            // Return cached file if it exists
            if (File.Exists(cachedFilePath))
            {
                _loggingService.LogInfo("Using cached conversion for WebM: {WebmFile}", webmFilePath);
                return cachedFilePath;
            }

            // Clean up old cache files for this WebM (keep only the latest)
            CleanupOldCacheFiles(Path.GetFileNameWithoutExtension(webmFilePath));

            return cachedFilePath;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error getting converted file path", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets a temporary .mp4 file for playback from a cached .daicache file.
    /// Creates a copy in the temp directory so the player can recognize the format.
    /// Temp files are automatically cleaned up by Windows.
    /// </summary>
    public string? GetPlaybackPath(string cachedFilePath)
    {
        if (!File.Exists(cachedFilePath))
            return null;

        try
        {
            // Create a temporary .mp4 file for playback
            var tempFileName = $"{Path.GetFileNameWithoutExtension(cachedFilePath)}_temp.mp4";
            var tempPlaybackPath = Path.Combine(Path.GetTempPath(), tempFileName);

            // Copy the cached file to temp with .mp4 extension for playback
            File.Copy(cachedFilePath, tempPlaybackPath, overwrite: true);
            _loggingService.LogInfo("Created temporary playback file: {TempFile}", tempPlaybackPath);

            return tempPlaybackPath;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error creating playback path from cache", ex);
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
            _loggingService.LogInfo("WebM file exists: {Exists}, Size: {Size} bytes", 
                File.Exists(webmFilePath), 
                File.Exists(webmFilePath) ? new FileInfo(webmFilePath).Length : 0);
            _loggingService.LogInfo("MP4 output cache directory: {CacheDir}, Exists: {Exists}", _cacheDirectory, Directory.Exists(_cacheDirectory));

            // Try conversion with libx264 video and re-encoded audio
            // If this fails, we'll get detailed error info in the logs
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                // Using: H.264 video (libx264) + AAC audio
                // -f mp4: explicitly specify output format so we don't need .mp4 extension
                // -crf: quality (0-51, 23 is default), lower = better quality but larger file
                // -b:a: audio bitrate (128k is good quality)
                Arguments = $"-hide_banner -loglevel error -y -i \"{webmFilePath}\" -c:v libx264 -crf 23 -c:a aac -b:a 128k -movflags +faststart -f mp4 \"{mp4Path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            _loggingService.LogInfo("FFmpeg command: ffmpeg {Args}", processInfo.Arguments);

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    _loggingService.LogError("Failed to start FFmpeg process for: {WebmFile}", null, webmFilePath);
                    return null;
                }

                _loggingService.LogInfo("FFmpeg process started with PID: {ProcessId}", process.Id);

                // Read streams asynchronously to prevent deadlock
                // FFmpeg writes progress info to stderr, which can fill the buffer if read synchronously
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                
                _loggingService.LogInfo("Waiting for FFmpeg process to complete...");
                process.WaitForExit();
                
                // Wait for both tasks to complete to ensure all streams are read
                System.Threading.Tasks.Task.WaitAll(stdoutTask, stderrTask);
                var error = stderrTask.Result;
                var output = stdoutTask.Result;

                _loggingService.LogInfo("FFmpeg process completed. Exit code: {ExitCode}", process.ExitCode);
                
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _loggingService.LogInfo("FFmpeg stderr output: {Error}", error);
                }

                if (process.ExitCode != 0)
                {
                    _loggingService.LogError("FFmpeg conversion failed for {WebmFile}. Exit code: {ExitCode}. Error: {Error}", null,
                        webmFilePath, process.ExitCode, string.IsNullOrWhiteSpace(error) ? "(empty)" : error);
                    CleanupPartialFile(mp4Path);
                    return null;
                }

                // Verify the output file was created
                if (!File.Exists(mp4Path))
                {
                    _loggingService.LogError("FFmpeg did not create output file: {Mp4File}", null, mp4Path);
                    return null;
                }

                var fileSize = new FileInfo(mp4Path).Length;
                _loggingService.LogInfo("Successfully converted WebM to MP4: {Mp4File} ({Size} bytes)", mp4Path, fileSize);
                
                return mp4Path;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Exception during WebM to MP4 conversion: {Message} {StackTrace}", ex);
            CleanupPartialFile(mp4Path);
            return null;
        }
    }

    /// <summary>
    /// Removes a partial or incomplete conversion file after failure
    /// </summary>
    private void CleanupPartialFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            _loggingService.LogWarning("Failed to delete partial conversion file: {FilePath}", filePath);
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
                    _loggingService.LogInfo("Cleaned up old cache file: {CacheFile}", Path.GetFileName(file));
                }
                catch (Exception)
                {
                    _loggingService.LogWarning("Failed to delete old cache file: {CacheFile}", Path.GetFileName(file));
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Error cleaning up old cache files: {Error}", ex.Message);
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
                catch (Exception)
                {
                    _loggingService.LogWarning("Failed to remove old cache file: {CacheFile}", file.Name);
                }
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
                    catch (Exception)
                    {
                        _loggingService.LogWarning("Failed to remove cache file for size management: {CacheFile}", file.Name);
                    }
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
