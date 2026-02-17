using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using EscapeGameKiosk.ViewModels;

namespace EscapeGameKiosk;

public partial class PasswordScreenView : UserControl
{
  private readonly PasswordViewModel? _viewModel;
  private bool _isUpdatingPassword;

  public PasswordScreenView()
  {
    InitializeComponent();
  }

  // Constructor for DI with ViewModel
  public PasswordScreenView(PasswordViewModel viewModel) : this()
  {
    _viewModel = viewModel;
    DataContext = _viewModel;

    // Subscribe to ViewModel events
    _viewModel.TriggerWiggleAnimation += OnTriggerWiggleAnimation;
    _viewModel.PasswordCleared += OnPasswordCleared;

    // Wire up event handlers for password reveal functionality
    PasswordInputBox.PasswordChanged += PasswordInputBox_OnPasswordChanged;
    PasswordInputBox.KeyDown += PasswordInputBox_OnKeyDown;
    PasswordRevealTextBox.TextChanged += PasswordRevealTextBox_OnTextChanged;
    PasswordRevealTextBox.KeyDown += PasswordRevealTextBox_OnKeyDown;
    PasswordRevealToggle.Checked += PasswordRevealToggle_OnChecked;
    PasswordRevealToggle.Unchecked += PasswordRevealToggle_OnUnchecked;
  }

  public Grid PasswordScreen => PasswordScreenRoot;
  public Border PasswordPanel => PasswordPanelBorder;
  public PasswordBox PasswordInput => PasswordInputBox;
  public Button UnlockButton => UnlockButtonControl;
  public bool IsAccessDeniedActive => _viewModel?.IsAccessDeniedActive ?? false;

  public void FocusPasswordInput()
  {
    // Focus whichever input is currently visible (PasswordBox vs reveal TextBox).
    if (PasswordRevealTextBox.Visibility == Visibility.Visible)
    {
      PasswordRevealTextBox.Focus();
      PasswordRevealTextBox.SelectionStart = PasswordRevealTextBox.Text.Length;
      return;
    }

    PasswordInputBox.Focus();
    PasswordInputBox.SelectAll();
  }

  public void TriggerPasswordWiggle()
  {
    OnTriggerWiggleAnimation(this, EventArgs.Empty);
  }

  public void ShowAccessDenied(int timeoutSeconds)
  {
    _viewModel?.ShowAccessDenied(timeoutSeconds);
  }

  public void HideAccessDenied()
  {
    _viewModel?.HideAccessDenied();
  }

  private void OnPasswordCleared(object? sender, EventArgs e)
  {
    // ViewModel notified us to clear UI
    PasswordInputBox.Password = string.Empty;
    PasswordRevealTextBox.Text = string.Empty;
  }

  private void OnTriggerWiggleAnimation(object? sender, EventArgs e)
  {
    if (PasswordPanelBorder.RenderTransform is not TranslateTransform transform)
    {
      transform = new TranslateTransform();
      PasswordPanelBorder.RenderTransform = transform;
    }

    var animation = new DoubleAnimationUsingKeyFrames
    {
      Duration = TimeSpan.FromMilliseconds(240)
    };
    animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
    animation.KeyFrames.Add(new EasingDoubleKeyFrame(-6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(40))));
    animation.KeyFrames.Add(new EasingDoubleKeyFrame(6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
    animation.KeyFrames.Add(new EasingDoubleKeyFrame(-6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
    animation.KeyFrames.Add(new EasingDoubleKeyFrame(6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
    animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));

    transform.BeginAnimation(TranslateTransform.XProperty, animation);
  }

  private void PasswordInputBox_OnPasswordChanged(object sender, RoutedEventArgs e)
  {
    if (_isUpdatingPassword || _viewModel == null)
    {
      return;
    }

    _isUpdatingPassword = true;
    _viewModel.Password = PasswordInputBox.Password;
    PasswordRevealTextBox.Text = PasswordInputBox.Password;
    _isUpdatingPassword = false;
  }

  private void PasswordInputBox_OnKeyDown(object sender, KeyEventArgs e)
  {
    if (e.Key == Key.Enter && _viewModel != null)
    {
      _viewModel.UnlockCommand.Execute(null);
    }
  }

  private void PasswordRevealTextBox_OnKeyDown(object sender, KeyEventArgs e)
  {
    if (e.Key == Key.Enter && _viewModel != null)
    {
      _viewModel.UnlockCommand.Execute(null);
    }
  }

  private void PasswordRevealTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
  {
    if (_isUpdatingPassword || PasswordRevealTextBox.Visibility != Visibility.Visible || _viewModel == null)
    {
      return;
    }

    _isUpdatingPassword = true;
    PasswordInputBox.Password = PasswordRevealTextBox.Text;
    _viewModel.RevealedPassword = PasswordRevealTextBox.Text;
    _isUpdatingPassword = false;
  }

  private void PasswordRevealToggle_OnChecked(object sender, RoutedEventArgs e)
  {
    PasswordRevealTextBox.Text = PasswordInputBox.Password;
    PasswordRevealTextBox.Visibility = Visibility.Visible;
    PasswordInputBox.Visibility = Visibility.Collapsed;
    PasswordRevealTextBox.Focus();
    PasswordRevealTextBox.SelectionStart = PasswordRevealTextBox.Text.Length;

    if (_viewModel != null)
    {
      _viewModel.IsPasswordRevealed = true;
      // Sync ViewModel with UI when revealing
      _viewModel.RevealedPassword = PasswordRevealTextBox.Text;
    }
  }

  private void PasswordRevealToggle_OnUnchecked(object sender, RoutedEventArgs e)
  {
    PasswordInputBox.Password = PasswordRevealTextBox.Text;
    PasswordRevealTextBox.Visibility = Visibility.Collapsed;
    PasswordInputBox.Visibility = Visibility.Visible;
    PasswordInputBox.Focus();
    PasswordInputBox.SelectAll();

    if (_viewModel != null)
    {
      _viewModel.IsPasswordRevealed = false;
    }
  }
}
