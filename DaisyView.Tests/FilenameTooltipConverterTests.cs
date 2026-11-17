using System;
using System.Globalization;
using DaisyView.Converters;
using Xunit;

namespace DaisyView.Tests;

/// <summary>
/// Tests for the FilenameTooltipConverter
/// </summary>
public class FilenameTooltipConverterTests
{
    private readonly FilenameTooltipConverter _converter = new();

    [Fact]
    public void Convert_WhenIsActiveTrue_ReturnsFilename()
    {
        // Arrange
        string filename = "test.jpg";
        object[] values = { filename, true };

        // Act
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(filename, result);
    }

    [Fact]
    public void Convert_WhenIsActiveFalse_ReturnsNull()
    {
        // Arrange
        string filename = "test.jpg";
        object[] values = { filename, false };

        // Act
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Convert_WhenFilenameIsNull_ReturnsNull()
    {
        // Arrange
        object[] values = { null!, true };

        // Act
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Convert_WhenValueArrayTooShort_ReturnsNull()
    {
        // Arrange
        object[] values = { "test.jpg" };

        // Act
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Convert_WhenIsActiveNotBoolean_ReturnsNull()
    {
        // Arrange
        string filename = "test.jpg";
        object[] values = { filename, "not a boolean" };

        // Act
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        // Arrange & Act & Assert
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack("test", new[] { typeof(string), typeof(bool) }, null!, CultureInfo.InvariantCulture));
    }
}
