using Xunit;
using DaisyView.Models;
using DaisyView.Constants;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the AppSettings model
/// </summary>
public class AppSettingsTests
{
    #region Default Value Tests

    [Fact]
    public void AppSettings_HasCorrectDefaultTheme()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal("Dark", settings.Theme);
    }

    [Fact]
    public void AppSettings_HasCorrectDefaultLoggingLevel()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal("Trace", settings.LoggingLevel);
    }

    [Fact]
    public void AppSettings_HasCorrectDefaultThumbnailSize()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal(AppConstants.ThumbnailSizes.Medium, settings.ThumbnailSize);
    }

    [Fact]
    public void AppSettings_HasCorrectDefaultWindowDimensions()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal(AppConstants.WindowDefaults.Width, settings.WindowWidth);
        Assert.Equal(AppConstants.WindowDefaults.Height, settings.WindowHeight);
        Assert.Equal(AppConstants.WindowDefaults.State, settings.WindowState);
    }

    [Fact]
    public void AppSettings_HasCorrectDefaultCacheSettings()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal(AppConstants.Timing.DefaultCacheMaxSizeBytes, settings.VideoCacheMaxSizeBytes);
        Assert.Equal(AppConstants.Timing.DefaultCacheMaxAgeHours, settings.VideoCacheMaxAgeHours);
    }

    [Fact]
    public void AppSettings_CollectionsAreInitialized()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.NotNull(settings.FavoriteFolders);
        Assert.NotNull(settings.RandomOrderCache);
        Assert.NotNull(settings.RandomEnabledFolders);
        Assert.Empty(settings.FavoriteFolders);
        Assert.Empty(settings.RandomOrderCache);
        Assert.Empty(settings.RandomEnabledFolders);
    }

    [Fact]
    public void AppSettings_LastActiveFolderPath_IsNullByDefault()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.Null(settings.LastActiveFolderPath);
    }

    #endregion

    #region Property Modification Tests

    [Fact]
    public void AppSettings_Theme_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.Theme = "Light";

        // Assert
        Assert.Equal("Light", settings.Theme);
    }

    [Fact]
    public void AppSettings_LoggingLevel_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.LoggingLevel = "Warning";

        // Assert
        Assert.Equal("Warning", settings.LoggingLevel);
    }

    [Fact]
    public void AppSettings_ThumbnailSize_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.ThumbnailSize = AppConstants.ThumbnailSizes.Large;

        // Assert
        Assert.Equal("Large", settings.ThumbnailSize);
    }

    [Fact]
    public void AppSettings_LastActiveFolderPath_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.LastActiveFolderPath = "C:\\MyPhotos";

        // Assert
        Assert.Equal("C:\\MyPhotos", settings.LastActiveFolderPath);
    }

    #endregion

    #region Window Placement Tests

    [Fact]
    public void AppSettings_WindowWidth_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.WindowWidth = 1920;

        // Assert
        Assert.Equal(1920, settings.WindowWidth);
    }

    [Fact]
    public void AppSettings_WindowHeight_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.WindowHeight = 1080;

        // Assert
        Assert.Equal(1080, settings.WindowHeight);
    }

    [Fact]
    public void AppSettings_WindowPosition_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.WindowLeft = 100;
        settings.WindowTop = 50;

        // Assert
        Assert.Equal(100, settings.WindowLeft);
        Assert.Equal(50, settings.WindowTop);
    }

    [Fact]
    public void AppSettings_WindowState_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.WindowState = "Maximized";

        // Assert
        Assert.Equal("Maximized", settings.WindowState);
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void AppSettings_FavoriteFolders_CanAddItems()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.FavoriteFolders.Add("C:\\Folder1");
        settings.FavoriteFolders.Add("C:\\Folder2");

        // Assert
        Assert.Equal(2, settings.FavoriteFolders.Count);
        Assert.Contains("C:\\Folder1", settings.FavoriteFolders);
        Assert.Contains("C:\\Folder2", settings.FavoriteFolders);
    }

    [Fact]
    public void AppSettings_RandomOrderCache_CanStoreOrders()
    {
        // Arrange
        var settings = new AppSettings();
        var order = new System.Collections.Generic.List<string> { "c.jpg", "a.jpg", "b.jpg" };

        // Act
        settings.RandomOrderCache["C:\\Photos"] = order;

        // Assert
        Assert.True(settings.RandomOrderCache.ContainsKey("C:\\Photos"));
        Assert.Equal(order, settings.RandomOrderCache["C:\\Photos"]);
    }

    [Fact]
    public void AppSettings_RandomEnabledFolders_CanAddItems()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.RandomEnabledFolders.Add("C:\\Photos");

        // Assert
        Assert.Single(settings.RandomEnabledFolders);
        Assert.Contains("C:\\Photos", settings.RandomEnabledFolders);
    }

    #endregion

    #region Cache Settings Tests

    [Fact]
    public void AppSettings_VideoCacheMaxSizeBytes_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.VideoCacheMaxSizeBytes = 10_737_418_240; // 10 GB

        // Assert
        Assert.Equal(10_737_418_240, settings.VideoCacheMaxSizeBytes);
    }

    [Fact]
    public void AppSettings_VideoCacheMaxAgeHours_CanBeModified()
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.VideoCacheMaxAgeHours = 168; // 1 week

        // Assert
        Assert.Equal(168, settings.VideoCacheMaxAgeHours);
    }

    #endregion
}
