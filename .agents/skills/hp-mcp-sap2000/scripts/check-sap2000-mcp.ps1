# Read-only health check of the HPSap2000 MCP chain on this machine: SAP2000 process, bridge app, named pipe,
# published exes and the .mcp.json entry. Touches nothing (no process start/stop, no pipe connection, no file write).
#
#   pwsh .agents/skills/hp-mcp-sap2000/scripts/check-sap2000-mcp.ps1 [-RepoRoot <path>]
#
# Exit code 0 = every check OK, 1 = at least one problem (the lines say which).
param([string]$RepoRoot = '')

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path }
$problems = 0
function Report([bool]$ok, [string]$what, [string]$detail) {
    $mark = if ($ok) { 'OK  ' } else { 'FAIL' }
    Write-Host ("[{0}] {1}" -f $mark, $what) -ForegroundColor ($(if ($ok) { 'Green' } else { 'Red' }))
    if ($detail) { Write-Host ("       {0}" -f $detail) }
    if (-not $ok) { $script:problems++ }
}

# 1. SAP2000
$sap = @(Get-Process -Name 'SAP2000' -ErrorAction SilentlyContinue)
Report ($sap.Count -ge 1) 'SAP2000.exe running' $(if ($sap.Count) { ($sap | ForEach-Object { "pid $($_.Id) '$($_.MainWindowTitle)'" }) -join '; ' } else { 'start SAP2000 27 from its shortcut, then File > Open the model (a double-clicked .SDB may not register the API object)' })
if ($sap.Count -gt 1) { Report $false 'exactly one SAP2000 instance' 'with two instances GetObject attaches to the newer one - close the other or set Tools > Active Instance for API' }

# 2. Bridge app
$bridge = @(Get-Process -Name 'HPSap2000.McpBridge' -ErrorAction SilentlyContinue)
$publishedBridge = Join-Path $RepoRoot 'HPSap2000\output\HPSap2000.McpBridge\HPSap2000.McpBridge.exe'
Report ($bridge.Count -ge 1) 'HPSap2000.McpBridge.exe running' $(if ($bridge.Count) { ($bridge | ForEach-Object { "pid $($_.Id) $($_.Path)" }) -join '; ' } else { "start $publishedBridge, click Start listener, Attach, tick 'Allow AI code execution'" })
if ($bridge.Count -gt 1) { Report $false 'exactly one bridge instance' 'the second one cannot own the pipe' }
Report (Test-Path $publishedBridge) 'published bridge exe exists' $publishedBridge

# 3. Pipe
$pipe = '\\.\pipe\hpsap2000-mcp-27'
$pipeUp = [bool]([IO.Directory]::GetFiles('\\.\pipe\') | Where-Object { $_ -ieq $pipe })
Report $pipeUp "pipe hpsap2000-mcp-27 listening" $(if ($pipeUp) { $pipe } else { 'bridge listener stopped or bridge not running' })

# 4. Server exe + .mcp.json
$server = Join-Path $RepoRoot 'HPSap2000\output\HPSap2000.Mcp.Server\HPSap2000.Mcp.Server.exe'
Report (Test-Path $server) 'published server exe exists' $server
$mcpJson = Join-Path $RepoRoot '.mcp.json'
if (Test-Path $mcpJson) {
    try {
        $cfg = Get-Content $mcpJson -Raw | ConvertFrom-Json
        $entry = $cfg.mcpServers.'hprebar-sap2000'
        if ($entry) {
            $cmdOk = Test-Path $entry.command
            $verOk = $entry.env.'HPSAP2000_MCP_Bridge__HostVersion' -eq '27'
            Report ($cmdOk -and $verOk) '.mcp.json hprebar-sap2000 entry' ("command={0} (exists: {1}); HostVersion={2}" -f $entry.command, $cmdOk, $entry.env.'HPSAP2000_MCP_Bridge__HostVersion')
        } else { Report $false '.mcp.json hprebar-sap2000 entry' "add 'hprebar-sap2000' -> $server with env HPSAP2000_MCP_Bridge__HostVersion=27 (never commit .mcp.json)" }
    } catch { Report $false '.mcp.json parses' $_.Exception.Message }
} else { Report $false '.mcp.json present' $mcpJson }

# 5. Running servers (informational)
$servers = @(Get-Process -Name 'HPSap2000.Mcp.Server' -ErrorAction SilentlyContinue)
Write-Host ("[info] HPSap2000.Mcp.Server processes: {0} (one per AI coding session; they lock the published exe)" -f $servers.Count)

# 6. Latest bridge log line about the self-check (proves Roslyn + SAP2000v1 resolved)
$logDir = Join-Path $env:LOCALAPPDATA 'HPSap2000\McpBridge\logs'
if (Test-Path $logDir) {
    $log = Get-ChildItem $logDir -Filter '*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($log) {
        $checkLine = Get-Content $log.FullName | Where-Object { $_ -match 'Scripting self-check OK' } | Select-Object -Last 1
        Report ([bool]$checkLine) 'scripting self-check logged' $(if ($checkLine) { $checkLine.Trim() } else { "no 'Scripting self-check OK' line in $($log.Name)" })
    }
}

exit $(if ($problems -eq 0) { 0 } else { 1 })
