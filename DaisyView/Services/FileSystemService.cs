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
    /// Gets subdirectories and folder shortcuts for a given folder path.
    /// </summary>
    public List<TreeNode> GetSubfolders(string folderPath, TreeNode? parentNode = null)
    {
        var subfolders = new List<TreeNode>();

        try
        {
            var directory = new DirectoryInfo(folderPath);

            // Get all direct subdirectories (excluding hidden ones)
            var directories = directory.GetDirectories()
                .Where(d => (d.Attributes & FileAttributes.Hidden) == 0)
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var dir in directories)
            {
                var node = new TreeNode
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    Parent = parentNode,
                    IsExpanded = false,
                    IsActive = false,
                    IsShortcut = false,
                    ShortcutFilePath = null
                };

                if (HasVisibleChildren(dir.FullName))
                {
                    node.Children.Add(new TreeNode { Name = "Loading...", FullPath = "", Parent = node });
                }

                subfolders.Add(node);
            }

            // Include .lnk files that point to folders.
            var shortcutFiles = directory.GetFiles("*.lnk")
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var shortcutFile in shortcutFiles)
            {
                var targetFolder = ResolveShortcutToFolder(shortcutFile.FullName);
                if (targetFolder == null)
                    continue;

                var shortcutNode = new TreeNode
                {
                    Name = Path.GetFileNameWithoutExtension(shortcutFile.Name),
                    FullPath = targetFolder,
                    Parent = parentNode,
                    IsExpanded = false,
                    IsActive = false,
                    IsShortcut = true,
                    ShortcutFilePath = shortcutFile.FullName,
                    FileCount = CountMediaFiles(targetFolder)
                };

                if (HasVisibleChildren(targetFolder))
                {
                    shortcutNode.Children.Add(new TreeNode { Name = "Loading...", FullPath = "", Parent = shortcutNode });
                }

                subfolders.Add(shortcutNode);
            }

            subfolders = subfolders
                .OrderBy(node => node.IsShortcut)
                .ThenBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _loggingService.LogTrace("Found {SubfolderCount} folder entries in {FolderPath}", subfolders.Count, folderPath);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to get subfolders for {FolderPath}", ex, folderPath);
        }

        return subfolders;
    }

    /// <summary>
    /// Resolves a Windows shortcut (.lnk) file to get its target folder path.
    /// </summary>
    public string? ResolveShortcutToFolder(string shortcutPath)
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
    /// Resolves a Windows shortcut (.lnk) file to get its target FILE path.
    /// Returns null if the target does not exist or is a directory.
    /// </summary>
    public string? ResolveShortcutToFile(string shortcutPath)
    {
        try
        {
            var link = (IShellLinkW)new ShellLink();
            var persistFile = (IPersistFile)link;
            persistFile.Load(shortcutPath, 0);

            var targetPath = new StringBuilder(260);
            link.GetPath(targetPath, targetPath.Capacity, IntPtr.Zero, 0);

            var target = targetPath.ToString();
            if (!string.IsNullOrEmpty(target) && File.Exists(target))
            {
                return target;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogTrace("Failed to resolve file shortcut {ShortcutPath}: {Message}", shortcutPath, ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Creates a .lnk shortcut in destFolder pointing to a specific file.
    /// </summary>
    public string AddFileShortcut(string targetFilePath, string destFolder)
    {
        if (!File.Exists(targetFilePath))
            throw new FileNotFoundException($"Target file does not exist: {targetFilePath}");

        if (!Directory.Exists(destFolder))
            throw new DirectoryNotFoundException($"Destination folder does not exist: {destFolder}");

        var baseName = Path.GetFileNameWithoutExtension(targetFilePath);
        var shortcutPath = GetUniqueShortcutPath(destFolder, baseName);

        var link = (IShellLinkW)new ShellLink();
        link.SetPath(targetFilePath);
        link.SetDescription($"Favorite: {targetFilePath}");

        var persistFile = (IPersistFile)link;
        persistFile.Save(shortcutPath, true);

        _loggingService.LogUserAction("File shortcut created", $"{targetFilePath} -> {shortcutPath}");
        return shortcutPath;
    }

    /// <summary>
    /// Ensures a Favorites slot subfolder exists, creating it if needed.
    /// Slot names are "1"-"9" and "0".
    /// </summary>
    public string EnsureFavoritesSlotFolder(string favoritesFolderPath, string slot)
    {
        var slotFolder = Path.Combine(favoritesFolderPath, slot);
        if (!Directory.Exists(slotFolder))
        {
            Directory.CreateDirectory(slotFolder);
            _loggingService.LogUserAction("Favorites slot folder created", slotFolder);
        }
        return slotFolder;
    }

    private bool HasVisibleChildren(string folderPath)
    {
        try
        {
            var directory = new DirectoryInfo(folderPath);
            var hasRealSubfolders = directory.GetDirectories()
                .Any(d => (d.Attributes & FileAttributes.Hidden) == 0);

            if (hasRealSubfolders)
                return true;

            return directory.GetFiles("*.lnk")
                .Any(shortcut => ResolveShortcutToFolder(shortcut.FullName) != null);
        }
        catch
        {
            // If we cannot inspect children, keep expander available to try on demand.
            return true;
        }
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

            // Get images from shortcut files (folder shortcuts and file shortcuts)
            var shortcuts = directory.GetFiles("*.lnk");
            foreach (var shortcut in shortcuts)
            {
                // First try: folder shortcut → gather all media files from target folder
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
                    continue;
                }

                // Second try: file shortcut → the shortcut points directly to a media file
                var targetFile = ResolveShortcutToFile(shortcut.FullName);
                if (targetFile != null && MediaTypeHelper.IsSupportedMedia(targetFile))
                {
                    var isVideo = MediaTypeHelper.IsVideoFile(targetFile);
                    // Use the parent folder name (slot number) as a label
                    var slotName = Path.GetFileName(Path.GetDirectoryName(shortcut.FullName) ?? string.Empty);
                    images.Add(new ImageFile
                    {
                        FileName = Path.GetFileName(targetFile),
                        FilePath = targetFile,
                        IsMarked = false,
                        IsActive = false,
                        IsVideo = isVideo,
                        ThumbnailGenerated = false,
                        IsFromShortcut = true,
                        ShortcutName = slotName,
                        FavoriteShortcutFilePath = shortcut.FullName
                    });

                    _loggingService.LogTrace("Found favorite file shortcut '{ShortcutFile}' -> {TargetFile}",
                        shortcut.Name, targetFile);
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
    /// Creates a new folder.
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
    /// Adds a .lnk shortcut to sourceFolderPath inside virtualFolderPath.
    /// </summary>
    public string AddFolderShortcut(string sourceFolderPath, string virtualFolderPath)
    {
        try
        {
            if (!Directory.Exists(sourceFolderPath))
                throw new DirectoryNotFoundException($"Source folder does not exist: {sourceFolderPath}");

            if (!Directory.Exists(virtualFolderPath))
                throw new DirectoryNotFoundException($"Virtual folder does not exist: {virtualFolderPath}");

            var suggestedName = Path.GetFileName(Path.TrimEndingDirectorySeparator(sourceFolderPath));
            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = "Folder Shortcut";
            }

            var shortcutPath = GetUniqueShortcutPath(virtualFolderPath, suggestedName);

            var link = (IShellLinkW)new ShellLink();
            link.SetPath(sourceFolderPath);
            link.SetDescription($"Shortcut to {sourceFolderPath}");

            var persistFile = (IPersistFile)link;
            persistFile.Save(shortcutPath, true);

            _loggingService.LogUserAction("Folder shortcut created", $"{sourceFolderPath} -> {shortcutPath}");
            return shortcutPath;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to add shortcut for {SourceFolder} to {VirtualFolder}", ex, sourceFolderPath, virtualFolderPath);
            throw;
        }
    }

    /// <summary>
    /// Removes an existing folder shortcut (.lnk).
    /// </summary>
    public void RemoveFolderShortcut(string shortcutPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(shortcutPath) || !File.Exists(shortcutPath))
                return;

            if (!string.Equals(Path.GetExtension(shortcutPath), ".lnk", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only .lnk files can be removed with RemoveFolderShortcut.");

            File.Delete(shortcutPath);
            _loggingService.LogUserAction("Folder shortcut removed", shortcutPath);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to remove folder shortcut {ShortcutPath}", ex, shortcutPath);
            throw;
        }
    }

    /// <summary>
    /// Counts supported media files in a folder (non-recursive, best-effort).
    /// </summary>
    private static int CountMediaFiles(string folderPath)
    {
        try
        {
            return Directory.EnumerateFiles(folderPath)
                .Count(f => MediaTypeHelper.IsSupportedMedia(f));
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Renames a folder shortcut (.lnk) file.
    /// </summary>
    public string RenameShortcut(string shortcutPath, string newName)
    {
        try
        {
            if (!File.Exists(shortcutPath))
                throw new FileNotFoundException("Shortcut file not found.", shortcutPath);

            var directory = Path.GetDirectoryName(shortcutPath)
                ?? throw new InvalidOperationException("Cannot determine shortcut directory.");

            var newPath = Path.Combine(directory, newName + ".lnk");
            if (File.Exists(newPath))
                throw new InvalidOperationException($"A shortcut named '{newName}' already exists.");

            File.Move(shortcutPath, newPath);
            _loggingService.LogUserAction("Shortcut renamed", $"{shortcutPath} -> {newPath}");
            return newPath;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to rename shortcut {ShortcutPath}", ex, shortcutPath);
            throw;
        }
    }

    private static string GetUniqueShortcutPath(string virtualFolderPath, string baseName)
    {
        var sanitizedBaseName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(sanitizedBaseName))
        {
            sanitizedBaseName = "Folder Shortcut";
        }

        var index = 0;
        while (true)
        {
            var suffix = index == 0 ? string.Empty : $" ({index})";
            var candidatePath = Path.Combine(virtualFolderPath, $"{sanitizedBaseName}{suffix}.lnk");
            if (!File.Exists(candidatePath))
            {
                return candidatePath;
            }

            index++;
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
