# Unattended stdio smoke of the published server exe against a live AutoCAD: starts AutoCAD with the bridge
# (bridge.scr), ticks the opt-in through UI Automation, then drives HPAutoCad.Mcp.Server.exe over stdio with
# McpShared/tools/mcp-call.py exactly as a host AI would: initialize, tools/list, get_autocad_context, execute_autocad_code
# (read, dry run, real run, count). Kills AutoCAD at the end. Publish the exe first:
#   dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server
#Requires -Version 7.3
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\..\output\HPAutoCad.Mcp.Server\HPAutoCad.Mcp.Server.exe'),
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\output\smoke'),
    [int]$StartupTimeoutSec = 420
)

. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning
if (-not (Test-Path $Exe)) { throw "Server exe not found: $Exe (publish it first, see the header of this script)" }
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$mcp = Join-Path $PSScriptRoot '..\..\..\McpShared\tools\mcp-call.py'   # shared stdio helper (McpShared/tools)
$env:PYTHONIOENCODING = 'utf-8'

function Call([string]$name, [string]$method, [string[]]$extra) {
    $out = Join-Path $OutDir "$name.json"
    $argv = @($Exe, $method) + $extra + @('--out', $out)
    python $mcp @argv | Out-Null
    return Get-Content $out -Raw | ConvertFrom-Json
}

function ExecuteText($r) { $r.result.content[0].text | ConvertFrom-Json }
function Short($o, [int]$max = 300) { $t = $o | ConvertTo-Json -Compress -Depth 6; if ($t.Length -gt $max) { $t.Substring(0, $max) } else { $t } }

$p = Start-AcadWithBridge (Join-Path $PSScriptRoot 'bridge.scr') $StartupTimeoutSec
$results = @()
function Check([string]$name, [bool]$ok, [string]$detail) { $script:results += [pscustomobject]@{ name = $name; pass = $ok; detail = $detail }; Write-Host ("{0} {1} {2}" -f ($(if ($ok) { 'PASS' } else { 'FAIL' })), $name, $detail) }

