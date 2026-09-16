# Phase 0 gate runner: builds the three MCP server exes in Release, snapshots tools/list (before|after),
# runs the five xUnit suites and writes their summaries to reports/phase-00-<Tag>-tests.txt.
# Usage: pwsh run-phase-00-gate.ps1 -Tag before|after   (run from the repo root)
param([Parameter(Mandatory)][ValidatePattern('^(before|after)$')][string]$Tag)
$ErrorActionPreference = 'Continue'
$root = (Get-Location).Path
$reports = Join-Path $root 'plans\260916-2152-etabs-mcp-2026\reports'
$log = Join-Path $reports "phase-00-$Tag-tests.txt"
"# phase-00 $Tag — $(Get-Date -Format 'yyyy-MM-dd HH:mm')" | Set-Content -Encoding UTF8 $log

function Run-In([string]$dir, [string]$cmd, [string]$label) {
  Push-Location (Join-Path $root $dir)
  try {
    "`n## $label`n`$ $cmd" | Add-Content $log
    $out = & cmd /c "$cmd 2>&1"
    $tail = $out | Select-Object -Last 12
    $tail | Add-Content $log
    "exit=$LASTEXITCODE" | Add-Content $log
    "{0,-42} exit={1}  {2}" -f $label, $LASTEXITCODE, (($out | Where-Object { $_ -match 'total:|Total tests|Passed!|Failed!|error' } | Select-Object -Last 1))
  } finally { Pop-Location }
}

# 1. Release builds of the three server exes (bin/Release, never the published/locked exe under output/)
Run-In 'HPRebar'   'dotnet build HPRebar.Mcp.Server -c Release'   'build revit server'
Run-In 'HPAutoCad' 'dotnet build HPAutoCad.Mcp.Server -c Release' 'build autocad server'
Run-In 'HPNavis'   'dotnet build HPNavis.Mcp.Server -c Release'   'build navis server'

# 2. tools/list snapshots + Core.dll hashes
Push-Location $root
& pwsh -NoProfile -File (Join-Path $reports 'snapshot-tools-list.ps1') -Tag $Tag 2>&1 | Tee-Object -Variable snap
Pop-Location
"`n## snapshot $Tag" | Add-Content $log; $snap | Add-Content $log

# 3. The five suites (each folder carries its own global.json pinning the MTP runner; never pass --nologo)
Run-In 'McpShared' 'dotnet test HPRebar.Mcp.Server.Core.Tests'     'test McpShared Server.Core'
Run-In 'McpShared' 'dotnet test HPRebar.McpBridge.Core.Net48Tests'  'test McpShared Bridge.Core net48'
Run-In 'HPRebar'   'dotnet test HPRebar.Mcp.Server.Tests'          'test Revit server'
Run-In 'HPAutoCad' 'dotnet test HPAutoCad.Mcp.Server.Tests'        'test AutoCAD server'
Run-In 'HPNavis'   'dotnet test HPNavis.Mcp.Server.Tests'          'test Navis server'
"`nlog: $log"
