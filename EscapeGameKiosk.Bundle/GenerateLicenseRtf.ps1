# GenerateLicenseRtf.ps1
# Converts a plain-text license file to a minimal RTF file suitable for the
# WiX WixStandardBootstrapperApplication rtfLicense theme.
#
# Usage: GenerateLicenseRtf.ps1 <input-txt> <output-rtf>
param(
  [Parameter(Mandatory)][string] $InputFile,
  [Parameter(Mandatory)][string] $OutputFile
)

$text = [System.IO.File]::ReadAllText($InputFile)

# Escape RTF special characters
$escaped = $text `
  -replace '\\', '\\' `
  -replace '\{', '\{' `
  -replace '\}', '\}'

# Convert each line to an RTF paragraph
$lines = $escaped -split '\r?\n'
$body = ($lines | ForEach-Object { "$_\par" }) -join "`r`n"

$rtf = "{\rtf1\ansi\deff0{\fonttbl{\f0\fswiss\fcharset0 Arial;}}\f0\fs18 $body}"

[System.IO.File]::WriteAllText($OutputFile, $rtf, [System.Text.Encoding]::ASCII)
Write-Host "Generated: $OutputFile"
