# Unattended stdio smoke of the published server exe against a live AutoCAD: starts AutoCAD with the bridge
# (bridge.scr), ticks the opt-in through UI Automation, then drives HPAutoCad.Mcp.Server.exe over stdio with
# mcp-call.py exactly as a host AI would: initialize, tools/list, get_autocad_context, execute_autocad_code
# (read, dry run, real run, count). Kills AutoCAD at the end. Publish the exe first:
#   dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server
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
$mcp = Join-Path $PSScriptRoot 'mcp-call.py'
$env:PYTHONIOENCODING = 'utf-8'

function Call([string]$name, [string]$method, [string[]]$extra) {
    $out = Join-Path $OutDir "$name.json"
    $argv = @($Exe, $method) + $extra + @('--out', $out)
    python $mcp @argv | Out-Null
    return Get-Content $out -Raw | ConvertFrom-Json
}

function ExecuteText($r) { $r.result.content[0].text | ConvertFrom-Json }

$p = Start-AcadWithBridge (Join-Path $PSScriptRoot 'bridge.scr') $StartupTimeoutSec
$results = @()
function Check([string]$name, [bool]$ok, [string]$detail) { $script:results += [pscustomobject]@{ name = $name; pass = $ok; detail = $detail }; Write-Host ("{0} {1} {2}" -f ($(if ($ok) { 'PASS' } else { 'FAIL' })), $name, $detail) }

try {
    if (-not $pipeUp) { throw 'bridge pipe never came up' }
    Start-Sleep -Seconds 3
    if (-not (Set-OptIn $true)) { throw 'could not tick the opt-in' }

    $init = Call 'initialize' 'initialize' @()
    Check 'initialize' ($init.result.serverInfo.name -eq 'HPAutoCad MCP') ("serverInfo=" + ($init.result.serverInfo | ConvertTo-Json -Compress))

    $list = Call 'tools-list' 'tools/list' @()
    $names = @($list.result.tools | % name | Sort-Object)
    Check 'tools/list = 12, autocad names only' ($names.Count -eq 12 -and ($names -contains 'execute_autocad_code') -and -not ($names -match 'revit')) ($names -join ', ')

    $ctx = Call 'context' 'tools/call' @('get_autocad_context', '{"includeSelection": true}')
    $ctxText = ExecuteText $ctx
    Check 'get_autocad_context' ($ctxText.host -eq 'autocad' -and $ctxText.hostVersion -eq '2026' -and $null -eq $ctxText.revitVersion -and $ctxText.executionEnabled -eq $true) (($ctxText | ConvertTo-Json -Compress).Substring(0, 300))

    $count = 'var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead); var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead); var n = 0; foreach (ObjectId id in ms) if (id.ObjectClass.DxfName == "LINE") n++; return n;'
    $line = 'var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead); var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite); var l = new Line(Point3d.Origin, new Point3d(units.ToDrawing(args.Double("lengthMm", 1000)), 0, 0)); ms.AppendEntity(l); tr.AddNewlyCreatedDBObject(l, true); log("added " + l.Handle); return new { handle = l.Handle.ToString(), length = l.Length };'

    $read = ExecuteText (Call 'execute-read' 'tools/call' @('execute_autocad_code', (@{ code = 'return db.Filename;'; transaction = 'none'; label = 'read' } | ConvertTo-Json -Compress)))
    Check 'execute none read' (-not $read.isError -and $read.value -like '*.dw*' -and $read.runId -gt 0) ("value=$($read.value) runId=$($read.runId) durationMs=$($read.durationMs)")

    $before = (ExecuteText (Call 'count-before' 'tools/call' @('execute_autocad_code', (@{ code = $count; transaction = 'none'; label = 'count' } | ConvertTo-Json -Compress)))).value
    $dry = ExecuteText (Call 'execute-dryrun' 'tools/call' @('execute_autocad_code', (@{ code = $line; transaction = 'auto'; dryRun = $true; label = 'dry line'; args = @{ lengthMm = 500 } } | ConvertTo-Json -Compress)))
    $afterDry = (ExecuteText (Call 'count-after-dry' 'tools/call' @('execute_autocad_code', (@{ code = $count; transaction = 'none'; label = 'count' } | ConvertTo-Json -Compress)))).value
    Check 'execute dryRun line' (-not $dry.isError -and $dry.rolledBack -eq $true -and $dry.changed.added -eq 1 -and $afterDry -eq $before) ("changed=" + ($dry.changed | ConvertTo-Json -Compress) + " rolledBack=$($dry.rolledBack) value=" + ($dry.value | ConvertTo-Json -Compress) + " count $before->$afterDry")

    $real = ExecuteText (Call 'execute-real' 'tools/call' @('execute_autocad_code', (@{ code = $line; transaction = 'auto'; label = 'real line'; args = @{ lengthMm = 1234.5 } } | ConvertTo-Json -Compress)))
    $afterReal = (ExecuteText (Call 'count-after-real' 'tools/call' @('execute_autocad_code', (@{ code = $count; transaction = 'none'; label = 'count' } | ConvertTo-Json -Compress)))).value
    Check 'execute real line' (-not $real.isError -and $real.rolledBack -eq $false -and $real.changed.added -eq 1 -and $afterReal -eq ($before + 1) -and $real.runId -gt 0) ("changed=" + ($real.changed | ConvertTo-Json -Compress) + " runId=$($real.runId) hint=$($real.hint) count $before->$afterReal")

    $run = ExecuteText (Call 'get-run' 'tools/call' @('get_run', (@{ runId = [int]$real.runId } | ConvertTo-Json -Compress)))
    Check 'get_run of the real run' ($run.runId -eq $real.runId -or $run.id -eq $real.runId -or ($run | ConvertTo-Json -Compress) -match 'real line') (($run | ConvertTo-Json -Compress).Substring(0, [Math]::Min(300, ($run | ConvertTo-Json -Compress).Length)))
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
