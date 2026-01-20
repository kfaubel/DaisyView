using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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
        private VideoConversionService? _videoConversionService;

        public SlideshowWindow(List<ImageFile> images, int activeImageIndex = 0, VideoConversionService? videoConversionService = null)
        {
            InitializeComponent();

            _images = images;
            _currentImageIndex = Math.Max(0, activeImageIndex);
            _videoConversionService = videoConversionService;
            // Setup video looping timer
            _videoLoopTimer = new DispatcherTimer();
            _videoLoopTimer.Interval = TimeSpan.FromMilliseconds(100);
            _videoLoopTimer.Tick += VideoLoopTimer_Tick;

            // Wire up MediaElement error handling
            VideoDisplay.MediaFailed += VideoDisplay_MediaFailed;

            // Display first image
            DisplayCurrentImage();

            // Wire up events
            PreviewKeyDown += SlideshowWindow_PreviewKeyDown;
            MouseDown += SlideshowWindow_MouseDown;
            MouseWheel += SlideshowWindow_MouseWheel;
            Closed += (s, e) => _videoLoopTimer?.Stop();
        }

        /// <summary>
        /// Handles MediaElement playback errors
        /// </summary>
        private void VideoDisplay_MediaFailed(object? sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            FileNameDisplay.Text = $"Media playback error: {e.ErrorException?.Message ?? "Unknown error"}";
            FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
            VideoNotSupportedOverlay.Visibility = Visibility.Visible;
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
                    if (VideoDisplay.Position >= VideoDisplay.NaturalDuration.TimeSpan - TimeSpan.FromMilliseconds(100))
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
                if (currentImage.IsVideo)
                {
                    // Check if we have a converted video file ready
                    if (!string.IsNullOrEmpty(currentImage.ConvertedVideoPath) && System.IO.File.Exists(currentImage.ConvertedVideoPath))
                    {
                        PlayVideo(currentImage.ConvertedVideoPath);
                    }
                    else if (_videoConversionService != null)
                    {
                        // Start conversion asynchronously and show thumbnail while converting
                        // Intentional fire-and-forget: conversion runs in background, UI updates via Dispatcher
                        DisplayVideoNotSupported(currentImage);
                        _ = ConvertAndPlayVideoAsync(currentImage);
                    }
                    else
                    {
                        // No conversion service available, show thumbnail
                        DisplayVideoNotSupported(currentImage);
                    }
                }
                else
                {
                    // Display image
                    _videoLoopTimer?.Stop();
                    VideoDisplay.Stop();
                    VideoDisplay.Source = null;
                    
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(System.IO.Path.GetFullPath(currentImage.FilePath), UriKind.Absolute);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    VideoDisplay.Visibility = Visibility.Collapsed;
                    VideoNotSupportedOverlay.Visibility = Visibility.Collapsed;
                    ImageDisplay.Visibility = Visibility.Visible;
                    ImageDisplay.Source = bitmap;
                }
                
                // Update file name with color based on marked state
                FileNameDisplay.Text = currentImage.FileName;
                FileNameDisplay.Foreground = currentImage.IsMarked 
                    ? System.Windows.Media.Brushes.Red 
                    : System.Windows.Media.Brushes.White;
            }
            catch (Exception ex)
            {
                _videoLoopTimer?.Stop();
                VideoDisplay.Stop();
                VideoDisplay.Source = null;
                VideoDisplay.Visibility = Visibility.Collapsed;
                VideoNotSupportedOverlay.Visibility = Visibility.Collapsed;
                ImageDisplay.Visibility = Visibility.Visible;
                ImageDisplay.Source = null;
                FileNameDisplay.Text = $"Error loading: {currentImage.FileName} - {ex.Message}";
                FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
            }
        }

        /// <summary>
        /// Displays a video by playing it in the MediaElement
        /// </summary>
        private void PlayVideo(string videoPath)
        {
            try
            {
                _videoLoopTimer?.Stop();
                VideoDisplay.Stop();
                VideoDisplay.Source = new Uri(System.IO.Path.GetFullPath(videoPath), UriKind.Absolute);
                
                ImageDisplay.Visibility = Visibility.Collapsed;
                VideoNotSupportedOverlay.Visibility = Visibility.Collapsed;
                VideoDisplay.Visibility = Visibility.Visible;
                
                _videoLoopTimer?.Start();
                VideoDisplay.Play();
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
                    // Get temporary playback path (creates temp .mp4 from .daicache if needed)
                    var playbackPath = _videoConversionService.GetPlaybackPath(cachedFilePath);
                    
                    if (string.IsNullOrEmpty(playbackPath))
                    {
                        Dispatcher.Invoke(() => DisplayVideoNotSupported(currentImage, VideoConversionStatus.Failed));
                        return;
                    }
                    
                    // Update the image object with the playback file path
                    currentImage.ConvertedVideoPath = playbackPath;
                    
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
    /// Handles keyboard input (arrows, spacebar, ESC, F11)
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
}
