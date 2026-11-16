using Xunit;
using DaisyView.Services;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the ThumbnailService
/// </summary>
public class ThumbnailServiceTests
{
    [Theory]
    [InlineData("small", 150)]
    [InlineData("Small", 150)]
    [InlineData("SMALL", 150)]
    [InlineData("medium", 300)]
    [InlineData("Medium", 300)]
    [InlineData("large", 500)]
    [InlineData("Large", 500)]
    public void GetThumbnailSizePixels_ReturnsCorrectSize(string sizeString, int expectedSize)
    {
        // Act
        var result = ThumbnailService.GetThumbnailSizePixels(sizeString);

        // Assert
        Assert.Equal(expectedSize, result);
    }

    [Fact]
    public void GetThumbnailSizePixels_ReturnsDefaultForUnknownSize()
    {
        // Act
        var result = ThumbnailService.GetThumbnailSizePixels("unknown");

        // Assert
        Assert.Equal(300, result); // Default is medium
    }
}
