# Read-only health check of the HPCivil3d MCP chain on this machine: the Civil 3D process, the bridge bundle, the named
# pipe, the published server exe and the .mcp.json entry. Touches nothing (no process start/stop, no pipe connection,
# no file write). Works in Windows PowerShell 5.1 and pwsh 7.
#
#   pwsh .claude/skills/hp-mcp-civil3d/scripts/check-civil3d-mcp.ps1 [-RepoRoot <path>]
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

# 1. acad.exe — Civil 3D is the same executable as AutoCAD; the window title tells the product apart
$acad = @(Get-Process -Name 'acad' -ErrorAction SilentlyContinue)
$civil = @($acad | Where-Object { $_.MainWindowTitle -match 'Civil 3D' })
Report ($civil.Count -ge 1) 'Civil 3D 2026 running (acad.exe with a "Civil 3D" window)' $(if ($civil.Count) { ($civil | ForEach-Object { "pid $($_.Id) '$($_.MainWindowTitle)'" }) -join '; ' } elseif ($acad.Count) { "acad.exe running but not as Civil 3D: " + (($acad | ForEach-Object { "pid $($_.Id) '$($_.MainWindowTitle)'" }) -join '; ') + " - the Civil bundle loads only under /product C3D" } else { 'start Civil 3D 2026 from its shortcut and open a drawing (no drawing = -32003)' })
if ($civil.Count -gt 1) { Write-Host '[info] more than one Civil 3D: only the first to start owns the pipe; the other says "pipe in use" in its bridge window' }

# 2. Bridge bundle (autoloaded by Civil 3D only: Platform="Civil3D")
$bundle = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle'
$loader = Join-Path $bundle 'Contents\HPCivil3d.McpBridge.Loader.dll'
$manifest = Join-Path $bundle 'PackageContents.xml'
Report (Test-Path $loader) 'bridge bundle deployed' $(if (Test-Path $loader) { "$bundle ($((Get-Item $loader).LastWriteTime.ToString('s')))" } else { "missing $bundle - run 'dotnet build HPCivil3d/HPCivil3d.slnx -c Debug' with Civil 3D closed" })
if (Test-Path $manifest) {
    $platform = (Select-String -Path $manifest -Pattern 'Platform="([^"]+)"' | Select-Object -First 1).Matches[0].Groups[1].Value
    Report ($platform -eq 'Civil3D') 'bundle Platform="Civil3D" (loads only in Civil 3D)' "Platform=$platform"
}
$others = @(Get-ChildItem (Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins') -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'Mcp' -and $_.Name -notmatch '^HP' })
if ($others.Count) { Write-Host ("[info] other MCP bundles present (Platform=AutoCAD* loads into every product; this skill never touches them): {0}" -f (($others | ForEach-Object Name) -join ', ')) }

# 3. Pipe — a directory listing, never Test-Path: connecting would make the bridge accept and drop a client
$pipe = '\\.\pipe\hpcivil3d-mcp-2026'
$pipeUp = [bool]([IO.Directory]::GetFiles('\\.\pipe\') | Where-Object { $_ -ieq $pipe })
Report $pipeUp 'pipe hpcivil3d-mcp-2026 listening' $(if ($pipeUp) { $pipe } else { 'listener stopped or bundle not loaded (SECURELOAD dialog waiting? ribbon HPCivil3d > MCP > MCP Bridge, or command HPC3DMCPBRIDGE)' })

# 4. Server exe + .mcp.json
$server = Join-Path $RepoRoot 'HPCivil3d\output\HPCivil3d.Mcp.Server\HPCivil3d.Mcp.Server.exe'
Report (Test-Path $server) 'published server exe exists' $(if (Test-Path $server) { "$server ($((Get-Item $server).LastWriteTime.ToString('s')))" } else { "$server - dotnet publish HPCivil3d/HPCivil3d.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPCivil3d/output/HPCivil3d.Mcp.Server" })
$mcpJson = Join-Path $RepoRoot '.mcp.json'
if (Test-Path $mcpJson) {
    try {
        $cfg = Get-Content $mcpJson -Raw | ConvertFrom-Json
        $entry = $cfg.mcpServers.'hprebar-civil3d'
        if ($entry) {
            $cmdOk = Test-Path $entry.command
            $verOk = $entry.env.'HPCIVIL3D_MCP_Bridge__HostVersion' -eq '2026'
            Report ($cmdOk -and $verOk) '.mcp.json hprebar-civil3d entry' ("command={0} (exists: {1}); HostVersion={2}" -f $entry.command, $cmdOk, $entry.env.'HPCIVIL3D_MCP_Bridge__HostVersion')
        } else { Report $false '.mcp.json hprebar-civil3d entry' "add 'hprebar-civil3d' -> $server with env HPCIVIL3D_MCP_Bridge__HostVersion=2026 (never commit .mcp.json)" }
    } catch { Report $false '.mcp.json parses' $_.Exception.Message }
} else { Report $false '.mcp.json present' $mcpJson }

# 5. Running servers (informational)
$servers = @(Get-Process -Name 'HPCivil3d.Mcp.Server' -ErrorAction SilentlyContinue)
Write-Host ("[info] HPCivil3d.Mcp.Server processes: {0} (one per Claude Code session; they lock the published exe - stop them before republishing)" -f $servers.Count)

# 6. Latest bridge log: the self-check line proves Roslyn + the Civil API resolved in the bridge's load context
$logDir = Join-Path $env:LOCALAPPDATA 'HPCivil3d\McpBridge\logs'
if (Test-Path $logDir) {
    $log = Get-ChildItem $logDir -Filter 'mcpbridge*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($log) {
        $tail = Get-Content $log.FullName -Tail 400
        $selfCheck = $tail | Where-Object { $_ -match 'self-check OK' } | Select-Object -Last 1
        Report ([bool]$selfCheck) 'bridge scripting self-check OK (latest log)' $(if ($selfCheck) { $selfCheck.Trim() } else { "no 'self-check OK' line in $($log.Name)" })
        if ($selfCheck -and $selfCheck -notmatch 'civil Civil3D') { Report $false 'self-check ran under Civil 3D (civil Civil3D)' 'the bridge started outside Civil 3D or the civil global did not resolve' }
        $inUse = $tail | Where-Object { $_ -match 'could not create pipe|already in use' } | Select-Object -Last 1
        if ($inUse) { Write-Host ("[info] latest pipe conflict line: {0}" -f $inUse.Trim()) }
    }
} else { Write-Host "[info] no bridge log folder yet ($logDir)" }

Write-Host ''
if ($problems -eq 0) { Write-Host 'All checks OK - call get_civil3d_context next (executionEnabled must be true: tick "Allow AI code execution" in the bridge window; read civil3d.drawingUnit and insunitsMismatch before writing).' -ForegroundColor Green; exit 0 }
Write-Host ("{0} problem(s) - fix the FAIL lines above, then call get_civil3d_context." -f $problems) -ForegroundColor Yellow
exit 1
