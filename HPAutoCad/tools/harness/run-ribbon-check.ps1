# Unattended check of the "MCP AutoCAD" Ribbon tab in a live AutoCAD 2026: starts AutoCAD with the bridge
# (no listener: ribbon.scr only opens nothing — the tab must appear on its own), then through UI Automation
# finds the tab (exactly one), switches workspace twice through COM and checks it is still exactly one, and
# presses the buttons whose effect is observable from outside: "Bật listener" (pipe appears), "Tắt listener"
# (pipe disappears), "Bảng điều khiển" (bridge window appears), "Trạng thái" (no failure in loader.log).
# A check the automation peers cannot see is reported as MANUAL, never as PASS. Kills only the AutoCAD it started.
#Requires -Version 7.3
param(
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\output\ribbon-check'),
    [int]$StartupTimeoutSec = 420,
    [string]$OtherWorkspace = '3D Modeling'
)

. (Join-Path $PSScriptRoot 'harness-common.ps1')
Assert-NoAutocadRunning
New-Item -ItemType Directory -Force $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path
$loaderLog = "$env:LOCALAPPDATA\HPAutoCad\McpBridge\logs\loader.log"
$loaderLines = if (Test-Path $loaderLog) { (Get-Content $loaderLog).Count } else { 0 }
$pipe = '\\.\pipe\hpautocad-mcp-2026'
$results = @()
$manual = @()

