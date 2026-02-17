using System;
using System.Collections.Specialized;
using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.ViewModels;

public partial class VideoViewModel : ObservableObject
{
  private readonly ILogger<VideoViewModel> _logger;
  private readonly System.Windows.Threading.DispatcherTimer _timer;

  [ObservableProperty]
  private Uri? _videoSource;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(IsLocalVideo))]
  private bool _isWebVideo;

  [ObservableProperty]
  private Uri? _webVideoUri;

  [ObservableProperty]
  private bool _isWebVideoLoading;

  [ObservableProperty]
  private bool _isPlaying;

  [ObservableProperty]
  private bool _isVideoLoaded;

  [ObservableProperty]
  private string _playPauseButtonContent = Constants.UI.PlaySymbol;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(PlaybackTimestamp))]
  private double _position;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(PlaybackTimestamp))]
  private double _duration;

  [ObservableProperty]
  private double _volume = 0.5;

  [ObservableProperty]
  private double _speedRatio = 1.0;

  [ObservableProperty]
  private bool _isDraggingSeek;

  public VideoViewModel(ILogger<VideoViewModel> logger)
  {
    _logger = logger;

    _timer = new System.Windows.Threading.DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(Constants.Timers.VideoControlIntervalMs)
    };
    _timer.Tick += Timer_OnTick;
  }

  public event EventHandler? LockRequested;
  public event EventHandler? PlayRequested;
  public event EventHandler? PauseRequested;
  public event EventHandler? StopRequested;
  public event EventHandler<double>? SeekRequested;

  public bool IsLocalVideo => !IsWebVideo;

  public string PlaybackTimestamp => $"{FormatPosition(Position)} / {FormatDuration(Duration)}";

  [RelayCommand]
  private void PlayPause()
  {
    if (!IsVideoLoaded)
    {
      return;
    }

    if (IsPlaying)
    {
      PauseRequested?.Invoke(this, EventArgs.Empty);
      IsPlaying = false;
    }
    else
    {
      PlayRequested?.Invoke(this, EventArgs.Empty);
      IsPlaying = true;
    }

    UpdatePlayPauseButton();
  }

  [RelayCommand]
  private void StopVideo()
  {
    if (!IsVideoLoaded)
    {
      return;
    }

    StopRequested?.Invoke(this, EventArgs.Empty);
    IsPlaying = false;
    Position = 0;
    UpdatePlayPauseButton();
  }

  [RelayCommand]
  private void Lock()
  {
    _logger.LogInformation("Lock requested from video screen");
    LockRequested?.Invoke(this, EventArgs.Empty);
  }

  public void SetVideoSource(Uri source)
  {
    VideoSource = source;
    IsVideoLoaded = true;
    IsPlaying = false;
    Position = 0;
    UpdatePlayPauseButton();
    _logger.LogInformation("Video source set: {Source}", source);
  }

  public void LoadVideo(Uri source)
  {
    if (TryBuildYouTubeEmbedUri(source, out Uri? embedUri))
    {
      IsWebVideo = true;
      IsWebVideoLoading = true;
      WebVideoUri = embedUri;
      VideoSource = null;
      Duration = 0;
      Position = 0;
      IsVideoLoaded = true;
      IsPlaying = false;
      UpdatePlayPauseButton();
      _logger.LogInformation("Loading YouTube video: {Source}", source);
      return;
    }

    IsWebVideo = false;
    IsWebVideoLoading = false;
    WebVideoUri = null;
    SetVideoSource(source);
    _logger.LogInformation("Loading local video: {Source}", source);
  }

  public void Start()
  {
    _timer.Start();
    _logger.LogDebug("Video control timer started");
  }

  public void StopPlayback()
  {
    _timer.Stop();
    StopVideoCommand.Execute(null);

    // For web videos (YouTube in WebView2), StopRequested only affects the local MediaElement.
    // Clear the web video state so the view can unload/stop the embedded player.
    if (IsWebVideo)
    {
      WebVideoUri = null;
      IsWebVideo = false;
      IsWebVideoLoading = false;
      VideoSource = null;
      IsVideoLoaded = false;
      IsPlaying = false;
      Position = 0;
      Duration = 0;
      UpdatePlayPauseButton();
    }

    _logger.LogDebug("Video control timer stopped");
  }

  public void OnMediaOpened(double durationMs)
  {
    IsVideoLoaded = true;
    IsPlaying = false;
    Duration = durationMs;
    UpdatePlayPauseButton();
    _logger.LogInformation("Video loaded. Duration: {Duration}ms", durationMs);
  }

  public void OnMediaEnded()
  {
    StopVideoCommand.Execute(null);
    _logger.LogInformation("Video playback ended");
  }

  public void StartSeek()
  {
    IsDraggingSeek = true;
  }

  public void EndSeek(double positionMs)
  {
    if (IsVideoLoaded)
    {
      SeekRequested?.Invoke(this, positionMs);
      Position = positionMs;
    }
    IsDraggingSeek = false;
  }

  public void UpdatePosition(double positionMs)
  {
    if (IsDraggingSeek)
    {
      SeekRequested?.Invoke(this, positionMs);
    }
  }

  private void Timer_OnTick(object? sender, EventArgs e)
  {
    // Position will be updated from the view when the MediaElement position changes
    // This is handled via property binding in the view
  }

  private void UpdatePlayPauseButton()
  {
    PlayPauseButtonContent = IsPlaying ? Constants.UI.PauseSymbol : Constants.UI.PlaySymbol;
  }

  private static bool TryBuildYouTubeEmbedUri(Uri source, out Uri? embedUri)
  {
    embedUri = null;

    if (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps)
    {
      return false;
    }

    string host = source.Host.ToLowerInvariant();
    bool isYouTube = host == "youtu.be" || host.EndsWith("youtube.com");
    if (!isYouTube)
    {
      return false;
    }

    string? videoId = ExtractYouTubeVideoId(source);
    if (string.IsNullOrWhiteSpace(videoId))
    {
      return false;
    }

    // Use the privacy-enhanced domain.
    // Do not autoplay: the user should explicitly start playback (consistent with local video UX).
    // Include an explicit origin/widget_referrer to help satisfy embedded player client identity requirements.
    // Note: Player behavior may still be affected by WebView2/Edge autoplay policies.
    const string appOrigin = "https://escapegamekiosk.local";
    string url = $"https://www.youtube-nocookie.com/embed/{videoId}?autoplay=0&controls=1&rel=0&modestbranding=1&playsinline=1&origin={Uri.EscapeDataString(appOrigin)}&widget_referrer={Uri.EscapeDataString(appOrigin)}";
    embedUri = new Uri(url);
    return true;
  }

  private static string? ExtractYouTubeVideoId(Uri uri)
  {
    // https://youtu.be/<id>
    if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
    {
      string path = uri.AbsolutePath.Trim('/');
      return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    // https://www.youtube.com/watch?v=<id>
    NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
    string? v = query["v"];
    if (!string.IsNullOrWhiteSpace(v))
    {
      return v;
    }

    // https://www.youtube.com/embed/<id>
    string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length >= 2 && segments[0].Equals("embed", StringComparison.OrdinalIgnoreCase))
    {
      return segments[1];
    }

    return null;
  }

  private static string FormatPosition(double milliseconds)
  {
    if (milliseconds <= 0)
    {
      return "0:00";
    }

    return FormatTime(milliseconds);
  }

  private static string FormatDuration(double milliseconds)
  {
    if (milliseconds <= 0)
    {
      return "--:--";
    }

    return FormatTime(milliseconds);
  }

  private static string FormatTime(double milliseconds)
  {
    TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);

    if (time.TotalHours >= 1)
    {
      return $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
    }

    return $"{time.Minutes}:{time.Seconds:00}";
  }
}
