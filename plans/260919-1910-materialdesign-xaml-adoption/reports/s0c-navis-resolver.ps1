<#
.SYNOPSIS
    Spike S0-C: HPNavis (net48) with MaterialDesignThemes beside a foreign plugin that loads toolkit 4.9.0 at start.

.DESCRIPTION
    Plants a throw-away plugin folder AAASpikeForeign (EventWatcherPlugin + MaterialDesignThemes.Wpf 4.9.0 +
    MaterialDesignColors 2.1.4 + Microsoft.Xaml.Behaviors 1.1.39) into the Navisworks Manage 2026 per-user plugins,
    runs HPNavis/tools/harness/run-ribbon-check.ps1 (starts its own Roamer, opens the bridge window from the ribbon),
    then reads the bridge log (MD-SPIKE line: which toolkit version the BAML bound to, copies in the AppDomain,
    PluginAssemblyResolver "Resolved" lines) and the foreign plugin's own log. Removes the foreign folder in finally.

.EXAMPLE
    pwsh s0c-navis-resolver.ps1 -ForeignPluginDir <scratch>\foreign-plugin -OutDir <reports>\s0c-loose [-Tag loose|repacked]
#>
param(
    [Parameter(Mandatory)] [string] $ForeignPluginDir,
    [Parameter(Mandatory)] [string] $OutDir,
    [ValidateSet('loose', 'repacked')] [string] $Tag = 'loose'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$pluginsRoot = Join-Path $env:APPDATA 'Autodesk\Navisworks Manage 2026\Plugins'
$foreign = Join-Path $pluginsRoot 'AAASpikeForeign'
$logDir = Join-Path $env:LOCALAPPDATA 'HPNavis\McpBridge\logs'
New-Item -ItemType Directory -Force $OutDir | Out-Null
if (Get-Process Roamer -ErrorAction SilentlyContinue) { throw 'Close Navisworks first.' }

$logMark = Get-Date
try {
    New-Item -ItemType Directory -Force $foreign | Out-Null
    Copy-Item (Join-Path $ForeignPluginDir 'bin\Debug\net48\AAASpikeForeign.dll') $foreign -Force
    Get-ChildItem (Join-Path $ForeignPluginDir 'planted') -Filter '*.dll' | Copy-Item -Destination $foreign -Force
    Remove-Item (Join-Path $foreign 'spike-foreign.log') -ErrorAction SilentlyContinue
    "planted foreign plugin: " + ((Get-ChildItem $foreign | ForEach-Object Name) -join ', ')

    $ribbon = Join-Path $repo 'HPNavis\tools\harness\run-ribbon-check.ps1'
    $ribbonOut = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ribbon 2>&1
    $ribbonExit = $LASTEXITCODE
    $ribbonOut | Set-Content (Join-Path $OutDir 'ribbon-check-output.txt') -Encoding UTF8
    "ribbon-check exit $ribbonExit; " + (@($ribbonOut | Where-Object { $_ -match '^(PASS|FAIL|MANUAL)' }).Count) + " result lines"
    $ribbonOut | Where-Object { $_ -match '^(FAIL|MANUAL)' } | ForEach-Object { "  $_" }

    Start-Sleep -Seconds 2
    $foreignLog = Get-Content (Join-Path $foreign 'spike-foreign.log') -ErrorAction SilentlyContinue
    $inv = [Globalization.CultureInfo]::InvariantCulture
    $bridgeLog = @(Get-ChildItem $logDir -Filter 'mcpbridge-*.log' | ForEach-Object { Get-Content $_.FullName -Encoding UTF8 } |
        Where-Object { $_ -match '^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d' -and [datetime]::ParseExact($_.Substring(0, 19), 'yyyy-MM-dd HH:mm:ss', $inv) -ge $logMark.AddSeconds(-2) })
    $bridgeLog | Set-Content (Join-Path $OutDir 'bridge-log.txt') -Encoding UTF8
    $spike = @($bridgeLog | Where-Object { $_ -match 'MD-SPIKE' })
    $resolved = @($bridgeLog | Where-Object { $_ -match 'MaterialDesign|Xaml\.Behaviors' -and $_ -match 'resolv|Resolved|->' })
    $errors = @($bridgeLog | Where-Object { $_ -match '\[ERR\]|Exception' })

    $checks = @()
    function Check([string]$name, [bool]$ok, [string]$detail = '') { $script:checks += [pscustomobject]@{ name = $name; ok = $ok; detail = $detail }; Write-Host ("{0} {1} {2}" -f $(if ($ok) { 'PASS' } else { 'FAIL' }), $name, $detail) }
    Check 'foreign plugin loaded its 4.9.0 copy at start' ($null -ne $foreignLog -and ($foreignLog -join ' ') -match 'loaded 4\.9\.0\.0') ($foreignLog -join ' | ')
    Check 'ribbon check passed (window opened from the ribbon, no FAIL)' ($ribbonExit -ne 1 -and @($ribbonOut | Where-Object { $_ -match '^FAIL' }).Count -eq 0) "exit $ribbonExit"
    Check 'bridge window logged its MD-SPIKE line' ($spike.Count -ge 1) ($spike -join ' | ')
    if ($Tag -eq 'loose') {
        Check 'loose: BAML bound to OUR 5.3.2 copy (xamlBundledTheme=5.3.2.0, templatePart=5.3.2.0) despite the foreign 4.9.0' ($spike.Count -ge 1 -and $spike[-1] -match 'xamlBundledTheme=5\.3\.2\.0' -and $spike[-1] -match 'templatePart=5\.3\.2\.0') ''
        Check 'loose: resolver served the toolkit from our folder' ($resolved.Count -ge 1) ($resolved -join ' | ')
    } else {
        Check 'repacked: no assembly named MaterialDesignThemes.Wpf but the foreign 4.9.0 (copiesInProcess lists only 4.9.0.0@AAASpikeForeign)' ($spike.Count -ge 1 -and $spike[-1] -match 'copiesInProcess=4\.9\.0\.0@AAASpikeForeign(\s|$|")' -and $spike[-1] -notmatch '5\.3\.2\.0@') ''
    }
    Check 'isIMaterialDesignThemeDictionary=True + packIcon=True' ($spike.Count -ge 1 -and $spike[-1] -match 'isIMaterialDesignThemeDictionary=True' -and $spike[-1] -match 'packIcon=True') ''
    Check 'no error / exception in the bridge log' ($errors.Count -eq 0) (($errors | Select-Object -First 3) -join ' | ')
    $failed = @($checks | Where-Object { -not $_.ok }).Count
    Write-Host ("SUMMARY S0-C {0}: {1} pass, {2} fail" -f $Tag, ($checks.Count - $failed), $failed)
    $checks | ForEach-Object { "{0} {1} {2}" -f $(if ($_.ok) { 'PASS' } else { 'FAIL' }), $_.name, $_.detail } | Set-Content (Join-Path $OutDir 'checks.txt') -Encoding UTF8
    exit $(if ($failed -gt 0) { 1 } else { 0 })
}
finally {
    if (Get-Process Roamer -ErrorAction SilentlyContinue) { Get-Process Roamer | Stop-Process -Force -Confirm:$false }
    if (Test-Path $foreign) { Remove-Item $foreign -Recurse -Force; "foreign plugin folder removed" }
}
