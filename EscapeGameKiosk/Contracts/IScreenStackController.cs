using System.Windows.Controls;

namespace EscapeGameKiosk.Contracts;

/// <summary>
/// Interface for managing the screen stack (root content and overlays).
/// </summary>
public interface IScreenStackController
{
  /// <summary>
  /// Gets the current root screen.
  /// </summary>
  UserControl? CurrentRoot { get; }

  /// <summary>
  /// Shows a screen as the root content.
  /// </summary>
  /// <param name="root">The screen to show as root.</param>
  void ShowRoot(UserControl root);

  /// <summary>
  /// Pushes an overlay screen on top of the current content.
  /// </summary>
  /// <param name="overlay">The overlay screen to show.</param>
  void PushOverlay(UserControl overlay);

  /// <summary>
  /// Pops the topmost overlay from the stack.
  /// </summary>
  void PopOverlay();

  /// <summary>
  /// Clears all overlays from the stack.
  /// </summary>
  void ClearOverlays();
}
