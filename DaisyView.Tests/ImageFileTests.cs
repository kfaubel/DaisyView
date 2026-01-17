using Xunit;
using DaisyView.Models;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the ImageFile model
/// </summary>
public class ImageFileTests
{
    [Fact]
    public void ImageFile_InitializesWithCorrectValues()
    {
        // Arrange & Act
        var file = new ImageFile
        {
            FileName = "test.jpg",
            FilePath = "C:\\Pictures\\test.jpg",
            IsMarked = false,
            IsActive = false,
            ThumbnailGenerated = false
        };

        // Assert
        Assert.Equal("test.jpg", file.FileName);
        Assert.Equal("C:\\Pictures\\test.jpg", file.FilePath);
        Assert.False(file.IsMarked);
        Assert.False(file.IsActive);
        Assert.False(file.ThumbnailGenerated);
    }

    [Fact]
    public void ImageFile_CanBeMarked()
    {
        // Arrange
        var file = new ImageFile
        {
            FileName = "test.jpg",
            FilePath = "C:\\Pictures\\test.jpg"
        };

        // Act
        file.IsMarked = true;

        // Assert
        Assert.True(file.IsMarked);
    }

    [Fact]
    public void ImageFile_CanBeActive()
    {
        // Arrange
        var file = new ImageFile
        {
            FileName = "test.jpg",
            FilePath = "C:\\Pictures\\test.jpg"
        };

        // Act
        file.IsActive = true;

        // Assert
        Assert.True(file.IsActive);
    }
}
