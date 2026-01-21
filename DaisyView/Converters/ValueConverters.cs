using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DaisyView.Converters;

/// <summary>
/// Value converter to convert boolean to brush for red/default borders
/// Used to highlight active images with red border
/// </summary>
public class BoolToRedBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return new SolidColorBrush(Color.FromRgb(255, 0, 0));
        }
        return new SolidColorBrush(Color.FromRgb(200, 200, 200));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Value converter to convert byte array thumbnail data to BitmapImage
/// </summary>
public class ByteArrayToBitmapImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte[] thumbnailData && thumbnailData.Length > 0)
        {
            try
            {
                using var stream = new MemoryStream(thumbnailData);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Value converter to convert boolean to white or gray brush
/// Used for enabled/disabled visual states
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isEnabled && isEnabled)
        {
            return new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }
        return new SolidColorBrush(Color.FromRgb(102, 102, 102));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean audio enabled state to speaker icon
/// </summary>
public class AudioIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool audioEnabled && audioEnabled)
        {
            return "🔊"; // Speaker icon
        }
        return "🔇"; // Muted speaker icon
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean audio enabled state to gold or grey color
/// </summary>
public class AudioColorConverter : IMultiValueConverter
{
    private static readonly SolidColorBrush GoldBrush = new(Color.FromRgb(255, 215, 0));
    private static readonly SolidColorBrush GreyBrush = new(Color.FromRgb(128, 128, 128));
    private static readonly SolidColorBrush WhiteBrush = new(Color.FromRgb(255, 255, 255));

    static AudioColorConverter()
    {
        // Freeze brushes for better performance
        GoldBrush.Freeze();
        GreyBrush.Freeze();
        WhiteBrush.Freeze();
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is bool audioEnabled)
        {
            return audioEnabled ? GoldBrush : GreyBrush;
        }
        return WhiteBrush;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts an integer to Visibility - Visible if > 0, Collapsed otherwise
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count && count > 0)
        {
            return System.Windows.Visibility.Visible;
        }
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
