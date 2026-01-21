using Xunit;
using DaisyView.Services;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the UpdateCheckService
/// </summary>
public class UpdateCheckServiceTests
{
    #region Basic Version Comparison Tests

    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("1.0.1", "1.0.0", false)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("v1.0.0", "v1.0.1", true)]
    public void IsNewerVersion_ReturnsCorrectComparison(string current, string newer, bool expected)
    {
        // Act
        var result = UpdateCheckService.IsNewerVersion(current, newer);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Invalid Version Handling Tests

    [Theory]
    [InlineData("invalid", "1.0.0")]
    [InlineData("1.0.0", "invalid")]
    [InlineData("", "1.0.0")]
    [InlineData("1.0.0", "")]
    [InlineData("abc", "xyz")]
    public void IsNewerVersion_ReturnsFalse_ForInvalidVersions(string current, string newer)
    {
        // Act
        var result = UpdateCheckService.IsNewerVersion(current, newer);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNewerVersion_HandlesNullVersions()
    {
        // Act & Assert
        Assert.False(UpdateCheckService.IsNewerVersion(null!, "1.0.0"));
        Assert.False(UpdateCheckService.IsNewerVersion("1.0.0", null!));
    }

    #endregion

    #region Version Prefix Handling Tests

    [Theory]
    [InlineData("v1.0.0", "v2.0.0", true)]
    [InlineData("V1.0.0", "V1.0.1", true)]
    [InlineData("v1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "v1.0.1", true)]
    public void IsNewerVersion_HandlesVersionPrefix(string current, string newer, bool expected)
    {
        // Act
        var result = UpdateCheckService.IsNewerVersion(current, newer);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Multi-Part Version Tests

    [Theory]
    [InlineData("1.2.3", "1.2.4", true)]
    [InlineData("1.2.3", "1.3.0", true)]
    [InlineData("1.2.3", "2.0.0", true)]
    [InlineData("1.2.3", "1.2.3", false)]
    [InlineData("2.0.0", "1.9.9", false)]
    public void IsNewerVersion_ComparesAllVersionParts(string current, string newer, bool expected)
    {
        // Act
        var result = UpdateCheckService.IsNewerVersion(current, newer);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1.0.0.0", "1.0.0.1", true)]
    [InlineData("1.0.0.0", "1.0.0.0", false)]
    public void IsNewerVersion_HandlesFourPartVersions(string current, string newer, bool expected)
    {
        // Act
        var result = UpdateCheckService.IsNewerVersion(current, newer);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("0.0.1", "0.0.2", true)]
    [InlineData("0.1.0", "0.2.0", true)]
    [InlineData("10.0.0", "9.9.9", false)]
    [InlineData("1.10.0", "1.9.0", false)]
    public void IsNewerVersion_HandlesEdgeCases(string current, string newer, bool expected)
    {
        // Act
        var result = UpdateCheckService.IsNewerVersion(current, newer);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1.0", "1.1", true)]
    [InlineData("1.0", "2.0", true)]
    public void IsNewerVersion_HandlesTwoPartVersionStrings(string current, string newer, bool expected)
    {
        // Act
        var result = UpdateCheckService.IsNewerVersion(current, newer);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1", "2")]
    [InlineData("a", "b")]
    public void IsNewerVersion_ReturnsFalse_ForSinglePartVersions(string current, string newer)
    {
        // Single-part versions are not valid semantic versions
        // Act
        var result = UpdateCheckService.IsNewerVersion(current, newer);

        // Assert - should return false as they're not valid versions
        Assert.False(result);
    }

    #endregion
}
