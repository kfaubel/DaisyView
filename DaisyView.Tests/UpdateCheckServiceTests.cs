using Xunit;
using DaisyView.Services;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the UpdateCheckService
/// </summary>
public class UpdateCheckServiceTests
{
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

    [Fact]
    public void IsNewerVersion_HandlesInvalidVersions()
    {
        // Act & Assert
        Assert.False(UpdateCheckService.IsNewerVersion("invalid", "1.0.0"));
        Assert.False(UpdateCheckService.IsNewerVersion("1.0.0", "invalid"));
    }
}
