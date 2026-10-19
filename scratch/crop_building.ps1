Add-Type -AssemblyName System.Drawing

$src = "C:\Users\STR-HP03\.gemini\antigravity\brain\4cb249bf-78d3-4e3f-aa18-e0869d1403cc\he_dam_tang1_cad_verified.jpg"
$bmp = New-Object System.Drawing.Bitmap $src

# Bounding box in 3840x2880:
# From our visual inspection, the building frame is in:
# X: approx 350 to 750 (width ~ 400)
# Y: approx 500 to 800 (height ~ 300)
# Let's crop around X: 300 to 800, Y: 450 to 850 with padding!

$x = [int]($bmp.Width * 0.08)
$y = [int]($bmp.Height * 0.15)
$w = [int]($bmp.Width * 0.12)
$h = [int]($bmp.Height * 0.12)

# Make sure bounds are valid
$rect = New-Object System.Drawing.Rectangle $x, $y, $w, $h
$cropped = $bmp.Clone($rect, $bmp.PixelFormat)

$dst = "C:\Users\STR-HP03\.gemini\antigravity\brain\4cb249bf-78d3-4e3f-aa18-e0869d1403cc\he_dam_tang1_closeup.png"
$cropped.Save($dst, [System.Drawing.Imaging.ImageFormat]::Png)

$cropped.Dispose()
$bmp.Dispose()

Write-Host "Cropped closeup saved to $dst"
