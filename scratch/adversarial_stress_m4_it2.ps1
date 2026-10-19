# Adversarial Stress Test Harness for HPGrasshopper M4 Iteration 2
# Tests headless execution stability, timings, exit codes, and zero modal popup dialogs

$bridgeProjectPath = "HPGrasshopper/HPGrasshopper.McpBridge.Tests"
$serverProjectPath = "HPGrasshopper/HPGrasshopper.Mcp.Server.Tests"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "STAGE 1: Consecutive Stress Run of HPGrasshopper.McpBridge.Tests" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$bridgeResults = @()
for ($i = 1; $i -le 5; $i++) {
    Write-Host "[Bridge Run $i/5] Starting..." -NoNewline
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $output = dotnet test $bridgeProjectPath -c Release --no-build 2>&1 | Out-String
    $sw.Stop()
    $dur = [math]::Round($sw.Elapsed.TotalSeconds, 2)
    $hasPassed = ($output -match "Passed!\s*-\s*Failed:\s*0,\s*Passed:\s*110")
    $exitCode = $LASTEXITCODE

    # Check for any modal dialogs in running processes
    $dialogs = Get-Process | Where-Object { $_.MainWindowTitle -match "Hard Error|System Warning" }

    if ($hasPassed -and ($exitCode -eq 0) -and ($dialogs.Count -eq 0)) {
        Write-Host " PASS (${dur}s, ExitCode: $exitCode, Dialogs: 0)" -ForegroundColor Green
        $bridgeResults += [PSCustomObject]@{
            Iteration = $i
            Passed = $true
            DurationSec = $dur
            ExitCode = $exitCode
            DialogCount = 0
        }
    } else {
        Write-Host " FAIL (${dur}s, ExitCode: $exitCode, Dialogs: $($dialogs.Count))" -ForegroundColor Red
        $bridgeResults += [PSCustomObject]@{
            Iteration = $i
            Passed = $false
            DurationSec = $dur
            ExitCode = $exitCode
            DialogCount = $dialogs.Count
        }
    }
}

Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host "STAGE 2: Consecutive Stress Run of HPGrasshopper.Mcp.Server.Tests" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

$serverResults = @()
for ($i = 1; $i -le 3; $i++) {
    Write-Host "[Server Run $i/3] Starting..." -NoNewline
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $output = dotnet test $serverProjectPath -c Release --no-build 2>&1 | Out-String
    $sw.Stop()
    $dur = [math]::Round($sw.Elapsed.TotalSeconds, 2)
    $hasPassed = ($output -match "Passed!\s*-\s*Failed:\s*0,\s*Passed:\s*77")
    $exitCode = $LASTEXITCODE

    $dialogs = Get-Process | Where-Object { $_.MainWindowTitle -match "Hard Error|System Warning" }

    if ($hasPassed -and ($exitCode -eq 0) -and ($dialogs.Count -eq 0)) {
        Write-Host " PASS (${dur}s, ExitCode: $exitCode, Dialogs: 0)" -ForegroundColor Green
        $serverResults += [PSCustomObject]@{
            Iteration = $i
            Passed = $true
            DurationSec = $dur
            ExitCode = $exitCode
            DialogCount = 0
        }
    } else {
        Write-Host " FAIL (${dur}s, ExitCode: $exitCode, Dialogs: $($dialogs.Count))" -ForegroundColor Red
        $serverResults += [PSCustomObject]@{
            Iteration = $i
            Passed = $false
            DurationSec = $dur
            ExitCode = $exitCode
            DialogCount = $dialogs.Count
        }
    }
}

Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host "SUMMARY REPORT" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
$bridgeResults | Format-Table -AutoSize
$serverResults | Format-Table -AutoSize

$allBridgePass = ($bridgeResults | Where-Object { -not $_.Passed }).Count -eq 0
$allServerPass = ($serverResults | Where-Object { -not $_.Passed }).Count -eq 0

if ($allBridgePass -and $allServerPass) {
    Write-Host "ALL STRESS RUNS PASSED WITH 0 MODAL DIALOGS AND 100% CLEAN EXITS!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "STRESS TESTING FAILED!" -ForegroundColor Red
    exit 1
}
