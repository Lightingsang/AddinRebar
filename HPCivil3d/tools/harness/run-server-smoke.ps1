# Unattended stdio smoke of the server exe against a live Civil 3D 2026: starts Civil 3D with the bridge (bridge.scr),
# ticks the opt-in through UI Automation, opens a copy of a Civil tutorial drawing that holds alignments, profiles,
# surfaces, a corridor, a pipe network, a site and COGO points, then drives HPCivil3d.Mcp.Server.exe over stdio with
# McpShared/tools/mcp-call.py exactly as a host AI would — initialize, tools/list, get_civil3d_context, execute, and
# every one of the 12 seed tools by name (reads on the real objects, the two writes as dryRun then real). The registry
# root is isolated under output/, the drawing is a copy and is never saved, and only the acad.exe this script started
# is touched. Publish the exe first:
#   dotnet publish HPCivil3d/HPCivil3d.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPCivil3d/output/HPCivil3d.Mcp.Server
#Requires -Version 7.3
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\..\output\HPCivil3d.Mcp.Server\HPCivil3d.Mcp.Server.exe'),
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\output\smoke'),
    [string]$Drawing = 'Profile-5F.dwg',
    [string]$ParcelDrawing = 'Corridor-1a.dwg',   # the main drawing's site holds no parcels; this one has 52 lots (Meters)
    [int]$StartupTimeoutSec = 420
)

. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning
if (-not (Test-Path $Exe)) { throw "Server exe not found: $Exe (publish it first, see the header of this script)" }
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $OutDir, (Join-Path $OutDir 'registry\tools-library'), (Join-Path $OutDir 'scene') | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$mcp = Join-Path $PSScriptRoot '..\..\..\McpShared\tools\mcp-call.py'   # shared stdio helper (McpShared/tools)
$env:PYTHONIOENCODING = 'utf-8'
$registry = Join-Path $OutDir 'registry'
$envArgs = @('--env', "HPCIVIL3D_MCP_Registry__LibraryPath=$registry\tools-library", '--env', "HPCIVIL3D_MCP_Registry__DbPath=$registry\registry.db")

# the scene: one tutorial drawing copied out of the install (never opened in place); stale locks would make it read-only
$scene = Join-Path $OutDir 'scene'
Get-ChildItem $scene -Force -Include '*.dwl', '*.dwl2' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
$tutorials = 'C:\Program Files\Autodesk\AutoCAD 2026\C3D\Help\Civil Tutorials\Drawings'
$drawingPath = Join-Path $scene $Drawing
if (-not (Test-Path (Join-Path $tutorials $Drawing))) { throw "tutorial drawing $Drawing not found under $tutorials" }
Copy-Item (Join-Path $tutorials $Drawing) $drawingPath -Force
$parcelPath = Join-Path $scene $ParcelDrawing
if (Test-Path (Join-Path $tutorials $ParcelDrawing)) { Copy-Item (Join-Path $tutorials $ParcelDrawing) $parcelPath -Force }

function Call([string]$name, [string]$method, [string[]]$extra) {
    $out = Join-Path $OutDir "$name.json"
    $argv = @($Exe, $method) + $extra + $envArgs + @('--out', $out)
    python $mcp @argv | Out-Null
    return Get-Content $out -Raw | ConvertFrom-Json
}
function ExecuteText($r) { $r.result.content[0].text | ConvertFrom-Json }
function Short($o, [int]$max = 300) { $t = $o | ConvertTo-Json -Compress -Depth 6; if ($t.Length -gt $max) { $t.Substring(0, $max) } else { $t } }
function Com([string]$script) {
    # Windows PowerShell 5.1 for GetActiveObject; pid-guarded like every other harness. Civil 3D rejects COM for a while after opening a drawing.
    $ps = "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$script:acadPid') { throw ('refusing COM: acad pids [' + (`$ids -join ',') + '], harness owns $script:acadPid') }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); $script"
    for ($i = 0; $i -lt 12; $i++) { $out = powershell -NoProfile -Command $ps 2>&1; if ($LASTEXITCODE -eq 0) { return ($out -join "`n") }; Start-Sleep -Seconds 5 }
    return $null
}
function Seed([string]$name, [string]$json, [string]$tag = '') { ExecuteText (Call "seed-$name$tag" 'tools/call' @($name, $json)) }
function Execute([string]$code, [string]$transaction, [string]$label, [bool]$dryRun = $false, $arguments = $null) {
    $body = @{ code = $code; transaction = $transaction; label = $label; dryRun = $dryRun }
    if ($null -ne $arguments) { $body.args = $arguments }
    ExecuteText (Call "execute-$label" 'tools/call' @('execute_civil3d_code', ($body | ConvertTo-Json -Compress -Depth 6)))
}

