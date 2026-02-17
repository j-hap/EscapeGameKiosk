using System.Windows.Controls;

namespace EscapeGameKiosk.Contracts;

/// <summary>
/// Interface for high-level screen navigation.
/// </summary>
public interface IScreenNavigationController
{
  /// <summary>
  /// Gets the current root screen being displayed.
  /// </summary>
  UserControl? CurrentRoot { get; }

  /// <summary>
  /// Shows the password entry screen.
  /// </summary>
  void ShowPasswordScreen();

  /// <summary>
  /// Shows the first content screen (e.g., video).
  /// </summary>
  void ShowFirstContentScreen();

  /// <summary>
  /// Attempts to show the next content screen in sequence.
  /// </summary>
  /// <returns>True if there was a next screen to show; otherwise, false.</returns>
  bool TryShowNextContentScreen();

  /// <summary>
  /// Shows the exit confirmation overlay.
  /// </summary>
  void ShowExitOverlay();

  /// <summary>
  /// Dismisses the exit confirmation overlay.
  /// </summary>
  void DismissExitOverlay();
}
