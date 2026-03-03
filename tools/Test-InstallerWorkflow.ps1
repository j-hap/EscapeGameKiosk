<#
.SYNOPSIS
    Verifies that the EscapeGameKiosk installer workflow produces a startable application.

.DESCRIPTION
    Optionally runs the bundle installer silently, then checks that:
      1. Install folder and key files exist.
      2. The .NET 10 WindowsDesktop runtime is reachable — either system-wide or
         locally via the host\fxr\ tree that dotnet-install.ps1 lays down next to
         the exe.
      3. The application actually starts and stays alive (i.e. does not crash on
         launch), then is cleanly terminated.
    Optionally uninstalls after the test.

    The WiX Burn bundle (EscapeGameKioskSetup.exe) supports a fully non-interactive
    mode via the /quiet flag, which bypasses the bootstrapper UI entirely.
    The MSI alone can be driven with msiexec /qn.

.PARAMETER InstallerPath
    Path to EscapeGameKioskSetup.exe (the Burn bundle).
    If provided, the installer is run silently before the checks.
    If omitted, the script assumes the application is already installed.

.PARAMETER InstallDir
    Path to the application install directory.
    Defaults to %LOCALAPPDATA%\Programs\EscapeGameKiosk (the WiX per-user default).

.PARAMETER Uninstall
    If specified, run the bundle uninstaller silently after the tests.
    Requires -InstallerPath to be provided (the same exe handles uninstall via /uninstall).

.PARAMETER InstallTimeoutSeconds
    How long to wait for the installer process to complete.
    Default: 300 seconds (5 minutes — allows time for dotnet-install.ps1 to download).

.PARAMETER StartupGracePeriodSeconds
    How long to wait after launching the exe before declaring it "alive".
    Default: 4 seconds.

.EXAMPLE
    # Check an already-installed application
    .\Test-InstallerWorkflow.ps1

.PARAMETER ForceDotNetInstall
    Pass FORCEDOTNETINSTALL=1 to the bundle/MSI so the dotnet-install.ps1 custom
    action fires even when DotNetCompatibilityCheck detects a system-wide runtime.
    Use this on developer machines to verify the private-runtime download path.

.EXAMPLE
    # Install silently, run checks, then uninstall
    .\Test-InstallerWorkflow.ps1 `
        -InstallerPath ".\EscapeGameKiosk.Bundle\bin\Release\EscapeGameKioskSetup.exe" `
        -Uninstall

.EXAMPLE
    # Force the dotnet-install.ps1 path even with a system runtime present
    .\Test-InstallerWorkflow.ps1 `
        -InstallerPath ".\EscapeGameKiosk.Bundle\bin\Release\EscapeGameKioskSetup.exe" `
        -ForceDotNetInstall
