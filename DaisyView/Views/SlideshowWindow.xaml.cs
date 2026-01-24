using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DaisyView.Constants;
using DaisyView.Helpers;
using DaisyView.Models;
using DaisyView.Services;

namespace DaisyView.Views;

/// <summary>
/// Full-screen slideshow view for viewing images
/// Displays images stretched to maintain aspect ratio on a black background
/// Supports keyboard (arrows, spacebar, ESC, F11) and mouse (buttons, wheel) navigation
/// </summary>
public partial class SlideshowWindow : Window
{
    private List<ImageFile> _images = new();
    private int _currentImageIndex;
        private DispatcherTimer? _videoLoopTimer;
        private DispatcherTimer? _cursorHideTimer;
        private VideoConversionService? _videoConversionService;
        private FitsImageService? _fitsImageService;
        private bool _isCursorHidden = false;
        private bool _audioEnabled = true;
        private Point _lastMousePosition = new();

        public SlideshowWindow(List<ImageFile> images, int activeImageIndex = 0, VideoConversionService? videoConversionService = null, bool audioEnabled = true, FitsImageService? fitsImageService = null)
        {
            InitializeComponent();

            _images = images;
            _currentImageIndex = Math.Max(0, activeImageIndex);
            _videoConversionService = videoConversionService;
            _fitsImageService = fitsImageService;
            _audioEnabled = audioEnabled;
            // Setup video looping timer
            _videoLoopTimer = new DispatcherTimer();
            _videoLoopTimer.Interval = AppConstants.Timing.VideoLoopCheckInterval;
            _videoLoopTimer.Tick += VideoLoopTimer_Tick;

            // Setup cursor hide timer
            _cursorHideTimer = new DispatcherTimer();
            _cursorHideTimer.Interval = AppConstants.Timing.CursorHideDelay;
            _cursorHideTimer.Tick += CursorHideTimer_Tick;
            
            // Ensure cursor is visible initially
            Mouse.OverrideCursor = null;
            _isCursorHidden = false;

            // Wire up MediaElement error handling
            VideoDisplay.MediaFailed += VideoDisplay_MediaFailed;

            // Display first image
            DisplayCurrentImage();

            // Wire up events
            PreviewKeyDown += SlideshowWindow_PreviewKeyDown;
            MouseDown += SlideshowWindow_MouseDown;
            MouseWheel += SlideshowWindow_MouseWheel;
            MouseMove += SlideshowWindow_MouseMove;
            Closed += (s, e) => 
            {
                _videoLoopTimer?.Stop();
                _cursorHideTimer?.Stop();
                // Restore cursor visibility when closing slideshow
                Mouse.OverrideCursor = null;
            };
        }

        /// <summary>
        /// Handles MediaElement playback errors
        /// </summary>
        /// <summary>
        /// Handles MediaElement playback errors
        /// </summary>
        private void VideoDisplay_MediaFailed(object? sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            _videoLoopTimer?.Stop();
            var errorMsg = e.ErrorException?.Message ?? "Unknown error";
            FileNameDisplay.Text = $"Media error: {errorMsg}";
            FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
        }

        /// <summary>
        /// Handles MediaElement when file is opened and ready to play
        /// </summary>
        private void VideoDisplay_MediaOpened(object? sender, RoutedEventArgs e)
        {
            try
            {
                // File is loaded and ready, start playback
                _videoLoopTimer?.Start();
                VideoDisplay.Play();
            }
            catch (Exception ex)
            {
                FileNameDisplay.Text = $"Playback error: {ex.Message}";
                FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
            }
        }

