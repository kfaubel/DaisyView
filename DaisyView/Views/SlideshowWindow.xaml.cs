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
    private bool _mediaReadyForLooping = false;
    private DispatcherTimer? _gifAnimationTimer;
    private List<System.Windows.Media.Imaging.BitmapSource>? _gifFrames;
    private List<TimeSpan>? _gifFrameDurations;
    private int _gifFrameIndex = 0;
    private DispatcherTimer? _cursorHideTimer;
    private FitsImageService? _fitsImageService;
    private bool _isCursorHidden = false;
    private bool _audioEnabled = true;
    private Point _lastMousePosition = new();
    private Action<string>? _onAddToFavorites;

        public SlideshowWindow(List<ImageFile> images, int activeImageIndex = 0, bool audioEnabled = true, FitsImageService? fitsImageService = null, Action<string>? onAddToFavorites = null)
        {
            InitializeComponent();

            _images = images;
            _currentImageIndex = Math.Max(0, activeImageIndex);
            _fitsImageService = fitsImageService;
            _audioEnabled = audioEnabled;
            _onAddToFavorites = onAddToFavorites;
            // Setup video looping timer
            _videoLoopTimer = new DispatcherTimer();
            _videoLoopTimer.Interval = AppConstants.Timing.VideoLoopCheckInterval;
            _videoLoopTimer.Tick += VideoLoopTimer_Tick;

            // Setup cursor hide timer
            _cursorHideTimer = new DispatcherTimer();
            _cursorHideTimer.Interval = AppConstants.Timing.CursorHideDelay;
            _cursorHideTimer.Tick += CursorHideTimer_Tick;

            // Setup GIF frame animation timer
            _gifAnimationTimer = new DispatcherTimer();
            _gifAnimationTimer.Interval = TimeSpan.FromMilliseconds(100);
            _gifAnimationTimer.Tick += GifAnimationTimer_Tick;
            
            // Ensure cursor is visible initially
            Mouse.OverrideCursor = null;
            _isCursorHidden = false;

            // Wire up MediaElement error handling
            VideoDisplay.MediaFailed += VideoDisplay_MediaFailed;
            VideoDisplay.MediaEnded += VideoDisplay_MediaEnded;

            // Display first image
            DisplayCurrentImage();

            // Wire up events
            PreviewKeyDown += SlideshowWindow_PreviewKeyDown;
            MouseDown += SlideshowWindow_MouseDown;
            MouseWheel += SlideshowWindow_MouseWheel;
            MouseMove += SlideshowWindow_MouseMove;
            ContentRendered += (s, e) => Focus();
            Closed += (s, e) => 
            {
                _videoLoopTimer?.Stop();
                StopGifAnimation();
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
                _mediaReadyForLooping = true;

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
        /// Handles end of media playback and restarts from the beginning.
        /// </summary>
        private void VideoDisplay_MediaEnded(object? sender, RoutedEventArgs e)
        {
            RestartPlaybackFromStart();
        }

        /// <summary>
        /// Handles video looping
        /// </summary>
        private void VideoLoopTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (!_mediaReadyForLooping || VideoDisplay.Source == null)
                    return;

                if (VideoDisplay.Source != null && VideoDisplay.NaturalDuration.HasTimeSpan)
                {
                    // Check if video has finished playing
                    if (VideoDisplay.Position >= VideoDisplay.NaturalDuration.TimeSpan - AppConstants.Timing.VideoLoopCheckInterval)
                    {
                        RestartPlaybackFromStart();
                    }
                }
            }
            catch
            {
                // Silently handle any timing issues
            }
        }

        /// <summary>
        /// Restarts playback from the beginning.
        /// </summary>
        private void RestartPlaybackFromStart()
        {
            try
            {
                VideoDisplay.Position = TimeSpan.Zero;
                VideoDisplay.Play();
            }
            catch
            {
                // Ignore transient playback state issues.
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
                if (MediaTypeHelper.IsGif(currentImage.FilePath))
                {
                    HandleGifDisplay(currentImage);
                }
                else if (ShouldPlayAsVideo(currentImage))
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
            return image.IsVideo;
        }

        /// <summary>
        /// Handles video file display
        /// </summary>
        private void HandleVideoDisplay(ImageFile currentImage)
        {
            PlayVideo(currentImage.FilePath);
        }

        /// <summary>
        /// Displays and animates a GIF using frame metadata for reliable looping.
        /// </summary>
        private void HandleGifDisplay(ImageFile currentImage)
        {
            _videoLoopTimer?.Stop();
            _mediaReadyForLooping = false;
            VideoDisplay.Stop();
            VideoDisplay.Source = null;

            StopGifAnimation();

            var fullPath = System.IO.Path.GetFullPath(currentImage.FilePath);
            if (!System.IO.File.Exists(fullPath))
                throw new InvalidOperationException($"GIF file not found: {fullPath}");

            var gifDecoder = new System.Windows.Media.Imaging.GifBitmapDecoder(
                new Uri(fullPath, UriKind.Absolute),
                System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

            if (gifDecoder.Frames.Count == 0)
                throw new InvalidOperationException("GIF contains no frames.");

            _gifFrames = new List<System.Windows.Media.Imaging.BitmapSource>(gifDecoder.Frames.Count);
            _gifFrameDurations = new List<TimeSpan>(gifDecoder.Frames.Count);
            _gifFrameIndex = 0;

            foreach (var frame in gifDecoder.Frames)
            {
                if (frame.CanFreeze)
                    frame.Freeze();

                _gifFrames.Add(frame);
                _gifFrameDurations.Add(GetGifFrameDelay(frame));
            }

            VideoDisplay.Visibility = Visibility.Collapsed;
            VideoNotSupportedOverlay.Visibility = Visibility.Collapsed;
            ImageDisplay.Visibility = Visibility.Visible;
            ImageDisplay.Source = _gifFrames[0];

            if (_gifFrames.Count > 1)
            {
                _gifAnimationTimer ??= new DispatcherTimer();
                _gifAnimationTimer.Interval = _gifFrameDurations[0];
                _gifAnimationTimer.Start();
            }
        }

        /// <summary>
        /// Displays a static image
        /// </summary>
        private void HandleStaticImageDisplay(ImageFile currentImage)
        {
            _videoLoopTimer?.Stop();
            _mediaReadyForLooping = false;
            VideoDisplay.Stop();
            VideoDisplay.Source = null;
            StopGifAnimation();

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
            _mediaReadyForLooping = false;
            VideoDisplay.Stop();
            VideoDisplay.Source = null;
            StopGifAnimation();
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
                _mediaReadyForLooping = false;
                VideoDisplay.Stop();
                VideoDisplay.Source = null;
                StopGifAnimation();
                
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
    /// Advances GIF animation frames based on embedded per-frame delays.
    /// </summary>
    private void GifAnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_gifFrames == null || _gifFrames.Count == 0 || _gifFrameDurations == null)
            return;

        _gifFrameIndex = (_gifFrameIndex + 1) % _gifFrames.Count;
        ImageDisplay.Source = _gifFrames[_gifFrameIndex];
        _gifAnimationTimer!.Interval = _gifFrameDurations[_gifFrameIndex];
    }

    /// <summary>
    /// Stops GIF animation and clears associated frame state.
    /// </summary>
    private void StopGifAnimation()
    {
        _gifAnimationTimer?.Stop();
        _gifFrames = null;
        _gifFrameDurations = null;
        _gifFrameIndex = 0;
    }

    /// <summary>
    /// Reads frame delay metadata from a GIF frame.
    /// </summary>
    private static TimeSpan GetGifFrameDelay(System.Windows.Media.Imaging.BitmapFrame frame)
    {
        const int defaultDelayCentiseconds = 10;
        int delayCentiseconds = defaultDelayCentiseconds;

        if (frame.Metadata is System.Windows.Media.Imaging.BitmapMetadata metadata && metadata.ContainsQuery("/grctlext/Delay"))
        {
            object? delayValue = metadata.GetQuery("/grctlext/Delay");
            if (delayValue is ushort ushortDelay)
            {
                delayCentiseconds = ushortDelay;
            }
            else if (delayValue is byte byteDelay)
            {
                delayCentiseconds = byteDelay;
            }
            else if (delayValue is int intDelay)
            {
                delayCentiseconds = intDelay;
            }
        }

        if (delayCentiseconds <= 1)
            delayCentiseconds = defaultDelayCentiseconds;

        return TimeSpan.FromMilliseconds(delayCentiseconds * 10.0);
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
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        switch (key)
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
            case Key.D1: case Key.NumPad1: _onAddToFavorites?.Invoke("1"); break;
            case Key.D2: case Key.NumPad2: _onAddToFavorites?.Invoke("2"); break;
            case Key.D3: case Key.NumPad3: _onAddToFavorites?.Invoke("3"); break;
            case Key.D4: case Key.NumPad4: _onAddToFavorites?.Invoke("4"); break;
            case Key.D5: case Key.NumPad5: _onAddToFavorites?.Invoke("5"); break;
            case Key.D6: case Key.NumPad6: _onAddToFavorites?.Invoke("6"); break;
            case Key.D7: case Key.NumPad7: _onAddToFavorites?.Invoke("7"); break;
            case Key.D8: case Key.NumPad8: _onAddToFavorites?.Invoke("8"); break;
            case Key.D9: case Key.NumPad9: _onAddToFavorites?.Invoke("9"); break;
            case Key.D0: case Key.NumPad0: _onAddToFavorites?.Invoke("0"); break;
            default:
                e.Handled = false;
                break;
        }
    }

    /// <summary>
    /// Handles mouse button clicks (left: previous, right: next, middle: exit)
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
        
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            Close();
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
