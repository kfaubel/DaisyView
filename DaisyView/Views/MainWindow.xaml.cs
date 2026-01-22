using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DaisyView.Models;
using DaisyView.Services;
using DaisyView.ViewModels;

namespace DaisyView.Views;

/// <summary>
/// Main application window
/// Displays file system tree view on the left and image thumbnails on the right
/// </summary>
public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;
    private SettingsService? _settingsService;

    public MainWindow()
    {
        InitializeComponent();
        
        _viewModel = new MainWindowViewModel();
        _settingsService = new SettingsService();
        DataContext = _viewModel;

        // Restore window size and position from settings
        RestoreWindowPlacement();

        // Handle window closing to clean up resources and save placement
        Closing += MainWindow_Closing;

        // Wire up events
        Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// Handles window closing - saves settings and cleans up resources
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Unsubscribe from ViewModel events
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        // Save window placement before closing
        if (_settingsService != null)
        {
            _settingsService.SaveWindowPlacement(
                this.Width,
                this.Height,
                this.Left,
                this.Top,
                this.WindowState.ToString());
        }

        // Dispose the ViewModel
        _viewModel?.Dispose();
    }

    /// <summary>
    /// Restores window size and position from saved settings
    /// </summary>
    private void RestoreWindowPlacement()
    {
        if (_settingsService == null)
            return;

        try
        {
            var width = _settingsService.GetWindowWidth();
            var height = _settingsService.GetWindowHeight();
            var left = _settingsService.GetWindowLeft();
            var top = _settingsService.GetWindowTop();
            var windowState = _settingsService.GetWindowState();

            // Restore window size if valid
            if (width > 0 && height > 0)
            {
                this.Width = width;
                this.Height = height;
            }

            // Restore window position only if it's reasonable
            // (positive coordinates suggest a valid saved position, not defaults)
            if (left >= 0 && top >= 0 && width > 100 && height > 100)
            {
                this.Left = left;
                this.Top = top;
            }
            else
            {
                // If no valid saved position, center the window
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // Restore window state (Normal, Maximized, or Minimized)
            if (Enum.TryParse<WindowState>(windowState, out var state))
            {
                this.WindowState = state;
            }
        }
        catch (Exception ex)
        {
            // If anything goes wrong, just use defaults
            System.Diagnostics.Debug.WriteLine($"Error restoring window placement: {ex.Message}");
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Register handler for Grid elements to capture double-click events
        EventManager.RegisterClassHandler(typeof(Grid), Control.MouseDoubleClickEvent,
            new MouseButtonEventHandler(Thumbnail_MouseDoubleClick), handledEventsToo: true);

        // Register keyboard handler for F11 to start slideshow
        PreviewKeyDown += MainWindow_PreviewKeyDown;

        // Subscribe to changes in the ViewModel
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            // Check clipboard initially
            UpdateClipboardStatus();
            // Set initial size button color
            UpdateSizeButtonColors(_viewModel.ThumbnailSize);
        }
    }

    /// <summary>
    /// Updates the clipboard content status in the ViewModel
    /// </summary>
    private void UpdateClipboardStatus()
    {
        if (_viewModel != null)
        {
            try
            {
                var data = System.Windows.Clipboard.GetDataObject();
                _viewModel.HasClipboardContent = data != null && data.GetDataPresent(DataFormats.FileDrop);
            }
            catch
            {
                _viewModel.HasClipboardContent = false;
            }
        }
    }

    /// <summary>
    /// Handles property changes in the ViewModel
    /// </summary>
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.ActiveImage))
        {
            ScrollThumbnailIntoView();
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.ActiveFolder))
        {
            ScrollTreeToActiveFolder();
        }
    }

    /// <summary>
    /// Scrolls the tree view to make the active folder visible
    /// </summary>
    private void ScrollTreeToActiveFolder()
    {
        if (FolderTreeView == null)
            return;

        var activeFolder = _viewModel?.ActiveFolder;
        if (activeFolder == null)
            return;

        // Defer the scroll to after rendering
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // Find the TreeViewItem for the active folder
            var item = FindTreeViewItem(FolderTreeView, activeFolder);
            if (item != null)
            {
                item.BringIntoView();
            }
        }), System.Windows.Threading.DispatcherPriority.Render);
    }

    /// <summary>
    /// Recursively finds the TreeViewItem for a given TreeNode
    /// </summary>
    private TreeViewItem? FindTreeViewItem(ItemsControl parent, TreeNode target, int depth = 0)
    {
        // Prevent infinite recursion with a reasonable depth limit
        if (depth > 50)
            return null;

        foreach (var item in parent.Items)
        {
            var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (container != null)
            {
                if (container.DataContext == target)
                    return container;

                // Recursively search children
                var childItem = FindTreeViewItem(container, target, depth + 1);
                if (childItem != null)
                    return childItem;
            }
        }
        return null;
    }

    /// <summary>
    /// Scrolls the thumbnail view to make the active image visible
    /// </summary>
    private void ScrollThumbnailIntoView()
    {
        if (ThumbnailPanel == null)
            return;

        var activeImage = _viewModel?.ActiveImage;
        if (activeImage == null)
            return;

        // Find the visual container for the active image
        var container = ThumbnailPanel.ItemContainerGenerator.ContainerFromItem(activeImage) as FrameworkElement;
        if (container != null)
        {
            container.BringIntoView();
        }
    }

    /// <summary>
    /// Handles tree view expansion to load subfolders
    /// </summary>
    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.Source is TreeViewItem item && item.DataContext is TreeNode node)
        {
            _viewModel?.ExpandFolder(node);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles tree view selection to navigate to the selected folder
    /// </summary>
    private void TreeViewItem_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // Skip navigation if we're already in the middle of a programmatic navigation
        if (_viewModel?.IsNavigating == true)
            return;

        if (e.NewValue is TreeNode node)
        {
            _viewModel?.NavigateToFolder(node.FullPath);
            // Note: ActiveFolder is set via the TwoWay binding when IsActive is set
        }
    }

    /// <summary>
    /// Handles favorite folder double-click to navigate
    /// </summary>
    private void FavoriteFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string folderPath)
        {
            _viewModel?.NavigateToFolder(folderPath);
        }
    }

    /// <summary>
    /// Handles thumbnail selection to set active image
    /// </summary>
    private void Thumbnail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Handle Grid element by getting DataContext
        var dataContext = (sender as FrameworkElement)?.DataContext as ImageFile;
        if (dataContext != null)
        {
            // Clear active state from all images
            foreach (var img in _viewModel!.Images)
                img.IsActive = false;

            // Set this image as active
            dataContext.IsActive = true;
            _viewModel.ActiveImage = dataContext;
        }
    }

    /// <summary>
    /// Handles thumbnail double-click to open slideshow
    /// </summary>
    private void Thumbnail_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel?.OpenSlideshow();
    }

    /// <summary>
    /// Handles keyboard input (F11 to open slideshow, Ctrl+A to select all)
    /// </summary>
    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            _viewModel?.OpenSlideshow();
            e.Handled = true;
        }
        else if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel?.SelectAll();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles small size button click
    /// </summary>
    private void SmallSizeButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.SetThumbnailSize("Small");
        UpdateSizeButtonColors("Small");
    }

    /// <summary>
    /// Handles medium size button click
    /// </summary>
    private void MediumSizeButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.SetThumbnailSize("Medium");
        UpdateSizeButtonColors("Medium");
    }

    /// <summary>
    /// Handles large size button click
    /// </summary>
    private void LargeSizeButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.SetThumbnailSize("Large");
        UpdateSizeButtonColors("Large");
    }

    /// <summary>
    /// Updates the size button colors - active size is gold, others are white
    /// </summary>
    private void UpdateSizeButtonColors(string activeSize)
    {
        if (SmallSizeButton != null)
            SmallSizeButton.Foreground = activeSize == "Small" 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gold)
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);

        if (MediumSizeButton != null)
            MediumSizeButton.Foreground = activeSize == "Medium"
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gold)
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);

        if (LargeSizeButton != null)
            LargeSizeButton.Foreground = activeSize == "Large"
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gold)
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
    }

    /// <summary>
    /// Handles the Copy button click - copies marked images to clipboard
    /// </summary>
    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        var markedFiles = _viewModel.Images.Where(i => i.IsMarked).Select(i => i.FilePath).ToList();
        if (markedFiles.Count == 0)
        {
            _viewModel.StatusMessage = "No images marked for copying.";
            return;
        }

        try
        {
            var fileCollection = new System.Collections.Specialized.StringCollection();
            fileCollection.AddRange(markedFiles.ToArray());
            
            var data = new DataObject();
            data.SetFileDropList(fileCollection);
            System.Windows.Clipboard.SetDataObject(data);
            
            UpdateClipboardStatus();
            _viewModel.StatusMessage = $"Copied {markedFiles.Count} image(s) to clipboard.";
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Error copying files: {ex.Message}";
        }
    }

    /// <summary>
    /// Handles the Cut button click - cuts marked images to clipboard
    /// Linked files from shortcuts cannot be cut - only directly present files
    /// </summary>
    private void CutButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        // Filter out files from shortcuts - they cannot be cut
        var markedFiles = _viewModel.Images
            .Where(i => i.IsMarked && !i.IsFromShortcut)
            .Select(i => i.FilePath)
            .ToList();
        
        var shortcutFilesCount = _viewModel.Images.Count(i => i.IsMarked && i.IsFromShortcut);
        
        if (markedFiles.Count == 0)
        {
            if (shortcutFilesCount > 0)
            {
                _viewModel.StatusMessage = $"Cannot cut linked files from shortcuts. {shortcutFilesCount} linked file(s) skipped.";
            }
            else
            {
                _viewModel.StatusMessage = "No images marked for cutting.";
            }
            return;
        }

        try
        {
            var fileCollection = new System.Collections.Specialized.StringCollection();
            fileCollection.AddRange(markedFiles.ToArray());
            
            var data = new DataObject();
            data.SetFileDropList(fileCollection);
            System.Windows.Clipboard.SetDataObject(data);
            
            UpdateClipboardStatus();
            var skipMessage = shortcutFilesCount > 0 ? $" ({shortcutFilesCount} linked file(s) skipped)" : "";
            _viewModel.StatusMessage = $"Cut {markedFiles.Count} image(s) to clipboard.{skipMessage}";
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Error cutting files: {ex.Message}";
        }
    }

    /// <summary>
    /// Handles the Paste button click - pastes images from clipboard to current folder
    /// </summary>
    private void PasteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || _viewModel.ActiveFolder == null)
        {
            if (_viewModel != null)
            {
                _viewModel.StatusMessage = "No folder selected for pasting.";
            }
            return;
        }

        try
        {
            var data = System.Windows.Clipboard.GetDataObject();
            if (data == null || !data.GetDataPresent(DataFormats.FileDrop))
            {
                _viewModel.StatusMessage = "No files in clipboard.";
                UpdateClipboardStatus();
                return;
            }

            var files = data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0)
            {
                _viewModel.StatusMessage = "No files in clipboard.";
                UpdateClipboardStatus();
                return;
            }

            // Copy files to the current folder
            var destFolder = _viewModel.ActiveFolder.FullPath;
            int copiedCount = 0;

            foreach (var filePath in files)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        var fileName = Path.GetFileName(filePath);
                        var destPath = Path.Combine(destFolder, fileName);
                        
                        // Handle duplicate filenames
                        if (File.Exists(destPath))
                        {
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                            var ext = Path.GetExtension(filePath);
                            int count = 1;
                            while (File.Exists(destPath))
                            {
                                destPath = Path.Combine(destFolder, $"{nameWithoutExt}_{count}{ext}");
                                count++;
                            }
                        }

                        File.Copy(filePath, destPath, overwrite: true);
                        copiedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _viewModel.StatusMessage = $"Error copying {Path.GetFileName(filePath)}: {ex.Message}";
                }
            }

            _viewModel.StatusMessage = $"Pasted {copiedCount} image(s).";
            UpdateClipboardStatus();
            _viewModel.NavigateToFolder(_viewModel.ActiveFolder.FullPath);
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Error pasting files: {ex.Message}";
        }
    }

    /// <summary>
    /// Handles the Delete button click - moves marked images to recycle bin
    /// Linked files from shortcuts cannot be deleted - only directly present files
    /// </summary>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        // Filter out files from shortcuts - they cannot be deleted
        var markedFiles = _viewModel.Images
            .Where(i => i.IsMarked && !i.IsFromShortcut)
            .Select(i => i.FilePath)
            .ToList();
        
        var shortcutFilesCount = _viewModel.Images.Count(i => i.IsMarked && i.IsFromShortcut);
        
        if (markedFiles.Count == 0)
        {
            if (shortcutFilesCount > 0)
            {
                _viewModel.StatusMessage = $"Cannot delete linked files from shortcuts. {shortcutFilesCount} linked file(s) skipped.";
            }
            else
            {
                _viewModel.StatusMessage = "No images marked for deletion.";
            }
            return;
        }

        var skipMessage = shortcutFilesCount > 0 ? $" ({shortcutFilesCount} linked file(s) will be skipped)" : "";
        var result = MessageBox.Show($"Move {markedFiles.Count} image(s) to recycle bin?{skipMessage}", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                foreach (var filePath in markedFiles)
                {
                    if (File.Exists(filePath))
                    {
                        // Send to recycle bin using Windows Shell
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filePath, 
                            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    }
                }

                var deletedMessage = shortcutFilesCount > 0 ? $" ({shortcutFilesCount} linked file(s) skipped)" : "";
                _viewModel.StatusMessage = $"Moved {markedFiles.Count} image(s) to recycle bin.{deletedMessage}";
                _viewModel.NavigateToFolder(_viewModel.ActiveFolder?.FullPath);
            }
            catch (Exception ex)
            {
                _viewModel.StatusMessage = $"Error deleting files: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Handles Help/About button click
    /// </summary>
    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        var helpWindow = new HelpWindow();
        helpWindow.Owner = this;
        helpWindow.ShowDialog();
    }
}