$p = Start-AcadWithBridge (Join-Path $PSScriptRoot 'bridge.scr') $StartupTimeoutSec
$results = @()
function Check([string]$name, [bool]$ok, [string]$detail) { $script:results += [pscustomobject]@{ name = $name; pass = $ok; detail = $detail }; Write-Host ("{0} {1} {2}" -f ($(if ($ok) { 'PASS' } else { 'FAIL' })), $name, $detail) }

try {
    if (-not $pipeUp) { throw 'bridge pipe never came up' }
    Start-Sleep -Seconds 3
    if (-not (Set-OptIn $true)) { throw 'could not tick the opt-in' }

    $init = Call 'initialize' 'initialize' @()
    Check 'initialize' ($init.result.serverInfo.name -eq 'HPCivil3d MCP') ("serverInfo=" + (Short $init.result.serverInfo))

    $list = Call 'tools-list' 'tools/list' @()
    $names = @($list.result.tools | % name | Sort-Object)
    $fixed = @('execute_civil3d_code', 'get_civil3d_context', 'inspect_type', 'cancel_execution', 'search_tools', 'get_tool', 'run_tool', 'get_run', 'propose_tool', 'test_tool', 'publish_tool', 'manage_tool',
               'get_civil_document_info', 'list_alignments', 'get_alignment_geometry', 'list_profiles', 'list_surfaces', 'get_surface_elevation', 'list_corridors', 'list_pipe_networks', 'list_parcels', 'list_cogo_points', 'create_cogo_points', 'create_alignment_from_polyline')
    $missing = @($fixed | ? { $names -notcontains $_ })
    Check 'tools/list = 4 core + 8 registry + 12 seeds, civil3d names only' ($names.Count -eq 24 -and $missing.Count -eq 0 -and -not ($names -match 'revit|autocad')) ("$($names.Count) tools; missing: $($missing -join ', ')")

    # open the scene drawing through COM and wait until the context names it
    $null = Com "`$a.Documents.Open('$drawingPath') | Out-Null; 'opened'"
    $ctxText = $null
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 3
        $ctxText = ExecuteText (Call 'context' 'tools/call' @('get_civil3d_context', '{"includeSelection": false}'))
        if ($ctxText.docTitle -and (Split-Path $ctxText.docTitle -Leaf) -eq $Drawing) { break }
    }
    Check "get_civil3d_context on $Drawing" ($ctxText.host -eq 'civil3d' -and $ctxText.hostVersion -eq '2026' -and $null -eq $ctxText.revitVersion -and $ctxText.civil3d.isCivilDocument -eq $true -and (Split-Path $ctxText.docTitle -Leaf) -eq $Drawing) (Short $ctxText.civil3d)

    $read = Execute 'return db.Filename;' 'none' 'read'
    Check 'execute none read' (-not $read.isError -and $read.value -like '*.dwg' -and $read.runId -gt 0) ("value=$(Split-Path $read.value -Leaf) runId=$($read.runId)")

    # ---- the 12 seed tools, called by name as real MCP tools -------------------------------------------------
    $info = Seed 'get_civil_document_info' '{}'
    Check 'get_civil_document_info' (-not $info.isError -and $info.value.success -and $info.value.counts.alignments -ge 1 -and $info.value.drawingUnit -in @('Meters', 'Feet') -and $info.value.styles.alignment.Count -ge 1) (Short $info.value.counts)
    $unit = $info.value.drawingUnit

    $alignments = Seed 'list_alignments' '{"limit": 100}'
    Check 'list_alignments' (-not $alignments.isError -and $alignments.value.count -ge 1 -and $alignments.value.items[0].lengthMm -gt 0 -and $alignments.value.items[0].endStationLabel) ("count=$($alignments.value.count) first=" + (Short $alignments.value.items[0]))
    $firstAlignment = $alignments.value.items[0]

    $geometry = Seed 'get_alignment_geometry' (@{ alignment = $firstAlignment.handle; sampleStepMm = 50000; maxSamples = 50 } | ConvertTo-Json -Compress)
    Check 'get_alignment_geometry by handle with samples' (-not $geometry.isError -and $geometry.value.count -ge 1 -and $geometry.value.entities[0].start.x -ne 0 -and $geometry.value.samples.Count -ge 2 -and $geometry.value.alignment.name -eq $firstAlignment.name) ("entities=$($geometry.value.count) samples=$($geometry.value.samples.Count) first=" + (Short $geometry.value.entities[0]))
    $byName = Seed 'get_alignment_geometry' (@{ alignment = $firstAlignment.name } | ConvertTo-Json -Compress) '-byname'
    Check 'get_alignment_geometry by name gives the same alignment' (-not $byName.isError -and $byName.value.alignment.handle -eq $firstAlignment.handle) ("handle=$($byName.value.alignment.handle)")
    $unknown = Seed 'get_alignment_geometry' '{"alignment": "NO-SUCH-ALIGNMENT"}' '-unknown'
    Check 'get_alignment_geometry refuses an unknown alignment as ArgumentException' ($unknown.isError -and $unknown.message -like 'ArgumentException*') (Short $unknown.message)

    $profiles = Seed 'list_profiles' '{}'
    Check 'list_profiles' (-not $profiles.isError -and $profiles.value.count -ge 1 -and $profiles.value.items[0].name -and $profiles.value.items[0].alignment) ("count=$($profiles.value.count) first=" + (Short $profiles.value.items[0]))

    $surfaces = Seed 'list_surfaces' '{}'
    $surface = $surfaces.value.items | ? { $_.pointCount -gt 0 } | Select-Object -First 1
    Check 'list_surfaces' (-not $surfaces.isError -and $surfaces.value.count -ge 1 -and $null -ne $surface -and $surface.elevationMax -ge $surface.elevationMin -and $surface.boundsMm.maxX -gt $surface.boundsMm.minX) ("count=$($surfaces.value.count) first=" + (Short $surface))

    $cx = ($surface.boundsMm.minX + $surface.boundsMm.maxX) / 2; $cy = ($surface.boundsMm.minY + $surface.boundsMm.maxY) / 2
    $elevation = Seed 'get_surface_elevation' (@{ surface = $surface.name; points = @(@{ x = $cx; y = $cy }, @{ x = 1e12; y = 1e12 }) } | ConvertTo-Json -Compress -Depth 4)
    Check 'get_surface_elevation: centre answers, a far point is OUTSIDE_SURFACE' (-not $elevation.isError -and $elevation.value.okCount -eq 1 -and $elevation.value.outsideCount -eq 1 -and $elevation.value.items[0].elevation -ge $surface.elevationMin -and $elevation.value.items[1].error -eq 'OUTSIDE_SURFACE') (Short $elevation.value.items)

    $corridors = Seed 'list_corridors' '{}'
    Check 'list_corridors' (-not $corridors.isError -and $corridors.value.count -ge 1 -and $corridors.value.items[0].baselines.Count -ge 1 -and $corridors.value.items[0].baselines[0].alignment) (Short $corridors.value.items[0])

    $networks = Seed 'list_pipe_networks' '{"includeParts": true, "partLimit": 50}'
    Check 'list_pipe_networks with parts' (-not $networks.isError -and $networks.value.count -ge 1 -and $networks.value.items[0].pipeCount -ge 1 -and $networks.value.items[0].pipes.Count -ge 1 -and $networks.value.items[0].pipes[0].length2DMm -gt 0 -and $networks.value.items[0].structures[0].rimElevation) ("networks=$($networks.value.count) pipes=$($networks.value.items[0].pipeCount) first pipe=" + (Short $networks.value.items[0].pipes[0]))

    $parcels = Seed 'list_parcels' '{}'
    Check 'list_parcels' (-not $parcels.isError -and $parcels.value.success -and $parcels.value.areaUnit -eq "$unit²") ("count=$($parcels.value.count) sites in summary: $($parcels.value.summary)")

    $cogo = Seed 'list_cogo_points' '{"limit": 5}'
    Check 'list_cogo_points limit 5' (-not $cogo.isError -and $cogo.value.count -le 5 -and $cogo.value.total -ge $cogo.value.count -and ($cogo.value.count -eq 0 -or $cogo.value.items[0].number -ge 0)) ("count=$($cogo.value.count) total=$($cogo.value.total) first=" + (Short $cogo.value.items[0]))
    $cogoTotal = [int]$cogo.value.total

    $dryCogo = ExecuteText (Call 'seed-create_cogo_points-dry' 'tools/call' @('create_cogo_points', '{"points": [{"x": 1000, "y": 2000, "elevation": 12.5, "description": "MCP SMOKE"}], "dryRun": true}'))
    $totalAfterDry = [int](Seed 'list_cogo_points' '{"limit": 1}' '-afterdry').value.total
    Check 'create_cogo_points dryRun rolls back' (-not $dryCogo.isError -and $dryCogo.rolledBack -eq $true -and $dryCogo.value.createdCount -eq 1 -and $dryCogo.value.items[0].number -gt 0 -and $totalAfterDry -eq $cogoTotal) ("changed=" + (Short $dryCogo.changed) + " number=$($dryCogo.value.items[0].number) total $cogoTotal->$totalAfterDry")
    $realCogo = Seed 'create_cogo_points' '{"points": [{"x": 1000, "y": 2000, "elevation": 12.5, "name": "MCP-A"}, {"x": 3000, "y": 2000, "elevation": 13}], "description": "MCP SMOKE"}' '-real'
    $totalAfterReal = [int](Seed 'list_cogo_points' '{"limit": 1}' '-afterreal').value.total
    Check 'create_cogo_points real adds two points' (-not $realCogo.isError -and $realCogo.rolledBack -eq $false -and $realCogo.value.createdCount -eq 2 -and $totalAfterReal -eq $cogoTotal + 2) ("total $cogoTotal->$totalAfterReal items=" + (Short $realCogo.value.items))
    $byDescription = Seed 'list_cogo_points' '{"descriptionPattern": "MCP SMOKE*"}' '-bydesc'
    Check 'list_cogo_points finds the new points by description' (-not $byDescription.isError -and $byDescription.value.count -eq 2 -and ($byDescription.value.items | % name) -contains 'MCP-A') (Short $byDescription.value.items)

    $polyline = Execute 'var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite); var pl = new Polyline(); pl.AddVertexAt(0, new Point2d(units.ToDrawing(args.Double("x0")), units.ToDrawing(args.Double("y0"))), 0, 0, 0); pl.AddVertexAt(1, new Point2d(units.ToDrawing(args.Double("x0") + 300000), units.ToDrawing(args.Double("y0"))), 0, 0, 0); pl.AddVertexAt(2, new Point2d(units.ToDrawing(args.Double("x0") + 300000), units.ToDrawing(args.Double("y0") + 200000)), 0, 0, 0); space.AppendEntity(pl); tr.AddNewlyCreatedDBObject(pl, true); return pl.Handle.ToString();' 'auto' 'polyline' $false @{ x0 = $cx; y0 = $cy }
    Check 'execute draws the source polyline' (-not $polyline.isError -and $polyline.value) ("handle=$($polyline.value)")
    $alignmentCount = [int](Seed 'list_alignments' '{"limit": 180}' '-before').value.count
    $dryAlignment = ExecuteText (Call 'seed-create_alignment-dry' 'tools/call' @('create_alignment_from_polyline', (@{ polyline = $polyline.value; name = 'MCP Smoke Road'; dryRun = $true } | ConvertTo-Json -Compress)))
    $countAfterDry = [int](Seed 'list_alignments' '{"limit": 180}' '-afterdry').value.count
    Check 'create_alignment_from_polyline dryRun rolls back' (-not $dryAlignment.isError -and $dryAlignment.rolledBack -eq $true -and $dryAlignment.value.alignment.entityCount -ge 2 -and $dryAlignment.value.alignment.lengthMm -gt 400000 -and $countAfterDry -eq $alignmentCount) ("alignment=" + (Short $dryAlignment.value.alignment) + " count $alignmentCount->$countAfterDry")
    $realAlignment = Seed 'create_alignment_from_polyline' (@{ polyline = $polyline.value; name = 'MCP Smoke Road'; site = '' } | ConvertTo-Json -Compress) '-real'
    $countAfterReal = [int](Seed 'list_alignments' '{"limit": 180}' '-afterreal').value.count
    Check 'create_alignment_from_polyline real adds one siteless alignment' (-not $realAlignment.isError -and $realAlignment.rolledBack -eq $false -and $realAlignment.value.alignment.name -eq 'MCP Smoke Road' -and $countAfterReal -eq $alignmentCount + 1 -and $realAlignment.value.alignment.site -eq '') ("count $alignmentCount->$countAfterReal alignment=" + (Short $realAlignment.value.alignment))
    $duplicate = Seed 'create_alignment_from_polyline' (@{ polyline = $polyline.value; name = 'MCP Smoke Road' } | ConvertTo-Json -Compress) '-duplicate'
    Check 'create_alignment_from_polyline refuses a duplicate name as ArgumentException' ($duplicate.isError -and $duplicate.message -like 'ArgumentException*already exists*') (Short $duplicate.message)
    $badHandle = Seed 'create_alignment_from_polyline' '{"polyline": "ZZZZZZ", "name": "MCP Smoke Road 2"}' '-badhandle'
    Check 'create_alignment_from_polyline refuses a bad handle as ArgumentException' ($badHandle.isError -and $badHandle.message -like 'ArgumentException*') (Short $badHandle.message)

    $search = ExecuteText (Call 'search-alignment' 'tools/call' @('search_tools', '{"query": "alignment", "limit": 10}'))
    $found = @($search.tools | % name)
    Check 'search_tools "alignment" finds the alignment seeds' (($found -contains 'list_alignments') -and ($found -contains 'get_alignment_geometry')) ($found -join ', ')
    $run = ExecuteText (Call 'get-run' 'tools/call' @('get_run', (@{ runId = [int]$realCogo.runId } | ConvertTo-Json -Compress)))
    Check 'get_run of the real create_cogo_points run' ($run.id -eq $realCogo.runId -and $run.toolName -eq 'create_cogo_points') (Short $run)

    # ---- parcels need a drawing that has some: a second tutorial drawing, Meters, 52 lots in Site 1 -----------------
    if (Test-Path $parcelPath) {
        $null = Com "`$a.Documents.Open('$parcelPath') | Out-Null; 'opened'"
        for ($i = 0; $i -lt 30; $i++) {
            Start-Sleep -Seconds 3
            $ctx2 = ExecuteText (Call 'context-parcels' 'tools/call' @('get_civil3d_context', '{"includeSelection": false}'))
            if ($ctx2.docTitle -and (Split-Path $ctx2.docTitle -Leaf) -eq $ParcelDrawing) { break }
        }
        $lots = Seed 'list_parcels' '{"site": "Site 1", "namePattern": "Single-Family*"}' '-lots'
        Check "list_parcels on ${ParcelDrawing}: named lots with area in $($ctx2.civil3d.drawingUnit)²" (-not $lots.isError -and $lots.value.count -ge 1 -and $lots.value.items[0].area -gt 0 -and $lots.value.items[0].site -eq 'Site 1' -and $lots.value.areaUnit -eq "$($ctx2.civil3d.drawingUnit)²") ("count=$($lots.value.count) first=" + (Short $lots.value.items[0]))
        $noSite = Seed 'list_parcels' '{"site": "NO-SUCH-SITE"}' '-nosite'
        Check 'list_parcels refuses an unknown site as ArgumentException' ($noSite.isError -and $noSite.message -like 'ArgumentException*') (Short $noSite.message)
    }
}
catch {
    Check 'smoke aborted' $false $_.Exception.Message
}
finally {
    "=== stopping acad (the drawing is a copy and is never saved)"
    Stop-Acad $p
    Get-ChildItem $scene -Force -Include '*.dwl', '*.dwl2' -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    Remove-Item Env:HP_HARNESS_ACAD_PID -ErrorAction SilentlyContinue
}

$passed = @($results | ? pass).Count
"{{`"passed`": $passed, `"total`": $($results.Count)}}"
$results | ConvertTo-Json | Set-Content (Join-Path $OutDir 'summary.json')
exit $(if ($passed -eq $results.Count) { 0 } else { 1 })
