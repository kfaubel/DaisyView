using Xunit;
using DaisyView.Models;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the TreeNode model
/// </summary>
public class TreeNodeTests
{
    #region Initialization Tests

    [Fact]
    public void TreeNode_InitializesWithCorrectValues()
    {
        // Arrange & Act
        var node = new TreeNode
        {
            Name = "TestFolder",
            FullPath = "C:\\TestFolder",
            IsExpanded = false,
            IsActive = false
        };

        // Assert
        Assert.Equal("TestFolder", node.Name);
        Assert.Equal("C:\\TestFolder", node.FullPath);
        Assert.False(node.IsExpanded);
        Assert.False(node.IsActive);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void TreeNode_DefaultValuesAreCorrect()
    {
        // Arrange & Act
        var node = new TreeNode { Name = "Test", FullPath = "C:\\Test" };

        // Assert
        Assert.False(node.IsExpanded);
        Assert.False(node.IsActive);
        Assert.Empty(node.Children);
        Assert.Null(node.Parent);
    }

    #endregion

    #region Children Tests

    [Fact]
    public void TreeNode_CanHaveChildren()
    {
        // Arrange
        var parent = new TreeNode { Name = "Parent", FullPath = "C:\\Parent" };
        var child = new TreeNode { Name = "Child", FullPath = "C:\\Parent\\Child", Parent = parent };

        // Act
        parent.Children.Add(child);

        // Assert
        Assert.Single(parent.Children);
        Assert.Equal("Child", parent.Children[0].Name);
        Assert.Equal(parent, child.Parent);
    }

    [Fact]
    public void TreeNode_CanHaveMultipleChildren()
    {
        // Arrange
        var parent = new TreeNode { Name = "Parent", FullPath = "C:\\Parent" };

        // Act
        parent.Children.Add(new TreeNode { Name = "Child1", FullPath = "C:\\Parent\\Child1", Parent = parent });
        parent.Children.Add(new TreeNode { Name = "Child2", FullPath = "C:\\Parent\\Child2", Parent = parent });
        parent.Children.Add(new TreeNode { Name = "Child3", FullPath = "C:\\Parent\\Child3", Parent = parent });

        // Assert
        Assert.Equal(3, parent.Children.Count);
    }

    [Fact]
    public void TreeNode_ChildrenCollection_IsObservable()
    {
        // Arrange
        var node = new TreeNode { Name = "Test", FullPath = "C:\\Test" };
        var collectionChangedRaised = false;
        node.Children.CollectionChanged += (s, e) => collectionChangedRaised = true;

        // Act
        node.Children.Add(new TreeNode { Name = "Child", FullPath = "C:\\Test\\Child" });

        // Assert
        Assert.True(collectionChangedRaised);
    }

    [Fact]
    public void TreeNode_CanNavigateParentHierarchy()
    {
        // Arrange
        var root = new TreeNode { Name = "Root", FullPath = "C:\\" };
        var level1 = new TreeNode { Name = "Level1", FullPath = "C:\\Level1", Parent = root };
        var level2 = new TreeNode { Name = "Level2", FullPath = "C:\\Level1\\Level2", Parent = level1 };
        root.Children.Add(level1);
        level1.Children.Add(level2);

        // Act & Assert
        Assert.Equal(root, level1.Parent);
        Assert.Equal(level1, level2.Parent);
        Assert.Null(root.Parent);
    }

    #endregion

    #region IsExpanded Property Tests

    [Fact]
    public void TreeNode_CanBeExpanded()
    {
        // Arrange
        var node = new TreeNode { Name = "TestFolder", FullPath = "C:\\TestFolder" };

        // Act
        node.IsExpanded = true;

        // Assert
        Assert.True(node.IsExpanded);
    }

    [Fact]
    public void TreeNode_IsExpanded_RaisesPropertyChanged()
    {
        // Arrange
        var node = new TreeNode { Name = "Test", FullPath = "C:\\Test" };
        var propertyChangedRaised = false;
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TreeNode.IsExpanded))
                propertyChangedRaised = true;
        };

        // Act
        node.IsExpanded = true;

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void TreeNode_IsExpanded_DoesNotRaisePropertyChanged_WhenValueUnchanged()
    {
        // Arrange
        var node = new TreeNode { Name = "Test", FullPath = "C:\\Test", IsExpanded = true };
        var propertyChangedCount = 0;
        node.PropertyChanged += (s, e) => propertyChangedCount++;

        // Act
        node.IsExpanded = true; // Same value

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    #endregion

    #region IsActive Property Tests

    [Fact]
    public void TreeNode_CanBeActive()
    {
        // Arrange
        var node = new TreeNode { Name = "TestFolder", FullPath = "C:\\TestFolder" };

        // Act
        node.IsActive = true;

        // Assert
        Assert.True(node.IsActive);
    }

    [Fact]
    public void TreeNode_IsActive_RaisesPropertyChanged()
    {
        // Arrange
        var node = new TreeNode { Name = "Test", FullPath = "C:\\Test" };
        var propertyChangedRaised = false;
        node.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TreeNode.IsActive))
                propertyChangedRaised = true;
        };

        // Act
        node.IsActive = true;

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void TreeNode_IsActive_DoesNotRaisePropertyChanged_WhenValueUnchanged()
    {
        // Arrange
        var node = new TreeNode { Name = "Test", FullPath = "C:\\Test", IsActive = true };
        var propertyChangedCount = 0;
        node.PropertyChanged += (s, e) => propertyChangedCount++;

        // Act
        node.IsActive = true; // Same value

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TreeNode_ChildrenCanBeCleared()
    {
        // Arrange
        var node = new TreeNode { Name = "Test", FullPath = "C:\\Test" };
        node.Children.Add(new TreeNode { Name = "Child1", FullPath = "C:\\Test\\Child1" });
        node.Children.Add(new TreeNode { Name = "Child2", FullPath = "C:\\Test\\Child2" });

        // Act
        node.Children.Clear();

        // Assert
        Assert.Empty(node.Children);
    }

    [Fact]
    public void TreeNode_ChildCanBeRemoved()
    {
        // Arrange
        var node = new TreeNode { Name = "Test", FullPath = "C:\\Test" };
        var child = new TreeNode { Name = "Child", FullPath = "C:\\Test\\Child" };
        node.Children.Add(child);

        // Act
        node.Children.Remove(child);

        // Assert
        Assert.Empty(node.Children);
    }

    #endregion
}
