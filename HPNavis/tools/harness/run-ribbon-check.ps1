# Live check of the Ribbon tab "HPNavis" > panel "MCP" > button "MCP Bridge" in Navisworks Manage 2026: starts Roamer.exe
# with the sample model WITHOUT the show-window variable, so the bridge window can only come from the button; finds the
# tab header through UI Automation scoped to that Roamer, selects it, takes a screenshot of the Ribbon (icon check),
# invokes "MCP Bridge", expects the bridge window + the "status window opened" log line, invokes again and expects
# one window (Activate, not a twin), and checks the Add-ins entry is gone. Anything UI Automation cannot see is
# printed as MANUAL and exits 2 - never a PASS. Closes only the Roamer it started, never saves the model.
#
#   powershell.exe -ExecutionPolicy Bypass -File HPNavis/tools/harness/run-ribbon-check.ps1 [-Model <.nwd>] [-WithNoDoc]
#
# Windows PowerShell 5.1 (UIA); relaunches itself when started from pwsh. Outputs under HPNavis/output/ribbon-check/.
param(
    [string]$Model = '',
    [switch]$WithNoDoc    # also start a Roamer without a model: Navisworks greys every tab on its start page, so only the header is checked
)

if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath)
    if ($Model) { $argList += @('-Model', $Model) }
    if ($WithNoDoc) { $argList += '-WithNoDoc' }
    & $ps @argList
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'harness-common.ps1')

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
if (-not $Model) { $Model = Join-Path $script:NavisDir 'Samples\gatehouse\gatehouse_pub.nwd' }
if (-not (Test-Path $Model)) { throw "model not found: $Model" }
$outDir = Join-Path $repo 'HPNavis\output\ribbon-check'
New-Item -ItemType Directory -Force $outDir | Out-Null
$TabId = 'ID_HPNAVIS'
$TabTitle = 'HPNavis'

Assert-NoNavisworksRunning
Assert-PluginDeployed

