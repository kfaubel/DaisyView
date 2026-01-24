# DaisyView - Image Viewer

A feature-rich Windows image viewer built with C# and WPF.

## Features

- **File System Navigation**: Tree view of local drives and folders
- **Image Gallery**: Thumbnail view with multiple sizes (jpg, png, gif, bmp, tif, webp)
- **Video Support**: MP4, AVI, MPEG, and WebM playback (WebM auto-converts to MP4)
- **Slideshow Mode**: Full-screen viewing with keyboard/mouse navigation
- **Favorites**: Quick access to frequently used folders
- **Image Marking**: Mark images for batch move/delete operations
- **Random Sort**: Shuffle image display order per folder
- **Themes**: Light, Dark, or System theme
- **Auto-Updates**: Checks for new versions via GitHub Releases

## Installation

### From Release
1. Download `DaisyView-win-x64.zip` from [Releases](https://github.com/kfaubel/DaisyView/releases)
2. Extract to a folder
3. Run `DaisyView.exe`

### Requirements
- Windows 10/11 (64-bit)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- FFmpeg (optional, for WebM video support)

## Building from Source

### Prerequisites
- .NET 8.0 SDK
- Windows 10/11

### Build & Run
```powershell
# Build
dotnet build DaisyView.sln

# Build specific configuration
dotnet build --configuration Release

# Run
dotnet run --project DaisyView/DaisyView.csproj

# Run tests
dotnet test DaisyView.sln
```

### Publish Release Build
```powershell
dotnet publish DaisyView -c Release -r win-x64 --no-self-contained --output ./publish
```

## Creating Releases

The `release.ps1` script automates version bumping, committing, tagging, and pushing to trigger GitHub Actions:

```powershell
# Bump patch version (1.0.4 → 1.0.5)
.\release.ps1 patch

# Bump minor version (1.0.4 → 1.1.0)
.\release.ps1 minor

# Bump major version (1.0.4 → 2.0.0)
.\release.ps1 major

# Preview without making changes
.\release.ps1 patch -DryRun
```

The script will:
1. Update version in `DaisyView.csproj`
2. Commit and push the change
3. Create and push a `vX.Y.Z` tag
4. GitHub Actions builds and publishes the release automatically

## Keyboard Shortcuts

### Main Window
| Key | Action |
|-----|--------|
| F11 | Enter slideshow mode |

### Slideshow Mode
| Key | Action |
|-----|--------|
| → / Mouse Right / Scroll Down | Next image |
| ← / Mouse Left / Scroll Up | Previous image |
| Space | Mark/unmark image |
| M | Toggle audio mute (videos) |
| Esc / F11 | Exit slideshow |

## Configuration

Settings are stored in `%LOCALAPPDATA%\DaisyView\settings.json`:

| Setting | Values | Default |
|---------|--------|---------|
| Theme | Light, Dark, System | Dark |
| ThumbnailSize | Small (100px), Medium (200px), Large (400px) | Medium |
| LoggingLevel | Trace, Information, Warning, Error | Trace |
| VideoCacheMaxSizeBytes | bytes | 20 GB |
| VideoCacheMaxAgeHours | hours | 720 (30 days) |

## Video Support

### Supported Formats
- **Native**: MP4, GIF
- **Converted**: WebM, AVI, MPEG/MPG (auto-converted to MP4)

### FFmpeg Setup (for WebM/AVI/MPEG)
```powershell
# Using Chocolatey
choco install ffmpeg

# Using winget
winget install ffmpeg

# Verify installation
ffmpeg -version
```

Converted videos are cached in `%LOCALAPPDATA%\DaisyView\VideoCache\`

## Project Structure

```
DaisyView/
├── Constants/         # Application constants
├── Converters/        # XAML value converters
├── Helpers/           # Utility helpers (MediaTypeHelper)
├── Models/            # Data models (AppSettings, TreeNode, ImageFile)
├── Services/          # Business logic (Settings, Thumbnail, VideoConversion)
├── ViewModels/        # MVVM ViewModels
└── Views/             # XAML UI (MainWindow, SlideshowWindow, HelpWindow)

DaisyView.Tests/       # Unit tests (206 tests)
```

## Logging

Logs are written to `%LOCALAPPDATA%\DaisyView\Logs\` and include:
- User actions and navigation
- File operations with timing
- Video conversion status
- Errors and warnings

## License

MIT License
