using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DaisyView.Models;

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

    public SlideshowWindow(List<ImageFile> images, int activeImageIndex = 0)
    {
        InitializeComponent();

        _images = images;
        _currentImageIndex = Math.Max(0, activeImageIndex);

        // Window setup
        WindowState = WindowState.Maximized;
        Background = System.Windows.Media.Brushes.Black;

        // Display first image
        DisplayCurrentImage();

        // Wire up events
        PreviewKeyDown += SlideshowWindow_PreviewKeyDown;
        MouseDown += SlideshowWindow_MouseDown;
        MouseWheel += SlideshowWindow_MouseWheel;
    }

    /// <summary>
    /// Displays the current image with its file name at the bottom
    /// </summary>
    private void DisplayCurrentImage()
    {
        if (_images.Count == 0)
            return;

        var currentImage = _images[_currentImageIndex];
        
        try
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(currentImage.FilePath);
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            ImageDisplay.Source = bitmap;
            
            // Update file name with color based on marked state
            FileNameDisplay.Text = currentImage.FileName;
            FileNameDisplay.Foreground = currentImage.IsMarked 
                ? System.Windows.Media.Brushes.Red 
                : System.Windows.Media.Brushes.White;
        }
        catch
        {
            FileNameDisplay.Text = $"Error loading: {currentImage.FileName}";
            FileNameDisplay.Foreground = System.Windows.Media.Brushes.Yellow;
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
