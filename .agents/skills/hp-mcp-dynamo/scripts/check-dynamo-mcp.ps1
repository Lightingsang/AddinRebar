# Read-only health check of the HPDynamo MCP chain on this machine: Revit process, the bridge add-in manifest,
# the Dynamo ViewExtension manifest, the named pipe, the published/built server exe, and the .mcp.json entry.
# Touches nothing (no process start/stop, no pipe connection, no file write).
#
#   pwsh .agents/skills/hp-mcp-dynamo/scripts/check-dynamo-mcp.ps1 [-RepoRoot <path>] [-RevitVersion 2026]
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
Report ($revit.Count -ge 1) "Revit.exe running" $(if ($revit.Count) { ($revit | ForEach-Object { "pid $($_.Id) '$($_.MainWindowTitle)'" }) -join '; ' } else { "start Revit $RevitVersion and open a project" })

# 2. Bridge add-in manifest + folder
$addins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
$manifest = Join-Path $addins 'HPDynamo.McpBridge.addin'
$dll = Join-Path $addins 'HPDynamo.McpBridge\HPDynamo.McpBridge.dll'
Report (Test-Path $manifest) 'bridge add-in manifest deployed' $(if (Test-Path $manifest) { $manifest } else { "missing $manifest" })
Report (Test-Path $dll) 'bridge add-in DLL deployed' $(if (Test-Path $dll) { (Get-Item $dll).LastWriteTime.ToString('s') } else { $dll })

# 3. Dynamo ViewExtension manifest
$dynamoViewExtDir = Join-Path $env:APPDATA 'Dynamo\Dynamo Revit\3.7\viewExtensions'
$viewExtXml = Join-Path $dynamoViewExtDir 'HPDynamo_ViewExtensionDefinition.xml'
Report (Test-Path $viewExtXml) 'Dynamo ViewExtension manifest deployed' $(if (Test-Path $viewExtXml) { $viewExtXml } else { "missing $viewExtXml" })

# 4. Pipe (hpdynamo-mcp-2026)
$pipeName = "hpdynamo-mcp-$RevitVersion"
$pipe = "\\.\pipe\$pipeName"
$pipeUp = [bool]([IO.Directory]::GetFiles('\\.\pipe\') | Where-Object { $_ -ieq $pipe })
Report $pipeUp "pipe $pipeName listening" $(if ($pipeUp) { $pipe } else { 'bridge listener stopped or add-in not loaded inside Revit' })

# 5. Server exe + .mcp.json
$server = Join-Path $RepoRoot 'HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe'
Report (Test-Path $server) 'server exe exists' $(if (Test-Path $server) { "$server ($((Get-Item $server).LastWriteTime.ToString('s')))" } else { "$server - build with 'dotnet build HPDynamo/HPDynamo.slnx'" })
$mcpJson = Join-Path $RepoRoot '.mcp.json'
if (Test-Path $mcpJson) {
    try {
        $cfg = Get-Content $mcpJson -Raw | ConvertFrom-Json
        $entry = $cfg.mcpServers.'hprebar-dynamo'
        if ($entry) {
            $cmdOk = Test-Path $entry.command
            Report $cmdOk '.mcp.json hprebar-dynamo entry' ("command={0} (exists: {1})" -f $entry.command, $cmdOk)
        } else {
            Report $false '.mcp.json hprebar-dynamo entry' "add 'hprebar-dynamo' -> $server"
        }
    } catch { Report $false '.mcp.json parses' $_.Exception.Message }
} else { Report $false '.mcp.json present' $mcpJson }

# 6. Running servers
$servers = @(Get-Process -Name 'HPDynamo.Mcp.Server' -ErrorAction SilentlyContinue)
Write-Host ("[info] HPDynamo.Mcp.Server processes running: {0}" -f $servers.Count)

if ($problems -gt 0) {
    Write-Host ("`n[summary] {0} issue(s) found. See references/troubleshooting.md" -f $problems) -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "`n[summary] All checks passed! Ready to communicate with HPDynamo." -ForegroundColor Green
    exit 0
}
