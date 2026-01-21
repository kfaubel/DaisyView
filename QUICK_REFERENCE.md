# DaisyView - Developer Quick Reference

## Project Structure

```
DaisyView/
├── Constants/
│   └── AppConstants.cs          # Centralized magic numbers/strings
├── Converters/
│   └── ValueConverters.cs       # XAML value converters
├── Helpers/
│   └── MediaTypeHelper.cs       # Media file type detection
├── Models/
│   ├── AppSettings.cs           # Application configuration
│   ├── TreeNode.cs              # File system tree node
│   ├── ImageFile.cs             # Image/video file representation
│   └── GitHubRelease.cs         # GitHub release info for updates
├── Services/
│   ├── SettingsService.cs       # JSON settings persistence
│   ├── LoggingService.cs        # Serilog-based logging
│   ├── FileSystemService.cs     # File system operations & watching
│   ├── ThumbnailService.cs      # Thumbnail generation
│   ├── VideoConversionService.cs # Video conversion with caching
│   └── UpdateCheckService.cs    # GitHub update checking
├── ViewModels/
│   ├── RelayCommand.cs          # ICommand implementation
│   └── MainWindowViewModel.cs   # Main application logic
└── Views/
    ├── MainWindow.xaml/.cs      # Main UI
    ├── SlideshowWindow.xaml/.cs # Fullscreen slideshow
    └── HelpWindow.xaml/.cs      # Help dialog

DaisyView.Tests/
├── AppConstantsTests.cs
├── AppSettingsTests.cs
├── FileSystemServiceTests.cs
├── ImageFileTests.cs
├── MediaTypeHelperTests.cs
├── RandomShuffleTests.cs
├── RelayCommandTests.cs
├── SettingsServiceTests.cs
├── ThumbnailServiceTests.cs
├── TreeNodeTests.cs
└── UpdateCheckServiceTests.cs
```

## Common Development Tasks

### Adding a New Feature
1. Create model (if needed) in `Models/`
2. Create/update service in `Services/`
3. Add command/property to `MainWindowViewModel.cs`
4. Update XAML binding in `Views/`
5. Add unit tests in `DaisyView.Tests/`

### Adding a New Command
```csharp
// In MainWindowViewModel.cs
public ICommand MyCommand => _myCommand ??= new RelayCommand(MyMethod);
private ICommand? _myCommand;

private void MyMethod(object? parameter) { /* implementation */ }
```
```xml
<!-- In XAML -->
<Button Command="{Binding MyCommand}" />
```

### Using MediaTypeHelper
```csharp
using DaisyView.Helpers;

MediaTypeHelper.IsSupportedMedia(filePath);  // Any supported file
MediaTypeHelper.IsStaticImage(filePath);     // jpg, png, bmp, etc.
MediaTypeHelper.IsVideoFile(filePath);       // mp4, gif, webm, etc.
MediaTypeHelper.NeedsConversion(filePath);   // webm, avi, mpeg
MediaTypeHelper.IsGif(filePath);
MediaTypeHelper.IsMp4(filePath);
```

### Using AppConstants
```csharp
using DaisyView.Constants;

// Thumbnail sizes
AppConstants.ThumbnailSizes.Small;           // "Small"
AppConstants.ThumbnailSizes.SmallPixels;     // 100
AppConstants.ThumbnailSizes.GetPixelSize("Medium"); // 200

// Timing
AppConstants.Timing.CursorHideDelay;         // TimeSpan (2 seconds)
AppConstants.Timing.VideoLoopCheckInterval;  // TimeSpan (100ms)
AppConstants.Timing.DefaultCacheMaxSizeBytes; // 20 GB

// Paths
AppConstants.Paths.AppDataFolderName;        // "DaisyView"
AppConstants.Paths.VideoCacheFolderName;     // "VideoCache"

// Colors
AppConstants.Colors.ActiveGold;              // "#FFD700"
```

