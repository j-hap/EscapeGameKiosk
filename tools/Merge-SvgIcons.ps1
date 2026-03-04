<#
.SYNOPSIS
  Merges two source SVG files into a single composited SVG.

.DESCRIPTION
  Reads <BackgroundSvg> and <OverlaySvg>, each assumed to share the same
  512×512 viewBox.  All SVG-namespace child elements (defs, path, rect,
  polygon, g, …) are imported into a new SVG document in order:
  background layer first, then overlay layer.
  Sodipodi / Inkscape metadata nodes are silently dropped.

  The result is written to <Output>, overwriting any existing file.

  This keeps the two source SVGs as the sole source of truth and avoids
  hand-duplicating path data into a composite file.

.EXAMPLE
  .\tools\Merge-SvgIcons.ps1 `
      -BackgroundSvg EscapeGameKiosk\Assets\lock.svg `
      -OverlaySvg    EscapeGameKiosk.Configurator\Assets\gear.svg `
      -Output        EscapeGameKiosk.Configurator\Assets\logo.svg
#>
param(
  [Parameter(Mandatory)][string]$BackgroundSvg,
  [Parameter(Mandatory)][string]$OverlaySvg,
  [Parameter(Mandatory)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$svgNs = 'http://www.w3.org/2000/svg'

# Returns all direct SVG-namespace child elements of the document root,
# skipping Inkscape / Sodipodi metadata nodes.
function Get-SvgChildren([xml]$doc) {
  $doc.DocumentElement.ChildNodes | Where-Object {
    $_ -is [System.Xml.XmlElement] -and $_.NamespaceURI -eq $svgNs
  }
}

# Skip a <defs> that contains nothing useful (Inkscape writes an empty <defs/>).
function Test-NonEmptyDefs([System.Xml.XmlElement]$el) {
  if ($el.LocalName -ne 'defs') { return $true }
  # keep only if it has at least one child element
  # @() ensures the result is always an array (never $null) under Set-StrictMode
  return @($el.ChildNodes | Where-Object { $_ -is [System.Xml.XmlElement] }).Count -gt 0
}

$bgDoc = [xml](Get-Content $BackgroundSvg -Raw -Encoding UTF8)
$ovDoc = [xml](Get-Content $OverlaySvg    -Raw -Encoding UTF8)

# ── Build output document ────────────────────────────────────────────────────
$out = New-Object System.Xml.XmlDocument
$decl = $out.CreateXmlDeclaration('1.0', 'UTF-8', $null)
[void]$out.AppendChild($decl)

$root = $out.CreateElement('svg', $svgNs)
$root.SetAttribute('viewBox', '0 0 512 512')
$root.SetAttribute('width', '512')
$root.SetAttribute('height', '512')
[void]$out.AppendChild($root)

# ── Background layer ─────────────────────────────────────────────────────────
[void]$root.AppendChild($out.CreateComment(" background: $(Split-Path $BackgroundSvg -Leaf) "))
foreach ($node in (Get-SvgChildren $bgDoc)) {
  if (-not (Test-NonEmptyDefs $node)) { continue }
  [void]$root.AppendChild($out.ImportNode($node, $true))
}

# ── Overlay layer ────────────────────────────────────────────────────────────
[void]$root.AppendChild($out.CreateComment(" overlay: $(Split-Path $OverlaySvg -Leaf) "))
foreach ($node in (Get-SvgChildren $ovDoc)) {
  if (-not (Test-NonEmptyDefs $node)) { continue }
  [void]$root.AppendChild($out.ImportNode($node, $true))
}

# ── Write output ─────────────────────────────────────────────────────────────
$outDir = Split-Path $Output -Parent
if ($outDir -and -not (Test-Path $outDir)) {
  New-Item -ItemType Directory -Path $outDir | Out-Null
}

# Use XmlWriterSettings for clean, readable indentation.
$xws = New-Object System.Xml.XmlWriterSettings
$xws.Indent = $true
$xws.IndentChars = '  '
$xws.Encoding = [System.Text.Encoding]::UTF8
$xws.OmitXmlDeclaration = $false

$writer = [System.Xml.XmlWriter]::Create($Output, $xws)
try { $out.WriteTo($writer) }
finally { $writer.Dispose() }

Write-Host "Merged SVG → $Output"
