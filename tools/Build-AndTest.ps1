<#
.SYNOPSIS
    Builds EscapeGameKiosk and its installer, then runs the end-to-end workflow test.

.DESCRIPTION
    1. Restores and builds the full solution (app + MSI + Bundle) in Release mode.
    2. Runs Test-InstallerWorkflow.ps1 with the freshly-built bundle.
       - Installs silently.
       - Checks files, runtime, and app launch.
       - Uninstalls silently.

    FORCING A PRIVATE RUNTIME INSTALL
    ──────────────────────────────────
    By default the WiX DotNetCompatibilityCheck element skips the dotnet-install
    custom action if a system-wide .NET 10 runtime is already present.  To exercise
    the actual download-and-install code path even on a developer machine that has
    the runtime globally installed, use -ForcePrivateRuntime.

    The trick: DotNetCompatibilityCheck probes for the runtime using the same
    environment-variable-first order as the .NET app host:
      1. DOTNET_ROOT_X64  (x64 Windows)
      2. DOTNET_ROOT
      3. System paths (%ProgramFiles%\dotnet, %LocalAppData%\Microsoft\dotnet)

    Setting DOTNET_ROOT_X64 and DOTNET_ROOT to a path that contains no runtime
    causes the check to report "not found" (property DOTNETRUNTIMECHECK = 0) even
    though a global runtime exists.  Because those env vars are set in this script's
    process before Start-Process launches the installer, they are inherited through:
      this script → Burn bundle → msiexec → custom action subprocess

    The system installation at %ProgramFiles%\dotnet is never touched.
    After the test the env vars are restored to their original values.

.PARAMETER Configuration
    MSBuild configuration.  Default: Release.

.PARAMETER ForcePrivateRuntime
    Set DOTNET_ROOT_X64 / DOTNET_ROOT to an empty temp directory before running
    the installer so DotNetCompatibilityCheck always reports the runtime as absent,
    forcing the dotnet-install.ps1 download path to execute.

.PARAMETER SkipBuild
    Skip the build step (use the last build output as-is).

.PARAMETER StartupGracePeriodSeconds
    Forwarded to Test-InstallerWorkflow.ps1.  Default: 4.

.EXAMPLE
    # Build and test; skip dotnet-install if system runtime is present
    .\tools\Build-AndTest.ps1

.EXAMPLE
    # Build and test; always exercise the dotnet-install.ps1 download path
    .\tools\Build-AndTest.ps1 -ForcePrivateRuntime
#>
[CmdletBinding()]
param(
  [string] $Configuration = 'Release',
  [switch] $ForcePrivateRuntime,
  [switch] $SkipBuild,
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

# ── Step 2: Optionally spoof env vars to force private runtime install ────────

$savedRootX64 = $env:DOTNET_ROOT_X64
$savedRoot = $env:DOTNET_ROOT
$fakeDotnetDir = $null

if ($ForcePrivateRuntime) {
  Step 'Isolating runtime detection (ForcePrivateRuntime)'

  # Create an empty directory.  DotNetCompatibilityCheck will find no runtime
  # here and set DOTNETRUNTIMECHECK = 0, which triggers the custom action.
  $fakeDotnetDir = Join-Path ([System.IO.Path]::GetTempPath()) "dotnet-test-empty-$(New-Guid)"
  New-Item -ItemType Directory -Path $fakeDotnetDir | Out-Null

  $env:DOTNET_ROOT_X64 = $fakeDotnetDir
  $env:DOTNET_ROOT = $fakeDotnetDir

  Info "DOTNET_ROOT_X64 → $fakeDotnetDir"
  Info "DOTNET_ROOT     → $fakeDotnetDir"
  Info 'System runtime at %ProgramFiles%\dotnet is NOT touched'
  Info 'dotnet-install.ps1 will download and install a private runtime into [INSTALLFOLDER]'
} else {
  Step 'Runtime detection (system runtime used if present)'
  Info 'Use -ForcePrivateRuntime to always exercise the dotnet-install.ps1 code path'
}

# ── Step 3: Run the workflow test ─────────────────────────────────────────────

Step 'Running Test-InstallerWorkflow.ps1'

try {
  & $testScript `
    -InstallerPath             $bundleExe `
    -StartupGracePeriodSeconds $StartupGracePeriodSeconds
  # -Uninstall

  $testExitCode = $LASTEXITCODE
} finally {
  # ── Restore env vars and clean up the fake dotnet dir ─────────────────────
  if ($ForcePrivateRuntime) {
    Step 'Restoring environment'

    if ($null -eq $savedRootX64) { Remove-Item Env:DOTNET_ROOT_X64 -ErrorAction SilentlyContinue }
    else { $env:DOTNET_ROOT_X64 = $savedRootX64 }

    if ($null -eq $savedRoot) { Remove-Item Env:DOTNET_ROOT -ErrorAction SilentlyContinue }
    else { $env:DOTNET_ROOT = $savedRoot }

    if ($fakeDotnetDir -and (Test-Path $fakeDotnetDir)) {
      Remove-Item -Recurse -Force $fakeDotnetDir -ErrorAction SilentlyContinue
    }

    Info 'DOTNET_ROOT_X64 and DOTNET_ROOT restored'
  }
}

exit $testExitCode
