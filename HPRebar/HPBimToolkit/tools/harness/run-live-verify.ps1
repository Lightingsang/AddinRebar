<#
.SYNOPSIS
    Runs the HP BIM Toolkit live verification N times: start a second Revit on a model copy (start-revit-with-toolkit.ps1),
    drive the dashboard over CDP (live-verify.mjs), then close only the Revit this run started. A Revit that stops
    responding is reported as a failed run and killed (it is always the harness's own copy).
#>
param([int] $Runs = 1, [int] $DebugPort = 9333)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $PSCommandPath
$summary = @()

for ($run = 1; $run -le $Runs; $run++) {
    "===== run $run / $Runs ====="
    $start = & pwsh -NoProfile -File (Join-Path $here 'start-revit-with-toolkit.ps1') -DebugPort $DebugPort -TimeoutSec 300 2>&1
    $start | ForEach-Object { "  $_" }
    $revitPid = ($start | Select-String -Pattern '^REVIT_PID=(\d+)').Matches | Select-Object -First 1 | ForEach-Object { [int]$_.Groups[1].Value }

    $verify = & node (Join-Path $here 'live-verify.mjs') $DebugPort 2>&1
    $verify | ForEach-Object { "  $_" }
    $passed = $LASTEXITCODE -eq 0
    $responding = $revitPid -and (Get-Process -Id $revitPid -ErrorAction SilentlyContinue).Responding

    if ($revitPid) {
        Stop-Process -Id $revitPid -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
    }

    $summary += [pscustomobject]@{ Run = $run; Passed = $passed; RevitResponding = [bool]$responding; Result = ($verify | Select-Object -Last 1) }
}

$summary | Format-Table -AutoSize | Out-String
if ($summary.Where({ -not $_.Passed -or -not $_.RevitResponding }).Count -gt 0) { exit 1 }
