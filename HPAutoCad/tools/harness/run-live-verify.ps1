# Phase-5 live verification, unattended: starts AutoCAD 2026 with the bridge, then drives the published
# HPAutoCad.Mcp.Server.exe over one stdio session (live-verify.py) through the execute matrix, every seed,
# the MISS -> propose -> approve -> HIT loop and the quarantine -> restore loop; checks the Revit exe beside it;
# optionally proves isolation (a second AutoCAD fails fast on the pipe, Civil 3D never loads the bundle).
# Kills every AutoCAD it started. Publish both exes first (see HPAutoCad/README.md).
#Requires -Version 7.3
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\..\output\HPAutoCad.Mcp.Server\HPAutoCad.Mcp.Server.exe'),
    [string]$RevitExe = (Join-Path $PSScriptRoot '..\..\..\HPRebar\output\HPRebar.Mcp.Server\HPRebar.Mcp.Server.exe'),
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\output\live-verify'),
    [int]$StartupTimeoutSec = 420,
    [switch]$IncludeIsolation,
    [switch]$OnlyIsolation,
    [switch]$SkipRevit
)

. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning
if (-not (Test-Path $Exe)) { throw "Server exe not found: $Exe" }
$Exe = (Resolve-Path $Exe).Path
$revit = if (-not $SkipRevit -and (Test-Path $RevitExe)) { (Resolve-Path $RevitExe).Path } else { '' }
if (-not $revit) { Write-Host 'Revit exe not found or skipped: scenario E will not run' }
New-Item -ItemType Directory -Force $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$py = Join-Path $PSScriptRoot 'live-verify.py'
$mcp = Join-Path $PSScriptRoot 'mcp-call.py'
$env:PYTHONIOENCODING = 'utf-8'
$logDir = "$env:LOCALAPPDATA\HPAutoCad\McpBridge\logs"
$loaderLog = Join-Path $logDir 'loader.log'
$summaries = @()

function Run([string]$only, [string[]]$extra) {
    $argv = @('--exe', $Exe, '--out', $OutDir) + $extra
    if ($only) { $argv += @('--only', $only) }
    python $py @argv
    $script:summaries += [pscustomobject]@{ step = $(if ($only) { $only } else { 'main' }); exit = $LASTEXITCODE }
}

function Check([string]$name, [bool]$ok, [string]$detail) {
    Write-Host ("{0} {1} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail)
    $script:summaries += [pscustomobject]@{ step = $name; exit = $(if ($ok) { 0 } else { 1 }) }
}

function Read-StatusDetail([int]$processId) {
    # The bridge window's StatusDetail TextBlock (AutomationId) — where a listener fault is shown to the user.
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $byPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $byPid)
    if (-not $win) { return $null }
    $byId = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'StatusDetail')
    $el = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byId)
    if ($el) { return $el.Current.Name } else { return $null }
}

