using Xunit;
using DaisyView.Constants;
using System.Linq;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the AppConstants class
/// </summary>
public class AppConstantsTests
{
    #region ThumbnailSizes Tests

    [Fact]
    public void ThumbnailSizes_HasCorrectStringValues()
    {
        Assert.Equal("Small", AppConstants.ThumbnailSizes.Small);
        Assert.Equal("Medium", AppConstants.ThumbnailSizes.Medium);
        Assert.Equal("Large", AppConstants.ThumbnailSizes.Large);
    }

    [Fact]
    public void ThumbnailSizes_HasCorrectPixelValues()
    {
        Assert.Equal(100, AppConstants.ThumbnailSizes.SmallPixels);
        Assert.Equal(200, AppConstants.ThumbnailSizes.MediumPixels);
        Assert.Equal(400, AppConstants.ThumbnailSizes.LargePixels);
    }

    [Theory]
    [InlineData("small", 100)]
    [InlineData("Small", 100)]
    [InlineData("SMALL", 100)]
    [InlineData("medium", 200)]
    [InlineData("Medium", 200)]
    [InlineData("MEDIUM", 200)]
    [InlineData("large", 400)]
    [InlineData("Large", 400)]
    [InlineData("LARGE", 400)]
    public void GetPixelSize_ReturnsCorrectValue_CaseInsensitive(string sizeName, int expected)
    {
        Assert.Equal(expected, AppConstants.ThumbnailSizes.GetPixelSize(sizeName));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("extra-large")]
    public void GetPixelSize_ReturnsDefaultForInvalidSize(string sizeName)
    {
        // Default is MediumPixels (200)
        Assert.Equal(200, AppConstants.ThumbnailSizes.GetPixelSize(sizeName));
    }

    [Fact]
    public void ThumbnailSizes_DefaultVisibleCount_IsPositive()
    {
        Assert.True(AppConstants.ThumbnailSizes.DefaultVisibleCount > 0);
        Assert.Equal(10, AppConstants.ThumbnailSizes.DefaultVisibleCount);
    }

    #endregion

    #region Timing Tests

    [Fact]
    public void Timing_VideoLoopCheckInterval_IsReasonable()
    {
        Assert.True(AppConstants.Timing.VideoLoopCheckInterval.TotalMilliseconds > 0);
        Assert.True(AppConstants.Timing.VideoLoopCheckInterval.TotalMilliseconds <= 1000);
    }

    [Fact]
    public void Timing_CursorHideDelay_IsReasonable()
    {
        Assert.True(AppConstants.Timing.CursorHideDelay.TotalSeconds >= 1);
        Assert.True(AppConstants.Timing.CursorHideDelay.TotalSeconds <= 10);
    }

    [Fact]
    public void Timing_DriveCheckTimeout_IsReasonable()
    {
        Assert.True(AppConstants.Timing.DriveCheckTimeout.TotalSeconds >= 1);
    }

    [Fact]
    public void Timing_CacheDefaults_ArePositive()
    {
        Assert.True(AppConstants.Timing.DefaultCacheMaxAgeHours > 0);
        Assert.True(AppConstants.Timing.DefaultCacheMaxSizeBytes > 0);
        // 20GB default
        Assert.Equal(21_474_836_480, AppConstants.Timing.DefaultCacheMaxSizeBytes);
        // 30 days = 720 hours
        Assert.Equal(720, AppConstants.Timing.DefaultCacheMaxAgeHours);
    }

    #endregion

    #region Paths Tests

    [Fact]
    public void Paths_HasExpectedValues()
    {
        Assert.Equal("DaisyView", AppConstants.Paths.AppDataFolderName);
        Assert.Equal("VideoCache", AppConstants.Paths.VideoCacheFolderName);
        Assert.Equal("settings.json", AppConstants.Paths.SettingsFileName);
        Assert.Equal("Logs", AppConstants.Paths.LogsFolderName);
    }

    [Fact]
    public void Paths_DoNotContainInvalidCharacters()
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        
        Assert.DoesNotContain(AppConstants.Paths.AppDataFolderName, c => invalidChars.Contains(c));
        Assert.DoesNotContain(AppConstants.Paths.VideoCacheFolderName, c => invalidChars.Contains(c));
        Assert.DoesNotContain(AppConstants.Paths.SettingsFileName, c => invalidChars.Contains(c));
        Assert.DoesNotContain(AppConstants.Paths.LogsFolderName, c => invalidChars.Contains(c));
    }

    #endregion

    #region Colors Tests

    [Fact]
    public void Colors_AreValidHexFormat()
    {
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", AppConstants.Colors.ActiveGold);
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", AppConstants.Colors.InactiveGrey);
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", AppConstants.Colors.ErrorYellow);
    }

    [Fact]
    public void Colors_HaveExpectedValues()
    {
        Assert.Equal("#FFD700", AppConstants.Colors.ActiveGold);
        Assert.Equal("#808080", AppConstants.Colors.InactiveGrey);
        Assert.Equal("#FFFF00", AppConstants.Colors.ErrorYellow);
    }

    #endregion

    #region WindowDefaults Tests

    [Fact]
    public void WindowDefaults_HasReasonableDimensions()
    {
        Assert.True(AppConstants.WindowDefaults.Width >= 800);
        Assert.True(AppConstants.WindowDefaults.Height >= 600);
    }

    [Fact]
    public void WindowDefaults_StateIsValid()
    {
        var validStates = new[] { "Normal", "Maximized", "Minimized" };
        Assert.Contains(AppConstants.WindowDefaults.State, validStates);
    }

    #endregion
}
