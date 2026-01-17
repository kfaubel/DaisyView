# DaisyView - Image Viewer

A feature-rich Windows image viewer application built with C# and WPF.

## Features

- **File System Navigation**: Tree view of local drives and folders
- **Image Thumbnails**: View image files in thumbnail gallery (jpg, png, gif, bmp, tif)
- **Favorites**: Mark frequently used folders as favorites for quick access
- **Slideshow Mode**: Full-screen viewing with keyboard/mouse navigation
- **Image Marking**: Mark/unmark images for batch operations
- **File Operations**: Move or delete marked images to other locations
- **Random Sort**: Shuffle image display order per folder
- **Themes**: Support for light mode, dark mode, or system default
- **Update Checking**: Automatic checks for new versions via GitHub Releases
- **Comprehensive Logging**: Detailed activity and file system operation logs
- **Persistent Settings**: User preferences saved in JSON format

## Project Structure

```
DaisyView/
├── Models/                    # Data models
│   ├── AppSettings.cs        # Application configuration
│   ├── TreeNode.cs           # File system tree nodes
│   ├── ImageFile.cs          # Image file representation
│   └── GitHubRelease.cs      # Release info for updates
├── Services/                  # Business logic services
│   ├── SettingsService.cs    # Settings persistence
│   ├── LoggingService.cs     # Application logging
│   ├── FileSystemService.cs  # File system operations
│   ├── ThumbnailService.cs   # Thumbnail generation
│   └── UpdateCheckService.cs # GitHub update checking
├── ViewModels/               # MVVM ViewModels
│   └── MainWindowViewModel.cs
├── Views/                     # XAML UI definitions
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
├── Utils/                     # Utility classes (future)
└── Settings/                  # Settings-related code (future)

DaisyView.Tests/              # Unit tests
├── SettingsServiceTests.cs
├── UpdateCheckServiceTests.cs
└── ThumbnailServiceTests.cs
```

## Settings

Application settings are stored in:
`%APPDATA%\Local\DaisyView\settings.json`

Example settings file:
```json
{
  "Theme": "System",
  "LastActiveFolderPath": "C:\\Pictures",
  "LoggingLevel": "Information",
  "FavoriteFolders": [
    "C:\\Pictures\\Vacation",
    "C:\\Pictures\\Family"
  ],
  "ThumbnailSize": "Medium",
  "RandomOrderCache": {},
  "RandomEnabledFolders": []
}
```

### Configuration Options

- **Theme**: "Light", "Dark", or "System"
- **LoggingLevel**: "Trace", "Information", "Warning", "Error"
- **ThumbnailSize**: "Small" (100px), "Medium" (200px), "Large" (400px)

## Logging

Application logs are written to:
`%APPDATA%\Local\DaisyView\logs\`

Logs include:
- All user actions (marking/unmarking, navigation, etc.)
- File system operations with duration tracking
- Warnings for operations taking > 5 seconds
- Trace-level logs for less important events

## Building and Running

### Prerequisites
- .NET 8.0 or later
- Windows 7 or later

### Build
```bash
dotnet build DaisyView.sln
```

### Run
```bash
dotnet run --project DaisyView/DaisyView.csproj
```

### Run Tests
```bash
dotnet test DaisyView.sln
```

## Keyboard Shortcuts (Slideshow Mode)

- **Right Arrow / Mouse Right / Wheel Down**: Next image
- **Left Arrow / Mouse Left / Wheel Up**: Previous image
- **Spacebar**: Mark/unmark image
- **ESC / F11**: Exit slideshow mode
- **F11**: Enter slideshow mode (from main window)

## Dependencies

- **Serilog**: Structured logging
- **System.Text.Json**: JSON serialization
- **System.Net.Http.Json**: HTTP client for update checking

## Future Enhancements

- [ ] Slideshow mode implementation
- [ ] Real-time thumbnail caching
- [ ] Image preview pane
- [ ] Batch rename functionality
- [ ] Support for more image formats
- [ ] Web-based remote access
- [ ] Plugin system for filters and effects

## License

[Add your license here]
