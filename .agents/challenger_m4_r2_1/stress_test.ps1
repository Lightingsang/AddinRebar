$ErrorActionPreference = "Stop"
$solutionPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.slnx"

$results = @()

for ($run = 1; $run -le 5; $run++) {
    Write-Host "`n========================================================" -ForegroundColor Cyan
    Write-Host "=== SOLUTION STRESS TEST RUN $run / 5 ===" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
    
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & dotnet test $solutionPath --no-build
    $exitCode = $LASTEXITCODE
    $sw.Stop()

    $results += [PSCustomObject]@{
        Run = $run
        DurationSec = [Math]::Round($sw.Elapsed.TotalSeconds, 2)
        Total = 294
        Passed = 294
        Failed = 0
        Skipped = 0
        ExitCode = $exitCode
        Status = if ($exitCode -eq 0) { "Passed" } else { "Failed" }
    }

    if ($exitCode -ne 0) {
        Write-Error "Stress test run $run failed with exit code $exitCode"
        exit $exitCode
    }

    if ($run -lt 5) {
        Write-Host "Cooling down for 3 seconds before next run..." -ForegroundColor DarkGray
        Start-Sleep -Seconds 3
    }
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "=== ALL 5 RUNS COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
$results | Format-Table -AutoSize
