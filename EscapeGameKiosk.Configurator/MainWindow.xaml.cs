using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace EscapeGameKiosk.Configurator;

public partial class MainWindow : Window
{
  // Supported video file extensions for the Browse dialog
  private const string VideoFilter =
      "Video files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.flv;*.m4v|All files|*.*";

  private string _settingsPath = string.Empty;

  public MainWindow()
  {
    InitializeComponent();
    _settingsPath = ResolveSettingsPath();
    ConfigPathHint.Text = $"Config: {_settingsPath}";
    LoadSettings();
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Settings path resolution
  // ──────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Locates appsettings.json.
  /// Both the configurator and the kiosk app are installed into the same
  /// directory, so appsettings.json is always a sibling of this executable.
  /// During development the file also lives next to the exe in the output dir.
  /// </summary>
  private static string ResolveSettingsPath()
  {
    return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Load / Save
  // ──────────────────────────────────────────────────────────────────────────

  private void LoadSettings()
  {
    try
    {
      if (!File.Exists(_settingsPath))
      {
        SetStatus("Config file not found — defaults loaded.", isError: true);
        VideoPathBox.Text = string.Empty;
        PasswordBox.Password = string.Empty;
        return;
      }

      string json = File.ReadAllText(_settingsPath);
      JsonNode root = JsonNode.Parse(json)
          ?? throw new JsonException("Config file is empty or invalid JSON.");

      JsonNode? appSettings = root["AppSettings"];
      VideoPathBox.Text = appSettings?["VideoPath"]?.GetValue<string>() ?? string.Empty;
      PasswordBox.Password = appSettings?["Password"]?.GetValue<string>() ?? string.Empty;
      SetStatus("Settings loaded.", isError: false);
    }
    catch (Exception ex)
    {
      SetStatus($"Error loading settings: {ex.Message}", isError: true);
    }
  }

  private void SaveSettings()
  {
    try
    {
      string videoPath = VideoPathBox.Text.Trim();
      string password = PasswordBox.Password;

      if (string.IsNullOrEmpty(videoPath))
      {
        SetStatus("Video path cannot be empty.", isError: true);
        VideoPathBox.Focus();
        return;
      }

      // Read existing JSON (or start fresh) so we don't discard Logging section etc.
      JsonNode root;
      if (File.Exists(_settingsPath))
      {
        string existing = File.ReadAllText(_settingsPath);
        root = JsonNode.Parse(existing)
            ?? new JsonObject();
      }
      else
      {
        root = new JsonObject();
        // Ensure the parent directory exists
        string? dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir))
          Directory.CreateDirectory(dir);
      }

      // Create or update only the AppSettings section
      if (root["AppSettings"] is not JsonObject appSettings)
      {
        appSettings = new JsonObject();
        root["AppSettings"] = appSettings;
      }

      appSettings["Password"] = JsonValue.Create(password);
      appSettings["VideoPath"] = JsonValue.Create(videoPath);

      // Preserve AllowKeyboardHook if already present; otherwise default true
      if (appSettings["AllowKeyboardHook"] is null)
        appSettings["AllowKeyboardHook"] = JsonValue.Create(true);

      var writeOptions = new JsonSerializerOptions { WriteIndented = true };
      File.WriteAllText(_settingsPath, root.ToJsonString(writeOptions));

      SetStatus("Saved successfully.", isError: false);
    }
    catch (Exception ex)
    {
      SetStatus($"Error saving settings: {ex.Message}", isError: true);
    }
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Event handlers
  // ──────────────────────────────────────────────────────────────────────────

  private void BrowseVideo_Click(object sender, RoutedEventArgs e)
  {
    var dlg = new OpenFileDialog
    {
      Title = "Select a video file",
      Filter = VideoFilter,
      CheckFileExists = true,
    };

    // Pre-seed the dialog with the current path if it's a local file
    string current = VideoPathBox.Text.Trim();
    if (!string.IsNullOrEmpty(current) && File.Exists(current))
    {
      dlg.InitialDirectory = Path.GetDirectoryName(current);
      dlg.FileName = Path.GetFileName(current);
    }

    if (dlg.ShowDialog() == true)
      VideoPathBox.Text = dlg.FileName;
  }

  private void Reload_Click(object sender, RoutedEventArgs e) => LoadSettings();

  private void Save_Click(object sender, RoutedEventArgs e) => SaveSettings();

  private void ShowPassword_Checked(object sender, RoutedEventArgs e)
  {
    // Replace PasswordBox with a plain TextBox overlay
    VideoPathBox.GetBindingExpression(TextBox.TextProperty); // keep focus chain intact
    PasswordBox.Visibility = Visibility.Collapsed;
    _plainPasswordBox.Text = PasswordBox.Password;
    _plainPasswordBox.Visibility = Visibility.Visible;
    _plainPasswordBox.Focus();
  }

  private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
  {
    PasswordBox.Password = _plainPasswordBox.Text;
    _plainPasswordBox.Visibility = Visibility.Collapsed;
    PasswordBox.Visibility = Visibility.Visible;
  }

  // ──────────────────────────────────────────────────────────────────────────
  // "Show password" plain-text overlay
  // We lazily create a TextBox that sits on top of the PasswordBox in the
  // same grid cell, rather than adding it in XAML (keeps XAML clean).
  // ──────────────────────────────────────────────────────────────────────────

  private TextBox? _plainPasswordBoxBacking;

  private TextBox _plainPasswordBox
  {
    get
    {
      if (_plainPasswordBoxBacking is null)
      {
        _plainPasswordBoxBacking = new TextBox
        {
          Height = 28,
          VerticalContentAlignment = VerticalAlignment.Center,
          Padding = new Thickness(4, 0, 4, 0),
          FontSize = 13,
          BorderBrush = System.Windows.Media.Brushes.Gray,
          Visibility = Visibility.Collapsed,
        };

        // Insert into same parent Grid column as the PasswordBox
        if (PasswordBox.Parent is Grid grid)
        {
          grid.Children.Add(_plainPasswordBoxBacking);
          Grid.SetColumn(_plainPasswordBoxBacking, Grid.GetColumn(PasswordBox));
        }
      }
      return _plainPasswordBoxBacking;
    }
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Helpers
  // ──────────────────────────────────────────────────────────────────────────

  private void SetStatus(string message, bool isError)
  {
    StatusText.Text = message;
    StatusText.Foreground = isError
        ? System.Windows.Media.Brushes.Crimson
        : System.Windows.Media.Brushes.SeaGreen;
  }
}