#>
[CmdletBinding()]
param(
  [string] $InstallerPath = '',
  [string] $InstallDir = (Join-Path $env:LOCALAPPDATA 'Programs\EscapeGameKiosk'),
  [switch] $Uninstall,
  [switch] $ForceDotNetInstall,
  [int]    $InstallTimeoutSeconds = 300,
  [int]    $StartupGracePeriodSeconds = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'   # collect all failures before stopping

# ── Helpers ─────────────────────────────────────────────────────────────────

$script:PassCount = 0
$script:FailCount = 0

function Pass([string]$msg) {
  Write-Host "  [PASS] $msg" -ForegroundColor Green
  $script:PassCount++
}

function Fail([string]$msg) {
  Write-Host "  [FAIL] $msg" -ForegroundColor Red
  $script:FailCount++
}

function Section([string]$title) {
  Write-Host "`n── $title" -ForegroundColor Cyan
}

function Invoke-SilentProcess([string]$exe, [string[]]$argList, [int]$timeoutSec, [string]$label) {
  Write-Host "  [INFO] Running: $exe $($argList -join ' ')" -ForegroundColor Yellow
  $proc = Start-Process -FilePath $exe -ArgumentList $argList -PassThru -Wait:$false -ErrorAction Stop
  Write-Host "  [INFO] Waiting up to ${timeoutSec}s for $label (PID $($proc.Id))..." -ForegroundColor Yellow
  $completed = $proc.WaitForExit($timeoutSec * 1000)
  if (-not $completed) {
    $proc.Kill()
    Fail "$label did not complete within ${timeoutSec}s — process was killed"
    return $false
  }
  if ($proc.ExitCode -ne 0) {
    Fail "$label exited with code $($proc.ExitCode)"
    return $false
  }
  Pass "$label completed successfully (exit code 0)"
  return $true
}

# ── 0. Silent install (optional) ─────────────────────────────────────────────

Section '0. Silent installation'

if ($InstallerPath -eq '') {
  Write-Host "  [INFO] -InstallerPath not provided — skipping install step, assuming app is already installed" -ForegroundColor Yellow
} elseif (-not (Test-Path $InstallerPath)) {
  Fail "Installer not found: $InstallerPath"
} else {
  # Burn bundle flags:
  #   /quiet       — no UI, no progress window
  #   /norestart   — suppress any reboot prompts
  # These flags are passed to the Burn engine directly; WixStdBA is not shown.
  $installerResolved = (Resolve-Path $InstallerPath).Path
  $installArgs = [System.Collections.Generic.List[string]]@('/quiet', '/norestart')
  if ($ForceDotNetInstall) {
    $installArgs.Add('FORCEDOTNETINSTALL=1')
    Write-Host '  [INFO] FORCEDOTNETINSTALL=1 — dotnet-install.ps1 will run regardless of detected runtime' -ForegroundColor Yellow
  }
  Invoke-SilentProcess -exe $installerResolved `
    -argList $installArgs `
    -timeoutSec $InstallTimeoutSeconds `
    -label 'Bundle installer' | Out-Null
}

# ── 1. File-system checks ────────────────────────────────────────────────────

Section '1. Install directory and key files'

if (Test-Path $InstallDir -PathType Container) {
  Pass "Install directory exists: $InstallDir"
} else {
  Fail "Install directory not found: $InstallDir"
}

$requiredFiles = @(
  'EscapeGameKiosk.exe',
  'EscapeGameKiosk.dll',
  'EscapeGameKiosk.runtimeconfig.json',
  'appsettings.json'
)

foreach ($f in $requiredFiles) {
  $full = Join-Path $InstallDir $f
  if (Test-Path $full) {
    Pass "Found: $f"
  } else {
    Fail "Missing: $f"
  }
}

# ── 2. runtimeconfig.json sanity ────────────────────────────────────────────

Section '2. runtimeconfig.json content'

$rtconfigPath = Join-Path $InstallDir 'EscapeGameKiosk.runtimeconfig.json'
if (Test-Path $rtconfigPath) {
  try {
    $rtconfig = Get-Content $rtconfigPath -Raw | ConvertFrom-Json

    # .NET 5+ uses "frameworks" (array); earlier tooling used "framework" (object).
    $fwList = @()
    if ($rtconfig.runtimeOptions.PSObject.Properties['frameworks']) {
      $fwList = $rtconfig.runtimeOptions.frameworks
    } elseif ($rtconfig.runtimeOptions.PSObject.Properties['framework']) {
      $fwList = @($rtconfig.runtimeOptions.framework)
    }

    $desktopFw = $fwList | Where-Object { $_.name -eq 'Microsoft.WindowsDesktop.App' }
    if ($desktopFw) {
      Pass "Framework Microsoft.WindowsDesktop.App found in runtimeconfig.json"
    } else {
      Fail "Microsoft.WindowsDesktop.App not listed in runtimeconfig.json frameworks (found: $($fwList.name -join ', '))"
    }

    $v10 = $fwList | Where-Object { $_.version -like '10.*' }
    if ($v10) {
      Pass "Framework version is .NET 10.x: $($v10.version -join ', ')"
    } else {
      Fail "No .NET 10.x framework version found in runtimeconfig.json (found: $($fwList.version -join ', '))"
    }
  } catch {
    Fail "Could not parse runtimeconfig.json: $_"
  }
} else {
  Fail "runtimeconfig.json not found — skipping content checks"
}

# ── 3. Runtime availability ──────────────────────────────────────────────────

Section '3. .NET 10 WindowsDesktop runtime availability'

# 3a. Check for a local (app-local) host\fxr installation
$localHostFxrRoot = Join-Path $InstallDir 'host\fxr'
$localHostFxrDll = $null
if (Test-Path $localHostFxrRoot -PathType Container) {
  # Find the highest-versioned subfolder
  $localHostFxrDll = Get-ChildItem -Path $localHostFxrRoot -Filter 'hostfxr.dll' -Recurse |
  Sort-Object { [version]($_.Directory.Name -replace '[^0-9.]', '') } |
  Select-Object -Last 1 -ExpandProperty FullName
}

if ($localHostFxrDll) {
  Pass "App-local hostfxr.dll found: $localHostFxrDll"
  $useLocalRuntime = $true
} else {
  Write-Host "  [INFO] No app-local host\fxr found — checking system-wide runtime" -ForegroundColor Yellow
  $useLocalRuntime = $false
}

# 3b. Check for local shared\Microsoft.WindowsDesktop.App (needed alongside hostfxr)
if ($useLocalRuntime) {
  $localDesktopShared = Join-Path $InstallDir 'shared\Microsoft.WindowsDesktop.App'
  if (Test-Path $localDesktopShared -PathType Container) {
    $versions = Get-ChildItem $localDesktopShared -Directory | Where-Object { $_.Name -like '10.*' }
    if ($versions) {
      Pass "App-local Microsoft.WindowsDesktop.App found: $($versions[-1].Name)"
    } else {
      Fail "shared\Microsoft.WindowsDesktop.App exists but contains no 10.x version folder"
    }
  } else {
    Fail "App-local shared\Microsoft.WindowsDesktop.App not found (required alongside hostfxr.dll)"
  }

  $localNETCoreShared = Join-Path $InstallDir 'shared\Microsoft.NETCore.App'
  if (Test-Path $localNETCoreShared -PathType Container) {
    $versions = Get-ChildItem $localNETCoreShared -Directory | Where-Object { $_.Name -like '10.*' }
    if ($versions) {
      Pass "App-local Microsoft.NETCore.App found: $($versions[-1].Name)"
    } else {
      Fail "shared\Microsoft.NETCore.App exists but contains no 10.x version folder"
    }
  } else {
    Fail "App-local shared\Microsoft.NETCore.App not found (required for hostfxr to function)"
  }
}

# 3c. If no local runtime, verify a system-wide one exists
if (-not $useLocalRuntime) {
  $dotnetExe = Get-Command dotnet -ErrorAction SilentlyContinue
  if ($dotnetExe) {
    $runtimes = & dotnet --list-runtimes 2>$null
    $desktop10 = $runtimes | Where-Object { $_ -match 'Microsoft\.WindowsDesktop\.App\s+10\.' }
    if ($desktop10) {
      Pass "System-wide Microsoft.WindowsDesktop.App 10.x found:`n      $($desktop10 -join "`n      ")"
    } else {
      Fail "No system-wide Microsoft.WindowsDesktop.App 10.x runtime found and no app-local runtime present"
    }
  } else {
    Fail "dotnet.exe not on PATH and no app-local runtime — cannot verify runtime availability"
  }
}

