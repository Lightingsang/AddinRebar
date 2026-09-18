# Unattended check of the "HPAutoCad" Ribbon tab (panel "MCP", button "MCP Bridge") in a live AutoCAD 2026:
# starts AutoCAD with the bridge (no listener: ribbon.scr opens nothing — the tab must appear on its own), then
# through UI Automation finds the tab (exactly one), switches workspace there and back through COM and checks it
# is still exactly one, flips COLORTHEME there and back (read back through COM, one loader.log "panel added" line
# per flip, still one tab, a screenshot per theme), presses "MCP Bridge" (the bridge window appears) and presses it
# again (no second window). A check the automation peers cannot see — the icon — is reported as MANUAL with the
# screenshots, never as PASS. Restores the workspace and theme in finally; kills only the AutoCAD it started.
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
$tabId = 'HPAUTOCAD_MCP_TAB'
$results = @()
$manual = @()

function Check([string]$name, [bool]$ok, [string]$detail) { $script:results += [pscustomobject]@{ name = $name; pass = $ok; detail = $detail }; Write-Host ("{0} {1} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail) }
function Manual([string]$name, [string]$why) { $script:manual += $name; Write-Host "MANUAL $name — $why" }
function Com([string]$script) {
    # Windows PowerShell 5.1 for GetActiveObject; pid-guarded like every other harness.
    $ps = "`$ids = @(Get-Process acad -ErrorAction SilentlyContinue | % Id); if (`$ids.Count -ne 1 -or [string]`$ids[0] -ne '$script:acadPid') { throw ('refusing COM: acad pids [' + (`$ids -join ',') + '], harness owns $script:acadPid') }; `$a = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); $script"
    for ($i = 0; $i -lt 4; $i++) { $out = powershell -NoProfile -Command $ps 2>&1; if ($LASTEXITCODE -eq 0) { return ($out -join "`n") }; Write-Host "COM attempt $($i + 1) failed: $(($out -join ' ').Substring(0, [Math]::Min(160, ($out -join ' ').Length)))"; Start-Sleep -Seconds 4 }
    return ($out -join "`n")
}
function Count-BridgeWindows {
    # The bridge window is the only place with the "AllowExecution" checkbox; one checkbox = one window.
    $frame = Get-AcadFrame $script:acadPid
    if (-not $frame) { return -1 }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'AllowExecution')
    return $frame.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond).Count
}
function Count-RibbonButtons([string]$name) {
    # Every Button in the frame whose UIA name is exactly $name — the Ribbon exposes a RibbonButton that way once its tab shows.
    $frame = Get-AcadFrame $script:acadPid
    if (-not $frame) { return -1 }
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    $n = 0
    foreach ($b in $frame.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)) { if (($b.Current.Name -replace "`r?`n", ' ') -eq $name) { $n++ } }
    return $n
}
# The tab is shared by every HP add-in for AutoCAD (one tab, one panel per tool): the loader logs "panel added" whenever
# it (re)builds its own panel, and "tab created" only when it was the first add-in to reach the Ribbon.
function Count-CreatedLines { @((Get-Content $loaderLog | Select-Object -Skip $loaderLines) -match "ribbon panel HPAUTOCAD_MCP_PANEL added to tab $tabId").Count }
# WSCURRENT and COLORTHEME are profile settings: whatever happens, the finally block puts them back.
$originalWorkspace = $null
$originalTheme = $null

