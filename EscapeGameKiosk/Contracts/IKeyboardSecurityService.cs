namespace EscapeGameKiosk.Contracts;

/// <summary>
/// Service for managing keyboard security and blocking specific key combinations.
/// </summary>
public interface IKeyboardSecurityService : IDisposable
{
  /// <summary>
  /// Starts the keyboard hook to block restricted keys.
  /// </summary>
  void Start();

  /// <summary>
  /// Stops the keyboard hook.
  /// </summary>
  void Stop();

  /// <summary>
  /// Determines whether a specific key press should be blocked.
  /// </summary>
  /// <param name="virtualKeyCode">The virtual key code.</param>
  /// <param name="isAltDown">Whether the Alt key is pressed.</param>
  /// <param name="isCtrlDown">Whether the Ctrl key is pressed.</param>
  /// <returns>True if the key should be blocked, false otherwise.</returns>
  bool ShouldBlockKey(int virtualKeyCode, bool isAltDown, bool isCtrlDown);
}
