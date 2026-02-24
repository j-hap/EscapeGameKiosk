namespace EscapeGameKiosk.Gestures;

public interface ITouchGestureService
{
  /// <summary>
  /// Returns the names of three/four-finger gesture registry values that
  /// are currently enabled (value != 0 or missing, defaulting to enabled).
  /// An empty list means all gestures are already disabled.
  /// </summary>
  IReadOnlyList<string> GetEnabledGestureNames();
}
