# Renders the ribbon icons of HPRebar + HPRebar.McpBridge from the glyph paths in RibbonIcons.cs (the only source of
# truth — this script parses that file, nothing is duplicated here) into PNGs at 16 / 32 / 64 px on both Revit themes,
# plus one contact sheet, so a glyph change can be judged by eye before Revit is restarted. Revit itself never needs
# these PNGs: the buttons take the DrawingImage directly.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPRebar/tools/icons/preview-ribbon-icons.ps1 [-OutDir <dir>]
#
# Windows PowerShell 5.1 (STA by default, which WPF rendering needs; self-relaunches from pwsh). Writes HPRebar/output/icons/.
param([string]$OutDir = '')

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath)
    if ($OutDir) { $argList += @('-OutDir', $OutDir) }
    & $ps @argList
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$source = Join-Path $PSScriptRoot '..\..\HPRebar\Resources\Icons\RibbonIcons.cs'
if (-not $OutDir) { $OutDir = Join-Path $PSScriptRoot '..\..\output\icons' }
New-Item -ItemType Directory -Force $OutDir | Out-Null

# Same palette as RibbonIcons.cs: ink per theme, steel orange, HP MCP blue.
$palette = @{
    light = @{ ink = [System.Windows.Media.Color]::FromRgb(0x3C, 0x3C, 0x3C); bg = [System.Windows.Media.Color]::FromRgb(0xF0, 0xF0, 0xF0) }
    dark  = @{ ink = [System.Windows.Media.Color]::FromRgb(0xE6, 0xE6, 0xE6); bg = [System.Windows.Media.Color]::FromRgb(0x2B, 0x2B, 0x2B) }
    Steel = [System.Windows.Media.Color]::FromRgb(0xE0, 0x64, 0x1E)
    McpAccent = [System.Windows.Media.Color]::FromRgb(0x06, 0x96, 0xD7)
}

# Parse `Name = Glyph((brush, "path"), ...);` blocks out of the C# file.
$text = Get-Content $source -Raw
$glyphs = [ordered]@{}
foreach ($m in [regex]::Matches($text, '(?s)(\w+)\s*=\s*Glyph\((.*?)\);')) {
    $parts = @()
    foreach ($p in [regex]::Matches($m.Groups[2].Value, '\((ink|Steel|McpAccent),\s*"([^"]+)"\)')) {
        $parts += @{ Role = $p.Groups[1].Value; Path = $p.Groups[2].Value }
    }
    if ($parts.Count -gt 0) { $glyphs[$m.Groups[1].Value] = $parts }
}
if ($glyphs.Count -eq 0) { throw "no Glyph(...) blocks found in $source" }

function Brush([System.Windows.Media.Color]$c) { $b = New-Object System.Windows.Media.SolidColorBrush $c; $b.Freeze(); $b }

function Draw($dc, $parts, [string]$theme, [double]$scale) {
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform $scale, $scale))
    foreach ($part in $parts) {
        $color = if ($part.Role -eq 'ink') { $palette[$theme].ink } else { $palette[$part.Role] }
        $dc.DrawGeometry((Brush $color), $null, [System.Windows.Media.Geometry]::Parse($part.Path))
    }
    $dc.Pop()
}

function Save($visual, [int]$w, [int]$h, [string]$path) {
    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $w, $h, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create($path)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}

# Individual PNGs (transparent background), like the ribbon receives them.
foreach ($name in $glyphs.Keys) {
    foreach ($theme in 'light', 'dark') {
        foreach ($size in 16, 32, 64) {
            $visual = New-Object System.Windows.Media.DrawingVisual
            $dc = $visual.RenderOpen()
            Draw $dc $glyphs[$name] $theme ($size / 32.0)
            $dc.Close()
            Save $visual $size $size (Join-Path $OutDir ("{0}_{1}_{2}.png" -f $name, $theme, $size))
        }
    }
}

# Contact sheet: one row per glyph, columns 16 / 32 / 64 on the light background, then the same on the dark one.
$sizes = 16, 32, 64
$cell = 80; $label = 140
$cols = $sizes.Count * 2
$width = $label + $cols * $cell
$height = $glyphs.Count * $cell + 30
$sheet = New-Object System.Windows.Media.DrawingVisual
$dc = $sheet.RenderOpen()
$typeface = New-Object System.Windows.Media.Typeface 'Segoe UI'
$culture = [Globalization.CultureInfo]::InvariantCulture
$ltr = [System.Windows.FlowDirection]::LeftToRight
$dc.DrawRectangle((Brush $palette.light.bg), $null, (New-Object System.Windows.Rect 0, 0, ($label + $sizes.Count * $cell), $height))
$dc.DrawRectangle((Brush $palette.dark.bg), $null, (New-Object System.Windows.Rect ($label + $sizes.Count * $cell), 0, ($sizes.Count * $cell), $height))
$row = 0
foreach ($name in $glyphs.Keys) {
    $ft = New-Object System.Windows.Media.FormattedText -ArgumentList @($name, $culture, $ltr, $typeface, 13.0, (Brush $palette.light.ink), 96.0)
    $dc.DrawText($ft, (New-Object System.Windows.Point 8, (30 + $row * $cell + 30)))
    $col = 0
    foreach ($theme in 'light', 'dark') {
        foreach ($size in $sizes) {
            $x = $label + $col * $cell + ($cell - $size) / 2
            $y = 30 + $row * $cell + ($cell - $size) / 2
            $dc.PushTransform((New-Object System.Windows.Media.TranslateTransform $x, $y))
            Draw $dc $glyphs[$name] $theme ($size / 32.0)
            $dc.Pop()
            if ($row -eq 0) {
                $hdr = New-Object System.Windows.Media.FormattedText -ArgumentList @(("{0} {1}px" -f $theme, $size), $culture, $ltr, $typeface, 11.0, (Brush $palette[$theme].ink), 96.0)
                $dc.DrawText($hdr, (New-Object System.Windows.Point ($label + $col * $cell + 6), 8))
            }
            $col++
        }
    }
    $row++
}
$dc.Close()
$sheetPath = Join-Path $OutDir 'contact-sheet.png'
Save $sheet $width $height $sheetPath
Write-Host ("{0} glyphs x 2 themes x 3 sizes -> {1}; sheet {2}" -f $glyphs.Count, $OutDir, $sheetPath)
