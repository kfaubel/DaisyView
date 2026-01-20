# DaisyView - Image Viewer

A feature-rich Windows image viewer application built with C# and WPF.

## Features

- **File System Navigation**: Tree view of local drives and folders
- **Image Thumbnails**: View image files in thumbnail gallery (jpg, png, gif, bmp, tif, webp)
- **Video Support**: WebM video playback with automatic conversion to MP4 (with caching)
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
│   ├── VideoConversionService.cs # WebM to MP4 conversion
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
  "VideoCacheMaxSizeBytes": 21474836480,
  "VideoCacheMaxAgeHours": 720,
  "RandomOrderCache": {},
  "RandomEnabledFolders": []
}
```

### Configuration Options

- **Theme**: "Light", "Dark", or "System"
- **LoggingLevel**: "Trace", "Information", "Warning", "Error"
- **ThumbnailSize**: "Small" (100px), "Medium" (200px), "Large" (400px)
- **VideoCacheMaxSizeBytes**: Maximum cache size for converted WebM files (default: 20 GB)
- **VideoCacheMaxAgeHours**: Maximum age for cached files in hours (default: 30 days)

## Logging

Application logs are written to:
`%APPDATA%\Local\DaisyView\logs\`

Logs include:
- All user actions (marking/unmarking, navigation, etc.)
- File system operations with duration tracking
- Warnings for operations taking > 5 seconds
- Trace-level logs for less important events
- Video conversion operations and cache management

## Video Support (WebM)

DaisyView supports WebM video files in slideshow mode through automatic conversion:

- **Automatic Conversion**: WebM files are converted to MP4 on-demand using FFmpeg
- **Smart Caching**: Converted files are cached to avoid re-conversion
- **Cache Management**: Old files are automatically cleaned up based on:
  - Age limit (default: 30 days)
  - Size limit (default: 20 GB)
- **Cache Location**: `%APPDATA%\Local\DaisyView\VideoCache\`

### FFmpeg Setup Required

To use WebM video support, FFmpeg must be installed:

**Option 1 - Using Chocolatey** (recommended):
```powershell
choco install ffmpeg
```

**Option 2 - Using winget**:
```powershell
winget install ffmpeg
```

**Option 3 - Manual Installation**:
1. Download from https://ffmpeg.org/download.html
2. Extract and add to system PATH
3. Verify: `ffmpeg -version`

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

## Publishing Releases to GitHub

DaisyView uses GitHub Releases to distribute new versions. This guide covers both creating the first release and publishing subsequent updates.

### Prerequisites

Before releasing, ensure:
- You have push access to the repository
- All changes are committed and working directory is clean
- Code has been tested locally
- Documentation is up-to-date
- Version number is decided

### Initial Release (v1.0.0 or first version)

Follow these steps for the very first release:

#### 1. Prepare the Repository
```bash
# Ensure you're on main branch
git checkout main

# Pull latest changes
git pull origin main

# Verify clean working directory
git status  # Should show "nothing to commit, working tree clean"
```

#### 2. Update Version Number

Edit [DaisyView/DaisyView.csproj](DaisyView/DaisyView.csproj) and update the `<Version>` tag:

```xml
<PropertyGroup>
  ...
  <Version>1.0.0</Version>  <!-- Update this -->
  ...
</PropertyGroup>
```

#### 3. Commit Version Change
```bash
git add DaisyView/DaisyView.csproj
git commit -m "chore: bump version to 1.0.0"
git push origin main
```

#### 4. Create and Push Git Tag
```bash
# Create annotated tag (recommended)
git tag -a v1.0.0 -m "Release version 1.0.0

- Initial release of DaisyView
- Image viewer with slideshow support
- WebM video playback support
- File system navigation with thumbnails"

