using System;

namespace EscapeGameKiosk.Services;

/// <summary>
/// Service for validating and resolving video source paths.
/// </summary>
public interface IVideoSourceService
{
  /// <summary>
  /// Validates and resolves a video path to an absolute URI.
  /// </summary>
  /// <param name="videoPath">The video path from configuration (relative or absolute).</param>
  /// <param name="resolvedUri">The resolved absolute URI if valid; otherwise, null.</param>
  /// <returns>True if the video path is valid and file exists; otherwise, false.</returns>
  bool TryResolveVideoSource(string? videoPath, out Uri? resolvedUri);

  /// <summary>
  /// Checks if a file exists at the specified path.
  /// </summary>
  /// <param name="path">The file path to check.</param>
  /// <returns>True if the file exists; otherwise, false.</returns>
  bool FileExists(string path);
}