        /// <summary>
        /// Handles video looping
        /// </summary>
        private void VideoLoopTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (VideoDisplay.Source != null && VideoDisplay.NaturalDuration.HasTimeSpan)
                {
                    // Check if video has finished playing
                    if (VideoDisplay.Position >= VideoDisplay.NaturalDuration.TimeSpan - AppConstants.Timing.VideoLoopCheckInterval)
                    {
                        // Reset to beginning and play again
                        VideoDisplay.Position = TimeSpan.Zero;
                        VideoDisplay.Play();
                    }
                }
            }
            catch
            {
                // Silently handle any timing issues
            }
        }

        /// <summary>
        /// Displays the current image or video with its file name at the bottom
        /// </summary>
        private void DisplayCurrentImage()
        {
            if (_images.Count == 0)
                return;

            var currentImage = _images[_currentImageIndex];
            
            try
            {
                if (ShouldPlayAsVideo(currentImage))
                {
                    HandleVideoDisplay(currentImage);
                }
                else
                {
                    HandleStaticImageDisplay(currentImage);
                }
                
                UpdateFileNameDisplay(currentImage);
            }
            catch (Exception ex)
            {
                HandleDisplayError(currentImage, ex);
            }
        }

        /// <summary>
        /// Determines if the file should be played as a video (including GIFs)
        /// </summary>
        private static bool ShouldPlayAsVideo(ImageFile image)
        {
            return image.IsVideo || MediaTypeHelper.IsGif(image.FilePath);
        }

        /// <summary>
        /// Checks if a video format requires conversion to MP4
        /// </summary>
        private static bool NeedsConversion(string filePath)
        {
            return MediaTypeHelper.NeedsConversion(filePath);
        }

        /// <summary>
        /// Handles video file display, including cached and non-cached files
        /// </summary>
        private void HandleVideoDisplay(ImageFile currentImage)
        {
            if (NeedsConversion(currentImage.FilePath))
            {
                var playbackPath = GetCachedVideoPath(currentImage);
                
                if (!string.IsNullOrEmpty(playbackPath) && System.IO.File.Exists(playbackPath))
                {
                    PlayVideo(playbackPath);
                }
                else
                {
                    // No cache available, start conversion
                    DisplayVideoNotSupported(currentImage);
                    _ = ConvertAndPlayVideoAsync(currentImage);
                }
            }
            else
            {
                // MP4 and GIF can play directly
                PlayVideo(currentImage.FilePath);
            }
        }

        /// <summary>
        /// Attempts to get a cached video path for formats requiring conversion
        /// </summary>
        private string? GetCachedVideoPath(ImageFile currentImage)
        {
            // First check if ConvertedVideoPath is already set
            if (!string.IsNullOrEmpty(currentImage.ConvertedVideoPath) && System.IO.File.Exists(currentImage.ConvertedVideoPath))
            {
                return currentImage.ConvertedVideoPath;
            }

            if (_videoConversionService == null)
                return null;

            // Check if cache file exists by looking it up
            var cacheFilePath = _videoConversionService.GetConvertedFilePath(currentImage.FilePath);
            if (cacheFilePath != null && System.IO.File.Exists(cacheFilePath))
            {
                var playbackPath = _videoConversionService.GetPlaybackPath(cacheFilePath);
                if (!string.IsNullOrEmpty(playbackPath) && System.IO.File.Exists(playbackPath))
                {
                    currentImage.ConvertedVideoPath = playbackPath;
                    return playbackPath;
                }
            }

            // Try fallback: search cache directory for matching files
            return TryFindCacheFileFallback(currentImage);
        }

        /// <summary>
        /// Fallback method to find cache files when primary lookup fails
        /// </summary>
        private string? TryFindCacheFileFallback(ImageFile currentImage)
        {
            try
            {
                var cacheDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DaisyView", "VideoCache");

                if (!System.IO.Directory.Exists(cacheDir))
                    return null;

                var baseFileName = System.IO.Path.GetFileNameWithoutExtension(currentImage.FilePath);
                var cachedFiles = System.IO.Directory.GetFiles(cacheDir, $"{baseFileName}_*.daicache");

                if (cachedFiles.Length > 0 && _videoConversionService != null)
                {
                    var playbackPath = _videoConversionService.GetPlaybackPath(cachedFiles[0]);
                    if (!string.IsNullOrEmpty(playbackPath) && System.IO.File.Exists(playbackPath))
                    {
                        currentImage.ConvertedVideoPath = playbackPath;
                        return playbackPath;
                    }
                }
            }
            catch
            {
                // Silently ignore fallback errors
            }

            return null;
        }

        /// <summary>
        /// Displays a static image
        /// </summary>
        private void HandleStaticImageDisplay(ImageFile currentImage)
        {
            _videoLoopTimer?.Stop();
            VideoDisplay.Stop();
            VideoDisplay.Source = null;

            System.Windows.Media.Imaging.BitmapSource? imageSource = null;

            // Check if this is a FITS file
            if (MediaTypeHelper.IsFitsFile(currentImage.FilePath))
            {
                imageSource = _fitsImageService?.LoadFitsImage(currentImage.FilePath);
            }
            else
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(System.IO.Path.GetFullPath(currentImage.FilePath), UriKind.Absolute);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                imageSource = bitmap;
            }

            VideoDisplay.Visibility = Visibility.Collapsed;
            VideoNotSupportedOverlay.Visibility = Visibility.Collapsed;
            ImageDisplay.Visibility = Visibility.Visible;
            ImageDisplay.Source = imageSource;
        }

        /// <summary>
        /// Updates the file name display with position and appropriate color
        /// </summary>
        private void UpdateFileNameDisplay(ImageFile currentImage)
        {
            var position = $"[{_currentImageIndex + 1}/{_images.Count}] ";
            FileNameDisplay.Text = position + currentImage.DisplayName;
            FileNameDisplay.Foreground = currentImage.IsMarked
                ? System.Windows.Media.Brushes.Red
                : System.Windows.Media.Brushes.White;
        }

        /// <summary>
        /// Handles display errors by showing error message
        /// </summary>
        private void HandleDisplayError(ImageFile currentImage, Exception ex)
        {
            _videoLoopTimer?.Stop();
            VideoDisplay.Stop();
            VideoDisplay.Source = null;
            VideoDisplay.Visibility = Visibility.Collapsed;
            VideoNotSupportedOverlay.Visibility = Visibility.Collapsed;
            ImageDisplay.Visibility = Visibility.Visible;
            ImageDisplay.Source = null;
            FileNameDisplay.Text = $"Error loading: {currentImage.DisplayName} - {ex.Message}";
            FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
        }

        /// <summary>
        /// Displays a video by playing it in the MediaElement
        /// </summary>
        private void PlayVideo(string videoPath)
        {
            try
            {
                // Fully stop and clear the previous video
                _videoLoopTimer?.Stop();
                VideoDisplay.Stop();
                VideoDisplay.Source = null;
                
                // Ensure we have a valid file path
                if (!System.IO.File.Exists(videoPath))
                {
                    FileNameDisplay.Text = $"Video file not found: {videoPath}";
                    FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
                    return;
                }

                // Verify file size to ensure it's a valid file
                var fileInfo = new System.IO.FileInfo(videoPath);
                if (fileInfo.Length == 0)
                {
                    FileNameDisplay.Text = $"Error: Video file is empty";
                    FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
                    return;
                }

                // Create proper file:// URI for MediaElement
                var fullPath = System.IO.Path.GetFullPath(videoPath);
                var fileUri = new Uri(fullPath, UriKind.Absolute);
                
                // Hide overlay before setting source
                ImageDisplay.Visibility = Visibility.Collapsed;
                VideoNotSupportedOverlay.Visibility = Visibility.Collapsed;
                VideoDisplay.Visibility = Visibility.Visible;
                
                // Set source - MediaElement will handle loading asynchronously
                // The MediaOpened event will fire when the file is ready to play
                VideoDisplay.Source = fileUri;
                VideoDisplay.Volume = _audioEnabled ? 1.0 : 0.0;
            }
            catch (Exception ex)
            {
                FileNameDisplay.Text = $"Error playing video: {ex.Message}";
                FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
            }
        }

        /// <summary>
        /// Displays video playback status overlay with thumbnail fallback
        /// </summary>
        private void DisplayVideoNotSupported(ImageFile currentImage, VideoConversionStatus status = VideoConversionStatus.Converting)
        {
            _videoLoopTimer?.Stop();
            VideoDisplay.Stop();
            VideoDisplay.Source = null;
            
            // Display the thumbnail as a fallback
            if (currentImage.ThumbnailData != null && currentImage.ThumbnailData.Length > 0)
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new System.IO.MemoryStream(currentImage.ThumbnailData);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                ImageDisplay.Source = bitmap;
            }
            else
            {
                ImageDisplay.Source = null;
            }

            // Update overlay message based on status
            switch (status)
            {
                case VideoConversionStatus.Converting:
                    OverlayIconText.Text = "⏳";
                    OverlayTitleText.Text = "Converting video...";
                    OverlaySubtitleText.Text = "Please wait, converting WebM to MP4";
                    break;
                case VideoConversionStatus.Failed:
                    OverlayIconText.Text = "❌";
                    OverlayTitleText.Text = "Video playback not supported";
                    OverlaySubtitleText.Text = "WebM format is not supported";
                    break;
            }

            // Show the overlay message
            ImageDisplay.Visibility = Visibility.Visible;
            VideoDisplay.Visibility = Visibility.Collapsed;
            VideoNotSupportedOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Converts a WebM video to MP4 asynchronously and plays it when ready
        /// </summary>
        private async Task ConvertAndPlayVideoAsync(ImageFile currentImage)
        {
            if (_videoConversionService == null || string.IsNullOrEmpty(currentImage.FilePath))
                return;

            try
            {
                var cachedFilePath = await _videoConversionService.ConvertWebmToMp4Async(currentImage.FilePath);
                
                // Only proceed if we're still viewing the same image
                if (_currentImageIndex >= _images.Count || _images[_currentImageIndex] != currentImage)
                    return;

                if (!string.IsNullOrEmpty(cachedFilePath) && System.IO.File.Exists(cachedFilePath))
                {
                    // Update the image object with the converted cache path for future use
                    currentImage.ConvertedVideoPath = cachedFilePath;
                    
                    // Get temporary playback path (creates temp .mp4 from .daicache if needed)
                    var playbackPath = _videoConversionService.GetPlaybackPath(cachedFilePath);
                    
                    if (string.IsNullOrEmpty(playbackPath))
                    {
                        Dispatcher.Invoke(() => DisplayVideoNotSupported(currentImage, VideoConversionStatus.Failed));
                        return;
                    }
                    
                    // Invoke on UI thread to update display
                    Dispatcher.Invoke(() => PlayVideo(playbackPath));
                }
                else
                {
                    // Conversion failed, show error
                    Dispatcher.Invoke(() => DisplayVideoNotSupported(currentImage, VideoConversionStatus.Failed));
                }
            }
            catch (Exception ex)
            {
                // Show conversion error on UI thread
                if (_currentImageIndex < _images.Count && _images[_currentImageIndex] == currentImage)
                {
                    Dispatcher.Invoke(() =>
                    {
                        FileNameDisplay.Text = $"Conversion error: {ex.Message}";
                        FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
                    });
                }
            }
        }

    /// <summary>
    /// Moves to the next image, wrapping to the first image if at the end
    /// </summary>
    private void ShowNextImage()
    {
        if (_images.Count == 0)
            return;

        _currentImageIndex = (_currentImageIndex + 1) % _images.Count;
        DisplayCurrentImage();
    }

    /// <summary>
    /// Moves to the previous image, wrapping to the last image if at the beginning
    /// </summary>
    private void ShowPreviousImage()
    {
        if (_images.Count == 0)
            return;

        _currentImageIndex = (_currentImageIndex - 1 + _images.Count) % _images.Count;
        DisplayCurrentImage();
    }

    /// <summary>
    /// Marks/unmarks the current image
    /// </summary>
    private void MarkCurrentImage()
    {
        if (_images.Count > 0)
        {
            _images[_currentImageIndex].IsMarked = !_images[_currentImageIndex].IsMarked;
            DisplayCurrentImage(); // Refresh to update color
        }
    }

    /// <summary>
    /// Toggles audio on/off for video playback
    /// </summary>
    private void ToggleAudio()
    {
        _audioEnabled = !_audioEnabled;
        VideoDisplay.Volume = _audioEnabled ? 1.0 : 0.0;
    }

    /// <summary>
    /// Handles keyboard input (arrows, spacebar, ESC, F11)
    /// </summary>
    /// </summary>
    private void SlideshowWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        switch (e.Key)
        {
            case Key.Right:
                ShowNextImage();
                break;
            case Key.Left:
                ShowPreviousImage();
                break;
            case Key.Space:
                MarkCurrentImage();
                break;
            case Key.M:
                ToggleAudio();
                break;
            case Key.Escape:
            case Key.F11:
                Close();
                break;
            default:
                e.Handled = false;
                break;
        }
    }

    /// <summary>
    /// Handles mouse button clicks (left: previous, right: next)
    /// </summary>
    private void SlideshowWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        switch (e.LeftButton)
        {
            case MouseButtonState.Pressed:
                ShowPreviousImage();
                e.Handled = true;
                break;
        }
        
        if (e.RightButton == MouseButtonState.Pressed)
        {
            ShowNextImage();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles mouse wheel (up: previous, down: next)
    /// </summary>
    private void SlideshowWindow_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
            ShowPreviousImage();
        else
            ShowNextImage();

        e.Handled = true;
    }

    /// <summary>
    /// Gets the currently displayed image
    /// </summary>
    public ImageFile? GetCurrentImage()
    {
        if (_currentImageIndex >= 0 && _currentImageIndex < _images.Count)
            return _images[_currentImageIndex];
        return null;
    }

    /// <summary>
    /// Gets all images in their current order
    /// </summary>
    public List<ImageFile> GetImages()
    {
        return _images;
    }

    /// <summary>
    /// Handles mouse movement - shows cursor and resets hide timer
    /// </summary>
    private void SlideshowWindow_MouseMove(object sender, MouseEventArgs e)
    {
        // Only respond to actual mouse movement, not UI focus events
        Point currentPosition = e.GetPosition(this);
        if (currentPosition == _lastMousePosition)
            return;

        _lastMousePosition = currentPosition;

        // Show cursor if it was hidden
        if (_isCursorHidden)
        {
            Mouse.OverrideCursor = null;
            _isCursorHidden = false;
        }

        // Restart the hide timer
        _cursorHideTimer?.Stop();
        _cursorHideTimer?.Start();
    }

    /// <summary>
    /// Handles cursor hide timer - hides cursor after 2 seconds of inactivity
    /// </summary>
    private void CursorHideTimer_Tick(object? sender, EventArgs e)
    {
        _cursorHideTimer?.Stop();
        Mouse.OverrideCursor = Cursors.None;
        _isCursorHidden = true;
    }
}
