using System.IO;

namespace EscapeGameKiosk.Services;

/// <summary>
/// Validates application configuration settings.
/// </summary>
public sealed class ConfigurationValidator : IConfigurationValidator
{
  private readonly VideoSourceService _videoSourceService = new();

  /// <summary>
  /// Validates the application settings.
  /// </summary>
  /// <param name="settings">The settings to validate.</param>
  /// <returns>A validation result containing success status and error messages.</returns>
  public ValidationResult Validate(AppSettings settings)
  {
    List<string> errors = new();

    // Validate password
    if (string.IsNullOrWhiteSpace(settings.Password))
    {
      errors.Add("Password cannot be empty. Please configure a password in appsettings.json.");
    }

    // Validate video path (if specified)
    if (!string.IsNullOrWhiteSpace(settings.VideoPath))
    {
      if (!IsValidVideoPath(settings.VideoPath))
      {
        errors.Add($"Video file not found: '{settings.VideoPath}'. Please verify the path in appsettings.json.");
      }
    }
    else
    {
      errors.Add("VideoPath is not configured. Please specify a video file path in appsettings.json.");
    }

    // AllowKeyboardHook is already a boolean, so no validation needed
    // (JSON deserialization will fail if it's not a valid boolean)

    return errors.Count > 0 
      ? ValidationResult.Failure(errors) 
      : ValidationResult.Success();
  }

  /// <summary>
  /// Validates that a video path exists and is accessible.
  /// </summary>
  /// <param name="videoPath">The video path to validate.</param>
  /// <returns>True if the path is valid and file exists; otherwise, false.</returns>
  private static bool IsValidVideoPath(string videoPath)
  {
    try
    {
      // Allow YouTube URLs
      if (new VideoSourceService().TryResolveVideoSource(videoPath, out Uri? uri)
          && uri != null
          && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
      {
        return true;
      }

      // Handle relative paths
      string fullPath = Path.IsPathRooted(videoPath)
        ? videoPath
        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, videoPath);

      return File.Exists(fullPath);
    }
    catch
    {
      return false;
    }
  }
}
