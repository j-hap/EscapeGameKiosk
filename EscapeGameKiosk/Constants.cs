namespace EscapeGameKiosk;

/// <summary>
/// Application-wide constants and default values.
/// </summary>
public static class Constants
{
  /// <summary>
  /// Security and exit sequence constants.
  /// </summary>
  public static class Security
  {
    /// <summary>
    /// Default exit sequence (0=top-left, 1=top-right, 2=bottom-right, 3=bottom-left).
    /// </summary>
    public static readonly int[] DefaultExitSequence = [0, 2, 1, 3];

    /// <summary>
    /// Timeout for exit sequence in seconds.
    /// </summary>
    public const int ExitTimeoutSeconds = 4;

    /// <summary>
    /// Corner size in pixels for exit sequence detection.
    /// </summary>
    public const double CornerSizePixels = 30.0;
  }

  /// <summary>
  /// Password lockout constants.
  /// </summary>
  public static class Lockout
  {
    /// <summary>
    /// Base lockout duration in seconds.
    /// </summary>
    public const int BaseSeconds = 5;

    /// <summary>
    /// Additional seconds added per tier.
    /// </summary>
    public const int StepSeconds = 5;

    /// <summary>
    /// Number of failed attempts before lockout tier increases.
    /// </summary>
    public const int Threshold = 3;
  }

  /// <summary>
  /// UI text constants.
  /// </summary>
  public static class UI
  {
    public const string AccessRequiredText = "ACCESS REQUIRED";
    public const string AccessDeniedText = "ACCESS DENIED";
    public const string PlaySymbol = "\u25B6";
    public const string PauseSymbol = "\u23F8";
  }

  /// <summary>
  /// Timer interval constants.
  /// </summary>
  public static class Timers
  {
    /// <summary>
    /// Video control timer interval in milliseconds.
    /// </summary>
    public const int VideoControlIntervalMs = 250;

    /// <summary>
    /// Lockout countdown timer interval in milliseconds.
    /// </summary>
    public const int LockoutCountdownIntervalMs = 1000;
  }

  /// <summary>
  /// Virtual key codes for keyboard hook.
  /// </summary>
  public static class VirtualKeys
  {
    public const int Tab = 0x09;
    public const int Escape = 0x1B;
    public const int Menu = 0x12;      // Alt key
    public const int F4 = 0x73;
    public const int Control = 0x11;
    public const int LeftWin = 0x5B;
    public const int RightWin = 0x5C;
  }
}
