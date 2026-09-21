Add-Type -AssemblyName System.Drawing

$dir = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\output\geolink-verify"
$pngs = Get-ChildItem $dir -Filter *.png

foreach ($f in $pngs) {
    $bmp = [System.Drawing.Bitmap]::FromFile($f.FullName)
    $w = $bmp.Width
    $h = $bmp.Height
    $format = $bmp.PixelFormat.ToString()
    
    $colors = New-Object System.Collections.Generic.HashSet[string]
    $stepX = [Math]::Max(1, [int]($w / 25))
    $stepY = [Math]::Max(1, [int]($h / 25))
    
    for ($x = 0; $x -lt $w; $x += $stepX) {
        for ($y = 0; $y -lt $h; $y += $stepY) {
            $c = $bmp.GetPixel($x, $y)
            $hex = "{0:X2}{1:X2}{2:X2}" -f $c.R, $c.G, $c.B
            $colors.Add($hex) | Out-Null
        }
    }
    
    $bmp.Dispose()
    
    [PSCustomObject]@{
        Name = $f.Name
        SizeKB = [Math]::Round($f.Length / 1024, 1)
        Dimensions = "${w}x${h}"
        PixelFormat = $format
        SampledDistinctColors = $colors.Count
    } | Format-List
}
