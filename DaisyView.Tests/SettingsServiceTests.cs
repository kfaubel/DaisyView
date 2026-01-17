using Xunit;
using DaisyView.Services;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the SettingsService
/// </summary>
public class SettingsServiceTests
{
    [Fact]
    public void GetSettings_ReturnsDefaultSettings_WhenNoFileExists()
    {
        // Arrange
        var service = new SettingsService();

        // Act
        var settings = service.GetSettings();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("System", settings.Theme);
        Assert.Equal("Information", settings.LoggingLevel);
        Assert.Empty(settings.FavoriteFolders);
    }

    [Fact]
    public void AddFavoriteFolder_AddsToFavorites()
    {
        // Arrange
        var service = new SettingsService();
        var folderPath = "C:\\TestFolder";

        // Act
        service.AddFavoritFolder(folderPath);
        var favorites = service.GetFavoriteFolders();

        // Assert
        Assert.Contains(folderPath, favorites);
    }

    [Fact]
    public void RemoveFavoriteFolder_RemovesFromFavorites()
    {
        // Arrange
        var service = new SettingsService();
        var folderPath = "C:\\TestFolder";
        service.AddFavoritFolder(folderPath);

        // Act
        service.RemoveFavoriteFolder(folderPath);
        var favorites = service.GetFavoriteFolders();

        // Assert
        Assert.DoesNotContain(folderPath, favorites);
    }

    [Fact]
    public void IsFavorite_ReturnsTrueForFavoritedFolder()
    {
        // Arrange
        var service = new SettingsService();
        var folderPath = "C:\\TestFolder";
        service.AddFavoritFolder(folderPath);

        // Act
        var isFavorite = service.IsFavorite(folderPath);

        // Assert
        Assert.True(isFavorite);
    }

    [Fact]
    public void SetLastActiveFolderPath_SavesPath()
    {
        // Arrange
        var service = new SettingsService();
        var folderPath = "C:\\ActiveFolder";

        // Act
        service.SetLastActiveFolderPath(folderPath);
        var retrieved = service.GetLastActiveFolderPath();

        // Assert
        Assert.Equal(folderPath, retrieved);
    }
}
