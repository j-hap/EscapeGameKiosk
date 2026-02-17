using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EscapeGameKiosk.ViewModels;
using Microsoft.Web.WebView2.Core;

namespace EscapeGameKiosk;

public partial class VideoScreenView : UserControl
{
  private readonly VideoViewModel? _viewModel;
  private readonly DispatcherTimer _positionTimer;
  private bool _preloadRequested;
  private bool _preloadMuted;
  private bool _webView2RuntimeMissingNotified;
  private bool _webView2Configured;

  // Workaround for YouTube embed Error 153 in WebView2.
  private const string YouTubeReferer = "https://escapegamekiosk.local";

  public VideoScreenView()
  {
    InitializeComponent();

    _positionTimer = new DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(250)
    };
    _positionTimer.Tick += PositionTimer_OnTick;

    // WebView2 is an HWND control; keep it simple and avoid popups that interrupt kiosk flow.
  }

  // Constructor for DI with ViewModel
  public VideoScreenView(VideoViewModel viewModel) : this()
  {
    _viewModel = viewModel;
    DataContext = _viewModel;

    // Subscribe to ViewModel events
    if (_viewModel != null)
    {
      _viewModel.PlayRequested += OnPlayRequested;
      _viewModel.PauseRequested += OnPauseRequested;
      _viewModel.StopRequested += OnStopRequested;
      _viewModel.SeekRequested += OnSeekRequested;

      // Subscribe to property changes to detect video source changes
      _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    // Wire up MediaElement events
    VideoPlayerElement.MediaOpened += VideoPlayer_OnMediaOpened;
    VideoPlayerElement.MediaEnded += VideoPlayer_OnMediaEnded;
    VideoPlayerElement.MediaFailed += VideoPlayer_OnMediaFailed;

    // Wire up slider events for seeking
    SeekToSlider.PreviewMouseLeftButtonDown += SeekToSlider_OnPreviewMouseLeftButtonDown;
    SeekToSlider.PreviewMouseLeftButtonUp += SeekToSlider_OnPreviewMouseLeftButtonUp;
    SeekToSlider.ValueChanged += SeekToSlider_OnValueChanged;

    // Wire up volume and speed changes to MediaElement
    VolumeSlider.ValueChanged += VolumeSlider_OnValueChanged;
    SpeedRatioComboBox.SelectionChanged += SpeedRatioComboBox_OnSelectionChanged;
  }

  public Grid VideoScreen => VideoScreenRoot;
  public MediaElement VideoPlayer => VideoPlayerElement;
  public Button PlayPauseButton => PlayPauseButtonControl;
  public Button StopButton => StopButtonControl;
  public Slider Volume => VolumeSlider;
  public ComboBox SpeedRatio => SpeedRatioComboBox;
  public Slider SeekTo => SeekToSlider;
  public Button LockButton => LockButtonControl;

  private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    if (_viewModel == null)
    {
      return;
    }

    if (e.PropertyName == nameof(VideoViewModel.WebVideoUri))
    {
      if (_viewModel.WebVideoUri != null)
      {
        _ = LoadWebVideoAsync(_viewModel.WebVideoUri);
      }
      else
      {
        _viewModel.IsWebVideoLoading = false;
        // WebView2.Source cannot be set to null - navigate to blank page instead
        if (WebVideoElement.CoreWebView2 != null)
        {
          WebVideoElement.CoreWebView2.Navigate("about:blank");
        }
      }

      return;
    }

    if (e.PropertyName == nameof(VideoViewModel.VideoSource) && _viewModel.VideoSource != null)
    {
      // ViewModel's VideoSource changed → Update MediaElement
      VideoPlayerElement.Source = _viewModel.VideoSource;
      VideoPlayerElement.Position = TimeSpan.Zero;

      // Preload first frame + duration before the user presses Play.
      // With LoadedBehavior=Manual, MediaOpened often won't fire until Play() is called.
      _preloadRequested = true;
      _preloadMuted = true;
      VideoPlayerElement.IsMuted = true;
      VideoPlayerElement.Play();
      System.Diagnostics.Debug.WriteLine($"[VideoScreenView] VideoSource property changed, applied to MediaElement: {_viewModel.VideoSource}");
    }
  }

  private async System.Threading.Tasks.Task LoadWebVideoAsync(Uri uri)
  {
    try
    {
      _viewModel!.IsWebVideoLoading = true;
      await WebVideoElement.EnsureCoreWebView2Async();
      EnsureWebView2Configured();

      // Explicitly set Referer for the main document request.
      // This aligns with YouTube's required minimum functionality guidance for WebViews/desktop apps.
      if (WebVideoElement.CoreWebView2 != null)
      {
        var request = WebVideoElement.CoreWebView2.Environment.CreateWebResourceRequest(
          uri.ToString(),
          "GET",
          null,
          $"Referer: {YouTubeReferer}\r\n");

        WebVideoElement.CoreWebView2.NavigateWithWebResourceRequest(request);
        System.Diagnostics.Debug.WriteLine($"[VideoScreenView] Web video navigated with Referer header: {uri}");
      }
    }
    catch (WebView2RuntimeNotFoundException ex)
    {
      System.Diagnostics.Debug.WriteLine($"[VideoScreenView] WebView2 runtime missing: {ex.Message}");
      _viewModel!.IsWebVideoLoading = false;
      NotifyWebView2RuntimeMissing();
    }
    catch (Exception ex)
    {
      // Catch-all: other WebView2 initialization issues (corrupt runtime, policy blocks, etc.).
      System.Diagnostics.Debug.WriteLine($"[VideoScreenView] WebView2 init/navigation failed: {ex.Message}");
      _viewModel!.IsWebVideoLoading = false;
      NotifyWebView2RuntimeMissing();
    }
  }

  private void EnsureWebView2Configured()
  {
    if (_webView2Configured)
    {
      return;
    }

    if (WebVideoElement.CoreWebView2 == null)
    {
      return;
    }

    _webView2Configured = true;

    WebVideoElement.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
    WebVideoElement.CoreWebView2.WebResourceRequested += WebView2_OnWebResourceRequested;

    WebVideoElement.CoreWebView2.NavigationStarting += WebView2_OnNavigationStarting;
    WebVideoElement.CoreWebView2.ContentLoading += WebView2_OnContentLoading;
    WebVideoElement.CoreWebView2.DOMContentLoaded += WebView2_OnDomContentLoaded;
    WebVideoElement.CoreWebView2.NavigationCompleted += WebView2_OnNavigationCompleted;
  }

  private void WebView2_OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
  {
    if (_viewModel?.IsWebVideo == true)
    {
      _viewModel.IsWebVideoLoading = true;
    }
  }

  private void WebView2_OnContentLoading(object? sender, CoreWebView2ContentLoadingEventArgs e)
  {
    if (_viewModel?.IsWebVideo == true)
    {
      _viewModel.IsWebVideoLoading = true;
    }
  }

  private void WebView2_OnDomContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
  {
    if (_viewModel?.IsWebVideo == true)
    {
      _viewModel.IsWebVideoLoading = false;
    }
  }

  private void WebView2_OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
  {
    if (_viewModel?.IsWebVideo == true)
    {
      // Even on failure, hide the loading overlay so the user isn't stuck on a spinner.
      _viewModel.IsWebVideoLoading = false;
    }
  }

  private void WebView2_OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
  {
    try
    {
      // Only touch YouTube-related requests.
      if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out Uri? uri))
      {
        return;
      }

      string host = uri.Host.ToLowerInvariant();
      bool isYouTubeRelated = host.Contains("youtube.com") || host.Contains("youtube-nocookie.com") || host.Contains("googlevideo.com") || host.Contains("ytimg.com");
      if (!isYouTubeRelated)
      {
        return;
      }

      e.Request.Headers.SetHeader("Referer", YouTubeReferer);
    }
    catch
    {
      // Ignore header modification errors.
    }
  }

  private static string BuildYouTubeEmbedHtml(Uri embedUri)
  {
    // The meta referrer policy is a key part of the workaround from #5418.
    // Use a minimal HTML wrapper with a full-viewport iframe.
    string url = embedUri.ToString();

    string encodedUrl = System.Net.WebUtility.HtmlEncode(url);

    return $@"<!doctype html>
<html>
  <head>
    <meta charset='utf-8'>
    <meta name='referrer' content='strict-origin-when-cross-origin'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
      html, body {{ margin: 0; padding: 0; background: #000; height: 100%; overflow: hidden; }}
      iframe {{ border: 0; width: 100%; height: 100%; }}
    </style>
  </head>
  <body>
    <iframe
      src='{encodedUrl}'
      allow='autoplay; encrypted-media; fullscreen'
      allowfullscreen
      referrerpolicy='strict-origin-when-cross-origin'></iframe>
  </body>
</html>";
  }

  private void NotifyWebView2RuntimeMissing()
  {
    if (_webView2RuntimeMissingNotified)
    {
      return;
    }

    _webView2RuntimeMissingNotified = true;

    Dispatcher.Invoke(() =>
    {
      MessageBox.Show(
        "This configuration uses a YouTube link, but the Microsoft Edge WebView2 Runtime is not available on this machine.\n\n" +
        "To play YouTube videos, install the 'Microsoft Edge WebView2 Runtime' (Evergreen).\n\n" +
        "Alternatively, change AppSettings:VideoPath to a local video file.",
        "WebView2 Runtime Missing",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    });
  }

  public void SetVideoSource(Uri source)
  {
    _viewModel?.SetVideoSource(source);
    VideoPlayerElement.Source = source;
    System.Diagnostics.Debug.WriteLine($"[VideoScreenView] Video source set to: {source}");
  }

  public void Start()
  {
    _viewModel?.Start();
    _positionTimer.Start();
  }

  public void Stop()
  {
    _positionTimer.Stop();
    _viewModel?.StopPlayback();
  }

  private void PositionTimer_OnTick(object? sender, EventArgs e)
  {
    if (_viewModel == null)
    {
      return;
    }

    // Web video playback is owned by the embedded player.
    if (_viewModel.IsWebVideo)
    {
      return;
    }

    if (_viewModel.IsDraggingSeek)
    {
      return;
    }

    // Keep ViewModel.Position synced to actual playback time.
    if (VideoPlayerElement.Source != null)
    {
      _viewModel.Position = VideoPlayerElement.Position.TotalMilliseconds;
    }

    // If duration wasn't available at MediaOpened time, try to populate it later.
    if (_viewModel.Duration <= 0 && VideoPlayerElement.NaturalDuration.HasTimeSpan)
    {
      _viewModel.Duration = VideoPlayerElement.NaturalDuration.TimeSpan.TotalMilliseconds;
    }
  }

  // ViewModel event handlers - Execute MediaElement operations
  private void OnPlayRequested(object? sender, EventArgs e)
  {
    if (VideoPlayerElement.Source != null)
    {
      VideoPlayerElement.Play();
    }
  }

  private void OnPauseRequested(object? sender, EventArgs e)
  {
    VideoPlayerElement.Pause();
  }

  private void OnStopRequested(object? sender, EventArgs e)
  {
    VideoPlayerElement.Stop();
    VideoPlayerElement.Position = TimeSpan.Zero;
  }

  private void OnSeekRequested(object? sender, double positionMs)
  {
    VideoPlayerElement.Position = TimeSpan.FromMilliseconds(positionMs);
  }

  // MediaElement event handlers - Update ViewModel
  private void VideoPlayer_OnMediaOpened(object sender, RoutedEventArgs e)
  {
    double durationMs = 0;
    if (VideoPlayerElement.NaturalDuration.HasTimeSpan)
    {
      durationMs = VideoPlayerElement.NaturalDuration.TimeSpan.TotalMilliseconds;
    }

    // Mark as loaded even if duration isn't available yet.
    _viewModel?.OnMediaOpened(durationMs);

    // Sync initial values to MediaElement
    VideoPlayerElement.Volume = VolumeSlider.Value;
    VideoPlayerElement.SpeedRatio = GetSelectedSpeedRatio();

    // If we were preloading, pause at the first frame and unmute.
    if (_preloadRequested)
    {
      _preloadRequested = false;
      VideoPlayerElement.Position = TimeSpan.Zero;
      VideoPlayerElement.Pause();

      if (_preloadMuted)
      {
        _preloadMuted = false;
        VideoPlayerElement.IsMuted = false;
      }
    }

    System.Diagnostics.Debug.WriteLine(
      VideoPlayerElement.NaturalDuration.HasTimeSpan
        ? $"[VideoScreenView] MediaOpened fired - Duration: {durationMs}ms"
        : "[VideoScreenView] MediaOpened fired - Duration not available");
  }

  private void VideoPlayer_OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
  {
    System.Diagnostics.Debug.WriteLine($"[VideoScreenView] MediaFailed: {e.ErrorException?.Message}");

    // If preload failed, ensure we don't leave the player muted.
    _preloadRequested = false;
    if (_preloadMuted)
    {
      _preloadMuted = false;
      VideoPlayerElement.IsMuted = false;
    }
  }

  private void VideoPlayer_OnMediaEnded(object sender, RoutedEventArgs e)
  {
    _viewModel?.OnMediaEnded();
  }

  // Seeking logic - Update ViewModel
  private void SeekToSlider_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
  {
    _viewModel?.StartSeek();
  }

  private void SeekToSlider_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
  {
    _viewModel?.EndSeek(SeekToSlider.Value);
  }

  private void SeekToSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
  {
    // Only drive seeking while the user is dragging the thumb.
    if (_viewModel != null && _viewModel.IsDraggingSeek)
    {
      _viewModel.UpdatePosition(SeekToSlider.Value);
    }
  }

  // Volume and Speed changes - Apply to MediaElement
  private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
  {
    if (VideoPlayerElement != null)
    {
      VideoPlayerElement.Volume = VolumeSlider.Value;
    }
  }

  private void SpeedRatioComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (VideoPlayerElement != null)
    {
      VideoPlayerElement.SpeedRatio = GetSelectedSpeedRatio();
    }
  }

  private double GetSelectedSpeedRatio()
  {
    if (SpeedRatioComboBox.SelectedItem is ComboBoxItem item
        && double.TryParse(item.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio))
    {
      return ratio;
    }

    return 1.0;
  }
}
