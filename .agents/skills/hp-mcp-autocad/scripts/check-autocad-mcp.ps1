# Read-only health check of the HPAutoCad MCP chain on this machine: AutoCAD process, the bridge bundle, the named pipe,
# the published server exe and the .mcp.json entry. Touches nothing (no process start/stop, no pipe connection, no file write).
#
#   pwsh .claude/skills/hp-mcp-autocad/scripts/check-autocad-mcp.ps1 [-RepoRoot <path>]
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

# 1. AutoCAD
$acad = @(Get-Process -Name 'acad' -ErrorAction SilentlyContinue)
Report ($acad.Count -ge 1) 'acad.exe running' $(if ($acad.Count) { ($acad | ForEach-Object { "pid $($_.Id) '$($_.MainWindowTitle)'" }) -join '; ' } else { 'start AutoCAD 2026 and open a drawing (no drawing = -32003)' })
if ($acad.Count -gt 1) { Write-Host '[info] more than one AutoCAD: only the first to start owns the pipe; the other reports "pipe in use" in its bridge window' }

# 2. Bridge bundle (autoloaded by AutoCAD from the ApplicationPlugins folder)
$bundle = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle'
$loader = Join-Path $bundle 'Contents\HPAutoCad.McpBridge.Loader.dll'
$aec = Join-Path $bundle 'Contents\Bridge\HPAutoCad.Aec.dll'
Report (Test-Path $loader) 'bridge bundle deployed' $(if (Test-Path $loader) { $bundle } else { "missing $bundle - run 'dotnet build HPAutoCad/HPAutoCad.slnx -c Debug' with AutoCAD closed" })
Report (Test-Path $aec) 'AEC engine beside the bridge (HPAutoCad.Aec.dll)' $(if (Test-Path $aec) { (Get-Item $aec).LastWriteTime.ToString('s') } else { 'stale bundle: every AEC seed fails to compile (CS0246) until the bundle is rebuilt' })

# 3. Pipe
$pipe = '\\.\pipe\hpautocad-mcp-2026'
$pipeUp = [bool]([IO.Directory]::GetFiles('\\.\pipe\') | Where-Object { $_ -ieq $pipe })
Report $pipeUp 'pipe hpautocad-mcp-2026 listening' $(if ($pipeUp) { $pipe } else { 'bridge listener stopped or bundle not loaded (SECURELOAD? ribbon HPAutoCad > MCP > MCP Bridge, or command HPMCPBRIDGE)' })

# 4. Server exe + .mcp.json
$server = Join-Path $RepoRoot 'HPAutoCad\output\HPAutoCad.Mcp.Server\HPAutoCad.Mcp.Server.exe'
Report (Test-Path $server) 'published server exe exists' $(if (Test-Path $server) { "$server ($((Get-Item $server).LastWriteTime.ToString('s')))" } else { $server })
$mcpJson = Join-Path $RepoRoot '.mcp.json'
if (Test-Path $mcpJson) {
    try {
        $cfg = Get-Content $mcpJson -Raw | ConvertFrom-Json
        $entry = $cfg.mcpServers.'hprebar-autocad'
        if ($entry) {
            $cmdOk = Test-Path $entry.command
            $verOk = $entry.env.'HPAUTOCAD_MCP_Bridge__HostVersion' -eq '2026'
            Report ($cmdOk -and $verOk) '.mcp.json hprebar-autocad entry' ("command={0} (exists: {1}); HostVersion={2}" -f $entry.command, $cmdOk, $entry.env.'HPAUTOCAD_MCP_Bridge__HostVersion')
        } else { Report $false '.mcp.json hprebar-autocad entry' "add 'hprebar-autocad' -> $server with env HPAUTOCAD_MCP_Bridge__HostVersion=2026 (never commit .mcp.json)" }
    } catch { Report $false '.mcp.json parses' $_.Exception.Message }
} else { Report $false '.mcp.json present' $mcpJson }

# 5. Running servers (informational)
$servers = @(Get-Process -Name 'HPAutoCad.Mcp.Server' -ErrorAction SilentlyContinue)
Write-Host ("[info] HPAutoCad.Mcp.Server processes: {0} (one per Claude Code session; they lock the published exe - rename it before republishing)" -f $servers.Count)

# 6. Latest bridge log: the self-check line proves Roslyn + HPAutoCad.Aec resolved in the bridge's load context
$logDir = Join-Path $env:LOCALAPPDATA 'HPAutoCad\McpBridge\logs'
if (Test-Path $logDir) {
    $log = Get-ChildItem $logDir -Filter '*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($log) {
        $tail = Get-Content $log.FullName -Tail 400
        $selfCheck = $tail | Where-Object { $_ -match 'self-check OK' } | Select-Object -Last 1
        Report ([bool]$selfCheck) 'bridge scripting self-check OK (latest log)' $(if ($selfCheck) { $selfCheck.Trim() } else { "no 'self-check OK' line in $($log.Name)" })
        if ($selfCheck -and $selfCheck -notmatch 'aec tolerance') { Report $false 'self-check mentions the AEC engine' 'the bridge ran without HPAutoCad.Aec: rebuild the bundle' }
    }
} else { Write-Host "[info] no bridge log folder yet ($logDir)" }

Write-Host ''
if ($problems -eq 0) { Write-Host 'All checks OK - call get_autocad_context next (executionEnabled must be true: tick "Allow AI code execution" in the bridge window).' -ForegroundColor Green; exit 0 }
Write-Host ("{0} problem(s) - fix the FAIL lines above, then call get_autocad_context." -f $problems) -ForegroundColor Yellow
exit 1