# Push tag to GitHub
git push origin v1.0.0
```

#### 5. Create Release on GitHub

Navigate to: https://github.com/kfaubel/DaisyView/releases

Click **"Create a new release"** and fill in:

**Tag**: `v1.0.0` (should auto-complete)

**Release title**: `DaisyView 1.0.0 - Initial Release`

**Description**:
```markdown
# DaisyView 1.0.0 - Initial Release

## Features
- File system navigation with tree view
- Image thumbnail gallery (jpg, png, gif, bmp, tif, webp)
- WebM video playback with automatic MP4 conversion
- Full-screen slideshow mode
- Image marking and batch operations
- Light/Dark/System theme support
- Update checking via GitHub Releases
- Comprehensive logging

## Requirements
- Windows 7 or later
- .NET 8.0 Runtime
- FFmpeg (for WebM video support)

## Installation
1. Download DaisyView-1.0.0.exe from below
2. Run installer
3. (Optional) Install FFmpeg for WebM support

## Bug Reports
Found an issue? Please create an [issue](https://github.com/kfaubel/DaisyView/issues)

### Release Artifacts
- `DaisyView-1.0.0.exe` - Windows installer
```

**Attach Release Files** (if available):
- Build the Release version: `dotnet publish -c Release`
- Attach the published executable or installer

**Publish release**

### Subsequent Releases (v1.0.1, v1.1.0, etc.)

Use this streamlined process for future releases:

#### 1. Verify Changes and Update Version
```bash
# Update version in DaisyView.csproj
# Example: 1.0.0 → 1.0.1 (patch), 1.1.0 (minor), 2.0.0 (major)
```

#### 2. Commit and Tag
```bash
# Commit version change
git add DaisyView/DaisyView.csproj
git commit -m "chore: bump version to 1.0.1"
git push origin main

# Create and push tag
git tag -a v1.0.1 -m "Release 1.0.1

Bug fixes:
- Fixed cache cleanup issue
- Improved FFmpeg error handling"

git push origin v1.0.1
```

#### 3. Create Release on GitHub
- Go to: https://github.com/kfaubel/DaisyView/releases/new
- Tag: `v1.0.1`
- Title: `DaisyView 1.0.1`
- Description: Include summary of changes, bug fixes, and new features

#### 4. Publish

### Quick Release Checklist

Use this checklist for each release:

- [ ] All changes committed to `main` branch
- [ ] Version number updated in `DaisyView.csproj`
- [ ] Version change committed and pushed
- [ ] Git tag created (`git tag -a vX.Y.Z`)
- [ ] Git tag pushed (`git push origin vX.Y.Z`)
- [ ] GitHub Release page created
- [ ] Release notes written with clear change summary
- [ ] Release published on GitHub
- [ ] Binary/executable attached (optional but recommended)

### Version Numbering (Semantic Versioning)

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR** (X.0.0): Breaking changes, major features
- **MINOR** (1.X.0): New features, backward compatible
- **PATCH** (1.0.X): Bug fixes, no new features

Examples:
- `v1.0.0` → `v1.0.1`: Bug fix
- `v1.0.1` → `v1.1.0`: New feature (WebM support)
- `v1.1.0` → `v2.0.0`: Breaking API change

### Accessing Releases as User

Users can download releases from:
https://github.com/kfaubel/DaisyView/releases

Each release includes:
- Release notes
- Download links for executables
- Version number and tag
- Release date

### Troubleshooting

**Tag already exists error:**
```bash
# Delete local tag
git tag -d v1.0.0

# Delete remote tag
git push origin --delete v1.0.0

# Recreate tag
git tag -a v1.0.0 -m "Release message"
git push origin v1.0.0
```

**Need to modify release after publishing:**
1. Edit the release page on GitHub (click pencil icon)
2. Update notes and files as needed
3. Save changes (no new tag needed)

**Automated releases (future consideration):**
Consider setting up GitHub Actions to:
- Automatically build on tag push
- Create release artifacts
- Publish to release page
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
- **FFmpeg**: Video conversion (external dependency, not NuGet package)

## License

[Add your license here]
