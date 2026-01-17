# DaisyView - Implementation Notes & Known Issues

## Known Issues & Limitations

### Current Limitations (by design)
1. **Thumbnail Caching**: Thumbnails are not cached to disk; they're regenerated each session
   - Status: Acceptable for MVP; can add caching in future for performance
   
2. **Trash/Recycle Bin**: Delete operations are permanent (not using Windows trash)
   - Status: Placeholder exists; full implementation requires P/Invoke or third-party library
   - Recommendation: Consider using `FileOperation` API for proper trash support

3. **Network Drives**: Should work but not specifically tested
   - Status: Basic functionality should work; may have performance implications

4. **Very Large Folders**: No pagination on thumbnail view
   - Status: UI might become sluggish with 10,000+ images
   - Recommendation: Add virtual scrolling or pagination

5. **Hidden Folders**: Intentionally excluded from tree view per spec
   - Status: As designed; can be made configurable

## Implementation Notes

### Theme System
- Color resources are stored in App.xaml
- ThemeBrushes are updated at runtime in App.xaml.cs
- System theme detection reads from Windows Registry
- Theme changes require application restart (not hot-reload)

### Settings & Persistence
- Settings are read on app startup and written on every change
- No transaction support - concurrent access could cause issues
- Settings file format is plain JSON (can be manually edited)
- Recommend backing up settings.json before major updates

### Logging
- Logs rotate daily; old logs are not automatically deleted
- Log folder can grow over time (implement cleanup if needed)
- Trace level logging can be very verbose; use Information level for normal operation
- File system operations > 5 seconds trigger warning level

### File System Watching
- Only directories that are opened are watched for changes
- File system changes trigger property change notifications
- No debouncing on rapid consecutive changes
- Watchers are properly disposed when folders are closed

### Thumbnail Generation
- Visible thumbnails (first ~10) are generated synchronously
- Remaining thumbnails are queued for background generation
- Background task is cancelled if user navigates to a different folder
- No persistent thumbnail cache

### Slideshow Mode
- Images are displayed with Uniform stretch (maintains aspect ratio)
- Next/Previous wraps around (circular navigation)
- Marked status is tracked but not reflected in main view until slideshow closes
- Mouse button handling: Left button issues, Right button works
  - Note: Middle button detection may not work on all mice

### Random Sort
- Random order is persisted per folder
- Clearing random discard cached sort order
- Re-enabling random creates a new random order
- Clearing folder cache resets random ordering

## Code Structure Decisions

### Why MVVM?
- Cleaner separation of concerns
- Easier to unit test business logic
- Binding reduces code-behind complexity
- Standard for WPF applications

### Why Serilog?
- Flexible sink configuration (file, console, etc.)
- Structured logging support for future enhancement
- Good performance characteristics
- Rich configuration options

### Why ICommand?
- Built-in WPF pattern for button/control binding
- RelayCommand provides simple implementation without dependencies
- Supports can-execute logic without polling

### Why Lazy TreeView Loading?
- Improves UI responsiveness for large folder hierarchies
- Reduces memory usage for unexplored branches
- Only loads what user needs

## Potential Performance Issues & Solutions

### Issue: Many images in a folder
- Current: All loaded at once
- Solution: Add virtual scrolling or pagination

### Issue: Deep folder hierarchies
- Current: Fully expanded trees could use lots of memory
- Solution: Already using lazy loading; tree is efficient

### Issue: Slow network shares
- Current: File operations may block UI thread
- Solution: Already using async/await for long operations

### Issue: Many favorites
- Current: All loaded into ListBox
- Solution: Could virtualize if list gets very large (1000+)

## Security Considerations

### Current Mitigations
- File paths are validated before operations
- Settings file is only read from user's AppData (not shared)
- No network access except GitHub update checking
- HTTPS used for GitHub API

### Potential Risks
- No input validation on manually edited JSON settings
- No authentication for update checking (GitHub public API)
- File operations run with current user privileges
- No encryption of settings file

## Testing Coverage

### Covered
- ✅ Model initialization and state changes
- ✅ Settings persistence (CRUD operations)
- ✅ Version comparison logic
- ✅ Command execution and predicates
- ✅ Thumbnail size calculations
- ✅ File system path validation

### Not Covered (Manual/Integration Testing Needed)
- ⚠️ UI binding and data flow
- ⚠️ File system watching and change detection
- ⚠️ Actual image file loading and display
- ⚠️ Thumbnail generation from real images
- ⚠️ GitHub API communication
- ⚠️ Theme application and switching
- ⚠️ Slideshow navigation and controls
- ⚠️ File move/delete operations

## Future Optimization Opportunities

### Quick Wins (1-2 hours)
- [ ] Add thumbnail caching to disk
- [ ] Implement debouncing for file system change events
- [ ] Add keyboard shortcut support to main window
- [ ] Cache folder size calculations

### Medium Effort (4-8 hours)
- [ ] Implement virtual scrolling for thumbnail view
- [ ] Add search/filter capability
- [ ] Implement proper trash/recycle bin support
- [ ] Add image rotation and basic editing

### Larger Initiatives (16+ hours)
- [ ] Support for more image formats
- [ ] Plugin/extension system
- [ ] Web-based remote viewer
- [ ] Multi-selection and batch operations
- [ ] Database-backed thumbnail cache

## Deployment Considerations

### For Distribution
- Publish using `dotnet publish -c Release`
- Create installer (consider WIX or InnoSetup)
- Code sign executable (optional but recommended)
- Create GitHub release with .exe and changelog

### First-Time Setup
- App automatically creates AppData folders
- Default settings are applied if missing
- No user configuration required for basic functionality

### Updates
- Update checking happens automatically on startup
- User can choose to skip update
- Downloaded update requires manual installation (current implementation)

## Dependencies & Licensing

### NuGet Packages Used
- Serilog (Apache 2.0) - Logging
- System.Text.Json (MIT) - JSON serialization
- System.Net.Http.Json (MIT) - HTTP JSON support
- xUnit (Apache 2.0) - Unit testing
- Moq (BSD 3-Clause) - Mocking library

All dependencies are open source and compatible with commercial use.

---

**Last Updated**: January 16, 2026
**Version**: 1.0.0 (In Development)
