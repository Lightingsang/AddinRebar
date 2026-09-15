# Snapshot `tools/list` of the freshly built Revit and AutoCAD MCP exes on an isolated registry root,
# plus the SHA-256 of each exe, so a before/after comparison provably ran a rebuilt binary.
# Usage: pwsh snapshot-tools-list.ps1 -Tag before|after   (run from the repo root)
param([Parameter(Mandatory)][ValidatePattern('^(before|after)$')][string]$Tag)
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$reports = Join-Path $root 'plans\260915-0824-navisworks-mcp-2026\reports'
$py = Join-Path $root 'McpShared\tools\mcp-call.py'   # the shared stdio helper (the AutoCAD copy moved here 2026-09-16)
$iso = Join-Path $env:TEMP "hp-mcp-snapshot-$Tag"
New-Item -ItemType Directory -Force $iso | Out-Null

$hosts = @(
  @{ name='revit';   exe=Join-Path $root 'HPRebar\HPRebar.Mcp.Server\bin\Release\net10.0\HPRebar.Mcp.Server.exe';   prefix='HPREBAR_MCP_' },
  @{ name='autocad'; exe=Join-Path $root 'HPAutoCad\HPAutoCad.Mcp.Server\bin\Release\net10.0\HPAutoCad.Mcp.Server.exe'; prefix='HPAUTOCAD_MCP_' }
)
foreach ($h in $hosts) {
  $lib = Join-Path $iso "$($h.name)\tools-library"; $db = Join-Path $iso "$($h.name)\registry.db"
  New-Item -ItemType Directory -Force $lib | Out-Null
  $out = Join-Path $reports "phase-00-tools-list-$Tag-$($h.name).json"
  & python $py $h.exe 'tools/list' --env "$($h.prefix)Registry__LibraryPath=$lib" --env "$($h.prefix)Registry__DbPath=$db" --out $out | Out-Null
  # Canonicalise: sort tools by name, drop nothing else, so the diff is order-independent.
  $json = Get-Content $out -Raw | ConvertFrom-Json
  $tools = @($json.result.tools | Sort-Object name)
  ($tools | ConvertTo-Json -Depth 20) | Set-Content -Encoding UTF8 $out
  # The .exe is only the apphost stub (identical across rebuilds); the engine lives in HPRebar.Mcp.Server.Core.dll beside it.
  $core = Join-Path (Split-Path $h.exe) 'HPRebar.Mcp.Server.Core.dll'
  $sha = (Get-FileHash $core -Algorithm SHA256).Hash
  "$sha  $core" | Set-Content -Encoding ASCII (Join-Path $reports "phase-00-core-dll-$Tag-$($h.name).sha256")
  "{0,-8} tools={1,3}  Server.Core.dll sha256={2}" -f $h.name, $tools.Count, $sha.Substring(0,16)
}
