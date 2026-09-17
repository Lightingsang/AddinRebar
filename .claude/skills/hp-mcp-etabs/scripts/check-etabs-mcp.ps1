# Read-only health check of the HPEtabs MCP chain on this machine: ETABS process, bridge app, named pipe,
# published exes and the .mcp.json entry. Touches nothing (no process start/stop, no pipe connection, no file write).
#
#   pwsh .claude/skills/hp-mcp-etabs/scripts/check-etabs-mcp.ps1 [-RepoRoot <path>]
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

# 1. ETABS
$etabs = @(Get-Process -Name 'ETABS' -ErrorAction SilentlyContinue)
Report ($etabs.Count -ge 1) 'ETABS.exe running' $(if ($etabs.Count) { ($etabs | ForEach-Object { "pid $($_.Id) '$($_.MainWindowTitle)'" }) -join '; ' } else { 'start ETABS 22 from its shortcut, then File > Open the model (a double-clicked .EDB may not register the API object)' })
if ($etabs.Count -gt 1) { Report $false 'exactly one ETABS instance' 'with two instances GetObject attaches to the newer one - close the other or set Tools > Active Instance for API' }

# 2. Bridge app
$bridge = @(Get-Process -Name 'HPEtabs.McpBridge' -ErrorAction SilentlyContinue)
$publishedBridge = Join-Path $RepoRoot 'HPEtabs\output\HPEtabs.McpBridge\HPEtabs.McpBridge.exe'
Report ($bridge.Count -ge 1) 'HPEtabs.McpBridge.exe running' $(if ($bridge.Count) { ($bridge | ForEach-Object { "pid $($_.Id) $($_.Path)" }) -join '; ' } else { "start $publishedBridge, click Start listener, Attach, tick 'Allow AI code execution'" })
if ($bridge.Count -gt 1) { Report $false 'exactly one bridge instance' 'the second one cannot own the pipe' }
Report (Test-Path $publishedBridge) 'published bridge exe exists' $publishedBridge

# 3. Pipe
$pipe = '\\.\pipe\hpetabs-mcp-22'
# Test-Path on a pipe path works in pwsh 7 only; listing the pipe directory works on Windows PowerShell 5.1 too.
$pipeUp = [bool]([IO.Directory]::GetFiles('\\.\pipe\') | Where-Object { $_ -ieq $pipe })
Report $pipeUp "pipe hpetabs-mcp-22 listening" $(if ($pipeUp) { $pipe } else { 'bridge listener stopped or bridge not running' })

# 4. Server exe + .mcp.json
$server = Join-Path $RepoRoot 'HPEtabs\output\HPEtabs.Mcp.Server\HPEtabs.Mcp.Server.exe'
Report (Test-Path $server) 'published server exe exists' $server
$mcpJson = Join-Path $RepoRoot '.mcp.json'
if (Test-Path $mcpJson) {
    try {
        $cfg = Get-Content $mcpJson -Raw | ConvertFrom-Json
        $entry = $cfg.mcpServers.'hprebar-etabs'
        if ($entry) {
            $cmdOk = Test-Path $entry.command
            $verOk = $entry.env.'HPETABS_MCP_Bridge__HostVersion' -eq '22'
            Report ($cmdOk -and $verOk) '.mcp.json hprebar-etabs entry' ("command={0} (exists: {1}); HostVersion={2}" -f $entry.command, $cmdOk, $entry.env.'HPETABS_MCP_Bridge__HostVersion')
        } else { Report $false '.mcp.json hprebar-etabs entry' "add 'hprebar-etabs' -> $server with env HPETABS_MCP_Bridge__HostVersion=22 (never commit .mcp.json)" }
    } catch { Report $false '.mcp.json parses' $_.Exception.Message }
} else { Report $false '.mcp.json present' $mcpJson }

# 5. Running servers (informational)
$servers = @(Get-Process -Name 'HPEtabs.Mcp.Server' -ErrorAction SilentlyContinue)
Write-Host ("[info] HPEtabs.Mcp.Server processes: {0} (one per Claude Code session; they lock the published exe)" -f $servers.Count)

# 6. Latest bridge log line about the self-check (proves Roslyn + ETABSv1 resolved)
$logDir = Join-Path $env:LOCALAPPDATA 'HPEtabs\McpBridge\logs'
if (Test-Path $logDir) {
    $log = Get-ChildItem $logDir -Filter '*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($log) {
        $tail = Get-Content $log.FullName -Tail 400
        $selfCheck = $tail | Where-Object { $_ -match 'self-check OK' } | Select-Object -Last 1
        $attach = $tail | Where-Object { $_ -match 'Attached to ETABS|not registered for the API|Detached' } | Select-Object -Last 1
        Report ([bool]$selfCheck) 'bridge scripting self-check OK (latest log)' $(if ($selfCheck) { $selfCheck.Trim() } else { "no 'self-check OK' line in $($log.Name)" })
        if ($attach) { Write-Host ("[info] last attach line: {0}" -f $attach.Trim()) }
    }
} else { Write-Host "[info] no bridge log folder yet ($logDir)" }

Write-Host ''
if ($problems -eq 0) { Write-Host 'All checks OK - call get_etabs_context next.' -ForegroundColor Green; exit 0 }
Write-Host ("{0} problem(s) - fix the FAIL lines above, then call get_etabs_context." -f $problems) -ForegroundColor Yellow
exit 1
