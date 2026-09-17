# Read-only health check of the HPNavis MCP chain on this machine: Navisworks (Roamer.exe) process, the bridge plugin folder,
# the named pipe, the published server exe, the .mcp.json entry and the bridge's own self-check line. Touches nothing (no
# process start/stop, no pipe connection, no file write).
#
#   pwsh .claude/skills/hp-mcp-navisworks/scripts/check-navisworks-mcp.ps1 [-RepoRoot <path>] [-NavisVersion 2026]
#
# Exit code 0 = every check OK, 1 = at least one problem (the lines say which).
param([string]$RepoRoot = '', [int]$NavisVersion = 2026)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path }
$problems = 0
function Report([bool]$ok, [string]$what, [string]$detail) {
    $mark = if ($ok) { 'OK  ' } else { 'FAIL' }
    Write-Host ("[{0}] {1}" -f $mark, $what) -ForegroundColor ($(if ($ok) { 'Green' } else { 'Red' }))
    if ($detail) { Write-Host ("       {0}" -f $detail) }
    if (-not $ok) { $script:problems++ }
}

# 1. Navisworks (Roamer.exe is the Navisworks Manage / Simulate process)
$roamer = @(Get-Process -Name 'Roamer' -ErrorAction SilentlyContinue)
Report ($roamer.Count -ge 1) 'Roamer.exe (Navisworks) running' $(if ($roamer.Count) { ($roamer | ForEach-Object { "pid $($_.Id) '$($_.MainWindowTitle)'" }) -join '; ' } else { "start Navisworks Manage $NavisVersion and open a model (no model = -32003)" })
if ($roamer.Count -gt 1) { Write-Host '[info] more than one Navisworks: only the first to start owns the pipe; the other says "already in use" in its bridge window' }

# 2. Plugin folder (folder name must equal the assembly name — Navisworks discovers plugins by that convention)
$plugins = Join-Path $env:APPDATA "Autodesk\Navisworks Manage $NavisVersion\Plugins"
$pluginDir = Join-Path $plugins 'HPNavis.McpBridge'
$dll = Join-Path $pluginDir 'HPNavis.McpBridge.dll'
Report (Test-Path $dll) 'bridge plugin deployed' $(if (Test-Path $dll) { "$pluginDir ($((Get-Item $dll).LastWriteTime.ToString('s')))" } else { "missing $dll - run 'dotnet build HPNavis/HPNavis.slnx -c Debug' with Navisworks closed" })
$ribbon = Join-Path $pluginDir 'en-US\HPNavisRibbon.xaml'
Report (Test-Path $ribbon) 'ribbon layout beside the plugin (en-US\HPNavisRibbon.xaml)' $(if (Test-Path $ribbon) { 'tab HPNavis > MCP > MCP Bridge' } else { 'stale deploy: the ribbon button is missing; rebuild with Roamer closed' })
$foreign = Join-Path $plugins 'NavisworksMCPPlugin'
if (Test-Path $foreign) { Write-Host "[info] foreign plugin beside ours: $foreign (left alone; it keeps its own DLL copies)" }

# 3. Pipe (hpnavis-mcp-<version>; listening only while Roamer runs with the listener started)
$pipeName = "hpnavis-mcp-$NavisVersion"
$pipe = "\\.\pipe\$pipeName"
$pipeUp = [bool]([IO.Directory]::GetFiles('\\.\pipe\') | Where-Object { $_ -ieq $pipe })
Report $pipeUp "pipe $pipeName listening" $(if ($pipeUp) { $pipe } else { 'bridge listener stopped or plugin not loaded (ribbon HPNavis > MCP > MCP Bridge > Start listener)' })

# 4. Server exe + .mcp.json
$server = Join-Path $RepoRoot 'HPNavis\output\HPNavis.Mcp.Server\HPNavis.Mcp.Server.exe'
Report (Test-Path $server) 'published server exe exists' $(if (Test-Path $server) { "$server ($((Get-Item $server).LastWriteTime.ToString('s')))" } else { "$server - publish with 'dotnet publish HPNavis/HPNavis.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPNavis/output/HPNavis.Mcp.Server'" })
$mcpJson = Join-Path $RepoRoot '.mcp.json'
if (Test-Path $mcpJson) {
    try {
        $cfg = Get-Content $mcpJson -Raw | ConvertFrom-Json
        $entry = $cfg.mcpServers.'hprebar-navis'
        if ($entry) {
            $cmdOk = Test-Path $entry.command
            $ver = $entry.env.'HPNAVIS_MCP_Bridge__HostVersion'
            $verOk = ($ver -eq "$NavisVersion") -or (-not $ver -and $NavisVersion -eq 2026)
            Report ($cmdOk -and $verOk) '.mcp.json hprebar-navis entry' ("command={0} (exists: {1}); HostVersion={2}" -f $entry.command, $cmdOk, $(if ($ver) { $ver } else { '(default 2026)' }))
        } else { Report $false '.mcp.json hprebar-navis entry' "add 'hprebar-navis' -> $server with env HPNAVIS_MCP_Bridge__HostVersion=$NavisVersion (never commit .mcp.json)" }
    } catch { Report $false '.mcp.json parses' $_.Exception.Message }
} else { Report $false '.mcp.json present' $mcpJson }

# 5. Running servers (informational)
$servers = @(Get-Process -Name 'HPNavis.Mcp.Server' -ErrorAction SilentlyContinue)
Write-Host ("[info] HPNavis.Mcp.Server processes: {0} (one per Claude Code session; they lock the published exe - rename it before republishing)" -f $servers.Count)

# 6. Latest bridge log: the self-check line proves Roslyn 5.9 resolved on .NET Framework 4.8 and ran a Search
$logDir = Join-Path $env:LOCALAPPDATA 'HPNavis\McpBridge\logs'
if (Test-Path $logDir) {
    $log = Get-ChildItem $logDir -Filter '*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($log) {
        $tail = Get-Content $log.FullName -Tail 400
        $selfCheck = $tail | Where-Object { $_ -match 'self-check OK' } | Select-Object -Last 1
        Report ([bool]$selfCheck) 'bridge scripting self-check OK (latest log)' $(if ($selfCheck) { $selfCheck.Trim() } else { "no 'self-check OK' line in $($log.Name) - the assembly resolver did not deliver Roslyn; read the log" })
        $inUse = $tail | Where-Object { $_ -match 'could not create pipe|already in use' } | Select-Object -Last 1
        if ($inUse) { Write-Host ("[info] pipe conflict logged: {0}" -f $inUse.Trim()) }
    }
} else { Write-Host "[info] no bridge log folder yet ($logDir) - the plugin has never started" }

Write-Host ''
if ($problems -eq 0) { Write-Host 'All checks OK - call get_navis_context next (executionEnabled must be true: tick "Allow AI code execution in this Navisworks session"; heavy operations need the second checkbox).' -ForegroundColor Green; exit 0 }
Write-Host ("{0} problem(s) - fix the FAIL lines above, then call get_navis_context." -f $problems) -ForegroundColor Yellow
exit 1
