using System;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using EscapeGameKiosk.Services;

namespace EscapeGameKiosk.ViewModels;

public partial class PasswordViewModel : ObservableObject
{
  private readonly ILogger<PasswordViewModel> _logger;
  private readonly IPasswordValidationService _passwordService;
  private readonly AppSettings _settings;
  private readonly System.Windows.Threading.DispatcherTimer _errorTimer;
  private readonly Brush _accessRequiredBrush;
  private readonly Brush _accessDeniedBrush;

  [ObservableProperty]
  private string _password = string.Empty;

  [ObservableProperty]
  private string _revealedPassword = string.Empty;

  [ObservableProperty]
  private string _headerText = Constants.UI.AccessRequiredText;

  [ObservableProperty]
  private Brush _headerBrush;

  [ObservableProperty]
  private bool _isUnlockButtonEnabled = true;

  [ObservableProperty]
  private bool _isAccessDeniedActive;

  [ObservableProperty]
  private bool _isPasswordRevealed;

  [ObservableProperty]
  private int _remainingSeconds;

  public PasswordViewModel(
    ILogger<PasswordViewModel> logger,
    IPasswordValidationService passwordService,
    AppSettings settings)
  {
    _logger = logger;
    _passwordService = passwordService;
    _settings = settings;

    _accessRequiredBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6));
    _accessDeniedBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
    _headerBrush = _accessRequiredBrush;

    _errorTimer = new System.Windows.Threading.DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(Constants.Timers.LockoutCountdownIntervalMs)
    };
    _errorTimer.Tick += ErrorTimer_OnTick;
  }

  public event EventHandler? UnlockRequested;
  public event EventHandler? TriggerWiggleAnimation;
  public event EventHandler? PasswordCleared;  // New event for UI cleanup

  [RelayCommand]
  private void Unlock()
  {
    if (IsAccessDeniedActive)
    {
      return;
    }

    if (_passwordService.ValidatePassword(Password, _settings.Password))
    {
      _logger.LogInformation("Password validated successfully");
      _passwordService.ResetFailedAttempts();
      HideAccessDenied();
      UnlockRequested?.Invoke(this, EventArgs.Empty);
      ClearPassword();
      return;
    }

    int lockoutSeconds = _passwordService.RegisterFailedAttempt();
    _logger.LogWarning("Failed password attempt #{Count}. Lockout: {Seconds}s",
      _passwordService.FailedAttemptCount, lockoutSeconds);

    TriggerWiggleAnimation?.Invoke(this, EventArgs.Empty);
    ShowAccessDenied(lockoutSeconds);
  }

  public void ShowAccessDenied(int timeoutSeconds)
  {
    RemainingSeconds = Math.Max(1, timeoutSeconds);
    IsAccessDeniedActive = true;
    HeaderText = GetCountdownText(RemainingSeconds);
    HeaderBrush = _accessDeniedBrush;
    IsUnlockButtonEnabled = false;
    _errorTimer.Stop();
    _errorTimer.Start();
  }

  public void HideAccessDenied()
  {
    _errorTimer.Stop();
    IsAccessDeniedActive = false;
    HeaderText = Constants.UI.AccessRequiredText;
    HeaderBrush = _accessRequiredBrush;
    IsUnlockButtonEnabled = true;
  }

  public void ClearPassword()
  {
    Password = string.Empty;
    RevealedPassword = string.Empty;
    PasswordCleared?.Invoke(this, EventArgs.Empty);  // Notify View to clear UI
  }

  private void ErrorTimer_OnTick(object? sender, EventArgs e)
  {
    RemainingSeconds--;
    if (RemainingSeconds <= 0)
    {
      HideAccessDenied();
      return;
    }

    HeaderText = GetCountdownText(RemainingSeconds);
  }

  private static string GetCountdownText(int remainingSeconds)
  {
    return $"{Constants.UI.AccessDeniedText} – {remainingSeconds}s";
  }

  partial void OnPasswordChanged(string value)
  {
    if (!IsPasswordRevealed)
    {
      RevealedPassword = value;
    }
  }

  partial void OnRevealedPasswordChanged(string value)
  {
    if (IsPasswordRevealed)
    {
      Password = value;
    }
  }
}
