# Read-only health check of the HPRebar Revit MCP chain on this machine: Revit process, the bridge add-in manifest, the named
# pipe, the published server exe, the .mcp.json entry and the bridge's own self-check line. Touches nothing (no process
# start/stop, no pipe connection, no file write).
#
#   pwsh .claude/skills/hp-mcp-revit/scripts/check-revit-mcp.ps1 [-RepoRoot <path>] [-RevitVersion 2026]
#
# Exit code 0 = every check OK, 1 = at least one problem (the lines say which).
param([string]$RepoRoot = '', [int]$RevitVersion = 2026)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path }
$problems = 0
function Report([bool]$ok, [string]$what, [string]$detail) {
    $mark = if ($ok) { 'OK  ' } else { 'FAIL' }
    Write-Host ("[{0}] {1}" -f $mark, $what) -ForegroundColor ($(if ($ok) { 'Green' } else { 'Red' }))
    if ($detail) { Write-Host ("       {0}" -f $detail) }
    if (-not $ok) { $script:problems++ }
}

# 1. Revit
$revit = @(Get-Process -Name 'Revit' -ErrorAction SilentlyContinue)
Report ($revit.Count -ge 1) "Revit.exe running" $(if ($revit.Count) { ($revit | ForEach-Object { "pid $($_.Id) '$($_.MainWindowTitle)'" }) -join '; ' } else { "start Revit $RevitVersion and open a project (no document = run error 'No document is open')" })
if ($revit.Count -gt 1) { Write-Host '[info] more than one Revit: only the first to start owns the pipe; the other reports "pipe in use" in its bridge window' }

# 2. Bridge add-in manifest + folder (deployed per user by `dotnet build HPRebar/HPRebar.slnx -c Debug.R26`)
$addins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
$manifest = Join-Path $addins 'HPRebar.McpBridge.addin'
$dll = Join-Path $addins 'HPRebar.McpBridge\HPRebar.McpBridge.dll'
Report (Test-Path $manifest) 'bridge add-in manifest deployed' $(if (Test-Path $manifest) { $manifest } else { "missing $manifest - run 'dotnet build HPRebar/HPRebar.slnx -c Debug.R$($RevitVersion % 100)' with Revit closed" })
Report (Test-Path $dll) 'bridge add-in DLL beside the manifest' $(if (Test-Path $dll) { (Get-Item $dll).LastWriteTime.ToString('s') } else { $dll })

# 3. Pipe (hprebar-mcp-r<version>; listening only while Revit runs with the listener started)
$pipeName = "hprebar-mcp-r$RevitVersion"
$pipe = "\\.\pipe\$pipeName"
$pipeUp = [bool]([IO.Directory]::GetFiles('\\.\pipe\') | Where-Object { $_ -ieq $pipe })
Report $pipeUp "pipe $pipeName listening" $(if ($pipeUp) { $pipe } else { 'bridge listener stopped or add-in not loaded (ribbon HPRebar > MCP > MCP Bridge > Start listener; "publisher could not be verified" needs Always Load once)' })

# 4. Server exe + .mcp.json
$server = Join-Path $RepoRoot 'HPRebar\output\HPRebar.Mcp.Server\HPRebar.Mcp.Server.exe'
Report (Test-Path $server) 'published server exe exists' $(if (Test-Path $server) { "$server ($((Get-Item $server).LastWriteTime.ToString('s')))" } else { "$server - publish with 'dotnet publish HPRebar/HPRebar.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPRebar/output/HPRebar.Mcp.Server'" })
$mcpJson = Join-Path $RepoRoot '.mcp.json'
if (Test-Path $mcpJson) {
    try {
        $cfg = Get-Content $mcpJson -Raw | ConvertFrom-Json
        $entry = $cfg.mcpServers.'hprebar-revit'
        if ($entry) {
            $cmdOk = Test-Path $entry.command
            $ver = $entry.env.'HPREBAR_MCP_Bridge__RevitVersion'
            if (-not $ver) { $ver = $entry.env.'HPREBAR_MCP_Bridge__HostVersion' }
            $verOk = ($ver -eq "$RevitVersion") -or (-not $ver -and $RevitVersion -eq 2026)
            Report ($cmdOk -and $verOk) '.mcp.json hprebar-revit entry' ("command={0} (exists: {1}); RevitVersion={2}" -f $entry.command, $cmdOk, $(if ($ver) { $ver } else { '(default 2026)' }))
        } else { Report $false '.mcp.json hprebar-revit entry' "add 'hprebar-revit' -> $server with env HPREBAR_MCP_Bridge__RevitVersion=$RevitVersion (never commit .mcp.json)" }
    } catch { Report $false '.mcp.json parses' $_.Exception.Message }
} else { Report $false '.mcp.json present' $mcpJson }

# 5. Running servers (informational)
$servers = @(Get-Process -Name 'HPRebar.Mcp.Server' -ErrorAction SilentlyContinue)
Write-Host ("[info] HPRebar.Mcp.Server processes: {0} (one per Claude Code session; they lock the published exe - rename it before republishing)" -f $servers.Count)

# 6. Latest bridge log: the self-check line proves Roslyn compiled and ran a script inside Revit's load context
$logDir = Join-Path $env:LOCALAPPDATA 'HPRebar\McpBridge\logs'
if (Test-Path $logDir) {
    $log = Get-ChildItem $logDir -Filter '*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($log) {
        $tail = Get-Content $log.FullName -Tail 400
        $selfCheck = $tail | Where-Object { $_ -match 'self-check OK' } | Select-Object -Last 1
        Report ([bool]$selfCheck) 'bridge scripting self-check OK (latest log)' $(if ($selfCheck) { $selfCheck.Trim() } else { "no 'self-check OK' line in $($log.Name) - Roslyn did not load in the add-in's load context; read the log" })
        $ready = $tail | Where-Object { $_ -match 'MCP bridge ready on pipe' } | Select-Object -Last 1
        if ($ready) { Write-Host ("[info] {0}" -f $ready.Trim()) }
    }
} else { Write-Host "[info] no bridge log folder yet ($logDir) - the add-in has never started" }

Write-Host ''
if ($problems -eq 0) { Write-Host 'All checks OK - call get_revit_context next (executionEnabled must be true: tick "Allow AI code execution in this Revit session" in the bridge window; isModifiable must be false).' -ForegroundColor Green; exit 0 }
Write-Host ("{0} problem(s) - fix the FAIL lines above, then call get_revit_context." -f $problems) -ForegroundColor Yellow
exit 1
