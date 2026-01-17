using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Manages file system operations including tree view building and watching for changes
/// Monitors open folders for real-time updates while ignoring changes to closed folders
/// </summary>
public class FileSystemService
{
    private readonly LoggingService _loggingService;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tif", ".tiff", ".webp" };

    public event EventHandler<FileSystemEventArgs>? FileSystemChanged;

    public FileSystemService(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <summary>
    /// Gets all drive letters on the system, including mapped network drives
    /// </summary>
    public List<TreeNode> GetRootDrives()
    {
        var drives = new List<TreeNode>();

        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                // Skip hidden or inaccessible drives
                if (!drive.IsReady)
                    continue;

                var node = new TreeNode
                {
                    Name = $"{drive.Name.TrimEnd('\\')} ({drive.VolumeLabel})",
                    FullPath = drive.RootDirectory.FullName,
                    IsExpanded = false,
                    IsActive = false
                };
                
                // Add a placeholder child so the tree view expander shows
                node.Children.Add(new TreeNode { Name = "Loading...", FullPath = "" });

                drives.Add(node);
            }

            _loggingService.LogInfo("Found {DriveCount} accessible drives", drives.Count);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to get root drives", ex);
        }

        return drives;
    }

    /// <summary>
    /// Gets subdirectories for a given folder path
    /// </summary>
    public List<TreeNode> GetSubfolders(string folderPath, TreeNode? parentNode = null)
    {
        var subfolders = new List<TreeNode>();

        try
        {
            var directory = new DirectoryInfo(folderPath);
            
            // Get all subdirectories (excluding hidden ones)
            var directories = directory.GetDirectories()
                .Where(d => (d.Attributes & FileAttributes.Hidden) == 0)
                .OrderBy(d => d.Name)
                .ToList();

            foreach (var dir in directories)
            {
                var node = new TreeNode
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    Parent = parentNode,
                    IsExpanded = false,
                    IsActive = false
                };
                
                // Add placeholder child so the expander shows
                node.Children.Add(new TreeNode { Name = "Loading...", FullPath = "" });

                subfolders.Add(node);
            }

            _loggingService.LogTrace("Found {SubfolderCount} subfolders in {FolderPath}", subfolders.Count, folderPath);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to get subfolders for {FolderPath}", ex, folderPath);
        }

        return subfolders;
    }

    /// <summary>
    /// Gets image files in a folder (jpg, jpeg, png, gif, bmp, tif, tiff, webp)
    /// </summary>
    public List<ImageFile> GetImageFiles(string folderPath)
    {
        var images = new List<ImageFile>();

        try
        {
            var directory = new DirectoryInfo(folderPath);
            
            var files = directory.GetFiles()
                .Where(f => ImageExtensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
                .OrderBy(f => f.Name)
                .ToList();

            foreach (var file in files)
            {
                images.Add(new ImageFile
                {
                    FileName = file.Name,
                    FilePath = file.FullName,
                    IsMarked = false,
                    IsActive = false,
                    ThumbnailGenerated = false
                });
            }

            _loggingService.LogTrace("Found {ImageCount} image files in {FolderPath}", images.Count, folderPath);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to get image files for {FolderPath}", ex, folderPath);
        }

        return images;
    }

    /// <summary>
    /// Asynchronously gets image files in a folder
    /// </summary>
    public async Task<List<ImageFile>> GetImageFilesAsync(string folderPath)
    {
        return await Task.Run(() => GetImageFiles(folderPath));
    }

    /// <summary>
    /// Starts watching a folder for file system changes
    /// </summary>
    public void WatchFolder(string folderPath)
    {
        try
        {
            if (_watchers.ContainsKey(folderPath))
                return;

            var watcher = new FileSystemWatcher(folderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                IncludeSubdirectories = false
            };

            watcher.Created += OnFileSystemChanged;
            watcher.Deleted += OnFileSystemChanged;
            watcher.Renamed += OnFileSystemChanged;
            watcher.Changed += OnFileSystemChanged;

            watcher.EnableRaisingEvents = true;
            _watchers[folderPath] = watcher;

            _loggingService.LogTrace("Started watching folder: {FolderPath}", folderPath);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to watch folder {FolderPath}", ex, folderPath);
        }
    }

    /// <summary>
    /// Stops watching a folder for changes
    /// </summary>
    public void UnwatchFolder(string folderPath)
    {
        if (_watchers.TryGetValue(folderPath, out var watcher))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            _watchers.Remove(folderPath);
            _loggingService.LogTrace("Stopped watching folder: {FolderPath}", folderPath);
        }
    }

    /// <summary>
    /// Handles file system changes
    /// </summary>
    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        _loggingService.LogTrace("File system change detected: {ChangeType} - {Path}", e.ChangeType, e.FullPath);
        FileSystemChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Checks if a path exists and is accessible
    /// </summary>
    public bool PathExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Moves files to a destination folder
    /// </summary>
    public Task MoveFilesAsync(List<string> sourceFiles, string destinationFolder)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            Directory.CreateDirectory(destinationFolder);

            foreach (var sourceFile in sourceFiles)
            {
                var fileName = Path.GetFileName(sourceFile);
                var destFile = Path.Combine(destinationFolder, fileName);

                // Handle file name conflicts
                if (File.Exists(destFile))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(sourceFile);
                    var ext = Path.GetExtension(sourceFile);
                    int counter = 1;
                    
                    while (File.Exists(destFile))
                    {
                        destFile = Path.Combine(destinationFolder, $"{nameWithoutExt} ({counter}){ext}");
                        counter++;
                    }
                }

                File.Move(sourceFile, destFile, overwrite: false);
                _loggingService.LogUserAction("File moved", $"From: {sourceFile} To: {destFile}");
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _loggingService.LogFileSystemOperation("MoveFiles", destinationFolder, (long)duration);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to move files to {DestinationFolder}", ex, destinationFolder);
            throw;
        }
    }

    /// <summary>
    /// Moves files to trash/recycle bin
    /// </summary>
    public Task DeleteFilesAsync(List<string> files)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            foreach (var file in files)
            {
                // TODO: Implement proper trash/recycle bin functionality
                // For now, just delete the file
                File.Delete(file);
                _loggingService.LogUserAction("File deleted", file);
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _loggingService.LogFileSystemOperation("DeleteFiles", "multiple", (long)duration);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to delete files", ex);
            throw;
        }
    }

    /// <summary>
    /// Creates a new folder
    /// </summary>
    public void CreateFolder(string folderPath, string folderName)
    {
        try
        {
            var newFolderPath = Path.Combine(folderPath, folderName);
            Directory.CreateDirectory(newFolderPath);
            _loggingService.LogUserAction("Folder created", newFolderPath);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to create folder {FolderPath}", ex, folderPath);
            throw;
        }
    }

    /// <summary>
    /// Cleans up all watchers
    /// </summary>
    public void Dispose()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }
}
