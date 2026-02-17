using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using EscapeGameKiosk.Contracts;

namespace EscapeGameKiosk;

public sealed class ScreenStackController : IScreenStackController
{
  private readonly ContentControl _rootHost;
  private readonly Panel _overlayHost;
  private readonly List<UserControl> _overlayStack = new();

  public ScreenStackController(ContentControl rootHost, Panel overlayHost)
  {
    _rootHost = rootHost;
    _overlayHost = overlayHost;
  }

  public UserControl? CurrentRoot { get; private set; }

  public void ShowRoot(UserControl root)
  {
    if (ReferenceEquals(CurrentRoot, root))
    {
      return;
    }

    _rootHost.Content = root;
    CurrentRoot = root;
  }

  public void PushOverlay(UserControl overlay)
  {
    if (_overlayStack.Contains(overlay))
    {
      _overlayStack.Remove(overlay);
      _overlayHost.Children.Remove(overlay);
    }

    _overlayStack.Add(overlay);
    _overlayHost.Children.Add(overlay);
    Panel.SetZIndex(overlay, 1000 + _overlayStack.Count);
    overlay.Visibility = Visibility.Visible;
    UpdateOverlayHitTesting();
  }

  public void PopOverlay()
  {
    if (_overlayStack.Count == 0)
    {
      return;
    }

    UserControl overlay = _overlayStack[^1];
    _overlayStack.RemoveAt(_overlayStack.Count - 1);
    _overlayHost.Children.Remove(overlay);
    overlay.Visibility = Visibility.Collapsed;
    UpdateOverlayHitTesting();
  }

  public void ClearOverlays()
  {
    foreach (UserControl overlay in _overlayStack)
    {
      _overlayHost.Children.Remove(overlay);
      overlay.Visibility = Visibility.Collapsed;
    }

    _overlayStack.Clear();
    UpdateOverlayHitTesting();
  }

  private void UpdateOverlayHitTesting()
  {
    _overlayHost.IsHitTestVisible = _overlayStack.Count > 0;
  }
}
