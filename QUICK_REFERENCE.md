# DaisyView - Quick Reference Guide

## File Location Quick Reference

### Models
- `Models/AppSettings.cs` - Application configuration model
- `Models/TreeNode.cs` - File system tree node
- `Models/ImageFile.cs` - Image file representation
- `Models/GitHubRelease.cs` - GitHub release info

### Services
- `Services/SettingsService.cs` - JSON settings management
- `Services/LoggingService.cs` - Serilog-based logging
- `Services/FileSystemService.cs` - File system operations & watching
- `Services/ThumbnailService.cs` - Thumbnail generation
- `Services/UpdateCheckService.cs` - GitHub update checking

### ViewModels
- `ViewModels/ViewModelBase.cs` - Base class with INotifyPropertyChanged
- `ViewModels/RelayCommand.cs` - ICommand implementation
- `ViewModels/MainWindowViewModel.cs` - Main application logic

### Views
- `Views/MainWindow.xaml` / `MainWindow.xaml.cs` - Main UI
- `Views/SlideshowWindow.xaml` / `SlideshowWindow.xaml.cs` - Fullscreen slideshow
- `Views/MoveToDialog.xaml` / `MoveToDialog.xaml.cs` - Move files dialog
- `App.xaml` / `App.xaml.cs` - Application setup

### Tests
- `Tests/SettingsServiceTests.cs`
- `Tests/UpdateCheckServiceTests.cs`
- `Tests/ThumbnailServiceTests.cs`
- `Tests/ImageFileTests.cs`
- `Tests/TreeNodeTests.cs`
- `Tests/FileSystemServiceTests.cs`
- `Tests/RelayCommandTests.cs`

## Common Tasks

### Adding a New Feature
1. Create model (if needed) in `Models/`
2. Create/update service in `Services/`
3. Add command/property to `MainWindowViewModel.cs`
4. Update XAML binding in `Views/MainWindow.xaml`
5. Add code-behind logic in `Views/MainWindow.xaml.cs`
6. Add unit tests in `DaisyView.Tests/`

### Adding a New Command
1. Add command property to ViewModel:
   ```csharp
   public ICommand MyCommand => _myCommand ??= new RelayCommand(MyMethod);
   private ICommand? _myCommand;
   ```
2. Implement the method:
   ```csharp
   private void MyMethod(object? parameter) { }
   ```
3. Bind in XAML:
   ```xml
   <Button Command="{Binding MyCommand}" />
   ```

### Modifying Theme Colors
1. Edit color definitions in `App.xaml` under `Application.Resources`
2. Update the corresponding brush resources
3. Theme is applied at startup in `App.xaml.cs` based on settings

### Adding Logging
```csharp
_loggingService.LogUserAction("Action description", "Additional details");
_loggingService.LogInfo("Information message");
_loggingService.LogWarning("Warning message");
_loggingService.LogError("Error occurred", ex);
_loggingService.LogTrace("Detailed trace info");
```

### Reading/Writing Settings
```csharp
// Read
var settings = _settingsService.GetSettings();

// Write specific setting
_settingsService.UpdateSetting(s => s.Theme = "Dark");

// Favorites
_settingsService.AddFavoritFolder(path);
_settingsService.RemoveFavoriteFolder(path);
_settingsService.IsFavorite(path);

// Random order
_settingsService.SaveRandomOrder(folderPath, fileList);
_settingsService.GetRandomOrder(folderPath);
_settingsService.ClearRandomOrder(folderPath);
```

### File System Operations
```csharp
// Get roots and subfolders
var drives = _fileSystemService.GetRootDrives();
var subfolders = _fileSystemService.GetSubfolders(folderPath);

// Get images
var images = _fileSystemService.GetImageFiles(folderPath);

// Watch for changes
_fileSystemService.WatchFolder(folderPath);
_fileSystemService.UnwatchFolder(folderPath);

// File operations
await _fileSystemService.MoveFilesAsync(sourceList, destinationPath);
await _fileSystemService.DeleteFilesAsync(fileList);
_fileSystemService.CreateFolder(folderPath, folderName);
```

