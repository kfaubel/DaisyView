using Xunit;
using DaisyView.Models;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the TreeNode model
/// </summary>
public class TreeNodeTests
{
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
    public void TreeNode_CanBeActive()
    {
        // Arrange
        var node = new TreeNode { Name = "TestFolder", FullPath = "C:\\TestFolder" };

        // Act
        node.IsActive = true;

        // Assert
        Assert.True(node.IsActive);
    }
}
