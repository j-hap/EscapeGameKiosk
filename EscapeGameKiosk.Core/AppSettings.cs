namespace EscapeGameKiosk;

public sealed class AppSettings
{
  public string Password { get; set; } = "";
  public string VideoPath { get; set; } = "";
  public bool AllowKeyboardHook { get; set; } = true;
}