$p = Start-AcadWithBridge (Join-Path $PSScriptRoot 'bridge.scr') $StartupTimeoutSec
try {
    if (-not $pipeUp) { throw 'bridge pipe never came up' }
    Start-Sleep -Seconds 3

    if ($OnlyIsolation) { $IncludeIsolation = $true }
    if (-not $OnlyIsolation) {
    "=== opt-in OFF: execute must be refused"
    $null = Set-OptIn $false
    Run 'disabled' @()

    "=== opt-in ON: matrix, seeds, registry loops" + $(if ($revit) { ', Revit beside' } else { '' })
    if (-not (Set-OptIn $true)) { throw 'could not tick the opt-in' }
    $mainArgs = @('--reset-verify-tools')
    if ($revit) { $mainArgs += @('--revit-exe', $revit) }
    Run '' $mainArgs

    "=== no drawing: close the drawing through COM, expect a refusal"
    powershell -NoProfile -Command "`$ids = @(Get-Process acad | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$($p.Id)') { throw 'refusing COM' }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); `$a.ActiveDocument.Close(`$false); 'closed, docs left: ' + `$a.Documents.Count"
    Start-Sleep -Seconds 3
    Run 'nodoc' @()

    "=== busy: LINE waiting for input -> refused after the grace; ESC posted -> retry succeeds"
    powershell -NoProfile -Command "`$ids = @(Get-Process acad | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$($p.Id)') { throw 'refusing COM' }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); `$null = `$a.Documents.Add(); 'new drawing, docs: ' + `$a.Documents.Count"
    Start-Sleep -Seconds 3
    Run 'busy' @()
    }

    if ($IncludeIsolation) {
        "=== isolation 1: a second AutoCAD 2026 must fail fast on the pipe while the first keeps serving"
        $p2 = Start-Process -FilePath 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe' -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"', '/b', "`"$(Join-Path $PSScriptRoot 'bridge.scr')`"") -PassThru
        $detail = $null
        $sw = [Diagnostics.Stopwatch]::StartNew()
        while ($sw.Elapsed.TotalSeconds -lt 240 -and -not $detail) {
            Answer-SecureLoad | Out-Null
            Start-Sleep -Seconds 5
            try { $detail = Read-StatusDetail $p2.Id } catch { $detail = $null }
            if ($detail -and $detail -notmatch 'in use') { $detail = $null }
        }
        Check 'F second instance reports the pipe in use (status window)' ([bool]$detail) ("after $([int]$sw.Elapsed.TotalSeconds) s: '$detail'")
        $logText = (Get-ChildItem "$logDir\mcpbridge-*.log" | Sort-Object LastWriteTime | Select-Object -Last 1 | Get-Content -Tail 80) -join "`n"
        Check 'F second instance logged the pipe fault (shared log file)' ($logText -match 'could not create pipe') ''
        $ctx = python $mcp $Exe tools/call get_autocad_context '{}' | ConvertFrom-Json
        $ctxText = $ctx.result.content[0].text | ConvertFrom-Json
        Check 'F first instance still serves during the second' ($ctxText.host -eq 'autocad' -and -not $ctx.result.isError) (($ctxText | ConvertTo-Json -Compress).Substring(0, 120))
        if (-not $p2.HasExited) { Stop-Process -Id $p2.Id -Force -Confirm:$false }
        Start-Sleep -Seconds 5

        "=== isolation 2: Civil 3D 2026 (same R25.1) must not load the bundle (Platform=AutoCAD)"
        $loaderLines = if (Test-Path $loaderLog) { (Get-Content $loaderLog).Count } else { 0 }
        $c3d = Start-Process -FilePath 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe' -ArgumentList @('/nologo', '/product', 'C3D', '/language', '"en-US"') -PassThru
        $sw = [Diagnostics.Stopwatch]::StartNew()
        $loaded = $false
        while ($sw.Elapsed.TotalSeconds -lt 300) {
            Answer-SecureLoad | Out-Null
            Start-Sleep -Seconds 5
            $now = if (Test-Path $loaderLog) { (Get-Content $loaderLog).Count } else { 0 }
            if ($now -gt $loaderLines) { $loaded = $true; break }
            if ($c3d.HasExited) { break }
            if ($sw.Elapsed.TotalSeconds -ge 180 -and $c3d.MainWindowHandle -ne 0) { break }   # window up for a while, nothing loaded
        }
        $mainUp = $c3d.MainWindowHandle -ne 0
        Check 'F Civil 3D started and never wrote to loader.log' ($mainUp -and -not $loaded) ("window=$mainUp loaded=$loaded after $([int]$sw.Elapsed.TotalSeconds) s")
        if (-not $c3d.HasExited) { Stop-Process -Id $c3d.Id -Force -Confirm:$false }
    }
}
catch {
    Check 'wrapper aborted' $false $_.Exception.Message
}
finally {
    "=== killing acad"
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -Confirm:$false }
    Get-Process acad -ErrorAction SilentlyContinue | Where-Object { $_.StartTime -gt $p.StartTime.AddSeconds(-1) } | Stop-Process -Force -Confirm:$false -ErrorAction SilentlyContinue
}

"=== pipes seen by this user"
[System.IO.Directory]::GetFiles('\\.\pipe\') | ForEach-Object { [System.IO.Path]::GetFileName($_) } | Where-Object { $_ -like 'hprebar-mcp-*' -or $_ -like 'hpautocad-mcp-*' }
"=== audit lines"
Get-ChildItem "$env:APPDATA\HPAutoCad\McpBridge\audit\*.log" -ErrorAction SilentlyContinue | ForEach-Object { "$($_.Name): $((Get-Content $_.FullName).Count) lines" }
$summaries | ConvertTo-Json | Set-Content (Join-Path $OutDir 'wrapper-summary.json')
$failed = @($summaries | Where-Object exit -ne 0)
"wrapper: $($summaries.Count) steps, $($failed.Count) failed" + $(if ($failed) { ': ' + (($failed | % step) -join ', ') } else { '' })
exit $(if ($failed.Count -eq 0) { 0 } else { 1 })