## Key Design Patterns

### MVVM Pattern
- **Model**: AppSettings, TreeNode, ImageFile, GitHubRelease
- **View**: XAML files (MainWindow, SlideshowWindow, MoveToDialog)
- **ViewModel**: MainWindowViewModel with properties and commands
- **Binding**: Two-way binding via {Binding PropertyName}

### Command Pattern
- Commands implement ICommand interface
- RelayCommand executes Action<T> with optional predicate
- Commands enable/disable based on CanExecute predicate

### Observer Pattern
- INotifyPropertyChanged for property binding
- Event handlers for tree expansion, selection
- Custom events (e.g., FolderNavigated)

### Factory Pattern
- SettingsService creates/loads AppSettings
- FileSystemService creates TreeNode and ImageFile objects

## Common Binding Scenarios

### Simple Property Binding
```xml
<TextBlock Text="{Binding PropertyName}" />
```

### Two-Way Binding
```xml
<ToggleButton IsChecked="{Binding RandomEnabled, Mode=TwoWay}" />
```

### Command Binding
```xml
<Button Command="{Binding ToggleFavoriteCommand}" />
```

### Collection Binding with Template
```xml
<ItemsControl ItemsSource="{Binding Images}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <!-- Item template here -->
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

## Debugging Tips

### Enable Trace Logging
Set LoggingLevel to "Trace" in settings.json:
```json
{
  "LoggingLevel": "Trace"
}
```

### View Settings File
Navigate to: `%APPDATA%\Local\DaisyView\settings.json`

### View Log Files
Navigate to: `%APPDATA%\Local\DaisyView\logs\`

### Check Update Checking
Monitor logs for GitHub API responses

### TreeView Binding Issues
- Verify ItemsSource binding on TreeView
- Check ItemTemplate is properly structured
- Ensure Children collection exists on TreeNode

### Command Not Firing
- Verify command property exists in ViewModel
- Check binding path is correct
- Ensure CanExecute predicate doesn't block execution

## Configuration Keys in Settings

```json
{
  "Theme": "Light|Dark|System",
  "LastActiveFolderPath": "C:\\Path\\To\\Folder",
  "LoggingLevel": "Trace|Information|Warning|Error",
  "FavoriteFolders": ["C:\\Folder1", "C:\\Folder2"],
  "ThumbnailSize": "Small|Medium|Large",
  "RandomOrderCache": {
    "C:\\Folder": ["file1.jpg", "file2.jpg"]
  },
  "RandomEnabledFolders": ["C:\\Folder"]
}
```

## Event Hooks

### Available Events
- `MainWindowViewModel.FolderNavigated` - When user opens a folder
- `FileSystemService.FileSystemChanged` - When watched folder contents change
- `ThumbnailService.ThumbnailGenerated` - When a thumbnail is ready
- `Window.Closing` - When application is closing

## Resource Keys

### Colors
- `LightBackground`, `DarkBackground`
- `LightForeground`, `DarkForeground`
- `LightBorder`, `DarkBorder`
- `LightPanel`, `DarkPanel`
- `LightAccent`, `DarkAccent`

### Brushes
- `BackgroundBrush`
- `ForegroundBrush`
- `BorderBrush`
- `PanelBrush`
- `AccentBrush`

## Performance Considerations

- Thumbnail generation is async to avoid UI blocking
- Tree view uses lazy loading to avoid loading all folders
- File system watching only on open folders
- Settings cached in memory during runtime
- Logs are written to disk asynchronously by Serilog

## Code Style Guidelines

- Use nullable reference types (`string?`)
- Use required keyword for essential properties
- Use C# latest features (string interpolation, switch expressions)
- Always dispose IDisposable objects
- Use async/await for long-running operations
- Add XML documentation to public members
- Use meaningful names and helper comments

---

**Quick Links**:
- Settings location: `%APPDATA%\Local\DaisyView\settings.json`
- Logs location: `%APPDATA%\Local\DaisyView\logs\`
- GitHub Issues: [Add repo URL]
- Documentation: See README.md and DEVELOPMENT.md
