using EscapeGameKiosk.Services;
using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace EscapeGameKiosk.Tests.Services;

public class VideoSourceServiceTests
{
  private static string CreateTempDirectory()
  {
    string dir = Path.Combine(Path.GetTempPath(), "EscapeGameKiosk.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(dir);
    return dir;
  }

  private static string CreateFile(string directory, string filename)
  {
    string fullPath = Path.Combine(directory, filename);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    File.WriteAllText(fullPath, "test");
    return fullPath;
  }

  [Fact]
  public void TryResolveVideoSource_WithExistingAbsolutePath_ReturnsFileUri()
  {
    string tempDir = CreateTempDirectory();
    try
    {
      // Arrange
      string absolutePath = CreateFile(tempDir, "test.mp4");
      var service = new VideoSourceService();

      // Act
      bool result = service.TryResolveVideoSource(absolutePath, out Uri? videoUri);

      // Assert
      result.Should().BeTrue();
      videoUri.Should().NotBeNull();
      videoUri!.IsAbsoluteUri.Should().BeTrue();
      videoUri!.Scheme.Should().Be(Uri.UriSchemeFile);
      videoUri!.LocalPath.Should().Be(absolutePath);
    }
    finally
    {
      Directory.Delete(tempDir, recursive: true);
    }
  }

  [Fact]
  public void TryResolveVideoSource_WithHttpUrl_ReturnsFalse()
  {
    // Arrange
    var service = new VideoSourceService();
    string httpUrl = "http://example.com/video.mp4";

    // Act
    bool result = service.TryResolveVideoSource(httpUrl, out Uri? videoUri);

    // Assert
    result.Should().BeFalse();
    videoUri.Should().BeNull();
  }

  [Fact]
  public void TryResolveVideoSource_WithHttpsUrl_ReturnsFalse()
  {
    // Arrange
    var service = new VideoSourceService();
    string httpsUrl = "https://example.com/video.mp4";

    // Act
    bool result = service.TryResolveVideoSource(httpsUrl, out Uri? videoUri);

    // Assert
    result.Should().BeFalse();
    videoUri.Should().BeNull();
  }

  [Theory]
  [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
  [InlineData("https://youtu.be/dQw4w9WgXcQ")]
  [InlineData("www.youtube.com/watch?v=dQw4w9WgXcQ")]
  public void TryResolveVideoSource_WithYouTubeUrl_ReturnsTrue(string url)
  {
    // Arrange
    var service = new VideoSourceService();

    // Act
    bool result = service.TryResolveVideoSource(url, out Uri? videoUri);

    // Assert
    result.Should().BeTrue();
    videoUri.Should().NotBeNull();
    videoUri!.Scheme.Should().Match(s => s == Uri.UriSchemeHttp || s == Uri.UriSchemeHttps);
  }

  [Fact]
  public void TryResolveVideoSource_WithEmptyPath_ReturnsFalse()
  {
    // Arrange
    var service = new VideoSourceService();

    // Act
    bool result = service.TryResolveVideoSource("", out Uri? videoUri);

    // Assert
    result.Should().BeFalse();
    videoUri.Should().BeNull();
  }

  [Fact]
  public void TryResolveVideoSource_WithNullPath_ReturnsFalse()
  {
    // Arrange
    var service = new VideoSourceService();

    // Act
    bool result = service.TryResolveVideoSource(null!, out Uri? videoUri);

    // Assert
    result.Should().BeFalse();
    videoUri.Should().BeNull();
  }

  [Fact]
  public void TryResolveVideoSource_WithWhitespacePath_ReturnsFalse()
  {
    // Arrange
    var service = new VideoSourceService();

    // Act
    bool result = service.TryResolveVideoSource("   ", out Uri? videoUri);

    // Assert
    result.Should().BeFalse();
    videoUri.Should().BeNull();
  }

  [Fact]
  public void TryResolveVideoSource_WithExistingRelativePath_ResolvesToAbsolutePath()
  {
    string tempDir = CreateTempDirectory();
    try
    {
      // Arrange
      _ = CreateFile(tempDir, "video.mp4");
      var service = new VideoSourceService(baseDirectory: tempDir);
      string relativePath = "video.mp4";

      // Act
      bool result = service.TryResolveVideoSource(relativePath, out Uri? videoUri);

      // Assert
      result.Should().BeTrue();
      videoUri.Should().NotBeNull();
      videoUri!.IsAbsoluteUri.Should().BeTrue();
      videoUri!.Scheme.Should().Be(Uri.UriSchemeFile);
      videoUri!.LocalPath.Should().Be(Path.Combine(tempDir, "video.mp4"));
    }
    finally
    {
      Directory.Delete(tempDir, recursive: true);
    }
  }

  [Fact]
  public void TryResolveVideoSource_WithExistingRelativePathInSubfolder_ResolvesCorrectly()
  {
    string tempDir = CreateTempDirectory();
    try
    {
      // Arrange
      string relativePath = Path.Combine("Videos", "subfolder", "test.mp4");
      _ = CreateFile(tempDir, relativePath);
      var service = new VideoSourceService(baseDirectory: tempDir);

      // Act
      bool result = service.TryResolveVideoSource(relativePath, out Uri? videoUri);

      // Assert
      result.Should().BeTrue();
      videoUri.Should().NotBeNull();
      videoUri!.Scheme.Should().Be(Uri.UriSchemeFile);
      videoUri!.LocalPath.Should().Be(Path.Combine(tempDir, relativePath));
    }
    finally
    {
      Directory.Delete(tempDir, recursive: true);
    }
  }

  [Theory]
  [InlineData("video.mp4")]
  [InlineData("video.avi")]
  [InlineData("video.wmv")]
  [InlineData("video.mov")]
  [InlineData("VIDEO.MP4")]
  public void TryResolveVideoSource_WithVariousExtensions_Succeeds(string filename)
  {
    string tempDir = CreateTempDirectory();
    try
    {
      // Arrange
      _ = CreateFile(tempDir, filename);
      var service = new VideoSourceService(baseDirectory: tempDir);

      // Act
      bool result = service.TryResolveVideoSource(filename, out Uri? videoUri);

      // Assert
      result.Should().BeTrue();
      videoUri.Should().NotBeNull();
    }
    finally
    {
      Directory.Delete(tempDir, recursive: true);
    }
  }

  [Fact]
  public void TryResolveVideoSource_WithFileUriScheme_ReturnsFalse()
  {
    // Arrange
    var service = new VideoSourceService();
    string fileUri = "file:///C:/Videos/test.mp4";

    // Act
    bool result = service.TryResolveVideoSource(fileUri, out Uri? videoUri);

    // Assert
    result.Should().BeFalse();
    videoUri.Should().BeNull();
  }

  [Fact]
  public void TryResolveVideoSource_WithInvalidUri_ReturnsFalse()
  {
    // Arrange
    var service = new VideoSourceService();
    string invalidUri = "ht!tp://invalid";

    // Act
    bool result = service.TryResolveVideoSource(invalidUri, out Uri? videoUri);

    // Assert
    result.Should().BeFalse();
    videoUri.Should().BeNull();
  }

  [Fact]
  public void TryResolveVideoSource_CalledMultipleTimes_ProducesSameResult()
  {
    string tempDir = CreateTempDirectory();
    try
    {
      // Arrange
      _ = CreateFile(tempDir, "test.mp4");
      var service = new VideoSourceService(baseDirectory: tempDir);
      string videoPath = "test.mp4";

      // Act
      service.TryResolveVideoSource(videoPath, out Uri? uri1);
      service.TryResolveVideoSource(videoPath, out Uri? uri2);

      // Assert
      uri1.Should().NotBeNull();
      uri2.Should().NotBeNull();
      uri1!.AbsoluteUri.Should().Be(uri2!.AbsoluteUri);
    }
    finally
    {
      Directory.Delete(tempDir, recursive: true);
    }
  }

  [Fact]
  public void TryResolveVideoSource_WithExistingWindowsPath_HandlesCorrectly()
  {
    string tempDir = CreateTempDirectory();
    try
    {
      // Arrange
      string path = CreateFile(tempDir, "movie.mp4");
      var service = new VideoSourceService();

      // Act
      bool result = service.TryResolveVideoSource(path, out Uri? videoUri);

      // Assert
      result.Should().BeTrue();
      videoUri.Should().NotBeNull();
      videoUri!.Scheme.Should().Be(Uri.UriSchemeFile);
      videoUri!.LocalPath.Should().Be(path);
    }
    finally
    {
      Directory.Delete(tempDir, recursive: true);
    }
  }

  [Fact]
  public void TryResolveVideoSource_WithUncPath_ReturnsFalse_WhenFileDoesNotExist()
  {
    // Arrange
    var service = new VideoSourceService();
    string uncPath = @"\\network\share\video.mp4";

    // Act
    bool result = service.TryResolveVideoSource(uncPath, out Uri? videoUri);

    // Assert
    result.Should().BeFalse();
    videoUri.Should().BeNull();
  }
}
