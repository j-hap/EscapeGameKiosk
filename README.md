# EscapeGameKiosk

A fullscreen Windows WPF app for escape-room style video playback with a password gate and lock
overlay.

## Vibe Coding / AI Disclaimer

Most of this repository was developed with AI assistance (“vibe coding”), including refactors and
generated code/tests.

- Review changes before deploying (especially anything related to kiosk/security behavior).
- Treat this as application code, not a security product; validate your kiosk hardening at the
  OS/device level.
- If something behaves unexpectedly, assume intent may be ambiguous and verify against requirements.

## Run (development)

```
dotnet build .\EscapeGameKiosk.sln
cd .\EscapeGameKiosk
DOTNET_ENVIRONMENT=Development dotnet run -c Debug
```
The project includes a `Properties\launchSettings.json` that passes `--config appsettings.json`,
so `dotnet run` automatically loads the in-tree `appsettings.json` instead of looking in
`%APPDATA%`. The same applies to the Configurator project.
If you don’t want to `cd`, you can run:

```
dotnet run --project .\EscapeGameKiosk -c Debug
```

## Tests

```
dotnet test .\EscapeGameKiosk.sln -c Debug
```

## Deploy (installer)

The project builds a self-contained per-user installer — no administrator rights required on the
target machine.

### Outputs

| File                                                              | Description                                           |
| ----------------------------------------------------------------- | ----------------------------------------------------- |
| `EscapeGameKiosk.Bundle\bin\x64\Release\EscapeGameKioskSetup.exe` | Bootstrapper EXE — run this on the target machine     |
| `EscapeGameKiosk.Installer\bin\x64\Release\EscapeGameKiosk.msi`   | MSI (embedded inside the EXE; also usable standalone) |

### Prerequisites on the target machine

The installer does not bundle the runtimes. Both must be present before running the app:

| Requirement                                                                                 | Notes                                                                                                                              |
| ------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| [.NET 10 Windows Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/10.0)  | **Must be installed manually if absent.** The installer will place the app files but the app will not launch without this runtime. |
| [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) | Pre-installed on Windows 10 21H2+ and all Windows 11 versions. No action needed on modern machines.                                |

### Build

```powershell
# First time / CI: restore the WiX CLI tool
dotnet tool restore

# Build everything: app → publish → MSI → EXE
dotnet build EscapeGameKiosk.Bundle\EscapeGameKiosk.Bundle.wixproj -c Release -p:Platform=x64
```

Or build the whole solution (includes tests and other projects):

```powershell
dotnet build EscapeGameKiosk.sln -c Release
```

### Install

Run `EscapeGameKioskSetup.exe` on the target machine.

- Installs to `%LOCALAPPDATA%\Programs\EscapeGameKiosk` by default (no UAC prompt).
- Adds a Start Menu shortcut under **EscapeGameKiosk**.
- Appears in **Settings → Apps** for clean uninstall.
- A folder-browse dialog lets you change the install location (UAC is only prompted if you choose a
  protected path like `C:\Program Files`).

### Uninstall

Use **Settings → Apps → EscapeGameKiosk → Uninstall**, or re-run `EscapeGameKioskSetup.exe` and
choose **Uninstall**.

### Configuration after install

Use the **Configurator** app (see [Configurator](#configurator)) to create and edit the settings
file on first run. Settings are stored in the user's roaming profile — not in the install folder —
so they survive upgrades and reinstalls:

```
%APPDATA%\EscapeGameKiosk\appsettings.json
```

The kiosk will not start if this file is missing; it will show a message directing you to run the
Configurator first.

See the [Configuration](#configuration) section for available settings.

## Configurator

`EscapeGameKioskConfigurator.exe` is a small companion utility that ships alongside the kiosk app.
It provides a simple GUI for editing the two settings that need to change between escape-room
sessions — the video source and the exit password — without touching JSON by hand.

### Running the Configurator

After installation the configurator appears in the same Start Menu folder as the kiosk:

**Start → EscapeGameKiosk → EscapeGameKiosk Configurator**

Or launch it directly:

```
%LOCALAPPDATA%\Programs\EscapeGameKiosk\EscapeGameKioskConfigurator.exe
```

During development:

```powershell
dotnet run --project .\EscapeGameKiosk.Configurator -c Debug
```

### What it edits

The configurator reads and writes `%APPDATA%\EscapeGameKiosk\appsettings.json` — the same file
the kiosk reads at startup. It only updates the `AppSettings` section; other sections (e.g.
`Logging`) are preserved. If the file does not exist the configurator creates it (including the
parent directory) when **Save** is pressed.

| Field          | Description                                                                                            |
| -------------- | ------------------------------------------------------------------------------------------------------ |
| **Path / URL** | Local video file path (e.g. `C:\Videos\intro.mp4`) or a web URL. Use **Browse…** to pick a local file. |
| **Password**   | Password required to exit the kiosk. Click **Show** to reveal it while typing.                         |

### Buttons

| Button      | Action                                                                                                 |
| ----------- | ------------------------------------------------------------------------------------------------------ |
| **Browse…** | Opens a file-picker pre-seeded with the current path (if the file exists).                             |
| **Show**    | Toggles password visibility.                                                                           |
| **Reload**  | Discards unsaved changes and reloads from disk.                                                        |
| **Save**    | Writes changes to `appsettings.json`. The status bar (bottom-left) confirms success or shows an error. |

The status bar also shows the full path of the config file being edited, so it is always clear which
installation is being configured when multiple environments exist side-by-side.

## Touchpad gesture check on startup

When the app starts it reads the registry to check whether any three- or four-finger precision
touchpad gestures are enabled. If any are detected, the app opens the Windows Settings touchpad page
(`ms-settings:devices-touchpad`) and shows a dialog listing the enabled gestures with instructions
to set each to **Nothing** under *Bluetooth & devices → Touchpad → Three-finger gestures /
Four-finger gestures*. After making the changes, press **Retry** to re-check. Press **Cancel** to
exit the app without launching the kiosk.

## Configuration

Edit `appsettings.json`:

The app reads settings from the `AppSettings` section:

- `AppSettings:Password`: The unlock password.
- `AppSettings:VideoPath`: Path to a local video file OR a YouTube URL. - Relative file paths are
	resolved from the EXE directory.
- `AppSettings:AllowKeyboardHook`: Enable the keyboard hook that blocks common OS shortcuts.

## Hidden Exit

The app only exits from the password screen. Tap the four screen corners (within ~30px of the
corner) in this order:

1. Top-left
2. Bottom-right
3. Top-right
4. Bottom-left

You must complete the sequence within 4 seconds between taps and enter the unlock password to exit
the application.

## Limitations

Windows does not allow apps to block secure attention keys (Ctrl+Alt+Del). For full kiosk behavior,
use Windows assigned access or group policy in addition to the app.

## Touchpad gestures

Multi-finger touchpad gestures are handled by Windows, not the app. The app checks for enabled
gestures at startup and guides you to disable them manually via Windows Settings (see [Touchpad
gesture check on startup](#touchpad-gesture-check-on-startup)). There are no standalone scripts for
disabling or re-enabling gestures.