### Logging
```csharp
_loggingService.LogUserAction("Action", "Details");
_loggingService.LogInfo("Information message");
_loggingService.LogWarning("Warning message");
_loggingService.LogError("Error occurred", exception);
_loggingService.LogTrace("Detailed trace info");
```

### Settings Service
```csharp
// Read settings
var settings = _settingsService.GetSettings();

// Update a setting (auto-saves)
_settingsService.UpdateSetting(s => s.Theme = "Dark");

// Favorites
_settingsService.AddFavoritFolder(path);
_settingsService.RemoveFavoriteFolder(path);
_settingsService.IsFavorite(path);

// Window placement
_settingsService.SaveWindowPlacement(width, height, left, top, state);
_settingsService.GetWindowWidth();

// Random order
_settingsService.SaveRandomOrder(folderPath, fileList);
_settingsService.GetRandomOrder(folderPath);
_settingsService.ClearRandomOrder(folderPath);
```

### File System Service
```csharp
// Navigation
var drives = _fileSystemService.GetRootDrives();
var subfolders = _fileSystemService.GetSubfolders(folderPath);
var images = _fileSystemService.GetImageFiles(folderPath);

// File watching
_fileSystemService.WatchFolder(folderPath);
_fileSystemService.UnwatchFolder(folderPath);

// File operations
await _fileSystemService.MoveFilesAsync(sourceList, destinationPath);
await _fileSystemService.DeleteFilesAsync(fileList);
```

### Video Conversion
```csharp
// Convert to MP4 (cached)
var mp4Path = await _videoConversionService.ConvertWebmToMp4Async(videoPath);

// Cache management
_videoConversionService.ClearCache();
```

## IDisposable Pattern

Classes implementing IDisposable follow this pattern:
```csharp
public class MyService : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            // Dispose managed resources
        }
        _disposed = true;
    }
}
```

## XAML Binding Examples

```xml
<!-- Simple binding -->
<TextBlock Text="{Binding PropertyName}" />

<!-- Two-way binding -->
<ToggleButton IsChecked="{Binding RandomEnabled, Mode=TwoWay}" />

<!-- Command with parameter -->
<Button Command="{Binding MyCommand}" CommandParameter="{Binding SelectedItem}" />

<!-- Collection with template -->
<ItemsControl ItemsSource="{Binding Images}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Image Source="{Binding ThumbnailData, Converter={StaticResource ByteArrayConverter}}" />
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

## Quick Commands

```powershell
# Build
dotnet build DaisyView.sln
dotnet build DaisyView.sln --configuration Release

# Test
dotnet test DaisyView.sln

# Run
dotnet run --project DaisyView/DaisyView.csproj

# Publish
dotnet publish DaisyView -c Release -r win-x64 --no-self-contained --output ./publish

# Release (automated)
.\release.ps1 patch   # 1.0.4 → 1.0.5
.\release.ps1 minor   # 1.0.4 → 1.1.0
.\release.ps1 major   # 1.0.4 → 2.0.0
```

## File Locations

| Item | Path |
|------|------|
| Settings | `%LOCALAPPDATA%\DaisyView\settings.json` |
| Logs | `%LOCALAPPDATA%\DaisyView\Logs\` |
| Video Cache | `%LOCALAPPDATA%\DaisyView\VideoCache\` |
| Version | `DaisyView/DaisyView.csproj` (`<Version>` tag) |

## Debugging Tips

- **Enable trace logging**: Set `LoggingLevel` to `"Trace"` in settings.json
- **View logs**: Check `%LOCALAPPDATA%\DaisyView\Logs\`
- **Command not firing**: Verify binding path and CanExecute predicate
- **Binding issues**: Check Output window for binding errors

## Links

- [README.md](README.md) - Project overview and user documentation
- [Releases](https://github.com/kfaubel/DaisyView/releases)
- [Issues](https://github.com/kfaubel/DaisyView/issues)
