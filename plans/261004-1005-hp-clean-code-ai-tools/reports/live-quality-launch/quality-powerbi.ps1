# Power BI run of the script-quality live check: Power BI Desktop with a blank report, the Debug bridge app, UIA for
# listener / opt-in / connect, then the shared Python harness. Windows PowerShell 5.1.
param([switch]$DescribeOnly)
$ErrorActionPreference = 'Stop'
$repo = 'F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar'
. (Join-Path $PSScriptRoot 'bridge-ui.ps1')
if (Get-Process PBIDesktop -ErrorAction SilentlyContinue) { throw 'a Power BI Desktop is already running - not ours' }
if (Get-Process HPPowerBi.McpBridge -ErrorAction SilentlyContinue) { throw 'a Power BI bridge is already running' }

$pbi = Start-Process 'C:\Program Files\Microsoft Power BI Desktop\bin\PBIDesktop.exe' -PassThru
"PBIDesktop pid $($pbi.Id) started $(Get-Date -Format HH:mm:ss)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 300) { Start-Sleep 3; $pbi.Refresh(); if ($pbi.HasExited) { throw "PBIDesktop exited ($($pbi.ExitCode))" }; if ($pbi.MainWindowTitle -match 'Untitled') { break } }
"PBIDesktop window '$($pbi.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds) s"
Start-Sleep -Seconds 25
"msmdsrv pids: $((Get-Process msmdsrv -ErrorAction SilentlyContinue | ForEach-Object { $_.Id }) -join ', ')"

$bridge = Start-Process (Join-Path $repo 'HPPowerBi\HPPowerBi.McpBridge\bin\Debug\net8.0-windows\HPPowerBi.McpBridge.exe') -PassThru
try {
    $w = Get-AppWindow $bridge.Id
    Start-Sleep -Seconds 4
    Describe-Controls $w
    if ($DescribeOnly) { return }
    $null = Invoke-ButtonNamed $w '^Start'
    Set-ExecutionOptIn $w $true
    $null = Invoke-ButtonNamed $w '^(Connect|Attach|Refresh)'
    Start-Sleep -Seconds 8
    "status texts: $((Read-Texts $w 'onnect|ttach|port|Power BI') -join ' | ')"
    & python (Join-Path $repo 'McpShared\tools\live-verify-quality.py') --host powerbi --out (Join-Path $repo 'plans\261004-1005-hp-clean-code-ai-tools\reports\live-quality')
    "harness exit $LASTEXITCODE"
    "pbi $((Get-Item 'C:\Program Files\Microsoft Power BI Desktop\bin\PBIDesktop.exe').VersionInfo.ProductVersion)"
}
finally {
    try { Set-ExecutionOptIn (Get-AppWindow $bridge.Id 5) $false } catch { }
    if (-not $bridge.HasExited) { $null = $bridge.CloseMainWindow(); if (-not $bridge.WaitForExit(10000)) { Stop-Process -Id $bridge.Id -Force } }
    $pbi.Refresh()
    if (-not $pbi.HasExited) { $null = $pbi.CloseMainWindow(); if (-not $pbi.WaitForExit(30000)) { "PBIDesktop did not exit on close; killing"; Stop-Process -Id $pbi.Id -Force } }
    "Power BI and bridge closed"
}
