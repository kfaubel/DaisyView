using System;
using System.Windows;
using DaisyView.Models;
using DaisyView.Services;

namespace DaisyView.Views;

/// <summary>
/// Dialog for selecting a destination folder to move marked images to
/// Also supports creating new folders and moving to trash
/// </summary>
public partial class MoveToDialog : Window
{
    private readonly FileSystemService _fileSystemService;
    private TreeNode? _selectedFolder;

    public string? SelectedPath { get; private set; }
    public bool MoveToTrash { get; private set; }

    public MoveToDialog(FileSystemService fileSystemService)
    {
        InitializeComponent();
        _fileSystemService = fileSystemService;

        // Load root drives
        var drives = _fileSystemService.GetRootDrives();
        foreach (var drive in drives)
        {
            FolderTreeView.Items.Add(CreateTreeViewItem(drive));
        }
    }

    /// <summary>
    /// Creates a WPF TreeViewItem from a TreeNode
    /// </summary>
    private System.Windows.Controls.TreeViewItem CreateTreeViewItem(TreeNode node)
    {
        var item = new System.Windows.Controls.TreeViewItem
        {
            Header = node.Name,
            DataContext = node
        };

        // Add dummy item if folder has subfolders (for lazy loading)
        if (HasSubfolders(node.FullPath))
        {
            item.Items.Add(new System.Windows.Controls.TreeViewItem { Header = "..." });
        }

        item.Expanded += TreeViewItem_OnExpanded;
        return item;
    }

    /// <summary>
    /// Checks if a folder has subfolders
    /// </summary>
    private bool HasSubfolders(string folderPath)
    {
        try
        {
            var subfolders = _fileSystemService.GetSubfolders(folderPath);
            return subfolders.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Handles lazy loading of subfolders when tree view item is expanded
    /// </summary>
    private void TreeViewItem_OnExpanded(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TreeViewItem item && item.DataContext is TreeNode node)
        {
            // Clear dummy item and load real subfolders
            if (item.Items.Count == 1 && item.Items[0] is System.Windows.Controls.TreeViewItem dummyItem && dummyItem.Header?.ToString() == "...")
            {
                item.Items.Clear();
            }

            if (item.Items.Count == 0)
            {
                var subfolders = _fileSystemService.GetSubfolders(node.FullPath, node);
                foreach (var subfolder in subfolders)
                {
                    item.Items.Add(CreateTreeViewItem(subfolder));
                }
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles tree view item selection
    /// </summary>
    private void TreeViewItem_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is System.Windows.Controls.TreeViewItem item && item.DataContext is TreeNode node)
        {
            _selectedFolder = node;
            MoveButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Handles the Move button click
    /// </summary>
    private void MoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFolder != null)
        {
            SelectedPath = _selectedFolder.FullPath;
            MoveToTrash = false;
            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Handles the New Folder button click
    /// </summary>
    private void NewFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFolder == null)
        {
            MessageBox.Show("Please select a folder first.", "No Folder Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // TODO: Show input dialog for folder name
        // var folderName = InputDialog.Show("Enter new folder name:", "New Folder");
        // if (folderName != null)
        // {
        //     _fileSystemService.CreateFolder(_selectedFolder.FullPath, folderName);
        //     // Reload the tree
        // }
    }

    /// <summary>
    /// Handles the Cancel button click
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Handles the Move to Trash button click
    /// </summary>
    private void TrashButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedPath = null;
        MoveToTrash = true;
        DialogResult = true;
        Close();
    }
}