# The bridge auto-loads with the bundle; this script only needs a drawing open, no HPMCP command.
$scr = Join-Path $OutDir 'ribbon.scr'
Set-Content $scr "_.REGEN`r`n"
$p = Start-AcadWithBridge $scr 45   # the listener stays off, so the pipe wait times out on purpose; the tab wait below is the real one
try {
    # Start-AcadWithBridge waits for the pipe, which stays down here (listener off): give the Ribbon a moment instead.
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $tabs = @()
    while ($sw.Elapsed.TotalSeconds -lt 120 -and $tabs.Count -eq 0) { Answer-SecureLoad | Out-Null; Start-Sleep -Seconds 5; try { $tabs = @(Find-RibbonTabs $p.Id $tabId) } catch { $tabs = @() } }
    if ($tabs.Count -eq 0 -and $p.HasExited) { throw "acad exited early (code $($p.ExitCode))" }
    $created = (Get-Content $loaderLog | Select-Object -Skip $loaderLines) -match "ribbon panel HPAUTOCAD_MCP_PANEL added to tab $tabId \(tab (created|existing), bridge available\)"
    Check 'loader.log says the MCP panel was added to the shared tab with the bridge available' ([bool]$created) ("$($created.Count) line(s) after $([int]$sw.Elapsed.TotalSeconds) s")
    if ($tabs.Count -eq 0) {
        Manual 'tab "HPAutoCad" visible exactly once' "UI Automation did not expose a tab button with AutomationId $tabId; check by eye"
    } else {
        Check 'tab "HPAutoCad" visible exactly once' ($tabs.Count -eq 1) ("$($tabs.Count) tab(s), name '$($tabs[0].Current.Name)'")
        Check 'tab title is "HPAutoCad"' ($tabs[0].Current.Name -eq 'HPAutoCad') ("'$($tabs[0].Current.Name)'")
    }

    $original = (Com "`$a.ActiveDocument.GetVariable('WSCURRENT')").Trim()
    # SetVariable, not SendCommand: WSCURRENT through the command line never returned in a live run (the ribbon
    # rebuild swallows the command echo), while the system-variable route switches and returns at once.
    $originalWorkspace = $original
    $null = Com "`$a.ActiveDocument.SetVariable('WSCURRENT', '$OtherWorkspace'); 'ok'"
    Start-Sleep -Seconds 6
    $afterSwitch = @(Find-RibbonTabs $p.Id $tabId)
    $null = Com "`$a.ActiveDocument.SetVariable('WSCURRENT', '$original'); 'ok'"
    Start-Sleep -Seconds 6
    $afterBack = @(Find-RibbonTabs $p.Id $tabId)
    $now = (Com "`$a.ActiveDocument.GetVariable('WSCURRENT')").Trim()
    if ($now -eq $original) { $originalWorkspace = $null }   # restored on the happy path
    if ($tabs.Count -eq 0) { Manual 'exactly one tab after switching workspace and back' "UIA cannot count tabs; workspace went '$original' -> '$OtherWorkspace' -> '$now'" }
    else { Check 'exactly one tab after switching workspace and back' ($afterSwitch.Count -eq 1 -and $afterBack.Count -eq 1 -and $now -eq $original) ("'$original' -> '$OtherWorkspace': $($afterSwitch.Count) tab(s); back to '$now': $($afterBack.Count) tab(s)") }

    $null = Select-RibbonTab $p.Id $tabId
    $shot = Join-Path $OutDir 'ribbon-tab.png'
    $null = Save-RibbonScreenshot $p.Id $shot

    # COLORTHEME flips the icon ink, so the loader removes and re-creates its panel (never the shared tab) on the
    # next idle tick: the proof is the read-back value, one more "panel added" line per flip, and still exactly one tab.
    # [int16]: COM SetVariable wants the VARIANT type the sysvar is stored as — AutoCAD integer sysvars are
    # 16-bit (VT_I2), the same reason the managed SetSystemVariable takes a short; [int] (VT_I4) is rejected.
    $theme = (Com "`$a.ActiveDocument.GetVariable('COLORTHEME')").Trim()
    $originalTheme = $theme
    $otherTheme = if ($theme -eq '0') { 1 } else { 0 }
    $createdBefore = Count-CreatedLines
    $null = Com "`$a.ActiveDocument.SetVariable('COLORTHEME', [int16]$otherTheme); 'ok'"
    Start-Sleep -Seconds 6
    $themeNow = (Com "`$a.ActiveDocument.GetVariable('COLORTHEME')").Trim()
    $afterTheme = @(Find-RibbonTabs $p.Id $tabId)
    $createdAfterFlip = Count-CreatedLines
    $null = Select-RibbonTab $p.Id $tabId
    $shotOther = Join-Path $OutDir "ribbon-tab-theme$otherTheme.png"
    $null = Save-RibbonScreenshot $p.Id $shotOther
    $null = Com "`$a.ActiveDocument.SetVariable('COLORTHEME', [int16]$theme); 'ok'"
    Start-Sleep -Seconds 6
    $themeBack = (Com "`$a.ActiveDocument.GetVariable('COLORTHEME')").Trim()
    $afterThemeBack = @(Find-RibbonTabs $p.Id $tabId)
    $createdAfterBack = Count-CreatedLines
    if ($themeBack -eq $theme) { $originalTheme = $null }   # restored on the happy path
    Check 'COLORTHEME flipped and came back through COM' ($themeNow -eq "$otherTheme" -and $themeBack -eq $theme) ("$theme -> $themeNow -> $themeBack")
    Check 'the loader rebuilt its panel on each COLORTHEME change' ($createdAfterFlip -gt $createdBefore -and $createdAfterBack -gt $createdAfterFlip) ("panel-added lines $createdBefore -> $createdAfterFlip -> $createdAfterBack")
    if ($tabs.Count -eq 0) { Manual 'exactly one tab after switching COLORTHEME and back' 'UIA cannot count tabs' }
    else { Check 'exactly one tab after switching COLORTHEME and back' ($afterTheme.Count -eq 1 -and $afterThemeBack.Count -eq 1) ("theme $theme -> ${otherTheme}: $($afterTheme.Count) tab(s); back: $($afterThemeBack.Count) tab(s)") }
    $null = Select-RibbonTab $p.Id $tabId
    Manual 'icon "MCP Bridge" is the window-with-plug glyph, crisp, ink matching the theme' "UIA cannot read pixels; look at $shot (theme $theme) and $shotOther (theme $otherTheme)"

    $buttons = Count-RibbonButtons 'MCP Bridge'
    Check 'exactly one Ribbon button named "MCP Bridge"' ($buttons -eq 1) ("buttons=$buttons")
    $windows = Count-BridgeWindows
    Check 'no bridge window before the click' ($windows -eq 0) ("windows=$windows")
    if (Invoke-RibbonButton $p.Id '^MCP Bridge$') {
        Start-Sleep -Seconds 3
        $windows = Count-BridgeWindows
        Check 'button "MCP Bridge" opens the bridge window' ($windows -eq 1) ("windows=$windows")
        $null = Invoke-RibbonButton $p.Id '^MCP Bridge$'
        Start-Sleep -Seconds 3
        $windows = Count-BridgeWindows
        Check 'second click opens no second window' ($windows -eq 1) ("windows=$windows")
    } else { Manual 'button "MCP Bridge" opens the bridge window' 'button not found through UIA' }
    $anyFailure = (Get-Content $loaderLog | Select-Object -Skip $loaderLines) -match 'failed|exception'
    Check 'no failure or exception in loader.log for the whole run' ($anyFailure.Count -eq 0) ("lines: $($anyFailure.Count)")
}
catch {
    Check 'ribbon check aborted' $false $_.Exception.Message
}
finally {
    # A throw between a set and its restore must not leave the user's profile on the other theme or workspace.
    if (-not $p.HasExited) {
        if ($null -ne $originalTheme) { "restoring COLORTHEME $originalTheme"; $null = Com "`$a.ActiveDocument.SetVariable('COLORTHEME', [int16]$originalTheme); 'ok'" }
        if ($null -ne $originalWorkspace) { "restoring WSCURRENT '$originalWorkspace'"; $null = Com "`$a.ActiveDocument.SetVariable('WSCURRENT', '$originalWorkspace'); 'ok'" }
    }
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
