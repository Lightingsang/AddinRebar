# Renders the Ribbon icons of the HPNavis MCP bridge from one vector glyph into the 16x16 and 32x32 PNGs that
# Navisworks reads ([Command(Icon=..., LargeIcon=...)] and the layout XAML accept image files only). Every
# coordinate of the glyph is even, so the 16-px render is an exact half of the 32-px one: no anti-aliased fringe,
# crisp on a 100 % DPI Ribbon. Re-run after changing a glyph and commit the PNGs.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPNavis/tools/icons/render-ribbon-icons.ps1
#
# Windows PowerShell 5.1 (STA by default, which WPF rendering needs); writes HPNavis/HPNavis.McpBridge/Ribbon/Images/.
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

if (-not $OutDir) { $OutDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..\HPNavis.McpBridge\Ribbon\Images')).Path }

# Ink = the repo's line-art grey, accent = its blue (Color.Accent #0696D7). Navisworks 2026 has one (light) theme.
$ink = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x3C, 0x3C, 0x3C))
$accent = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(0x06, 0x96, 0xD7))
$ink.Freeze(); $accent.Freeze()

# MCP Bridge: a window with a title bar (the status window) holding a plug (the connection the bridge offers).
# 32x32 box; even-odd fill turns the inner rectangle of the frame into a hole. Plug = two prongs, body, cable.
$glyphs = @{
    'McpBridge' = @(
        @{ Brush = $ink;    Path = 'M2,4 H30 V28 H2 Z M4,8 H28 V26 H4 Z' },
        @{ Brush = $accent; Path = 'M10,10 H12 V14 H10 Z M20,10 H22 V14 H20 Z M8,14 H24 V20 H8 Z M14,20 H18 V24 H14 Z' }
    )
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
foreach ($name in $glyphs.Keys) {
    foreach ($size in 16, 32) {
        $visual = New-Object System.Windows.Media.DrawingVisual
        [System.Windows.Media.RenderOptions]::SetEdgeMode($visual, [System.Windows.Media.EdgeMode]::Aliased)
        $dc = $visual.RenderOpen()
        $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform ($size / 32.0), ($size / 32.0)))
        foreach ($part in $glyphs[$name]) {
            $dc.DrawGeometry($part.Brush, $null, [System.Windows.Media.Geometry]::Parse($part.Path))
        }
        $dc.Pop()
        $dc.Close()

        $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
        $bitmap.Render($visual)
        $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
        $path = Join-Path $OutDir ("{0}_{1}.png" -f $name, $size)
        $stream = [IO.File]::Create($path)
        try { $encoder.Save($stream) } finally { $stream.Dispose() }
        Write-Host ("{0}  {1}x{1}  {2} bytes" -f $path, $size, (Get-Item $path).Length)
    }
}
