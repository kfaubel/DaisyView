using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DaisyView.Constants;
using DaisyView.Models;
using DaisyView.Services;

namespace DaisyView.ViewModels;

/// <summary>
/// Base class for ViewModels to implement INotifyPropertyChanged
/// </summary>
public class ViewModelBase : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Event args for folder navigation
/// </summary>
public class FolderNavigationEventArgs : EventArgs
{
    public required string FolderPath { get; set; }
}

/// <summary>
/// ViewModel for the main application window
/// Coordinates between the file system tree view and thumbnail view
/// </summary>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private bool _disposed = false;
    private readonly SettingsService _settingsService;
    private readonly LoggingService _loggingService;
    private readonly FileSystemService _fileSystemService;
    private readonly ThumbnailService _thumbnailService;
    private readonly VideoConversionService _videoConversionService;
    private readonly FitsImageService _fitsImageService;
    private bool _isNavigating = false;

    private ObservableCollection<TreeNode> _rootNodes = new();
    private TreeNode? _activeFolder;
    private ObservableCollection<ImageFile> _images = new();
    private ImageFile? _activeImage;
    private bool _randomEnabled;
    private bool _isFavorite;
    private string _thumbnailSize = "Medium";
    private List<string> _favorites = new();
    private bool _audioEnabled = true;
    private int _lastPriorityStartIndex = -1;
    private int _lastPriorityEndIndex = -1;
    private string? _currentFolderPath;

    // Commands
    private ICommand? _navigateToFolderCommand;
    private ICommand? _toggleFavoriteCommand;
    private ICommand? _toggleRandomCommand;
    private ICommand? _markImageCommand;
    private ICommand? _openSlideshowCommand;
    private ICommand? _toggleAudioCommand;
    private ICommand? _expandFolderCommand;
    private ICommand? _selectAllCommand;
    private ICommand? _invertSelectionCommand;

    public event EventHandler<FolderNavigationEventArgs>? FolderNavigated;

    public ObservableCollection<TreeNode> RootNodes
    {
        get => _rootNodes;
        set { _rootNodes = value; OnPropertyChanged(nameof(RootNodes)); }
    }

    public TreeNode? ActiveFolder
    {
        get => _activeFolder;
        set { _activeFolder = value; OnPropertyChanged(nameof(ActiveFolder)); }
    }

    public ObservableCollection<ImageFile> Images
    {
        get => _images;
        set 
        { 
            if (_images != null)
            {
                foreach (var img in _images)
                {
                    img.PropertyChanged -= Image_PropertyChanged;
                }
            }
            _images = value; 
            if (_images != null)
            {
                foreach (var img in _images)
                {
                    img.PropertyChanged += Image_PropertyChanged;
                }
            }
            OnPropertyChanged(nameof(Images));
        }
    }

    private void Image_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageFile.IsMarked))
        {
            OnPropertyChanged(nameof(MoveButtonEnabled));
            OnPropertyChanged(nameof(MarkedCount));
        }
    }

    /// <summary>
    /// Gets the count of marked/selected images
    /// </summary>
    public int MarkedCount => Images.Count(i => i.IsMarked);

    public ImageFile? ActiveImage
    {
        get => _activeImage;
        set
        {
            _activeImage = value;
            OnPropertyChanged(nameof(ActiveImage));
            if (_currentFolderPath != null && value != null)
                _settingsService.SetLastActiveImage(_currentFolderPath, value.FileName);
        }
    }

    public bool RandomEnabled
    {
        get => _randomEnabled;
        set { _randomEnabled = value; OnPropertyChanged(nameof(RandomEnabled)); }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set { _isFavorite = value; OnPropertyChanged(nameof(IsFavorite)); }
    }

    public string ThumbnailSize
    {
        get => _thumbnailSize;
        set { _thumbnailSize = value; OnPropertyChanged(nameof(ThumbnailSize)); OnPropertyChanged(nameof(ThumbnailSizePixels)); OnPropertyChanged(nameof(ThumbnailHeightPixels)); }
    }

    /// <summary>
    /// Gets the thumbnail size in pixels for binding to UI
    /// </summary>
    public int ThumbnailSizePixels
    {
        get => ThumbnailService.GetThumbnailSizePixels(_thumbnailSize);
    }

    /// <summary>
    /// Gets the thumbnail height in pixels based on 16:9 aspect ratio
    /// </summary>
    public int ThumbnailHeightPixels
    {
        get => (int)(ThumbnailSizePixels * 9 / 16.0);
    }

    public List<string> Favorites
    {
        get => _favorites;
        set { _favorites = value; OnPropertyChanged(nameof(Favorites)); }
    }

    public bool MoveButtonEnabled
    {
        get => Images.Any(i => i.IsMarked);
    }

    private bool _hasClipboardContent = false;

    public bool HasClipboardContent
    {
        get => _hasClipboardContent;
        set { _hasClipboardContent = value; OnPropertyChanged(nameof(HasClipboardContent)); }
    }

    /// <summary>
    /// Tracks files that were cut (for move operation) - these should be deleted after successful paste
    /// </summary>
    private List<string> _cutFiles = new();

    /// <summary>
    /// Gets or sets the list of files that were cut and pending deletion after paste
    /// </summary>
    public List<string> CutFiles
    {
        get => _cutFiles;
        set { _cutFiles = value; }
    }

    private string _statusMessage = "";

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
    }

    public bool AudioEnabled
    {
        get => _audioEnabled;
        set { _audioEnabled = value; OnPropertyChanged(nameof(AudioEnabled)); }
    }

    public bool IsNavigating
    {
        get => _isNavigating;
        set { _isNavigating = value; }
    }

    // Command Properties
    public ICommand NavigateToFolderCommand => _navigateToFolderCommand ??= new RelayCommand<string>(NavigateToFolder);
    public ICommand ToggleFavoriteCommand => _toggleFavoriteCommand ??= new RelayCommand(_ => ToggleFavorite(), _ => ActiveFolder != null);
    public ICommand ToggleRandomCommand => _toggleRandomCommand ??= new RelayCommand(_ => ToggleRandom(), _ => ActiveFolder != null && Images.Count > 0);
    public ICommand MarkImageCommand => _markImageCommand ??= new RelayCommand<ImageFile>(MarkImage, img => img != null);
    public ICommand OpenSlideshowCommand => _openSlideshowCommand ??= new RelayCommand(_ => OpenSlideshow(), _ => Images.Count > 0);
    public ICommand ToggleAudioCommand => _toggleAudioCommand ??= new RelayCommand(_ => ToggleAudio());
    public ICommand ExpandFolderCommand => _expandFolderCommand ??= new RelayCommand<TreeNode>(ExpandFolder, node => node != null);
    public ICommand SelectAllCommand => _selectAllCommand ??= new RelayCommand(_ => SelectAll(), _ => Images.Count > 0);
    public ICommand InvertSelectionCommand => _invertSelectionCommand ??= new RelayCommand(_ => InvertSelection(), _ => Images.Count > 0);

    /// <summary>
    /// Gets the settings service for shared access (e.g., window placement saving)
    /// </summary>
    public SettingsService SettingsService => _settingsService;

    public MainWindowViewModel()
    {
        _settingsService = new SettingsService();
        _loggingService = new LoggingService(_settingsService);
        _fileSystemService = new FileSystemService(_loggingService);
        _thumbnailService = new ThumbnailService(_loggingService);
        _videoConversionService = new VideoConversionService(_loggingService, _settingsService);
        _fitsImageService = new FitsImageService(_loggingService);

        // Subscribe to file system changes to refresh the view when files are added/removed
        _fileSystemService.FileSystemChanged += OnFileSystemChanged;

        LoadRootDrivesSync();
        LoadLastActiveFolder();
        LoadFavorites();
        LoadThumbnailSize();
    }

    /// <summary>
    /// Handles file system changes (files added, deleted, renamed in the current folder)
    /// </summary>
    private void OnFileSystemChanged(object? sender, System.IO.FileSystemEventArgs e)
    {
        _loggingService.LogTrace("File system change detected: {ChangeType} - {Path}", e.ChangeType, e.FullPath);
        
        // Refresh the current folder view on the UI thread
        if (ActiveFolder != null)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                // Check if the change was a directory change - if so, refresh the tree node's children
                var changedPath = e.FullPath;
                var parentPath = System.IO.Path.GetDirectoryName(changedPath);
                
                if (parentPath != null && string.Equals(parentPath, ActiveFolder.FullPath, StringComparison.OrdinalIgnoreCase))
                {
                    // The change happened in the current folder - refresh both images and subfolders
                    RefreshCurrentFolderSubfolders();
                }
                
                NavigateToFolder(ActiveFolder.FullPath);
            });
        }
    }

    /// <summary>
    /// Refreshes the subfolders of the current active folder in the tree view
    /// </summary>
    private void RefreshCurrentFolderSubfolders()
    {
        if (ActiveFolder == null)
            return;

        try
        {
            // Clear existing children and reload
            ActiveFolder.Children.Clear();
            var subfolders = _fileSystemService.GetSubfolders(ActiveFolder.FullPath, ActiveFolder);
            foreach (var subfolder in subfolders)
            {
                ActiveFolder.Children.Add(subfolder);
            }
            _loggingService.LogTrace("Refreshed subfolders for: {FolderPath}", ActiveFolder.FullPath);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to refresh subfolders for {FolderPath}", ex, ActiveFolder.FullPath);
        }
    }

    /// <summary>
    /// Loads the root drives into the tree view synchronously but with per-drive timeouts
    /// </summary>
    private void LoadRootDrivesSync()
    {
        try
        {
            _loggingService.LogTrace("LoadRootDrives START");
            var drives = _fileSystemService.GetRootDrives();
            RootNodes = new ObservableCollection<TreeNode>(drives);
            _loggingService.LogTrace("Loaded {DriveCount} root drives", drives.Count);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load root drives", ex);
            RootNodes = new ObservableCollection<TreeNode>();
        }
    }

    /// <summary>
    /// Loads the last active folder from settings
    /// </summary>
    private void LoadLastActiveFolder()
    {
        _loggingService.LogTrace("LoadLastActiveFolder() START");
        try
        {
            var lastPath = _settingsService.GetLastActiveFolderPath();
            _loggingService.LogTrace("Last active folder from settings: {Path}", lastPath ?? "null");
            
            if (!string.IsNullOrEmpty(lastPath))
            {
                // Check path existence with a timeout to avoid hanging on network drives
                var checkTask = Task.Run(() => _fileSystemService.PathExists(lastPath));
                var pathExists = checkTask.Wait(TimeSpan.FromSeconds(2)) && checkTask.Result;
                
                if (pathExists)
                {
                    _loggingService.LogTrace("Path exists, calling NavigateToFolder");
                    NavigateToFolder(lastPath);
                }
                else
                {
                    _loggingService.LogTrace("Path is unreachable or does not exist, using default");
                }
            }
            else
            {
                _loggingService.LogTrace("Path is null");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("LoadLastActiveFolder exception", ex);
        }
    }

    /// <summary>
    /// Loads favorite folders from settings, removing any that no longer exist
    /// </summary>
    private void LoadFavorites()
    {
        try
        {
            // Clean up any favorites that no longer exist on startup
            var removedCount = _settingsService.CleanupInvalidFavorites();
            if (removedCount > 0)
            {
                _loggingService.LogInfo("Removed {Count} invalid favorite folder(s) that no longer exist", removedCount);
            }
            
            Favorites = new List<string>(_settingsService.GetFavoriteFolders());
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load favorites", ex);
        }
    }

    /// <summary>
    /// Loads the thumbnail size from settings
    /// </summary>
    private void LoadThumbnailSize()
    {
        try
        {
            var settings = _settingsService.GetSettings();
            ThumbnailSize = settings.ThumbnailSize;
            _thumbnailService.SetThumbnailSize(settings.ThumbnailSize);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load thumbnail size", ex);
        }
    }

    /// <summary>
    /// Navigates to a specific folder
    /// </summary>
    public void NavigateToFolder(string? folderPath)
    {
        _loggingService.LogTrace("NavigateToFolder called with: {Path}", folderPath ?? "null");
        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }

        _ = NavigateToFolderAsync(folderPath);
    }

    /// <summary>
    /// Asynchronously navigates to a folder and loads images without blocking the UI
    /// </summary>
    private async Task NavigateToFolderAsync(string? folderPath)
    {
        _loggingService.LogTrace("NavigateToFolderAsync START for: {Path}", folderPath);
        
        if (string.IsNullOrEmpty(folderPath))
            return;

        _isNavigating = true;

        try
        {
            if (!_fileSystemService.PathExists(folderPath))
            {
                _loggingService.LogWarning("Attempted to navigate to non-existent folder: {FolderPath}", folderPath);
                return;
            }

            _loggingService.LogUserAction("Navigate to folder", folderPath);

            // Cancel thumbnail generation for the previous folder
            _thumbnailService.CancelBackgroundGeneration();

            // Load images from the new folder asynchronously
            var images = await _fileSystemService.GetImageFilesAsync(folderPath);
            Images = new ObservableCollection<ImageFile>(images);
            _currentFolderPath = folderPath;

            // Restore last active image for this folder, or fall back to first image
            var lastActiveFileName = _settingsService.GetLastActiveImage(folderPath);
            var restoredImage = lastActiveFileName != null
                ? images.FirstOrDefault(i => string.Equals(i.FileName, lastActiveFileName, StringComparison.OrdinalIgnoreCase))
                : null;

            if (restoredImage != null)
            {
                restoredImage.IsActive = true;
                ActiveImage = restoredImage;
            }
            else if (images.Count > 0)
            {
                images[0].IsActive = true;
                ActiveImage = images[0];
            }
            else
            {
                ActiveImage = null;
            }

            // Update settings
            _settingsService.SetLastActiveFolderPath(folderPath);

            // Watch this folder for changes
            _fileSystemService.WatchFolder(folderPath);

            // Check if this folder is a favorite
            IsFavorite = _settingsService.IsFavorite(folderPath);

            // Generate thumbnails
            var visibleCount = AppConstants.ThumbnailSizes.DefaultVisibleCount;

            // Load random order if it was previously enabled
            var randomOrder = _settingsService.GetRandomOrder(folderPath);
            if (randomOrder != null)
            {
                RandomEnabled = true;
                ReorderImagesRandomly(randomOrder);
                // After reordering, Images has been updated, so use that for thumbnails
                _thumbnailService.GenerateThumbnailsAsync(Images.ToList(), visibleCount);
            }
            else
            {
                RandomEnabled = false;
                // Generate thumbnails for the normal order
                _thumbnailService.GenerateThumbnailsAsync(images, visibleCount);
            }

            _lastPriorityStartIndex = -1;
            _lastPriorityEndIndex = -1;

            // Start background conversion of WebM files to populate cache
            _ = StartBackgroundVideoConversionAsync(images);

            // Fire navigation event
            FolderNavigated?.Invoke(this, new FolderNavigationEventArgs { FolderPath = folderPath });
            
            // Expand the tree to show this folder and mark it as active
            // Wait for the initial delay, then expand and mark the folder
            await Task.Delay(100);
            await ExpandAndMarkFolderAsync(folderPath);
            
            // Refresh the subfolders for the active folder to show any new/deleted folders
            RefreshCurrentFolderSubfolders();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("NavigateToFolderAsync exception", ex);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    /// <summary>
    /// Asynchronously expands the tree view to show a folder and marks it as active
    /// </summary>
    private async Task ExpandAndMarkFolderAsync(string folderPath)
    {
        var pathParts = folderPath.Split(new[] { System.IO.Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0)
            return;

        // Find the root drive
        var rootDrive = pathParts[0] + System.IO.Path.DirectorySeparatorChar;
        var rootNode = RootNodes.FirstOrDefault(n => n.FullPath.Equals(rootDrive, StringComparison.OrdinalIgnoreCase));
        
        if (rootNode == null)
            return;

        // Expand root and wait for it to load
        await ExpandFolderAsync(rootNode);
        rootNode.IsExpanded = true;
        
        // Traverse down the path, expanding each folder
        var currentPath = rootDrive;
        var currentNode = rootNode;
        TreeNode? targetNode = null;

        for (int i = 1; i < pathParts.Length; i++)
        {
            currentPath = System.IO.Path.Combine(currentPath, pathParts[i]);
            
            // Find the child matching this path segment
            var childNode = currentNode.Children.FirstOrDefault(n => 
                n.FullPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase));

            if (childNode == null)
                break; // Path doesn't exist in tree

            // Expand this folder and wait for it to load before continuing
            await ExpandFolderAsync(childNode);
            childNode.IsExpanded = true; // Explicitly ensure it's expanded
            
            // Keep track of the final target node
            targetNode = childNode;
            currentNode = childNode;
        }
        
        // Only mark the final target folder as active
        if (targetNode != null)
        {
            targetNode.IsActive = true;
            ActiveFolder = targetNode;
        }
    }

    /// <summary>
    /// Asynchronously expands a folder and loads its children
    /// </summary>
    private async Task ExpandFolderAsync(TreeNode? node)
    {
        await ExpandFolderInternalAsync(node);
    }

    /// <summary>
    /// Expands a folder in the tree view and loads its children asynchronously
    /// </summary>
    public async void ExpandFolder(TreeNode? node)
    {
        await ExpandFolderInternalAsync(node);
    }

    /// <summary>
    /// Internal implementation for folder expansion - shared by both async and sync callers
    /// </summary>
    private async Task ExpandFolderInternalAsync(TreeNode? node)
    {
        if (node == null)
            return;

        _loggingService.LogTrace("ExpandFolder called for: {FolderPath}", node.FullPath);

        try
        {
            // Remove placeholder if it exists
            var placeholder = node.Children.FirstOrDefault(c => c.FullPath == "");
            if (placeholder != null)
            {
                _loggingService.LogTrace("Removing placeholder from: {FolderPath}", node.FullPath);
                node.Children.Remove(placeholder);
            }

            if (node.Children.Count == 0)
            {
                // Load children for this node asynchronously
                _loggingService.LogTrace("Loading subfolders for: {FolderPath}", node.FullPath);

                // Run file system operation on background thread
                var subfolders = await Task.Run(() => _fileSystemService.GetSubfolders(node.FullPath, node));

                _loggingService.LogTrace("Got {SubfolderCount} subfolders for: {FolderPath}", subfolders.Count, node.FullPath);

                foreach (var subfolder in subfolders)
                {
                    node.Children.Add(subfolder);
                }

                _loggingService.LogTrace("Expanded folder: {FolderPath}", node.FullPath);
            }

            node.IsExpanded = true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to expand folder {FolderPath}", ex, node.FullPath);
        }
    }

    /// <summary>
    /// Starts background conversion of video files to MP4 to populate the cache
    /// This runs asynchronously without blocking the UI
    /// Only WebM files need conversion; MP4, AVI, MPEG, MPG play directly
    /// </summary>
    private async Task StartBackgroundVideoConversionAsync(List<ImageFile> images)
    {
        try
        {
            // Get only files that need conversion: WebM, AVI, MPEG, MPG
            // (MP4 is already supported by MediaElement, so doesn't need conversion)
            var filesToConvert = images.Where(img => img.IsVideo)
                .Where(img =>
                {
                    var ext = System.IO.Path.GetExtension(img.FilePath).ToLower();
                    return ext == ".webm" || ext == ".avi" || ext == ".mpeg" || ext == ".mpg";
                })
                .ToList();

            if (filesToConvert.Count == 0)
            {
                _loggingService.LogTrace("No files found for background conversion");
                StatusMessage = "Cache is up to date.";
                return;
            }

            _loggingService.LogInfo("Starting background conversion of {FileCount} files", filesToConvert.Count);

            int convertedCount = 0;

            // Convert each file asynchronously
            foreach (var file in filesToConvert)
            {
                try
                {
                    // Check if already converted
                    var cacheFilePath = _videoConversionService.GetConvertedFilePath(file.FilePath);
                    if (cacheFilePath != null && System.IO.File.Exists(cacheFilePath))
                    {
                        _loggingService.LogTrace("File already converted: {FilePath}", file.FileName);
                        // Store the cache file path (.daicache) - slideshow will convert to playback path as needed
                        file.ConvertedVideoPath = cacheFilePath;
                        convertedCount++;
                        StatusMessage = $"[{convertedCount}/{filesToConvert.Count} files cached]";
                        continue;
                    }

                    // Convert the file and update the ImageFile with the converted cache path
                    _loggingService.LogInfo("Converting file to MP4 in background: {FileName}", file.FileName);
                    var convertedCachePath = await _videoConversionService.ConvertWebmToMp4Async(file.FilePath);
                    
                    if (!string.IsNullOrEmpty(convertedCachePath) && System.IO.File.Exists(convertedCachePath))
                    {
                        // Store the cache file path (.daicache) - slideshow will convert to playback path as needed
                        file.ConvertedVideoPath = convertedCachePath;
                        convertedCount++;
                        _loggingService.LogInfo("Background conversion completed: {FileName} -> {CachePath}", file.FileName, convertedCachePath);
                    }
                    else
                    {
                        _loggingService.LogWarning("Background conversion failed: {FileName}", file.FileName);
                    }

                    // Update status message
                    StatusMessage = $"[{convertedCount}/{filesToConvert.Count} files cached]";
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Error during background conversion of {FileName}", ex, file.FileName);
                }
            }

            _loggingService.LogInfo("Background conversion completed for {FileCount} files", filesToConvert.Count);
            
            // Show final cache status message
            StatusMessage = "Cache is up to date.";
        }
        catch (Exception ex)
        {
            _loggingService.LogError("StartBackgroundVideoConversionAsync error", ex);
            StatusMessage = "Cache update failed.";
        }
    }

    /// <summary>
    /// Toggles the favorite status of the active folder
    /// </summary>
    public void ToggleFavorite()
    {
        if (ActiveFolder == null)
        {
            _loggingService.LogWarning("ToggleFavorite called but ActiveFolder is null");
            return;
        }

        try
        {
            _loggingService.LogInfo("ToggleFavorite: Current IsFavorite={IsFavorite}, Path={Path}", 
                IsFavorite, ActiveFolder.FullPath);
            
            // Toggle the favorite state
            // Note: We manage the state ourselves rather than relying on binding order
            if (IsFavorite)
            {
                // Currently favorite, so remove it
                _settingsService.RemoveFavoriteFolder(ActiveFolder.FullPath);
                _loggingService.LogUserAction("Removed favorite", ActiveFolder.FullPath);
                IsFavorite = false;
            }
            else
            {
                // Not currently favorite, so add it
                _settingsService.AddFavoritFolder(ActiveFolder.FullPath);
                _loggingService.LogUserAction("Added favorite", ActiveFolder.FullPath);
                IsFavorite = true;
            }

            _loggingService.LogInfo("ToggleFavorite: After toggle IsFavorite={IsFavorite}", IsFavorite);
            LoadFavorites();
            _loggingService.LogInfo("ToggleFavorite: Favorites count after LoadFavorites={Count}", 
                Favorites.Count);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to toggle favorite", ex);
        }
    }

    /// <summary>
    /// Toggles random sort for the active folder
    /// </summary>
    public void ToggleRandom()
    {
        if (ActiveFolder == null)
            return;

        try
        {
            _loggingService.LogUserAction("Toggle random sort", $"Current state: {RandomEnabled}, Will be: {!RandomEnabled}");
            _loggingService.LogTrace("Images count before toggle: {Count}", Images.Count);
            _loggingService.LogTrace("Images before toggle: {Images}", string.Join(", ", Images.Select(i => i.FileName)));
            
            RandomEnabled = !RandomEnabled;
            _loggingService.LogTrace("RandomEnabled is now: {RandomEnabled}", RandomEnabled);

            if (RandomEnabled)
            {
                // Create random order using Fisher-Yates shuffle for better randomness
                var fileNames = Images.Select(i => i.FileName).ToList();
                _loggingService.LogTrace("Starting random shuffle with {Count} images", fileNames.Count);
                _loggingService.LogTrace("Before shuffle (first 5): {Order}", string.Join(", ", fileNames.Take(5)));
                
                var randomOrder = fileNames.ToList();
                
                // Fisher-Yates shuffle using Random.Shared for better randomness
                for (int i = randomOrder.Count - 1; i > 0; i--)
                {
                    int randomIndex = Random.Shared.Next(i + 1);
                    // Swap
                    var temp = randomOrder[i];
                    randomOrder[i] = randomOrder[randomIndex];
                    randomOrder[randomIndex] = temp;
                }

                _loggingService.LogTrace("After shuffle (first 5): {Order}", string.Join(", ", randomOrder.Take(5)));
                _loggingService.LogTrace("Full random order: {RandomOrder}", string.Join(", ", randomOrder));
                _settingsService.SaveRandomOrder(ActiveFolder.FullPath, randomOrder);
                _loggingService.LogTrace("Calling ReorderImagesRandomly with {Count} items", randomOrder.Count);
                ReorderImagesRandomly(randomOrder);
                _loggingService.LogTrace("After ReorderImagesRandomly, Images: {Images}", string.Join(", ", Images.Select(i => i.FileName)));
                
                // Regenerate thumbnails in the new random order
                var visibleCount = AppConstants.ThumbnailSizes.DefaultVisibleCount;
                _loggingService.LogTrace("Regenerating thumbnails for {Count} images in random order", Images.Count);
                _thumbnailService.GenerateThumbnailsAsync(Images.ToList(), visibleCount);
            }
            else
            {
                // Clear random order
                _loggingService.LogTrace("Disabling random order");
                _settingsService.ClearRandomOrder(ActiveFolder.FullPath);

                // Reload images in normal order
                var images = _fileSystemService.GetImageFiles(ActiveFolder.FullPath);
                _loggingService.LogTrace("Reloaded {Count} images in alphabetical order: {Images}", images.Count, string.Join(", ", images.Select(i => i.FileName)));
                Images = new ObservableCollection<ImageFile>(images);
                
                // Generate thumbnails for the reloaded images
                var visibleCount = AppConstants.ThumbnailSizes.DefaultVisibleCount;
                _loggingService.LogTrace("Regenerating thumbnails for {Count} images in alphabetical order", Images.Count);
                _thumbnailService.GenerateThumbnailsAsync(images, visibleCount);
                
                if (images.Count > 0)
                {
                    images[0].IsActive = true;
                    ActiveImage = images[0];
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to toggle random sort", ex);
        }
    }

    /// <summary>
    /// Toggles audio playback during slideshow mode
    /// </summary>
    public void ToggleAudio()
    {
        try
        {
            AudioEnabled = !AudioEnabled;
            _loggingService.LogUserAction("Toggle audio", $"Audio is now: {(AudioEnabled ? "enabled" : "disabled")}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to toggle audio", ex);
        }
    }

    /// <summary>
    /// Reorders images based on a provided list of file names
    /// </summary>
    private void ReorderImagesRandomly(List<string> fileNameOrder)
    {
        _loggingService.LogTrace("ReorderImagesRandomly called with {Count} items", fileNameOrder.Count);
        _loggingService.LogTrace("Original order: {Order}", string.Join(", ", Images.Select(i => i.FileName)));
        _loggingService.LogTrace("Target order: {Order}", string.Join(", ", fileNameOrder));

        // Create a mapping of filename to image for fast lookup
        var imageMap = Images.ToDictionary(i => i.FileName);
        _loggingService.LogTrace("Created imageMap with {Count} items", imageMap.Count);
        
        // Create new collection in the specified order
        var reorderedImages = new ObservableCollection<ImageFile>();
        foreach (var fileName in fileNameOrder)
        {
            if (imageMap.TryGetValue(fileName, out var image))
            {
                reorderedImages.Add(image);
                _loggingService.LogTrace("Added {FileName} to reordered collection", fileName);
            }
            else
            {
                _loggingService.LogWarning("Image not found for fileName: {FileName}", fileName);
            }
        }

        // Add any images not in the order (shouldn't happen but safety check)
        var missingCount = 0;
        foreach (var image in Images)
        {
            if (!reorderedImages.Contains(image))
            {
                _loggingService.LogWarning("Image not in reordered list, adding: {FileName}", image.FileName);
                reorderedImages.Add(image);
                missingCount++;
            }
        }
        if (missingCount > 0)
        {
            _loggingService.LogWarning("Had to add {MissingCount} missing images to reordered collection", missingCount);
        }

        _loggingService.LogTrace("Reordered images collection: {Order}", string.Join(", ", reorderedImages.Select(i => i.FileName)));
        _loggingService.LogTrace("Final reordered images count: {Count}", reorderedImages.Count);

        // Replace the entire collection to trigger UI refresh
        _loggingService.LogTrace("Replacing Images collection with reordered collection");
        Images = reorderedImages;
        _loggingService.LogTrace("Images collection replaced, new count: {Count}, new order: {Order}", Images.Count, string.Join(", ", Images.Select(i => i.FileName)));
        
        // Update active to first image
        if (Images.Count > 0)
        {
            foreach (var img in Images)
                img.IsActive = false;
            
            Images[0].IsActive = true;
            ActiveImage = Images[0];
            _loggingService.LogTrace("Set first image as active: {FileName}", Images[0].FileName);
        }
    }

    /// <summary>
    /// Marks or unmarks a specific image
    /// </summary>
    public void MarkImage(ImageFile? image)
    {
        if (image != null)
        {
            image.IsMarked = !image.IsMarked;
            _loggingService.LogUserAction("Toggle image mark", $"File: {image.FileName}, Marked: {image.IsMarked}");
        }
    }

    /// <summary>
    /// Marks/unmarks the active image
    /// </summary>
    public void ToggleImageMark()
    {
        if (ActiveImage != null)
        {
            MarkImage(ActiveImage);
        }
    }

    /// <summary>
    /// Selects (marks) all images in the current folder
    /// </summary>
    public void SelectAll()
    {
        foreach (var image in Images)
        {
            image.IsMarked = true;
        }
        _loggingService.LogUserAction("Select all images", $"Marked {Images.Count} images");
    }

    /// <summary>
    /// Inverts the selection (marks) of all images
    /// </summary>
    public void InvertSelection()
    {
        foreach (var image in Images)
        {
            image.IsMarked = !image.IsMarked;
        }
        _loggingService.LogUserAction("Invert selection", $"Toggled {Images.Count} images");
    }

    /// <summary>
    /// Opens the slideshow view with the current images
    /// </summary>
    public void OpenSlideshow()
    {
        if (Images.Count == 0)
            return;

        try
        {
            var currentIndex = ActiveImage != null ? Images.IndexOf(ActiveImage) : 0;
            var slideshowWindow = new Views.SlideshowWindow(Images.ToList(), currentIndex, _videoConversionService, AudioEnabled, _fitsImageService);
            slideshowWindow.ShowDialog();

            // Update image states after slideshow closes
            var closedImages = slideshowWindow.GetImages();
            foreach (var image in Images)
            {
                var closedImage = closedImages.FirstOrDefault(i => i.FilePath == image.FilePath);
                if (closedImage != null)
                {
                    image.IsMarked = closedImage.IsMarked;
                }
            }

            // Set the last viewed image as active
            var lastImage = slideshowWindow.GetCurrentImage();
            if (lastImage != null)
            {
                var activeImage = Images.FirstOrDefault(i => i.FilePath == lastImage.FilePath);
                if (activeImage != null)
                {
                    // Clear IsActive on all images first
                    foreach (var image in Images)
                    {
                        image.IsActive = false;
                    }
                    activeImage.IsActive = true;
                    ActiveImage = activeImage;
                }
            }

            _loggingService.LogUserAction("Closed slideshow", $"Last viewed: {ActiveImage?.FileName}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to open slideshow", ex);
        }
    }

    /// <summary>
    /// Changes the thumbnail size and saves the setting
    /// </summary>
    public void SetThumbnailSize(string size)
    {
        ThumbnailSize = size;
        _settingsService.UpdateSetting(s => { s.ThumbnailSize = size; });
        _loggingService.LogUserAction("Changed thumbnail size", size);

        // Regenerate thumbnails with the new size
        if (Images.Count > 0)
        {
            _thumbnailService.CancelBackgroundGeneration();
            _thumbnailService.SetThumbnailSize(size);
            
            // Clear existing thumbnail data and regenerate
            foreach (var image in Images)
            {
                image.ThumbnailGenerated = false;
                image.ThumbnailData = Array.Empty<byte>();
            }

            // Regenerate with new size
            var visibleCount = AppConstants.ThumbnailSizes.DefaultVisibleCount;
            _thumbnailService.GenerateThumbnailsAsync(Images.ToList(), visibleCount);

            _lastPriorityStartIndex = -1;
            _lastPriorityEndIndex = -1;
        }
    }

    /// <summary>
    /// Reprioritizes thumbnail generation so tiles currently in view are processed first.
    /// </summary>
    public void PrioritizeVisibleThumbnails(int visibleStartIndex, int visibleEndIndex)
    {
        if (Images.Count == 0)
            return;

        var safeStart = Math.Clamp(visibleStartIndex, 0, Images.Count - 1);
        var safeEnd = Math.Clamp(visibleEndIndex, safeStart, Images.Count - 1);

        if (safeStart == _lastPriorityStartIndex && safeEnd == _lastPriorityEndIndex)
            return;

        _lastPriorityStartIndex = safeStart;
        _lastPriorityEndIndex = safeEnd;
        _thumbnailService.GenerateThumbnailsAsync(Images.ToList(), safeStart, safeEnd);
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
            // Unsubscribe from file system changes
            _fileSystemService.FileSystemChanged -= OnFileSystemChanged;
            
            // Dispose managed resources
            _fileSystemService.Dispose();
            _thumbnailService.Dispose();
            
            // Unsubscribe from image property changes
            if (_images != null)
            {
                foreach (var img in _images)
                {
                    img.PropertyChanged -= Image_PropertyChanged;
                }
            }
        }

        _disposed = true;
    }
}
