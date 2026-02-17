# EscapeGameKiosk

A fullscreen Windows WPF app for escape-room style video playback with a password gate and lock overlay.

## Vibe Coding / AI Disclaimer

Most of this repository was developed with AI assistance (“vibe coding”), including refactors and generated code/tests.

- Review changes before deploying (especially anything related to kiosk/security behavior).
- Treat this as application code, not a security product; validate your kiosk hardening at the OS/device level.
- If something behaves unexpectedly, assume intent may be ambiguous and verify against requirements.

## Run (development)

```
dotnet build .\EscapeGameKiosk.sln
cd .\EscapeGameKiosk
DOTNET_ENVIRONMENT=Development dotnet run -c Debug
```

If you don’t want to `cd`, you can run:

```
dotnet run --project .\EscapeGameKiosk -c Debug
```

## Tests

```
dotnet test .\EscapeGameKiosk.sln -c Debug
```

## Publish (single-file EXE)

```
dotnet publish .\EscapeGameKiosk\EscapeGameKiosk.csproj -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true
```

The output EXE is under `bin\Release\net10.0-windows\win-x64\publish`.

## Configuration

Edit `appsettings.json`:

The app reads settings from the `AppSettings` section:

- `AppSettings:Password`: The unlock password.
- `AppSettings:VideoPath`: Path to a local video file OR a YouTube URL.
	- Relative file paths are resolved from the EXE directory.
- `AppSettings:AllowKeyboardHook`: Enable the keyboard hook that blocks common OS shortcuts.

## Hidden Exit

The app only exits from the password screen. Tap the four screen corners (within ~30px of the corner) in this order:

1. Top-left
2. Bottom-right
3. Top-right
4. Bottom-left

You must complete the sequence within 4 seconds between taps and enter the unlock password to exit the application.

## Limitations

Windows does not allow apps to block secure attention keys (Ctrl+Alt+Del). For full kiosk behavior, use Windows assigned access or group policy in addition to the app.

## Touchpad gestures

Multi-finger touchpad gestures are handled by Windows. To disable three- and four-finger gestures on setup machines, run:

```
powershell -ExecutionPolicy Bypass -File scripts\disable-touchpad-gestures.ps1
```

To restore the previous settings later:

```
powershell -ExecutionPolicy Bypass -File scripts\reenable-touchpad-gestures.ps1
```
