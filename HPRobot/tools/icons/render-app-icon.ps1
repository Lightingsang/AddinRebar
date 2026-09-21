# Renders the HPRobot MCP Bridge icon from one vector glyph into a multi-size .ico (16..256, PNG-compressed entries)
# that the exe embeds (ApplicationIcon: Explorer, taskbar, Alt-Tab) and the status window shows in its title bar
# (Window.Icon). Same family as the Revit/AutoCAD/Navisworks/ETABS/SAP2000 MCP glyphs (a host + a plug in the repo's accent blue):
# here the host is a structural FEA portal frame with X-bracing and supports — what Robot Structural Analysis designs and calculates —
# and the plug's cable runs into the frame: the model, plugged into MCP.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPRobot/tools/icons/render-app-icon.ps1
#
# Windows PowerShell 5.1 (STA, which WPF rendering needs); writes HPRobot/HPRobot.McpBridge/Resources/HPRobotMcpBridge.ico
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

if (-not $OutDir) { $OutDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..\HPRobot.McpBridge\Resources')).Path }

# Ink = repo line-art grey (#4A4A4A), accent = repo blue (Color.Accent #0696D7).
$ink = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x4A, 0x4A, 0x4A))
$accent = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x06, 0x96, 0xD7))
$ink.Freeze(); $accent.Freeze()

# 32x32 bounding box:
# Structure:
# Left column: (2,6) to (4,24) -> M2,6 H4 V24 H2 Z
# Right column: (14,6) to (16,24) -> M14,6 H16 V24 H14 Z
# Top beam: (2,6) to (16,8) -> M2,6 H16 V8 H2 Z
# Middle beam: (2,14) to (16,16) -> M2,14 H16 V16 H2 Z
# X-Brace in bottom panel:
# Diagonal 1: (3,15) to (15,23) with 1.5px width -> M3,15 L15,23 L14,24 L2,16 Z
# Diagonal 2: (15,15) to (3,23) with 1.5px width -> M15,15 L3,23 L2,24 L14,16 Z
# Left Pin Support (triangle + footing): M0,27 H6 V29 H0 Z M1,27 L3,23 L5,27 Z
# Right Roller Support (triangle + footing): M12,27 H18 V29 H12 Z M13,27 L15,23 L17,27 Z
#
# Plug (Accent blue #0696D7):
# Prongs: (22,4)-(24,8) and (26,4)-(28,8)
# Plug body: (20,8) to (30,16)
# Cable running down: (24,16)-(26,20)
# Cable running into structure: (15,18)-(26,20)
$parts = @(
    @{ 
        Brush = $ink;    
        Path = 'F1 M2,6 H4 V24 H2 Z M14,6 H16 V24 H14 Z M2,6 H16 V8 H2 Z M2,14 H16 V16 H2 Z M3,15 L15,23 L14,24.5 L2,16.5 Z M15,15 L3,23 L2,24.5 L14,16.5 Z M0,27 H6 V29 H0 Z M1,27 L3,23.5 L5,27 Z M12,27 H18 V29 H12 Z M13,27 L15,23.5 L17,27 Z' 
    },
    @{ 
        Brush = $accent; 
        Path = 'F1 M22,4 H24 V8 H22 Z M26,4 H28 V8 H26 Z M20,8 H30 V16 H20 Z M24,16 H26 V20 H24 Z M15,18 H26 V20 H15 Z' 
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

$ico = Join-Path $OutDir 'HPRobotMcpBridge.ico'
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

$preview = Join-Path $OutDir 'HPRobotMcpBridge_256.png'
[IO.File]::WriteAllBytes($preview, $pngs[256])
Write-Host ("{0}  {1} bytes" -f $preview, (Get-Item $preview).Length)