# ── 4. Launch test ───────────────────────────────────────────────────────────

Section "4. Application launch test (${StartupGracePeriodSeconds}s grace period)"

$exePath = Join-Path $InstallDir 'EscapeGameKiosk.exe'
$proc = $null

if (-not (Test-Path $exePath)) {
  Fail "Cannot run launch test — EscapeGameKiosk.exe not found"
} else {
  try {
    Write-Host "  [INFO] Starting process: $exePath" -ForegroundColor Yellow

    # When ForceDotNetInstall is active the private runtime was laid down in
    # [INSTALLFOLDER] and the system runtime must be hidden so the app is forced
    # to use it.  Set DOTNET_ROOT_X64 / DOTNET_ROOT to a non-existent path in
    # the current process immediately before Start-Process; the child inherits
    # this environment at launch.  Restore them right after Start-Process returns
    # (the child's environment is already fixed at that point).
    $savedRootX64 = $env:DOTNET_ROOT_X64
    $savedRoot = $env:DOTNET_ROOT
    if ($ForceDotNetInstall) {
      $env:DOTNET_ROOT_X64 = Join-Path $InstallDir 'nonexistent-dotnet-root'
      $env:DOTNET_ROOT = $env:DOTNET_ROOT_X64
      Write-Host "  [INFO] Launch env: DOTNET_ROOT_X64/DOTNET_ROOT → nonexistent path (forcing app-local runtime)" -ForegroundColor Yellow
    }

    try {
      $proc = Start-Process -FilePath $exePath `
        -WorkingDirectory $InstallDir `
        -PassThru `
        -ErrorAction Stop
    } finally {
      # Restore immediately — child process environment is already captured.
      if ($null -eq $savedRootX64) { Remove-Item Env:DOTNET_ROOT_X64 -ErrorAction SilentlyContinue }
      else { $env:DOTNET_ROOT_X64 = $savedRootX64 }
      if ($null -eq $savedRoot) { Remove-Item Env:DOTNET_ROOT -ErrorAction SilentlyContinue }
      else { $env:DOTNET_ROOT = $savedRoot }
    }

    Write-Host "  [INFO] Waiting ${StartupGracePeriodSeconds}s for process PID $($proc.Id)..." -ForegroundColor Yellow
    Start-Sleep -Seconds $StartupGracePeriodSeconds

    $proc.Refresh()
    if ($proc.HasExited) {
      Fail "Process exited within grace period (exit code: $($proc.ExitCode)) — likely a startup crash"
    } else {
      Pass "Process is still alive after ${StartupGracePeriodSeconds}s (PID $($proc.Id)) — application started successfully"
    }
  } catch {
    Fail "Failed to start process: $_"
  } finally {
    if ($proc -and -not $proc.HasExited) {
      Write-Host "  [INFO] Terminating test process PID $($proc.Id)" -ForegroundColor Yellow
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
  }
}

# ── 5. Silent uninstall (optional) ──────────────────────────────────────────

Section '5. Silent uninstall'

if (-not $Uninstall) {
  Write-Host "  [INFO] -Uninstall not specified — skipping uninstall step" -ForegroundColor Yellow
} elseif ($InstallerPath -eq '') {
  Write-Host "  [INFO] -InstallerPath not provided — cannot run uninstall (need the bundle exe)" -ForegroundColor Yellow
} elseif (-not (Test-Path $InstallerPath)) {
  Fail "Installer not found for uninstall: $InstallerPath"
} else {
  # Burn bundle uninstall:
  #   /uninstall   — signals Burn to remove all packages in the chain
  #   /quiet       — no UI
  #   /norestart   — suppress reboot prompts
  $installerResolved = (Resolve-Path $InstallerPath).Path
  $ok = Invoke-SilentProcess -exe $installerResolved `
    -argList @('/uninstall', '/quiet', '/norestart') `
    -timeoutSec $InstallTimeoutSeconds `
    -label 'Bundle uninstaller'
  if ($ok) {
    # Verify the install directory is gone
    if (-not (Test-Path $InstallDir)) {
      Pass "Install directory was removed: $InstallDir"
    } else {
      Fail "Install directory still exists after uninstall: $InstallDir"
    }
  }
}

# ── Summary ──────────────────────────────────────────────────────────────────

$total = $script:PassCount + $script:FailCount
Write-Host "`n════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Results: $($script:PassCount) passed, $($script:FailCount) failed (of $total checks)" -ForegroundColor $(if ($script:FailCount -eq 0) { 'Green' } else { 'Red' })
Write-Host "════════════════════════════════════════`n" -ForegroundColor Cyan

if ($script:FailCount -gt 0) {
  exit 1
} else {
  exit 0
}
