using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using EscapeGameKiosk.Services;

namespace EscapeGameKiosk.ViewModels;

public partial class ExitViewModel : ObservableObject
{
  private readonly ILogger<ExitViewModel> _logger;
  private readonly IPasswordValidationService _passwordService;
  private readonly AppSettings _settings;

  [ObservableProperty]
  private string _exitPassword = string.Empty;

  public ExitViewModel(
    ILogger<ExitViewModel> logger,
    IPasswordValidationService passwordService,
    AppSettings settings)
  {
    _logger = logger;
    _passwordService = passwordService;
    _settings = settings;
  }

  public event EventHandler? ExitConfirmed;
  public event EventHandler? ExitCancelled;

  [RelayCommand]
  private void ConfirmExit()
  {
    if (_passwordService.ValidatePassword(ExitPassword, _settings.Password))
    {
      _logger.LogInformation("Exit password confirmed. Closing application.");
      ExitConfirmed?.Invoke(this, EventArgs.Empty);
      return;
    }

    _logger.LogWarning("Invalid exit password attempt");
    ExitPassword = string.Empty;
  }

  [RelayCommand]
  private void CancelExit()
  {
    _logger.LogInformation("Exit cancelled by user");
    ExitPassword = string.Empty;
    ExitCancelled?.Invoke(this, EventArgs.Empty);
  }

  public void ClearPassword()
  {
    ExitPassword = string.Empty;
  }
}
