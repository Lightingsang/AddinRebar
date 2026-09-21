# Renders the HPExcel MCP Bridge icon from vector glyphs into a multi-size .ico (16..256, PNG-compressed entries)
# that the exe embeds (ApplicationIcon: Explorer, taskbar, Alt-Tab) and the status window shows in its title bar
# (Window.Icon). Same family as the Revit/AutoCAD/Navisworks/ETABS/SAP2000 MCP glyphs (a host + a plug in the repo's accent blue):
# here the host is a Microsoft Excel spreadsheet table grid in Excel green (#107C41) with a distinct 'X' emblem,
# and the plug's cable runs into the spreadsheet grid: the Excel workbook, plugged into MCP.
# Coordinates are aligned to even pixels so 16 and 32 px renders are aliased and crisp;
# larger sizes are anti-aliased scale-ups.
#
# Usage:
#   powershell.exe -ExecutionPolicy Bypass -File HPExcel/tools/icons/render-app-icon.ps1
#
# Writes HPExcel/HPExcel.McpBridge/Resources/HPExcelMcpBridge.ico (+ 256 px PNG preview beside it).

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

if (-not $OutDir) { $OutDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..\HPExcel.McpBridge\Resources')).Path }

# Colors:
# Excel Green (#107C41) - signature Office Excel branding
$excelGreen = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x10, 0x7C, 0x41))
# Grid Ink (#4A4A4A) - line art grey for interior cell lines
$gridInk = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x4A, 0x4A, 0x4A))
# Accent Blue (#0696D7) - repo's MCP plug accent
$accent = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x06, 0x96, 0xD7))
# White (#FFFFFF) - 'X' emblem contrast
$white = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0xFF, 0xFF, 0xFF))

$excelGreen.Freeze(); $gridInk.Freeze(); $accent.Freeze(); $white.Freeze()

# 32x32 coordinate box:
# 1. Excel Green Header & Table Frame (x: 2-18, y: 4-28):
#    - Header bar: (2,4) to (18,10)
#    - Outer columns: left (2,10)-(4,28), right (16,10)-(18,28)
#    - Bottom baseline: (2,26)-(18,28)
# 2. Interior Cell Grid lines in grey ink:
#    - Horizontal dividers at y=14-16 and y=20-22
#    - Vertical divider at x=9-11
# 3. Excel Green Badge with White 'X':
#    - Badge at (1,3) to (11,13)
#    - White 'X' strokes: diagonal cross
# 4. MCP Plug (Accent blue):
#    - Prongs at (22,4)-(24,8) and (26,4)-(28,8)
#    - Body at (20,8) to (30,16)
#    - Cable down from (24,16) to (26,20) and left into grid (16,20) to (26,22)

$parts = @(
    # Spreadsheet Table Frame in Excel Green
    @{ Brush = $excelGreen; Path = 'F1 M2,4 H18 V10 H2 Z M2,10 H4 V28 H2 Z M16,10 H18 V28 H16 Z M2,26 H18 V28 H2 Z' },
    # Interior cell grid lines in dark ink
    @{ Brush = $gridInk;    Path = 'F1 M2,14 H18 V16 H2 Z M2,20 H18 V22 H2 Z M9,10 H11 V26 H9 Z' },
    # Excel Badge on top-left in rich green
    @{ Brush = $excelGreen; Path = 'F1 M1,3 H11 V13 H1 Z' },
    # White 'X' letter inside the badge
    @{ Brush = $white;      Path = 'F1 M3,4 H5 L9,12 H7 Z M7,4 H9 L3,12 H5 Z' },
    # MCP Plug in Accent Blue plugging into the spreadsheet
    @{ Brush = $accent;     Path = 'F1 M22,4 H24 V8 H22 Z M26,4 H28 V8 H26 Z M20,8 H30 V16 H20 Z M24,16 H26 V20 H24 Z M16,20 H26 V22 H16 Z' }
)

function Render-Png([int]$size) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    if ($size % 16 -eq 0) { [System.Windows.Media.RenderOptions]::SetEdgeMode($visual, [System.Windows.Media.EdgeMode]::Aliased) }
    $dc = $visual.RenderOpen()
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform ($size / 32.0), ($size / 32.0)))
    foreach ($part in $parts) { $dc.DrawGeometry($part.Brush, $null, [System.Windows.Media.Geometry]::Parse($part.Path)) }
    $dc.Pop()
    $dc.Close()
    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $ms = New-Object IO.MemoryStream
    $encoder.Save($ms)
    return ,$ms.ToArray()
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$pngs = @{}
foreach ($s in $sizes) { $pngs[$s] = Render-Png $s }

# Write ICO container with PNG-compressed images
$ico = Join-Path $OutDir 'HPExcelMcpBridge.ico'
$stream = [IO.File]::Create($ico)
$writer = New-Object IO.BinaryWriter $stream
try {
    $writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    foreach ($s in $sizes) {
        $dim = if ($s -ge 256) { 0 } else { $s }
        $writer.Write([Byte]$dim); $writer.Write([Byte]$dim); $writer.Write([Byte]0); $writer.Write([Byte]0)
        $writer.Write([UInt16]1); $writer.Write([UInt16]32)
        $writer.Write([UInt32]$pngs[$s].Length); $writer.Write([UInt32]$offset)
        $offset += $pngs[$s].Length
    }
    foreach ($s in $sizes) { $writer.Write([byte[]]$pngs[$s]) }
} finally { $writer.Dispose(); $stream.Dispose() }
Write-Host ("{0}  {1} sizes ({2})  {3} bytes" -f $ico, $sizes.Count, ($sizes -join ','), (Get-Item $ico).Length)

$preview = Join-Path $OutDir 'HPExcelMcpBridge_256.png'
[IO.File]::WriteAllBytes($preview, $pngs[256])
Write-Host ("{0}  {1} bytes" -f $preview, (Get-Item $preview).Length)