try {
    if (-not $pipeUp) { throw 'bridge pipe never came up' }
    Start-Sleep -Seconds 3
    if (-not (Set-OptIn $true)) { throw 'could not tick the opt-in' }

    $init = Call 'initialize' 'initialize' @()
    Check 'initialize' ($init.result.serverInfo.name -eq 'HPAutoCad MCP') ("serverInfo=" + (Short $init.result.serverInfo))

    $list = Call 'tools-list' 'tools/list' @()
    $names = @($list.result.tools | % name | Sort-Object)
    # >= 24: the user's registry may hold tools approved after the seeds; the 4 core, 8 registry and 12 seed names must all be there.
    $fixed = @('execute_autocad_code', 'get_autocad_context', 'inspect_type', 'cancel_execution', 'search_tools', 'get_tool', 'run_tool', 'get_run', 'propose_tool', 'test_tool', 'publish_tool', 'manage_tool',
               'list_layers', 'list_block_definitions', 'get_entities', 'list_layouts', 'get_drawing_info', 'get_selected_entities', 'draw_polyline', 'draw_circle', 'add_text', 'create_layer', 'insert_block', 'add_linear_dimension')
    $missing = @($fixed | ? { $names -notcontains $_ })
    Check 'tools/list = 4 core + 8 registry + 12 seeds (+ approved tools), autocad names only' ($names.Count -ge 24 -and $missing.Count -eq 0 -and -not ($names -match 'revit')) ("$($names.Count) tools; missing: $($missing -join ', ')")

    $ctx = Call 'context' 'tools/call' @('get_autocad_context', '{"includeSelection": true}')
    $ctxText = ExecuteText $ctx
    Check 'get_autocad_context' ($ctxText.host -eq 'autocad' -and $ctxText.hostVersion -eq '2026' -and $null -eq $ctxText.revitVersion -and $ctxText.executionEnabled -eq $true) (Short $ctxText)

    $count = 'var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead); var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead); var n = 0; foreach (ObjectId id in ms) if (id.ObjectClass.DxfName == "LINE") n++; return n;'
    $line = 'var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead); var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite); var l = new Line(Point3d.Origin, new Point3d(units.ToDrawing(args.Double("lengthMm", 1000)), 0, 0)); ms.AppendEntity(l); tr.AddNewlyCreatedDBObject(l, true); log("added " + l.Handle); return new { handle = l.Handle.ToString(), length = l.Length };'

    $read = ExecuteText (Call 'execute-read' 'tools/call' @('execute_autocad_code', (@{ code = 'return db.Filename;'; transaction = 'none'; label = 'read' } | ConvertTo-Json -Compress)))
    Check 'execute none read' (-not $read.isError -and $read.value -like '*.dw*' -and $read.runId -gt 0) ("value=$($read.value) runId=$($read.runId) durationMs=$($read.durationMs)")

    $before = (ExecuteText (Call 'count-before' 'tools/call' @('execute_autocad_code', (@{ code = $count; transaction = 'none'; label = 'count' } | ConvertTo-Json -Compress)))).value
    $dry = ExecuteText (Call 'execute-dryrun' 'tools/call' @('execute_autocad_code', (@{ code = $line; transaction = 'auto'; dryRun = $true; label = 'dry line'; args = @{ lengthMm = 500 } } | ConvertTo-Json -Compress)))
    $afterDry = (ExecuteText (Call 'count-after-dry' 'tools/call' @('execute_autocad_code', (@{ code = $count; transaction = 'none'; label = 'count' } | ConvertTo-Json -Compress)))).value
    Check 'execute dryRun line' (-not $dry.isError -and $dry.rolledBack -eq $true -and $dry.changed.added -eq 1 -and $afterDry -eq $before) ("changed=" + (Short $dry.changed) + " rolledBack=$($dry.rolledBack) value=" + (Short $dry.value) + " count $before->$afterDry")

    $real = ExecuteText (Call 'execute-real' 'tools/call' @('execute_autocad_code', (@{ code = $line; transaction = 'auto'; label = 'real line'; args = @{ lengthMm = 1234.5 } } | ConvertTo-Json -Compress)))
    $afterReal = (ExecuteText (Call 'count-after-real' 'tools/call' @('execute_autocad_code', (@{ code = $count; transaction = 'none'; label = 'count' } | ConvertTo-Json -Compress)))).value
    Check 'execute real line' (-not $real.isError -and $real.rolledBack -eq $false -and $real.changed.added -eq 1 -and $afterReal -eq ($before + 1) -and $real.runId -gt 0) ("changed=" + (Short $real.changed) + " runId=$($real.runId) hint=$($real.hint) count $before->$afterReal")

    $run = ExecuteText (Call 'get-run' 'tools/call' @('get_run', (@{ runId = [int]$real.runId } | ConvertTo-Json -Compress)))
    Check 'get_run of the real run' ($run.id -eq $real.runId) (Short $run)

    # ---- the 12 seed tools, called by name as real MCP tools -------------------------------------------------
    $search = ExecuteText (Call 'search-layer' 'tools/call' @('search_tools', '{"query": "list layers", "limit": 10}'))
    $found = @($search.tools | % name)
    Check 'search_tools "list layers" finds the layer seeds' (($found -contains 'list_layers') -and ($found -contains 'create_layer')) ($found -join ', ')
    $byCategory = @((ExecuteText (Call 'search-layer-category' 'tools/call' @('search_tools', '{"query": "layers", "category": "Layer"}'))).tools | % name | sort)
    Check 'search_tools category=Layer returns exactly the two Layer seeds' (($byCategory -join ',') -eq 'create_layer,list_layers') ($byCategory -join ', ')

    function Seed([string]$name, [string]$json) { ExecuteText (Call "seed-$name" 'tools/call' @($name, $json)) }
    $layers = Seed 'list_layers' '{"includeCounts": true}'
    Check 'list_layers' (-not $layers.isError -and @($layers.value).Count -ge 1 -and ($layers.value | % name) -contains '0') (Short $layers.value)
    $blocks = Seed 'list_block_definitions' '{}'
    Check 'list_block_definitions' (-not $blocks.isError) ("count=" + @($blocks.value).Count)
    $ents = Seed 'get_entities' '{"type": "LINE"}'
    Check 'get_entities LINE' (-not $ents.isError -and $ents.value.count -ge 1 -and $ents.value.items[0].bboxMm.max.x -gt 0) (Short $ents.value)
    $layouts = Seed 'list_layouts' '{}'
    Check 'list_layouts' (-not $layouts.isError -and ($layouts.value | ? isModel).Count -eq 1) (Short $layouts.value)
    $info = Seed 'get_drawing_info' '{}'
    Check 'get_drawing_info' (-not $info.isError -and $info.value.insunits -and $info.value.currentLayer -eq '0') (Short $info.value)
    $selected = Seed 'get_selected_entities' '{}'
    Check 'get_selected_entities (nothing selected)' (-not $selected.isError -and @($selected.value).Count -eq 0) (Short $selected)

    $created = Seed 'create_layer' '{"name": "MCP-TEST", "colorIndex": 1, "lineweight": 50}'
    Check 'create_layer' (-not $created.isError -and $created.value.created -eq $true -and $created.changed.added -eq 1) (Short $created.value)
    $pl = Seed 'draw_polyline' '{"points": [{"x":0,"y":0},{"x":3000,"y":0},{"x":3000,"y":4500},{"x":0,"y":4500}], "closed": true, "layer": "MCP-TEST"}'
    Check 'draw_polyline on the new layer' (-not $pl.isError -and $pl.value.vertexCount -eq 4 -and [math]::Abs($pl.value.lengthMm - 15000) -lt 1) (Short $pl.value)
    $dryCircle = Seed 'draw_circle' '{"center": {"x": 1500, "y": 2250}, "radiusMm": 500, "dryRun": true}'
    Check 'draw_circle dryRun' (-not $dryCircle.isError -and $dryCircle.rolledBack -eq $true -and $dryCircle.changed.added -eq 1) (Short $dryCircle)
    $txt = Seed 'add_text' '{"text": "KITCHEN", "position": {"x": 1500, "y": 2250}, "heightMm": 250, "mtext": true, "widthMm": 2000}'
    Check 'add_text (MText)' (-not $txt.isError -and $txt.value.type -eq 'MText') (Short $txt.value)
    $dim = Seed 'add_linear_dimension' '{"p1": {"x":0,"y":0}, "p2": {"x":3000,"y":0}, "dimLinePoint": {"x":1500,"y":-600}}'
    Check 'add_linear_dimension measures 3000 mm' (-not $dim.isError -and [math]::Abs($dim.value.measurementMm - 3000) -lt 1) (Short $dim.value)
    $missing = Seed 'insert_block' '{"blockName": "NO-SUCH-BLOCK", "position": {"x":0,"y":0}}'
    Check 'insert_block refuses an unknown block' ($missing.isError -and $missing.message -match 'not defined') (Short $missing.message)
    $undo = Seed 'get_entities' '{"layer": "MCP-TEST"}'
    Check 'get_entities on MCP-TEST sees the polyline' (-not $undo.isError -and $undo.value.count -ge 1) (Short $undo.value)
}
catch {
    Check 'smoke aborted' $false $_.Exception.Message
}
finally {
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -Confirm:$false }
}

$passed = @($results | ? pass).Count
"{{`"passed`": $passed, `"total`": $($results.Count)}}"
$results | ConvertTo-Json | Set-Content (Join-Path $OutDir 'summary.json')
exit $(if ($passed -eq $results.Count) { 0 } else { 1 })
