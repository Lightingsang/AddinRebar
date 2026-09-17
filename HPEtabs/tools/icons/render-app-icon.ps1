# Renders the HPEtabs MCP Bridge icon from one vector glyph into a multi-size .ico (16..256, PNG-compressed entries)
# that the exe embeds (ApplicationIcon: Explorer, taskbar, Alt-Tab) and the status window shows in its title bar
# (Window.Icon). Same family as the Revit/AutoCAD/Navisworks MCP glyphs (a host + a plug in the repo's accent blue):
# here the host is a three-storey structural frame on its foundation — what ETABS analyses — and the plug's cable
# runs into the frame's middle beam: the model, plugged into MCP. Every coordinate of the 32x32 glyph is even, so the
# 16 and 32 px renders are aliased and crisp; the larger sizes are anti-aliased scale-ups. Re-run after changing the
# glyph and commit the .ico.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPEtabs/tools/icons/render-app-icon.ps1
#
# Windows PowerShell 5.1 (STA, which WPF rendering needs); writes HPEtabs/HPEtabs.McpBridge/Resources/HPEtabsMcpBridge.ico
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

if (-not $OutDir) { $OutDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..\HPEtabs.McpBridge\Resources')).Path }

# Ink = the repo's line-art grey, accent = its blue (Color.Accent #0696D7). The window icon sits on both light and dark
# title bars, so the ink is a mid grey that reads on either.
$ink = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x4A, 0x4A, 0x4A))
$accent = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x06, 0x96, 0xD7))
$ink.Freeze(); $accent.Freeze()

# 32x32 box. Frame: two columns (x 2-4, 14-16) from the roof beam (y 6) to the foundation (y 26-30, x 0-18), three beams (y 6-8, 14-16, 22-24)
# spanning the columns. Plug: two prongs (x 22-24, 26-28; y 4-8), body (x 20-30, y 8-16), cable down (x 24-26, y 16-22)
# and left into the frame's middle beam region (x 16-26, y 20-22) — one accent path so the plug reads as one object. F1 = nonzero fill, so overlapping members merge instead of cutting holes.
$parts = @(
    @{ Brush = $ink;    Path = 'F1 M2,6 H4 V26 H2 Z M14,6 H16 V26 H14 Z M2,6 H16 V8 H2 Z M2,14 H16 V16 H2 Z M2,22 H16 V24 H2 Z M0,26 H18 V30 H0 Z' },
    @{ Brush = $accent; Path = 'F1 M22,4 H24 V8 H22 Z M26,4 H28 V8 H26 Z M20,8 H30 V16 H20 Z M24,16 H26 V20 H24 Z M16,20 H26 V22 H16 Z' }
)

function Render-Png([int]$size) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    # Multiples of 16 land every even coordinate on a pixel edge: aliased = crisp. Other sizes need anti-aliasing.
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
    return ,$ms.ToArray()   # the comma keeps the byte[] from being unrolled
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$pngs = @{}
foreach ($s in $sizes) { $pngs[$s] = Render-Png $s }

# ICO container with PNG-compressed images (Windows Vista+; WPF's Window.Icon reads it too).
$ico = Join-Path $OutDir 'HPEtabsMcpBridge.ico'
$stream = [IO.File]::Create($ico)
$writer = New-Object IO.BinaryWriter $stream
try {
    $writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]$sizes.Count)   # ICONDIR: reserved, type 1 = icon, count
    $offset = 6 + 16 * $sizes.Count
    foreach ($s in $sizes) {
        $dim = if ($s -ge 256) { 0 } else { $s }                                                 # 0 means 256
        $writer.Write([Byte]$dim); $writer.Write([Byte]$dim); $writer.Write([Byte]0); $writer.Write([Byte]0)
        $writer.Write([UInt16]1); $writer.Write([UInt16]32)                                       # planes, bits per pixel
        $writer.Write([UInt32]$pngs[$s].Length); $writer.Write([UInt32]$offset)
        $offset += $pngs[$s].Length
    }
    foreach ($s in $sizes) { $writer.Write([byte[]]$pngs[$s]) }
} finally { $writer.Dispose(); $stream.Dispose() }
Write-Host ("{0}  {1} sizes ({2})  {3} bytes" -f $ico, $sizes.Count, ($sizes -join ','), (Get-Item $ico).Length)

$preview = Join-Path $OutDir 'HPEtabsMcpBridge_256.png'
[IO.File]::WriteAllBytes($preview, $pngs[256])
Write-Host ("{0}  {1} bytes" -f $preview, (Get-Item $preview).Length)
