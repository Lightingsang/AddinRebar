$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
$scr = Join-Path $PSScriptRoot 'probe.scr'
Set-Content $scr @"
(princ (strcat "`n===APIAUTOLOADER=" (itoa (getvar "APIAUTOLOADER")) "===`n"))
(princ (strcat "===ACADVER=" (getvar "ACADVER") "===`n"))
(princ (strcat "===PRODUCT=" (getvar "PRODUCT") "===`n"))
(princ (strcat "===SECURELOAD=" (itoa (getvar "SECURELOAD")) "===`n"))
_.QUIT
_Y
"@
$p = Start-Process $acad -ArgumentList @('/nologo', '/product', 'ACAD', '/b', "`"$scr`"") -PassThru
$sw = [Diagnostics.Stopwatch]::StartNew()
while (-not $p.HasExited -and $sw.Elapsed.TotalSeconds -lt 25) {
    Start-Sleep -Seconds 1
}
if (-not $p.HasExited) {
    Stop-Process -Id $p.Id -Force
}
Remove-Item $scr -ErrorAction SilentlyContinue
