using EscapeGameKiosk;
using EscapeGameKiosk.Services;
using FluentAssertions;
using Xunit;

namespace EscapeGameKiosk.Tests.Services;

public class ConfigurationValidatorTests
{
  [Fact]
  public void Validate_WithValidSettings_ReturnsSuccess()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = "admin",
      VideoPath = CreateTempVideoFile(), // Create actual file
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeTrue();
    result.Errors.Should().BeEmpty();

    // Cleanup
    if (File.Exists(settings.VideoPath))
    {
      File.Delete(settings.VideoPath);
    }
  }

  [Fact]
  public void Validate_WithYouTubeVideoPath_ReturnsSuccess()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = "admin",
      VideoPath = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeTrue();
    result.Errors.Should().BeEmpty();
  }

  [Fact]
  public void Validate_WithEmptyPassword_ReturnsFailure()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = "",
      VideoPath = CreateTempVideoFile(),
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().ContainSingle()
      .Which.Should().Contain("Password cannot be empty");

    // Cleanup
    if (File.Exists(settings.VideoPath))
    {
      File.Delete(settings.VideoPath);
    }
  }

  [Fact]
  public void Validate_WithNullPassword_ReturnsFailure()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = null!,
      VideoPath = CreateTempVideoFile(),
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Contains("Password cannot be empty"));

    // Cleanup
    if (File.Exists(settings.VideoPath))
    {
      File.Delete(settings.VideoPath);
    }
  }

  [Fact]
  public void Validate_WithWhitespacePassword_ReturnsFailure()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = "   ",
      VideoPath = CreateTempVideoFile(),
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Contains("Password cannot be empty"));

    // Cleanup
    if (File.Exists(settings.VideoPath))
    {
      File.Delete(settings.VideoPath);
    }
  }

  [Fact]
  public void Validate_WithMissingVideoPath_ReturnsFailure()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = "admin",
      VideoPath = "",
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Contains("VideoPath is not configured"));
  }

  [Fact]
  public void Validate_WithNonExistentVideoFile_ReturnsFailure()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = "admin",
      VideoPath = @"C:\NonExistent\video.mp4",
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Contains("Video file not found"));
  }

  [Fact]
  public void Validate_WithMultipleErrors_ReturnsAllErrors()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = "",
      VideoPath = @"C:\NonExistent\video.mp4",
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().HaveCount(2);
    result.Errors.Should().Contain(e => e.Contains("Password"));
    result.Errors.Should().Contain(e => e.Contains("Video file not found"));
  }

  [Fact]
  public void Validate_WithRelativeVideoPath_ValidatesCorrectly()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    string tempFile = "test_video.mp4";
    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, tempFile);
    File.WriteAllText(fullPath, "dummy video content");

    var settings = new AppSettings
    {
      Password = "admin",
      VideoPath = tempFile, // Relative path
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeTrue();
    result.Errors.Should().BeEmpty();

    // Cleanup
    if (File.Exists(fullPath))
    {
      File.Delete(fullPath);
    }
  }

  [Fact]
  public void Validate_WithAbsoluteVideoPath_ValidatesCorrectly()
  {
    // Arrange
    var validator = new ConfigurationValidator();
    string tempFile = Path.GetTempFileName();
    File.WriteAllText(tempFile, "dummy video content");

    var settings = new AppSettings
    {
      Password = "admin",
      VideoPath = tempFile, // Absolute path
      AllowKeyboardHook = true
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeTrue();

    // Cleanup
    if (File.Exists(tempFile))
    {
      File.Delete(tempFile);
    }
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void Validate_WithDifferentKeyboardHookSettings_Succeeds(bool allowKeyboardHook)
  {
    // Arrange
    var validator = new ConfigurationValidator();
    var settings = new AppSettings
    {
      Password = "admin",
      VideoPath = CreateTempVideoFile(),
      AllowKeyboardHook = allowKeyboardHook
    };

    // Act
    var result = validator.Validate(settings);

    // Assert
    result.IsValid.Should().BeTrue();

    // Cleanup
    if (File.Exists(settings.VideoPath))
    {
      File.Delete(settings.VideoPath);
    }
  }

  // Helper method to create a temporary video file
  private static string CreateTempVideoFile()
  {
    string tempFile = Path.GetTempFileName();
    File.WriteAllText(tempFile, "dummy video content");
    return tempFile;
  }
}
