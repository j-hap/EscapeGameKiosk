using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EscapeGameKiosk.Contracts;
using EscapeGameKiosk.Services;
using EscapeGameKiosk.State;
using EscapeGameKiosk.ViewModels;

namespace EscapeGameKiosk;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
  private ILogger<MainWindow>? _logger;
  private AppSettings? _settings;
  private IPasswordValidationService? _passwordService;
  private IExitSequenceService? _exitSequenceService;
  private IVideoSourceService? _videoSourceService;
  private IKeyboardSecurityService? _keyboardSecurityService;
  private IKioskStateManager? _kioskStateManager;
  private IScreenNavigationController? _navigationController;
  private PasswordScreenView? _passwordScreenView;
  private VideoScreenView? _videoScreenView;
  private ExitScreenView? _exitScreenView;
  private PasswordViewModel? _passwordViewModel;
  private VideoViewModel? _videoViewModel;
  private ExitViewModel? _exitViewModel;

  private bool _allowClose;

  // Parameterless constructor for XAML and factory pattern
  public MainWindow()
  {
    InitializeComponent();
    ApplyWindowMode();
  }

  // Initialize method called by DI factory
  public void Initialize(
    ILogger<MainWindow> logger,
    AppSettings settings,
    IPasswordValidationService passwordService,
    IExitSequenceService exitSequenceService,
    IVideoSourceService videoSourceService,
    IKeyboardSecurityService keyboardSecurityService,
    IKioskStateManager kioskStateManager,
    IScreenNavigationController navigationController,
    PasswordScreenView passwordScreenView,
    VideoScreenView videoScreenView,
    ExitScreenView exitScreenView,
    PasswordViewModel passwordViewModel,
    VideoViewModel videoViewModel,
    ExitViewModel exitViewModel)
  {
    _logger = logger;
    _settings = settings;
    _passwordService = passwordService;
    _exitSequenceService = exitSequenceService;
    _videoSourceService = videoSourceService;
    _keyboardSecurityService = keyboardSecurityService;
    _kioskStateManager = kioskStateManager;
    _navigationController = navigationController;
    _passwordScreenView = passwordScreenView;
    _videoScreenView = videoScreenView;
    _exitScreenView = exitScreenView;
    _passwordViewModel = passwordViewModel;
    _videoViewModel = videoViewModel;
    _exitViewModel = exitViewModel;

    _exitScreenView.Visibility = Visibility.Collapsed;

    _kioskStateManager.StateChanged += KioskStateManager_OnStateChanged;

    Loaded += OnLoaded;
    Closing += OnClosing;
    Deactivated += OnDeactivated;
    StateChanged += OnStateChanged;

    HookScreenEvents();
    HookViewModelEvents();

    // Drive initial UI via the state manager.
    _kioskStateManager.ForceTransitionTo(KioskState.PasswordEntry, "Startup");

    _logger.LogInformation("MainWindow initialized with dependency injection and MVVM");
  }

  private void KioskStateManager_OnStateChanged(object? sender, KioskStateChangedEventArgs e)
  {
    ApplyKioskState(e.NewState);
  }

  private void ApplyKioskState(KioskState state)
  {
    if (_navigationController == null)
    {
      return;
    }

    switch (state)
    {
      case KioskState.Initializing:
        return;

      case KioskState.PasswordEntry:
      case KioskState.Locked:
        _videoScreenView?.Stop();
        _navigationController.ShowPasswordScreen();
        _exitViewModel?.ClearPassword();
        _exitSequenceService?.Reset();
        _passwordViewModel?.ClearPassword();
        _passwordViewModel?.HideAccessDenied();
        // Focus can fail if called before the PasswordScreen is actually visible (or if the reveal TextBox is active).
        Dispatcher.BeginInvoke(
          new Action(() => _passwordScreenView?.FocusPasswordInput()),
          DispatcherPriority.Input);
        return;

      case KioskState.VideoPlayback:
        if (_videoScreenView == null || _videoViewModel == null)
        {
          return;
        }

        _navigationController.ShowFirstContentScreen();
        _videoScreenView.Start();
        LoadVideoThroughViewModel();
        _passwordViewModel?.ClearPassword();
        return;

      case KioskState.ExitConfirmation:
        if (_exitScreenView == null)
        {
          return;
        }

        _exitViewModel?.ClearPassword();
        _navigationController.ShowExitOverlay();
        _exitScreenView.ExitPasswordInput.Focus();
        return;

      default:
        return;
    }
  }

  private void HookScreenEvents()
  {
    if (_passwordScreenView == null)
      return;

    _passwordScreenView.PasswordScreen.PreviewMouseLeftButtonDown += PasswordScreen_OnPreviewMouseLeftButtonDown;
  }

  private void HookViewModelEvents()
  {
    if (_passwordViewModel != null)
    {
      _passwordViewModel.UnlockRequested += PasswordViewModel_OnUnlockRequested;
    }

    if (_videoViewModel != null)
    {
      _videoViewModel.LockRequested += VideoViewModel_OnLockRequested;
    }

    if (_exitViewModel != null)
    {
      _exitViewModel.ExitConfirmed += ExitViewModel_OnExitConfirmed;
      _exitViewModel.ExitCancelled += ExitViewModel_OnExitCancelled;
    }
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    if (_passwordScreenView == null || _settings == null)
      return;

    // Validate video path exists, but don't load it yet
    if (!ValidateVideoPath())
    {
      MessageBox.Show(
          "Video source is missing or not configured. The application will now exit.",
          "EscapeGameKiosk",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      _allowClose = true;
      Close();
      return;
    }
    _passwordScreenView.PasswordInput.Focus();

    if (_settings.AllowKeyboardHook && _keyboardSecurityService != null)
    {
      _keyboardSecurityService.Start();
    }
  }

  private void ApplyWindowMode()
  {
#if DEBUG
    WindowStyle = WindowStyle.SingleBorderWindow;
    ResizeMode = ResizeMode.CanResize;
    WindowState = WindowState.Normal;
    Topmost = false;
    _allowClose = true;
#else
    WindowStyle = WindowStyle.None;
    ResizeMode = ResizeMode.NoResize;
    WindowState = WindowState.Maximized;
    Topmost = true;
#endif
  }

  private bool ValidateVideoPath()
  {
    if (_videoSourceService == null || _settings == null || _logger == null)
      return false;

    if (_videoSourceService.TryResolveVideoSource(_settings.VideoPath, out Uri? videoUri))
    {
      _logger.LogInformation("Video path validated: {VideoPath}", videoUri);
      return true;
    }

    _logger.LogError("Video path validation failed: {VideoPath}", _settings.VideoPath);
    return false;
  }

  private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
  {
    if (!_allowClose)
    {
      e.Cancel = true;
      return;
    }

    _keyboardSecurityService?.Dispose();
  }

  private void OnDeactivated(object? sender, EventArgs e)
  {
    if (!_allowClose)
    {
      Activate();
    }
  }

  private void OnStateChanged(object? sender, EventArgs e)
  {
    if (!_allowClose && WindowState == WindowState.Minimized)
    {
      WindowState = WindowState.Maximized;
    }
  }

  // ViewModel event handlers
  private void PasswordViewModel_OnUnlockRequested(object? sender, EventArgs e)
  {
    _kioskStateManager?.TryTransitionTo(KioskState.VideoPlayback, "UnlockRequested", out _);
  }

  private void VideoViewModel_OnLockRequested(object? sender, EventArgs e)
  {
    _kioskStateManager?.TryTransitionTo(KioskState.PasswordEntry, "LockRequested", out _);
  }

  private void ExitViewModel_OnExitConfirmed(object? sender, EventArgs e)
  {
    _allowClose = true;
    Close();
  }

  private void ExitViewModel_OnExitCancelled(object? sender, EventArgs e)
  {
    if (_kioskStateManager == null)
    {
      return;
    }

    _kioskStateManager.TryRollback("ExitCancelled", out _);
    _navigationController?.DismissExitOverlay();
    _exitViewModel?.ClearPassword();
    _exitSequenceService?.Reset();
    _logger?.LogInformation("Exit screen cancelled. Progress reset.");
  }

  private void LoadVideoThroughViewModel()
  {
    if (_videoSourceService == null || _settings == null || _videoViewModel == null || _logger == null)
      return;

    if (_videoSourceService.TryResolveVideoSource(_settings.VideoPath, out Uri? videoUri))
    {
      _videoViewModel.LoadVideo(videoUri!);  // ✓ Talk to ViewModel, not View
      _logger.LogInformation("Video loaded through ViewModel: {VideoPath}", videoUri);
    }
    else
    {
      _logger.LogError("Failed to resolve video source: {VideoPath}", _settings.VideoPath);
    }
  }


  private void PasswordScreen_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
  {
    if (_navigationController == null || _passwordScreenView == null || _exitSequenceService == null || _logger == null)
      return;

    if (_navigationController.CurrentRoot != _passwordScreenView)
    {
      return;
    }

    DependencyObject? source = e.OriginalSource as DependencyObject;
    if (source != null && IsDescendantOf(source, _passwordScreenView.PasswordPanel))
    {
      return;
    }

    Point pos = e.GetPosition(_passwordScreenView.PasswordScreen);
    int region = GetCornerRegion(pos, _passwordScreenView.PasswordScreen.ActualWidth, _passwordScreenView.PasswordScreen.ActualHeight, Constants.Security.CornerSizePixels);

    _logger.LogDebug("Exit tap at ({X:0},{Y:0}) -> region {Region} (progress {Progress}/{Total})",
      pos.X, pos.Y, region, _exitSequenceService.CurrentProgress, _exitSequenceService.SequenceLength);

    if (region < 0)
    {
      return;
    }
    RegisterExitTap(region);
  }

  private void RegisterExitTap(int region)
  {
    if (_exitSequenceService == null || _logger == null)
      return;

    bool sequenceComplete = _exitSequenceService.RegisterTap(region);

    _logger.LogDebug("Exit sequence progress: {Progress}/{Total}",
      _exitSequenceService.CurrentProgress, _exitSequenceService.SequenceLength);

    if (sequenceComplete)
    {
      _logger.LogInformation("Exit sequence complete. Prompting for exit password.");
      _kioskStateManager?.TryTransitionTo(KioskState.ExitConfirmation, "ExitSequenceComplete", out _);
    }
  }

  private static int GetCornerRegion(Point pos, double width, double height, double cornerSize)
  {
    bool left = pos.X <= cornerSize;
    bool right = pos.X >= width - cornerSize;
    bool top = pos.Y <= cornerSize;
    bool bottom = pos.Y >= height - cornerSize;

    if (left && top)
    {
      return 0;
    }

    if (right && top)
    {
      return 1;
    }

    if (right && bottom)
    {
      return 2;
    }

    if (left && bottom)
    {
      return 3;
    }

    return -1;
  }

  private static bool IsDescendantOf(DependencyObject child, DependencyObject parent)
  {
    DependencyObject? current = child;
    while (current != null)
    {
      if (current == parent)
      {
        return true;
      }

      current = VisualTreeHelper.GetParent(current);
    }

    return false;
  }
}
