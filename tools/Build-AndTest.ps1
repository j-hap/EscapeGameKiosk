<#
.SYNOPSIS
    Builds EscapeGameKiosk and its installer, then runs the end-to-end workflow test.

.DESCRIPTION
    1. Restores and builds the full solution (app + MSI + Bundle) in Release mode.
    2. Runs Test-InstallerWorkflow.ps1 with the freshly-built bundle.
       - Installs silently.
       - Checks files, runtime, and app launch.
       - Optionally uninstalls silently (-Uninstall).

    FORCING A PRIVATE RUNTIME INSTALL
    ──────────────────────────────────
    By default the WiX DotNetCompatibilityCheck element skips the dotnet-install
    custom action if a system-wide .NET 10 runtime is already present.  To exercise
    the actual download-and-install code path even on a developer machine that has
    the runtime globally installed, use -ForcePrivateRuntime.

    This has two effects:

    1. FORCEDOTNETINSTALL=1 is passed on the Burn command line.  The MSI custom
       action condition is (NOT DOTNETRUNTIMECHECK OR FORCEDOTNETINSTALL="1"), so
       dotnet-install.ps1 runs and lays down a private runtime in [INSTALLFOLDER]
       regardless of what DotNetCompatibilityCheck found.

    2. When launching the app in the post-install test (section 4), DOTNET_ROOT_X64
       and DOTNET_ROOT are pointed at a non-existent path for the duration of
       Start-Process so the app host cannot fall back to the system runtime.  Only
       the private host\fxr\ tree next to the .exe is visible.  The env vars are
       restored to their original values immediately after Start-Process returns
       (the child process environment is already captured at that point).

.PARAMETER Configuration
    MSBuild configuration.  Default: Release.

.PARAMETER ForcePrivateRuntime
    Two effects:
    1. Passes FORCEDOTNETINSTALL=1 to the installer so dotnet-install.ps1 runs
       and lays down a private runtime in [INSTALLFOLDER] even when a system-wide
       .NET 10 runtime is already present.
    2. Hides the system runtime from the app during the launch test by pointing
       DOTNET_ROOT_X64 / DOTNET_ROOT at a non-existent path for the duration of
       Start-Process, forcing the app to use only the private host\fxr\ tree.

.PARAMETER SkipBuild
    Skip the build step (use the last build output as-is).

.PARAMETER Uninstall
    Forwarded to Test-InstallerWorkflow.ps1.  When specified, the bundle
    uninstaller is run silently after all checks, and removal of [InstallDir]
    is verified.

.PARAMETER StartupGracePeriodSeconds
    Forwarded to Test-InstallerWorkflow.ps1.  Default: 4.

.EXAMPLE
    # Build and test; skip dotnet-install if system runtime is present
    .\tools\Build-AndTest.ps1

.EXAMPLE
    # Build and test; always exercise the dotnet-install.ps1 download path
    .\tools\Build-AndTest.ps1 -ForcePrivateRuntime

.EXAMPLE
    # Full end-to-end including uninstall (suitable for CI)
    .\tools\Build-AndTest.ps1 -ForcePrivateRuntime -Uninstall
#>
[CmdletBinding()]
param(
  [string] $Configuration = 'Release',
  [switch] $ForcePrivateRuntime,
  [switch] $SkipBuild,
  [switch] $Uninstall,
  [int]    $StartupGracePeriodSeconds = 4
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$bundleExe = Join-Path $repoRoot "EscapeGameKiosk.Bundle\bin\x64\$Configuration\EscapeGameKioskSetup.exe"
$testScript = Join-Path $repoRoot 'tools\Test-InstallerWorkflow.ps1'

# ── Helpers ──────────────────────────────────────────────────────────────────

function Step([string]$msg) {
  Write-Host "`n══ $msg" -ForegroundColor Cyan
}

function Info([string]$msg) { Write-Host "  $msg" -ForegroundColor Yellow }
function Ok([string]$msg) { Write-Host "  OK  $msg" -ForegroundColor Green }
function Err([string]$msg) { Write-Host "  ERR $msg" -ForegroundColor Red }

# ── Step 1: Build ─────────────────────────────────────────────────────────────

if ($SkipBuild) {
  Step 'Build (skipped)'
  Info '-SkipBuild specified — using existing output'
} else {
  Step "Build ($Configuration)"

  # Building the bundle project is sufficient — the ProjectReference in
  # EscapeGameKiosk.Installer.wixproj uses Targets="Publish", so MSBuild
  # publishes the app (populating publish\) before harvesting files into the MSI.
  $buildArgs = @(
    'build'
    (Join-Path $repoRoot 'EscapeGameKiosk.Bundle\EscapeGameKiosk.Bundle.wixproj')
    '--configuration', $Configuration
    '--nologo'
    '-p:Platform=x64'
  )

  Info "dotnet $($buildArgs -join ' ')"
  & dotnet @buildArgs
  if ($LASTEXITCODE -ne 0) {
    Err "Bundle build failed (exit code $LASTEXITCODE)"
    exit $LASTEXITCODE
  }
  Ok 'Build succeeded'
}

if (-not (Test-Path $bundleExe)) {
  Err "Bundle not found after build: $bundleExe"
  exit 1
}
Info "Bundle: $bundleExe"

# ── Step 2: Report runtime detection mode ────────────────────────────────────

if ($ForcePrivateRuntime) {
  Step 'Forcing private runtime install (ForcePrivateRuntime)'
  Info 'FORCEDOTNETINSTALL=1 will be passed to the installer'
  Info 'dotnet-install.ps1 will run and lay down a private runtime in [INSTALLFOLDER]'
  Info 'System runtime at %ProgramFiles%\dotnet is NOT touched'
} else {
  Step 'Runtime detection (system runtime used if present)'
  Info 'Use -ForcePrivateRuntime to always exercise the dotnet-install.ps1 code path'
}

# ── Step 3: Run the workflow test ─────────────────────────────────────────────

Step 'Running Test-InstallerWorkflow.ps1'

& $testScript `
  -InstallerPath             $bundleExe `
  -StartupGracePeriodSeconds $StartupGracePeriodSeconds `
  -ForceDotNetInstall:$ForcePrivateRuntime `
  -Uninstall:$Uninstall

exit $LASTEXITCODE