$results = @()
function Check([string]$name, [bool]$ok, [string]$detail = '') {
    $script:results += [ordered]@{ name = $name; result = $(if ($ok) { 'PASS' } else { 'FAIL' }); detail = $detail }
    Write-Host ("{0} {1} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail)
}
function Manual([string]$name, [string]$detail) {
    $script:results += [ordered]@{ name = $name; result = 'MANUAL'; detail = $detail }
    Write-Host "MANUAL $name $detail"
}

# Deployed files first: a missing layout/strings/icon is the most likely reason for a tab that never appears.
foreach ($rel in 'en-US\HPNavisRibbon.xaml', 'en-US\HPNavisRibbon.name', 'Images\McpBridge_16.png', 'Images\McpBridge_32.png') {
    Check "deployed $rel" (Test-Path (Join-Path $script:PluginDir $rel)) ''
}

# One Roamer (with or without a model): tab once, screenshot, window from the button, Activate on the second click.
function Test-RibbonOn([string]$model, [string]$tag) {
    $proc = $null
    $logStart = Get-Date
    try {
        $proc = Start-NavisworksWithModel $model 180 $false
        # the Ribbon keeps initialising after the main window title settles (longer with no model to load)
        Start-Sleep -Seconds 8

        $tabs = @()
        for ($i = 0; $i -lt 10 -and $tabs.Count -eq 0; $i++) { $tabs = @(Find-RibbonTabHeaders $TabId $TabTitle); if ($tabs.Count -eq 0) { Start-Sleep -Seconds 1 } }
        if ($tabs.Count -eq 0) { Manual "$tag tab '$TabTitle' visible exactly once" 'UI Automation found no tab header; check the screenshot' }
        else { Check "$tag tab '$TabTitle' visible exactly once" ($tabs.Count -eq 1) "found $($tabs.Count) header(s), control type $($tabs[0].Current.ControlType.ProgrammaticName)" }

        if (-not $model) {
            # With no document Navisworks shows its start page and greys every Ribbon tab, ours included: the button is
            # unreachable by design, the window still opens through the env variable / ExecuteAddInPlugin (live-verify nodoc).
            Check "$tag tab header disabled with the start page up (Navisworks greys every tab)" ($tabs.Count -eq 1 -and -not $tabs[0].Current.IsEnabled) ''
            $null = Set-RoamerMainWindowForeground; Start-Sleep -Milliseconds 500
            $null = Save-RibbonScreenshot (Join-Path $outDir "ribbon-$tag.png")
            return
        }

        $selected = Select-RibbonTab $TabId $TabTitle '^MCP Bridge$'
        # the button is only on screen once our tab is the displayed one; the screenshot must show that state
        $button = Wait-RibbonButtonVisible '^MCP Bridge$' 10
        $null = Set-RoamerMainWindowForeground; Start-Sleep -Milliseconds 500
        $shot = Join-Path $outDir "ribbon-$tag.png"
        $null = Save-RibbonScreenshot $shot
        Manual "$tag icon crisp and reads as a window with a plug" "see $shot (tab selected: $selected, button on screen: $([bool]$button))"

        Check "$tag no bridge window before the click (no show-window variable)" ((Count-BridgeWindows) -eq 0) ''

        if (-not $button) { $button = Find-RibbonButton '^MCP Bridge$' }
        if (-not $button) { Manual "$tag button 'MCP Bridge' opens the bridge window" 'UI Automation found no button named MCP Bridge' }
        else {
            Check "$tag button 'MCP Bridge' enabled" ($button.Current.IsEnabled) ''
            $null = Invoke-RibbonButton '^MCP Bridge$'
            $win = $null
            for ($i = 0; $i -lt 20 -and -not $win; $i++) { Start-Sleep -Milliseconds 500; $win = Find-BridgeWindow 1 }
            Check "$tag button 'MCP Bridge' opens the bridge window" ([bool]$win) ''
            $opened = @(Get-BridgeLogTail 200 | Where-Object { $_ -like '*MCP bridge status window opened*' -and $_ -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})' -and [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', $null) -ge $logStart.AddSeconds(-1) }).Count
            Check "$tag log says the status window opened" ($opened -ge 1) "x$opened"
            $null = Invoke-RibbonButton '^MCP Bridge$'
            Start-Sleep -Seconds 1
            Check "$tag second click activates, does not open a twin" ((Count-BridgeWindows) -eq 1) "windows: $(Count-BridgeWindows)"
        }

        # The Add-ins entry is hidden (AddInLocation.None). Navisworks only shows a "Tool add-ins" tab while some
        # AddInPlugin is visible; ours was the only one here, so the tab itself must be gone — and if another plugin
        # brings the tab back, our button must not be on it (buttons are only in the UIA tree while their tab shows).
        $addInsTab = @(Find-RibbonTabHeaders 'ID_TabAddIns' 'Tool add-ins 1') + @(Find-RibbonTabHeaders 'ID_TabAddIns' 'Add-ins')
        if ($addInsTab.Count -eq 0) { Check "$tag no 'HPNavis MCP' Add-ins entry (no Add-ins tab at all)" $true '' }
        else {
            $null = Select-RibbonTab 'ID_TabAddIns' $addInsTab[0].Current.Name ''
            $addIn = Find-RibbonButton '^HPNavis MCP$'
            Check "$tag no 'HPNavis MCP' Add-ins entry (Add-ins tab present, checked its buttons)" (-not $addIn) ''
            $null = Select-RibbonTab $TabId $TabTitle '^MCP Bridge$'
        }

        $errors = @(Get-BridgeLogTail 300 | Where-Object { $_ -match ' \[(ERR|FTL)\] ' -and $_ -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})' -and [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', $null) -ge $logStart.AddSeconds(-1) }).Count
        Check "$tag no ERR/FTL in the bridge log" ($errors -eq 0) "x$errors"
    }
    catch { Check "$tag run completed" $false $_.Exception.Message }
    finally { Stop-Navisworks $proc }
}

Test-RibbonOn $Model 'model'
if ($WithNoDoc) { Test-RibbonOn '' 'nodoc' }

$fails = @($results | Where-Object { $_.result -eq 'FAIL' }).Count
$manual = @($results | Where-Object { $_.result -eq 'MANUAL' }).Count
$pass = @($results | Where-Object { $_.result -eq 'PASS' }).Count
# the icon itself is always a human check (screenshot); any other MANUAL means UI Automation was blind -> exit 2
$blind = @($results | Where-Object { $_.result -eq 'MANUAL' -and $_.name -notlike '*icon crisp*' }).Count
[pscustomobject]@{ pass = $pass; fail = $fails; manual = $manual; results = $results } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $outDir 'summary.json') -Encoding UTF8
Write-Host "`nribbon check => $pass PASS, $fails FAIL, $manual MANUAL  (summary: $outDir\summary.json)"
exit $(if ($fails -gt 0) { 1 } elseif ($blind -gt 0) { 2 } else { 0 })
