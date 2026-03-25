using System.IO;

namespace EscapeGameKiosk;

/// <summary>
/// Well-known file-system paths used by both the kiosk and the configurator.
/// </summary>
public static class KioskPaths
{
  /// <summary>
  /// The sub-folder name used under %APPDATA% for all kiosk data.
  /// </summary>
  public const string AppDataFolderName = "EscapeGameKiosk";

  /// <summary>
  /// Absolute path to the default appsettings.json location:
  /// %APPDATA%\EscapeGameKiosk\appsettings.json.
  /// </summary>
  public static string DefaultSettingsPath => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      AppDataFolderName, "appsettings.json");

  /// <summary>
  /// Resolves the settings file path from an already-stripped argument list
  /// (i.e. the exe path is NOT present — matches what WPF's <c>StartupEventArgs.Args</c> provides).
  /// Priority: <c>--config &lt;path&gt;</c> → <see cref="DefaultSettingsPath"/>.
  /// Relative paths are resolved against the current working directory.
  /// </summary>
  public static string Resolve(string[] args)
  {
    for (int i = 0; i < args.Length - 1; i++)
    {
      if (args[i].Equals("--config", StringComparison.OrdinalIgnoreCase))
      {
        string p = args[i + 1];
        return Path.IsPathRooted(p) ? p : Path.GetFullPath(p);
      }
    }
    return DefaultSettingsPath;
  }

  /// <summary>
  /// Resolves the settings file path by reading
  /// <see cref="Environment.GetCommandLineArgs"/> directly (strips the exe at index 0).
  /// Convenience overload for non-WPF entry points such as the configurator.
  /// </summary>
  public static string Resolve()
  {
    string[] all = Environment.GetCommandLineArgs();
    return Resolve(all.Length > 1 ? all[1..] : []);
  }
}
