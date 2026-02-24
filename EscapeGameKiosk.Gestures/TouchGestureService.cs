using Microsoft.Win32;

namespace EscapeGameKiosk.Gestures;

public sealed class TouchGestureService : ITouchGestureService
{
  private readonly IReadOnlyList<GestureRegistrySetting> _settings;

  public TouchGestureService(IReadOnlyList<GestureRegistrySetting>? settings = null)
  {
    _settings = settings ?? GestureRegistrySetting.DefaultSettings;
  }

  public IReadOnlyList<string> GetEnabledGestureNames()
  {
    var enabled = new List<string>();

    foreach (var setting in _settings)
    {
      if (setting.Hive != RegistryHive.CurrentUser)
        continue;

      using var key = Registry.CurrentUser.OpenSubKey(setting.SubKeyPath);
      if (key is null)
      {
        // Key missing — Windows defaults these to enabled.
        enabled.Add(setting.ValueName);
        continue;
      }

      var value = key.GetValue(setting.ValueName);
      if (value is null)
      {
        // Value missing — defaults to enabled.
        enabled.Add(setting.ValueName);
        continue;
      }

      if (value is int intVal && intVal != 0)
      {
        enabled.Add(setting.ValueName);
      }
    }

    return enabled;
  }
}
