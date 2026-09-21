$acad = 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe'
$dll = 'C:\Users\STR-HP03\AppData\Roaming\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\HPAutoCad.Loader.dll'
$logDir = 'C:\Users\STR-HP03\AppData\Local\HPAutoCad\testnetload'
New-Item -ItemType Directory -Force $logDir | Out-Null
$scr = Join-Path $logDir 'testnetload.scr'
Set-Content $scr @"
LOGFILEPATH
$logDir\
LOGFILEON
CMDECHO
1
SECURELOAD
0
_.NETLOAD
"$dll"
HPGEOINFO
_.QUIT
_Y
"@
$p = Start-Process $acad -ArgumentList @('/nologo', '/product', 'ACAD', '/b', "`"$scr`"") -PassThru
$sw = [Diagnostics.Stopwatch]::StartNew()
while (-not $p.HasExited -and $sw.Elapsed.TotalSeconds -lt 30) {
    Start-Sleep -Seconds 1
}
if (-not $p.HasExited) {
    Stop-Process -Id $p.Id -Force
}
Write-Host "Process exited. Checking logs..."
Get-ChildItem $logDir
