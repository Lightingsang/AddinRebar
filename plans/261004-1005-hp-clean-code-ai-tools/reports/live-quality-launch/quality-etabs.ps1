# ETABS run of the script-quality live check: ETABS 22 started with no model (untitled), the Debug bridge app,
# the HPEtabs harness helpers for listener / opt-in / Attach, then the shared Python harness. Windows PowerShell 5.1.
$ErrorActionPreference = 'Stop'
$repo = 'F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar'
. (Join-Path $repo 'HPEtabs\tools\harness\harness-common.ps1')
Assert-NoBridgeRunning
if (Get-Process ETABS -ErrorAction SilentlyContinue) { throw 'an ETABS is already running - not ours' }

$etabs = Start-Process 'C:\Program Files\Computers and Structures\ETABS 22\ETABS.exe' -PassThru
"ETABS pid $($etabs.Id) started $(Get-Date -Format HH:mm:ss)"
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 180) { Start-Sleep 3; $etabs.Refresh(); if ($etabs.MainWindowTitle -match 'ETABS') { break } }
"ETABS window '$($etabs.MainWindowTitle)' after $([int]$sw.Elapsed.TotalSeconds) s"
Start-Sleep -Seconds 15

$exe = Join-Path $repo 'HPEtabs\HPEtabs.McpBridge\bin\Debug\net8.0-windows\HPEtabs.McpBridge.exe'
$bridge = Start-Bridge $exe
try {
    Start-Sleep -Seconds 2
    if (-not (Wait-Pipe 5)) { Invoke-BridgeButton 'ToggleListener' | Out-Null; if (-not (Wait-Pipe 30)) { throw 'pipe never appeared' } }
    if (-not (Set-OptIn 'AllowExecution' $true)) { throw 'could not tick Allow AI code execution' }
    if (-not (Invoke-BridgeButton 'Attach')) { throw 'Attach button not available' }
    "attach state: $(Wait-BridgeText 'AttachState' '^Attached to ETABS' 60)"
    & python (Join-Path $repo 'McpShared\tools\live-verify-quality.py') --host etabs --out (Join-Path $repo 'plans\261004-1005-hp-clean-code-ai-tools\reports\live-quality')
    "harness exit $LASTEXITCODE"
    "etabs $((Get-Item 'C:\Program Files\Computers and Structures\ETABS 22\ETABS.exe').VersionInfo.ProductVersion)"
}
finally {
    try { Set-OptIn 'AllowExecution' $false | Out-Null } catch { }
    Stop-Bridge $bridge
    $etabs.Refresh()
    if (-not $etabs.HasExited) {
        $null = $etabs.CloseMainWindow()
        if (-not $etabs.WaitForExit(30000)) { "ETABS did not exit on close; killing"; Stop-Process -Id $etabs.Id -Force }
    }
    "ETABS exited"
}
