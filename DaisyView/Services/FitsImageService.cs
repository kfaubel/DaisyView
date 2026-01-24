using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using nom.tam.fits;
using DaisyView.Helpers;

namespace DaisyView.Services;

/// <summary>
/// Service for loading and converting FITS (Flexible Image Transport System) astronomical images
/// </summary>
public class FitsImageService
{
    private readonly LoggingService _loggingService;

    public FitsImageService(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <summary>
    /// Loads a FITS file and converts it to a BitmapSource for display
    /// </summary>
    /// <param name="filePath">Path to the FITS file</param>
    /// <param name="decodePixelWidth">Optional width to scale the image to (for thumbnails)</param>
    /// <returns>BitmapSource that can be displayed in WPF, or null if loading fails</returns>
    public BitmapSource? LoadFitsImage(string filePath, int? decodePixelWidth = null)
    {
        if (!MediaTypeHelper.IsFitsFile(filePath))
        {
            _loggingService.LogWarning("File is not a FITS file: {FilePath}", filePath);
            return null;
        }

        try
        {
            var fits = new Fits(filePath);

            // Read the primary HDU (Header Data Unit)
            var hdu = fits.ReadHDU();
            if (hdu == null)
            {
                _loggingService.LogWarning("FITS file has no HDU: {FilePath}", filePath);
                return null;
            }

            var data = hdu.Data;
            if (data == null)
            {
                _loggingService.LogWarning("FITS HDU has no data: {FilePath}", filePath);
                return null;
            }

            // Get the image data as an array
            var kernel = data.DataArray;
            if (kernel == null)
            {
                _loggingService.LogWarning("FITS data kernel is null: {FilePath}", filePath);
                return null;
            }

            // Get image dimensions from header
            var header = hdu.Header;
            int naxis = header.GetIntValue("NAXIS", 0);
            
            if (naxis < 2)
            {
                _loggingService.LogWarning("FITS file is not a 2D image (NAXIS={Naxis}): {FilePath}", naxis, filePath);
                return null;
            }

            int width = header.GetIntValue("NAXIS1", 0);
            int height = header.GetIntValue("NAXIS2", 0);

            if (width <= 0 || height <= 0)
            {
                _loggingService.LogWarning("Invalid FITS dimensions {Width}x{Height}: {FilePath}", width, height, filePath);
                return null;
            }

            // Convert the FITS data to a grayscale bitmap
            var bitmap = ConvertFitsDataToBitmap(kernel, width, height, naxis);
            
            if (bitmap == null)
            {
                return null;
            }

            // Scale if needed for thumbnails
            if (decodePixelWidth.HasValue && decodePixelWidth.Value > 0 && decodePixelWidth.Value < width)
            {
                double scale = (double)decodePixelWidth.Value / width;
                var scaledBitmap = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
                scaledBitmap.Freeze();
                return scaledBitmap;
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to load FITS file {FilePath}: {Message}", filePath, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Converts FITS data array to a WPF BitmapSource
    /// </summary>
    private BitmapSource? ConvertFitsDataToBitmap(object kernel, int width, int height, int naxis)
    {
        try
        {
            double minVal = double.MaxValue;
            double maxVal = double.MinValue;
            double[,] normalizedData = new double[height, width];

            // Handle different data types and array structures
            // FITS data can be various numeric types (byte, short, int, float, double)
            if (kernel is Array array)
            {
                // For 2D images, the kernel is typically a jagged array
                if (naxis == 2)
                {
                    ExtractAndNormalize2DData(array, normalizedData, width, height, ref minVal, ref maxVal);
                }
                else if (naxis >= 3)
                {
                    // For 3D data (e.g., RGB or data cubes), take the first plane
                    Extract3DDataFirstPlane(array, normalizedData, width, height, ref minVal, ref maxVal);
                }
            }

            // Normalize to 0-255 range
            double range = maxVal - minVal;
            if (range <= 0) range = 1;

            byte[] pixels = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double normalized = (normalizedData[y, x] - minVal) / range * 255.0;
                    // FITS images are typically stored bottom-to-top, flip vertically
                    pixels[(height - 1 - y) * width + x] = (byte)Math.Clamp(normalized, 0, 255);
                }
            }

            // Create grayscale bitmap
            var bitmap = BitmapSource.Create(
                width, height,
                96, 96,
                PixelFormats.Gray8,
                null,
                pixels,
                width);

            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("Failed to convert FITS data to bitmap: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Extracts and normalizes 2D FITS data
    /// </summary>
    private void ExtractAndNormalize2DData(Array array, double[,] normalizedData, int width, int height, ref double minVal, ref double maxVal)
    {
        for (int y = 0; y < height; y++)
        {
            var row = array.GetValue(y);
            if (row is Array rowArray)
            {
                for (int x = 0; x < width && x < rowArray.Length; x++)
                {
                    double value = ConvertToDouble(rowArray.GetValue(x));
                    normalizedData[y, x] = value;
                    if (!double.IsNaN(value) && !double.IsInfinity(value))
                    {
                        if (value < minVal) minVal = value;
                        if (value > maxVal) maxVal = value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts the first plane from 3D FITS data (for RGB or data cubes)
    /// </summary>
    private void Extract3DDataFirstPlane(Array array, double[,] normalizedData, int width, int height, ref double minVal, ref double maxVal)
    {
        // Take the first plane of 3D data
        var firstPlane = array.GetValue(0);
        if (firstPlane is Array planeArray)
        {
            ExtractAndNormalize2DData(planeArray, normalizedData, width, height, ref minVal, ref maxVal);
        }
    }

    /// <summary>
    /// Converts various numeric types to double
    /// </summary>
    private static double ConvertToDouble(object? value)
    {
        return value switch
        {
            byte b => b,
            sbyte sb => sb,
            short s => s,
            ushort us => us,
            int i => i,
            uint ui => ui,
            long l => l,
            ulong ul => ul,
            float f => f,
            double d => d,
            decimal dec => (double)dec,
            _ => 0.0
        };
    }
}
