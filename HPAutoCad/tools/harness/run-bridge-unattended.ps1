# Unattended phase-2 run: starts AutoCAD with bridge.scr (HPMCPBRIDGE + HPMCPSTART), answers SECURELOAD,
# waits for the pipe, ticks the "Allow AI code execution" box through UI Automation (the opt-in is never
# persisted by design), runs the Python pipe scenarios, then the opt-in-off and no-document checks, and
# finally kills AutoCAD (nothing in the bridge may quit the host).
param([int]$StartupTimeoutSec = 420)

# The harness closes drawings without saving, types into AutoCAD through COM and kills acad.exe at the end:
# it must only ever touch the AutoCAD it started itself.
. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning

$log = "$env:LOCALAPPDATA\HPAutoCad\McpBridge\logs"
$scr = Join-Path $PSScriptRoot 'bridge.scr'
$py = Join-Path $PSScriptRoot 'pipe-scenarios.py'
$bridgeLog = Get-ChildItem "$log\mcpbridge-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
$bridgeLines = if ($bridgeLog) { (Get-Content $bridgeLog.FullName).Count } else { 0 }

$p = Start-AcadWithBridge $scr $StartupTimeoutSec

$env:HP_HARNESS_KEYS = '1'
$env:PYTHONIOENCODING = 'utf-8'
try {
    if ($pipeUp) {
        Start-Sleep -Seconds 3
        "=== opt-in OFF: expect -32001"
        $null = Set-OptIn $false
        python $py --only disabled
        "=== opt-in ON: main scenarios"
        if (Set-OptIn $true) {
            python $py
            "=== no document: expect -32003"
            python $py --com "`$a.ActiveDocument.Close(`$false); 'closed via COM, docs left: ' + `$a.Documents.Count"
            Start-Sleep -Seconds 3
            python $py --only nodoc
            "=== busy: LINE waiting for input, expect -32002 after the 10 s grace"
            python $py --com "`$null = `$a.Documents.Add(); 'new drawing, docs: ' + `$a.Documents.Count"
            Start-Sleep -Seconds 3
            python $py --only busy
        }
    }
}
finally {
    "=== killing acad"
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -Confirm:$false }
}

"=== audit lines"
Get-ChildItem "$env:APPDATA\HPAutoCad\McpBridge\audit\*.log" -ErrorAction SilentlyContinue | % { "$($_.Name): $((Get-Content $_.FullName).Count) lines" }
"=== new bridge log lines (filtered)"
$bridgeLog = Get-ChildItem "$log\mcpbridge-*.log" | Sort-Object LastWriteTime | Select-Object -Last 1
Get-Content $bridgeLog.FullName | Select-Object -Skip $bridgeLines | Select-String -Pattern 'starting|ready|listening|connected|refused|failed|Error|WRN|ERR|window|stopped' | Select-Object -First 60
