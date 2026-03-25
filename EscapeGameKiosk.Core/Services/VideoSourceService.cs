using System;
using System.IO;

namespace EscapeGameKiosk.Services;

/// <summary>
/// Implementation of video source validation and resolution.
/// </summary>
public sealed class VideoSourceService : IVideoSourceService
{
  private readonly string _baseDirectory;

  private static readonly string[] YouTubeHosts =
  [
    "youtube.com",
    "www.youtube.com",
    "m.youtube.com",
    "youtu.be",
    "www.youtu.be"
  ];

  public VideoSourceService(string? baseDirectory = null)
  {
    _baseDirectory = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
  }

  public bool TryResolveVideoSource(string? videoPath, out Uri? resolvedUri)
  {
    resolvedUri = null;

    if (string.IsNullOrWhiteSpace(videoPath))
    {
      return false;
    }

    // YouTube URL support
    if (TryParseYouTubeUrl(videoPath, out Uri? youtubeUri))
    {
      resolvedUri = youtubeUri;
      return true;
    }

    string candidatePath = videoPath;

    if (!Path.IsPathRooted(candidatePath))
    {
      candidatePath = Path.Combine(_baseDirectory, candidatePath);
    }

    candidatePath = Path.GetFullPath(candidatePath);

    if (!File.Exists(candidatePath))
    {
      return false;
    }

    // Use the Uri(string) constructor so Windows file paths become proper file:// URIs.
    resolvedUri = new Uri(candidatePath);
    return true;
  }

  private static bool TryParseYouTubeUrl(string raw, out Uri? youtubeUri)
  {
    youtubeUri = null;

    string trimmed = raw.Trim();

    if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
    {
      // Allow URLs without scheme (common when copy/pasting)
      if (Uri.TryCreate("https://" + trimmed, UriKind.Absolute, out Uri? withScheme))
      {
        uri = withScheme;
      }
    }

    if (uri == null)
    {
      return false;
    }

    if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
    {
      return false;
    }

    foreach (string host in YouTubeHosts)
    {
      if (string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
      {
        youtubeUri = uri;
        return true;
      }
    }

    return false;
  }

  public bool FileExists(string path)
  {
    return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
  }
}
