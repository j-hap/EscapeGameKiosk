using Microsoft.Win32;

namespace EscapeGameKiosk.Gestures;

public sealed record GestureRegistrySetting(
  RegistryHive Hive,
  string SubKeyPath,
  string ValueName,
  RegistryValueKind ValueKind,
  object DisabledValue)
{
  public static IReadOnlyList<GestureRegistrySetting> DefaultSettings { get; } = new List<GestureRegistrySetting>
  {
    // Precision touchpad multi-finger gestures (may not exist on all machines).
    new(
      RegistryHive.CurrentUser,
      @"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad",
      "ThreeFingerTapEnabled",
      RegistryValueKind.DWord,
      0),
    new(
      RegistryHive.CurrentUser,
      @"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad",
      "ThreeFingerSlideEnabled",
      RegistryValueKind.DWord,
      0),
    new(
      RegistryHive.CurrentUser,
      @"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad",
      "FourFingerTapEnabled",
      RegistryValueKind.DWord,
      0),
    new(
      RegistryHive.CurrentUser,
      @"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad",
      "FourFingerSlideEnabled",
      RegistryValueKind.DWord,
      0),
  };
}
