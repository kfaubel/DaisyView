using Xunit;
using DaisyView.Models;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the ImageFile model
/// </summary>
public class ImageFileTests
{
    #region Initialization Tests

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
    public void ImageFile_DefaultValuesAreCorrect()
    {
        // Arrange & Act
        var file = new ImageFile
        {
            FileName = "test.jpg",
            FilePath = "C:\\test.jpg"
        };

        // Assert - check default values
        Assert.False(file.IsMarked);
        Assert.False(file.IsActive);
        Assert.False(file.ThumbnailGenerated);
        Assert.False(file.IsVideo);
        Assert.Null(file.ThumbnailData);
        Assert.Null(file.ConvertedVideoPath);
    }

    #endregion

    #region IsMarked Property Tests

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
    public void ImageFile_IsMarked_RaisesPropertyChanged()
    {
        // Arrange
        var file = new ImageFile { FileName = "test.jpg", FilePath = "C:\\test.jpg" };
        var propertyChangedRaised = false;
        file.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImageFile.IsMarked))
                propertyChangedRaised = true;
        };

        // Act
        file.IsMarked = true;

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void ImageFile_IsMarked_DoesNotRaisePropertyChanged_WhenValueUnchanged()
    {
        // Arrange
        var file = new ImageFile { FileName = "test.jpg", FilePath = "C:\\test.jpg", IsMarked = true };
        var propertyChangedCount = 0;
        file.PropertyChanged += (s, e) => propertyChangedCount++;

        // Act
        file.IsMarked = true; // Same value

        // Assert
        Assert.Equal(0, propertyChangedCount);
    }

    #endregion

    #region IsActive Property Tests

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

    [Fact]
    public void ImageFile_IsActive_RaisesPropertyChanged()
    {
        // Arrange
        var file = new ImageFile { FileName = "test.jpg", FilePath = "C:\\test.jpg" };
        var propertyChangedRaised = false;
        file.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImageFile.IsActive))
                propertyChangedRaised = true;
        };

        // Act
        file.IsActive = true;

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion

    #region ThumbnailData Property Tests

    [Fact]
    public void ImageFile_ThumbnailData_CanBeSet()
    {
        // Arrange
        var file = new ImageFile { FileName = "test.jpg", FilePath = "C:\\test.jpg" };
        var thumbnailBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header

        // Act
        file.ThumbnailData = thumbnailBytes;

        // Assert
        Assert.Equal(thumbnailBytes, file.ThumbnailData);
    }

    [Fact]
    public void ImageFile_ThumbnailData_RaisesPropertyChanged()
    {
        // Arrange
        var file = new ImageFile { FileName = "test.jpg", FilePath = "C:\\test.jpg" };
        var propertyChangedRaised = false;
        file.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImageFile.ThumbnailData))
                propertyChangedRaised = true;
        };

        // Act
        file.ThumbnailData = new byte[] { 0x01 };

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion

    #region ThumbnailGenerated Property Tests

    [Fact]
    public void ImageFile_ThumbnailGenerated_CanBeSet()
    {
        // Arrange
        var file = new ImageFile { FileName = "test.jpg", FilePath = "C:\\test.jpg" };

        // Act
        file.ThumbnailGenerated = true;

        // Assert
        Assert.True(file.ThumbnailGenerated);
    }

    [Fact]
    public void ImageFile_ThumbnailGenerated_RaisesPropertyChanged()
    {
        // Arrange
        var file = new ImageFile { FileName = "test.jpg", FilePath = "C:\\test.jpg" };
        var propertyChangedRaised = false;
        file.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImageFile.ThumbnailGenerated))
                propertyChangedRaised = true;
        };

        // Act
        file.ThumbnailGenerated = true;

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion

    #region IsVideo Property Tests

    [Fact]
    public void ImageFile_IsVideo_CanBeSet()
    {
        // Arrange
        var file = new ImageFile { FileName = "video.mp4", FilePath = "C:\\video.mp4" };

        // Act
        file.IsVideo = true;

        // Assert
        Assert.True(file.IsVideo);
    }

    [Fact]
    public void ImageFile_IsVideo_RaisesPropertyChanged()
    {
        // Arrange
        var file = new ImageFile { FileName = "video.mp4", FilePath = "C:\\video.mp4" };
        var propertyChangedRaised = false;
        file.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImageFile.IsVideo))
                propertyChangedRaised = true;
        };

        // Act
        file.IsVideo = true;

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion

    #region ConvertedVideoPath Property Tests

    [Fact]
    public void ImageFile_ConvertedVideoPath_CanBeSet()
    {
        // Arrange
        var file = new ImageFile { FileName = "video.webm", FilePath = "C:\\video.webm" };

        // Act
        file.ConvertedVideoPath = "C:\\cache\\video.mp4";

        // Assert
        Assert.Equal("C:\\cache\\video.mp4", file.ConvertedVideoPath);
    }

    [Fact]
    public void ImageFile_ConvertedVideoPath_RaisesPropertyChanged()
    {
        // Arrange
        var file = new ImageFile { FileName = "video.webm", FilePath = "C:\\video.webm" };
        var propertyChangedRaised = false;
        file.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImageFile.ConvertedVideoPath))
                propertyChangedRaised = true;
        };

        // Act
        file.ConvertedVideoPath = "C:\\cache\\video.mp4";

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion
}
