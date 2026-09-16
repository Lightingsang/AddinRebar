# Unattended live check of the AEC tools in a real AutoCAD 2026: starts AutoCAD with the bridge (bridge.scr), ticks
# the opt-in, then runs $Script (default aec-tools-live.py = the read-only tools of phases A + B; run-aec-edit-tools-live.ps1
# passes aec-edit-tools-live.py = the phase-C write tools) over stdio against the server exe on an isolated registry root
# under $OutDir (the user's %AppData% registry is never touched). Default exe = the Debug build, so new seeds are
# tested without republishing; pass -Exe for the published one. Kills only the AutoCAD it started.
#Requires -Version 7.3
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\..\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe'),
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\output\aec-tools-live'),
    [string]$Script = 'aec-tools-live.py',
    [int]$StartupTimeoutSec = 420
)

. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning
if (-not (Test-Path $Exe)) { throw "Server exe not found: $Exe (build HPAutoCad.slnx first, or pass -Exe)" }
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$py = Join-Path $PSScriptRoot $Script
if (-not (Test-Path $py)) { throw "harness script not found: $py" }
$env:PYTHONIOENCODING = 'utf-8'

$registryRoot = Join-Path $OutDir 'registry'
if (Test-Path $registryRoot) { Remove-Item $registryRoot -Recurse -Force }
New-Item -ItemType Directory -Force $registryRoot | Out-Null
$env:HPAUTOCAD_MCP_Registry__LibraryPath = Join-Path $registryRoot 'tools-library'
$env:HPAUTOCAD_MCP_Registry__DbPath = Join-Path $registryRoot 'registry.db'
Write-Host "registry root for this run: $registryRoot"
$logDir = "$env:LOCALAPPDATA\HPAutoCad\McpBridge\logs"

$p = Start-AcadWithBridge (Join-Path $PSScriptRoot 'bridge.scr') $StartupTimeoutSec
$exit = 1
try {
    if (-not $pipeUp) { throw 'bridge pipe never came up' }
    Start-Sleep -Seconds 3
    if (-not (Set-OptIn $true)) { throw 'could not tick the opt-in' }
    $env:HP_HARNESS_ACAD_PID = "$($p.Id)"
    python $py --exe $Exe --out $OutDir
    $exit = $LASTEXITCODE
}
catch {
    Write-Host "FAIL wrapper aborted: $($_.Exception.Message)"
    $exit = 1
}
finally {
    "=== killing acad"
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -Confirm:$false }
}

"=== bridge log: self-check (must mention the aec tolerance — proves HPAutoCad.Aec resolved in the bridge's load context)"
$bridgeLog = Get-ChildItem "$logDir\mcpbridge-*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
if ($bridgeLog) { Get-Content $bridgeLog.FullName -Tail 400 | Select-String -Pattern 'self-check' | Select-Object -Last 1 | ForEach-Object { $_.Line } }
exit $exit
