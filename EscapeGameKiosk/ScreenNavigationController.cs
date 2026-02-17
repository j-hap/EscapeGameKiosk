using System.Collections.Generic;
using System.Windows.Controls;
using EscapeGameKiosk.Contracts;

namespace EscapeGameKiosk;

public sealed class ScreenNavigationController : IScreenNavigationController
{
  private readonly IScreenStackController _stack;
  private readonly PasswordScreenView _passwordScreen;
  private readonly ExitScreenView _exitScreen;
  private readonly IReadOnlyList<UserControl> _contentScreens;
  private int _contentIndex;

  public ScreenNavigationController(
      IScreenStackController stack,
      PasswordScreenView passwordScreen,
      ExitScreenView exitScreen,
      IReadOnlyList<UserControl> contentScreens)
  {
    _stack = stack;
    _passwordScreen = passwordScreen;
    _exitScreen = exitScreen;
    _contentScreens = contentScreens;
  }

  public UserControl? CurrentRoot => _stack.CurrentRoot;

  public void ShowPasswordScreen()
  {
    _stack.ClearOverlays();
    _stack.ShowRoot(_passwordScreen);
  }

  public void ShowFirstContentScreen()
  {
    _contentIndex = 0;
    ShowContentAt(_contentIndex);
  }

  public bool TryShowNextContentScreen()
  {
    if (_contentScreens.Count == 0)
    {
      return false;
    }

    if (_contentIndex + 1 >= _contentScreens.Count)
    {
      return false;
    }

    _contentIndex++;
    ShowContentAt(_contentIndex);
    return true;
  }

  public void ShowExitOverlay()
  {
    _stack.PushOverlay(_exitScreen);
  }

  public void DismissExitOverlay()
  {
    _stack.PopOverlay();
  }

  private void ShowContentAt(int index)
  {
    if (_contentScreens.Count == 0)
    {
      return;
    }

    if (index < 0 || index >= _contentScreens.Count)
    {
      return;
    }

    _stack.ClearOverlays();
    _stack.ShowRoot(_contentScreens[index]);
  }
}
