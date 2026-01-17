using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DaisyView.Models;

/// <summary>
/// Represents a node in the file system tree view
/// </summary>
public class TreeNode : INotifyPropertyChanged
{
    /// <summary>
    /// Display name of the folder (without path)
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Full path to the folder
    /// </summary>
    public required string FullPath { get; set; }

    /// <summary>
    /// Child nodes
    /// </summary>
    public ObservableCollection<TreeNode> Children { get; set; } = new();

    /// <summary>
    /// Parent node reference
    /// </summary>
    public TreeNode? Parent { get; set; }

    /// <summary>
    /// Whether this node is currently expanded in the tree view
    /// </summary>
    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    /// <summary>
    /// Whether this is the currently active (selected) node
    /// </summary>
    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