function Check([string]$name, [bool]$ok, [string]$detail) { $script:results += [pscustomobject]@{ name = $name; pass = $ok; detail = $detail }; Write-Host ("{0} {1} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail) }
function Manual([string]$name, [string]$why) { $script:manual += $name; Write-Host "MANUAL $name — $why" }
function Wait-Pipe([bool]$expected, [int]$seconds) { $sw = [Diagnostics.Stopwatch]::StartNew(); while ($sw.Elapsed.TotalSeconds -lt $seconds) { if ((Test-Path $pipe) -eq $expected) { return $true }; Start-Sleep -Milliseconds 500 }; return ((Test-Path $pipe) -eq $expected) }
function Com([string]$script) {
    # Windows PowerShell 5.1 for GetActiveObject; pid-guarded like every other harness.
    $ps = "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$script:acadPid') { throw ('refusing COM: acad pids [' + (`$ids -join ',') + '], harness owns $script:acadPid') }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); $script"
    for ($i = 0; $i -lt 4; $i++) { $out = powershell -NoProfile -Command $ps 2>&1; if ($LASTEXITCODE -eq 0) { return ($out -join "`n") }; Write-Host "COM attempt $($i + 1) failed: $(($out -join ' ').Substring(0, [Math]::Min(160, ($out -join ' ').Length)))"; Start-Sleep -Seconds 4 }
    return ($out -join "`n")
}

# The bridge auto-loads with the bundle; this script only needs a drawing open, no HPMCP command.
$scr = Join-Path $OutDir 'ribbon.scr'
Set-Content $scr "_.REGEN`r`n"
$p = Start-AcadWithBridge $scr 45   # the listener stays off, so the pipe wait times out on purpose; the tab wait below is the real one
try {
    # Start-AcadWithBridge waits for the pipe, which stays down here (listener off): give the Ribbon a moment instead.
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $tabs = @()
    while ($sw.Elapsed.TotalSeconds -lt 120 -and $tabs.Count -eq 0) { Answer-SecureLoad | Out-Null; Start-Sleep -Seconds 5; try { $tabs = @(Find-RibbonTabs $p.Id 'HPAUTOCAD_MCP_TAB') } catch { $tabs = @() } }
    if ($tabs.Count -eq 0 -and $p.HasExited) { throw "acad exited early (code $($p.ExitCode))" }
    $created = (Get-Content $loaderLog | Select-Object -Skip $loaderLines) -match 'ribbon tab HPAUTOCAD_MCP_TAB created'
    Check 'loader.log says the tab was created' ([bool]$created) ("$($created.Count) line(s) after $([int]$sw.Elapsed.TotalSeconds) s")
    if ($tabs.Count -eq 0) {
        Manual 'tab "MCP AutoCAD" visible exactly once' 'UI Automation did not expose a tab button with AutomationId HPAUTOCAD_MCP_TAB; check by eye'
    } else {
        Check 'tab "MCP AutoCAD" visible exactly once' ($tabs.Count -eq 1) ("$($tabs.Count) tab(s)")
    }

    $original = (Com "`$a.ActiveDocument.GetVariable('WSCURRENT')").Trim()
    # SetVariable, not SendCommand: WSCURRENT through the command line never returned in a live run (the ribbon
    # rebuild swallows the command echo), while the system-variable route switches and returns at once.
    $switched = Com "`$a.ActiveDocument.SetVariable('WSCURRENT', '$OtherWorkspace'); 'ok'"
    Start-Sleep -Seconds 6
    $afterSwitch = @(Find-RibbonTabs $p.Id 'HPAUTOCAD_MCP_TAB')
    $back = Com "`$a.ActiveDocument.SetVariable('WSCURRENT', '$original'); 'ok'"
    Start-Sleep -Seconds 6
    $afterBack = @(Find-RibbonTabs $p.Id 'HPAUTOCAD_MCP_TAB')
    $now = (Com "`$a.ActiveDocument.GetVariable('WSCURRENT')").Trim()
    if ($tabs.Count -eq 0) { Manual 'exactly one tab after switching workspace and back' "UIA cannot count tabs; workspace went '$original' -> '$OtherWorkspace' -> '$now'" }
    else { Check 'exactly one tab after switching workspace and back' ($afterSwitch.Count -eq 1 -and $afterBack.Count -eq 1 -and $now -eq $original) ("'$original' -> '$OtherWorkspace': $($afterSwitch.Count) tab(s); back to '$now': $($afterBack.Count) tab(s)") }

    $selected = Select-RibbonTab $p.Id 'HPAUTOCAD_MCP_TAB'
    Check 'listener is off before the button test' (-not (Test-Path $pipe)) "pipe exists=$(Test-Path $pipe)"
    if (Invoke-RibbonButton $p.Id '^Bật listener') { Check 'button "Bật listener" brings the pipe up' (Wait-Pipe $true 10) "pipe exists=$(Test-Path $pipe)" }
    else { Manual 'button "Bật listener" brings the pipe up' 'button not found through UIA' }
    if (Invoke-RibbonButton $p.Id '^Tắt listener') { Check 'button "Tắt listener" takes the pipe down' (Wait-Pipe $false 10) "pipe exists=$(Test-Path $pipe)" }
    else { Manual 'button "Tắt listener" takes the pipe down' 'button not found through UIA' }
    if (Invoke-RibbonButton $p.Id '^Bảng điều khiển') {
        Start-Sleep -Seconds 3
        $window = $null; try { $window = Find-BridgeWindow } catch { }
        $box = $null
        if ($window) { $box = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'AllowExecution'))) }
        Check 'button "Bảng điều khiển" opens the bridge window' ([bool]$box) ("window=" + $(if ($window) { $window.Current.Name } else { 'none' }))
    } else { Manual 'button "Bảng điều khiển" opens the bridge window' 'button not found through UIA' }
    if (Invoke-RibbonButton $p.Id '^Trạng thái') {
        Start-Sleep -Seconds 2
        $failed = (Get-Content $loaderLog | Select-Object -Skip $loaderLines) -match 'failed'
        Check 'button "Trạng thái" runs without a failure in loader.log' ($failed.Count -eq 0) ("failed lines: $($failed.Count)")
    } else { Manual 'button "Trạng thái" runs without a failure' 'button not found through UIA' }
}
catch {
    Check 'ribbon check aborted' $false $_.Exception.Message
}
finally {
    "=== killing acad"
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -Confirm:$false }
}

"=== new loader.log lines"
Get-Content $loaderLog | Select-Object -Skip $loaderLines | Select-String -Pattern 'ribbon|failed|initialize' | ForEach-Object { $_.Line }
$passed = @($results | Where-Object pass).Count
$summary = [pscustomobject]@{ passed = $passed; total = $results.Count; manual = $manual }
$summary | ConvertTo-Json -Compress
@{ results = $results; manual = $manual } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutDir 'summary.json')
exit $(if ($passed -ne $results.Count) { 1 } elseif ($manual.Count -gt 0) { 2 } else { 0 })
