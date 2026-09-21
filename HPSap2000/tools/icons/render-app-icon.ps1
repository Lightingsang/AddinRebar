# Renders the HPSap2000 MCP Bridge icon from vector glyphs into a multi-size .ico (16..256, PNG-compressed entries)
# that the exe embeds (ApplicationIcon: Explorer, taskbar, Alt-Tab) and the status window shows in its title bar
# (Window.Icon). Same family as the Revit/AutoCAD/Navisworks/ETABS MCP glyphs (a host + a plug in the repo's accent blue):
# here the host is a structural space truss / chevron braced frame on supports — what SAP2000 analyses — and the plug's
# cable runs into the frame: the model, plugged into MCP. Coordinates are aligned to even pixels so 16 and 32 px
# renders are aliased and crisp; larger sizes are anti-aliased scale-ups.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPSap2000/tools/icons/render-app-icon.ps1
#
# Windows PowerShell 5.1 (STA, which WPF rendering needs); writes HPSap2000/HPSap2000.McpBridge/Resources/HPSap2000McpBridge.ico
# (+ a 256 px PNG preview beside it for docs).
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

if (-not $OutDir) { $OutDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..\HPSap2000.McpBridge\Resources')).Path }

# Ink = the repo's line-art grey, accent = its blue (Color.Accent #0696D7).
$ink = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x4A, 0x4A, 0x4A))
$accent = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x06, 0x96, 0xD7))
$ink.Freeze(); $accent.Freeze()

# 32x32 box.
# SAP2000 Structural Truss:
# Top chord: (2,6) to (16,8)
# Bottom chord: (0,22) to (18,24)
# Left column/post: (2,6) to (4,24)
# Right column/post: (14,6) to (16,24)
# Center post: (8,6) to (10,24)
# Diagonal braces (Chevron / Pratt truss):
# Diagonal 1: (2,6) down to (9,24)
# Diagonal 2: (16,6) down to (9,24)
# Supports / Footings: (0,24) to (5,28) and (13,24) to (18,28)
# Plug (Accent blue):
# Prongs at (22,4)-(24,8) and (26,4)-(28,8)
# Body at (20,8) to (30,16)
# Cable down and left into the frame's top chord at (16,14)
$parts = @(
    @{
        Brush = $ink;
        Path = 'F1 ' +
               'M2,6 H16 V8 H2 Z ' +
               'M0,22 H18 V24 H0 Z ' +
               'M2,6 H4 V22 H2 Z ' +
               'M14,6 H16 V22 H14 Z ' +
               'M8,6 H10 V22 H8 Z ' +
               'M2,6 L4,6 L10,22 L8,22 Z ' +
               'M16,6 L14,6 L8,22 L10,22 Z ' +
               'M0,24 H5 V28 H0 Z ' +
               'M13,24 H18 V28 H13 Z'
    },
    @{
        Brush = $accent;
        Path = 'F1 ' +
               'M22,4 H24 V8 H22 Z ' +
               'M26,4 H28 V8 H26 Z ' +
               'M20,8 H30 V16 H20 Z ' +
               'M24,16 H26 V20 H24 Z ' +
               'M16,18 H26 V20 H16 Z'
    }
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

# ICO container with PNG-compressed images (Windows Vista+; WPF's Window.Icon reads it too).
$ico = Join-Path $OutDir 'HPSap2000McpBridge.ico'
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

$preview = Join-Path $OutDir 'HPSap2000McpBridge_256.png'
[IO.File]::WriteAllBytes($preview, $pngs[256])
Write-Host ("{0}  {1} bytes" -f $preview, (Get-Item $preview).Length)
