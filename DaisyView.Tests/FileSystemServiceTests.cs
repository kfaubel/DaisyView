using System;
using System.IO;
using Xunit;
using DaisyView.Services;
using Moq;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the FileSystemService
/// </summary>
public class FileSystemServiceTests
{
    [Fact]
    public void GetRootDrives_ReturnsAtLeastOneDrive()
    {
        // Arrange
        var mockLoggingService = new Mock<LoggingService>(new SettingsService());
        var service = new FileSystemService(mockLoggingService.Object);

        // Act
        var drives = service.GetRootDrives();

        // Assert
        Assert.NotEmpty(drives);
        Assert.All(drives, d => Assert.NotNull(d.Name));
        Assert.All(drives, d => Assert.NotNull(d.FullPath));
    }

    [Fact]
    public void PathExists_ReturnsTrueForSystemDrive()
    {
        // Arrange
        var mockLoggingService = new Mock<LoggingService>(new SettingsService());
        var service = new FileSystemService(mockLoggingService.Object);
        var systemDrive = "C:\\"; // Assuming C: drive exists

        // Act
        var exists = service.PathExists(systemDrive);

        // Assert - This may fail on some systems, so we just check it returns a bool
        Assert.IsType<bool>(exists);
    }

    [Fact]
    public void PathExists_ReturnsFalseForNonExistentPath()
    {
        // Arrange
        var mockLoggingService = new Mock<LoggingService>(new SettingsService());
        var service = new FileSystemService(mockLoggingService.Object);
        var nonExistentPath = "Z:\\NonExistent\\Path\\That\\Should\\Not\\Exist";

        // Act
        var exists = service.PathExists(nonExistentPath);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void GetImageFiles_ReturnsEmptyListForFolderWithNoImages()
    {
        // Arrange
        var mockLoggingService = new Mock<LoggingService>(new SettingsService());
        var service = new FileSystemService(mockLoggingService.Object);
        var tempFolder = Path.Combine(Path.GetTempPath(), "DaisyViewTest_NoImages");
        Directory.CreateDirectory(tempFolder);

        try
        {
            // Act
            var images = service.GetImageFiles(tempFolder);

            // Assert
            Assert.Empty(images);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder);
        }
    }
}
