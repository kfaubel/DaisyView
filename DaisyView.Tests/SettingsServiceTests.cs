using Xunit;
using DaisyView.Services;
using System;
using System.IO;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the SettingsService
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _testSettingsFolder;

    public SettingsServiceTests()
    {
        // Create a unique temporary folder for each test run
        _testSettingsFolder = Path.Combine(Path.GetTempPath(), "DaisyViewTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testSettingsFolder);
    }

    public void Dispose()
    {
        // Clean up test folder after tests
        if (Directory.Exists(_testSettingsFolder))
        {
            try
            {
                Directory.Delete(_testSettingsFolder, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void GetSettings_ReturnsDefaultSettings_WhenNoFileExists()
    {
        // Arrange - use isolated test folder
        var service = new SettingsService(_testSettingsFolder);

        // Act
        var settings = service.GetSettings();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("Dark", settings.Theme);
        Assert.Equal("Trace", settings.LoggingLevel);
        Assert.Empty(settings.FavoriteFolders);
    }

    [Fact]
    public void AddFavoriteFolder_AddsToFavorites()
    {
        // Arrange - use isolated test folder
        var service = new SettingsService(_testSettingsFolder);
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
        // Arrange - use isolated test folder
        var service = new SettingsService(_testSettingsFolder);
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
        // Arrange - use isolated test folder
        var service = new SettingsService(_testSettingsFolder);
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
        // Arrange - use isolated test folder
        var service = new SettingsService(_testSettingsFolder);
        var folderPath = "C:\\ActiveFolder";

        // Act
        service.SetLastActiveFolderPath(folderPath);
        var retrieved = service.GetLastActiveFolderPath();

        // Assert
        Assert.Equal(folderPath, retrieved);
    }

    #region IsFavorite Edge Cases

    [Fact]
    public void IsFavorite_ReturnsFalse_ForNonFavoritedFolder()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act
        var isFavorite = service.IsFavorite("C:\\SomeRandomFolder");

        // Assert
        Assert.False(isFavorite);
    }

    [Fact]
    public void AddFavoriteFolder_DoesNotAddDuplicates()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);
        var folderPath = "C:\\TestFolder";

        // Act
        service.AddFavoritFolder(folderPath);
        service.AddFavoritFolder(folderPath);
        var favorites = service.GetFavoriteFolders();

        // Assert - should only contain one instance
        Assert.Single(favorites.FindAll(f => f == folderPath));
    }

    [Fact]
    public void RemoveFavoriteFolder_HandlesNonExistentFolder()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act - should not throw
        service.RemoveFavoriteFolder("C:\\NonExistentFolder");

        // Assert
        Assert.Empty(service.GetFavoriteFolders());
    }

    #endregion

    #region SaveSettings Tests

    [Fact]
    public void SaveSettings_PersistsToFile()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);
        service.AddFavoritFolder("C:\\TestFolder");
        service.SetLastActiveFolderPath("C:\\LastFolder");

        // Act
        service.SaveSettings();

        // Assert - file should exist
        var settingsPath = Path.Combine(_testSettingsFolder, "settings.json");
        Assert.True(File.Exists(settingsPath));
    }

    [Fact]
    public void SaveSettings_CanBeReloaded()
    {
        // Arrange
        var service1 = new SettingsService(_testSettingsFolder);
        service1.AddFavoritFolder("C:\\PersistentFolder");
        service1.SetLastActiveFolderPath("C:\\LastPath");
        service1.SaveSettings();

        // Act - create new service instance that loads from same folder
        var service2 = new SettingsService(_testSettingsFolder);

        // Assert - settings should be persisted
        Assert.Contains("C:\\PersistentFolder", service2.GetFavoriteFolders());
        Assert.Equal("C:\\LastPath", service2.GetLastActiveFolderPath());
    }

    #endregion

    #region UpdateSetting Tests

    [Fact]
    public void UpdateSetting_ModifiesSettings()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act
        service.UpdateSetting(s => s.Theme = "Light");

        // Assert
        Assert.Equal("Light", service.GetSettings().Theme);
    }

    [Fact]
    public void UpdateSetting_AutoSaves()
    {
        // Arrange
        var service1 = new SettingsService(_testSettingsFolder);
        
        // Act
        service1.UpdateSetting(s => s.Theme = "Light");
        
        // Create new instance to verify persistence
        var service2 = new SettingsService(_testSettingsFolder);

        // Assert
        Assert.Equal("Light", service2.GetSettings().Theme);
    }

    #endregion

    #region RandomOrder Tests

    [Fact]
    public void SaveRandomOrder_StoresOrder()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);
        var folderPath = "C:\\Photos";
        var order = new System.Collections.Generic.List<string> { "c.jpg", "a.jpg", "b.jpg" };

        // Act
        service.SaveRandomOrder(folderPath, order);
        var retrieved = service.GetRandomOrder(folderPath);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(order, retrieved);
    }

    [Fact]
    public void GetRandomOrder_ReturnsNull_ForUnknownFolder()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act
        var order = service.GetRandomOrder("C:\\UnknownFolder");

        // Assert
        Assert.Null(order);
    }

    [Fact]
    public void ClearRandomOrder_RemovesOrder()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);
        var folderPath = "C:\\Photos";
        service.SaveRandomOrder(folderPath, new System.Collections.Generic.List<string> { "a.jpg" });

        // Act
        service.ClearRandomOrder(folderPath);
        var order = service.GetRandomOrder(folderPath);

        // Assert
        Assert.Null(order);
    }

    #endregion

    #region Window Placement Tests

    [Fact]
    public void SaveWindowPlacement_StoresAllValues()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act
        service.SaveWindowPlacement(1920, 1080, 100, 50, "Maximized");

        // Assert
        Assert.Equal(1920, service.GetWindowWidth());
        Assert.Equal(1080, service.GetWindowHeight());
        Assert.Equal(100, service.GetWindowLeft());
        Assert.Equal(50, service.GetWindowTop());
        Assert.Equal("Maximized", service.GetSettings().WindowState);
    }

    [Fact]
    public void GetWindowWidth_ReturnsDefaultForNewSettings()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act
        var width = service.GetWindowWidth();

        // Assert - default is 1200
        Assert.Equal(1200, width);
    }

    [Fact]
    public void GetWindowHeight_ReturnsDefaultForNewSettings()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act
        var height = service.GetWindowHeight();

        // Assert - default is 800
        Assert.Equal(800, height);
    }

    [Fact]
    public void WindowPlacement_PersistsAcrossInstances()
    {
        // Arrange
        var service1 = new SettingsService(_testSettingsFolder);
        service1.SaveWindowPlacement(1600, 900, 200, 100, "Normal");

        // Act - create new instance
        var service2 = new SettingsService(_testSettingsFolder);

        // Assert
        Assert.Equal(1600, service2.GetWindowWidth());
        Assert.Equal(900, service2.GetWindowHeight());
        Assert.Equal(200, service2.GetWindowLeft());
        Assert.Equal(100, service2.GetWindowTop());
    }

    #endregion

    #region GetSettingsFilePath Tests

    [Fact]
    public void GetSettingsFilePath_ReturnsCorrectPath()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act
        var path = service.GetSettingsFilePath();

        // Assert
        Assert.Equal(Path.Combine(_testSettingsFolder, "settings.json"), path);
    }

    #endregion

    #region LastActiveFolderPath Edge Cases

    [Fact]
    public void SetLastActiveFolderPath_AcceptsNull()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);
        service.SetLastActiveFolderPath("C:\\SomePath");

        // Act
        service.SetLastActiveFolderPath(null);

        // Assert
        Assert.Null(service.GetLastActiveFolderPath());
    }

    [Fact]
    public void GetLastActiveFolderPath_ReturnsNull_WhenNotSet()
    {
        // Arrange
        var service = new SettingsService(_testSettingsFolder);

        // Act
        var path = service.GetLastActiveFolderPath();

        // Assert
        Assert.Null(path);
    }

    #endregion
}
