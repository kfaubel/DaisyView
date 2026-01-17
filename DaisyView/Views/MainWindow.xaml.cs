using System;
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

    public MainWindow()
    {
        InitializeComponent();
        
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        // Handle window closing to clean up resources
        Closing += (s, e) =>
        {
            _viewModel?.Dispose();
        };

        // Wire up events
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Register handler for Grid elements to capture double-click events
        EventManager.RegisterClassHandler(typeof(Grid), Control.MouseDoubleClickEvent,
            new MouseButtonEventHandler(Thumbnail_MouseDoubleClick), handledEventsToo: true);

        // Subscribe to changes in ActiveImage to scroll it into view
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
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
    /// Handles thumbnail size selection
    /// </summary>
    private void SizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && SizeComboBox.SelectedItem is ComboBoxItem item)
        {
            var sizeText = item.Content?.ToString();
            if (sizeText != null)
            {
                _viewModel.SetThumbnailSize(sizeText);
            }
        }
    }

    /// <summary>
    /// Handles the Move To button click
    /// </summary>
    private void MoveToButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
            return;

        // Create a temporary file system service for the dialog
        var settingsService = new SettingsService();
        var loggingService = new LoggingService(settingsService);
        var fileSystemService = new FileSystemService(loggingService);

        var moveDialog = new MoveToDialog(fileSystemService)
        {
            Owner = this
        };

        if (moveDialog.ShowDialog() == true)
        {
            if (moveDialog.MoveToTrash)
            {
                // TODO: Move to trash instead of permanent delete
                var markedFiles = _viewModel.Images.Where(i => i.IsMarked).Select(i => i.FilePath).ToList();
                // For now, we'll implement permanent delete - trash functionality can be added later
                _ = fileSystemService.DeleteFilesAsync(markedFiles);
            }
            else if (moveDialog.SelectedPath != null)
            {
                _viewModel.MoveMarkedImagesCommand.Execute(moveDialog.SelectedPath);
            }
        }

        fileSystemService.Dispose();
        loggingService.Dispose();
    }
}

/// <summary>
/// Value converter to convert boolean to brush for red/default borders
/// </summary>
public class BoolToRedBrushConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
/// <summary>
/// Value converter to convert byte array thumbnail data to BitmapImage
/// </summary>
public class ByteArrayToBitmapImageConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is byte[] thumbnailData && thumbnailData.Length > 0)
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new System.IO.MemoryStream(thumbnailData);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}