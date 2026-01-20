# GitHub Releases Guide for DaisyView

This document provides complete instructions for managing GitHub Releases for DaisyView.

## Table of Contents
1. [Initial Setup](#initial-setup)
2. [Creating Your First Release](#creating-your-first-release)
3. [Publishing Subsequent Releases](#publishing-subsequent-releases)
4. [Release Checklist](#release-checklist)
5. [Best Practices](#best-practices)
6. [Troubleshooting](#troubleshooting)

---

## Initial Setup

### Prerequisites
- Git installed and configured
- GitHub account with push access to DaisyView repository
- Local clone of the repository

### Verify Git Configuration
```bash
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

---

## Creating Your First Release

### Step 1: Prepare the Repository

```bash
# Ensure you're on the main branch
git checkout main

# Pull latest changes from GitHub
git pull origin main

# Verify working directory is clean
git status
# Should output: nothing to commit, working tree clean
```

### Step 2: Update Version Number

The version is defined in [DaisyView/DaisyView.csproj](../DaisyView/DaisyView.csproj):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    ...
    <Version>1.0.0</Version>  <!-- Update this line -->
    ...
  </PropertyGroup>
</Project>
```

**Example versions:**
- First release: `1.0.0`
- Bug fix: `1.0.1`
- New feature: `1.1.0`
- Major version: `2.0.0`

### Step 3: Build and Test

```bash
# Clean previous build
dotnet clean DaisyView.sln

# Build Release configuration
dotnet build DaisyView.sln --configuration Release

# Run tests
dotnet test DaisyView.sln

# Optionally publish for distribution
dotnet publish -c Release -o ./publish
```

### Step 4: Commit Version Change

```bash
# Stage the version update
git add DaisyView/DaisyView.csproj

# Commit with descriptive message
git commit -m "chore: bump version to 1.0.0"

# Push to GitHub
git push origin main
```

### Step 5: Create Git Tag

```bash
# Create annotated tag with release notes
git tag -a v1.0.0 -m "Release version 1.0.0

Initial release of DaisyView - Image Viewer

## Major Features
- File system navigation with tree view
- Image thumbnail gallery
- WebM video support with MP4 conversion
- Fullscreen slideshow mode
- Theme support (Light/Dark/System)

## Requirements
- Windows 7 or later
- .NET 8.0 Runtime
- FFmpeg (optional, for WebM support)

## Contributors
- Initial development and implementation"

# Verify tag was created
git tag -v v1.0.0  # Should show the tag signature

# Push tag to GitHub
git push origin v1.0.0
```

### Step 6: Create GitHub Release

#### Option A: Using GitHub Web Interface (Recommended)

1. Navigate to: https://github.com/kfaubel/DaisyView/releases
2. Click **"Create a new release"** or **"Draft a new release"**
3. Fill in the following:

**Release Tag**
- Select or type: `v1.0.0`

**Release Title**
- `DaisyView 1.0.0 - Initial Release`

**Release Description** (use Markdown):
```markdown
# DaisyView 1.0.0 - Initial Release

A feature-rich image viewer for Windows with support for common image formats and WebM video playback.

## ✨ Features

### Core Features
- **File System Navigation** - Browse local drives and folders with tree view
- **Image Thumbnails** - View image files in thumbnail gallery
  - Supported formats: jpg, png, gif, bmp, tif, webp
- **WebM Video Support** - Play WebM videos with automatic MP4 conversion
- **Fullscreen Slideshow** - Present images in fullscreen with keyboard/mouse controls
- **Image Marking** - Mark/unmark images for batch operations
- **File Operations** - Move or delete marked images
- **Random Sort** - Shuffle image display order per folder
- **Theming** - Light, Dark, or System default theme
- **Update Checking** - Automatic checks for new versions
- **Comprehensive Logging** - Detailed activity and operation logs

## 🔧 Requirements

- **OS**: Windows 7 or later
- **.NET Runtime**: .NET 8.0 or later
- **FFmpeg** (optional): For WebM video playback support
  - Install via: Chocolatey, winget, or manual download

## 📥 Installation

### Option 1: Using Installer (Recommended)
1. Download `DaisyView-1.0.0.exe` from below
2. Run the installer
3. Follow the installation wizard
4. (Optional) Install FFmpeg for video support

### Option 2: Manual Installation
1. Download `DaisyView-1.0.0.zip`
2. Extract to desired location
3. Run `DaisyView.exe`

### Option 3: Using Package Manager

**Chocolatey** (if available):
```powershell
choco install daisyview
```

## 🎮 Quick Start

1. **Launch DaisyView** - Run the executable
2. **Navigate Folders** - Use the tree view on the left to browse
3. **View Thumbnails** - Select a folder to see image thumbnails
4. **Start Slideshow** - Press F11 or double-click a folder
5. **Mark Images** - Press Space in slideshow to mark images
6. **Batch Operations** - Right-click marked images to move or delete

## ⌨️ Keyboard Shortcuts (Slideshow Mode)

| Shortcut | Action |
|----------|--------|
| Right Arrow | Next image |
| Left Arrow | Previous image |
| Spacebar | Mark/unmark image |
| F11 | Toggle fullscreen |
| ESC | Exit slideshow |
| Mouse Wheel Up | Previous image |
| Mouse Wheel Down | Next image |

## 🎨 Configuration

Settings are stored in: `%APPDATA%\Local\DaisyView\settings.json`

### Available Settings
- `Theme` - "Light", "Dark", or "System"
- `ThumbnailSize` - "Small", "Medium", or "Large"
- `LoggingLevel` - "Trace", "Information", "Warning", "Error"
- `VideoCacheMaxSizeBytes` - Cache size for converted videos (default: 20 GB)
- `VideoCacheMaxAgeHours` - Cache file age limit (default: 30 days)

## 📝 Known Limitations

- Video support requires FFmpeg installation
- Initial WebM to MP4 conversion may take a few seconds
- Large folders (1000+ images) may take time to generate thumbnails

## 🐛 Bug Reports

Found an issue? Please report it:
https://github.com/kfaubel/DaisyView/issues

Include:
- What you were doing when the issue occurred
- Error messages or log excerpts
- Windows version and .NET runtime version

## 📚 Documentation

- [README.md](../../README.md) - Project overview
- [QUICK_REFERENCE.md](../../QUICK_REFERENCE.md) - Developer quick reference
- [DEVELOPMENT.md](../../DEVELOPMENT.md) - Development guide

## 👏 Credits

Built with:
- C# and WPF for desktop UI
- Serilog for structured logging
- FFmpeg for video processing

## 📄 License

[Add appropriate license - MIT, Apache 2.0, etc.]

---

### Release Artifacts

**Available Downloads:**
- `DaisyView-1.0.0.exe` - Windows installer (recommended)
- `DaisyView-1.0.0.zip` - Portable version
- Source code (zip) - Source code snapshot

**Checksums:**
```
SHA-256 Checksums
DaisyView-1.0.0.exe: [hash]
DaisyView-1.0.0.zip: [hash]
```

**Installation Size:**
- Installer: ~50 MB
- Installed: ~200 MB (including dependencies)
```

4. **Set Release Type**
   - Check "This is a pre-release" if still in beta
   - Leave unchecked for stable releases

5. **Attach Release Files** (optional but recommended)
   - Click "Attach binaries by dropping them here or selecting them"
   - Attach built executable or installer

6. **Click "Publish release"**

#### Option B: Using Command Line (Advanced)

If you have `gh` CLI installed:

```bash
gh release create v1.0.0 \
  --title "DaisyView 1.0.0 - Initial Release" \
  --notes "Release notes in markdown format" \
  --latest
```

### Step 7: Verify Release

1. Visit: https://github.com/kfaubel/DaisyView/releases
2. Confirm v1.0.0 appears in the list
3. Check that release notes display correctly
4. Verify any attached files are accessible

---

## Publishing Subsequent Releases

The process for future releases is simplified:

### Quick Release Process

```bash
# 1. Update version in DaisyView.csproj
#    Example: 1.0.0 → 1.0.1

# 2. Build and test
dotnet build DaisyView.sln --configuration Release
dotnet test DaisyView.sln

# 3. Commit version change
git add DaisyView/DaisyView.csproj
git commit -m "chore: bump version to 1.0.1"
git push origin main

# 4. Create and push tag
git tag -a v1.0.1 -m "Release 1.0.1 - Bug fixes and improvements"
git push origin v1.0.1

# 5. Create release on GitHub UI or using gh CLI
gh release create v1.0.1 \
  --title "DaisyView 1.0.1" \
  --notes "$(cat RELEASE_NOTES.md)"
```

### Release Notes Template

For consistency, use this template for release notes:

```markdown
## What's Changed

### ✨ New Features
- Feature description
- Feature description

### 🐛 Bug Fixes
- Fixed issue with...
- Fixed issue with...

### 🔧 Improvements
- Improved performance of...
- Enhanced error messages for...

### 📚 Documentation
- Updated installation guide
- Added troubleshooting section

## Breaking Changes

None

## System Requirements

- Windows 7 or later
- .NET 8.0 Runtime
- FFmpeg (optional, for WebM support)

## Installation

Download the installer or portable version from the assets below.

## Contributors

[List contributors if applicable]
```

---

## Release Checklist

Use this checklist before each release:

### Code Preparation
- [ ] All features complete and tested
- [ ] Unit tests passing
- [ ] Code review completed
- [ ] No open blocking issues
- [ ] Documentation updated

### Version Management
- [ ] Version number decided (semantic versioning)
- [ ] `DaisyView.csproj` updated with new version
- [ ] Version number documented in changelog
- [ ] All changes committed to `main` branch
- [ ] Changes pushed to GitHub

### Release Creation
- [ ] Git tag created locally (`git tag -a vX.Y.Z`)
- [ ] Git tag pushed to GitHub (`git push origin vX.Y.Z`)
- [ ] GitHub Release page created
- [ ] Release title is clear and descriptive
- [ ] Release notes written with clear format
- [ ] Release notes include:
  - [ ] New features
  - [ ] Bug fixes
  - [ ] Breaking changes (if any)
  - [ ] System requirements
  - [ ] Installation instructions
- [ ] Release artifacts attached (if applicable)
- [ ] Release published (not saved as draft)

### Post-Release
- [ ] Release visible on GitHub Releases page
- [ ] Release notes render correctly
- [ ] Download links work
- [ ] Update checking feature reflects new version (if automated)
- [ ] Announce release (if applicable - Twitter, forums, etc.)

---

## Best Practices

### 1. Semantic Versioning

Always follow [Semantic Versioning (SemVer)](https://semver.org/):

```
MAJOR.MINOR.PATCH

- MAJOR: Breaking changes
- MINOR: New features (backward compatible)
- PATCH: Bug fixes only
```

Examples:
- `1.0.0` → `1.0.1` (patch release - bug fix)
- `1.0.0` → `1.1.0` (minor release - new feature)
- `1.0.0` → `2.0.0` (major release - breaking change)

### 2. Clear Release Notes

Write release notes for your users, not developers:

✅ **Good:**
```
Fixed issue where fullscreen slideshow would freeze on WebM videos

Users experiencing freezing when playing WebM files in slideshow mode should see improved stability with this release.
```

❌ **Poor:**
```
Fixed async task deadlock in VideoConversionService
```

### 3. Consistent Tagging

Always use `v` prefix for version tags:
- ✅ `v1.0.0`, `v1.0.1`, `v1.1.0`
- ❌ `1.0.0`, `release-1.0.0`, `v1.0.0-final`

### 4. Pre-release Versions

For beta/rc versions, use pre-release notation:
- `v1.0.0-beta.1` (first beta)
- `v1.0.0-rc.1` (release candidate)

Mark on GitHub as "Pre-release" when creating the release.

### 5. Automated Builds

Consider implementing GitHub Actions to:
- Automatically build on tag creation
- Run tests before release
- Create release artifacts
- Automatically publish to release page

### 6. Release Artifacts

Always include:
- Executable or installer
- Portable version (zip)
- Source code (automatically included)
- Release notes with feature list
- System requirements

---

## Troubleshooting

### Common Issues and Solutions

#### "fatal: tag 'v1.0.0' already exists"

The tag already exists locally or remotely.

**Solution:**
```bash
# Check existing tags
git tag -l | grep v1.0.0

# Delete local tag
git tag -d v1.0.0

# Delete remote tag
git push origin --delete v1.0.0

# Create new tag
git tag -a v1.0.0 -m "Release message"
git push origin v1.0.0
```

#### "Permission denied" when pushing tag

You don't have write access to the repository.

**Solution:**
- Verify you have push access (check GitHub settings)
- Use correct GitHub authentication (SSH key or token)
- Ensure you're not on a fork (push to upstream repository)

#### Release doesn't appear after creating tag

GitHub Releases must be created separately from Git tags.

**Solution:**
1. Go to https://github.com/kfaubel/DaisyView/releases
2. Click "Create a new release"
3. Select the tag you just created
4. Fill in release details and publish

#### Incorrect version in release

The version in the executable doesn't match the release.

**Solution:**
1. Verify `DaisyView.csproj` has the correct version
2. Delete the release (can edit and delete)
3. Delete the Git tag: `git push origin --delete vX.Y.Z`
4. Rebuild with correct version and recreate release

#### How to modify a release after publishing

All changes to releases can be edited without recreating:

1. Go to https://github.com/kfaubel/DaisyView/releases
2. Find the release
3. Click the pencil icon (✏️) to edit
4. Update notes, title, or files
5. Save changes

#### GitHub Actions for automated releases

Consider setting up `.github/workflows/release.yml`:

```yaml
name: Create Release Build

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build Release
        run: dotnet build DaisyView.sln --configuration Release
      
      - name: Run Tests
        run: dotnet test DaisyView.sln
      
      - name: Publish
        run: dotnet publish -c Release -o ./publish
      
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: ./publish/**/*
```

---

## Summary

**Workflow for releases:**

1. **Update Version** → `DaisyView.csproj`
2. **Build & Test** → `dotnet build && dotnet test`
3. **Commit** → `git add/commit/push`
4. **Tag** → `git tag -a vX.Y.Z && git push origin vX.Y.Z`
5. **Release** → Create on GitHub Releases page
6. **Publish** → Click publish (or use `gh release create`)

**Each release takes ~5-10 minutes** once you're familiar with the process.

---

For questions or issues with GitHub Releases, see:
- [GitHub Docs - Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
- [Semantic Versioning](https://semver.org/)
- [GitHub CLI Documentation](https://cli.github.com/manual/)
