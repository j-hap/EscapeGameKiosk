using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.ViewModels;

public partial class MainViewModel : ObservableObject
{
  private readonly ILogger<MainViewModel> _logger;

  [ObservableProperty]
  private bool _isPasswordScreenVisible = true;

  [ObservableProperty]
  private bool _isVideoScreenVisible;

  [ObservableProperty]
  private bool _isExitScreenVisible;

  [ObservableProperty]
  private bool _allowClose;

  public MainViewModel(ILogger<MainViewModel> logger)
  {
    _logger = logger;
  }

  public void ShowPasswordScreen()
  {
    IsPasswordScreenVisible = true;
    IsVideoScreenVisible = false;
    IsExitScreenVisible = false;
    _logger.LogDebug("Showing password screen");
  }

  public void ShowVideoScreen()
  {
    IsPasswordScreenVisible = false;
    IsVideoScreenVisible = true;
    IsExitScreenVisible = false;
    _logger.LogDebug("Showing video screen");
  }

  public void ShowExitScreen()
  {
    IsExitScreenVisible = true;
    _logger.LogDebug("Showing exit screen overlay");
  }

  public void HideExitScreen()
  {
    IsExitScreenVisible = false;
    _logger.LogDebug("Hiding exit screen overlay");
  }

  public void RequestClose()
  {
    AllowClose = true;
    _logger.LogInformation("Application close allowed");
  }
}
