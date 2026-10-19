Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)

$targetPath = "C:\Users\STR-HP03\.gemini\antigravity\brain\4cb249bf-78d3-4e3f-aa18-e0869d1403cc\revit_tang1_beams_verified.png"
$bitmap.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)

$graphics.Dispose()
$bitmap.Dispose()

Write-Host "Screenshot saved to $targetPath ($($bounds.Width)x$($bounds.Height))"
