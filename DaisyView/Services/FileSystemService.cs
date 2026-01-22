using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using DaisyView.Constants;
using DaisyView.Helpers;
using DaisyView.Models;

namespace DaisyView.Services;

/// <summary>
/// Manages file system operations including tree view building and watching for changes
/// Monitors open folders for real-time updates while ignoring changes to closed folders
/// </summary>
public class FileSystemService : IDisposable
{
    #region COM Interop for Shell Links

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    #endregion

    private bool _disposed = false;
    private readonly LoggingService _loggingService;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

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
                // Check if drive is ready with a timeout to avoid hanging on network/disconnected drives
                bool isReady = false;
                try
                {
                    var checkTask = Task.Run(() => drive.IsReady);
                    isReady = checkTask.Wait(TimeSpan.FromSeconds(1)) && checkTask.Result;
                }
                catch
                {
                    // If checking fails, assume not ready
                    isReady = false;
                }

                if (!isReady)
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
                
                // Only add placeholder if this folder has subdirectories
                try
                {
                    var hasSubfolders = dir.GetDirectories()
                        .Any(d => (d.Attributes & FileAttributes.Hidden) == 0);
                    
                    if (hasSubfolders)
                    {
                        node.Children.Add(new TreeNode { Name = "Loading...", FullPath = "" });
                    }
                }
                catch
                {
                    // If we can't check for subfolders, add placeholder anyway to be safe
                    node.Children.Add(new TreeNode { Name = "Loading...", FullPath = "" });
                }

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
    /// Resolves a Windows shortcut (.lnk) file to get its target path
    /// </summary>
    /// <param name="shortcutPath">Full path to the .lnk file</param>
    /// <returns>Target path, or null if resolution fails or target is not a directory</returns>
    private string? ResolveShortcutToFolder(string shortcutPath)
    {
        try
        {
            var link = (IShellLinkW)new ShellLink();
            var persistFile = (IPersistFile)link;
            persistFile.Load(shortcutPath, 0);

            var targetPath = new StringBuilder(260);
            link.GetPath(targetPath, targetPath.Capacity, IntPtr.Zero, 0);

            var target = targetPath.ToString();
            if (!string.IsNullOrEmpty(target) && Directory.Exists(target))
            {
                return target;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogTrace("Failed to resolve shortcut {ShortcutPath}: {Message}", shortcutPath, ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Gets image files in a folder (jpg, jpeg, png, gif, bmp, tif, tiff, webp, webm, mp4, avi, mpeg, mpg)
    /// Also includes images from folders referenced by shortcuts (.lnk files) in the current folder
    /// </summary>
    public List<ImageFile> GetImageFiles(string folderPath)
    {
        var images = new List<ImageFile>();

        try
        {
            var directory = new DirectoryInfo(folderPath);
            
            // Get direct image files
            var files = directory.GetFiles()
                .Where(f => MediaTypeHelper.IsSupportedMedia(f.FullName))
                .OrderBy(f => f.Name)
                .ToList();

            foreach (var file in files)
            {
                var isVideo = MediaTypeHelper.IsVideoFile(file.FullName);
                images.Add(new ImageFile
                {
                    FileName = file.Name,
                    FilePath = file.FullName,
                    IsMarked = false,
                    IsActive = false,
                    IsVideo = isVideo,
                    ThumbnailGenerated = false,
                    IsFromShortcut = false,
                    ShortcutName = null
                });
            }

            // Get images from shortcut folders
            var shortcuts = directory.GetFiles("*.lnk");
            foreach (var shortcut in shortcuts)
            {
                var targetFolder = ResolveShortcutToFolder(shortcut.FullName);
                if (targetFolder != null)
                {
                    var shortcutName = Path.GetFileNameWithoutExtension(shortcut.Name);
                    var targetDir = new DirectoryInfo(targetFolder);
                    
                    var shortcutFiles = targetDir.GetFiles()
                        .Where(f => MediaTypeHelper.IsSupportedMedia(f.FullName))
                        .OrderBy(f => f.Name)
                        .ToList();

                    foreach (var file in shortcutFiles)
                    {
                        var isVideo = MediaTypeHelper.IsVideoFile(file.FullName);
                        images.Add(new ImageFile
                        {
                            FileName = file.Name,
                            FilePath = file.FullName,
                            IsMarked = false,
                            IsActive = false,
                            IsVideo = isVideo,
                            ThumbnailGenerated = false,
                            IsFromShortcut = true,
                            ShortcutName = shortcutName
                        });
                    }

                    _loggingService.LogTrace("Found {Count} images in shortcut '{ShortcutName}' -> {TargetFolder}", 
                        shortcutFiles.Count, shortcutName, targetFolder);
                }
            }

            _loggingService.LogTrace("Found {ImageCount} image files in {FolderPath} (including shortcuts)", images.Count, folderPath);
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
            watcher.Error += OnFileSystemWatcherError;

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
    /// Handles FileSystemWatcher errors
    /// </summary>
    private void OnFileSystemWatcherError(object sender, ErrorEventArgs e)
    {
        var exception = e.GetException();
        _loggingService.LogError("FileSystemWatcher error: {Message}", exception, exception?.Message ?? "Unknown error");
    }

    /// <summary>
    /// Cleans up all watchers
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
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Created -= OnFileSystemChanged;
                watcher.Deleted -= OnFileSystemChanged;
                watcher.Renamed -= OnFileSystemChanged;
                watcher.Changed -= OnFileSystemChanged;
                watcher.Error -= OnFileSystemWatcherError;
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        _disposed = true;
    }
}
