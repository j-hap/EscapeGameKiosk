using System;
using System.Runtime.InteropServices;
using EscapeGameKiosk.Contracts;
using Microsoft.Extensions.Logging;

namespace EscapeGameKiosk.Services;

/// <summary>
/// Service for managing keyboard security and blocking specific key combinations in kiosk mode.
/// Blocks Windows keys, Alt+Tab, Alt+F4, Ctrl+Escape, and Alt+Escape.
/// </summary>
public sealed class KeyboardSecurityService : IKeyboardSecurityService
{
  private readonly ILogger<KeyboardSecurityService> _logger;
  private LowLevelKeyboardHook? _keyboardHook;

  public KeyboardSecurityService(ILogger<KeyboardSecurityService> logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// Starts the keyboard hook to block restricted keys.
  /// </summary>
  public void Start()
  {
    if (_keyboardHook != null)
    {
      _logger.LogWarning("Keyboard hook already started");
      return;
    }

    _keyboardHook = new LowLevelKeyboardHook(ShouldBlockKeyInternal);
    _keyboardHook.Start();
    _logger.LogInformation("Keyboard security hook started");
  }

  /// <summary>
  /// Stops the keyboard hook.
  /// </summary>
  public void Stop()
  {
    if (_keyboardHook == null)
    {
      return;
    }

    _keyboardHook.Stop();
    _keyboardHook.Dispose();
    _keyboardHook = null;
    _logger.LogInformation("Keyboard security hook stopped");
  }

  /// <summary>
  /// Determines whether a specific key press should be blocked based on security rules.
  /// </summary>
  /// <param name="virtualKeyCode">The virtual key code.</param>
  /// <param name="isAltDown">Whether the Alt key is pressed.</param>
  /// <param name="isCtrlDown">Whether the Ctrl key is pressed.</param>
  /// <returns>True if the key should be blocked, false otherwise.</returns>
  public bool ShouldBlockKey(int virtualKeyCode, bool isAltDown, bool isCtrlDown)
  {
    // Block Windows keys (Left and Right)
    if (virtualKeyCode == Constants.VirtualKeys.LeftWin || 
        virtualKeyCode == Constants.VirtualKeys.RightWin)
    {
      _logger.LogDebug("Blocked Windows key: VK={VirtualKey}", virtualKeyCode);
      return true;
    }

    // Block Alt+Tab (task switcher)
    if (isAltDown && virtualKeyCode == Constants.VirtualKeys.Tab)
    {
      _logger.LogDebug("Blocked Alt+Tab");
      return true;
    }

    // Block Alt+F4 (close window)
    if (isAltDown && virtualKeyCode == Constants.VirtualKeys.F4)
    {
      _logger.LogDebug("Blocked Alt+F4");
      return true;
    }

    // Block Ctrl+Escape (Start menu)
    if (isCtrlDown && virtualKeyCode == Constants.VirtualKeys.Escape)
    {
      _logger.LogDebug("Blocked Ctrl+Escape");
      return true;
    }

    // Block Alt+Escape (cycle windows)
    if (isAltDown && virtualKeyCode == Constants.VirtualKeys.Escape)
    {
      _logger.LogDebug("Blocked Alt+Escape");
      return true;
    }

    return false;
  }

  /// <summary>
  /// Internal callback for the low-level keyboard hook.
  /// </summary>
  private bool ShouldBlockKeyInternal(LowLevelKeyboardHook.KbdllHookStruct data)
  {
    int vk = data.VkCode;
    bool altDown = (GetAsyncKeyState(Constants.VirtualKeys.Menu) & 0x8000) != 0;
    bool ctrlDown = (GetAsyncKeyState(Constants.VirtualKeys.Control) & 0x8000) != 0;

    return ShouldBlockKey(vk, altDown, ctrlDown);
  }

  public void Dispose()
  {
    Stop();
    GC.SuppressFinalize(this);
  }

  [DllImport("user32.dll")]
  private static extern short GetAsyncKeyState(int vKey);
}
