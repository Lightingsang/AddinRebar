# Robot run of the script-quality live check: Robot 2026 started with no project, the Debug bridge app, UIA for
# listener / opt-in / Attach, then the shared Python harness. Windows PowerShell 5.1.
$ErrorActionPreference = 'Stop'
$repo = 'F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar'
. (Join-Path $PSScriptRoot 'bridge-ui.ps1')
if (Get-Process robot -ErrorAction SilentlyContinue) { throw 'a Robot is already running - not ours' }
if (Get-Process HPRobot.McpBridge -ErrorAction SilentlyContinue) { throw 'a Robot bridge is already running' }

$robot = Start-Process 'C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.EXE' -PassThru
"Robot pid $($robot.Id) started $(Get-Date -Format HH:mm:ss)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 240) { Start-Sleep 3; $robot.Refresh(); if ($robot.HasExited) { throw "Robot exited ($($robot.ExitCode))" }; if ($robot.MainWindowTitle -match 'Robot') { break } }
"Robot window '$($robot.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds) s"
Start-Sleep -Seconds 20

$bridge = Start-Process (Join-Path $repo 'HPRobot\HPRobot.McpBridge\bin\Debug\net8.0-windows\HPRobot.McpBridge.exe') -PassThru
try {
    $w = Get-AppWindow $bridge.Id
    Start-Sleep -Seconds 3
    Describe-Controls $w
    $null = Invoke-ButtonNamed $w '^Start'
    Set-ExecutionOptIn $w $true
    $null = Invoke-ButtonNamed $w '^Attach$'
    Start-Sleep -Seconds 8
    "attach texts: $((Read-Texts $w 'ttach|Robot') -join ' | ')"
    & python (Join-Path $repo 'McpShared\tools\live-verify-quality.py') --host robot --out (Join-Path $repo 'plans\261004-1005-hp-clean-code-ai-tools\reports\live-quality')
    "harness exit $LASTEXITCODE"
    "robot $((Get-Item 'C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.EXE').VersionInfo.ProductVersion)"
}
finally {
    try { Set-ExecutionOptIn (Get-AppWindow $bridge.Id 5) $false } catch { }
    if (-not $bridge.HasExited) { $null = $bridge.CloseMainWindow(); if (-not $bridge.WaitForExit(10000)) { Stop-Process -Id $bridge.Id -Force } }
    $robot.Refresh()
    if (-not $robot.HasExited) { $null = $robot.CloseMainWindow(); if (-not $robot.WaitForExit(30000)) { "Robot did not exit on close; killing"; Stop-Process -Id $robot.Id -Force } }
    "Robot and bridge closed"
}
