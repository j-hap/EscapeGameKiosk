using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EscapeGameKiosk.ViewModels;

namespace EscapeGameKiosk;

public partial class ExitScreenView : UserControl
{
  private readonly ExitViewModel? _viewModel;
  private bool _isUpdatingPassword;

  public ExitScreenView()
  {
    InitializeComponent();
  }

  // Constructor for DI with ViewModel
  public ExitScreenView(ExitViewModel viewModel) : this()
  {
    _viewModel = viewModel;
    DataContext = _viewModel;

    // Wire up PasswordBox synchronization (WPF limitation)
    ExitPasswordInputBox.PasswordChanged += ExitPasswordInputBox_OnPasswordChanged;
    ExitPasswordInputBox.KeyDown += ExitPasswordInputBox_OnKeyDown;
  }

  public Grid ExitScreen => ExitScreenRoot;
  public PasswordBox ExitPasswordInput => ExitPasswordInputBox;
  public Button ExitConfirmButton => ExitConfirmButtonControl;
  public Button ExitCancelButton => ExitCancelButtonControl;

  private void ExitPasswordInputBox_OnPasswordChanged(object sender, RoutedEventArgs e)
  {
    if (_isUpdatingPassword || _viewModel == null)
    {
      return;
    }

    _isUpdatingPassword = true;
    _viewModel.ExitPassword = ExitPasswordInputBox.Password;
    _isUpdatingPassword = false;
  }

  private void ExitPasswordInputBox_OnKeyDown(object sender, KeyEventArgs e)
  {
    if (e.Key == Key.Enter && _viewModel != null)
    {
      _viewModel.ConfirmExitCommand.Execute(null);
    }
  }

  public void ClearPassword()
  {
    ExitPasswordInputBox.Password = string.Empty;
    _viewModel?.ClearPassword();
  }
}
