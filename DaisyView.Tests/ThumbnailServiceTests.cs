using Xunit;
using DaisyView.Services;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the ThumbnailService
/// </summary>
public class ThumbnailServiceTests
{
    [Theory]
    [InlineData("small", 100)]
    [InlineData("Small", 100)]
    [InlineData("SMALL", 100)]
    [InlineData("medium", 200)]
    [InlineData("Medium", 200)]
    [InlineData("large", 400)]
    [InlineData("Large", 400)]
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
        Assert.Equal(200, result); // Default is medium
    }
}
