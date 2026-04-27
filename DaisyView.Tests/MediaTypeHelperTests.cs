using Xunit;
using DaisyView.Helpers;

namespace DaisyView.Tests;

/// <summary>
/// Unit tests for the MediaTypeHelper class
/// </summary>
public class MediaTypeHelperTests
{
    #region IsSupportedMedia Tests

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("photo.png", true)]
    [InlineData("photo.bmp", true)]
    [InlineData("photo.tif", true)]
    [InlineData("photo.tiff", true)]
    [InlineData("photo.webp", true)]
    [InlineData("photo.fits", true)]
    [InlineData("photo.fit", true)]
    [InlineData("video.gif", true)]
    [InlineData("video.mp4", true)]
    [InlineData("video.avi", true)]
    [InlineData("video.mpeg", true)]
    [InlineData("video.mpg", true)]
    public void IsSupportedMedia_ReturnsTrue_ForSupportedExtensions(string fileName, bool expected)
    {
        Assert.Equal(expected, MediaTypeHelper.IsSupportedMedia(fileName));
    }

    [Theory]
    [InlineData("document.txt")]
    [InlineData("document.pdf")]
    [InlineData("music.mp3")]
    [InlineData("archive.zip")]
    [InlineData("readme")]
    public void IsSupportedMedia_ReturnsFalse_ForUnsupportedExtensions(string fileName)
    {
        Assert.False(MediaTypeHelper.IsSupportedMedia(fileName));
    }

    [Theory]
    [InlineData("PHOTO.JPG")]
    [InlineData("Photo.Jpeg")]
    [InlineData("VIDEO.MP4")]
    public void IsSupportedMedia_IsCaseInsensitive(string fileName)
    {
        Assert.True(MediaTypeHelper.IsSupportedMedia(fileName));
    }

    #endregion

    #region IsStaticImage Tests

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("photo.png", true)]
    [InlineData("photo.bmp", true)]
    [InlineData("photo.tif", true)]
    [InlineData("photo.tiff", true)]
    [InlineData("photo.webp", true)]
    [InlineData("photo.fits", true)]
    [InlineData("photo.fit", true)]
    public void IsStaticImage_ReturnsTrue_ForStaticImageExtensions(string fileName, bool expected)
    {
        Assert.Equal(expected, MediaTypeHelper.IsStaticImage(fileName));
    }

    [Theory]
    [InlineData("animation.gif")]
    [InlineData("video.mp4")]
    public void IsStaticImage_ReturnsFalse_ForVideoExtensions(string fileName)
    {
        Assert.False(MediaTypeHelper.IsStaticImage(fileName));
    }

    #endregion

    #region IsVideoFile Tests

    [Theory]
    [InlineData("video.mp4", true)]
    [InlineData("video.avi", true)]
    [InlineData("video.mpeg", true)]
    [InlineData("video.mpg", true)]
    public void IsVideoFile_ReturnsTrue_ForVideoExtensions(string fileName, bool expected)
    {
        Assert.Equal(expected, MediaTypeHelper.IsVideoFile(fileName));
    }

    [Theory]
    [InlineData("animation.gif")]
    [InlineData("photo.jpg")]
    [InlineData("photo.png")]
    [InlineData("photo.bmp")]
    public void IsVideoFile_ReturnsFalse_ForImageExtensions(string fileName)
    {
        Assert.False(MediaTypeHelper.IsVideoFile(fileName));
    }

    #endregion

    #region IsGif Tests

    [Theory]
    [InlineData("animation.gif", true)]
    [InlineData("ANIMATION.GIF", true)]
    [InlineData("Animation.Gif", true)]
    public void IsGif_ReturnsTrue_ForGifFiles(string fileName, bool expected)
    {
        Assert.Equal(expected, MediaTypeHelper.IsGif(fileName));
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("video.mp4")]
    public void IsGif_ReturnsFalse_ForNonGifFiles(string fileName)
    {
        Assert.False(MediaTypeHelper.IsGif(fileName));
    }

    #endregion

    #region IsMp4 Tests

    [Theory]
    [InlineData("video.mp4", true)]
    [InlineData("VIDEO.MP4", true)]
    [InlineData("Video.Mp4", true)]
    public void IsMp4_ReturnsTrue_ForMp4Files(string fileName, bool expected)
    {
        Assert.Equal(expected, MediaTypeHelper.IsMp4(fileName));
    }

    [Theory]
    [InlineData("video.avi")]
    public void IsMp4_ReturnsFalse_ForNonMp4Files(string fileName)
    {
        Assert.False(MediaTypeHelper.IsMp4(fileName));
    }

    #endregion

    #region AllSupportedExtensions Tests

    [Fact]
    public void AllSupportedExtensions_ContainsExpectedExtensions()
    {
        Assert.Contains(".jpg", MediaTypeHelper.AllSupportedExtensions);
        Assert.Contains(".jpeg", MediaTypeHelper.AllSupportedExtensions);
        Assert.Contains(".png", MediaTypeHelper.AllSupportedExtensions);
        Assert.Contains(".gif", MediaTypeHelper.AllSupportedExtensions);
        Assert.Contains(".mp4", MediaTypeHelper.AllSupportedExtensions);
        Assert.Contains(".fits", MediaTypeHelper.AllSupportedExtensions);
        Assert.Contains(".fit", MediaTypeHelper.AllSupportedExtensions);
    }

    [Fact]
    public void AllSupportedExtensions_HasExpectedCount()
    {
        // 10 image extensions + 4 video extensions = 14
        Assert.Equal(14, MediaTypeHelper.AllSupportedExtensions.Length);
    }

    #endregion

    #region IsFitsFile Tests

    [Theory]
    [InlineData("image.fits", true)]
    [InlineData("IMAGE.FITS", true)]
    [InlineData("Image.Fits", true)]
    [InlineData("image.fit", true)]
    [InlineData("IMAGE.FIT", true)]
    public void IsFitsFile_ReturnsTrue_ForFitsFiles(string fileName, bool expected)
    {
        Assert.Equal(expected, MediaTypeHelper.IsFitsFile(fileName));
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("video.mp4")]
    [InlineData("image.png")]
    public void IsFitsFile_ReturnsFalse_ForNonFitsFiles(string fileName)
    {
        Assert.False(MediaTypeHelper.IsFitsFile(fileName));
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("noextension")]
    public void IsSupportedMedia_HandlesFilesWithoutExtension(string fileName)
    {
        Assert.False(MediaTypeHelper.IsSupportedMedia(fileName));
    }

    [Theory]
    [InlineData("C:\\Users\\Photos\\vacation.jpg")]
    [InlineData("/home/user/photos/vacation.png")]
    public void IsSupportedMedia_WorksWithFullPaths(string filePath)
    {
        Assert.True(MediaTypeHelper.IsSupportedMedia(filePath));
    }

    #endregion
}
