# Unattended bridge run: starts Civil 3D 2026 with bridge.scr (HPC3DMCPBRIDGE + HPC3DMCPSTART), answers SECURELOAD,
# waits for the pipe, ticks the "Allow AI code execution" box through UI Automation (the opt-in is never
# persisted by design), runs the Python pipe scenarios, then the opt-in-off and no-document checks, and
# finally kills Civil 3D (nothing in the bridge may quit the host).
param([int]$StartupTimeoutSec = 420)

# The harness closes drawings without saving, types into Civil 3D through COM and kills acad.exe at the end:
# it must only ever touch the acad.exe it started itself.
. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning

$log = "$env:LOCALAPPDATA\HPCivil3d\McpBridge\logs"
$scr = Join-Path $PSScriptRoot 'bridge.scr'
$py = Join-Path $PSScriptRoot 'pipe-scenarios.py'
$bridgeLog = Get-ChildItem "$log\mcpbridge-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
$bridgeLines = if ($bridgeLog) { (Get-Content $bridgeLog.FullName).Count } else { 0 }

# One tutorial drawing with alignments and a surface, copied so nothing of the install is ever touched; a stale lock
# from a killed session would make the next open read-only, so the sweep comes first.
$scene = Join-Path $PSScriptRoot '..\..\output\live-verify\scene'
New-Item -ItemType Directory -Force $scene | Out-Null
Get-ChildItem $scene -Force -Include '*.dwl', '*.dwl2' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue   # lock files are hidden
$tutorials = 'C:\Program Files\Autodesk\AutoCAD 2026\C3D\Help\Civil Tutorials\Drawings'
$drawing = Join-Path $scene 'Align-7C.dwg'
if (Test-Path (Join-Path $tutorials 'Align-7C.dwg')) { Copy-Item (Join-Path $tutorials 'Align-7C.dwg') $drawing -Force } else { "scene: Align-7C.dwg missing in the tutorials folder" }

$p = Start-AcadWithBridge $scr $StartupTimeoutSec

$env:HP_HARNESS_KEYS = '1'
$env:PYTHONIOENCODING = 'utf-8'
$failures = 0
function Step([string]$name, [scriptblock]$run) { & $run; if ($LASTEXITCODE -ne 0) { $script:failures++; Write-Host "FAIL step '$name' exit $LASTEXITCODE" } }
try {
    if (-not $pipeUp) { $failures++; Write-Host "FAIL the bridge pipe never came up" }
    else {
        Start-Sleep -Seconds 3
        "=== opt-in OFF: expect -32001"
        if (-not (Set-OptIn $false)) { $failures++; Write-Host "FAIL opt-in could not be unticked" }
        Step 'disabled' { python $py --only disabled }
        "=== opt-in ON: main scenarios on Align-7C.dwg (alignments, a surface, Meters)"
        if (Set-OptIn $true) {
            if (Test-Path $drawing) { python $py --com "`$a.Documents.Open('$drawing') | Out-Null; 'opened ' + `$a.ActiveDocument.Name"; Start-Sleep -Seconds 6 }
            Step 'main' { python $py }
            "=== no document: expect -32003"
            python $py --com "`$a.ActiveDocument.Close(`$false); 'closed via COM, docs left: ' + `$a.Documents.Count"
            Start-Sleep -Seconds 3
            Step 'nodoc' { python $py --only nodoc }
            "=== busy: LINE waiting for input, expect -32002 after the 8 s grace"
            python $py --com "`$null = `$a.Documents.Add(); 'new drawing, docs: ' + `$a.Documents.Count"
            Start-Sleep -Seconds 3
            Step 'busy' { python $py --only busy }
        }
        else { $failures++; Write-Host "FAIL opt-in could not be ticked" }
    }
}
finally {
    "=== killing what we started"
    Stop-Acad $p
    Get-ChildItem $scene -Force -Include '*.dwl', '*.dwl2' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue   # lock files are hidden
    Remove-Item Env:HP_HARNESS_ACAD_PID, Env:HP_HARNESS_KEYS -ErrorAction SilentlyContinue
}

"=== audit lines"
Get-ChildItem "$env:APPDATA\HPCivil3d\McpBridge\audit\*.log" -ErrorAction SilentlyContinue | % { "$($_.Name): $((Get-Content $_.FullName).Count) lines" }
"=== new bridge log lines (filtered)"
$bridgeLog = Get-ChildItem "$log\mcpbridge-*.log" | Sort-Object LastWriteTime | Select-Object -Last 1
Get-Content $bridgeLog.FullName | Select-Object -Skip $bridgeLines | Select-String -Pattern 'starting|ready|listening|connected|refused|failed|Error|WRN|ERR|window|stopped' | Select-Object -First 60
"=== harness steps failed: $failures"
exit $failures
